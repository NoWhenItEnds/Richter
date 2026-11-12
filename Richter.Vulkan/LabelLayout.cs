using System.Runtime.InteropServices;
using Brutal.Numerics;

namespace Richter.Vulkan;

/// <summary>
/// Turns a scene's labels into glyph quads, leaving out any label that's off the screen or would
/// overlap one placed before it.
/// </summary>
/// <remarks>
/// Labels are placed in the order the scene added them, so earlier ones win. Positions are
/// rounded to whole framebuffer pixels, which keeps the glyphs as sharp as FreeType drew them.
/// </remarks>
internal class LabelLayout
{
    private const Single Margin = 2;

    private readonly MapFont _font;
    private readonly List<GlyphQuad> _quads = [];
    private readonly List<(Single Left, Single Top, Single Right, Single Bottom)> _placed = [];

    public LabelLayout(MapFont font)
    {
        _font = font;
    }

    /// <summary>
    /// Lays out <paramref name="labels"/> for a framebuffer <paramref name="width"/> by
    /// <paramref name="height"/> pixels. The span is valid until the next call.
    /// </summary>
    public ReadOnlySpan<GlyphQuad> LayOut(IReadOnlyList<Label> labels, Int32 width, Int32 height)
    {
        _quads.Clear();
        _placed.Clear();

        foreach (Label label in labels)
        {
            // From the scene's pixels (from the centre, y up) to the framebuffer's (from the
            // top-left, y down), with the text's middle on the anchor.
            Single textWidth = label.Text.Sum(character => _font.GlyphFor(character).Advance);
            Single left = MathF.Round(label.Anchor.X + width / 2f);
            Single baseline = MathF.Round(height / 2f - label.Anchor.Y + (_font.Ascent + _font.Descent) / 2f);
            (Single Left, Single Top, Single Right, Single Bottom) box = (left, baseline - _font.Ascent, left + textWidth, baseline - _font.Descent);

            if (IsOnScreen(box, width, height) && !_placed.Any(placed => Overlaps(placed, box)))
            {
                _placed.Add(box);
                AddGlyphs(label, left, baseline);
            }
        }

        return CollectionsMarshal.AsSpan(_quads);
    }

    private static Boolean IsOnScreen((Single Left, Single Top, Single Right, Single Bottom) box, Int32 width, Int32 height)
    {
        return box.Right > 0 && box.Left < width && box.Bottom > 0 && box.Top < height;
    }

    private static Boolean Overlaps((Single Left, Single Top, Single Right, Single Bottom) a, (Single Left, Single Top, Single Right, Single Bottom) b)
    {
        return a.Left < b.Right + Margin && b.Left < a.Right + Margin && a.Top < b.Bottom + Margin && b.Top < a.Bottom + Margin;
    }

    private void AddGlyphs(Label label, Single left, Single baseline)
    {
        const Single texel = 1f / MapFont.AtlasSize;
        Single pen = left;

        foreach (Char character in label.Text)
        {
            Glyph glyph = _font.GlyphFor(character);

            // A space has an advance but nothing to draw. Each quad is a pixel bigger than its
            // glyph all round, to leave room for the text shader's dark outline.
            if (glyph.Width > 0 && glyph.Height > 0)
            {
                _quads.Add(new GlyphQuad(
                    new float4(MathF.Round(pen) + glyph.Left - 1, baseline - glyph.Top - 1, glyph.Width + 2, glyph.Height + 2),
                    new float4((glyph.X - 1) * texel, (glyph.Y - 1) * texel, (glyph.X + glyph.Width + 1) * texel, (glyph.Y + glyph.Height + 1) * texel),
                    label.Colour));
            }

            pen += glyph.Advance;
        }
    }
}
