#version 450

// A filled disc, drawn as a square of two triangles around it. Positions and sizes are in pixels
// from the centre of the window, with y up.

layout(push_constant) uniform View
{
    vec2 halfSize; // Half the window's size, in pixels.
} view;

layout(location = 0) in vec2 centre;
layout(location = 1) in float radius;
layout(location = 2) in vec4 colour;

layout(location = 0) out vec2 offset;
layout(location = 1) out float discRadius;
layout(location = 2) out vec4 discColour;

const vec2 corners[6] = vec2[](
    vec2(-1.0, -1.0), vec2(1.0, -1.0), vec2(1.0, 1.0),
    vec2(-1.0, -1.0), vec2(1.0, 1.0), vec2(-1.0, 1.0));

void main()
{
    // A pixel more than the radius leaves room for the soft edge.
    offset = corners[gl_VertexIndex] * (radius + 1.0);
    discRadius = radius;
    discColour = colour;

    // Vulkan's clip space has y down.
    vec2 pixel = centre + offset;
    gl_Position = vec4(pixel.x / view.halfSize.x, -pixel.y / view.halfSize.y, 0.0, 1.0);
}
