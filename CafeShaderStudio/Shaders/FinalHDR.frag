#version 330 core
out vec4 FragColor;
  
in vec2 TexCoords;

uniform int ENABLE_BLOOM;
uniform int ENABLE_LUT;
uniform int ENABLE_SRGB;

uniform vec3 backgroundTopColor;
uniform vec3 backgroundBottomColor;

uniform sampler2D uColorTex;
uniform sampler2D uBloomTex;
uniform sampler3D uLutTex;

const float LUT_SIZE = 8.0;

const float GAMMA = 2.2;

void main()
{             
    vec4 hdrColor = texture(uColorTex, TexCoords).rgba;
    vec3 outputColor = hdrColor.rgb;

    if (ENABLE_BLOOM == 1)
    {
        vec3 bloomColor = texture(uBloomTex, TexCoords).rgb;
        //Add bloom post effects
        outputColor += bloomColor;
    }
    if (ENABLE_LUT == 1)
    {
        vec3 scale = vec3((LUT_SIZE - 1.0) / LUT_SIZE);
        vec3 offset = vec3(1.0 / (2.0 * LUT_SIZE));
   
        //Show only right side to compare
        if(TexCoords.x > 0.5 || true)
            outputColor = texture(uLutTex, scale * outputColor + offset).rgb;
    }
    if (ENABLE_SRGB == 1)
    {
        outputColor.rgb = pow(outputColor.rgb, vec3(1.0/GAMMA));
    }

    FragColor.rgb = outputColor.rgb;
    FragColor.a = 1.0;
}