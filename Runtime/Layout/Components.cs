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

    /// <summary>
    /// Maximum text layout size in world space, 0 means no limit
    /// </summary>
    public struct TextLayoutMaxSize : IComponentData
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

    public struct TextLayoutGlyphRuntimeBuffer : IBufferElementData
    {
        public Entity Entity;
        public float2 PositionEm;
        public float2 AdvanceEm;
        public float2 OffsetEm;
        public float2 RealSizeEm;
        public float2 QuadSizeEm;
        public int Cluster;
    }

    public struct TextLayoutRequireUpdate : IComponentData, IEnableableComponent
    {

    }
}