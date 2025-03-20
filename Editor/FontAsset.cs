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

                FontLibrary.Initialize();
                var fontIndex = FontLibrary.LoadFont(fontData, fontData.Length);
                Debug.Log($"Font Index: {fontIndex}");
                FontLibrary.Shutdown();
            }
        }
#endif
    }
}