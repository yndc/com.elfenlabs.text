using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Elfenlabs.Text
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct TextRenderInitializationSystem : ISystem
    {
        void OnUpdate(ref SystemState state)
        {
            // SystemAPI.QueryBuilder().WithAll<TextStringConfig>().Build();
        }

        partial struct TextRenderInitializationJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            public Entity QuadPrototype;

            public void Execute(Entity entity, [ChunkIndexInQuery] int chunkIndexInQuery, in TextStringConfig textStringConfig)
            {
                var quad = ECB.Instantiate(chunkIndexInQuery, QuadPrototype);
                ECB.SetComponent(chunkIndexInQuery, quad, new Parent { Value = entity });
            }
        }
    }
}