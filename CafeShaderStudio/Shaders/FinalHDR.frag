#version 330 core

precision highp float;

uniform vec2 pixelSize;
uniform vec4 highlight_color;
uniform vec4 outline_color;

out vec4 FragColor;
  
in vec2 TexCoords;

uniform int ENABLE_BLOOM;
uniform int ENABLE_LUT;
uniform int ENABLE_SRGB;
uniform int ENABLE_FBO_ALPHA;

uniform sampler2D uColorTex;
uniform sampler2D uBloomTex;
uniform sampler2D uHighlightTex;

const float LUT_SIZE = 8.0;

const float GAMMA = 2.2;


void main()
{             
    vec4 hdrColor = texture(uColorTex, TexCoords).rgba;
    vec4 highlightTex = texture(uHighlightTex, TexCoords).rgba;

    vec3 outputColor = hdrColor.rgb;

    if (ENABLE_BLOOM == 1)
    {
        vec3 bloomColor = texture(uBloomTex, TexCoords).rgb;
        //Add bloom post effects
        outputColor += bloomColor;
    }
    if (ENABLE_SRGB == 1)
    {
        outputColor.rgb = pow(outputColor.rgb, vec3(1.0/GAMMA));
    }
    FragColor.rgb = outputColor.rgb;

    //Used for keeping alpha information if needed
    if (ENABLE_FBO_ALPHA == 1)
        FragColor.a = hdrColor.a;
    else
        FragColor.a = 1.0;
}