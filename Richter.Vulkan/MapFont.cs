using System.Runtime.InteropServices;

namespace Richter.Vulkan;

/// <summary>
/// A font rasterised at one size into a single texture of glyphs (an atlas), for the map's labels.
/// </summary>
/// <remarks>
/// FreeType renders each glyph once, as the game starts, and the atlas is packed in rows. It holds
/// printable ASCII and the Latin-1 letters, so names like "Réunion" draw; anything else draws as a
/// question mark.
/// </remarks>
public class MapFont
{
    /// <summary> The atlas is this many pixels across and down, one byte of coverage a pixel. </summary>
    internal const Int32 AtlasSize = 512;

    private const Int32 PixelHeight = 14;

    /// <summary>
    /// The empty pixels around each glyph. The text shader reads two pixels out from a glyph for
    /// its outline, so three keep it from picking up the next glyph.
    /// </summary>
    private const Int32 Padding = 3;

    private readonly Dictionary<Char, Glyph> _glyphs;

    private MapFont(Byte[] atlas, Dictionary<Char, Glyph> glyphs, Single ascent, Single descent)
    {
        Atlas = atlas;
        _glyphs = glyphs;
        Ascent = ascent;
        Descent = descent;
    }

    /// <summary> Coverage, row by row from the top, <see cref="AtlasSize"/> square. </summary>
    internal Byte[] Atlas { get; }

    /// <summary> Pixels from the baseline up to the top of the tallest letters. </summary>
    internal Single Ascent { get; }

    /// <summary>
    /// Pixels from the baseline down to the bottom of the lowest letters, as a negative number.
    /// </summary>
    internal Single Descent { get; }

    /// <summary>
    /// Reads a TrueType or OpenType font and rasterises it. Throws
    /// <see cref="InvalidDataException"/> if FreeType can't read it.
    /// </summary>
    public static async Task<MapFont> ReadAsync(Stream stream, CancellationToken cancellationToken)
    {
        using MemoryStream copy = new MemoryStream();
        await stream.CopyToAsync(copy, cancellationToken);

        return Rasterise(copy.ToArray());
    }

    /// <summary> The glyph for <paramref name="character"/>, or for '?' if the atlas lacks it. </summary>
    internal Glyph GlyphFor(Char character)
    {
        return _glyphs.TryGetValue(character, out Glyph glyph) ? glyph : _glyphs['?'];
    }

    private static unsafe MapFont Rasterise(Byte[] fontFile)
    {
        Byte[] atlas = new Byte[AtlasSize * AtlasSize];
        Dictionary<Char, Glyph> glyphs = new Dictionary<Char, Glyph>();
        Single ascent;
        Single descent;

        Check(FreeTypeNative.InitFreeType(out IntPtr library), "start FreeType");

        try
        {
            // FreeType reads the font from this memory for as long as the face is open.
            fixed (Byte* file = fontFile)
            {
                Check(FreeTypeNative.NewMemoryFace(library, file, new CLong(fontFile.Length), new CLong(0), out FreeTypeNative.FaceRec* face), "read the font");

                try
                {
                    Check(FreeTypeNative.SetPixelSizes(face, 0, PixelHeight), "size the font");
                    ascent = face->Size->Ascender.Value / 64f;
                    descent = face->Size->Descender.Value / 64f;
                    Pack(face, atlas, glyphs);
                }
                finally
                {
                    FreeTypeNative.DoneFace(face);
                }
            }
        }
        finally
        {
            FreeTypeNative.DoneFreeType(library);
        }

        return new MapFont(atlas, glyphs, ascent, descent);
    }

    /// <summary> Renders each character and packs it into the atlas, left to right in rows. </summary>
    private static unsafe void Pack(FreeTypeNative.FaceRec* face, Byte[] atlas, Dictionary<Char, Glyph> glyphs)
    {
        IEnumerable<Char> characters = Enumerable.Range(' ', '~' - ' ' + 1)
            .Concat(Enumerable.Range(0xA0, 0x100 - 0xA0))
            .Select(code => (Char)code);
        Int32 x = Padding;
        Int32 y = Padding;
        Int32 rowHeight = 0;

        foreach (Char character in characters)
        {
            Check(FreeTypeNative.LoadChar(face, new CULong(character), FreeTypeNative.LoadRender), $"render '{character}'");
            FreeTypeNative.GlyphSlotRec* slot = face->Glyph;
            Int32 width = (Int32)slot->Bitmap.Width;
            Int32 height = (Int32)slot->Bitmap.Rows;

            if (x + width + Padding > AtlasSize)
            {
                x = Padding;
                y += rowHeight + Padding;
                rowHeight = 0;
            }

            if (y + height + Padding > AtlasSize)
            {
                throw new InvalidDataException($"The font's glyphs don't fit in a {AtlasSize}-pixel atlas at {PixelHeight} pixels high.");
            }

            for (Int32 row = 0; row < height; row++)
            {
                new ReadOnlySpan<Byte>(slot->Bitmap.Buffer + row * slot->Bitmap.Pitch, width)
                    .CopyTo(atlas.AsSpan((y + row) * AtlasSize + x, width));
            }

            glyphs[character] = new Glyph(x, y, width, height, slot->BitmapLeft, slot->BitmapTop, slot->AdvanceX.Value / 64f);
            x += width + Padding;
            rowHeight = Math.Max(rowHeight, height);
        }
    }

    private static void Check(Int32 error, String action)
    {
        if (error != 0)
        {
            throw new InvalidDataException($"FreeType couldn't {action} (error {error}).");
        }
    }
}

/// <summary> Where a glyph is in the atlas, and how to place it, all in pixels. </summary>
/// <remarks>
/// <see cref="Left"/> and <see cref="Top"/> are from the pen position on the baseline to the
/// bitmap's top-left corner, with up positive.
/// </remarks>
[StructLayout(LayoutKind.Auto)]
internal readonly struct Glyph
{
    public Glyph(Int32 x, Int32 y, Int32 width, Int32 height, Int32 left, Int32 top, Single advance)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        Left = left;
        Top = top;
        Advance = advance;
    }

    public Int32 X { get; }

    public Int32 Y { get; }

    public Int32 Width { get; }

    public Int32 Height { get; }

    public Int32 Left { get; }

    public Int32 Top { get; }

    public Single Advance { get; }
}
