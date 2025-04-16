using Unity.Entities;
using Unity.Rendering;
using Unity.Mathematics;
using Unity.Collections;

namespace Elfenlabs.Text
{
    public struct TextStringBuffer : IBufferElementData
    {
        public byte Value;
        public static implicit operator TextStringBuffer(byte value) => new TextStringBuffer { Value = value };
        public static implicit operator byte(TextStringBuffer textBuffer) => textBuffer.Value;
    }

    public struct TextGlyphBuffer : IBufferElementData
    {
        public Entity Entity;
        public float2 PositionEm;
        public float2 AdvanceEm;
        public float2 OffsetEm;
        public float2 RealSizeEm;
        public float2 QuadSizeEm;
        public int Cluster;
        public int Line;
    }

    public struct TextFontSize : IComponentData
    {
        public float Value;
    }

    public struct TextGlyphRequireUpdate : IComponentData, IEnableableComponent { }

    [MaterialProperty("_GlyphAtlasIndex")]
    public struct MaterialPropertyGlyphAtlasIndex : IComponentData
    {
        public int Value;
    }

    [MaterialProperty("_GlyphRect")]
    public struct MaterialPropertyGlyphRect : IComponentData
    {
        public float4 Value;
    }

    [MaterialProperty("_GlyphBaseColor")]
    public struct MaterialPropertyGlyphBaseColor : IComponentData
    {
        public float4 Value;
    }

    [MaterialProperty("_GlyphOutlineThickness")]
    public struct MaterialPropertyGlyphOutlineThickness : IComponentData
    {
        public float Value;
    }

    [MaterialProperty("_GlyphOutlineColor")]
    public struct MaterialPropertyGlyphOutlineColor : IComponentData
    {
        public float4 Value;
    }
}
