#version 330
layout (location = 0) in vec3 vPositon;

uniform mat4 lightSpaceMatrix;

uniform mat4 mtxMdl;
uniform mat4 mtxCam;

void main()
{
    gl_Position = lightSpaceMatrix * mtxMdl * vec4(vPositon, 1.0);
}  