#version 330

layout(location = 0) in vec3 vPosition;
layout(location = 2) in vec2 vTexCoord;

uniform mat4 mtxMdl;
uniform mat4 mtxCam;
uniform mat4 mtxView;
uniform mat4 mtxProj;

out vec2 f_texcoord0;

void main()
{
    f_texcoord0 = vTexCoord;
    gl_Position = mtxCam*mtxMdl*vec4(vPosition.xyz, 1.0);
}