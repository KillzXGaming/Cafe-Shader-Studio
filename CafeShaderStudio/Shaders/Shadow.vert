#version 330
layout(location = 0) in vec4 position;

uniform mat4 lightSpaceMatrix;

uniform mat4 mtxMdl;
uniform mat4 mtxCam;

void main()
{
    gl_Position = lightSpaceMatrix * mtxMdl * position;
}  