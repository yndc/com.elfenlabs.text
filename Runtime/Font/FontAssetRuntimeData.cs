using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace Elfenlabs.Text
{
    public struct FontAssetRuntimeData : ICleanupSharedComponentData, IEquatable<FontAssetRuntimeData>
    {
        public FontDescription Description;
        public Entity PrototypeEntity;

        [NativeDisableContainerSafetyRestriction]
        public UnsafeHashMap<int, GlyphRuntimeData> GlyphRectMap;

        public readonly bool Equals(FontAssetRuntimeData other)
        {
            return GlyphRectMap.Equals(other.GlyphRectMap);
        }

        public override readonly int GetHashCode()
        {
            return GlyphRectMap.GetHashCode();
        }
    }
}