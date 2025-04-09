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
                in FontAssetData fontAssetData,
                in FontAssetRuntimeData fontAssetRuntimeData
            )
            {
                ECB.AddComponent(chunkIndexInQuery, entity, new TextShapedTag { });

                Debug.Log($"TextBufferData Length: {textBufferData.Length}");
                for (int i = 0; i < textBufferData.Length; i++)
                {
                    Debug.Log($"TextBufferData: {(char)textBufferData[i].Value}");
                }

                var a = textBufferData.AsNativeBuffer().ReinterpretCast<TextBufferData, byte>();

                Debug.Log($"TextBufferData AsNativeBuffer Length: {a.Count()}");
                for (int i = 0; i < a.Count(); i++)
                {
                    Debug.Log($"TextBufferData AsNativeBuffer: {(char)a[i]}");
                }

                // Generate glyphs for each character in the string
                FontLibrary.ShapeText(
                    FontPluginHandle.Value,
                    fontAssetRuntimeData.Index,
                    Allocator.Temp,
                    textBufferData.AsNativeBuffer().ReinterpretCast<TextBufferData, byte>(),
                    out var glyphs);

                // Position each glyphs
                var position = new float2(0f, 0f);
                for (int i = 0; i < glyphs.Count(); i++)
                {
                    var codePoint = glyphs[i].CodePoint;
                    if (fontAssetRuntimeData.GlyphRectMap.TryGetValue(codePoint, out var glyphRect))
                    {
                        Debug.Log($"CodePoint: {codePoint}, GlyphRect: x={glyphRect.x}, y={glyphRect.y}, width={glyphRect.z}, height={glyphRect.w}");
                        var glyphEntity = ECB.Instantiate(chunkIndexInQuery, fontAssetRuntimeData.PrototypeEntity);
                        ECB.AddComponent(chunkIndexInQuery, glyphEntity, new Parent { Value = entity });
                        ECB.AddComponent(chunkIndexInQuery, glyphEntity, new MaterialPropertyGlyphAtlasIndex { Value = 0 });
                        ECB.AddComponent(chunkIndexInQuery, glyphEntity, new MaterialPropertyGlyphRect { Value = glyphRect });

                        // TODO: set conditional
                        ECB.AddComponent(chunkIndexInQuery, glyphEntity, new MaterialPropertyGlyphBaseColor { Value = new float4(1f, 1f, 1f, 1f) });
                        ECB.AddComponent(chunkIndexInQuery, glyphEntity, new MaterialPropertyGlyphOutlineThickness { Value = 0.2f });
                        ECB.AddComponent(chunkIndexInQuery, glyphEntity, new MaterialPropertyGlyphOutlineColor { Value = new float4(0f, 0f, 0f, 1f) });

                        // Set the local transform of the glyph entity
                        ECB.AddComponent(chunkIndexInQuery, glyphEntity, new LocalTransform
                        {
                            Position = new float3(position + new float2(glyphs[i].XOffset, glyphs[i].YOffset), 0f),
                            Rotation = quaternion.identity,
                            Scale = 1f
                        });
                    }

                    // Regardless of whether the glyph was found, we need to update the position for the next glyph
                    // position.x += glyphs[i].XAdvance;
                    position.x += 1;
                    position.y += glyphs[i].YAdvance;
                }
            }
        }
    }
}