using UnityEngine;
using Unity.Entities;
using Elfenlabs.String;
using System;
using Unity.Transforms;
using Unity.Mathematics;

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

    public struct FontAssetBakeRequest : ISharedComponentData, IEquatable<FontAssetBakeRequest>
    {
        public FontAssetAuthoring Value;

        public readonly bool Equals(FontAssetBakeRequest other)
        {
            return Value == other.Value;
        }

        public readonly override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }

    public class TextMeshAuthoringBaker : Baker<TextMeshAuthoring>
    {
        public override void Bake(TextMeshAuthoring authoring)
        {
            DependsOn(authoring.Font);

            var entity = GetEntity(TransformUsageFlags.Dynamic);
            var buffer = AddBuffer<TextStringBuffer>(entity);
            StringUtility.CopyToDynamicBuffer(authoring.Text, buffer);
            AddSharedComponentManaged(entity, new FontAssetBakeRequest { Value = authoring.Font });
            AddComponent(entity, new Parent());
            AddComponent(entity, new TextFontSize { Value = authoring.FontSize });
            AddComponent(entity, new TextLayoutMaxSize { Value = new float2(authoring.MaxWidth, 0f) });
            AddComponent(entity, new TextLayoutBreakRule { Value = BreakRule.Word });
            AddComponent(entity, new TextLayoutAlign { Value = authoring.Align });
            AddComponent(entity, new TextLayoutRequireUpdate());
            SetComponentEnabled<TextLayoutRequireUpdate>(entity, true);
        }
    }
}
