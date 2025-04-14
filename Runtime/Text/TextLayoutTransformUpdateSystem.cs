using Elfenlabs.String;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Elfenlabs.Text
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct TextLayoutTransformUpdateSystem : ISystem
    {
        void OnUpdate(ref SystemState state)
        {
            var layoutTransformQuery = SystemAPI.QueryBuilder()
                .WithAllRW<TextLayoutRequireUpdate>()
                .WithAll<TextLayoutGlyphRuntimeBuffer>()
                .WithAll<TextFontSize>()
                .Build();

            var transformUpdateJob = new TextLayoutTransformUpdateJob
            {
                RequireUpdateLookup = SystemAPI.GetComponentLookup<TextLayoutRequireUpdate>(),
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(),
                PostTransformMatrixLookup = SystemAPI.GetComponentLookup<PostTransformMatrix>(),
            };

            state.Dependency = transformUpdateJob.ScheduleParallel(layoutTransformQuery, state.Dependency);
        }

        partial struct TextLayoutTransformUpdateJob : IJobEntity
        {
            [NativeDisableParallelForRestriction]
            public ComponentLookup<TextLayoutRequireUpdate> RequireUpdateLookup;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<LocalTransform> LocalTransformLookup;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<PostTransformMatrix> PostTransformMatrixLookup;

            public void Execute(
                Entity entity,
                in DynamicBuffer<TextLayoutGlyphRuntimeBuffer> textGlyphs,
                in TextFontSize textFontWorldSize
            )
            {
                RequireUpdateLookup.SetComponentEnabled(entity, false);

                for (int i = 0; i < textGlyphs.Length; i++)
                {
                    var glyph = textGlyphs[i];
                    var emToWorld = textFontWorldSize.Value;

                    // Base position Y is negated because Unity's Y axis is up, while the font's Y axis is down
                    var finalPosition = new float2(glyph.PositionEm.x, -glyph.PositionEm.y) + glyph.OffsetEm + 0.5f * glyph.RealSizeEm;

                    LocalTransformLookup[glyph.Entity] = new LocalTransform
                    {
                        Position = new float3(finalPosition * emToWorld, 0f),
                        Rotation = quaternion.identity,
                        Scale = 1f
                    };

                    PostTransformMatrixLookup[glyph.Entity] = new PostTransformMatrix
                    {
                        Value = float4x4.TRS(float3.zero, quaternion.identity, new float3(glyph.QuadSizeEm * emToWorld, 1f))
                    };
                }
            }
        }
    }
}