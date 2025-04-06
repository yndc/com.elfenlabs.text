using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Entities.Serialization;

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
            DependsOn(authoring.Font);

            var entity = GetEntity(TransformUsageFlags.Dynamic);
            var config = authoring.Font.CreateAssetResource(Allocator.Persistent);

            AddComponent(entity, new TextStringConfig { Value = authoring.Text });
            AddSharedComponentManaged(entity, config);
        }
    }
}
