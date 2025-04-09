using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Elfenlabs.String;
using System;
using Unity.Transforms;

namespace Elfenlabs.Text
{
    public class TextMeshAuthoring : MonoBehaviour
    {
        public FontAssetAuthoring Font;

        public string Text;

        public float FontSize = 1f;
    }

    public struct FontAssetPreBakeReference : ISharedComponentData, IEquatable<FontAssetPreBakeReference>
    {
        public FontAssetAuthoring Value;

        public readonly bool Equals(FontAssetPreBakeReference other)
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
            var buffer = AddBuffer<TextBufferData>(entity);
            StringUtility.CopyToDynamicBuffer(authoring.Text, buffer);
            AddSharedComponentManaged(entity, new FontAssetPreBakeReference { Value = authoring.Font });
            AddComponent(entity, new Parent());
            AddComponent(entity, new TextFontWorldSize { Value = authoring.FontSize });
        }
    }
}
