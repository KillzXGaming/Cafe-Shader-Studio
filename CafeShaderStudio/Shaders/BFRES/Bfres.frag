#version 330

//Samplers
uniform sampler2D diffuseMap;

//Toggles
uniform int hasDiffuseMap;

//GL
uniform mat4 mtxCam;

uniform int colorOverride;

in vec2 f_texcoord0;
in vec3 fragPosition;

in vec4 vertexColor;
in vec3 normal;
in vec3 viewNormal;

uniform bool alphaTest;
uniform int alphaFunc;
uniform float alphaRefValue;

out vec4 fragOutput;
out vec4 brightnessOutput;

float GetComponent(int Type, vec4 Texture);

void main(){

    if (colorOverride == 1)
    {
        fragOutput = vec4(1);
        brightnessOutput =  vec4(0);
        return;
    }

    vec4 bloom = vec4(0);
    vec3 N = normal;
    vec3 displayNormal = (N.xyz * 0.5) + 0.5;

    vec4 diffuseMapColor = vec4(1);
    vec2 texCoord0 = f_texcoord0;

    if (hasDiffuseMap == 1) {
        diffuseMapColor = texture(diffuseMap,texCoord0);
    }

    float halfLambert = max(displayNormal.y,0.5);
    fragOutput = vec4(diffuseMapColor.rgb * halfLambert, diffuseMapColor.a);
    brightnessOutput = bloom;

    //Alpha test. Todo handle these via macros
    if (alphaTest)
    {
        switch (alphaFunc)
        {
            case 0: //gequal
                if (fragOutput.a <= alphaRefValue)
                {
                     discard;
                }
            break;
            case 1: //greater
                if (fragOutput.a < alphaRefValue)
                {
                     discard;
                }
            break;
            case 2: //equal
                if (fragOutput.a == alphaRefValue)
                {
                     discard;
                }
            break;
            case 3: //less
                if (fragOutput.a > alphaRefValue)
                {
                     discard;
                }
            break;
            case 4: //lequal
                if (fragOutput.a >= alphaRefValue)
                {
                     discard;
                }
            break;
        }
     }
}