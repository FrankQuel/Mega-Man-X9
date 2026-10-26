Shader "MegaManX9/RainSeamlessRain"
{
    Properties
    {
        _RainColor ("Rain tint", Color) = (0.50,0.58,0.76,0.27)
        _Slant ("Diagonal fall", Range(0,0.6)) = 0.26
        [HideInInspector] _RainClock ("Controlled rain clock", Float) = 0
        [HideInInspector] _SurfaceHeights ("Lowest foundation heights", 2D) = "black" {}
        [HideInInspector] _SurfaceRange ("Surface map left / inverse width / clearance / enabled", Vector) = (-8,0.003571429,0.025,0)
        [HideInInspector] _SurfaceMapTexel ("Surface map texel", Float) = 0.0001220703
        [HideInInspector] _SolidCount ("Solid volume count", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Pass
        {
            Name "ContinuousRain"
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_FLOAT(_SurfaceHeights);
            SAMPLER(sampler_SurfaceHeights);
            CBUFFER_START(UnityPerMaterial)
                float4 _RainColor;
                float4 _SurfaceRange;
                float _RainClock;
                float _SurfaceMapTexel;
                float _Slant;
                float _SolidCount;
                float4 _SolidRects[128];
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 worldXY : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(world);
                output.worldXY = world.xy;
                return output;
            }
            float2 Hash22(float2 value)
            {
                float3 p = frac(float3(value.x, value.y, value.x) * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yzx + 33.33);
                return frac((p.xx + p.yz) * p.zy);
            }
            float Streaks(float2 world, float2 spacing, float speed, float halfLength, float width, float seed)
            {
                // Advect in world coordinates: camera motion never restarts or tiles the pattern.
                float2 coordinate = float2(world.x - world.y * _Slant, world.y + _RainClock * speed);
                float2 cell = floor(coordinate / spacing);
                float2 local = coordinate - cell * spacing;
                float antialias = max(fwidth(coordinate.x) * 0.65, 0.001);
                float coverage = 0;
                // Neighbor capsules preserve continuity even when a streak crosses a hash-cell edge.
                [unroll] for (int row = -1; row <= 1; row++)
                {
                    [unroll] for (int column = -1; column <= 1; column++)
                    {
                        float2 offset = float2(column, row);
                        float2 random = Hash22(cell + offset + seed);
                        float present = step(0.34, frac(random.x * 13.17 + random.y * 7.31));
                        float2 center = (offset + 0.16 + random * 0.68) * spacing;
                        float2 delta = local - center;
                        float distance = length(float2(delta.x, max(abs(delta.y) - halfLength, 0)));
                        coverage += (1 - smoothstep(width, width + antialias, distance)) * present;
                    }
                }
                return saturate(coverage);
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float u = (input.worldXY.x - _SurfaceRange.x) * _SurfaceRange.y;
                if (_SurfaceRange.w > 0.5 && u >= 0 && u <= 1)
                {
                    float top = SAMPLE_TEXTURE2D_LOD(_SurfaceHeights, sampler_SurfaceHeights, float2(u, 0.5), 0).r;
                    // Conservative one-texel shoulders stop leaks along thin roof edges.
                    top = max(top, SAMPLE_TEXTURE2D_LOD(_SurfaceHeights, sampler_SurfaceHeights, float2(u - _SurfaceMapTexel, 0.5), 0).r);
                    top = max(top, SAMPLE_TEXTURE2D_LOD(_SurfaceHeights, sampler_SurfaceHeights, float2(u + _SurfaceMapTexel, 0.5), 0).r);
                    clip(input.worldXY.y - top - _SurfaceRange.z);
                }
                float farRain = Streaks(input.worldXY + float2(17.3, 9.2), float2(0.73, 1.75), 8.2, 0.14, 0.006, 37.7);
                float nearRain = Streaks(input.worldXY, float2(0.47, 1.35), 11.4, 0.23, 0.011, 3.1);
                float alpha = saturate((nearRain + farRain * 0.45) * _RainColor.a);
                clip(alpha - 0.003);
                // Exact axis-aligned occupied volumes from this level's BoxCollider2D terrain.
                // The mask does not cast a column shadow underneath elevated floors or ceilings.
                [loop] for (int index = 0; index < 128; index++)
                {
                    if (index >= _SolidCount) break;
                    float4 solid = _SolidRects[index];
                    if (input.worldXY.x >= solid.x && input.worldXY.x <= solid.z &&
                        input.worldXY.y >= solid.y && input.worldXY.y <= solid.w) discard;
                }
                return half4(_RainColor.rgb, alpha);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
