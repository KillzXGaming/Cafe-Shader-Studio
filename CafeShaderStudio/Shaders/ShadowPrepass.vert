#version 330
layout (location = 0) in vec2 aPos;
layout (location = 1) in vec2 aTexCoords;

uniform sampler2D depthTexture;

out vec2 TexCoords;
out vec3 ScreenCoords;

void main()
{
    gl_Position = vec4(aPos.x, aPos.y, 0.0, 1.0);   
    TexCoords = aTexCoords;

    ScreenCoords.xy = gl_Position.xy / gl_Position.w;
    ScreenCoords.z = 1.0;
}