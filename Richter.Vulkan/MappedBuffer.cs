using Brutal;
using Brutal.VulkanApi;
using Brutal.VulkanApi.Vma;

namespace Richter.Vulkan;

/// <summary>
/// A GPU buffer the CPU writes into directly, which grows to fit what it's given.
/// </summary>
/// <remarks>
/// Write to it only once the GPU has finished with the last frame, after the frame fence: it's
/// rewritten in place, and growing it destroys the old buffer. With one frame in flight, that's
/// the start of every frame.
/// </remarks>
internal unsafe class MappedBuffer : IDisposable
{
    private readonly VmaAllocator _allocator;
    private readonly VkBufferUsageFlags _usage;
    private VkBuffer _buffer;
    private VmaAllocation _allocation;
    private Byte* _mapped;
    private Int64 _capacity;

    public MappedBuffer(VmaAllocator allocator, VkBufferUsageFlags usage)
    {
        _allocator = allocator;
        _usage = usage;
    }

    /// <summary> Null until the first <see cref="Write{T}"/> of something non-empty. </summary>
    public VkBuffer Buffer => _buffer;

    /// <summary>
    /// Copies <paramref name="items"/> to the start of the buffer, growing it first if needed.
    /// </summary>
    public void Write<T>(ReadOnlySpan<T> items)
        where T : unmanaged
    {
        Int64 size = (Int64)items.Length * sizeof(T);

        if (size > _capacity)
        {
            // Doubling keeps the number of reallocations small as the scene grows.
            Grow(Math.Max(size, _capacity * 2));
        }

        if (size > 0)
        {
            items.CopyTo(new Span<T>(_mapped, items.Length));

            // VMA may pick memory the GPU doesn't see CPU writes to until they're flushed. On
            // coherent memory this does nothing.
            VkResult result = _allocator.FlushAllocation(_allocation, ByteSize.Zero, ByteSize.Of(items));
            if (result != VkResult.Success)
            {
                throw new InvalidOperationException($"Flushing a mapped buffer failed: {result}.");
            }
        }
    }

    public void Dispose()
    {
        Release();
    }

    private void Grow(Int64 capacity)
    {
        Release();
        _allocator.CreateBuffer(
            new VkBufferCreateInfo { Size = new ByteSize64((UInt64)capacity), Usage = _usage },
            new VmaAllocationCreateInfo
            {
                Usage = VmaMemoryUsage.Auto,
                // Persistently mapped, and written in order, which suits memory the CPU writes and
                // the GPU reads.
                Flags = VmaAllocationCreateFlags.HostAccessSequentialWriteBit | VmaAllocationCreateFlags.MappedBit,
            },
            out _buffer,
            out _allocation,
            out VmaAllocationInfo info);
        _mapped = (Byte*)info.PMappedData;
        _capacity = capacity;
    }

    private void Release()
    {
        if (_capacity > 0)
        {
            _allocator.DestroyBuffer(_buffer, _allocation);
            _capacity = 0;
        }
    }
}
