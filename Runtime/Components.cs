using Unity.Entities;
using Unity.Rendering;
using Unity.Mathematics;
using Unity.Collections;

namespace Elfenlabs.Text
{
    public struct TextBufferData : IBufferElementData
    {
        public byte Value;
        public static implicit operator TextBufferData(byte value) => new TextBufferData { Value = value };
        public static implicit operator byte(TextBufferData textBuffer) => textBuffer.Value;
    }

    public struct TextShapedTag : IComponentData
    {

    }

    public struct TextFontWorldSize : IComponentData
    {
        public float Value;
    }

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
