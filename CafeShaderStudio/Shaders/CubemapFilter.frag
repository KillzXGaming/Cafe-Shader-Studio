// FS0057EAFE5DFF21A8
#version 440 core
#extension GL_ARB_separate_shader_objects : enable
#extension GL_ARB_shader_ballot : require
#extension GL_ARB_shader_viewport_layer_array : require
#extension GL_EXT_shader_image_load_formatted : require
#extension GL_EXT_texture_shadow_lod : require
#pragma optionNV(fastmath off)

#define ftoi floatBitsToInt
#define ftou floatBitsToUint
#define itof intBitsToFloat
#define utof uintBitsToFloat

out vec4 FragColor;

in vec3 WorldPos;

uniform float mipLevel;

uniform samplerCube cubemapTexture;

float gpr0 = 0.0f;
float gpr1 = 0.0f;
float gpr2 = 0.0f;
float gpr3 = 0.0f;
float gpr4 = 0.0f;
float gpr5 = 0.0f;
float gpr6 = 0.0f;
float gpr7 = 0.0f;
float gpr8 = 0.0f;
float gpr9 = 0.0f;
float gpr256 = 0.0f;
float gpr257 = 0.0f;
float gpr258 = 0.0f;

void main()
{		
        // 00008 MOV32_IMM (0x0103f8000007f005)
        gpr5 = utof(0x3F800000U);
        // 00010 IPA (0xe003ff87cff7ff04)
        gpr4 = gl_FragCoord.w;
        // 00018 MOV_C (0x4c98078c00870009)
        gpr9 = 4.0;
        // 00028 MUFU (0x5080000000470408)
        precise float tmp1 = (utof(0x3F800000U) / gpr4);
        gpr8 = tmp1;
        // 00030 IPA (0xe043ff880087ff00)
        gpr0 = ((WorldPos.x * gl_FragCoord.w) * gpr8);
        // 00038 IPA (0xe043ff884087ff01)
        gpr1 = ((WorldPos.y * gl_FragCoord.w) * gpr8);
        // 00048 IPA (0xe043ff888087ff02)
        gpr2 = ((WorldPos.z * gl_FragCoord.w) * gpr8);
        // 00050 FMNMX_R (0x5c62578000170003)
        gpr3 = utof((!(true) ? ftou(min(abs(gpr0), abs(gpr1))) : ftou(max(abs(gpr0), abs(gpr1)))));
        // 00058 FMNMX_R (0x5c60578000370207)
        gpr7 = utof((!(true) ? ftou(min(abs(gpr2), gpr3)) : ftou(max(abs(gpr2), gpr3))));
        // 00068 MUFU (0x5080000000470703)
        precise float tmp2 = (utof(0x3F800000U) / gpr7);
        gpr3 = tmp2;
        // 00070 FMUL_R (0x5c68100000370000)
        precise float tmp3 = (gpr0 * gpr3);
        gpr0 = tmp3;
        // 00078 FMUL_R (0x5c68100000370101)
        precise float tmp4 = (gpr1 * gpr3);
        gpr1 = tmp4;
        // 00088 FMUL_R (0x5c68100000370202)
        precise float tmp5 = (gpr2 * gpr3);
        gpr2 = tmp5;


    vec3 N = normalize(WorldPos);
    gpr256 = textureLod(cubemapTexture, N, mipLevel).x;
    gpr257 = textureLod(cubemapTexture, N, mipLevel).y;
    gpr258 = textureLod(cubemapTexture, N, mipLevel).z;

    gpr0 = gpr256;
    gpr1 = gpr257;
    gpr2 = gpr258;
    // 000a8 IPA (0xe043ff890087ff06)
    gpr6 = N.x;
    // 000b0 MUFU (0x5080000000470903)
    precise float tmp6 = (utof(0x3F800000U) / gpr9);
    gpr3 = tmp6;
    // 000b8 MUFU (0x5080000000470606)
    precise float tmp7 = (1.0 / 256.0);
    gpr6 = tmp7;
    // 000c8 FMNMX_R (0x5c60178000170004)
    gpr4 = utof((!(true) ? ftou(min(gpr0, gpr1)) : ftou(max(gpr0, gpr1))));
    // 000d0 FMNMX_R (0x5c60178000470204)
    gpr4 = utof((!(true) ? ftou(min(gpr2, gpr4)) : ftou(max(gpr2, gpr4))));
    // 000d8 FFMA_IMM (0x32a102c080070405)
    float tmp8 = -4.0;
    precise float tmp9 = fma(gpr4, tmp8, gpr5);
    gpr5 = tmp9;
    // 000e8 F2F_R (0x5ca8148000570a07)
    gpr7 = floor(gpr5);
    // 000f0 FADD_R (0x5c58300000770505)
    float tmp10 = -(gpr7);
    precise float tmp11 = (gpr5 + tmp10);
    gpr5 = tmp11;
    // 000f8 FFMA_IMM (0x32a0023e80070504)
    precise float tmp12 = fma(gpr5, 0.00390625, gpr4);
    gpr4 = tmp12;
    // 00108 FMNMX_IMM (0x386017bb80070404)
    gpr4 = utof((!(true) ? ftou(min(gpr4, 0.00390625)) : ftou(max(gpr4, 0.00390625))));
    // 00110 FMUL_R (0x5c6c100000670405)
    precise float tmp13 = (gpr4 * gpr6);
    gpr5 = clamp(tmp13, 0.0, 1.0);
    // 00118 MUFU (0x5080000000470407)
    precise float tmp14 = (1.0 / gpr4);
    gpr7 = tmp14;
    // 00128 MUFU (0x5080000000370505)
    precise float tmp15 = log2(gpr5);
    gpr5 = tmp15;
    // 00130 FMUL_R (0x5c68100000570303)
    precise float tmp16 = (gpr3 * gpr5);
    gpr3 = tmp16;
    // 00138 RRO_R (0x5c90008000370003)
    gpr3 = gpr3;
    // 00148 MUFU (0x5080000000270303)
    precise float tmp17 = exp2(gpr3);
    gpr3 = tmp17;
    // 00150 FMUL_R (0x5c68100000770000)
    precise float tmp18 = (gpr0 * gpr7);
    gpr0 = tmp18;
    // 00158 FMUL_R (0x5c68100000770101)
    precise float tmp19 = (gpr1 * gpr7);
    gpr1 = tmp19;
    // 00168 FMUL_R (0x5c68100000770202)
    precise float tmp20 = (gpr2 * gpr7);
    gpr2 = tmp20;
    FragColor.r = gpr0;
    FragColor.g = gpr1;
    FragColor.b = gpr2;
    FragColor.a = gpr3;
}