#version 450

layout(location = 0) in vec2 fsin_texCoords;
layout(location = 1) in float fsin_Alpha;
layout(location = 0) out vec4 fsout_Color;

layout(set = 1, binding = 0) uniform texture2D SurfaceTexture;
layout(set = 1, binding = 1) uniform sampler SurfaceSampler;

void main() {
    // fsout_Color =  vec4(0,0,0,texture(sampler2D(SurfaceTexture, SurfaceSampler), fsin_texCoords));
    fsout_Color =  vec4(vec3(1,1,1) * (texture(sampler2D(SurfaceTexture, SurfaceSampler), fsin_texCoords).r),fsin_Alpha);
    // fsout_Color =  vec4(1,1,0,1)*texture(sampler2D(SurfaceTexture, SurfaceSampler), fsin_texCoords);
}
