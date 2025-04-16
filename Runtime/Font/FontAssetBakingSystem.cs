using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Elfenlabs.Text
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct FontAssetBakingSystem : ISystem
    {
        void OnUpdate(ref SystemState state)
        {
            var list = new List<FontAssetBakeRequest>();
            var preBakeReferenceQuery = SystemAPI.QueryBuilder()
                .WithAll<FontAssetBakeRequest>()
                .Build();

            state.EntityManager.GetAllUniqueSharedComponentsManaged(list);

            foreach (var fontAssetReference in list)
            {
                if (fontAssetReference.Value == null)
                    continue;
                var assetData = fontAssetReference.Value.CreateAssetData(Allocator.Persistent);
                preBakeReferenceQuery.SetSharedComponentFilterManaged(fontAssetReference);
                state.EntityManager.AddSharedComponent(preBakeReferenceQuery, assetData);
                state.EntityManager.RemoveComponent<FontAssetBakeRequest>(preBakeReferenceQuery);
            }
        }
    }
}