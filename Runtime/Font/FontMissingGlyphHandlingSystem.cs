using Elfenlabs.Collections;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace Elfenlabs.Text
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(TextGlyphInitializationSystem))]
    public partial struct FontMissingGlyphHandlingSystem : ISystem
    {
        void OnUpdate(ref SystemState state)
        {
            var pluginHandle = SystemAPI.GetSingleton<FontPluginRuntimeHandle>().Value;
            var query = SystemAPI.QueryBuilder()
                .WithAll<FontAssetRuntimeData>()
                .WithAll<TextGlyphBuffer>()
                .Build();

            state.EntityManager.GetAllUniqueSharedComponents<FontAssetRuntimeData>(out var fontAssetRuntimes, Allocator.Temp);

            foreach (var fontAssetRuntime in fontAssetRuntimes)
            {
                if (fontAssetRuntime.PrototypeEntity == Entity.Null)
                    continue;

                var missingGlyphSet = fontAssetRuntime.MissingGlyphSet;
                if (missingGlyphSet.IsEmpty)
                    continue;

                var index = 0;
                var glyphs = new NativeBuffer<GlyphMetrics>(missingGlyphSet.Count(), Allocator.Temp);
                foreach (var glyphCodePoint in missingGlyphSet)
                {
                    glyphs[index] = new GlyphMetrics { CodePoint = glyphCodePoint };
                    index++;
                    Debug.Log($"Adding glyph: {glyphCodePoint}");
                }

                var material = state.World.GetExistingSystemManaged<EntitiesGraphicsSystem>().GetMaterial(fontAssetRuntime.MaterialID);
                var textureArray = material.mainTexture as Texture2DArray;
                var textureBuffer = NativeBuffer<Color32>.Alias(textureArray.GetPixelData<Color32>(0, 0));

                FontLibrary.GetGlyphMetrics(
                    pluginHandle,
                    fontAssetRuntime.Description.Handle,
                    fontAssetRuntime.AssetReference.Value.AtlasConfig.GlyphSize,
                    fontAssetRuntime.AssetReference.Value.AtlasConfig.Padding,
                    ref glyphs);

                var packedCount = fontAssetRuntime.Atlas.PackGlyphs(glyphs.AsNativeArray());

                FontLibrary.RenderGlyphsToAtlas(
                    pluginHandle,
                    fontAssetRuntime.Description.Handle,
                    fontAssetRuntime.AssetReference.Value.AtlasConfig,
                    fontAssetRuntime.AssetReference.Value.RenderConfig,
                    in glyphs,
                    ref textureBuffer);

                textureArray.Apply();

                for (int i = 0; i < glyphs.Count(); i++)
                {
                    var glyph = glyphs[i];
                    fontAssetRuntime.GlyphMap.Add(
                        glyphs[i].CodePoint,
                        new GlyphRuntimeData(glyph, fontAssetRuntime.AssetReference.Value.AtlasConfig.Size));
                }

                Debug.Log($"Packed {packedCount} glyphs.");

                // TODO: at the moment all text that shares the same font asset will be updated.
                // In the future only the text that requires the glyphs should be updated.
                query.SetSharedComponentFilter(fontAssetRuntime);
                var entities = query.ToEntityArray(Allocator.Temp);
                Debug.Log($"Updating {entities.Length} entities with missing glyphs.");
                foreach (var entity in entities)
                {
                    state.EntityManager.SetComponentEnabled<TextGlyphRequireUpdate>(entity, true);
                }

                missingGlyphSet.Clear();
            }
        }
    }
}