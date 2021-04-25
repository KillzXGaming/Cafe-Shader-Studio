#version 330

layout (location = 0) in vec3 vPositon;
layout (location = 1) in vec3 vNormal;
layout (location = 2) in vec2 vTexCoord0;
layout (location = 3) in vec2 vTexCoord1;
layout (location = 4) in vec2 vTexCoord2;
layout (location = 5) in vec4 vColor;
layout (location = 6) in ivec4 vBoneIndex;
layout (location = 7) in vec4 vBoneWeight;
layout (location = 8) in vec3 vTangent;
layout (location = 9) in vec3 vBitangent;

uniform mat4 mtxMdl;
uniform mat4 mtxCam;

// Skinning uniforms
uniform mat4 bones[170];
uniform int SkinCount;
uniform int UseSkinning;
uniform int BoneIndex;
uniform mat4 RigidBindTransform;

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
vec3 skinNRM(vec3 nr, ivec4 index)
{
    vec3 newNormal = vec3(0);
    if (SkinCount == 1) //Rigid
    {
	    newNormal =  mat3(bones[index.x]) * nr;
    }
    else //Smooth
    {
	    newNormal =  mat3(bones[index.x]) * nr * vBoneWeight.x;
	    newNormal += mat3(bones[index.y]) * nr * vBoneWeight.y;
	    newNormal += mat3(bones[index.z]) * nr * vBoneWeight.z;
	    newNormal += mat3(bones[index.w]) * nr * vBoneWeight.w;
    }
    return newNormal;
}

out vec2 f_texcoord0;

out vec4 vertexColor;
out vec3 normal;
out vec3 fragPosition;
out vec3 viewNormal;

void main(){
    vec4 worldPosition = vec4(vPositon.xyz, 1);
    normal = normalize(mat3(mtxMdl) * vNormal.xyz);

    //Vertex Rigging
    if (UseSkinning == 1) //Animated object using the skeleton
    {
        ivec4 index = vBoneIndex;

        //Apply skinning to vertex position and normal
	    if (SkinCount > 0)
		    worldPosition = skin(worldPosition.xyz, index);
	    if(SkinCount > 0)
		    normal = normalize(mat3(mtxMdl) * (skinNRM(vNormal.xyz, index)).xyz);
        //Single bind models that have no skinning to the bone they are mapped to
        if (SkinCount == 0)
        {
            worldPosition = RigidBindTransform * worldPosition;
            normal = mat3(RigidBindTransform) * normal;
        }
    }

    f_texcoord0 = vTexCoord0;
    vertexColor = vColor;
    viewNormal = normal;
    fragPosition = (mtxMdl * worldPosition).xyz;

    gl_Position = mtxCam*vec4(fragPosition, 1);
}