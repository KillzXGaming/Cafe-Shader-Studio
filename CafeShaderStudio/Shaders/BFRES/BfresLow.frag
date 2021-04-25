#version 330

//Samplers
uniform sampler2D diffuseMap;

//Toggles
uniform int hasDiffuseMap;

//GL
uniform mat4 mtxCam;

in vec2 f_texcoord0;
in vec3 fragPosition;

in vec3 normal;
in vec3 viewNormal;

out vec4 fragOutput;
out vec4 brightnessOutput;

void main(){

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
}