#version 330

uniform sampler2D textureInput;
uniform sampler2D backgroundTexture;

in vec2 TexCoords;

uniform int isSRGB;
uniform int hasTexture;
uniform int displayAlpha;

uniform float width;
uniform float height;
uniform int currentMipLevel;

out vec4 fragOutput;

void main()
{  
    vec4 color = vec4(0);
    float alpha = 1.0;

    if (hasTexture == 1)
    {
        color = texture(textureInput, TexCoords);
        alpha = color.a;

        if (isSRGB == 1)
            color.rgb = pow(color.rgb, vec3(1.0/2.2));
    }
    else
        color = texture(backgroundTexture, TexCoords);

    if (displayAlpha == 0)
       alpha = 1.0;

    fragOutput = vec4(color.rgb, alpha);
}  