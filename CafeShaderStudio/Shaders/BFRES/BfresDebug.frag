#version 330

uniform sampler2D UVTestPattern;

uniform int debugShading;

in vec2 texCoord0;
in vec3 normals;
in vec3 boneWeightsColored;
in vec3 tangent;
in vec3 bitangent;

layout (location = 0) out vec4 fragOutput;
layout (location = 1) out vec4 brightColor;

const int DISPLAY_NORMALS = 1;
const int DISPLAY_LIGHTING = 2;
const int DISPLAY_DIFFUSE = 3;
const int DISPLAY_VTX_CLR = 4;
const int DISPLAY_UV = 5;
const int DISPLAY_UV_PATTERN = 6;
const int DISPLAY_WEIGHTS = 7;
const int DISPLAY_TANGENT = 8;
const int DISPLAY_BITANGENT = 9;

void main(){
    vec4 outputColor = vec4(0);
    vec2 displayTexCoord = texCoord0;

    vec3 N = normals;

    if (debugShading == DISPLAY_NORMALS)
    {
        vec3 displayNormal = (N * 0.5) + 0.5;
        outputColor.rgb = displayNormal;
    }

    if (debugShading == DISPLAY_UV)
         outputColor.rgb = vec3(displayTexCoord.x, displayTexCoord.y, 1.0);
    if (debugShading == DISPLAY_UV_PATTERN)
        outputColor.rgb = texture(UVTestPattern, displayTexCoord).rgb;
    if (debugShading == DISPLAY_WEIGHTS)
        outputColor.rgb = boneWeightsColored;
    if (debugShading == DISPLAY_TANGENT)
    {
        vec3 displayTangent = (tangent * 0.5) + 0.5;
        outputColor.rgb = displayTangent;
    }
    if (debugShading == DISPLAY_BITANGENT)
    {
        vec3 displayBitangent = (bitangent * 0.5) + 0.5;
        outputColor.rgb = displayBitangent;
    }

    fragOutput = outputColor;
    brightColor = vec4(0,0,0,1);
}