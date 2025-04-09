using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace Elfenlabs.Text
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct FontAssetBakingSystem : ISystem
    {
        EntityQuery preBakeReferenceQuery;

        void OnCreate(ref SystemState state)
        {
            preBakeReferenceQuery = state.GetEntityQuery(ComponentType.ReadOnly<FontAssetPreBakeReference>());
        }

        void OnUpdate(ref SystemState state)
        {
            var list = new List<FontAssetPreBakeReference>();
            state.EntityManager.GetAllUniqueSharedComponentsManaged(list);
            foreach (var fontAssetReference in list)
            {
                if (fontAssetReference.Value == null)
                    continue;
                var assetData = fontAssetReference.Value.CreateAssetData(Allocator.Persistent);
                preBakeReferenceQuery.SetSharedComponentFilterManaged(fontAssetReference);
                state.EntityManager.AddSharedComponent(preBakeReferenceQuery, assetData);
                state.EntityManager.RemoveComponent<FontAssetPreBakeReference>(preBakeReferenceQuery);
            }
        }
    }
}