#version 330
in float faceIndex;

layout (location = 0) out vec4 fragOutput;

uniform int pickFace;
uniform int pickedIndex;
uniform vec4 color;

void main(){
     fragOutput = color;
}