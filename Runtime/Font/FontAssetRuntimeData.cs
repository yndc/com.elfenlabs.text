using System;
using Elfenlabs.Texture;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine.Rendering;

namespace Elfenlabs.Text
{
    public struct FontAssetRuntimeData : ICleanupSharedComponentData, IEquatable<FontAssetRuntimeData>
    {
        public BlobAssetReference<FontAssetData> AssetReference;
        public FontDescription Description;
        public Entity PrototypeEntity;

        [NativeDisableContainerSafetyRestriction]
        public UnsafeParallelHashMap<int, GlyphRuntimeData> GlyphMap;

        [NativeDisableContainerSafetyRestriction]
        public UnsafeParallelHashSet<int> MissingGlyphSet;

        public AtlasPacker<GlyphMetrics> Atlas;
        public BatchMaterialID MaterialID;

        public readonly bool Equals(FontAssetRuntimeData other)
        {
            return AssetReference.Equals(other.AssetReference);
        }

        public override readonly int GetHashCode()
        {
            return AssetReference.GetHashCode();
        }
    }
}