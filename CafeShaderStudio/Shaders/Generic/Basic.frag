#version 330

in vec2 f_texcoord0;

uniform sampler2D textureMap;

uniform int hasTextures;
uniform int picking;
uniform vec4 color;

out vec4 fragColor;

void main()
{
    if (picking == 1)
		fragColor = color;
	else if (hasTextures == 1)
		fragColor = texture(textureMap, f_texcoord0).rgba;
	else
		fragColor = color;
}