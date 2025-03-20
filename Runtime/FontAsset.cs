using UnityEngine;
using Elfenlabs.Text;
using System;
using System.Runtime.InteropServices;
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

#if UNITY_EDITOR
        [CustomEditor(typeof(FontAsset))]
        public class FontAssetEditor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();

                FontAsset myTarget = (FontAsset)target;

                if (GUILayout.Button("Debug"))
                {
                    Generate();
                }
            }

            public void Generate()
            {
                var component = (FontAsset)target;
                var font = component.Font;
                var fontName = font.name;
                var fontPath = AssetDatabase.GetAssetPath(font);
                var fontData = System.IO.File.ReadAllBytes(fontPath);

                Debug.Log($"Font Name: {fontName}");
                Debug.Log($"Font Path: {fontPath}");
                Debug.Log($"Font Data: {fontData.Length} bytes");

                FontLibrary.CreateContext(out var handle);
                FontLibrary.LoadFont(handle, out var fontIndex, fontData, fontData.Length);
                Debug.Log($"Font Index: {fontIndex}");

                var str = "Hello, world!🥰🥰🥰";
                unsafe
                {
                    var buf = new NativeArray<Glyph>(1024, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                    var textBytes = Encoding.UTF8.GetBytes(str);
                    fixed (byte* ptr = textBytes)
                    {
                        FontLibrary.ShapeText(handle, fontIndex, (IntPtr)ptr, textBytes.Length, 1024, (IntPtr)buf.GetUnsafePtr(), out var glyphCount);
                        Debug.Log($"Glyph Count: {glyphCount}");
                        for (var i = 0; i < glyphCount; i++)
                        {
                            var glyph = buf[i];
                            Debug.Log($"Glyph {i}: {glyph.CodePoint} {glyph.XOffset} {glyph.YOffset} {glyph.XAdvance} {glyph.YAdvance}");
                        }
                    }
                    buf.Dispose();
                }

                FontLibrary.DestroyContext(handle);
            }
        }
#endif
    }
}