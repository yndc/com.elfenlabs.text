using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Elfenlabs.Text
{
    [UpdateAfter(typeof(TextLayoutWrapSystem))]
    [UpdateBefore(typeof(TextLayoutTransformUpdateSystem))]
    public partial struct TextLayoutAlignSystem : ISystem
    {
        void OnUpdate(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder()
                .WithAllRW<TextLayoutGlyphRuntimeBuffer>()
                .WithAll<TextLayoutSizeRuntime>()
                .WithAll<TextLayoutAlign>()
                .WithAll<TextLayoutRequireUpdate>()
                .Build();

            var job = new TextLayoutAlignJob();

            state.Dependency = job.ScheduleParallel(query, state.Dependency);
        }

        partial struct TextLayoutAlignJob : IJobEntity
        {
            struct AlignCalculator
            {
                readonly TextAlign align;
                readonly float lineMax;
                float current;
                int line;
                int lineStart;

                public AlignCalculator(float lineMax, TextAlign textAlign)
                {
                    this.lineMax = lineMax;
                    align = textAlign;
                    current = 0f;
                    line = 0;
                    lineStart = 0;
                }

                public void Align(ref DynamicBuffer<TextLayoutGlyphRuntimeBuffer> textGlyphs)
                {
                    for (int i = 0; i < textGlyphs.Length; i++)
                    {
                        var glyph = textGlyphs[i];
                        if (line != glyph.Line)
                        {
                            AlignLine(ref textGlyphs, i);

                            line = glyph.Line;
                            lineStart = i;
                            current = 0f;
                        }
                        current += glyph.AdvanceEm.x;
                    }

                    // Align the last line
                    AlignLine(ref textGlyphs, textGlyphs.Length);
                }

                void AlignLine(ref DynamicBuffer<TextLayoutGlyphRuntimeBuffer> textGlyphs, int lineEnd)
                {
                    if (current < lineMax)
                    {
                        var diff = lineMax - current;
                        var offset = 0f;
                        switch (align)
                        {
                            case TextAlign.Right:

                                offset = diff;

                                // We ignore the last glyph's advance if it is a space or a new line
                                var lastLineGlyph = textGlyphs[lineEnd - 1];
                                if (lastLineGlyph.RealSizeEm.Equals(float2.zero))
                                {
                                    offset += lastLineGlyph.AdvanceEm.x;
                                }
                                break;
                            case TextAlign.Center:
                                offset = diff / 2f;
                                break;
                            default:
                                break;
                        }

                        for (int j = lineStart; j < lineEnd; j++)
                        {
                            ref var glyphToAlign = ref textGlyphs.ElementAt(j);
                            glyphToAlign.PositionEm.x += offset;
                        }
                    }
                }
            }

            void Execute(
                ref DynamicBuffer<TextLayoutGlyphRuntimeBuffer> textGlyphs,
                in TextLayoutSizeRuntime textLayoutSize,
                in TextLayoutAlign textLayoutAlign
            )
            {
                // Left alignment is the default, so we don't need to do anything
                if (textLayoutAlign.Value == TextAlign.Left)
                    return;

                new AlignCalculator(textLayoutSize.Value.x, textLayoutAlign.Value).Align(ref textGlyphs);
            }
        }
    }
}