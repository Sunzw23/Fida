// Procedural paper tooth for the finished UI frame.
//
// The whole page is already rendered into one render target, so this pass replaces the
// final blit: it composites the page (and the outgoing page during a crossfade) and adds
// paper grain in screen space. No per-size CPU baking and no grain textures are needed.
#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0
#define PS_SHADERMODEL ps_4_0
#endif

sampler2D Scene : register(s0);
sampler2D Previous : register(s1);

float2 RenderSize = float2(1280, 720);
float Fade = 1.0;
float GrainAmount = 1.0;
float LumaKeep = 0.22;
float Warmth = 0.35;
float Zoom = 1.0;

struct VSIn { float4 Position : POSITION0; float2 UV : TEXCOORD0; };
struct VSOut { float4 Position : SV_POSITION; float2 UV : TEXCOORD0; };

VSOut SceneVS(VSIn input)
{
    VSOut output;
    output.Position = input.Position;
    output.UV = input.UV;
    return output;
}

float Hash(float2 p)
{
    float3 p3 = frac(float3(p.x, p.y, p.x) * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

float2 Hash22(float2 p)
{
    float3 p3 = frac(float3(p.x, p.y, p.x) * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.xx + p3.yz) * p3.zy);
}

float2 Rot2(float2 p, float a)
{
    float s = sin(a), c = cos(a);
    return float2(c * p.x - s * p.y, s * p.x + c * p.y);
}

// Quintic gradient noise: smooth, no axis-aligned blobs.
float Gradient(float2 p)
{
    float2 i = floor(p);
    float2 f = p - i;
    float2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
    float2 ga = Hash22(i) * 2.0 - 1.0;
    float2 gb = Hash22(i + float2(1.0, 0.0)) * 2.0 - 1.0;
    float2 gc = Hash22(i + float2(0.0, 1.0)) * 2.0 - 1.0;
    float2 gd = Hash22(i + float2(1.0, 1.0)) * 2.0 - 1.0;
    float va = dot(ga, f);
    float vb = dot(gb, f - float2(1.0, 0.0));
    float vc = dot(gc, f - float2(0.0, 1.0));
    float vd = dot(gd, f - float2(1.0, 1.0));
    return lerp(lerp(va, vb, u.x), lerp(vc, vd, u.x), u.y);
}

// Octaves rotate between steps so the lattice never lines up with the screen.
float Fbm2(float2 p)
{
    float v = Gradient(p);
    v += Gradient(Rot2(p, 2.4) * 2.03) * 0.5;
    return v * 0.6667;
}

float Fbm3(float2 p)
{
    float v = Gradient(p);
    v += Gradient(Rot2(p, 2.4) * 2.03) * 0.5;
    v += Gradient(Rot2(p, 4.1) * 4.09) * 0.25;
    return v * 0.5714;
}

// r = formation cloud, g = fibres, b = tooth
float3 Field(float2 px)
{
    float mottle = Fbm3(px * (1.0 / 220.0));

    float2 warp = float2(Fbm2(px * (1.0 / 520.0) + 11.0), Fbm2(px * (1.0 / 520.0) + 41.0)) * 7.0;

    float2 a = Rot2(px, 0.35) + warp; a.y *= 0.30;
    float2 b = Rot2(px, 1.15) + warp; b.y *= 0.42;
    float2 c = Rot2(px, 2.35) + warp; c.y *= 0.36;
    float density = 0.55 + Fbm2(px * (1.0 / 170.0) + 5.0) * 0.9;
    float fibres = (Fbm2(a * (1.0 / 3.2)) * 0.42
                  + Fbm2(b * (1.0 / 4.1)) * 0.33
                  + Fbm2(c * (1.0 / 2.7)) * 0.25) * density;

    // Gradient noise at ~1.6 px: a per-pixel hash here reads as sand.
    float tooth = Gradient(px * 0.62) * 1.6;
    return float3(mottle, fibres, tooth);
}

float3 Compose(float3 scene, float3 n)
{
    float luma = dot(scene, float3(0.2126, 0.7152, 0.0722));
    // Paper tooth is most visible on ivory sheets; ink and navy stay calm.
    float paper = lerp(LumaKeep, 1.0, saturate(luma * 1.8));

    float shade = n.x * 0.120 * 0.70
                + n.y * 0.150
                + n.z * 0.040 * 0.70;
    shade *= GrainAmount * paper;

    float3 warm = lerp(float3(1.0, 1.0, 1.0), float3(1.0, 0.975, 0.94), Warmth);
    float3 grainTint = lerp(float3(1.0, 1.0, 1.0), warm, saturate(abs(shade) * 60.0));
    return scene * (1.0 + shade) * grainTint;
}

float4 PaperPS(VSOut input) : COLOR0
{
    float3 scene = tex2D(Scene, input.UV).rgb;
    if (Fade < 0.999)
        scene = lerp(tex2D(Previous, input.UV).rgb, scene, saturate(Fade));
    float2 px = input.UV * RenderSize / max(Zoom, 1.0);
    return float4(Compose(scene, Field(px)), 1.0);
}

technique Paper
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL SceneVS();
        PixelShader = compile PS_SHADERMODEL PaperPS();
    }
}
