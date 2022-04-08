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


layout(location = 0) in vec2 Position;
layout(location = 1) in vec2 lineProp;

layout(location = 0) out vec2 lineProperties;

void main() {
    // vec2 vp = vec2(1275.0,1340.0);
    // vec4 cameraPosition = View * vec4(Position.x, Position.y, 0, 1);
    // vec4 clipPosition = Projection * cameraPosition;
    // gl_Position = clipPosition;    
    gl_Position = Projection * View * Rotation * vec4(Position.x, Position.y, 0, 1);    
    // gl_Position = pp;

    // vec4 centerscreen_Position = Projection * View * Rotation * vec4(CenterPosition.x, CenterPosition.y, 0, 1);    
    // vLineCenter = 0.5 * (pp.xy + vec2(1,1))*vp;
    lineProperties = lineProp;
    // vLineCenter = 0.5 * (centerscreen_Position.xy+vec2(1,1))*vp;
    // fsin_Color = Color;
}
