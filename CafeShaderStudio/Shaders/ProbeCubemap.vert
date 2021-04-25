// VSC08294CB0857A5F0
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

bvec2 HalfFloatNanComparison(bvec2 comparison, vec2 pair1, vec2 pair2) {
    bvec2 is_nan1 = isnan(pair1);
    bvec2 is_nan2 = isnan(pair2);
    return bvec2(comparison.x || is_nan1.x || is_nan2.x, comparison.y || is_nan1.y || is_nan2.y);
}

const float fswzadd_modifiers_a[] = float[4](-1.0f,  1.0f, -1.0f,  0.0f );
const float fswzadd_modifiers_b[] = float[4](-1.0f, -1.0f,  1.0f, -1.0f );

out gl_PerVertex {
    vec4 gl_Position;
};


layout (location = 0)  in vec4 in_attr0;
layout (location = 1)  in vec4 in_attr1;

layout (location = 0, component = 0) out vec4 out_attr0;

float gpr0 = 0.0f;
float gpr1 = 0.0f;
float gpr2 = 0.0f;
float gpr3 = 0.0f;
float gpr4 = 0.0f;

bool zero_flag = false;
bool sign_flag = false;
bool carry_flag = false;
bool overflow_flag = false;

void main() {
    gl_Position = vec4(0.0f, 0.0f, 0.0f, 1.0f);
    uint jmp_to = 10U;
    while (true) {
        switch (jmp_to) {
        case 0xAU: {
            // 00008 MOV32_IMM (0x0103f8000007f004)
            gpr4 = utof(0x3F800000U);
            // 00010 LD_A (0xefd87f800807ff00)
            gpr0 = in_attr0.x;
            // 00018 LD_A (0xefd87f800847ff01)
            gpr1 = in_attr0.y;
            // 00028 LD_A (0xefd87f800907ff02)
            gpr2 = in_attr1.x;
            // 00030 LD_A (0xefd87f800947ff03)
            gpr3 = in_attr1.y;
            // 00038 ST_A (0xeff07f8007c7ff04)
            gl_Position.w = gpr4;
            // 00048 ST_A (0xeff07f800787ffff)
            gl_Position.z = utof(0U);
            // 00050 FMUL_IMM (0x3868104000070000)
            precise float tmp1 = (gpr0 * utof(0x40000000U));
            gpr0 = tmp1;
            // 00058 ST_A (0xeff07f800807ff02)
            out_attr0[0] = gpr2;
            // 00068 FMUL_IMM (0x3868104000070101)
            precise float tmp2 = (gpr1 * utof(0x40000000U));
            gpr1 = tmp2;
            // 00070 ST_A (0xeff07f800847ff03)
            out_attr0[1] = gpr3;
            // 00078 ST_A (0xeff07f800707ff00)
            gl_Position.x = gpr0;
            // 00088 ST_A (0xeff07f800747ff01)
            gl_Position.y = gpr1;
            return;
        }
        default: return;
        }
    }
}
