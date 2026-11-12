#version 450

// One glyph of a label, drawn as a square of two triangles. Unlike the other shaders, positions
// are in framebuffer pixels from the top-left corner, so glyphs land on whole pixels.

layout(push_constant) uniform View
{
    vec2 halfSize; // Half the window's size, in pixels.
} view;

layout(location = 0) in vec4 rectangle; // x, y, width, height
layout(location = 1) in vec4 source;    // the glyph's corners in the atlas: u0, v0, u1, v1
layout(location = 2) in vec4 colour;

layout(location = 0) out vec2 atlasPosition;
layout(location = 1) out vec4 textColour;

const vec2 corners[6] = vec2[](
    vec2(0.0, 0.0), vec2(1.0, 0.0), vec2(1.0, 1.0),
    vec2(0.0, 0.0), vec2(1.0, 1.0), vec2(0.0, 1.0));

void main()
{
    vec2 corner = corners[gl_VertexIndex];
    atlasPosition = mix(source.xy, source.zw, corner);
    textColour = colour;

    // Framebuffer pixels run y down, as Vulkan's clip space does.
    vec2 pixel = rectangle.xy + corner * rectangle.zw;
    gl_Position = vec4(pixel / view.halfSize - 1.0, 0.0, 1.0);
}
