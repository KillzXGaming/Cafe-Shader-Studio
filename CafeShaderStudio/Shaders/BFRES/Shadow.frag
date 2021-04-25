#version 330

out vec4 fragOutput;

void main()
{             
   fragOutput = vec4 (gl_FragCoord.z);
}  