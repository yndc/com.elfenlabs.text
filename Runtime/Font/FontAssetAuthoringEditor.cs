using Elfenlabs.Collections;
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Elfenlabs.Texture;



#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Elfenlabs.Text
{
#if UNITY_EDITOR
    [CustomEditor(typeof(FontAssetAuthoring))]
    public class FontAssetEditor : UnityEditor.Editor
    {
        FontAssetAuthoring self;
        IntPtr libCtx;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            self = (FontAssetAuthoring)target;

            if (self.Font == null)
            {
                EditorGUILayout.HelpBox("Font is not set.", MessageType.Warning);
                return;
            }

            // self.CompactFlags = (AtlasCompactFlags)EditorGUILayout.EnumFlagsField("Compact Flags", self.CompactFlags);
            self.RenderConfig.Flags = (GlyphRenderFlags)EditorGUILayout.EnumFlagsField("Glyph Render Flags", self.RenderConfig.Flags);

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
            var fontDescription = LoadFont();
            var str = "Hello, world!🥰🥰🥰";
            var buf = NativeBuffer<byte>.FromString(str, Allocator.Temp);
            FontLibrary.ShapeText(
                libCtx,
                fontDescription.Handle,
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

            // Generate atlas texture
            var textureArray = self.TextureArray;
            var fontDescription = LoadFont();
            var textureBuffer = NativeBuffer<Color32>.Alias(textureArray.GetPixelData<Color32>(0, 0));

            // Prepare character set for generation
            var glyphs = PrepareGlyphBuffer(Allocator.Temp);

            var atlas = new AtlasPacker<GlyphMetrics>(
                new AtlasPacker<GlyphMetrics>.Config
                {
                    Size = self.AtlasConfig.Size,
                    Margin = self.AtlasConfig.Margin
                }, Allocator.Persistent);

            FontLibrary.GetGlyphMetrics(
                libCtx,
                fontDescription.Handle,
                self.AtlasConfig.GlyphSize,
                self.AtlasConfig.Padding,
                ref glyphs);

            var packedCount = atlas.PackGlyphs(glyphs.AsNativeArray());

            var remaining = glyphs.Count() - packedCount;
            if (remaining > 0)
            {
                Debug.LogWarning($"Packed {packedCount} glyphs, {remaining} remaining.");
            }

            self.AtlasBlobBytes = BlobUtility.ToBytes<AtlasPacker<GlyphMetrics>, AtlasPacker<GlyphMetrics>.Blob>(in atlas);

            FontLibrary.RenderGlyphsToAtlas(
                libCtx,
                fontDescription.Handle,
                self.AtlasConfig,
                self.RenderConfig,
                in glyphs,
                ref textureBuffer);

            textureArray.Apply();

            // Generate material
            if (self.Material == null)
            {
                self.Material = new(Shader.Find("Elfenlabs/Text-MTSDF"))
                {
                    name = "FontMaterial"
                };
                AssetDatabase.AddObjectToAsset(self.Material, self);
            }
            self.Material.SetTexture("_MainTex", textureArray);

            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();

            // Serialize the glyph mapping result
            self.Glyphs = new List<GlyphMetrics>(glyphs.Count());
            for (int i = 0; i < glyphs.Count(); i++)
            {
                self.Glyphs.Add(glyphs[i]);
            }

            // Cleanup buffers
            glyphs.Dispose();
        }

        public void ClearTexture()
        {
            if (self.TextureArray != null)
            {
                AssetDatabase.RemoveObjectFromAsset(self.TextureArray);
                DestroyImmediate(self.TextureArray);
            }

            if (self.AtlasBlobBytes.Length > 0)
            {
                self.AtlasBlobBytes = new byte[0];
            }

            var textureArray = new Texture2DArray(self.AtlasConfig.Size, self.AtlasConfig.Size, 1, TextureFormat.RGBA32, false);
            var rawColors = textureArray.GetPixelData<Color32>(0, 0);
            for (var i = 0; i < rawColors.Length; i++)
            {
                rawColors[i] = new Color32(0, 0, 0, 0);
            }
            textureArray.name = "FontAtlas";
            textureArray.Apply();

            self.TextureArray = textureArray;
            EditorUtility.SetDirty(target);
            AssetDatabase.AddObjectToAsset(textureArray, self);
            AssetDatabase.SaveAssets();
        }

        NativeBuffer<GlyphMetrics> PrepareGlyphBuffer(Allocator allocator)
        {
            var fontDescription = LoadFont();

            // Prepare character set for generation
            var charsetBuilder = new CharacterSetBuilder();
            for (int i = 0; i < self.UnicodeRanges.Count; i++)
                charsetBuilder.Add(self.UnicodeRanges[i]);
            for (int i = 0; i < self.UnicodeSamples.Count; i++)
                charsetBuilder.Add(self.UnicodeSamples[i]);
            for (int i = 0; i < self.Ligatures.Count; i++)
                charsetBuilder.Add(self.Ligatures[i]);
            var str = charsetBuilder.ToString();
            var strBuffer = NativeBuffer<byte>.FromString(str, Allocator.Temp);

            FontLibrary.ShapeText(libCtx, fontDescription.Handle, Allocator.Temp, strBuffer, out var glyphShapeResult);

            var glyphSet = new NativeHashSet<int>(glyphShapeResult.Count(), Allocator.Temp);

            for (int i = 0; i < glyphShapeResult.Count(); i++)
            {
                glyphSet.Add(glyphShapeResult[i].CodePoint);
            }

            var index = 0;
            var glyphs = new NativeBuffer<GlyphMetrics>(glyphSet.Count, allocator);
            foreach (var glyphCodePoint in glyphSet)
            {
                glyphs[index] = new GlyphMetrics { CodePoint = glyphCodePoint };
                index++;
            }

            strBuffer.Dispose();
            glyphShapeResult.Dispose();
            glyphSet.Dispose();

            return glyphs;
        }
        FontDescription LoadFont()
        {
            var font = self.Font;
            var fontName = font.name;
            var fontPath = AssetDatabase.GetAssetPath(font);
            var fontData = System.IO.File.ReadAllBytes(fontPath);
            var fontBuf = NativeBuffer<byte>.FromBytes(fontData, Allocator.Temp);

            Debug.Log($"Font Name: {fontName}");
            Debug.Log($"Font Path: {fontPath}");
            Debug.Log($"Font Data: {fontData.Length} bytes");

            FontLibrary.LoadFont(libCtx, fontBuf, out var fontDescription);

            fontBuf.Dispose();

            return fontDescription;
        }
    }
#endif
}