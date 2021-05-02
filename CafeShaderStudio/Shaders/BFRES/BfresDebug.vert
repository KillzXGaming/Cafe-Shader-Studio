﻿#version 330

in vec3 vPositon;
in vec3 vNormal;
in vec2 vTexCoord0;
in vec2 vTexCoord1;
in vec2 vTexCoord2;
in vec4 vColor;
in ivec4 vBoneIndex;
in vec4 vBoneWeight;
in vec3 vTangent;
in vec3 vBitangent;

uniform mat4 mtxMdl;
uniform mat4 mtxCam;

// Skinning uniforms
uniform mat4 bones[100];
uniform int SkinCount;
uniform int UseSkinning;
uniform int BoneIndex;

uniform sampler2D weightRamp1;
uniform sampler2D weightRamp2;
uniform int selectedBoneIndex;
uniform int weightRampType;

out vec2 texCoord0;
out vec3 normals;
out vec3 boneWeightsColored;
out vec3 tangent;
out vec3 bitangent;
out vec4 vertexColor;

vec4 skin(vec3 pos, ivec4 index)
{
    vec4 newPosition = vec4(pos.xyz, 1.0);
    if (SkinCount == 1) //Rigid
    {
        newPosition = bones[index.x] * vec4(pos, 1.0);
    }
    else //Smooth
    {
        newPosition = bones[index.x] * vec4(pos, 1.0) * vBoneWeight.x;
        newPosition += bones[index.y] * vec4(pos, 1.0) * vBoneWeight.y;
        newPosition += bones[index.z] * vec4(pos, 1.0) * vBoneWeight.z;
        if (vBoneWeight.w < 1) //Necessary. Bones may scale weirdly without
		    newPosition += bones[index.w] * vec4(pos, 1.0) * vBoneWeight.w;
    }
    return newPosition;
}

vec3 skinNormal(vec3 nr, ivec4 index)
{
    vec3 newNormal = vec3(0);

    if (SkinCount == 1) //Rigid
    {
        newNormal = mat3(bones[index.x]) * nr;
    }
    else
    {
	    newNormal = mat3(bones[index.x]) * nr * vBoneWeight.x;
	    newNormal += mat3(bones[index.y]) * nr * vBoneWeight.y;
	    newNormal += mat3(bones[index.z]) * nr * vBoneWeight.z;
	    newNormal += mat3(bones[index.w]) * nr * vBoneWeight.w;
    }
    return newNormal;
}

vec3 BoneWeightColor(float weights)
{
	float rampInputLuminance = weights;
	rampInputLuminance = clamp((rampInputLuminance), 0.001, 0.999);
    if (weightRampType == 1) // Greyscale
        return vec3(weights);
    else if (weightRampType == 2) // Color 1
	   return texture(weightRamp1, vec2(1 - rampInputLuminance, 0.50)).rgb;
    else // Color 2
        return texture(weightRamp2, vec2(1 - rampInputLuminance, 0.50)).rgb;
}

float BoneWeightDisplay(ivec4 index)
{
    float weight = 0;
    if (selectedBoneIndex == index.x)
        weight += vBoneWeight.x;
    if (selectedBoneIndex == index.y)
        weight += vBoneWeight.y;
    if (selectedBoneIndex == index.z)
        weight += vBoneWeight.z;
    if (selectedBoneIndex == index.w)
        weight += vBoneWeight.w;

    if (selectedBoneIndex == index.x && SkinCount == 1)
        weight = 1;
   if (selectedBoneIndex == BoneIndex && SkinCount == 0)
        weight = 1;

    return weight;
}

void main(){
    vec4 worldPosition = vec4(vPositon.xyz, 1);
    vec3 worldNormal = vNormal.xyz;

    //Vertex Rigging
    if (UseSkinning == 1) //Animated object using the skeleton
    {
        ivec4 index = vBoneIndex;
        //Apply skinning to vertex position and normal
	    if (SkinCount > 0)
		    worldPosition = skin(worldPosition.xyz, index);
	    if (SkinCount > 0)
		    worldNormal = skinNormal(worldNormal.xyz, index);
    }

    gl_Position = mtxMdl*mtxCam*worldPosition;

    normals = normalize(mat3(mtxMdl) * worldNormal.xyz);

    float totalWeight = BoneWeightDisplay(vBoneIndex);
    boneWeightsColored = BoneWeightColor(totalWeight).rgb;
    texCoord0 = vTexCoord0;
    tangent = vTangent;
    bitangent = vBitangent;
    vertexColor = vColor;
}