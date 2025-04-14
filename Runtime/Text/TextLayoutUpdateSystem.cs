using Elfenlabs.String;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Elfenlabs.Text
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct TextLayoutUpdateSystem : ISystem
    {
        void OnUpdate(ref SystemState state)
        {
            var layoutUpdateQuery = SystemAPI.QueryBuilder()
               .WithAllRW<TextLayoutGlyphRuntimeBuffer>()
               .WithAll<FontAssetRuntimeData>()
               .WithAll<TextStringBuffer>()
               .WithAll<TextLayoutMaxSize>()
               .WithAll<TextLayoutBreakRule>()
               .WithAll<TextLayoutRequireUpdate>()
               .Build();

            var layoutUpdateJob = new TextLayoutUpdateJob
            {
                RequireUpdateLookup = SystemAPI.GetComponentLookup<TextLayoutRequireUpdate>(),
            };

            state.Dependency = layoutUpdateJob.ScheduleParallel(layoutUpdateQuery, state.Dependency);

            var transformUpdateQuery = SystemAPI.QueryBuilder()
                .WithAll<TextLayoutGlyphRuntimeBuffer>()
                .WithAll<TextFontSize>()
                .Build();

            var transformUpdateJob = new GlyphTransformUpdateJob
            {
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(),
                PostTransformMatrixLookup = SystemAPI.GetComponentLookup<PostTransformMatrix>(),
            };

            state.Dependency = transformUpdateJob.ScheduleParallel(transformUpdateQuery, state.Dependency);
        }


        partial struct TextLayoutUpdateJob : IJobEntity
        {
            [NativeDisableParallelForRestriction]
            public ComponentLookup<TextLayoutRequireUpdate> RequireUpdateLookup;

            public void Execute(
                Entity entity,
                ref DynamicBuffer<TextLayoutGlyphRuntimeBuffer> textGlyphs,
                in FontAssetRuntimeData fontRuntimeData,
                in DynamicBuffer<TextStringBuffer> textString,
                in TextLayoutMaxSize textLayoutMaxSize,
                in TextLayoutBreakRule textLayoutBreakRule)
            {
                RequireUpdateLookup.SetComponentEnabled(entity, false);

                var fontUnitsToEm = 1f / fontRuntimeData.Description.UnitsPerEM;
                var maxLineWidth = textLayoutMaxSize.Value.x > 0f ? textLayoutMaxSize.Value.x : float.MaxValue;
                var lineHeight = fontRuntimeData.Description.Height * fontUnitsToEm;
                var lastBreakOpportunity = -1;

                // Calculate positioning of each glyphs
                var cursor = float2.zero;
                for (int i = 0; i < textGlyphs.Length; i++)
                {
                    ref var glyph = ref textGlyphs.ElementAt(i);

                    glyph.PositionEm = cursor;

                    var unicode = textString[glyph.Cluster].Value;
                    if (StringUtility.IsNewLine(unicode))
                    {
                        cursor.x = 0f;
                        cursor.y += lineHeight;
                        lastBreakOpportunity = -1;
                        continue;
                    }
                    if (StringUtility.IsBreakOpportunity(unicode))
                    {
                        lastBreakOpportunity = i;
                    }

                    if ((cursor.x + glyph.AdvanceEm.x) > maxLineWidth)
                    {
                        // If the current glyph is a break opportunity, we can break the line here
                        if (lastBreakOpportunity == i)
                        {
                            cursor.x = 0f;
                            cursor.y += lineHeight;
                            lastBreakOpportunity = -1;
                        }
                        else if (textLayoutBreakRule.Value == BreakRule.Word)
                        {
                            // No break opportunity, take the L and overflow
                            if (lastBreakOpportunity == -1)
                            {
                                cursor += glyph.AdvanceEm;
                            }

                            // Set the cursor to the next line and reposition all glyphs from lastBreakOpportunity to i
                            else
                            {
                                cursor.x = 0f;
                                cursor.y += lineHeight;

                                i = lastBreakOpportunity;
                                lastBreakOpportunity = -1;
                            }
                        }
                        else if (textLayoutBreakRule.Value == BreakRule.Character)
                        {
                            cursor.x = 0f;
                            cursor.y += lineHeight;
                            lastBreakOpportunity = -1;
                        }
                    }
                    else
                    {
                        cursor += glyph.AdvanceEm;
                    }
                }
            }
        }

        partial struct GlyphTransformUpdateJob : IJobEntity
        {
            [NativeDisableParallelForRestriction]
            public ComponentLookup<LocalTransform> LocalTransformLookup;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<PostTransformMatrix> PostTransformMatrixLookup;

            public void Execute(
                in DynamicBuffer<TextLayoutGlyphRuntimeBuffer> textGlyphs,
                in TextFontSize textFontWorldSize
            )
            {
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