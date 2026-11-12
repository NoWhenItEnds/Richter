using Brutal;
using Brutal.Numerics;
using Brutal.VulkanApi;
using Brutal.VulkanApi.Abstractions;
using Brutal.VulkanApi.Vma;

namespace Richter.Vulkan;

/// <summary> Draws the scene's filled discs, such as bodies and networks. </summary>
/// <remarks>
/// Each disc is one instance of six vertices: a square of two triangles that the vertex shader
/// places and the fragment shader rounds off.
/// </remarks>
internal class DiscLayer : IMapLayer
{
    internal const String VertexShader = "disc.vert";
    internal const String FragmentShader = "disc.frag";

    private readonly Device _device;
    private readonly MappedBuffer _buffer;
    private readonly VkPipelineLayout _layout;
    private readonly VkPipeline _pipeline;
    private Int32 _discCount;

    /// <param name="device"> The device to draw with. It must outlive this layer. </param>
    /// <param name="allocator"> Allocates the instance buffer. It must outlive this layer. </param>
    /// <param name="colourFormat"> The format of the images the layer draws to. </param>
    /// <param name="shaders"> SPIR-V by file name, including this layer's two shaders. </param>
    public DiscLayer(Device device, VmaAllocator allocator, VkFormat colourFormat, IReadOnlyDictionary<String, Byte[]> shaders)
    {
        _device = device;
        _buffer = new MappedBuffer(allocator, VkBufferUsageFlags.VertexBufferBit);
        _layout = device.CreatePipelineLayout([], [LayerPipeline.HalfSize], null);
        _pipeline = LayerPipeline.Create(
            device,
            colourFormat,
            shaders[VertexShader],
            shaders[FragmentShader],
            new VertexInput(1, 3).AddBindingWithAttributes<Disc>(0, 0, VkVertexInputRate.Instance),
            Presets.InputAssembly.TriangleList,
            _layout);
    }

    /// <inheritdoc/>
    public void Write(MapScene scene, VkExtent2D extent)
    {
        _buffer.Write(scene.Discs);
        _discCount = scene.Discs.Length;
    }

    /// <inheritdoc/>
    public void Record(CommandBuffer commandBuffer, float2 halfSize)
    {
        if (_discCount > 0)
        {
            commandBuffer.BindPipeline(VkPipelineBindPoint.Graphics, _pipeline);
            commandBuffer.PushConstants(_layout, VkShaderStageFlags.VertexBit, ByteSize.Zero, halfSize);
            commandBuffer.BindVertexBuffers(0, [_buffer.Buffer], [new ByteSize64(0)]);
            commandBuffer.Draw(6, _discCount, 0, 0);
        }
    }

    public void Dispose()
    {
        _device.DestroyPipeline(_pipeline, null);
        _device.DestroyPipelineLayout(_layout, null);
        _buffer.Dispose();
    }
}
