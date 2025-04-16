using Elfenlabs.Collections;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace Elfenlabs.Text
{
    public partial struct FontMissingGlyphHandlingSystem : ISystem
    {
        EntityQuery query;

        void OnCreate(ref SystemState state)
        {

        }

        void OnUpdate(ref SystemState state)
        {
            var pluginHandle = SystemAPI.GetSingleton<FontPluginRuntimeHandle>().Value;
            var q = query = SystemAPI.QueryBuilder()
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
                }

                var material = state.World.GetExistingSystemManaged<EntitiesGraphicsSystem>().GetMaterial(fontAssetRuntime.MaterialID);
                var textureArray = material.mainTexture as Texture2DArray;
                var textureBuffer = NativeBuffer<Color32>.Alias(textureArray.GetPixelData<Color32>(0, 0));

                FontLibrary.AtlasPackGlyphs(
                    pluginHandle,
                    fontAssetRuntime.Description.Handle,
                    fontAssetRuntime.AtlasHandle,
                    fontAssetRuntime.AssetReference.Value.RenderConfig,
                    ref glyphs,
                    ref textureBuffer,
                    out var packedCount);

                textureArray.Apply();

                for (int i = 0; i < glyphs.Count(); i++)
                {
                    var glyph = glyphs[i];
                    fontAssetRuntime.GlyphMap.Add(
                        glyphs[i].CodePoint,
                        new GlyphRuntimeData(glyph, fontAssetRuntime.AssetReference.Value.AtlasConfig.Size));
                }

                Debug.Log($"Packed {packedCount} glyphs.");

                q.SetSharedComponentFilter(fontAssetRuntime);

                // We trigger a change in the text buffer to force reinitialization of the glyphs
                // This is a bit of a hack, but it works for now
                // TODO: Find a better way to do this
                var entities = q.ToEntityArray(Allocator.Temp);
                Debug.Log($"Updating {entities.Length} entities with missing glyphs.");
                foreach (var entity in entities)
                {
                    Debug.Log($"Updating entity {entity} with missing glyphs.");
                    state.EntityManager.GetBuffer<TextStringBuffer>(entity);
                }

                fontAssetRuntime.MissingGlyphSet.Clear();
            }
        }
    }
}