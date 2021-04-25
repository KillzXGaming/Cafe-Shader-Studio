#version 330
uniform vec4 color;
layout (location = 0) out vec4 fragOutput;
layout (location = 1) out vec4 brightColor;

void main(){
    fragOutput = color;
    brightColor = vec4(0,0,0,1);
}