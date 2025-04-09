using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace Elfenlabs.Text
{
    public struct FontAssetRuntimeData : ICleanupSharedComponentData, IEquatable<FontAssetRuntimeData>
    {
        public int Index;

        public Entity PrototypeEntity;
        
        [NativeDisableContainerSafetyRestriction]
        public UnsafeHashMap<int, float4> GlyphRectMap;

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