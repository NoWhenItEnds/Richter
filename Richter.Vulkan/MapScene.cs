using System.Runtime.InteropServices;
using Brutal.Numerics;

namespace Richter.Vulkan;

/// <summary>
/// What the map draws in one frame: lines, then filled discs over them, then labels. Positions
/// and sizes are in pixels from the centre of the window, with y up.
/// </summary>
/// <remarks>
/// One scene is filled again each frame, so its lists keep their capacity and a frame allocates
/// little once they've grown.
/// </remarks>
public class MapScene
{
    private readonly List<Disc> _discs = [];
    private readonly List<LineVertex> _lineVertices = [];
    private readonly List<(Int32 First, Int32 Count)> _lines = [];
    private readonly List<Label> _labels = [];

    internal ReadOnlySpan<Disc> Discs => CollectionsMarshal.AsSpan(_discs);

    internal ReadOnlySpan<LineVertex> LineVertices => CollectionsMarshal.AsSpan(_lineVertices);

    /// <summary> Each line's first vertex in <see cref="LineVertices"/>, and how many it has. </summary>
    internal IReadOnlyList<(Int32 First, Int32 Count)> Lines => _lines;

    /// <summary> In the order added, which is the order of priority when labels would overlap. </summary>
    internal IReadOnlyList<Label> Labels => _labels;

    public void Clear()
    {
        _discs.Clear();
        _lineVertices.Clear();
        _lines.Clear();
        _labels.Clear();
    }

    /// <summary>
    /// Adds text that starts at <paramref name="anchor"/> and is centred on it from top to bottom.
    /// A label that would overlap one added earlier isn't drawn, so add the most important first.
    /// </summary>
    public void AddLabel(float2 anchor, String text, float4 colour)
    {
        _labels.Add(new Label(anchor, text, colour));
    }

    public void AddDisc(float2 centre, Single radius, float4 colour)
    {
        _discs.Add(new Disc(centre, radius, colour));
    }

    /// <summary> Adds a line through <paramref name="points"/> in order. </summary>
    public void AddLine(ReadOnlySpan<float2> points, float4 colour)
    {
        _lines.Add((_lineVertices.Count, points.Length));

        foreach (float2 point in points)
        {
            _lineVertices.Add(new LineVertex(point, colour));
        }
    }
}

/// <summary> One filled disc, as the disc shader reads it for each instance. </summary>
/// <remarks> The fields' order is the shader's attribute locations: 0, 1 and 2. </remarks>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct Disc
{
    public Disc(float2 centre, Single radius, float4 colour)
    {
        Centre = centre;
        Radius = radius;
        Colour = colour;
    }

    public float2 Centre { get; }

    public Single Radius { get; }

    public float4 Colour { get; }
}

/// <summary> A label to place: text starting at an anchor point, centred on it vertically. </summary>
internal readonly struct Label
{
    public Label(float2 anchor, String text, float4 colour)
    {
        Anchor = anchor;
        Text = text;
        Colour = colour;
    }

    public float2 Anchor { get; }

    public String Text { get; }

    public float4 Colour { get; }
}

/// <summary> One glyph to draw, as the text shader reads it for each instance. </summary>
/// <remarks>
/// <see cref="Rectangle"/> is in framebuffer pixels from the top-left corner (x, y, width,
/// height), so glyphs land on whole pixels and stay sharp. <see cref="Source"/> is the glyph's
/// corners in the atlas (u0, v0, u1, v1).
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct GlyphQuad
{
    public GlyphQuad(float4 rectangle, float4 source, float4 colour)
    {
        Rectangle = rectangle;
        Source = source;
        Colour = colour;
    }

    public float4 Rectangle { get; }

    public float4 Source { get; }

    public float4 Colour { get; }
}

/// <summary> One point of a line, as the line shader reads it. </summary>
/// <remarks> The fields' order is the shader's attribute locations: 0 and 1. </remarks>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct LineVertex
{
    public LineVertex(float2 position, float4 colour)
    {
        Position = position;
        Colour = colour;
    }

    public float2 Position { get; }

    public float4 Colour { get; }
}
