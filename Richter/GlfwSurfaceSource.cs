using System.Runtime.InteropServices;
using Brutal.GlfwApi;
using Brutal.Numerics;
using Brutal.VulkanApi;
using Richter.Vulkan;

namespace Richter;

/// <summary> Supplies Vulkan surfaces for a GLFW window. </summary>
public unsafe class GlfwSurfaceSource : IVulkanSurfaceSource
{
    private readonly GlfwWindow _window;

    /// <summary> Wraps a window created with no client API. </summary>
    public GlfwSurfaceSource(GlfwWindow window)
    {
        Byte** names = Glfw.GetRequiredInstanceExtensions(out UInt32 count);
        if (names == null)
        {
            throw new InvalidOperationException(
                "GLFW found no Vulkan loader, or no surface support for this window system.");
        }

        _window = window;

        // Holds the surface extensions for whichever window system GLFW picked (Win32, Wayland,
        // X11, Metal).
        HashSet<String> extensions = new HashSet<String>();
        for (Int32 i = 0; i < count; i++)
        {
            extensions.Add(Marshal.PtrToStringUTF8((IntPtr)names[i])!);
        }

        RequiredInstanceExtensions = extensions;
    }

    /// <inheritdoc/>
    public IReadOnlyCollection<String> RequiredInstanceExtensions { get; }

    /// <inheritdoc/>
    public int2 FramebufferSize => _window.FramebufferSize;

    /// <summary>
    /// Makes GLFW use the Vulkan loader that Brutal.Vulkan loaded. Call before
    /// <see cref="Glfw.Init"/>.
    /// </summary>
    /// <remarks>
    /// GLFW otherwise searches for a loader itself. On macOS Brutal can fall back to a bundled
    /// loader that GLFW wouldn't find, and the surface must come from the same loader as the
    /// instance.
    /// </remarks>
    public static void ShareVulkanLoader()
    {
        // Fully qualified because the Richter.Vulkan namespace hides Brutal's Vulkan class here.
        Glfw.PInvoke.InitVulkanLoader((IntPtr)Brutal.VulkanApi.Vulkan.GetInstanceProcAddr.Invoke);
    }

    /// <inheritdoc/>
    public VkSurfaceKHR CreateSurface(Instance instance)
    {
        VkResult result = (VkResult)Glfw.CreateWindowSurface(instance.VkHandle, _window, out UInt64 surfaceHandle);

        VkSurfaceKHR surface;
        if (result == VkResult.Success)
        {
            surface = new VkSurfaceKHR((IntPtr)surfaceHandle);
        }
        else
        {
            throw new VkResultError(result, $"Failed to create a Vulkan surface for the window: {result}.");
        }

        return surface;
    }
}
