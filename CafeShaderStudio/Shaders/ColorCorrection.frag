#version 330

in vec2 TexCoords;

uniform float uBrightness;
uniform float uSaturation;
uniform float uGamma;
uniform float uHue;

uniform vec4 uCurve0[256];
uniform vec4 uCurve1[256];

uniform vec4 uToycamLevel1;
uniform vec4 uToycamLevel2;

#define fragData(index, color) { fragOutput ## index = vec4(color, 1.0); }

vec3 SetCurve(vec3 color)
{
    color *= 255; //0x40E00000U
    vec3 findex = floor(color);
    ivec3 iindex = ivec3(findex);

    vec3 color0 = vec3(uCurve0[iindex.x].x, uCurve0[iindex.y].y, uCurve0[iindex.z].z);
    vec3 color1 = vec3(uCurve1[iindex.x].x, uCurve1[iindex.y].y, uCurve1[iindex.z].z);

    return mix(color0, color1, (color - findex));
}

vec3 SetBrightness(vec3 color, float amount)
{
    return color * vec3(amount);
}

vec3 SetGamma(vec3 color, float gamma)
{
    return pow(color.rgb, vec3(1.0/gamma));
}

vec3 SetSaturation(vec3 color, float adjustment)
{
    const vec3 W = vec3(0.2125, 0.7154, 0.0721);
    vec3 intensity = vec3(dot(color, W));
    return mix(intensity, color, adjustment);
}

//https://gist.github.com/mairod/a75e7b44f68110e1576d77419d608786
vec3 SetHueShift( vec3 color, float hueAdjust ){

    const vec3  kRGBToYPrime = vec3 (0.299, 0.587, 0.114);
    const vec3  kRGBToI      = vec3 (0.596, -0.275, -0.321);
    const vec3  kRGBToQ      = vec3 (0.212, -0.523, 0.311);

    const vec3  kYIQToR     = vec3 (1.0, 0.956, 0.621);
    const vec3  kYIQToG     = vec3 (1.0, -0.272, -0.647);
    const vec3  kYIQToB     = vec3 (1.0, -1.107, 1.704);

    float   YPrime  = dot (color, kRGBToYPrime);
    float   I       = dot (color, kRGBToI);
    float   Q       = dot (color, kRGBToQ);
    float   hue     = atan (Q, I);
    float   chroma  = sqrt (I * I + Q * Q);

    hue += hueAdjust;

    Q = chroma * sin (hue);
    I = chroma * cos (hue);

    vec3    yIQ   = vec3 (YPrime, I, Q);

    return vec3( dot (yIQ, kYIQToR), dot (yIQ, kYIQToG), dot (yIQ, kYIQToB) );
}

vec3 SetToycam( vec3 color ){
    color = pow(color, uToycamLevel1.rgb);
    color = pow(color, uToycamLevel2.rgb);
    return color;
}

const int LUT_SIZE = 8;

void main()
{  
    float depth = 1.0 / (LUT_SIZE - 1);

    vec3 color_in = vec3(TexCoords.x, TexCoords.y, 0.0);

    for (int i = 0; i < 8; i++)
    {
        vec3 color = color_in;
        color.rgb = SetBrightness(color.rgb, uBrightness);
        color.rgb = SetSaturation(color.rgb, uSaturation);
        color.rgb = SetHueShift(color.rgb, uHue);
        //color.rgb = SetToycam(color.rgb);
        color.rgb = clamp(SetGamma(color.rgb, uGamma), 0, 1);
        color.rgb = clamp(SetCurve(color.rgb), 0, 1);

        gl_FragData[i] = vec4(color, 1.0);
        color_in.b += depth;
    }
}  