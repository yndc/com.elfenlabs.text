using Unity.Entities;
using Unity.Mathematics;

namespace Elfenlabs.Text
{
    public enum BreakRule
    {
        None = 0,
        Word = 1,
        Character = 2,
    }

    public enum TextAlign
    {
        Left = 0,
        Center = 1,
        Right = 2,
        Justify = 3,
    }

    /// <summary>
    /// Maximum text layout size in world space, 0 means no limit
    /// </summary>
    public struct TextLayoutMaxSize : IComponentData
    {
        public float2 Value;
    }

    /// <summary>
    /// Minimal rect that contains the text layout in world space
    /// </summary>
    public struct TextLayoutSizeRuntime : IComponentData
    {
        public float2 Value;
    }

    /// <summary>
    /// Text layout break rule, refer to BreakRule enum
    /// </summary>
    public struct TextLayoutBreakRule : IComponentData
    {
        public BreakRule Value;
    }

    /// <summary>
    /// Text alignment in the text layout
    /// </summary>
    public struct TextLayoutAlign : IComponentData
    {
        public TextAlign Value;
    }

    public struct TextLayoutRequireUpdate : IComponentData, IEnableableComponent { }
}