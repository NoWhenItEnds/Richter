using Brutal;
using Brutal.Numerics;
using Brutal.VulkanApi;
using Brutal.VulkanApi.Abstractions;
using Brutal.VulkanApi.Vma;

namespace Richter.Vulkan;

/// <summary>
/// Draws the scene's labels, one instance a glyph, sampled from the font's atlas.
/// </summary>
/// <remarks>
/// The atlas is copied to the GPU once, when the layer is created, which waits for the copy to
/// finish. Labels are laid out again each frame by <see cref="LabelLayout"/>, which drops any
/// that would overlap one placed earlier.
/// </remarks>
internal unsafe class TextLayer : IMapLayer
{
    internal const String VertexShader = "text.vert";
    internal const String FragmentShader = "text.frag";

    private readonly Device _device;
    private readonly VmaAllocator _allocator;
    private readonly LabelLayout _labelLayout;
    private readonly MappedBuffer _buffer;
    private readonly VkImage _atlas;
    private readonly VmaAllocation _atlasAllocation;
    private readonly VkImageView _atlasView;
    private readonly VkSampler _atlasSampler;
    private readonly VkDescriptorSetLayout _setLayout;
    private readonly VkDescriptorPool _descriptorPool;
    private readonly VkDescriptorSet _descriptorSet;
    private readonly VkPipelineLayout _layout;
    private readonly VkPipeline _pipeline;
    private Int32 _glyphCount;

    /// <param name="device"> The device to draw with. It must outlive this layer. </param>
    /// <param name="allocator"> Allocates the buffers and the atlas. It must outlive this layer. </param>
    /// <param name="colourFormat"> The format of the images the layer draws to. </param>
    /// <param name="shaders"> SPIR-V by file name, including this layer's two shaders. </param>
    /// <param name="font"> The labels' font, already rasterised. </param>
    /// <param name="queue"> The queue that copies the atlas to the GPU. </param>
    /// <param name="commandBuffer"> Records the copy, so it must not be in use. </param>
    public TextLayer(
        Device device,
        VmaAllocator allocator,
        VkFormat colourFormat,
        IReadOnlyDictionary<String, Byte[]> shaders,
        MapFont font,
        Queue queue,
        CommandBuffer commandBuffer)
    {
        _device = device;
        _allocator = allocator;
        _labelLayout = new LabelLayout(font);
        _buffer = new MappedBuffer(allocator, VkBufferUsageFlags.VertexBufferBit);
        (_atlas, _atlasAllocation) = UploadAtlas(font, queue, commandBuffer);
        _atlasView = device.CreateImageView(
            new VkImageViewCreateInfo
            {
                Image = _atlas,
                ViewType = VkImageViewType._2D,
                Format = VkFormat.R8UNorm,
                SubresourceRange = ColourImage.WholeRange,
            },
            null);

        // Nearest filtering, because glyphs are drawn on whole pixels at the size FreeType drew them.
        _atlasSampler = device.CreateSampler(
            new VkSamplerCreateInfo
            {
                MagFilter = VkFilter.Nearest,
                MinFilter = VkFilter.Nearest,
                MipmapMode = VkSamplerMipmapMode.Nearest,
                AddressModeU = VkSamplerAddressMode.ClampToEdge,
                AddressModeV = VkSamplerAddressMode.ClampToEdge,
                AddressModeW = VkSamplerAddressMode.ClampToEdge,
            },
            null);
        _setLayout = CreateSetLayout();
        (_descriptorPool, _descriptorSet) = CreateDescriptorSet(_setLayout, _atlasView, _atlasSampler);

        // Unlike the other layers' pipelines, this one also takes the atlas, as a descriptor set.
        _layout = device.CreatePipelineLayout([_setLayout], [LayerPipeline.HalfSize], null);
        _pipeline = LayerPipeline.Create(
            device,
            colourFormat,
            shaders[VertexShader],
            shaders[FragmentShader],
            new VertexInput(1, 3).AddBindingWithAttributes<GlyphQuad>(0, 0, VkVertexInputRate.Instance),
            Presets.InputAssembly.TriangleList,
            _layout);
    }

    /// <inheritdoc/>
    public void Write(MapScene scene, VkExtent2D extent)
    {
        ReadOnlySpan<GlyphQuad> glyphs = _labelLayout.LayOut(scene.Labels, extent.Width, extent.Height);
        _buffer.Write(glyphs);
        _glyphCount = glyphs.Length;
    }

    /// <inheritdoc/>
    public void Record(CommandBuffer commandBuffer, float2 halfSize)
    {
        if (_glyphCount > 0)
        {
            VkDescriptorSet atlas = _descriptorSet;
            commandBuffer.BindPipeline(VkPipelineBindPoint.Graphics, _pipeline);
            commandBuffer.BindDescriptorSets(VkPipelineBindPoint.Graphics, _layout, 0, new ReadOnlySpan<VkDescriptorSet>(ref atlas), []);
            commandBuffer.PushConstants(_layout, VkShaderStageFlags.VertexBit, ByteSize.Zero, halfSize);
            commandBuffer.BindVertexBuffers(0, [_buffer.Buffer], [new ByteSize64(0)]);

            // Six vertices make each glyph's square; the shader places them, one instance a glyph.
            commandBuffer.Draw(6, _glyphCount, 0, 0);
        }
    }

    public void Dispose()
    {
        _device.DestroyPipeline(_pipeline, null);
        _device.DestroyPipelineLayout(_layout, null);
        _device.DestroyDescriptorPool(_descriptorPool, null);
        _device.DestroyDescriptorSetLayout(_setLayout, null);
        _device.DestroySampler(_atlasSampler, null);
        _device.DestroyImageView(_atlasView, null);
        _allocator.DestroyImage(_atlas, _atlasAllocation);
        _buffer.Dispose();
    }

    /// <summary>
    /// Creates the font atlas as a sampled texture, copies the font's pixels into it through a
    /// staging buffer, and waits for the copy to finish.
    /// </summary>
    private (VkImage Image, VmaAllocation Allocation) UploadAtlas(MapFont font, Queue queue, CommandBuffer commandBuffer)
    {
        VkExtent3D size = new VkExtent3D { Width = MapFont.AtlasSize, Height = MapFont.AtlasSize, Depth = 1 };
        _allocator.CreateImage(
            new VkImageCreateInfo
            {
                ImageType = VkImageType._2D,
                Format = VkFormat.R8UNorm,
                Extent = size,
                MipLevels = 1,
                ArrayLayers = 1,
                Samples = VkSampleCountFlags._1Bit,
                Tiling = VkImageTiling.Optimal,
                Usage = VkImageUsageFlags.TransferDstBit | VkImageUsageFlags.SampledBit,
                SharingMode = VkSharingMode.Exclusive,
                InitialLayout = VkImageLayout.Undefined,
            },
            new VmaAllocationCreateInfo { Usage = VmaMemoryUsage.Auto },
            out VkImage image,
            out VmaAllocation allocation,
            out _);

        using MappedBuffer staging = new MappedBuffer(_allocator, VkBufferUsageFlags.TransferSrcBit);
        staging.Write<Byte>(font.Atlas);

        commandBuffer.Reset(VkCommandBufferResetFlags.None);
        commandBuffer.Begin(new VkCommandBufferBeginInfo { Flags = VkCommandBufferUsageFlags.OneTimeSubmitBit });
        ColourImage.Transition(
            commandBuffer,
            image,
            VkImageLayout.Undefined,
            VkImageLayout.TransferDstOptimal,
            VkPipelineStageFlags.TopOfPipeBit,
            VkPipelineStageFlags.TransferBit,
            VkAccessFlags.None,
            VkAccessFlags.TransferWriteBit);
        VkBufferImageCopy region = new VkBufferImageCopy
        {
            ImageSubresource = new VkImageSubresourceLayers { AspectMask = VkImageAspectFlags.ColorBit, LayerCount = 1 },
            ImageExtent = size,
        };
        commandBuffer.CopyBufferToImage(staging.Buffer, image, VkImageLayout.TransferDstOptimal, new ReadOnlySpan<VkBufferImageCopy>(ref region));
        ColourImage.Transition(
            commandBuffer,
            image,
            VkImageLayout.TransferDstOptimal,
            VkImageLayout.ShaderReadOnlyOptimal,
            VkPipelineStageFlags.TransferBit,
            VkPipelineStageFlags.FragmentShaderBit,
            VkAccessFlags.TransferWriteBit,
            VkAccessFlags.ShaderReadBit);
        commandBuffer.End();

        VkCommandBuffer handle = commandBuffer.Handle;
        VkSubmitInfo submitInfo = new VkSubmitInfo { CommandBufferCount = 1, CommandBuffers = &handle };
        queue.Submit(new Span<VkSubmitInfo>(ref submitInfo), VkFence.NullHandle);

        // Once, at start-up, so waiting for the whole queue is simpler than a fence of its own.
        queue.WaitIdle();

        return (image, allocation);
    }

    private VkDescriptorSetLayout CreateSetLayout()
    {
        VkDescriptorSetLayoutBinding atlasBinding = new VkDescriptorSetLayoutBinding
        {
            Binding = 0,
            DescriptorType = VkDescriptorType.CombinedImageSampler,
            DescriptorCount = 1,
            StageFlags = VkShaderStageFlags.FragmentBit,
        };

        return _device.CreateDescriptorSetLayout(new VkDescriptorSetLayoutCreateInfo { BindingCount = 1, Bindings = &atlasBinding }, null);
    }

    /// <summary> A pool with room for one set, and that set, pointing at the atlas. </summary>
    private (VkDescriptorPool Pool, VkDescriptorSet Set) CreateDescriptorSet(VkDescriptorSetLayout layout, VkImageView atlasView, VkSampler sampler)
    {
        VkDescriptorPoolSize poolSize = new VkDescriptorPoolSize { Type = VkDescriptorType.CombinedImageSampler, DescriptorCount = 1 };
        VkDescriptorPool pool = _device.CreateDescriptorPool(
            new VkDescriptorPoolCreateInfo { MaxSets = 1, PoolSizeCount = 1, PoolSizes = &poolSize },
            null);

        VkDescriptorSet set = default;
        _device.AllocateDescriptorSets(
            new VkDescriptorSetAllocateInfo { DescriptorPool = pool, DescriptorSetCount = 1, SetLayouts = &layout },
            new ReadOnlySpan<VkDescriptorSet>(ref set));

        VkDescriptorImageInfo atlas = new VkDescriptorImageInfo
        {
            Sampler = sampler,
            ImageView = atlasView,
            ImageLayout = VkImageLayout.ShaderReadOnlyOptimal,
        };
        VkWriteDescriptorSet write = new VkWriteDescriptorSet
        {
            DstSet = set,
            DstBinding = 0,
            DescriptorCount = 1,
            DescriptorType = VkDescriptorType.CombinedImageSampler,
            ImageInfo = &atlas,
        };
        _device.UpdateDescriptorSets(new ReadOnlySpan<VkWriteDescriptorSet>(ref write), []);

        return (pool, set);
    }
}
