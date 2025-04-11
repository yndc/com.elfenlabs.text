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
        public int AtlasSize = 512;
        public int GlyphSize = 32;
        public int Padding = 4;
        public int Margin = 2;
        public float DistanceMappingRange = 0.5f;
        public GlyphRenderFlags GlyphRenderFlags = GlyphRenderFlags.None;
        public AtlasCompactFlags CompactFlags = AtlasCompactFlags.None;

        [Header("Character Set")]
        public List<UnicodeRange> UnicodeRanges;
        public List<string> UnicodeSamples;
        public List<string> Ligatures;

        [Header("Result")]
        public Texture2DArray Texture;

        [ReadOnly]
        public List<GlyphMetrics> Glyphs;

        public Material Material;

        public FontAssetData CreateAssetData(Allocator allocator)
        {
            var glyphMapBuilder = new BlobBuilder(Allocator.Temp);
            var fontBytesBuilder = new BlobBuilder(Allocator.Temp);
            ref BlobFlattenedHashMap<int, GlyphRuntimeData> glyphRectMap = ref glyphMapBuilder.ConstructRoot<BlobFlattenedHashMap<int, GlyphRuntimeData>>();
            ref BlobArray<byte> fontBytes = ref fontBytesBuilder.ConstructRoot<BlobArray<byte>>();

            var map = new UnsafeHashMap<int, GlyphRuntimeData>(Glyphs.Count, Allocator.Temp);
            for (int i = 0; i < Glyphs.Count; i++)
            {
                var glyph = Glyphs[i];
                map.Add(glyph.CodePoint, new GlyphRuntimeData(glyph, GlyphSize, AtlasSize));
            }

            glyphRectMap.Flatten(glyphMapBuilder, map);

            var fontPath = AssetDatabase.GetAssetPath(Font);
            var fontData = System.IO.File.ReadAllBytes(fontPath);
            var fontBytesBuffer = fontBytesBuilder.Allocate(ref fontBytes, fontData.Length);
            unsafe { Elfenlabs.Unsafe.UnsafeUtility.CopyArrayToPtr(fontData, fontBytesBuffer.GetUnsafePtr(), fontData.Length); }

            var assetData = new FontAssetData
            {
                FlattenedGlyphMap = glyphMapBuilder.CreateBlobAssetReference<BlobFlattenedHashMap<int, GlyphRuntimeData>>(allocator),
                FontBytes = fontBytesBuilder.CreateBlobAssetReference<BlobArray<byte>>(allocator),
                Material = Material,
                Padding = Padding,
                AtlasSize = AtlasSize,
                GlyphSize = GlyphSize,
            };

            glyphMapBuilder.Dispose();
            fontBytesBuilder.Dispose();
            map.Dispose();

            return assetData;
        }
    }
}