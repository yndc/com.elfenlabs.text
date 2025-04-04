Shader "Elfenlabs/Text-MTSDF"
{
    Properties
    {
        _MainTex ("Sprite Texture Array", 2DArray) = "white" {}
        _Threshold ("SDF Threshold", Range(0.0, 1.0)) = 0.5
        _Smoothness ("SDF Smoothness", Range(0.0, 1.0)) = 0.1
        _SmoothnessScreenDistanceFactor ("SDF Smoothness Screen Distance Factor", Range(0.0, 1.0)) = 0.5
        [IntRange] _TextAtlasIndex ("Atlas Index", Range(0, 16)) = 0
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
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct FragmentInput
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                float texIndex : TEXCOORD1;
                float4 color : COLOR0;
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

            TEXTURE2D_ARRAY(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float _ScreenPxRange;
                float _Threshold;
                float _Smoothness;
                float _SmoothnessScreenDistanceFactor;
                float _TextAtlasIndex;
            CBUFFER_END


            // DOTS Instancing buffer
            #if defined(UNITY_DOTS_INSTANCING_ENABLED)
                UNITY_DOTS_INSTANCING_START(UserPropertyMetadata)
                    UNITY_DOTS_INSTANCED_PROP(float, _TextAtlasIndex)
                    UNITY_DOTS_INSTANCED_PROP(float4, _GlyphRect)
                    UNITY_DOTS_INSTANCED_PROP(float4, _GlyphColor)
                UNITY_DOTS_INSTANCING_END(UserPropertyMetadata)
            #else 
            #endif

            FragmentInput vert(VertexInput IN)
            {
                FragmentInput OUT;
                
                float4x4 objectToWorld = GetObjectToWorldMatrix();
                OUT.position = TransformObjectToHClip(IN.position.xyz);
                
                #if defined(UNITY_DOTS_INSTANCING_ENABLED)
                    UNITY_SETUP_INSTANCE_ID(IN);
                #endif

                #if defined(UNITY_DOTS_INSTANCING_ENABLED)
                    float4 uvRect = UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, _GlyphRect);
                    OUT.uv = IN.uv * uvRect.zw + uvRect.xy;
                    OUT.texIndex = UNITY_ACCESS_DOTS_INSTANCED_PROP(float, _TextAtlasIndex);
                    OUT.color = UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, _GlyphColor);
                #else 
                    OUT.uv = IN.uv;
                    OUT.texIndex = _TextAtlasIndex;
                    OUT.color = IN.color;
                #endif

                return OUT;
            }

            float4 frag(FragmentInput IN) : SV_Target
            {
                float4 msd = SAMPLE_TEXTURE2D_ARRAY(_MainTex, sampler_MainTex, IN.uv, IN.texIndex);
                float sd = median(msd.r, msd.g, msd.b);
                float screenDistance = getScreenRange(_SmoothnessScreenDistanceFactor, IN.uv, 512);

                // Compute opacity with smooth transition
                float threshold = _Threshold;
                float cap = min(threshold, 1 - threshold);
                float smoothness = cap * _Smoothness;// * saturate(screenDistance);
                float opacity = smoothstep(threshold - smoothness, threshold + smoothness, sd);

                return float4(IN.color.rgb, opacity);
            }
            ENDHLSL
        }
    }
}