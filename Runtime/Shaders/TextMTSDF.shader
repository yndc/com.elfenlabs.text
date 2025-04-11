Shader "Elfenlabs/Text-MTSDF"
{
    Properties
    {
        _MainTex ("Sprite Texture Array", 2DArray) = "white" {}
        _GlyphThreshold ("SDF Threshold", Range(0.0, 1.0)) = 0.5
        _GlyphSmoothness ("SDF Smoothness", Range(0.0, 1.0)) = 0.1
        _GlyphSmoothnessScreenDistanceFactor ("SDF Smoothness Screen Distance Factor", Range(0.0, 1.0)) = 0.5
        _GlyphBaseColor ("Color", Color) = (1, 1, 1, 1)
        _GlyphRect ("Glyph Rect", Vector) = (0, 0, 1, 1)

        // Outline properties
        _GlyphOutlineThickness ("Outline Thickness", Range(0.0, 1.0)) = 0.5
        _GlyphOutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _GlyphOutlineOffset ("Outline Offset", Range(-1.0, 1.0)) = 0.5

        [IntRange] _GlyphAtlasIndex ("Atlas Index", Range(0, 16)) = 0
    }
    SubShader
    {
        Tags 
        { 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent" 
            "IgnoreProjector" = "True" 
            "RenderType" = "Transparent" 
            "PreviewType" = "Plane" 
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct VertexInput
            {
                float4 position : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct FragmentInput
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                float texIndex : TEXCOORD1;
                float4 color : COLOR0;
                float4 outlineColor : COLOR1;
                float outlineThickness : float0;
                float threshold : float1;
            };

            float median(float r, float g, float b) {
                return max(min(r, g), min(max(r, g), b));
            }

            float getScreenRange(float factor, float2 texCoord, float texSize) {
                float unitRange = (texSize * factor) / texSize;
                float2 screenTexSize = float2(1, 1) / fwidth(texCoord);
                return max(0.5 * dot(float2(unitRange, unitRange), screenTexSize), 1.0);
            }

            float4 getColor(half d, float4 faceColor)
            {
                half faceAlpha = 1 - saturate(d * 0.5);

                faceColor.rgb *= faceColor.a;

                faceColor *= faceAlpha;

                return faceColor;
            }

            float2 getUVRect(float2 uv, float4 rect)
            {
                return rect.xy + uv * rect.zw;
            }

            TEXTURE2D_ARRAY(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float _ScreenPxRange;
                float _GlyphSmoothness;
                float _GlyphSmoothnessScreenDistanceFactor;
                float _GlyphThreshold;
                float _GlyphAtlasIndex;
                float4 _GlyphRect;
                float4 _GlyphBaseColor;
                float _GlyphOutlineThickness;
                float4 _GlyphOutlineColor;
            CBUFFER_END

            // DOTS Instancing buffer
            #if defined(UNITY_DOTS_INSTANCING_ENABLED)
                UNITY_DOTS_INSTANCING_START(UserPropertyMetadata)
                    UNITY_DOTS_INSTANCED_PROP(float, _GlyphAtlasIndex)
                    UNITY_DOTS_INSTANCED_PROP(float4, _GlyphRect)
                    UNITY_DOTS_INSTANCED_PROP(float4, _GlyphBaseColor)
                    UNITY_DOTS_INSTANCED_PROP(float, _GlyphOutlineThickness)
                    UNITY_DOTS_INSTANCED_PROP(float4, _GlyphOutlineColor)
                UNITY_DOTS_INSTANCING_END(UserPropertyMetadata)
                #define _GlyphAtlasIndex UNITY_ACCESS_DOTS_INSTANCED_PROP(float, _GlyphAtlasIndex)
                #define _GlyphRect UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, _GlyphRect)
                #define _GlyphBaseColor UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, _GlyphBaseColor)
                #define _GlyphOutlineThickness UNITY_ACCESS_DOTS_INSTANCED_PROP(float, _GlyphOutlineThickness)
                #define _GlyphOutlineColor UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, _GlyphOutlineColor)
            #endif

            FragmentInput vert(VertexInput IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                FragmentInput OUT;
                
                OUT.position = TransformObjectToHClip(IN.position.xyz);

                OUT.uv = getUVRect(IN.uv, _GlyphRect);
                OUT.texIndex = _GlyphAtlasIndex;
                OUT.color = _GlyphBaseColor;
                OUT.outlineThickness = _GlyphOutlineThickness;
                OUT.outlineColor = _GlyphOutlineColor;
                OUT.threshold = _GlyphThreshold;

                return OUT;
            }

            float4 frag(FragmentInput IN) : SV_Target
            {
                float4 msd = SAMPLE_TEXTURE2D_ARRAY(_MainTex, sampler_MainTex, IN.uv, IN.texIndex);
                float sd = median(msd.r, msd.g, msd.b) * step(0.2, msd.a);
                float screenDistance = getScreenRange(_GlyphSmoothnessScreenDistanceFactor, IN.uv, 512);

                // Compute opacity with smooth transition
                float threshold = IN.threshold;
                float cap = min(threshold, 1 - threshold);
                float smoothness = cap * _GlyphSmoothness;// * saturate(screenDistance);

                // Calculate base text
                float3 baseColor = IN.color.rgb;
                float baseOpacity = smoothstep(threshold - smoothness, threshold + smoothness, sd);

                // Calculate outline 
                float outline = cap * IN.outlineThickness;
                float outlineOpacity = smoothstep(threshold - smoothness - outline, threshold + smoothness - outline, sd);
                float3 outlineColor = IN.outlineColor.rgb;

                // Calculate final color
                float3 color = lerp(outlineColor, baseColor, step(threshold - smoothness, sd));
                float opacity = max(baseOpacity, outlineOpacity);

                return float4(color, opacity);
            }
            ENDHLSL
        }
    }
}