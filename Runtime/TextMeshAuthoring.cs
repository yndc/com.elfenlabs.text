using UnityEngine;
using Unity.Entities;

namespace Elfenlabs.Text
{
    public class TextMeshAuthoring : MonoBehaviour
    {
        public FontAsset Font;

        public string Text;
    }

    public class TextMeshAuthoringBaker : Baker<TextMeshAuthoring>
    {
        public override void Bake(TextMeshAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new TextStringConfig { Value = authoring.Text });
        }
    }
}
