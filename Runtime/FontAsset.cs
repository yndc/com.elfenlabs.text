using UnityEngine;
using System;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Entities;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Elfenlabs.Text
{
    [CreateAssetMenu(fileName = "MSDF Font", menuName = "MSDF Font")]
    public class FontAsset : ScriptableObject
    {
        [Header("Font")]
        public Font Font;

        [Header("Atlas Settings")]
        public int AtlasSize = 512;
        public int GlyphSize = 32;
        public int Padding = 2;
        public float DistanceMappingRange = 0.5f;
        GlyphRenderFlags GlyphRenderFlags = GlyphRenderFlags.None;
        AtlasCompactFlags CompactFlags = AtlasCompactFlags.None;

        [Header("Character Set")]
        public List<UnicodeRange> UnicodeRanges;
        public List<string> UnicodeSamples;
        public List<string> Ligatures;

        [Header("Result")]
        public Texture2DArray Texture;

        [ReadOnly]
        public List<GlyphRect> GlyphRects;

        public Material Material;

        public FontAssetResource CreateAssetResource(Allocator allocator)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref FontBlobAsset fontBlob = ref builder.ConstructRoot<FontBlobAsset>();
            builder.AllocateString(ref fontBlob.Name, Font.name);

            var map = new NativeHashMap<int, float4>(GlyphRects.Count, Allocator.Temp);
            for (int i = 0; i < GlyphRects.Count; i++)
            {
                var glyph = GlyphRects[i];

                // Convert pixel coordinates to UV coordinates
                var uv = new float4(
                    glyph.X / Texture.width,
                    glyph.Y / Texture.height,
                    glyph.Width / Texture.width,
                    glyph.Height / Texture.height
                );

                map.Add(glyph.CodePoint, uv);
            }

            fontBlob.GlyphRectMap.Serialize(builder, map);
            var blobRef = builder.CreateBlobAssetReference<FontBlobAsset>(allocator);

            builder.Dispose();
            map.Dispose();

            return new FontAssetResource(blobRef, Material);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(FontAsset))]
        public class FontAssetEditor : UnityEditor.Editor
        {
            FontAsset self;
            IntPtr libCtx;

            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();

                self = (FontAsset)target;

                if (self.Font == null)
                {
                    EditorGUILayout.HelpBox("Font is not set.", MessageType.Warning);
                    return;
                }

                self.CompactFlags = (AtlasCompactFlags)EditorGUILayout.EnumFlagsField("Compact Flags", self.CompactFlags);
                self.GlyphRenderFlags = (GlyphRenderFlags)EditorGUILayout.EnumFlagsField("Glyph Render Flags", self.GlyphRenderFlags);

                if (GUILayout.Button("Shape Test"))
                {
                    PrepareLibrary();
                    ShapeTest();
                }

                if (GUILayout.Button("Generate Test"))
                {
                    PrepareLibrary();
                    Generate();
                }

                if (GUILayout.Button("Clear Texture"))
                {
                    PrepareLibrary();
                    ClearTexture();
                }

                CleanupLibrary();
            }

            void PrepareLibrary()
            {
                FontLibrary.CreateContext(
                    FontLibrary.UnityLog,
                    FontLibrary.UnityAllocator,
                    FontLibrary.UnityDisposer,
                    out libCtx);
            }

            void CleanupLibrary()
            {
                if (libCtx != IntPtr.Zero)
                {
                    FontLibrary.DestroyContext(libCtx);
                    libCtx = IntPtr.Zero;
                }
            }

            public void ShapeTest()
            {
                var fontIndex = LoadFont();
                var str = "Hello, world!🥰🥰🥰";
                var buf = NativeBuffer<byte>.FromString(str, Allocator.Temp);
                FontLibrary.ShapeText(
                    libCtx,
                    fontIndex,
                    Allocator.Temp,
                    buf,
                    out var glyphs);
                Debug.Log($"Glyph Count: {glyphs.Count()}");
                for (var i = 0; i < glyphs.Count(); i++)
                {
                    var glyph = glyphs[i];
                    Debug.Log($"Glyph {i}: {glyph.CodePoint} {glyph.XOffset} {glyph.YOffset} {glyph.XAdvance} {glyph.YAdvance}");
                }

                buf.Dispose();
                glyphs.Dispose();
            }

            public void Generate()
            {
                ClearTexture();

                // Prepare character set for generation
                var charsetBuilder = new CharacterSetBuilder();
                for (int i = 0; i < self.UnicodeRanges.Count; i++)
                    charsetBuilder.Add(self.UnicodeRanges[i]);
                for (int i = 0; i < self.UnicodeSamples.Count; i++)
                    charsetBuilder.Add(self.UnicodeSamples[i]);
                for (int i = 0; i < self.Ligatures.Count; i++)
                    charsetBuilder.Add(self.Ligatures[i]);
                var str = charsetBuilder.ToString();
                Debug.Log("String: " + str);

                // Generate atlas texture
                var stringBuffer = NativeBuffer<byte>.FromString(str, Allocator.Temp);
                var texture = self.Texture;
                var fontIndex = LoadFont();
                var textureBuffer = NativeBuffer<Color32>.Alias(texture.GetPixelData<Color32>(0, 0));
                FontLibrary.DrawAtlas(
                    libCtx,
                    fontIndex,
                    texture.width,
                    self.GlyphSize,
                    self.Padding,
                    self.DistanceMappingRange,
                    (int)self.GlyphRenderFlags,
                    (int)self.CompactFlags,
                    Allocator.Temp,
                    in stringBuffer,
                    ref textureBuffer,
                    out NativeBuffer<GlyphRect> glyphsBuffer
                );
                texture.Apply();

                // Generate material
                if (self.Material == null)
                {
                    self.Material = new(Shader.Find("Elfenlabs/Text-MTSDF"))
                    {
                        name = "FontMaterial"
                    };
                    AssetDatabase.AddObjectToAsset(self.Material, self);
                }
                self.Material.SetTexture("_MainTex", texture);

                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets();

                // Serialize the glyph mapping result
                self.GlyphRects = new List<GlyphRect>(glyphsBuffer.Count());
                for (int i = 0; i < glyphsBuffer.Count(); i++)
                {
                    self.GlyphRects.Add(glyphsBuffer[i]);
                }

                // Cleanup buffers
                stringBuffer.Dispose();
                textureBuffer.Dispose();
                glyphsBuffer.Dispose();
            }

            public void ClearTexture()
            {
                if (self.Texture != null)
                {
                    AssetDatabase.RemoveObjectFromAsset(self.Texture);
                    DestroyImmediate(self.Texture);
                }

                var textureArray = new Texture2DArray(self.AtlasSize, self.AtlasSize, 1, TextureFormat.RGBA32, false);
                var rawColors = textureArray.GetPixelData<Color32>(0, 0);
                for (var i = 0; i < rawColors.Length; i++)
                {
                    rawColors[i] = new Color32(0, 0, 0, 0);
                }
                textureArray.name = "FontAtlas";
                textureArray.Apply();

                self.Texture = textureArray;
                EditorUtility.SetDirty(target);
                AssetDatabase.AddObjectToAsset(textureArray, self);
                AssetDatabase.SaveAssets();
            }

            int LoadFont()
            {
                var font = self.Font;
                var fontName = font.name;
                var fontPath = AssetDatabase.GetAssetPath(font);
                var fontData = System.IO.File.ReadAllBytes(fontPath);
                var fontBuf = NativeBuffer<byte>.FromBytes(fontData, Allocator.Temp);

                Debug.Log($"Font Name: {fontName}");
                Debug.Log($"Font Path: {fontPath}");
                Debug.Log($"Font Data: {fontData.Length} bytes");

                FontLibrary.LoadFont(libCtx, fontBuf, out var fontIndex);
                Debug.Log($"Font Index: {fontIndex}");

                fontBuf.Dispose();

                return fontIndex;
            }
        }
#endif
    }

    public struct SerializedHashMap<K, T>
        where T : unmanaged
        where K : unmanaged, IEquatable<K>
    {
        public BlobArray<K> Keys;
        public BlobArray<T> Values;
        public void Serialize(BlobBuilder builder, NativeHashMap<K, T> map)
        {
            BlobBuilderArray<K> keys = builder.Allocate(ref Keys, map.Count);
            BlobBuilderArray<T> values = builder.Allocate(ref Values, map.Count);

            int i = 0;
            foreach (var kvp in map)
            {
                keys[i] = kvp.Key;
                values[i] = kvp.Value;
                i++;
            }
        }

        public NativeHashMap<K, T> Deserialize(Allocator allocator)
        {
            var map = new NativeHashMap<K, T>(Keys.Length, allocator);
            for (int i = 0; i < Keys.Length; i++)
            {
                map.Add(Keys[i], Values[i]);
            }
            return map;
        }
    }

    public struct FontBlobAsset
    {
        public BlobString Name;
        public SerializedHashMap<int, float4> GlyphRectMap;
    }
}