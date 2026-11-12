using Brutal.Numerics;
using Brutal.VulkanApi;

namespace Richter.Vulkan;

/// <summary>
/// The window system's side of presenting with Vulkan, so the renderer doesn't depend on a
/// windowing library.
/// </summary>
public interface IVulkanSurfaceSource
{
    /// <summary>
    /// Instance extensions the window system needs for surfaces, such as
    /// <c>VK_KHR_wayland_surface</c>.
    /// </summary>
    IReadOnlyCollection<String> RequiredInstanceExtensions { get; }

    /// <summary>
    /// The drawable size in pixels. Zero in either dimension while the window is minimised.
    /// </summary>
    int2 FramebufferSize { get; }

    /// <summary>
    /// Creates a surface for the window. The caller owns it and destroys it before the instance.
    /// </summary>
    /// <param name="instance">
    /// An instance created with <see cref="RequiredInstanceExtensions"/> enabled.
    /// </param>
    VkSurfaceKHR CreateSurface(Instance instance);
}
