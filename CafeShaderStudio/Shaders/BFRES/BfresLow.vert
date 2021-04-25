#version 330

layout(location = 0) in vec3 vPositon;
layout(location = 1) in vec3 vNormal;
layout(location = 8) in vec2 vTexCoord0;

uniform mat4 mtxMdl;
uniform mat4 mtxCam;

out vec2 f_texcoord0;

out vec4 vertexColor;
out vec3 normal;
out vec3 fragPosition;
out vec3 viewNormal;

void main(){
    vec4 worldPosition = vec4(vPositon.xyz, 1);
    normal = normalize(mat3(mtxMdl) * vNormal.xyz);

    f_texcoord0 = vTexCoord0;
    viewNormal = normal;
    fragPosition = (mtxMdl * worldPosition).xyz;

    gl_Position = mtxCam*vec4(fragPosition, 1);
}