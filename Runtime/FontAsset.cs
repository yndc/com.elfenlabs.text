using UnityEngine;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Text;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Elfenlabs.Text.Editor
{
    [CreateAssetMenu(fileName = "MSDF Font", menuName = "MSDF Font")]
    public class FontAsset : ScriptableObject
    {
        public Font Font;

        public Texture2D Texture;

        public CharacterPreset BakeCharacterPresets;

        public int GlyphSize = 32;

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
                FontLibrary.CreateContext(FontLibrary.UnityLog, FontLibrary.UnityAllocator, out libCtx);
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
                var charsetBuilder = new CharacterSetBuilder().WithPreset(CharacterPreset.Latin);
                var str = charsetBuilder.ToString();
                var strBytes = Encoding.UTF8.GetBytes(str);
                Debug.Log("String: " + str);

                // Generate the atlas 
                var texture = self.Texture;
                var fontIndex = LoadFont();
                var rawTexPtr = texture.GetRawTextureData<Color32>();
                unsafe
                {
                    int pixelSize = 4; // Each Color32 is 4 bytes (R, G, B, A)
                    int rowStride = texture.width * pixelSize; // 512 * 4 = 2048 bytes per row
                    fixed (byte* strPtr = strBytes)
                    {
                        FontLibrary.DrawAtlas(
                            libCtx,
                            fontIndex,
                            (IntPtr)strPtr,
                            strBytes.Length,
                            texture.width,
                            self.GlyphSize,
                            (IntPtr)rawTexPtr.GetUnsafePtr());
                    }
                }
                texture.Apply();
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets();

                // Get glyph set from text shaping results
                // var fontIndex = LoadFont();
                // var buf = new NativeArray<Glyph>(strBytes.Length * 2, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                // NativeHashSet<int> glyphSet;
                // unsafe
                // {
                //     fixed (byte* ptr = strBytes)
                //     {
                //         FontLibrary.ShapeText(libCtx, fontIndex, (IntPtr)ptr, strBytes.Length, 1024, (IntPtr)buf.GetUnsafePtr(), out var glyphCount);
                //         Debug.Log($"Glyph Count: {glyphCount}");

                //         glyphSet = new NativeHashSet<int>(glyphCount, Allocator.Temp);
                //         for (var i = 0; i < glyphCount; i++)
                //         {
                //             var glyph = buf[i];
                //             glyphSet.Add(glyph.CodePoint);
                //         }
                //     }
                // }

                // glyphSet.Dispose();
                // buf.Dispose();
            }

            public void ClearTexture()
            {
                if (self.Texture != null)
                {
                    AssetDatabase.RemoveObjectFromAsset(self.Texture);
                    DestroyImmediate(self.Texture);
                }

                var texture = new Texture2D(512, 512, TextureFormat.RGBA32, false);
                var rawColors = texture.GetRawTextureData<Color32>();
                for (var i = 0; i < rawColors.Length; i++)
                {
                    rawColors[i] = new Color32(0, 0, 0, 0);
                }
                texture.name = "FontAtlas";
                texture.Apply();

                self.Texture = texture;
                EditorUtility.SetDirty(target);
                AssetDatabase.AddObjectToAsset(texture, self);
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