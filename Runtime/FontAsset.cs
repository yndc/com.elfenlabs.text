using UnityEngine;
using Elfenlabs.Text;
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

                var str = "Hello, World!😊😊😊";
                FontLibrary.ShapeText(handle, fontIndex, str, str.Length, out var glyphs, out var glyphCount);

                Debug.Log($"Glyph Count: {glyphCount}");
                for (var i = 0; i < glyphCount; i++)
                {
                    unsafe
                    {
                        var glyph = ((Glyph*)glyphs)[i];
                        Debug.Log($"Glyph {i}: {glyph.CodePoint} {glyph.XOffset} {glyph.YOffset} {glyph.XAdvance} {glyph.YAdvance}");
                    }
                }

                FontLibrary.DestroyContext(handle);
            }
        }
#endif
    }
}