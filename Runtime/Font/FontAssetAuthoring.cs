using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Entities;
using Elfenlabs.Collections;
using UnityEditor;
using Elfenlabs.Unsafe;
using Unity.Collections.LowLevel.Unsafe;

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
        public int Padding = 2;
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
        public List<GlyphRect> GlyphRects;

        public Material Material;

        public FontAssetData CreateAssetData(Allocator allocator)
        {
            var glyphMapBuilder = new BlobBuilder(Allocator.Temp);
            var fontBytesBuilder = new BlobBuilder(Allocator.Temp);
            ref BlobFlattenedHashMap<int, float4> glyphRectMap = ref glyphMapBuilder.ConstructRoot<BlobFlattenedHashMap<int, float4>>();
            ref BlobArray<byte> fontBytes = ref fontBytesBuilder.ConstructRoot<BlobArray<byte>>();

            var map = new UnsafeHashMap<int, float4>(GlyphRects.Count, Allocator.Temp);
            for (int i = 0; i < GlyphRects.Count; i++)
            {
                var glyph = GlyphRects[i];

                Debug.Log($"Glyph: {glyph.CodePoint}, X: {glyph.X}, Y: {glyph.Y}, Width: {glyph.Width}, Height: {glyph.Height}");
                // Convert pixel coordinates to UV coordinates
                var uv = new float4(
                    (float)glyph.X / Texture.width,
                    (float)glyph.Y / Texture.height,
                    (float)glyph.Width / Texture.width,
                    (float)glyph.Height / Texture.height
                );

                Debug.Log($"Glyph: {glyph.CodePoint}, UV: {uv}");

                map.Add(glyph.CodePoint, uv);
            }

            glyphRectMap.Flatten(glyphMapBuilder, map);

            var fontPath = AssetDatabase.GetAssetPath(Font);
            var fontData = System.IO.File.ReadAllBytes(fontPath);
            var fontBytesBuffer = fontBytesBuilder.Allocate(ref fontBytes, fontData.Length);
            unsafe { Elfenlabs.Unsafe.UnsafeUtility.CopyArrayToPtr(fontData, fontBytesBuffer.GetUnsafePtr(), fontData.Length); }

            var assetData = new FontAssetData
            {
                FlattenedGlyphRectMap = glyphMapBuilder.CreateBlobAssetReference<BlobFlattenedHashMap<int, float4>>(allocator),
                FontBytes = fontBytesBuilder.CreateBlobAssetReference<BlobArray<byte>>(allocator),
                Material = Material,
                Padding = Padding
            };

            glyphMapBuilder.Dispose();
            fontBytesBuilder.Dispose();
            map.Dispose();

            return assetData;
        }
    }
}