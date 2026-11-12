using Brutal;
using Brutal.Numerics;
using Brutal.VulkanApi;
using Brutal.VulkanApi.Abstractions;
using Brutal.VulkanApi.Vma;

namespace Richter.Vulkan;

/// <summary> Draws the scene's lines, such as orbits and links, each as a strip through its points. </summary>
internal class LineLayer : IMapLayer
{
    internal const String VertexShader = "line.vert";
    internal const String FragmentShader = "line.frag";

    private readonly Device _device;
    private readonly MappedBuffer _buffer;
    private readonly VkPipelineLayout _layout;
    private readonly VkPipeline _pipeline;
    private readonly List<(Int32 First, Int32 Count)> _lines = [];

    /// <param name="device"> The device to draw with. It must outlive this layer. </param>
    /// <param name="allocator"> Allocates the vertex buffer. It must outlive this layer. </param>
    /// <param name="colourFormat"> The format of the images the layer draws to. </param>
    /// <param name="shaders"> SPIR-V by file name, including this layer's two shaders. </param>
    public LineLayer(Device device, VmaAllocator allocator, VkFormat colourFormat, IReadOnlyDictionary<String, Byte[]> shaders)
    {
        _device = device;
        _buffer = new MappedBuffer(allocator, VkBufferUsageFlags.VertexBufferBit);
        _layout = device.CreatePipelineLayout([], [LayerPipeline.HalfSize], null);
        _pipeline = LayerPipeline.Create(
            device,
            colourFormat,
            shaders[VertexShader],
            shaders[FragmentShader],
            new VertexInput(1, 2).AddBindingWithAttributes<LineVertex>(0, 0, VkVertexInputRate.Vertex),
            Presets.InputAssembly.LineStrip,
            _layout);
    }

    /// <inheritdoc/>
    public void Write(MapScene scene, VkExtent2D extent)
    {
        _buffer.Write(scene.LineVertices);
        _lines.Clear();
        _lines.AddRange(scene.Lines);
    }

    /// <inheritdoc/>
    public void Record(CommandBuffer commandBuffer, float2 halfSize)
    {
        if (_lines.Count > 0)
        {
            commandBuffer.BindPipeline(VkPipelineBindPoint.Graphics, _pipeline);
            commandBuffer.PushConstants(_layout, VkShaderStageFlags.VertexBit, ByteSize.Zero, halfSize);
            commandBuffer.BindVertexBuffers(0, [_buffer.Buffer], [new ByteSize64(0)]);

            foreach ((Int32 first, Int32 count) in _lines)
            {
                commandBuffer.Draw(count, 1, first, 0);
            }
        }
    }

    public void Dispose()
    {
        _device.DestroyPipeline(_pipeline, null);
        _device.DestroyPipelineLayout(_layout, null);
        _buffer.Dispose();
    }
}
