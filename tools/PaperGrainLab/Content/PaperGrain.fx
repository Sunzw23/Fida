// Paper grain composite, used as a full-screen pass over the finished UI frame.
// Grain lives in screen space, so it never tiles, never needs per-size CPU baking and
// costs one draw. Two implementations are provided: analytic noise and a baked tile.
#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0
#define PS_SHADERMODEL ps_4_0
#endif

sampler2D Scene : register(s0);
sampler2D PaperTile : register(s1);

float2 RenderSize = float2(1280, 720);
float2 TileRepeat = float2(2.0, 1.1);
float GrainAmount = 1.0;
float ToothAmount = 1.0;
float FiberAmount = 1.0;
float MottleAmount = 1.0;
float LumaKeep = 0.25;
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

// Octaves are rotated between steps so the lattice never lines up with the screen.
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
    // Formation cloud: very slow, so it reads as uneven pulp rather than dirt.
    float mottle = Fbm3(px * (1.0 / 220.0));

    // A light warp keeps short fibres from running dead straight; short cells and a
    // weak stretch give scattered strands instead of a brushed direction.
    float2 warp = float2(Fbm2(px * (1.0 / 520.0) + 11.0), Fbm2(px * (1.0 / 520.0) + 41.0)) * 7.0;

    float2 a = Rot2(px, 0.35) + warp; a.y *= 0.30;
    float2 b = Rot2(px, 1.15) + warp; b.y *= 0.42;
    float2 c = Rot2(px, 2.35) + warp; c.y *= 0.36;
    // Fibre density varies across the sheet; without this the strands look uniform.
    float density = 0.55 + Fbm2(px * (1.0 / 170.0) + 5.0) * 0.9;
    float fibres = (Fbm2(a * (1.0 / 3.2)) * 0.42
                  + Fbm2(b * (1.0 / 4.1)) * 0.33
                  + Fbm2(c * (1.0 / 2.7)) * 0.25) * density;

    // Tooth: gradient noise at ~1.6 px. A per-pixel hash here reads as sand; sampled
    // gradient noise keeps the same micro-detail without the pixel static.
    float tooth = Gradient(px * 0.62) * 1.6;
    return float3(mottle, fibres, tooth);
}

float3 Compose(float3 scene, float3 n, float paper)
{
    float shade = n.x * MottleAmount * 0.120
                + n.y * FiberAmount * 0.150
                + n.z * ToothAmount * 0.040;
    shade *= GrainAmount * paper;
    // Fibres read warmer than grey static; the tint only shows where paper is lit.
    float3 warm = lerp(float3(1.0, 1.0, 1.0), float3(1.0, 0.975, 0.94), Warmth);
    float3 grainTint = lerp(float3(1.0, 1.0, 1.0), warm, saturate(abs(shade) * 60.0));
    return scene * (1.0 + shade) * grainTint;
}

float3 Shade(float3 scene, float2 uv)
{
    float luma = dot(scene, float3(0.2126, 0.7152, 0.0722));
    // Paper tooth is most visible on ivory sheets; keep ink and navy calm.
    float paper = lerp(LumaKeep, 1.0, saturate(luma * 1.8));
    float3 n = Field(uv * RenderSize / max(Zoom, 1.0));
    return Compose(scene, n, paper);
}

float4 ProceduralPS(VSOut input) : COLOR0
{
    return float4(Shade(tex2D(Scene, input.UV).rgb, input.UV), 1.0);
}

float4 TiledPS(VSOut input) : COLOR0
{
    float3 scene = tex2D(Scene, input.UV).rgb;
    float luma = dot(scene, float3(0.2126, 0.7152, 0.0722));
    float paper = lerp(LumaKeep, 1.0, saturate(luma * 1.8));
    float2 uv = input.UV * TileRepeat;
    // Two decorrelated taps hide the cellular structure of a cheap value-noise bake.
    float3 tile = (tex2D(PaperTile, uv).rgb
                 + tex2D(PaperTile, Rot2(uv, 1.7) * 0.531).rgb) * 0.5;
    float3 n = (float3(tile.g, tile.r, tile.b) * 2.0 - 1.0) * 0.60;
    // Value-noise mottle in a bake reads as blotches, so the tile keeps it low.
    n.x *= 0.45;
    return float4(Compose(scene, n, paper), 1.0);
}

float4 PassthroughPS(VSOut input) : COLOR0
{
    return float4(tex2D(Scene, input.UV).rgb, 1.0);
}

// Previous revision, kept only so the two grain fields can be compared side by side.
float ValueNoiseOld(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    float a = Hash(i);
    float b = Hash(i + float2(1.0, 0.0));
    float c = Hash(i + float2(0.0, 1.0));
    float d = Hash(i + float2(1.0, 1.0));
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

float FbmOld(float2 p)
{
    return ValueNoiseOld(p) * 0.65 + ValueNoiseOld(p * 2.07 + 11.3) * 0.35;
}

float4 LegacyPS(VSOut input) : COLOR0
{
    float3 scene = tex2D(Scene, input.UV).rgb;
    float luma = dot(scene, float3(0.2126, 0.7152, 0.0722));
    float keep = lerp(LumaKeep, 1.0, saturate(luma * 1.7));
    float2 px = input.UV * RenderSize / max(Zoom, 1.0);
    float tooth = Hash(floor(px)) - 0.5;
    float fiber = (FbmOld(px * float2(1.0, 0.16)) - 0.5) * 0.7
                + (FbmOld(px * float2(0.22, 1.0) + 37.0) - 0.5) * 0.3;
    float mottle = FbmOld(px / 96.0) - 0.5;
    float shade = tooth * ToothAmount * 0.035
                + fiber * FiberAmount * 0.05
                + mottle * MottleAmount * 0.045;
    return float4(scene * (1.0 + shade * GrainAmount * keep), 1.0);
}

technique Procedural
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL SceneVS();
        PixelShader = compile PS_SHADERMODEL ProceduralPS();
    }
}

technique Tiled
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL SceneVS();
        PixelShader = compile PS_SHADERMODEL TiledPS();
    }
}

technique Passthrough
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL SceneVS();
        PixelShader = compile PS_SHADERMODEL PassthroughPS();
    }
}

technique Legacy
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL SceneVS();
        PixelShader = compile PS_SHADERMODEL LegacyPS();
    }
}
