using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Entities;
using UnityEngine;
using Elfenlabs.Collections;



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