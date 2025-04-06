using System;
using Elfenlabs.Mesh;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Elfenlabs.Text
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct TextRenderInitializationSystem : ISystem
    {
        Entity QuadPrototype;

        void OnCreate(ref SystemState state)
        {
            QuadPrototype = MeshUtility.CreatePrefab("Glyph", MeshUtility.CreateQuad(1f, 1f), state.EntityManager, Shader.Find("Elfenlabs/Text-MTSDF"), 0);
        }

        void OnUpdate(ref SystemState state)
        {
            // var query = SystemAPI.QueryBuilder()
            //     .WithAll<TextStringConfig>()
            //     .WithAll<TextFontConfig>()
            //     .WithNone<TextStringState>()
            //     .Build();

            // var job = new TextRenderInitializationJob
            // {
            //     ECB = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
            //     FontPluginHandle = SystemAPI.GetSingleton<FontPluginHandle>().Handle,
            //     QuadPrototype = QuadPrototype,
            // };

            // state.Dependency = job.ScheduleParallel(query, state.Dependency);
        }

        partial struct TextRenderInitializationJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            public Entity QuadPrototype;

            public IntPtr FontPluginHandle;

            public void Execute(Entity entity, [ChunkIndexInQuery] int chunkIndexInQuery, in TextStringConfig textStringConfig, in TextFontConfig textFontConfig)
            {
                ECB.AddComponent(chunkIndexInQuery, entity, new TextStringState { Value = textStringConfig.Value });

                // Generate glyphs for each character in the string
                FontLibrary.ShapeText(
                    FontPluginHandle,
                    textFontConfig.FontIndex,
                    Allocator.Temp,
                    NativeBuffer<byte>.Alias(textStringConfig.Value),
                    out var glyphs);

                for (int i = 0; i < glyphs.Count(); i++)
                {
                    var quad = ECB.Instantiate(chunkIndexInQuery, QuadPrototype);
                    ECB.SetComponent(chunkIndexInQuery, quad, new Parent { Value = entity });
                    // ECB.AddComponent(chunkIndexInQuery, quad, new GlyphTextureIndex { Value = glyphs[i].CodePoint });
                }

            }
        }
    }
}