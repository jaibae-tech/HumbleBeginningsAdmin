Shader "HumbleBeginnings/WorldViewer/ReliefUnlit"
{
    Properties
    {
        _BaseColor ("Base Color (Multiply)", Color) = (1,1,1,1)

        [Header(Color Ramps)]
        _LandRamp  ("Land Ramp (256x1)", 2D) = "white" {}
        _OceanRamp ("Ocean Ramp (256x1)", 2D) = "white" {}

        [Header(Shoreline)]
        _ShoreLow  ("Shore Low Width (below sea, height01)", Range(0,0.1)) = 0.02
        _ShoreHigh ("Shore High Width (above sea, height01)", Range(0,0.1)) = 0.03

        [Header(Relief Shading)]
        _SlopeStrength ("Slope Strength", Range(0,3)) = 1.2
        _CurvatureStrength ("Curvature Strength", Range(0,3)) = 0.8
        _CurvatureRadius ("Curvature Radius (texels)", Range(1,6)) = 2

        _AOEnabled ("AO Enabled (0/1)", Float) = 0
        _AOStrength ("AO Strength", Range(0,2)) = 0.7
        _AORadius ("AO Radius (texels)", Range(1,8)) = 3

        _OceanDarken ("Ocean Darken", Range(0,1)) = 0.15
        _MinShade ("Min Shade", Range(0,1)) = 0.25

        [Header(Rock Exposure)]
        _RockSlopeStart ("Rock Slope Start", Range(0,1)) = 0.35
        _RockSlopeEnd ("Rock Slope End", Range(0,1)) = 0.70
        _RockStrength ("Rock Strength", Range(0,1)) = 0.65

        [Header(Snow)]
        _SnowHeight01 ("Snow Height01 (baseline)", Range(0,1)) = 0.80
        _SnowBlend ("Snow Blend Width", Range(0,0.1)) = 0.03
        _SnowStrength ("Snow Strength", Range(0,1)) = 0.85
        _SnowLatitudeStrength ("Snow Latitude Strength", Range(0,0.3)) = 0.10
        _SnowLatitudePower ("Snow Latitude Power", Range(0.25,4)) = 1.5
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Global heightmap uploaded by WV_GlobalHeightmap
            TEXTURE2D(_HB_HeightTex);
            SAMPLER(sampler_HB_HeightTex);
            float4 _HB_HeightParams; // (W,H,1/W,1/H)
            float4 _HB_WorldParams;  // (SeaLevel01, HeightScale, TileSize, _)

            TEXTURE2D(_LandRamp);   SAMPLER(sampler_LandRamp);
            TEXTURE2D(_OceanRamp);  SAMPLER(sampler_OceanRamp);

            float4 _BaseColor;

            float _ShoreLow;
            float _ShoreHigh;

            float _SlopeStrength;
            float _CurvatureStrength;
            float _CurvatureRadius;

            float _AOEnabled;
            float _AOStrength;
            float _AORadius;

            float _OceanDarken;
            float _MinShade;

            float _RockSlopeStart;
            float _RockSlopeEnd;
            float _RockStrength;

            float _SnowHeight01;
            float _SnowBlend;
            float _SnowStrength;
            float _SnowLatitudeStrength;
            float _SnowLatitudePower;

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 worldPos    : TEXCOORD0;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS);
                o.worldPos = TransformObjectToWorld(v.positionOS);
                return o;
            }

            float Height01AtUV(float2 uv)
            {
                uv = saturate(uv);
                return SAMPLE_TEXTURE2D(_HB_HeightTex, sampler_HB_HeightTex, uv).r;
            }

            float2 WorldPosToUV(float3 worldPos)
            {
                float W = max(2.0, _HB_HeightParams.x);
                float H = max(2.0, _HB_HeightParams.y);
                float tileSize = max(0.0001, _HB_WorldParams.z);

                // World spans (W-1)*tileSize and (H-1)*tileSize in X/Z.
                float2 span = float2((W - 1.0) * tileSize, (H - 1.0) * tileSize);
                float2 uv = float2(worldPos.x / span.x, worldPos.z / span.y);
                return saturate(uv);
            }

            float ComputeSlope(float2 uv)
            {
                float2 texel = _HB_HeightParams.zw; // (1/W, 1/H)

                float hL = Height01AtUV(uv - float2(texel.x, 0));
                float hR = Height01AtUV(uv + float2(texel.x, 0));
                float hD = Height01AtUV(uv - float2(0, texel.y));
                float hU = Height01AtUV(uv + float2(0, texel.y));

                float heightScale = max(0.0001, _HB_WorldParams.y);
                float tileSize = max(0.0001, _HB_WorldParams.z);

                float dhdx = ((hR - hL) * heightScale) / (2.0 * tileSize);
                float dhdz = ((hU - hD) * heightScale) / (2.0 * tileSize);

                float grad = sqrt(dhdx * dhdx + dhdz * dhdz);

                // Scaled to a useful 0..1 range for typical world params.
                return saturate(grad * 0.35);
            }

            float ComputeCurvature(float2 uv, float radiusTexels)
            {
                float2 texel = _HB_HeightParams.zw;
                float2 r = texel * max(1.0, radiusTexels);

                float hC = Height01AtUV(uv);
                float hL = Height01AtUV(uv - float2(r.x, 0));
                float hR = Height01AtUV(uv + float2(r.x, 0));
                float hD = Height01AtUV(uv - float2(0, r.y));
                float hU = Height01AtUV(uv + float2(0, r.y));

                float lap = (hL + hR + hD + hU - 4.0 * hC);
                return lap;
            }

            float ComputeAO(float2 uv, float radiusTexels)
            {
                float2 texel = _HB_HeightParams.zw;
                float2 r = texel * max(1.0, radiusTexels);

                float hC = Height01AtUV(uv);
                float occ = 0.0;

                // 8 taps around the center
                occ += max(0.0, Height01AtUV(uv + float2( r.x, 0)) - hC);
                occ += max(0.0, Height01AtUV(uv + float2(-r.x, 0)) - hC);
                occ += max(0.0, Height01AtUV(uv + float2(0,  r.y)) - hC);
                occ += max(0.0, Height01AtUV(uv + float2(0, -r.y)) - hC);

                occ += max(0.0, Height01AtUV(uv + float2( r.x,  r.y)) - hC);
                occ += max(0.0, Height01AtUV(uv + float2(-r.x,  r.y)) - hC);
                occ += max(0.0, Height01AtUV(uv + float2( r.x, -r.y)) - hC);
                occ += max(0.0, Height01AtUV(uv + float2(-r.x, -r.y)) - hC);

                occ *= (0.125 * _AOStrength);
                return saturate(1.0 - occ);
            }

            float3 SampleRamp(TEXTURE2D_PARAM(rampTex, rampSampler), float t01)
            {
                // 256x1 ramp: sample along X.
                float2 uv = float2(saturate(t01), 0.5);
                return SAMPLE_TEXTURE2D(rampTex, rampSampler, uv).rgb;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float2 uv = WorldPosToUV(i.worldPos);

                float hC = Height01AtUV(uv);
                float sea = _HB_WorldParams.x;

                // Shoreline blend factor (0=ocean, 1=land).
                float shore = smoothstep(sea - _ShoreLow, sea + _ShoreHigh, hC);

                // Ramp sampling: remap height into 0..1 within each domain.
                float oceanT = (sea > 1e-5) ? saturate(hC / sea) : 0.0;
                float landT  = (sea < 0.999) ? saturate((hC - sea) / (1.0 - sea)) : 0.0;

                float3 oceanRGB = SampleRamp(TEXTURE2D_ARGS(_OceanRamp, sampler_OceanRamp), oceanT);
                float3 landRGB  = SampleRamp(TEXTURE2D_ARGS(_LandRamp,  sampler_LandRamp),  landT);

                float3 baseRGB = lerp(oceanRGB, landRGB, shore);

                // "Satellite cheats" (rock exposure + snowline)
                float slope = ComputeSlope(uv);

                float rockMask = smoothstep(_RockSlopeStart, _RockSlopeEnd, slope) * _RockStrength;
                float3 rockRGB = float3(0.55, 0.55, 0.56); // neutral rock tint
                baseRGB = lerp(baseRGB, rockRGB, rockMask * shore);

                float lat01 = abs(uv.y - 0.5) * 2.0; // 0 at equator, 1 at poles
                float latAdj = _SnowLatitudeStrength * pow(saturate(lat01), _SnowLatitudePower);
                float snowLine = saturate(_SnowHeight01 - latAdj);
                float snowMask = smoothstep(snowLine - _SnowBlend, snowLine + _SnowBlend, hC) * _SnowStrength;
                float3 snowRGB = float3(0.94, 0.95, 0.96);
                baseRGB = lerp(baseRGB, snowRGB, snowMask * shore);

                // Relief shading
                float shade = 1.0 - slope * _SlopeStrength;

                float curv = ComputeCurvature(uv, _CurvatureRadius);
                shade *= (1.0 + (-curv) * _CurvatureStrength);

                if (_AOEnabled > 0.5)
                {
                    float ao = ComputeAO(uv, _AORadius);
                    shade *= ao;
                }

                if (hC < sea)
                    shade *= (1.0 - _OceanDarken);

                shade = max(shade, _MinShade);
                shade = saturate(shade);

                float3 rgb = baseRGB * shade * _BaseColor.rgb;
                return half4(rgb, 1.0);
            }
            ENDHLSL
        }
    }
}
