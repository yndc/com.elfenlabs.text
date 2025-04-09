using System;
using Elfenlabs.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Elfenlabs.Text
{
    public struct FontAssetData : ISharedComponentData, IEquatable<FontAssetData>
    {
        public BlobAssetReference<BlobFlattenedHashMap<int, float4>> FlattenedGlyphRectMap;
        public BlobAssetReference<BlobArray<byte>> FontBytes;
        public UnityObjectRef<Material> Material;
        public int Padding;

        public bool Equals(FontAssetData other)
        {
            return FlattenedGlyphRectMap.Equals(other.FlattenedGlyphRectMap)
                && Material.Equals(other.Material)
                && Padding == other.Padding;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(
                FlattenedGlyphRectMap.GetHashCode(),
                Material.GetHashCode(),
                Padding);
        }
    }
}