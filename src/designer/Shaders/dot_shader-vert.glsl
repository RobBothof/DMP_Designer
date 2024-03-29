#version 450

layout(set = 0, binding = 0) uniform ProjectionBuffer {
    mat4 Projection;
};
layout(set = 0, binding = 1) uniform CameraBuffer {
    mat4 View;
};
layout(set = 0, binding = 2) uniform RotationBuffer {
    mat4 Rotation;
};


layout(location = 0) in vec2 Position;
layout(location = 1) in vec4 Color;
layout(location = 2) in vec2 UV;
layout(location = 3) in float Layer;

layout(location = 0) out vec4 dotColor;
layout(location = 1) out vec2 dotUV;

void main() {
    dotColor = Color;
    dotUV = UV;
    gl_Position = Projection * View * Rotation * vec4(Position.x, Position.y, 0, 1);    
}
