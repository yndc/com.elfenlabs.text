using UnityEngine;
using Unity.Entities;
using Elfenlabs.String;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;

namespace Elfenlabs.Text
{
    public class TextMeshAuthoring : MonoBehaviour
    {
        public FontAssetAuthoring Font;

        public string Text;

        public float FontSize = 1f;

        public float LineHeight = 1f;

        public float MaxWidth = 0f;

        public BreakRule BreakRule = BreakRule.Word;

        public TextAlign Align = TextAlign.Left;
    }

    public class TextMeshAuthoringBaker : Baker<TextMeshAuthoring>
    {
        public override void Bake(TextMeshAuthoring authoring)
        {
            DependsOn(authoring.Font);

            var entity = GetEntity(TransformUsageFlags.Dynamic);
            var buffer = AddBuffer<TextStringBuffer>(entity);
            StringUtility.CopyToDynamicBuffer(authoring.Text, buffer);
            AddBuffer<TextGlyphBuffer>(entity);
            AddComponent(entity, new Parent());
            AddComponent(entity, new TextFontSize { Value = authoring.FontSize });
            AddComponent(entity, new TextLayoutSizeRuntime());
            AddComponent(entity, new TextLayoutMaxSize { Value = new float2(authoring.MaxWidth, 0f) });
            AddComponent(entity, new TextLayoutBreakRule { Value = BreakRule.Word });
            AddComponent(entity, new TextLayoutAlign { Value = authoring.Align });
            AddComponent(entity, new TextLayoutRequireUpdate());
            AddComponent(entity, new TextGlyphRequireUpdate());
            SetComponentEnabled<TextLayoutRequireUpdate>(entity, true);
            SetComponentEnabled<TextGlyphRequireUpdate>(entity, true);

            var fontAssetHash = new Unity.Entities.Hash128((uint)authoring.Font.GetHashCode(), 0, 0, 0);
            if (!TryGetBlobAssetReference<FontAssetData>(fontAssetHash, out var fontAssetData))
            {
                fontAssetData = authoring.Font.CreateAssetReference(Allocator.Persistent);
                AddBlobAssetWithCustomHash(ref fontAssetData, fontAssetHash);
            }

            AddSharedComponent(entity, new FontAssetReference { Value = fontAssetData });
        }
    }
}
