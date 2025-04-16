using Elfenlabs.Collections;
using Elfenlabs.Entities;
using Elfenlabs.Mesh;
using Elfenlabs.Rendering;
using Elfenlabs.Unsafe;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Elfenlabs.Text
{
    [UpdateInGroup(typeof(AssetManagementSystemGroup))]
    [CreateAfter(typeof(EntitiesGraphicsSystem))]
    public partial struct FontAssetSystem : ISystem
    {
        EntityQuery assetInitQuery;
        EntityQuery assetCleanupQuery;
        EntityTypeHandle entityTypeHandle;
        SharedComponentTypeHandle<FontAssetReference> assetDataTypeHandle;
        SharedComponentTypeHandle<FontAssetRuntimeData> assetRuntimeDataTypeHandle;
        NativeBidirectionalHashMap<FontAssetReference, FontAssetRuntimeData> runtimeAssetMap;
        NativeHashCounter<FontAssetRuntimeData> runtimeAssetCounter;
        BatchMeshID glyphMeshID;
        Entity quadPrototype;

        void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FontPluginRuntimeHandle>();

            assetInitQuery = SystemAPI.QueryBuilder()
                .WithAll<FontAssetReference>()
                .WithNone<FontAssetRuntimeData>()
                .Build();

            assetCleanupQuery = SystemAPI.QueryBuilder()
                .WithAll<FontAssetRuntimeData>()
                .WithNone<FontAssetReference>()
                .Build();

            entityTypeHandle = state.GetEntityTypeHandle();
            assetDataTypeHandle = state.GetSharedComponentTypeHandle<FontAssetReference>();
            assetRuntimeDataTypeHandle = state.GetSharedComponentTypeHandle<FontAssetRuntimeData>();
            runtimeAssetMap = new NativeBidirectionalHashMap<FontAssetReference, FontAssetRuntimeData>(32, Allocator.Persistent);
            runtimeAssetCounter = new NativeHashCounter<FontAssetRuntimeData>(32, Allocator.Persistent);
            quadPrototype = MeshUtility.CreatePrefab(state.World, "Glyph", MeshUtility.CreateQuad(1f, 1f), Shader.Find("Elfenlabs/Text-MTSDF"), 0);
            glyphMeshID = RenderUtility.RegisterMesh(state.World, MeshUtility.CreateQuad());
        }

        void OnUpdate(ref SystemState state)
        {
            entityTypeHandle.Update(ref state);
            assetDataTypeHandle.Update(ref state);
            assetRuntimeDataTypeHandle.Update(ref state);

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var chunks = new NativeList<ArchetypeChunk>(math.max(assetInitQuery.CalculateChunkCount(), assetCleanupQuery.CalculateChunkCount()), Allocator.Temp);

            InitializeAssetRuntimes(ref state, ecb, chunks);
            CleanupAssetRuntimes(ref state, ecb, chunks);

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            chunks.Dispose();
        }

        void InitializeAssetRuntimes(ref SystemState state, EntityCommandBuffer ecb, NativeList<ArchetypeChunk> chunks)
        {
            var pluginHandle = SystemAPI.GetSingleton<FontPluginRuntimeHandle>().Value;
            assetInitQuery.ToArchetypeChunkList(chunks);
            foreach (var chunk in chunks)
            {
                var assetData = chunk.GetSharedComponent(assetDataTypeHandle);
                if (!runtimeAssetMap.TryGetValue(assetData, out var runtimeData))
                {
                    FontLibrary.LoadFont(
                        pluginHandle,
                        assetData.Value.Value.FontBytes.AsNativeBuffer(),
                        out var fontDesc);

                    FontLibrary.AtlasDeserialize(
                        pluginHandle, 
                        assetData.Value.Value.SerializedAtlasState.AsNativeBuffer(), 
                        out var atlasHandle);

                    runtimeData = new FontAssetRuntimeData
                    {
                        Description = fontDesc,
                        GlyphMap = assetData.Value.Value.FlattenedGlyphMap.Reconstruct(Allocator.Persistent),
                        PrototypeEntity = AdaptPrefab(ref state, ecb, quadPrototype, assetData.Value.Value.Material),
                        AtlasHandle = atlasHandle,
                    };

                    runtimeAssetMap.Add(assetData, runtimeData);
                    runtimeAssetCounter.Increment(runtimeData, chunk.Count);
                }

                ecb.AddSharedComponent(chunk.GetNativeArray(entityTypeHandle), runtimeData);
            }
        }

        void CleanupAssetRuntimes(ref SystemState state, EntityCommandBuffer ecb, NativeList<ArchetypeChunk> chunks)
        {
            assetCleanupQuery.ToArchetypeChunkList(chunks);
            foreach (var chunk in chunks)
            {
                var runtimeData = chunk.GetSharedComponent(assetRuntimeDataTypeHandle);
                var counter = runtimeAssetCounter.Decrement(runtimeData, chunk.Count);
                if (counter == 0)
                {
                    DisposeAssetRuntime(ref state, ecb, runtimeData);
                    runtimeAssetMap.Remove(runtimeData);
                }
            }
            ecb.RemoveComponent<FontAssetRuntimeData>(assetCleanupQuery, EntityQueryCaptureMode.AtPlayback);
        }

        void DisposeAssetRuntime(ref SystemState state, EntityCommandBuffer ecb, FontAssetRuntimeData runtimeData)
        {
            runtimeData.GlyphMap.Dispose();
            ecb.DestroyEntity(runtimeData.PrototypeEntity);
            var pluginHandle = SystemAPI.GetSingleton<FontPluginRuntimeHandle>().Value;
            FontLibrary.UnloadFont(pluginHandle, runtimeData.Description.Handle);
        }

        readonly Entity AdaptPrefab(ref SystemState state, EntityCommandBuffer ecb, Entity original, UnityObjectRef<Material> material)
        {
            var prototype = ecb.Instantiate(original);
            var batchMaterialID = RenderUtility.RegisterMaterial(state.World, material.Value);
            ecb.SetName(prototype, "Glyph-" + material.Value.name);
            ecb.SetComponent(prototype, new MaterialMeshInfo(batchMaterialID, glyphMeshID));
            ecb.AddComponent(prototype, new Prefab());
            return prototype;
        }
    }
}