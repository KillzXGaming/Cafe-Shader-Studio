#version 330

//Samplers
uniform sampler2D diffuseMap;
uniform sampler2D alphaMap;

//Toggles
uniform int hasDiffuseMap;
uniform int hasAlphaMap;

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
    vec2 texCoord0 = f_texcoord0;

    vec4 alphaMapColor = vec4(1);
    if (hasAlphaMap == 1) {
        alphaMapColor = texture(alphaMap, texCoord0);

        if (alphaMapColor.r == 0) {
            discard;
        }
    }
    

    vec4 diffuseMapColor = vec4(1);
    if (hasDiffuseMap == 1) {
        diffuseMapColor = texture(diffuseMap,texCoord0);

        //Alpha test. Todo handle these via macros
    if (alphaTest)
    {
        switch (alphaFunc)
        {
            case 0: //gequal
                if (diffuseMapColor.a <= alphaRefValue)
                {
                    discard;
                }
            break;
            case 1: //greater
                if (diffuseMapColor.a < alphaRefValue)
                {
                    discard;
                }
            break;
            case 2: //equal
                if (diffuseMapColor.a == alphaRefValue)
                {
                    discard;
                }
            break;
            case 3: //less
                if (diffuseMapColor.a > alphaRefValue)
                {
                    discard;
                }
            break;
            case 4: //lequal
                if (diffuseMapColor.a >= alphaRefValue)
                {
                    discard;
                }
            break;
        }
     }
    }

    if (colorOverride == 1)
    {
        fragOutput = vec4(1);
        brightnessOutput =  vec4(0);
        return;
    }

    vec4 bloom = vec4(0);
    vec3 N = normal;
    vec3 displayNormal = (N.xyz * 0.5) + 0.5;

    float halfLambert = max(displayNormal.y,0.5);
    fragOutput = vec4(diffuseMapColor.rgb * halfLambert, diffuseMapColor.a);
    brightnessOutput = bloom;
}