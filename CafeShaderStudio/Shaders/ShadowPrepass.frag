#version 330

uniform mat4 lightMatrix;

uniform sampler2D shadowMap;
uniform sampler2D depthTexture;

in vec2 TexCoords;
in vec3 ScreenCoords;

out vec4 fragOutput;

float CalculateShadow(float depth)
{
   vec3 projCoords = ScreenCoords;
   float closestDepth = texture(shadowMap, projCoords.xy).r;
   float shadow = depth > closestDepth ? 1.0 : 0.0;
   return shadow;
}

void main()
{             
   float depth = texture(depthTexture, TexCoords).r;
   float shadow = CalculateShadow(depth);

   shadow = 1.0;
   float ambientOcc = 1.0;
   float staticShadow = 1.0;

   fragOutput.r = shadow;
   fragOutput.g = staticShadow;
   fragOutput.b = ambientOcc;
   fragOutput.a = 1.0;
}  