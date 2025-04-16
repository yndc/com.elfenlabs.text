using System;
using Elfenlabs.Collections;
using Unity.Entities;
using UnityEngine;

namespace Elfenlabs.Text
{
    public struct FontAssetData : ISharedComponentData, IEquatable<FontAssetData>
    {
        public BlobAssetReference<BlobFlattenedHashMap<int, GlyphRuntimeData>> FlattenedGlyphMap;
        public BlobAssetReference<BlobArray<byte>> FontBytes;
        public UnityObjectRef<Material> Material;
        public AtlasConfig AtlasConfig;

        public bool Equals(FontAssetData other)
        {
            return FlattenedGlyphMap.Equals(other.FlattenedGlyphMap)
                && Material.Equals(other.Material)
                && AtlasConfig.Equals(other.AtlasConfig);
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(
                FlattenedGlyphMap.GetHashCode(),
                Material.GetHashCode(),
                AtlasConfig);
        }
    }
}