using Brutal.Numerics;
using Brutal.VulkanApi;

namespace Richter.Vulkan;

/// <summary>
/// The window's swapchain: the images that frames are drawn into and presented from, each with a
/// view and a semaphore that says its frame has finished rendering.
/// </summary>
/// <remarks>
/// <para>
/// The semaphores are per image rather than per frame, because presentation can still be waiting
/// on one after its frame's fence has signalled.
/// </para>
/// <para>
/// The format is an 8-bit sRGB one wherever the surface offers one, which nearly every surface
/// does. Shaders write linear colours and the hardware converts them for the screen, so colours
/// look the same whichever format a platform happens to list first. The layers' pipelines are
/// built for this format, and a rebuilt swapchain picks the same one, because the surface lists
/// the same formats every time.
/// </para>
/// </remarks>
internal class Swapchain : IDisposable
{
    private readonly Device _device;
    private readonly PhysicalDevice _physicalDevice;
    private readonly VkSurfaceKHR _surface;
    private VkImage[] _images = [];
    private VkImageView[] _imageViews = [];
    private VkSemaphore[] _renderFinished = [];

    /// <param name="device"> The device that owns the swapchain. It must outlive it. </param>
    /// <param name="physicalDevice"> The device's GPU, asked what the surface supports. </param>
    /// <param name="surface"> The window's surface. It must outlive the swapchain. </param>
    /// <param name="framebufferSize"> The window's framebuffer size, in pixels. </param>
    public Swapchain(Device device, PhysicalDevice physicalDevice, VkSurfaceKHR surface, int2 framebufferSize)
    {
        _device = device;
        _physicalDevice = physicalDevice;
        _surface = surface;
        Create(framebufferSize);
    }

    public VkSwapchainKHR Handle { get; private set; }

    public VkFormat Format { get; private set; }

    /// <summary> The size of the images, in pixels. </summary>
    public VkExtent2D Extent { get; private set; }

    /// <summary>
    /// The window's framebuffer size when the swapchain was built. Wayland never reports a resize
    /// through the swapchain, so the renderer compares this with the window's size each frame.
    /// </summary>
    public int2 FramebufferSize { get; private set; }

    public VkImage ImageAt(Int32 index)
    {
        return _images[index];
    }

    public VkImageView ViewAt(Int32 index)
    {
        return _imageViews[index];
    }

    /// <summary> Signalled when the frame drawn into image <paramref name="index"/> is finished. </summary>
    public VkSemaphore RenderFinishedAt(Int32 index)
    {
        return _renderFinished[index];
    }

    /// <summary>
    /// Replaces the swapchain with one for <paramref name="framebufferSize"/>. Call only while the
    /// GPU is idle, because the old images' views and semaphores are destroyed.
    /// </summary>
    public void Recreate(int2 framebufferSize)
    {
        VkSwapchainKHR old = Handle;
        DestroyImageResources();
        Create(framebufferSize);
        _device.DestroySwapchainKHR(old, null);
    }

    public void Dispose()
    {
        DestroyImageResources();
        _device.DestroySwapchainKHR(Handle, null);
    }

    private void Create(int2 framebufferSize)
    {
        VkSurfaceCapabilitiesKHR capabilities = _physicalDevice.GetSurfaceCapabilitiesKHR(_surface);

        // CurrentExtent is 0xFFFFFFFF (-1 as an Int32) when the surface lets the swapchain
        // choose, which Wayland always does.
        VkExtent2D extent = capabilities.CurrentExtent;
        if (extent.Width == -1 && extent.Height == -1)
        {
            extent = new VkExtent2D(
                Math.Clamp(framebufferSize.X, capabilities.MinImageExtent.Width, capabilities.MaxImageExtent.Width),
                Math.Clamp(framebufferSize.Y, capabilities.MinImageExtent.Height, capabilities.MaxImageExtent.Height));
        }

        // One more than the minimum so acquiring doesn't wait on the driver; a MaxImageCount of
        // 0 means no limit.
        Int32 imageCount = capabilities.MinImageCount + 1;
        if (capabilities.MaxImageCount > 0)
        {
            imageCount = Math.Min(imageCount, capabilities.MaxImageCount);
        }

        VkCompositeAlphaFlagsKHR[] alphaModes =
        [
            VkCompositeAlphaFlagsKHR.OpaqueBitKHR,
            VkCompositeAlphaFlagsKHR.PreMultipliedBitKHR,
            VkCompositeAlphaFlagsKHR.PostMultipliedBitKHR,
            VkCompositeAlphaFlagsKHR.InheritBitKHR,
        ];
        VkCompositeAlphaFlagsKHR compositeAlpha = alphaModes.First(mode => (capabilities.SupportedCompositeAlpha & mode) != 0);

        VkSurfaceFormatKHR[] surfaceFormats = new VkSurfaceFormatKHR[_physicalDevice.GetSurfaceFormatsKHR_Count(_surface)];
        _physicalDevice.GetSurfaceFormatsKHR_Write(_surface, surfaceFormats);
        VkSurfaceFormatKHR surfaceFormat = surfaceFormats
            .Where(format => format.ColorSpace == VkColorSpaceKHR.SRGBNonlinearKHR)
            .Where(format => format.Format is VkFormat.B8G8R8A8SRGB or VkFormat.R8G8B8A8SRGB)
            .DefaultIfEmpty(surfaceFormats[0])
            .First();

        Handle = _device.CreateSwapchainKHR(
            new VkSwapchainCreateInfoKHR
            {
                Surface = _surface,
                MinImageCount = imageCount,
                ImageFormat = surfaceFormat.Format,
                ImageColorSpace = surfaceFormat.ColorSpace,
                ImageExtent = extent,
                ImageArrayLayers = 1,
                ImageUsage = VkImageUsageFlags.ColorAttachmentBit,
                ImageSharingMode = VkSharingMode.Exclusive,
                PreTransform = capabilities.CurrentTransform,
                CompositeAlpha = compositeAlpha,
                // FIFO is the only present mode every implementation must support.
                PresentMode = VkPresentModeKHR.FifoKHR,
                Clipped = true,
                OldSwapchain = Handle,
            },
            null);
        Format = surfaceFormat.Format;
        Extent = extent;
        FramebufferSize = framebufferSize;

        _images = new VkImage[_device.GetSwapchainImagesKHR_Count(Handle)];
        _device.GetSwapchainImagesKHR_Write(Handle, _images);

        _imageViews = new VkImageView[_images.Length];
        _renderFinished = new VkSemaphore[_images.Length];
        for (Int32 i = 0; i < _images.Length; i++)
        {
            _imageViews[i] = _device.CreateImageView(
                new VkImageViewCreateInfo
                {
                    Image = _images[i],
                    ViewType = VkImageViewType._2D,
                    Format = surfaceFormat.Format,
                    SubresourceRange = ColourImage.WholeRange,
                },
                null);
            _renderFinished[i] = _device.CreateSemaphore(new VkSemaphoreCreateInfo(), null);
        }
    }

    private void DestroyImageResources()
    {
        foreach (VkImageView imageView in _imageViews)
        {
            _device.DestroyImageView(imageView, null);
        }

        foreach (VkSemaphore semaphore in _renderFinished)
        {
            _device.DestroySemaphore(semaphore, null);
        }

        _images = [];
        _imageViews = [];
        _renderFinished = [];
    }
}
