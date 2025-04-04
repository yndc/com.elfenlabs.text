using UnityEngine;
using System;
using Unity.Collections;
using System.Collections.Generic;

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
        public List<Glyph> GlyphRects;

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

                // Generate the atlas 
                var stringBuffer = NativeBuffer<byte>.FromString(str, Allocator.Temp);
                var texture = self.Texture;
                var fontIndex = LoadFont();
                var textureBuffer = NativeBuffer<Color32>.FromArray(texture.GetPixelData<Color32>(0, 0));
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
                    out NativeBuffer<Glyph> glyphsBuffer
                );
                texture.Apply();
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets();

                // Serialize the glyph mapping result
                self.GlyphRects = new List<Glyph>(glyphsBuffer.Count());
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
}