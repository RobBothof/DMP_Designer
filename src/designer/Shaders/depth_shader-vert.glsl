#version 450

layout(set = 0, binding = 0) uniform ProjectionBuffer
{
    mat4 Projection;
};
layout(set = 0, binding = 1) uniform CameraBuffer
{
    mat4 View;
};
layout(set = 0, binding = 2) uniform RotationBuffer
{
    mat4 Rotation;
};

layout(set = 0, binding = 3) uniform TranslationBuffer
{
    vec4 Translation;
};


layout(location = 0) in vec2 Position;
layout(location = 1) in vec2 TexCoords;
layout(location = 2) in float Alpha;

layout(location = 0) out vec2 fsin_texCoords;
layout(location = 1) out float fsin_Alpha;

void main() {
    fsin_Alpha = Alpha;
    fsin_texCoords = TexCoords;
    gl_Position = Projection * View * (Translation + Rotation * (vec4(Position.x, Position.y, 0, 1) - Translation));    
}
