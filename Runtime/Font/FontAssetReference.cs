using System;
using Elfenlabs.Collections;
using Elfenlabs.Texture;
using Unity.Entities;
using UnityEngine;

namespace Elfenlabs.Text
{
    public struct FontAssetReference : ISharedComponentData, IEquatable<FontAssetReference>
    {
        public BlobAssetReference<FontAssetData> Value;

        public bool Equals(FontAssetReference other)
        {
            return Value.Equals(other.Value);
        }

        public override readonly int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }

    public struct FontAssetData
    {
        public BlobFlattenedHashMap<int, GlyphRuntimeData> FlattenedGlyphMap;
        public BlobArray<byte> FontBytes;
        public AtlasPacker<GlyphMetrics>.BlobSerialized SerializedAtlasPacker;
        public AtlasConfig AtlasConfig;
        public RenderConfig RenderConfig;
        public UnityObjectRef<Material> Material;
    }
}