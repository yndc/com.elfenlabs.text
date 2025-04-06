using Unity.Entities;
using Unity.Rendering;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;
using Elfenlabs.Registry;
using System;
using Elfenlabs.Mesh;

namespace Elfenlabs.Text
{
    public struct FontAssetResource : ISharedComponentData, IEquatable<FontAssetResource>
    {
        public BlobAssetReference<FontBlobAsset> BlobAsset;
        public Material Material;
        public NativeHashMap<int, float4> GlyphRectMap;

        public FontAssetResource(BlobAssetReference<FontBlobAsset> blobAsset, Material material)
        {
            BlobAsset = blobAsset;
            Material = material;
            GlyphRectMap = default;
        }

        public void Initialize(EntityManager entityManager)
        {
            GlyphRectMap = BlobAsset.Value.GlyphRectMap.Deserialize(Allocator.Persistent);
        }

        public void Dispose()
        {
            GlyphRectMap.Dispose();
            BlobAsset.Dispose();
        }

        public bool Equals(FontAssetResource other)
        {
            return BlobAsset.Equals(other.BlobAsset) && Material == other.Material;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(BlobAsset.GetHashCode(), Material.GetHashCode());
        }
    }

    public struct TextStringConfig : IComponentData
    {
        public FixedString128Bytes Value;
    }

    public struct TextFontConfig : ISharedComponentData
    {
        public int FontIndex;
    }

    public struct TextStringState : IComponentData
    {
        public FixedString128Bytes Value;
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
