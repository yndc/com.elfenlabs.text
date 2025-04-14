using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Elfenlabs.Text
{
    [UpdateBefore(typeof(TextGlyphInitializationSystem))]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct TextStringChangeSystem : ISystem
    {
        void OnUpdate(ref SystemState state)
        {
            var changedTextQuery = SystemAPI.QueryBuilder()
                .WithAll<TextStringBuffer>()
                .WithAll<TextLayoutGlyphRuntimeBuffer>()
                .Build();

            changedTextQuery.SetChangedVersionFilter(ComponentType.ReadOnly<TextStringBuffer>());

            if (changedTextQuery.IsEmpty)
                return;

            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var textStringChangeJob = new TextStringChangeJob
            {
                ECB = ecb.AsParallelWriter(),
            };

            state.Dependency = textStringChangeJob.ScheduleParallel(changedTextQuery, state.Dependency);
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        partial struct TextStringChangeJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;

            public void Execute(
                [ChunkIndexInQuery] int chunkIndexInQuery,
                Entity entity,
                ref DynamicBuffer<TextLayoutGlyphRuntimeBuffer> textGlyphs,
                in DynamicBuffer<TextStringBuffer> textString)
            {
                // Update the glyphs based on the new string
                // For now we completely clear the glyphs and reinitialize them
                // Optimally we should only update the changed parts of the string

                foreach (var glyph in textGlyphs)
                {
                    ECB.DestroyEntity(chunkIndexInQuery, glyph.Entity);
                }

                ECB.RemoveComponent<TextLayoutGlyphRuntimeBuffer>(chunkIndexInQuery, entity);
                ECB.SetComponentEnabled<TextLayoutRequireUpdate>(chunkIndexInQuery, entity, true);
            }
        }
    }
}