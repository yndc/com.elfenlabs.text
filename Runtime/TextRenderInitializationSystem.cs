using System;
using Elfenlabs.Collections;
using Elfenlabs.Mesh;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

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
                .WithAll<TextFontWorldSize>()
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

            private readonly float GetFontUnitsToWorldScale(float unitsPerEM, float targetWorldHeight)
            {
                if (unitsPerEM <= 0)
                {
                    unitsPerEM = 1000f;
                }
                return targetWorldHeight / unitsPerEM;
            }

            public void Execute(
                Entity entity,
                [ChunkIndexInQuery] int chunkIndexInQuery,
                in DynamicBuffer<TextBufferData> textBufferData,
                in TextFontWorldSize textFontWorldSize,
                in FontAssetData fontAssetData,
                in FontAssetRuntimeData fontRuntimeData
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

                var worldScale = textFontWorldSize.Value;
                var fontUnitsToWorldScale = GetFontUnitsToWorldScale(fontRuntimeData.Description.UnitsPerEM, worldScale);

                // Position each glyphs
                var cursor = float2.zero;
                for (int i = 0; i < glyphs.Count(); i++)
                {
                    var glyphShape = glyphs[i];
                    var codePoint = glyphs[i].CodePoint;
                    var worldXOffset = glyphShape.XOffset * fontUnitsToWorldScale;
                    var worldYOffset = glyphShape.YOffset * fontUnitsToWorldScale;
                    var worldXAdvance = glyphShape.XAdvance * fontUnitsToWorldScale;
                    var worldYAdvance = glyphShape.YAdvance * fontUnitsToWorldScale;

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
                        var pixelSize = new float2(glyphInfo.AtlasUV.z, glyphInfo.AtlasUV.w) * fontAssetData.AtlasSize;
                        var pixelSizeNoPadding = pixelSize - fontAssetData.Padding * 2f;
                        var emSize = pixelSize / fontAssetData.GlyphSize;
                        var emSizeNoPadding = pixelSizeNoPadding / fontAssetData.GlyphSize;
                        var worldSize = emSize * worldScale;
                        var worldSizeNoPadding = emSizeNoPadding * worldScale;

                        Debug.Log($"Glyph {codePoint}," +
                            $"rect pixels: {pixelSizeNoPadding}, " +
                            $"metric pixels: {glyphInfo.PixelMetrics.Width - fontAssetData.Padding * 2f}, {glyphInfo.PixelMetrics.Height - fontAssetData.Padding * 2f} " +
                            $"metric top px: {glyphInfo.PixelMetrics.Top}");

                        // Calculate offset from the baseline 
                        var worldBearingOffset = new float2(
                            glyphInfo.LeftEM * worldScale,
                            (glyphInfo.TopEM - emSizeNoPadding.y) * worldScale
                        );

                        // Set the local transform of the glyph entity
                        ECB.AddComponent(chunkIndexInQuery, glyphEntity, new LocalTransform
                        {
                            Position = new float3(
                                cursor.x + worldXOffset + (0.5f * worldSizeNoPadding.x) + worldBearingOffset.x,
                                cursor.y + worldYOffset + (0.5f * worldSizeNoPadding.y) + worldBearingOffset.y,
                                0f),
                            Rotation = quaternion.identity,
                            Scale = 1f
                        });

                        ECB.AddComponent(chunkIndexInQuery, glyphEntity, new PostTransformMatrix
                        {
                            Value = float4x4.TRS(
                                float3.zero,
                                quaternion.identity,
                                new float3(worldSize, 1f))
                        });
                    }

                    // Regardless of whether the glyph was found, we need to update the position for the next glyph
                    cursor.x += worldXAdvance;
                    cursor.y += worldYAdvance;
                }
            }
        }
    }
}