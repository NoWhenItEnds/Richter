#version 450

layout(location = 0) in vec2 offset;
layout(location = 1) in float discRadius;
layout(location = 2) in vec4 discColour;

layout(location = 0) out vec4 colour;

void main()
{
    // Fades out over the edge's last pixel, so a small disc doesn't look jagged.
    float coverage = clamp(discRadius + 0.5 - length(offset), 0.0, 1.0);
    colour = vec4(discColour.rgb, discColour.a * coverage);
}
