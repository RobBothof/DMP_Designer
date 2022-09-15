#version 450

layout(location = 0) in float lineEdge;
layout(location = 1) in float lineWidth;
layout(location = 2) in vec4  lineColor;

layout(location = 0) out vec4 fsout_Color;

void main() {
    // fsout_Color = vec4(lineColor.rgb,(1.0-pow(abs(lineEdge*2-1),lineWidth*1.05)));
    fsout_Color = vec4(lineColor.rgb,(1.0-pow(abs(lineEdge*2-1),lineWidth*5*1.05)));
}
