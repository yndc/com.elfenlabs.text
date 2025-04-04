using Unity.Entities;
using Unity.Rendering;
using Unity.Mathematics;
using Unity.Collections;

namespace Elfenlabs.Text
{
    public struct TextStringConfig : IComponentData
    {
        public FixedString128Bytes Value;
    }

    public struct TextFontConfig : ISharedComponentData
    {
        public int FontIndex;
    }

    [MaterialProperty("_TextureIndex")]
    public struct GlyphTextureIndex : IComponentData
    {
        public int Value;
    }

    [MaterialProperty("_TextureRect")]
    public struct GlyphTextureRect : IComponentData
    {
        public float4 Value;
    }
}
