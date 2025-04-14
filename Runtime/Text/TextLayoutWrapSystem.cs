using Elfenlabs.String;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Elfenlabs.Text
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TextLayoutTransformUpdateSystem))]
    public partial struct TextLayoutWrapSystem : ISystem
    {
        void OnUpdate(ref SystemState state)
        {
            var wrapQuery = SystemAPI.QueryBuilder()
               .WithAllRW<TextLayoutGlyphRuntimeBuffer>()
               .WithAllRW<TextLayoutSizeRuntime>()
               .WithAll<FontAssetRuntimeData>()
               .WithAll<TextStringBuffer>()
               .WithAll<TextLayoutMaxSize>()
               .WithAll<TextLayoutBreakRule>()
               .WithAll<TextLayoutRequireUpdate>()
               .Build();

            var layoutUpdateJob = new TextLayoutWrapJob();

            state.Dependency = layoutUpdateJob.ScheduleParallel(wrapQuery, state.Dependency);
        }

        partial struct TextLayoutWrapJob : IJobEntity
        {
            public void Execute(
                Entity entity,
                ref DynamicBuffer<TextLayoutGlyphRuntimeBuffer> textGlyphs,
                ref TextLayoutSizeRuntime textLayoutSize,
                in FontAssetRuntimeData fontRuntimeData,
                in DynamicBuffer<TextStringBuffer> textString,
                in TextLayoutMaxSize textLayoutMaxSize,
                in TextLayoutBreakRule textLayoutBreakRule)
            {
                var fontUnitsToEm = 1f / fontRuntimeData.Description.UnitsPerEM;
                var maxLineWidth = textLayoutMaxSize.Value.x > 0f ? textLayoutMaxSize.Value.x : float.MaxValue;
                var lineHeight = fontRuntimeData.Description.Height * fontUnitsToEm;

                // Calculate positioning of each glyphs
                var calculator = new WrapCalculator(lineHeight, maxLineWidth, textLayoutBreakRule.Value);
                for (int i = 0; i < textGlyphs.Length; i++)
                {
                    ref var glyph = ref textGlyphs.ElementAt(i);

                    glyph.PositionEm = calculator.GetCursor();
                    glyph.Line = calculator.GetLine();

                    var unicode = textString[glyph.Cluster].Value;
                    if (StringUtility.IsNewLine(unicode))
                    {
                        calculator.AdvanceLine();
                        continue;
                    }
                    if (StringUtility.IsBreakOpportunity(unicode))
                    {
                        calculator.SetBreakOpportunity(i);
                    }

                    i = calculator.Advance(i, glyph.AdvanceEm);
                }

                textLayoutSize.Value = calculator.GetSize();
            }

            struct WrapCalculator
            {
                readonly float lineHeight;
                readonly float lineWidth;
                readonly BreakRule breakRule;
                float2 cursor;
                int lastBreakOpportunity;
                int line;
                float longestLine;

                public WrapCalculator(float lineHeight, float lineWidth, BreakRule breakRule = BreakRule.Word)
                {
                    this.lineHeight = lineHeight;
                    this.lineWidth = lineWidth;
                    this.breakRule = breakRule;
                    cursor = float2.zero;
                    lastBreakOpportunity = -1;
                    line = 0;
                    longestLine = 0;
                }

                public readonly float2 GetCursor()
                {
                    return cursor;
                }

                public readonly int GetLine()
                {
                    return line;
                }

                public readonly float2 GetSize()
                {
                    return new float2(longestLine, lineHeight * (line + 1));
                }

                public int Advance(int glyphPos, float2 advance)
                {
                    if ((cursor.x + advance.x) > lineWidth)
                    {
                        // If the current glyph is a break opportunity, we can break the line here
                        if (lastBreakOpportunity == glyphPos)
                        {
                            AdvanceLine();
                        }
                        else if (breakRule == BreakRule.Word)
                        {
                            // No break opportunity, take the L and overflow
                            if (lastBreakOpportunity == -1)
                            {
                                cursor += advance;
                            }

                            // Set the cursor to the next line and reposition all glyphs from lastBreakOpportunity to i
                            else
                            {
                                var rewindToPos = lastBreakOpportunity;
                                AdvanceLine();
                                return rewindToPos;
                            }
                        }
                        else if (breakRule == BreakRule.Character)
                        {
                            AdvanceLine();
                        }
                    }
                    else
                    {
                        cursor += advance;
                    }

                    return glyphPos;
                }

                public void AdvanceLine()
                {
                    longestLine = math.max(longestLine, cursor.x);
                    cursor.x = 0f;
                    cursor.y += lineHeight;
                    lastBreakOpportunity = -1;
                    line++;
                }

                public void SetBreakOpportunity(int glyphPos)
                {
                    lastBreakOpportunity = glyphPos;
                }
            }
        }
    }
}