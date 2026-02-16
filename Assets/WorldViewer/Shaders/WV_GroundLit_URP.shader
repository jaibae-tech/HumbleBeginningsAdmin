Shader "WorldViewer/GroundLit_URP"
{
    Properties
    {
        // Layer textures (tileable)
        _GrassAlbedo ("Grass Albedo", 2D) = "white" {}
        _GrassNormal ("Grass Normal (DX)", 2D) = "bump" {}
        _RockAlbedo  ("Rock Albedo", 2D) = "white" {}
        _RockNormal  ("Rock Normal (DX)", 2D) = "bump" {}
        _SandAlbedo  ("Sand/Dirt Albedo", 2D) = "white" {}
        _SandNormal  ("Sand/Dirt Normal (DX)", 2D) = "bump" {}

        // Optional detail normals (can be left unassigned)
        _DetailNormal ("Detail Normal (DX)", 2D) = "bump" {}
        _DetailNormalStrength ("Detail Normal Strength", Range(0,2)) = 0.6
        _DetailTiling ("Detail Tiling (world)", Float) = 0.35

        // Triplanar tiling (world-space)
        _BaseTiling ("Base Tiling (world)", Float) = 0.05

        // Slope / height blending
        _SlopeSharpness ("Slope Sharpness", Float) = 6
        _RockSlopeStart ("Rock Slope Start", Range(0,1)) = 0.25
        _RockSlopeEnd   ("Rock Slope End",   Range(0,1)) = 0.65

        _BeachHeight ("Beach Height Above Sea (e01)", Range(0,0.2)) = 0.03
        _BeachSoftness ("Beach Softness (e01)", Range(0.001,0.1)) = 0.02

        // Wet-sand band just above sea level
        _WetBandHeight ("Wet Band Height (e01)", Range(0,0.1)) = 0.02
        _WetDarken ("Wet Darken", Range(0,1)) = 0.25
        _WetSmoothness ("Wet Smoothness Add", Range(0,1)) = 0.35

        // Snow (procedural, no texture needed)
        _SnowStart ("Snow Start (e01 over sea)", Range(0,1)) = 0.55
        _SnowFade  ("Snow Fade (e01)", Range(0.01,0.4)) = 0.15
        _SnowSlopeCutoff ("Snow Slope Cutoff", Range(0,1)) = 0.35
        _SnowColor ("Snow Color", Color) = (0.92,0.95,1.0,1)
        _SnowNormalStrength ("Snow Normal Strength", Range(0,1)) = 0.25

        // Macro color variation (procedural noise)
        _MacroScale ("Macro Scale (world)", Float) = 0.0015
        _MacroStrength ("Macro Strength", Range(0,1)) = 0.18

        // Lighting response
        _Smoothness ("Smoothness", Range(0,1)) = 0.15
        _SpecularStrength ("Specular Strength", Range(0,1)) = 0.08
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0

            #pragma vertex vert
            #pragma fragment frag

            // Shadows / lighting variants
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

            // WorldViewer globals (set by WV_GlobalHeightmap)
            TEXTURE2D(_HB_HeightTex); SAMPLER(sampler_HB_HeightTex);
            float4 _HB_HeightParams;   // (W, H, 1/W, 1/H)
            float4 _HB_WorldParams;    // (SeaLevel01, HeightScale, TileSize, _)

            // Textures
            TEXTURE2D(_GrassAlbedo); SAMPLER(sampler_GrassAlbedo);
            TEXTURE2D(_GrassNormal); SAMPLER(sampler_GrassNormal);
            TEXTURE2D(_RockAlbedo);  SAMPLER(sampler_RockAlbedo);
            TEXTURE2D(_RockNormal);  SAMPLER(sampler_RockNormal);
            TEXTURE2D(_SandAlbedo);  SAMPLER(sampler_SandAlbedo);
            TEXTURE2D(_SandNormal);  SAMPLER(sampler_SandNormal);

            TEXTURE2D(_DetailNormal); SAMPLER(sampler_DetailNormal);

            // Params
            float _BaseTiling;
            float _DetailTiling;
            float _DetailNormalStrength;

            float _SlopeSharpness;
            float _RockSlopeStart;
            float _RockSlopeEnd;

            float _BeachHeight;
            float _BeachSoftness;

            float _WetBandHeight;
            float _WetDarken;
            float _WetSmoothness;

            float _SnowStart;
            float _SnowFade;
            float _SnowSlopeCutoff;
            float4 _SnowColor;
            float _SnowNormalStrength;

            float _MacroScale;
            float _MacroStrength;

            float _Smoothness;
            float _SpecularStrength;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 viewWS     : TEXCOORD2;
                float4 shadowCoord: TEXCOORD3;
            };

            // Simple hash-based noise (deterministic, cheap)
            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = Hash21(i);
                float b = Hash21(i + float2(1,0));
                float c = Hash21(i + float2(0,1));
                float d = Hash21(i + float2(1,1));
                float2 u = f * f * (3 - 2 * f);
                return lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y);
            }

            float2 WorldToHeightUV(float3 positionWS)
            {
                float tileSize = max(0.0001, _HB_WorldParams.z);
                float w = max(2.0, _HB_HeightParams.x);
                float h = max(2.0, _HB_HeightParams.y);

                float tx = positionWS.x / tileSize;
                float ty = positionWS.z / tileSize;

                // Map tiles (0..W-1, 0..H-1) to UV (0..1)
                return float2(tx / (w - 1.0), ty / (h - 1.0));
            }

            float Height01AtWS(float3 positionWS)
            {
                float2 uv = saturate(WorldToHeightUV(positionWS));
                return SAMPLE_TEXTURE2D(_HB_HeightTex, sampler_HB_HeightTex, uv).r;
            }

            float3 TriplanarAlbedo(TEXTURE2D_PARAM(tex, samp), float3 posWS, float3 nWS, float tiling)
            {
                float3 n = abs(nWS);
                float3 w = n / max(1e-5, (n.x + n.y + n.z));

                float2 uvX = posWS.zy * tiling;
                float2 uvY = posWS.xz * tiling;
                float2 uvZ = posWS.xy * tiling;

                float3 x = SAMPLE_TEXTURE2D(tex, samp, uvX).rgb;
                float3 y = SAMPLE_TEXTURE2D(tex, samp, uvY).rgb;
                float3 z = SAMPLE_TEXTURE2D(tex, samp, uvZ).rgb;

                return x * w.x + y * w.y + z * w.z;
            }

            float3 UnpackNormalDX(float4 packed)
            {
                // AmbientCG NormalDX is DirectX (Y down). Unity normal maps are typically Y up.
                // We invert Y so both NormalDX and Unity conventions behave consistently.
                float3 n;
                n.xy = packed.ag * 2.0 - 1.0; // works for DXT5nm-style too; for JPG it's still ok
                n.z  = sqrt(saturate(1.0 - dot(n.xy, n.xy)));
                n.y *= -1.0;
                return n;
            }

            float3 TriplanarNormal(TEXTURE2D_PARAM(tex, samp), float3 posWS, float3 nWS, float tiling)
            {
                float3 n = abs(nWS);
                float3 w = n / max(1e-5, (n.x + n.y + n.z));

                float2 uvX = posWS.zy * tiling;
                float2 uvY = posWS.xz * tiling;
                float2 uvZ = posWS.xy * tiling;

                float3 nx = UnpackNormalDX(SAMPLE_TEXTURE2D(tex, samp, uvX));
                float3 ny = UnpackNormalDX(SAMPLE_TEXTURE2D(tex, samp, uvY));
                float3 nz = UnpackNormalDX(SAMPLE_TEXTURE2D(tex, samp, uvZ));

                // Re-orient tangent-space normals into world axes for each projection
                float3 wx = float3( nx.z, nx.y, nx.x); // X projection (YZ plane)
                float3 wy = float3( ny.x, ny.z, ny.y); // Y projection (XZ plane)
                float3 wz = float3( nz.x, nz.y, nz.z); // Z projection (XY plane)

                float3 blended = normalize(wx * w.x + wy * w.y + wz * w.z);
                return blended;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vpi = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   vni = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = vpi.positionCS;
                OUT.positionWS = vpi.positionWS;
                OUT.normalWS   = normalize(vni.normalWS);
                OUT.viewWS     = GetWorldSpaceViewDir(vpi.positionWS);

                OUT.shadowCoord = GetShadowCoord(vpi);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 nWS = normalize(IN.normalWS);
                float3 vWS = SafeNormalize(IN.viewWS);

                // Height01 and sea level (01 space)
                float h01 = Height01AtWS(IN.positionWS);
                float sea01 = _HB_WorldParams.x;

                // Slope metric: 0 = flat up, 1 = vertical
                float slope = saturate(1.0 - abs(nWS.y));

                // --- Base material mixing ---
                // Beach: just above sea level
                float beach = smoothstep(sea01, sea01 + max(1e-4, _BeachHeight), h01);
                beach = smoothstep(0.0, 1.0, beach);
                float beachSoft = smoothstep(sea01, sea01 + max(1e-4, _BeachSoftness), h01);

                // Rock by slope
                float rockT = smoothstep(_RockSlopeStart, _RockSlopeEnd, slope);
                rockT = pow(saturate(rockT), max(1e-3, _SlopeSharpness * 0.25));

                // Albedo triplanar
                float3 grassCol = TriplanarAlbedo(TEXTURE2D_ARGS(_GrassAlbedo, sampler_GrassAlbedo), IN.positionWS, nWS, _BaseTiling);
                float3 rockCol  = TriplanarAlbedo(TEXTURE2D_ARGS(_RockAlbedo,  sampler_RockAlbedo),  IN.positionWS, nWS, _BaseTiling);
                float3 sandCol  = TriplanarAlbedo(TEXTURE2D_ARGS(_SandAlbedo,  sampler_SandAlbedo),  IN.positionWS, nWS, _BaseTiling);

                // Blend: grass↔rock by slope; then sand near shoreline/beach
                float3 col = lerp(grassCol, rockCol, rockT);
                col = lerp(sandCol, col, beachSoft); // below beach band -> sand, above -> (grass/rock)

                // Wet-sand band: only in narrow strip above sea
                float wet = smoothstep(sea01, sea01 + max(1e-4, _WetBandHeight), h01);
                wet = 1.0 - wet; // 1 near sea, 0 above band
                wet *= (1.0 - rockT); // less on cliffs
                col = lerp(col, col * (1.0 - _WetDarken), wet);

                // --- Macro variation (reduces tiling) ---
                float macro = ValueNoise(IN.positionWS.xz * _MacroScale);
                float macroMul = lerp(1.0 - _MacroStrength, 1.0 + _MacroStrength, macro);
                col *= macroMul;

                // --- Normals (triplanar + optional detail) ---
                float3 grassN = TriplanarNormal(TEXTURE2D_ARGS(_GrassNormal, sampler_GrassNormal), IN.positionWS, nWS, _BaseTiling);
                float3 rockN  = TriplanarNormal(TEXTURE2D_ARGS(_RockNormal,  sampler_RockNormal),  IN.positionWS, nWS, _BaseTiling);
                float3 sandN  = TriplanarNormal(TEXTURE2D_ARGS(_SandNormal,  sampler_SandNormal),  IN.positionWS, nWS, _BaseTiling);

                float3 baseN = normalize(lerp(grassN, rockN, rockT));
                baseN = normalize(lerp(sandN, baseN, beachSoft));

                // Detail normal (same texture for all layers, purely breaks up large flats)
                float3 detailN = TriplanarNormal(TEXTURE2D_ARGS(_DetailNormal, sampler_DetailNormal), IN.positionWS, nWS, _DetailTiling);
                // Blend detail in world space
                baseN = normalize(lerp(baseN, normalize(baseN + detailN * _DetailNormalStrength), saturate(_DetailNormalStrength)));

                // --- Snow (procedural color + slight normal softening) ---
                float snowLine = sea01 + _SnowStart;
                float snow = smoothstep(snowLine, snowLine + max(1e-4, _SnowFade), h01);
                float snowSlope = smoothstep(_SnowSlopeCutoff, 0.0, slope); // 1 on flats, 0 on steep
                snow *= snowSlope;

                // Snow breakup with macro noise (re-using)
                snow *= saturate(macro * 1.2);

                col = lerp(col, _SnowColor.rgb, snow);

                // Slightly reduce normal detail under snow (reads like powder)
                baseN = normalize(lerp(baseN, nWS, snow * _SnowNormalStrength));

                // --- URP lighting ---
                InputData inputData;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS = baseN;
                inputData.viewDirectionWS = vWS;
                inputData.shadowCoord = IN.shadowCoord;
                inputData.fogCoord = 0;
                inputData.vertexLighting = half3(0,0,0);
                inputData.bakedGI = SampleSH(baseN);
                inputData.normalizedScreenSpaceUV = float2(0,0);
                inputData.shadowMask = half4(1,1,1,1);

                Light mainLight = GetMainLight(inputData.shadowCoord);

                // Diffuse (Lambert)
                float ndl = saturate(dot(baseN, mainLight.direction));
                float3 diffuse = col * (mainLight.color * ndl);

                // Ambient via SH
                float3 ambient = col * inputData.bakedGI;

                // Cheap specular
                float3 h = normalize(mainLight.direction + vWS);
                float ndh = saturate(dot(baseN, h));
                float specPow = exp2(10.0 + _Smoothness * 8.0);
                float spec = pow(ndh, specPow) * _SpecularStrength;

                float shadowAtten = mainLight.shadowAttenuation * mainLight.distanceAttenuation;

                float3 lit = (diffuse + ambient) * shadowAtten + spec * mainLight.color;

                return half4(lit, 1.0);
            }

            ENDHLSL
        }
    }
}
