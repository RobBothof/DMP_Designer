#version 450

layout(location = 0) in vec4 dotColor;
layout(location = 1) in vec2 dotUV;
layout(location = 0) out vec4 fsout_Color;

void main() {
    fsout_Color = vec4(dotColor.rgb, 1.0-pow(length(dotUV*2-vec2(1,1)),5) );
}
