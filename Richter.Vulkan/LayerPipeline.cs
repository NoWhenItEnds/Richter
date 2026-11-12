using Brutal;
using Brutal.Memory;
using Brutal.Numerics;
using Brutal.VulkanApi;
using Brutal.VulkanApi.Abstractions;

namespace Richter.Vulkan;

/// <summary> Builds the pipelines the map's layers draw with, which differ only in their inputs. </summary>
internal static unsafe class LayerPipeline
{
    /// <summary>
    /// Every layer's vertex shader takes half the framebuffer's size in pixels, to turn pixels into
    /// clip space.
    /// </summary>
    internal static readonly VkPushConstantRange HalfSize = new VkPushConstantRange
    {
        StageFlags = VkShaderStageFlags.VertexBit,
        Size = ByteSize.Of<float2>(),
    };

    /// <summary>
    /// A pipeline that draws to a <paramref name="colourFormat"/> image with dynamic rendering,
    /// blending over what's there, with the viewport and scissor set when it's drawn.
    /// </summary>
    internal static VkPipeline Create(
        Device device,
        VkFormat colourFormat,
        Byte[] vertexSpirv,
        Byte[] fragmentSpirv,
        VertexInput vertexInput,
        PtrOwner<VkPipelineInputAssemblyStateCreateInfo> topology,
        VkPipelineLayout layout)
    {
        // The pipeline keeps what it needs, so the shader modules can go once it's built.
        using ShaderStages stages = new ShaderStages(
            device,
            [
                Presets.ShaderStage.Vertex(ShaderModuleExtensions.FromSpirv(device, vertexSpirv, null)),
                Presets.ShaderStage.Fragment(ShaderModuleExtensions.FromSpirv(device, fragmentSpirv, null)),
            ],
            null);

        VkDynamicState* dynamicStates = stackalloc VkDynamicState[] { VkDynamicState.Viewport, VkDynamicState.Scissor };
        VkPipelineDynamicStateCreateInfo dynamicState = new VkPipelineDynamicStateCreateInfo
        {
            DynamicStateCount = 2,
            DynamicStates = dynamicStates,
        };
        VkPipelineViewportStateCreateInfo viewportState = new VkPipelineViewportStateCreateInfo { ViewportCount = 1, ScissorCount = 1 };
        VkPipelineRenderingCreateInfo rendering = new VkPipelineRenderingCreateInfo
        {
            ColorAttachmentCount = 1,
            ColorAttachmentFormats = &colourFormat,
        };

        return device.CreateGraphicsPipeline(
            VkPipelineCache.NullHandle,
            new VkGraphicsPipelineCreateInfo
            {
                Next = &rendering,
                StageCount = stages.Count,
                Stages = stages,
                VertexInputState = vertexInput,
                InputAssemblyState = topology,
                ViewportState = &viewportState,
                RasterizationState = Presets.Rasterization.Fill.CullNone,
                MultisampleState = Presets.Multisample.None,
                ColorBlendState = Presets.BlendState.BlendColorAlpha,
                DynamicState = &dynamicState,
                Layout = layout,
            },
            null);
    }
}
