#version 450

// A point on a line. Positions are in pixels from the centre of the window, with y up.

layout(push_constant) uniform View
{
    vec2 halfSize; // Half the window's size, in pixels.
} view;

layout(location = 0) in vec2 position;
layout(location = 1) in vec4 colour;

layout(location = 0) out vec4 lineColour;

void main()
{
    lineColour = colour;

    // Vulkan's clip space has y down.
    gl_Position = vec4(position.x / view.halfSize.x, -position.y / view.halfSize.y, 0.0, 1.0);
}
