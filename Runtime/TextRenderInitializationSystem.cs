using System;
using System.Text;
using Elfenlabs.Collections;
using Elfenlabs.String;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;

namespace Elfenlabs.Text
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct TextRenderInitializationSystem : ISystem
    {
        void OnUpdate(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder()
                .WithAll<TextBufferData>()
                .WithAll<FontAssetData>()
                .WithAll<FontAssetRuntimeData>()
                .WithAll<TextSizeData>()
                .WithAll<TextLayoutMaxSize>()
                .WithAll<TextLayoutBreakRule>()
                .WithNone<TextShapedTag>()
                .Build();

            var job = new TextRenderInitializationJob
            {
                ECB = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                FontPluginHandle = SystemAPI.GetSingleton<FontPluginRuntimeHandle>(),
            };

            state.Dependency = job.ScheduleParallel(query, state.Dependency);
        }

        partial struct TextRenderInitializationJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            public FontPluginRuntimeHandle FontPluginHandle;

            public void Execute(
                Entity entity,
                [ChunkIndexInQuery] int chunkIndexInQuery,
                in DynamicBuffer<TextBufferData> textBufferData,
                in TextSizeData textFontWorldSize,
                in FontAssetData fontAssetData,
                in FontAssetRuntimeData fontRuntimeData,
                in TextLayoutMaxSize textLayoutMaxSize,
                in TextLayoutBreakRule textLayoutBreakRule
            )
            {
                ECB.AddComponent(chunkIndexInQuery, entity, new TextShapedTag { });

                // Generate glyphs for each character in the string
                FontLibrary.ShapeText(
                    FontPluginHandle.Value,
                    fontRuntimeData.Description.Handle,
                    Allocator.Temp,
                    textBufferData.AsNativeBuffer().ReinterpretCast<TextBufferData, byte>(),
                    out var glyphs);

                Debug.Log($"Shaped {glyphs.Count()} glyphs for {textBufferData.Length} characters");

                var positions = new NativeArray<float2>(glyphs.Count(), Allocator.Temp);

                var worldScale = textFontWorldSize.Value;
                var fontUnitsToWorld = worldScale / fontRuntimeData.Description.UnitsPerEM;
                var worldLineHeight = fontRuntimeData.Description.Height * fontUnitsToWorld;
                var atlasPixelToWorld = worldScale / fontAssetData.GlyphSize;
                var maxLineWidth = textLayoutMaxSize.Value.x > 0f ? textLayoutMaxSize.Value.x : float.MaxValue;
                var lastBreakOpportunity = -1;

                // Calculate positioning of each glyphs
                var cursor = float2.zero;
                for (int i = 0; i < glyphs.Count(); i++)
                {
                    var glyphShape = glyphs[i];
                    var worldXOffset = glyphShape.XOffset * fontUnitsToWorld;
                    var worldYOffset = glyphShape.YOffset * fontUnitsToWorld;
                    positions[i] = new float2(cursor.x + worldXOffset, cursor.y + worldYOffset);

                    var unicode = textBufferData[glyphs[i].Cluster].Value;
                    if (StringUtility.IsNewLine(unicode))
                    {
                        cursor.x = 0f;
                        cursor.y += worldLineHeight;
                        lastBreakOpportunity = -1;
                        continue;
                    }
                    if (StringUtility.IsBreakOpportunity(unicode))
                    {
                        lastBreakOpportunity = i;
                    }

                    var worldXAdvance = glyphShape.XAdvance * fontUnitsToWorld;
                    var worldYAdvance = glyphShape.YAdvance * fontUnitsToWorld;

                    if ((cursor.x + worldXAdvance) > maxLineWidth)
                    {
                        // If the current glyph is a break opportunity, we can break the line here
                        if (lastBreakOpportunity == i)
                        {
                            cursor.x = 0f;
                            cursor.y += worldLineHeight;
                            lastBreakOpportunity = -1;
                        }
                        else if (textLayoutBreakRule.Value == BreakRule.Word)
                        {
                            // No break opportunity, take the L and overflow
                            if (lastBreakOpportunity == -1)
                            {
                                cursor.x += worldXAdvance;
                                cursor.y += worldYAdvance;
                            }

                            // Set the cursor to the next line and reposition all glyphs from lastBreakOpportunity to i
                            else
                            {
                                cursor.x = 0f;
                                cursor.y += worldLineHeight;

                                i = lastBreakOpportunity;
                                lastBreakOpportunity = -1;
                            }
                        }
                        else if (textLayoutBreakRule.Value == BreakRule.Character)
                        {
                            cursor.x = 0f;
                            cursor.y += worldLineHeight;
                            lastBreakOpportunity = -1;
                        }
                    }
                    else
                    {
                        cursor.x += worldXAdvance;
                        cursor.y += worldYAdvance;
                    }
                }

                // Create glyphs and set their transforms
                for (int i = 0; i < glyphs.Count(); i++)
                {
                    var codePoint = glyphs[i].CodePoint;

                    if (fontRuntimeData.GlyphRectMap.TryGetValue(codePoint, out var glyphInfo))
                    {
                        var glyphEntity = ECB.Instantiate(chunkIndexInQuery, fontRuntimeData.PrototypeEntity);
                        ECB.AddComponent(chunkIndexInQuery, glyphEntity, new Parent { Value = entity });
                        ECB.AddComponent(chunkIndexInQuery, glyphEntity, new MaterialPropertyGlyphAtlasIndex { Value = 0 });
                        ECB.AddComponent(chunkIndexInQuery, glyphEntity, new MaterialPropertyGlyphRect { Value = glyphInfo.AtlasUV });

                        // TODO: set conditional
                        ECB.AddComponent(chunkIndexInQuery, glyphEntity, new MaterialPropertyGlyphBaseColor { Value = new float4(1f, 1f, 1f, 1f) });
                        ECB.AddComponent(chunkIndexInQuery, glyphEntity, new MaterialPropertyGlyphOutlineThickness { Value = 0.0f });
                        ECB.AddComponent(chunkIndexInQuery, glyphEntity, new MaterialPropertyGlyphOutlineColor { Value = new float4(0f, 0f, 0f, 1f) });

                        // Calculate sizes
                        var worldSizeNoPadding = new float2(glyphInfo.Metrics.WidthFontUnits, glyphInfo.Metrics.HeightFontUnits) * fontUnitsToWorld;
                        var worldPadding = fontAssetData.Padding * atlasPixelToWorld;

                        // Calculate offset from the baseline 
                        var worldBearingOffset = new float2(
                            glyphInfo.Metrics.LeftFontUnits * fontUnitsToWorld,
                            (glyphInfo.Metrics.TopFontUnits - glyphInfo.Metrics.HeightFontUnits) * fontUnitsToWorld
                        );

                        // Set the transforms
                        var basePosition = positions[i];
                        ECB.AddComponent(chunkIndexInQuery, glyphEntity, new LocalTransform
                        {
                            Position = new float3(
                                basePosition.x + (0.5f * worldSizeNoPadding.x) + worldBearingOffset.x,
                                -basePosition.y + (0.5f * worldSizeNoPadding.y) + worldBearingOffset.y,
                                0f),
                            Rotation = quaternion.identity,
                            Scale = 1f
                        });

                        ECB.AddComponent(chunkIndexInQuery, glyphEntity, new PostTransformMatrix
                        {
                            Value = float4x4.TRS(
                                float3.zero,
                                quaternion.identity,
                                new float3(worldSizeNoPadding + worldPadding * 2f, 1f))
                        });
                    }
                }
            }
        }
    }
}