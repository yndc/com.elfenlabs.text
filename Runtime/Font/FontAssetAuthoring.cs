using Elfenlabs.Collections;
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
        public List<byte> AtlasState;

        public Material Material;

        public FontAssetReference CreateAssetReference(Allocator allocator)
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

            // root.SerializedAtlasState
            var atlasStateBuffer = builder.Allocate(ref root.SerializedAtlasState, AtlasState.Count);
            unsafe { Elfenlabs.Unsafe.UnsafeUtility.CopyArrayToPtr(AtlasState.ToArray(), atlasStateBuffer.GetUnsafePtr(), AtlasState.Count); }

            root.AtlasConfig = AtlasConfig;
            root.Material = Material;

            var reference = builder.CreateBlobAssetReference<FontAssetData>(Allocator.Persistent);

            builder.Dispose();
            map.Dispose();

            return new FontAssetReference { Value = reference };
        }
    }
}