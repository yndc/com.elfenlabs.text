using Elfenlabs.Collections;
using Elfenlabs.Texture;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Elfenlabs.Text
{
    [CreateAssetMenu(fileName = "MSDF Font", menuName = "MSDF Font")]
    public class FontAssetAuthoring : ScriptableObject
    {
        [Header("Font")]
        public Font Font;

        [Header("Atlas Settings")]
        public AtlasConfig AtlasConfig;
        public RenderConfig RenderConfig;

        [Header("Character Set")]
        public List<UnicodeRange> UnicodeRanges;
        public List<string> UnicodeSamples;
        public List<string> Ligatures;

        [Header("Result")]
        public Texture2DArray TextureArray;

        [ReadOnly]
        public List<GlyphMetrics> Glyphs;

        [ReadOnly]
        [HideInInspector]
        public BlobAssetReference<AtlasPacker<GlyphMetrics>.BlobSerialized> AtlasState;

        public Material Material;

        public BlobAssetReference<FontAssetData> CreateAssetReference(Allocator allocator)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<FontAssetData>();

            // root.FlattenedGlyphMap
            var map = new UnsafeHashMap<int, GlyphRuntimeData>(Glyphs.Count, Allocator.Temp);
            for (int i = 0; i < Glyphs.Count; i++)
            {
                var glyph = Glyphs[i];
                map.Add(glyph.CodePoint, new GlyphRuntimeData(glyph, AtlasConfig.Size));
            }
            root.FlattenedGlyphMap.Flatten(builder, map);

            // root.FontBytes
            var fontPath = AssetDatabase.GetAssetPath(Font);
            var fontData = System.IO.File.ReadAllBytes(fontPath);
            var fontBytesBuffer = builder.Allocate(ref root.FontBytes, fontData.Length);
            unsafe { Elfenlabs.Unsafe.UnsafeUtility.CopyArrayToPtr(fontData, fontBytesBuffer.GetUnsafePtr(), fontData.Length); }

            // root.SerializedAtlasPacker
            var unpacked = AtlasState.Value.Deserialize(Allocator.Temp);
            root.SerializedAtlasPacker.Serialize(builder, unpacked);
            unpacked.Dispose();

            root.AtlasConfig = AtlasConfig;
            root.RenderConfig = RenderConfig;
            root.Material = Material;

            var reference = builder.CreateBlobAssetReference<FontAssetData>(Allocator.Persistent);

            builder.Dispose();
            map.Dispose();

            return reference;
        }
    }
}