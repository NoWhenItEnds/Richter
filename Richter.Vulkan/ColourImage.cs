using Brutal.VulkanApi;

namespace Richter.Vulkan;

/// <summary>
/// Helpers for the renderer's images, all of which are colour images with one mip level and one
/// layer: the swapchain's and the font atlas.
/// </summary>
internal static class ColourImage
{
    internal static readonly VkImageSubresourceRange WholeRange = new VkImageSubresourceRange
    {
        AspectMask = VkImageAspectFlags.ColorBit,
        LevelCount = 1,
        LayerCount = 1,
    };

    /// <summary> Records a barrier that moves <paramref name="image"/> from one layout to another. </summary>
    internal static void Transition(
        CommandBuffer commandBuffer,
        VkImage image,
        VkImageLayout from,
        VkImageLayout to,
        VkPipelineStageFlags sourceStage,
        VkPipelineStageFlags destinationStage,
        VkAccessFlags sourceAccess,
        VkAccessFlags destinationAccess)
    {
        commandBuffer.PipelineBarrier(
            sourceStage,
            destinationStage,
            VkDependencyFlags.None,
            [],
            [],
            [
                new VkImageMemoryBarrier
                {
                    SrcAccessMask = sourceAccess,
                    DstAccessMask = destinationAccess,
                    OldLayout = from,
                    NewLayout = to,
                    SrcQueueFamilyIndex = VK.QUEUE_FAMILY_IGNORED,
                    DstQueueFamilyIndex = VK.QUEUE_FAMILY_IGNORED,
                    Image = image,
                    SubresourceRange = WholeRange,
                },
            ]);
    }
}
