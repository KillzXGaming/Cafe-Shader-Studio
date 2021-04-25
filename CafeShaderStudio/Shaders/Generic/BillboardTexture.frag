#version 330

in vec2 f_texcoord0;

uniform sampler2D textureMap;

uniform int picking;
uniform vec4 color;

out vec4 fragColor;

void main()
{
    if (picking == 1)
		fragColor = color;
	else
		fragColor = texture(textureMap, f_texcoord0).rgba;
}