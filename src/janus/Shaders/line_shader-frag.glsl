#version 450

layout(location = 0) in vec2 lineProperties;
layout(location = 0) out vec4 fsout_Color;

layout(set = 0, binding = 3) uniform colorBuffer
{
    vec4 Color;
};

void main() {
    fsout_Color = vec4(Color.rgb,(1.0-pow(abs(lineProperties.x*2-1),lineProperties.y*1.5)));
}
