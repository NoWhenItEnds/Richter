#version 450

layout(set = 0, binding = 0) uniform sampler2D atlas;

layout(location = 0) in vec2 atlasPosition;
layout(location = 1) in vec4 textColour;

layout(location = 0) out vec4 colour;

// The outline that keeps text readable over discs and lines.
const vec3 outlineColour = vec3(0.08, 0.08, 0.08);

void main()
{
    // The atlas holds how much of each pixel the glyph covers. A pixel next to the glyph gets the
    // outline: the most any of its neighbours is covered.
    vec2 texel = 1.0 / vec2(textureSize(atlas, 0));
    float coverage = texture(atlas, atlasPosition).r;
    float outline = 0.0;

    for (int y = -1; y <= 1; y++)
    {
        for (int x = -1; x <= 1; x++)
        {
            outline = max(outline, texture(atlas, atlasPosition + vec2(x, y) * texel).r);
        }
    }

    colour = vec4(mix(outlineColour, textColour.rgb, coverage), textColour.a * max(coverage, outline));
}
