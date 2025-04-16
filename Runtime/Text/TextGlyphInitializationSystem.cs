using Elfenlabs.Collections;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Elfenlabs.Text
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct TextGlyphInitializationSystem : ISystem
    {
        void OnUpdate(ref SystemState state)
        {
            var initializationQuery = SystemAPI.QueryBuilder()
                .WithPresentRW<TextLayoutRequireUpdate>()
                .WithAllRW<TextGlyphRequireUpdate>()
                .WithAllRW<TextGlyphBuffer>()
                .WithAll<TextStringBuffer>()
                .WithAll<FontAssetReference>()
                .WithAll<FontAssetRuntimeData>()
                .Build();

            if (initializationQuery.IsEmpty)
                return;

            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var initializationJob = new TextGlyphInitializationJob
            {
                ECB = ecb.AsParallelWriter(),
                FontPluginHandle = SystemAPI.GetSingleton<FontPluginRuntimeHandle>(),
                GlyphRequireUpdateLookup = SystemAPI.GetComponentLookup<TextGlyphRequireUpdate>(),
                LayoutRequireUpdateLookup = SystemAPI.GetComponentLookup<TextLayoutRequireUpdate>()
            };

            state.Dependency = initializationJob.ScheduleParallel(initializationQuery, state.Dependency);
        }

        partial struct TextGlyphInitializationJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            public FontPluginRuntimeHandle FontPluginHandle;
            [NativeDisableParallelForRestriction]
            public ComponentLookup<TextGlyphRequireUpdate> GlyphRequireUpdateLookup;
            [NativeDisableParallelForRestriction]
            public ComponentLookup<TextLayoutRequireUpdate> LayoutRequireUpdateLookup;

            public void Execute(
                Entity entity,
                [ChunkIndexInQuery] int chunkIndexInQuery,
                ref DynamicBuffer<TextGlyphBuffer> textGlyphs,
                in DynamicBuffer<TextStringBuffer> textStringBuffer,
                in FontAssetReference fontAssetData,
                in FontAssetRuntimeData fontRuntimeData
            )
            {
                GlyphRequireUpdateLookup.SetComponentEnabled(entity, false);
                LayoutRequireUpdateLookup.SetComponentEnabled(entity, true);

                foreach (var glyph in textGlyphs)
                {
                    ECB.DestroyEntity(chunkIndexInQuery, glyph.Entity);
                }

                textGlyphs.Clear();

                // Generate glyphs for each character in the string
                FontLibrary.ShapeText(
                    FontPluginHandle.Value,
                    fontRuntimeData.Description.Handle,
                    Allocator.Temp,
                    textStringBuffer.AsNativeBuffer().ReinterpretCast<TextStringBuffer, byte>(),
                    out var glyphShape);

                var atlasPixelToEm = 1f / fontAssetData.Value.Value.AtlasConfig.GlyphSize;
                var fontUnitsToEm = 1f / fontRuntimeData.Description.UnitsPerEM;
                for (int i = 0; i < glyphShape.Count(); i++)
                {
                    if (fontRuntimeData.GlyphMap.TryGetValue(glyphShape[i].CodePoint, out var glyphInfo))
                    {
                        // Calculate runtime values in em units
                        var advance = new float2(glyphShape[i].XAdvance, glyphShape[i].YAdvance) * fontUnitsToEm;
                        var bearingOffset = new float2(
                            glyphInfo.Metrics.LeftFontUnits * fontUnitsToEm,
                            (glyphInfo.Metrics.TopFontUnits - glyphInfo.Metrics.HeightFontUnits) * fontUnitsToEm
                        );
                        var shapeOffset = new float2(glyphShape[i].XOffset, glyphShape[i].YOffset) * fontUnitsToEm;

                        // Real size is the real size of the glyph itself without padding
                        var realSize = new float2(glyphInfo.Metrics.WidthFontUnits, glyphInfo.Metrics.HeightFontUnits) * fontUnitsToEm;
                        var quadSize = realSize + (2f * fontAssetData.Value.Value.AtlasConfig.Padding * atlasPixelToEm);

                        Debug.Log("Glyph: " + glyphShape[i].CodePoint + " - " + glyphInfo.Metrics.LeftFontUnits + " - " + glyphInfo.Metrics.TopFontUnits + " - " + glyphInfo.Metrics.WidthFontUnits + " - " + glyphInfo.Metrics.HeightFontUnits);

                        var glyphEntity = ECB.Instantiate(chunkIndexInQuery, fontRuntimeData.PrototypeEntity);

                        ECB.AddComponent(chunkIndexInQuery, glyphEntity, new TextLayoutRequireUpdate());
                        ECB.AddComponent(chunkIndexInQuery, glyphEntity, new Parent { Value = entity });
                        ECB.AddComponent(chunkIndexInQuery, glyphEntity, new LocalTransform { Scale = 1f });
                        ECB.AddComponent(chunkIndexInQuery, glyphEntity, new PostTransformMatrix { Value = float4x4.identity });

                        ECB.AddComponent(chunkIndexInQuery, glyphEntity, new MaterialPropertyGlyphAtlasIndex { Value = 0 });
                        ECB.AddComponent(chunkIndexInQuery, glyphEntity, new MaterialPropertyGlyphRect { Value = glyphInfo.AtlasUV });

                        // TODO: set conditional
                        ECB.AddComponent(chunkIndexInQuery, glyphEntity, new MaterialPropertyGlyphBaseColor { Value = new float4(1f, 1f, 1f, 1f) });
                        ECB.AddComponent(chunkIndexInQuery, glyphEntity, new MaterialPropertyGlyphOutlineThickness { Value = 0.0f });
                        ECB.AddComponent(chunkIndexInQuery, glyphEntity, new MaterialPropertyGlyphOutlineColor { Value = new float4(0f, 0f, 0f, 1f) });

                        ECB.AppendToBuffer(chunkIndexInQuery, entity, new TextGlyphBuffer
                        {
                            Entity = glyphEntity,
                            Cluster = glyphShape[i].Cluster,
                            PositionEm = float2.zero,
                            Line = 0,
                            AdvanceEm = advance,
                            OffsetEm = shapeOffset + bearingOffset,
                            RealSizeEm = realSize,
                            QuadSizeEm = quadSize,
                        });
                    }
                    else
                    {
                        fontRuntimeData.MissingGlyphSet.Add(glyphShape[i].CodePoint);
                    }
                }
            }
        }
    }
}