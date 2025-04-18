using System;
using Elfenlabs.Collections;
using Elfenlabs.Entities;
using Elfenlabs.Mesh;
using Elfenlabs.Rendering;
using Elfenlabs.Unsafe;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
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
                    runtimeData = CreateAssetRuntime(ref state, ecb, pluginHandle, assetData);
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

        FontAssetRuntimeData CreateAssetRuntime(ref SystemState state, EntityCommandBuffer ecb, IntPtr pluginHandle, FontAssetReference assetRef)
        {
            FontLibrary.LoadFont(
                        pluginHandle,
                        assetRef.Value.Value.FontBytes.AsNativeBuffer(),
                        out var fontDesc);

            var atlas = assetRef.Value.Value.SerializedAtlasPacker.Deserialize(Allocator.Persistent);

            return new FontAssetRuntimeData
            {
                AssetReference = assetRef.Value,
                Description = fontDesc,
                GlyphMap = assetRef.Value.Value.FlattenedGlyphMap.Reconstruct(Allocator.Persistent),
                PrototypeEntity = AdaptPrefab(ref state, ecb, quadPrototype, assetRef.Value.Value.Material, out var batchMaterialID),
                Atlas = atlas,
                MissingGlyphSet = new UnsafeParallelHashSet<int>(32, Allocator.Persistent),
                MaterialID = batchMaterialID,
            };
        }

        void DisposeAssetRuntime(ref SystemState state, EntityCommandBuffer ecb, FontAssetRuntimeData runtimeData)
        {
            runtimeData.GlyphMap.Dispose();
            runtimeData.MissingGlyphSet.Dispose();
            ecb.DestroyEntity(runtimeData.PrototypeEntity);
            var pluginHandle = SystemAPI.GetSingleton<FontPluginRuntimeHandle>().Value;
            FontLibrary.UnloadFont(pluginHandle, runtimeData.Description.Handle);
        }

        readonly Entity AdaptPrefab(ref SystemState state, EntityCommandBuffer ecb, Entity original, UnityObjectRef<Material> material, out BatchMaterialID batchMaterialID)
        {
            var prototype = ecb.Instantiate(original);

            // Clone the material and texture so runtime modifications don't affect the original
            var originalTexture = material.Value.mainTexture as Texture2DArray;
            var textureClone = new Texture2DArray(
                originalTexture.width,
                originalTexture.height,
                originalTexture.depth,
                originalTexture.graphicsFormat,
                TextureCreationFlags.None
            );

            for (int depth = 0; depth < originalTexture.depth; depth++)
            {
                Graphics.CopyTexture(originalTexture, depth, 0, textureClone, depth, 0);
            }

            var materialClone = new Material(material.Value)
            {
                mainTexture = textureClone
            };

            // Register the cloned material
            batchMaterialID = RenderUtility.RegisterMaterial(state.World, materialClone);
            ecb.SetName(prototype, "Glyph-" + material.Value.name);
            ecb.SetComponent(prototype, new MaterialMeshInfo(batchMaterialID, glyphMeshID));
            ecb.AddComponent(prototype, new Prefab());
            return prototype;
        }
    }
}