using Brutal.Numerics;
using Brutal.VulkanApi;
using Brutal.VulkanApi.Abstractions;
using Brutal.VulkanApi.Vma;

namespace Richter.Vulkan;

/// <summary> Draws a <see cref="MapScene"/> to a window with Vulkan, each frame. </summary>
/// <remarks>
/// <para>
/// Owns the Vulkan instance, the device, the <see cref="Swapchain"/> and the map's layers, and
/// draws the layers in order: <see cref="LineLayer"/>, then <see cref="DiscLayer"/> over it,
/// then <see cref="TextLayer"/>. The viewport is dynamic state, so a resize needs a new swapchain
/// but not new pipelines.
/// </para>
/// <para>
/// One frame is in flight at a time, held there by the frame fence, so each layer can rewrite its
/// buffers in place once the fence has signalled. A second frame in flight would need a set of
/// buffers for each.
/// </para>
/// <para>
/// Wayland compositors don't show a window until it presents a frame, so a window with no
/// renderer never appears.
/// </para>
/// </remarks>
public unsafe class WindowRenderer : IDisposable
{
    private static readonly float4 ClearColour = new float4(0.1f, 0.1f, 0.1f, 1.0f);

    private readonly IVulkanSurfaceSource _surfaceSource;
    private readonly Instance _instance;
    private readonly VkSurfaceKHR _surface;
    private readonly Device _device;
    private readonly Queue _queue;
    private readonly VkCommandPool _commandPool;
    private readonly CommandBuffer _commandBuffer;
    private readonly VkSemaphore _imageAvailable;
    private readonly VkFence _frameFence;
    private readonly VmaAllocator _allocator;
    private readonly Swapchain _swapchain;
    private readonly IMapLayer[] _layers;
    private Boolean _swapchainOutOfDate;

    /// <summary>
    /// Creates the Vulkan instance, device, swapchain and layers for a window created with no
    /// client API.
    /// </summary>
    /// <param name="surfaceSource"> The window to draw to. It must outlive this renderer. </param>
    /// <param name="shaders"> SPIR-V for each of <see cref="ShaderFiles"/>, by file name. </param>
    /// <param name="font"> The labels' font, already rasterised. </param>
    public WindowRenderer(IVulkanSurfaceSource surfaceSource, IReadOnlyDictionary<String, Byte[]> shaders, MapFont font)
    {
        _surfaceSource = surfaceSource;

        HashSet<String> instanceExtensions = new HashSet<String>(surfaceSource.RequiredInstanceExtensions);
        VulkanHelpers.AddPlatformRequiredExtensions(instanceExtensions);

        // Fully qualified because this namespace, Richter.Vulkan, hides Brutal's Vulkan class.
        _instance = Brutal.VulkanApi.Vulkan.CreateInstance(new Instance.CreateInfo
        {
            // 1.3 for dynamic rendering, which avoids render pass and framebuffer objects.
            AppInfo = new VkApplicationInfo { ApiVersion = (Int32)VulkanHelpers.Api.VERSION_1_3 },
            Flags = VulkanHelpers.GetPlatformRequiredInstanceCreateFlags(),
            EnabledExtensions = instanceExtensions,
            EnabledLayers = [],
        });

        _surface = surfaceSource.CreateSurface(_instance);

        // One queue draws and presents, so pick the first device with a queue family that can
        // do both.
        PhysicalDevice physicalDevice = _instance.PhysicalDevices
            .FirstOrDefault(candidate => candidate.SupportsExtension(VK.KHR_SWAPCHAIN_EXTENSION_NAME) && DrawAndPresentFamily(candidate, _surface) >= 0)
            ?? throw new InvalidOperationException("No Vulkan device can both draw and present to the window.");
        Int32 queueFamily = DrawAndPresentFamily(physicalDevice, _surface);

        HashSet<String> deviceExtensions = new HashSet<String>();
        VulkanHelpers.AddSwapchainExtensions(deviceExtensions);
        VulkanHelpers.AddMandatoryIfSupportedDeviceExtensions(deviceExtensions, physicalDevice);

        Single priority = 1.0f;
        VkPhysicalDeviceVulkan13Features vulkan13Features = new VkPhysicalDeviceVulkan13Features { DynamicRendering = true };
        VkDeviceQueueCreateInfo queueCreateInfo = new VkDeviceQueueCreateInfo
        {
            QueueFamilyIndex = queueFamily,
            QueueCount = 1,
            QueuePriorities = &priority,
        };

        _device = physicalDevice.CreateDevice(new Device.CreateInfo
        {
            Extensions = deviceExtensions,
            QueueCreateInfos = new ReadOnlySpan<VkDeviceQueueCreateInfo>(ref queueCreateInfo),
            Next = &vulkan13Features,
        });
        _queue = _device.GetQueue(queueFamily, 0);

        _commandPool = _device.CreateCommandPool(
            new VkCommandPoolCreateInfo
            {
                QueueFamilyIndex = queueFamily,
                Flags = VkCommandPoolCreateFlags.ResetCommandBufferBit,
            },
            null);
        _commandBuffer = _device.AllocateCommandBuffer(
            new VkCommandBufferAllocateInfo { CommandPool = _commandPool, Level = VkCommandBufferLevel.Primary });

        _imageAvailable = _device.CreateSemaphore(new VkSemaphoreCreateInfo(), null);
        // Created signalled so the first frame doesn't wait on a frame that was never submitted.
        _frameFence = _device.CreateFence(new VkFenceCreateInfo { Flags = VkFenceCreateFlags.SignaledBit }, null);

        _allocator = _device.CreateVmaAllocator(new VmaAllocatorCreateInfo
        {
            Instance = _instance,
            PhysicalDevice = physicalDevice,
            Device = _device,
            VulkanApiVersion = (UInt32)VulkanHelpers.Api.VERSION_1_3,
        });

        // The swapchain's format is the pipelines' colour format, so it comes first.
        _swapchain = new Swapchain(_device, physicalDevice, _surface, surfaceSource.FramebufferSize);
        _layers =
        [
            new LineLayer(_device, _allocator, _swapchain.Format, shaders),
            new DiscLayer(_device, _allocator, _swapchain.Format, shaders),
            new TextLayer(_device, _allocator, _swapchain.Format, shaders, font, _queue, _commandBuffer),
        ];
    }

    /// <summary>
    /// The GLSL files in the game's <c>Data/Shaders</c> that the layers draw with. The caller
    /// compiles them before creating a renderer, so a broken one is reported before any window
    /// opens.
    /// </summary>
    public static IReadOnlyList<String> ShaderFiles { get; } =
    [
        LineLayer.VertexShader,
        LineLayer.FragmentShader,
        DiscLayer.VertexShader,
        DiscLayer.FragmentShader,
        TextLayer.VertexShader,
        TextLayer.FragmentShader,
    ];

    /// <summary>
    /// Draws <paramref name="scene"/> to the next swapchain image and presents it, rebuilding the
    /// swapchain first if the window changed size.
    /// </summary>
    /// <remarks>
    /// Call only while the framebuffer has a non-zero size; a minimised window has nothing to
    /// present to.
    /// </remarks>
    public void DrawFrame(MapScene scene)
    {
        int2 framebufferSize = _surfaceSource.FramebufferSize;
        if (_swapchainOutOfDate || framebufferSize != _swapchain.FramebufferSize)
        {
            _device.WaitIdle();
            _swapchain.Recreate(framebufferSize);
            _swapchainOutOfDate = false;
        }

        _device.WaitForFence(_frameFence, -1);

        VkResult acquireResult;
        Int32 imageIndex = 0;
        try
        {
            acquireResult = _device.AcquireNextImageKHR(_swapchain.Handle, -1, _imageAvailable, VkFence.NullHandle, out imageIndex);
        }
        catch (VkResultError error) when (error.Result == VkResult.ErrorOutOfDateKHR)
        {
            acquireResult = error.Result;
        }

        if (acquireResult == VkResult.ErrorOutOfDateKHR)
        {
            // Nothing was acquired, so the fence stays signalled and the next frame starts with
            // a fresh swapchain.
            _swapchainOutOfDate = true;
        }
        else
        {
            // A suboptimal image is still valid and its semaphore will signal, so present it
            // before rebuilding.
            _swapchainOutOfDate = acquireResult == VkResult.SuboptimalKHR;

            // Reset only after a successful acquire; resetting earlier would leave the next wait
            // blocked for ever.
            _device.ResetFences([_frameFence]);

            // The fence has signalled, so the GPU is done with the last frame's buffers.
            foreach (IMapLayer layer in _layers)
            {
                layer.Write(scene, _swapchain.Extent);
            }

            RecordFrame(imageIndex);
            Submit(imageIndex);
            Present(imageIndex);
        }
    }

    /// <summary>
    /// Waits for the GPU to finish, then destroys every Vulkan object this renderer created.
    /// </summary>
    public void Dispose()
    {
        _device.WaitIdle();

        foreach (IMapLayer layer in _layers)
        {
            layer.Dispose();
        }

        _device.DestroyVmaAllocator(_allocator);
        _swapchain.Dispose();
        _device.DestroyFence(_frameFence, null);
        _device.DestroySemaphore(_imageAvailable, null);
        _device.DestroyCommandPool(_commandPool, null);
        _device.Dispose();

        _instance.DestroySurfaceKHR(_surface, null);
        _instance.Dispose();
    }

    /// <summary>
    /// The first of <paramref name="device"/>'s queue families that can both draw and present to
    /// <paramref name="surface"/>, or -1 if none can.
    /// </summary>
    private static Int32 DrawAndPresentFamily(PhysicalDevice device, VkSurfaceKHR surface)
    {
        VkQueueFamilyProperties[] families = new VkQueueFamilyProperties[device.GetQueueFamilyProperties_Count()];
        device.GetQueueFamilyProperties_Write(families);

        return Enumerable.Range(0, families.Length)
            .Where(index => (families[index].QueueFlags & VkQueueFlags.GraphicsBit) != 0 && device.GetSurfaceSupportKHR(index, surface))
            .DefaultIfEmpty(-1)
            .First();
    }

    private void RecordFrame(Int32 imageIndex)
    {
        VkImage image = _swapchain.ImageAt(imageIndex);
        VkExtent2D extent = _swapchain.Extent;

        _commandBuffer.Reset(VkCommandBufferResetFlags.None);
        _commandBuffer.Begin(new VkCommandBufferBeginInfo { Flags = VkCommandBufferUsageFlags.OneTimeSubmitBit });

        ColourImage.Transition(
            _commandBuffer,
            image,
            VkImageLayout.Undefined,
            VkImageLayout.ColorAttachmentOptimal,
            VkPipelineStageFlags.ColorAttachmentOutputBit,
            VkPipelineStageFlags.ColorAttachmentOutputBit,
            VkAccessFlags.None,
            VkAccessFlags.ColorAttachmentWriteBit);

        VkRenderingAttachmentInfo colourAttachment = new VkRenderingAttachmentInfo
        {
            ImageView = _swapchain.ViewAt(imageIndex),
            ImageLayout = VkImageLayout.ColorAttachmentOptimal,
            LoadOp = VkAttachmentLoadOp.Clear,
            StoreOp = VkAttachmentStoreOp.Store,
            ClearValue = new VkClearColorValue { Float32 = ClearColour },
        };

        _commandBuffer.BeginRendering(new VkRenderingInfo
        {
            RenderArea = new VkRect2D(extent),
            LayerCount = 1,
            ColorAttachmentCount = 1,
            ColorAttachments = &colourAttachment,
        });

        VkViewport viewport = new VkViewport { Width = extent.Width, Height = extent.Height, MaxDepth = 1 };
        VkRect2D scissor = new VkRect2D(extent);
        _commandBuffer.SetViewport(0, new ReadOnlySpan<VkViewport>(ref viewport));
        _commandBuffer.SetScissor(0, new ReadOnlySpan<VkRect2D>(ref scissor));
        float2 halfSize = new float2(extent.Width / 2f, extent.Height / 2f);

        foreach (IMapLayer layer in _layers)
        {
            layer.Record(_commandBuffer, halfSize);
        }

        _commandBuffer.EndRendering();

        ColourImage.Transition(
            _commandBuffer,
            image,
            VkImageLayout.ColorAttachmentOptimal,
            VkImageLayout.PresentSrcKHR,
            VkPipelineStageFlags.ColorAttachmentOutputBit,
            VkPipelineStageFlags.BottomOfPipeBit,
            VkAccessFlags.ColorAttachmentWriteBit,
            VkAccessFlags.None);

        _commandBuffer.End();
    }

    private void Submit(Int32 imageIndex)
    {
        VkSemaphore waitSemaphore = _imageAvailable;
        VkPipelineStageFlags waitStage = VkPipelineStageFlags.ColorAttachmentOutputBit;
        VkCommandBuffer commandBuffer = _commandBuffer.Handle;
        VkSemaphore signalSemaphore = _swapchain.RenderFinishedAt(imageIndex);

        VkSubmitInfo submitInfo = new VkSubmitInfo
        {
            WaitSemaphoreCount = 1,
            WaitSemaphores = &waitSemaphore,
            WaitDstStageMask = &waitStage,
            CommandBufferCount = 1,
            CommandBuffers = &commandBuffer,
            SignalSemaphoreCount = 1,
            SignalSemaphores = &signalSemaphore,
        };
        _queue.Submit(new Span<VkSubmitInfo>(ref submitInfo), _frameFence);
    }

    private void Present(Int32 imageIndex)
    {
        VkSemaphore waitSemaphore = _swapchain.RenderFinishedAt(imageIndex);
        VkSwapchainKHR swapchain = _swapchain.Handle;

        VkResult presentResult;
        try
        {
            presentResult = _queue.PresentKHR(new VkPresentInfoKHR
            {
                WaitSemaphoreCount = 1,
                WaitSemaphores = &waitSemaphore,
                SwapchainCount = 1,
                Swapchains = &swapchain,
                ImageIndices = &imageIndex,
            });
        }
        catch (VkResultError error) when (error.Result == VkResult.ErrorOutOfDateKHR)
        {
            presentResult = error.Result;
        }

        if (presentResult != VkResult.Success)
        {
            _swapchainOutOfDate = true;
        }
    }
}
