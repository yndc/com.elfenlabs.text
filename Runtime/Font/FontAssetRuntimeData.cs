using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

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
            return PrototypeEntity.Equals(other.PrototypeEntity);
        }

        public override readonly int GetHashCode()
        {
            return PrototypeEntity.GetHashCode();
        }
    }
}