Shader "Elfenlabs/Text-MTSDF"
{
    Properties
    {
        _MainTex ("Sprite Texture Array", 2DArray) = "white" {}
        _GlyphThreshold ("SDF Threshold", Range(0.0, 1.0)) = 0.5
        _GlyphSmoothness ("SDF Smoothness", Range(0.0, 1.0)) = 0.1
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
                float2 atlasUV : TEXCOORD0;
                float texIndex : TEXCOORD1;
                float4 baseColor : COLOR0;
                float4 outlineColor : COLOR1;
                float outlineThickness : float0;
                float threshold : float1;
            };

            float median(float r, float g, float b) {
                return max(min(r, g), min(max(r, g), b));
            }

            float4 getColor(half d, float4 faceColor)
            {
                half faceAlpha = 1 - saturate(d * 0.5);

                faceColor.rgb *= faceColor.a;

                faceColor *= faceAlpha;

                return faceColor;
            }

            float2 getAtlasUV(float2 uv, float4 rect)
            {
                return rect.xy + uv * rect.zw;
            }

            TEXTURE2D_ARRAY(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float _ScreenPxRange;
                float _GlyphSmoothness;
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

                OUT.atlasUV = getAtlasUV(IN.uv, _GlyphRect);
                OUT.texIndex = _GlyphAtlasIndex;
                OUT.baseColor = _GlyphBaseColor;
                OUT.outlineThickness = _GlyphOutlineThickness;
                OUT.outlineColor = _GlyphOutlineColor;
                OUT.threshold = _GlyphThreshold;

                return OUT;
            }

            float4 frag(FragmentInput IN) : SV_Target
            {
                // Sample MSDF texture
                float4 msd = SAMPLE_TEXTURE2D_ARRAY(_MainTex, sampler_MainTex, IN.atlasUV, IN.texIndex);
                float sd = median(msd.r, msd.g, msd.b);

                float screenPixelDist = fwidth(sd);

                // Calculate smoothing range based on gradient and user control
                // Add a small epsilon to prevent division by zero or overly sharp edges at glancing angles
                float smooth = max(screenPixelDist * _GlyphSmoothness, 0.0001);

                // Distance from the glyph edge (threshold = _GlyphThreshold) in SDF units
                float glyphDist = sd - _GlyphThreshold;

                // --- Calculate Base Glyph Alpha & Base Color Lerp Factor ---
                // This determines both the base shape alpha and the point where color transitions
                // Calculated always.
                float glyphAlpha = smoothstep(-smooth, smooth, glyphDist);
                float colorLerp = glyphAlpha; // Use base glyph transition for color blend

                // --- Calculate Potential Outline Alpha (calculated always) ---
                // Convert outline thickness (0..1 range) to SDF units offset from threshold
                float outlineWidthSD = IN.outlineThickness * 0.5;
                // Distance from the *outer edge* of the outline
                // If thickness is 0, outlineWidthSD is 0, and outlineDist effectively equals glyphDist
                float outlineDist = sd - (_GlyphThreshold - outlineWidthSD);
                // Calculate alpha based on the outline distance. If thickness is 0, this yields glyphAlpha.
                float potentialOutlineAlpha = smoothstep(-smooth, smooth, outlineDist);

                // --- Determine Final Alpha without branching ---
                // Use step to get 0 if thickness is ~0, and 1 if thickness is > 0.001
                float outlineActive = step(0.001, IN.outlineThickness);
                // Lerp between base alpha and potential outline alpha based on whether outline is active.
                // If outlineActive=0, result is glyphAlpha.
                // If outlineActive=1, result is potentialOutlineAlpha.
                float combinedAlpha = lerp(glyphAlpha, potentialOutlineAlpha, outlineActive);

                // --- Blend Colors (always uses the base glyph edge transition) ---
                float3 blendedColor = lerp(IN.outlineColor.rgb, IN.baseColor.rgb, colorLerp);

                // Apply base color alpha (from vertex/material color) to the final alpha
                combinedAlpha *= IN.baseColor.a;

                // Return non-premultiplied color suitable for Blend SrcAlpha OneMinusSrcAlpha
                return float4(blendedColor, combinedAlpha);
            }
            ENDHLSL
        }
    }
}