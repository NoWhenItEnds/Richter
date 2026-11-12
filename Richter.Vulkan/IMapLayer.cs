using Brutal.Numerics;
using Brutal.VulkanApi;

namespace Richter.Vulkan;

/// <summary>
/// One kind of thing the map draws, such as its lines or its labels, with the pipeline and the
/// buffers that draw it.
/// </summary>
/// <remarks>
/// <see cref="WindowRenderer"/> draws its layers in order, each over the ones before. A layer's
/// shaders are named by its <c>VertexShader</c> and <c>FragmentShader</c> constants, which
/// <see cref="WindowRenderer.ShaderFiles"/> lists.
/// </remarks>
internal interface IMapLayer : IDisposable
{
    /// <summary>
    /// Copies this layer's part of <paramref name="scene"/> into its buffers, for a framebuffer of
    /// <paramref name="extent"/>. Call only once the frame fence has signalled, because the
    /// buffers are rewritten in place.
    /// </summary>
    void Write(MapScene scene, VkExtent2D extent);

    /// <summary>
    /// Records the draw for what <see cref="Write"/> last copied, inside the frame's rendering.
    /// </summary>
    /// <param name="commandBuffer"> The frame's command buffer, with rendering begun. </param>
    /// <param name="halfSize"> Half the framebuffer's size in pixels, for the vertex shader. </param>
    void Record(CommandBuffer commandBuffer, float2 halfSize);
}
