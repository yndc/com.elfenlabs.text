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

        public int AtlasSize = 512;

        public int Padding = 2;

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
                var charsetBuilder = new CharacterSetBuilder().WithPreset(CharacterPreset.Latin);
                var str = charsetBuilder.ToString();
                var stringBuffer = NativeBuffer<byte>.FromString(str, Allocator.Temp);
                Debug.Log("String: " + str);

                // Generate the atlas 
                var texture = self.Texture;
                var fontIndex = LoadFont();
                var textureBuffer = NativeBuffer<Color32>.FromArray(texture.GetRawTextureData<Color32>());
                NativeBuffer<Glyph> glyphsBuffer;
                FontLibrary.DrawAtlas(
                    libCtx,
                    fontIndex,
                    texture.width,
                    self.GlyphSize,
                    self.Padding,
                    Allocator.Temp,
                    in stringBuffer,
                    ref textureBuffer,
                    out glyphsBuffer
                );
                texture.Apply();
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets();

                for (int i = 0; i < glyphsBuffer.Count(); i++)
                {
                    var glyph = glyphsBuffer[i];
                    Debug.Log($"Glyph {i}: {glyph.CodePoint} {glyph.XOffset} {glyph.YOffset} {glyph.XAdvance} {glyph.YAdvance}");
                }

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

                var texture = new Texture2D(self.AtlasSize, self.AtlasSize, TextureFormat.RGBA32, false);
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