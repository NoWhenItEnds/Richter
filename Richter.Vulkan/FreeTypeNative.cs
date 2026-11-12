using System.Runtime.InteropServices;

namespace Richter.Vulkan;

/// <summary>
/// The few FreeType functions and structs that <see cref="MapFont"/> needs, bound by hand,
/// because BRUTAL's FreeType package ships only the native library.
/// </summary>
/// <remarks>
/// The structs follow <c>freetype.h</c> and <c>ftimage.h</c> from that package, up to the last
/// field read. C's <c>long</c> (<c>FT_Long</c>, <c>FT_Pos</c>, <c>FT_Fixed</c>) is
/// <see cref="CLong"/>, which is 8 bytes on Linux and macOS but 4 on Windows, so the layouts hold
/// on every platform. Nested C structs stay nested here, because flattening one would lose the
/// padding C puts before it.
/// </remarks>
internal static unsafe partial class FreeTypeNative
{
    /// <summary> <c>FT_LOAD_RENDER</c>: load the glyph and render it to an 8-bit bitmap. </summary>
    internal const Int32 LoadRender = 1 << 2;

    private const String Library = "freetype";

    [LibraryImport(Library, EntryPoint = "FT_Init_FreeType")]
    internal static partial Int32 InitFreeType(out nint library);

    [LibraryImport(Library, EntryPoint = "FT_New_Memory_Face")]
    internal static partial Int32 NewMemoryFace(nint library, Byte* fileBase, CLong fileSize, CLong faceIndex, out FaceRec* face);

    [LibraryImport(Library, EntryPoint = "FT_Set_Pixel_Sizes")]
    internal static partial Int32 SetPixelSizes(FaceRec* face, UInt32 pixelWidth, UInt32 pixelHeight);

    [LibraryImport(Library, EntryPoint = "FT_Load_Char")]
    internal static partial Int32 LoadChar(FaceRec* face, CULong charCode, Int32 loadFlags);

    [LibraryImport(Library, EntryPoint = "FT_Done_Face")]
    internal static partial Int32 DoneFace(FaceRec* face);

    [LibraryImport(Library, EntryPoint = "FT_Done_FreeType")]
    internal static partial Int32 DoneFreeType(IntPtr library);

    /// <summary> <c>FT_Generic</c>. </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct Generic
    {
        public IntPtr Data;
        public IntPtr Finalizer;
    }

    /// <summary> <c>FT_FaceRec</c>, up to <c>size</c>. </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct FaceRec
    {
        public CLong NumFaces;
        public CLong FaceIndex;
        public CLong FaceFlags;
        public CLong StyleFlags;
        public CLong NumGlyphs;
        public IntPtr FamilyName;
        public IntPtr StyleName;
        public Int32 NumFixedSizes;
        public IntPtr AvailableSizes;
        public Int32 NumCharmaps;
        public IntPtr Charmaps;
        public Generic Generic;
        public CLong BoundsXMin;
        public CLong BoundsYMin;
        public CLong BoundsXMax;
        public CLong BoundsYMax;
        public UInt16 UnitsPerEm;
        public Int16 Ascender;
        public Int16 Descender;
        public Int16 Height;
        public Int16 MaxAdvanceWidth;
        public Int16 MaxAdvanceHeight;
        public Int16 UnderlinePosition;
        public Int16 UnderlineThickness;
        public GlyphSlotRec* Glyph;
        public SizeRec* Size;
    }

    /// <summary> <c>FT_SizeRec</c>, with its <c>FT_Size_Metrics</c>, in 26.6 fixed point. </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct SizeRec
    {
        public IntPtr Face;
        public Generic Generic;
        public UInt16 XPixelsPerEm;
        public UInt16 YPixelsPerEm;
        public CLong XScale;
        public CLong YScale;
        public CLong Ascender;
        public CLong Descender;
        public CLong Height;
        public CLong MaxAdvance;
    }

    /// <summary> <c>FT_Bitmap</c>. </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct Bitmap
    {
        public UInt32 Rows;
        public UInt32 Width;
        public Int32 Pitch;
        public Byte* Buffer;
        public UInt16 NumGrays;
        public Byte PixelMode;
        public Byte PaletteMode;
        public IntPtr Palette;
    }

    /// <summary>
    /// <c>FT_GlyphSlotRec</c>, up to <c>bitmap_top</c>. Metrics are in 26.6 fixed point.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct GlyphSlotRec
    {
        public IntPtr Library;
        public IntPtr Face;
        public IntPtr Next;
        public UInt32 GlyphIndex;
        public Generic Generic;
        public CLong Width;
        public CLong Height;
        public CLong HorizontalBearingX;
        public CLong HorizontalBearingY;
        public CLong HorizontalAdvance;
        public CLong VerticalBearingX;
        public CLong VerticalBearingY;
        public CLong VerticalAdvance;
        public CLong LinearHorizontalAdvance;
        public CLong LinearVerticalAdvance;
        public CLong AdvanceX;
        public CLong AdvanceY;
        public Int32 Format;
        public Bitmap Bitmap;
        public Int32 BitmapLeft;
        public Int32 BitmapTop;
    }
}
