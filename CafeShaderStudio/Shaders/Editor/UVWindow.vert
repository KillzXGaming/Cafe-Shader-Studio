﻿#version 330
layout (location = 0) in vec2 aPos;
layout (location = 1) in vec2 aTexCoords;

uniform mat4 mtxMdl;
uniform mat4 mtxCam;

uniform vec2 scale;
uniform vec2 texCoordScale;

out vec2 TexCoords;

void main()
{
   vec2 scaleCenter = vec2(0.5);

    gl_Position = mtxCam * (vec4(aPos.x, aPos.y, 0.0, 1.0) * vec4(scale, 1, 1)); 
    TexCoords = (aTexCoords - scaleCenter) * texCoordScale + scaleCenter;
}