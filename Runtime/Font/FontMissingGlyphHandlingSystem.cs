using System;
using Elfenlabs.Collections;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;
using UnityEngine;

namespace Elfenlabs.Text
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(TextGlyphInitializationSystem))]
    public partial struct FontMissingGlyphHandlingSystem : ISystem
    {
        NativeHashMap<FontAssetRuntimeData, AtlasUpdateParameters> currentJobs;

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

                // Create parameters
                var param = new AtlasUpdateParameters(fontAssetRuntime)
                {
                    Glyphs = new NativeBuffer<GlyphMetrics>(fontAssetRuntime.MissingGlyphSet.Count(), Allocator.Persistent) // Persistent might be overkill, but it's safe
                };

                // Run glyph metrics update job
                var updateMetricsJob = new UpdateGlyphMetricsJob
                {
                    PluginHandle = pluginHandle,
                    Parameters = param
                };

                var updateMetricsHandle = updateMetricsJob.Schedule();

                updateMetricsHandle.Complete();

                // Immediately reupdate the glyphs with the new metrics
                // We do this before the atlas rendering itself so that get get instant layout updates
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

                // Obtain pointer to the texture buffer
                var material = state.World.GetExistingSystemManaged<EntitiesGraphicsSystem>().GetMaterial(fontAssetRuntime.MaterialID);
                var textureArray = material.mainTexture as Texture2DArray;
                var textureBuffer = NativeBuffer<Color32>.Alias(textureArray.GetPixelData<Color32>(0, 0));
                param.TextureBuffer = textureBuffer;

                // Run render job
                var renderJob = new RenderAtlasJob
                {
                    PluginHandle = pluginHandle,
                    Parameters = param
                };

                var renderHandle = renderJob.Schedule(updateMetricsHandle);

                renderHandle.Complete();

                // Apply the texture to the GPU
                textureArray.Apply();

                // Dispose of the glyph metrics buffer
                param.Glyphs.Dispose();
            }
        }

        struct AtlasUpdateParameters
        {
            public readonly FontAssetRuntimeData FontRuntime;
            public NativeBuffer<GlyphMetrics> Glyphs;
            public NativeBuffer<Color32> TextureBuffer;
            public AtlasUpdateParameters(FontAssetRuntimeData fontRuntime)
            {
                FontRuntime = fontRuntime;
                Glyphs = default;
                TextureBuffer = default;
            }
        }

        /// <summary>
        /// The first job, responsible for obtaining glyph metrics and packing them into the atlas
        /// </summary>
        partial struct UpdateGlyphMetricsJob : IJob
        {
            [NativeDisableUnsafePtrRestriction]
            public IntPtr PluginHandle;

            [NativeDisableUnsafePtrRestriction]
            public AtlasUpdateParameters Parameters;

            public void Execute()
            {
                // Create glyph buffer from the missing glyph set
                var glyphIndex = 0;
                foreach (var glyphCodePoint in Parameters.FontRuntime.MissingGlyphSet)
                {
                    Parameters.Glyphs[glyphIndex] = new GlyphMetrics { CodePoint = glyphCodePoint };
                    glyphIndex++;
                }

                // Obtain glyph metrics for glyph sizes
                FontLibrary.GetGlyphMetrics(
                    PluginHandle,
                    Parameters.FontRuntime.Description.Handle,
                    Parameters.FontRuntime.AssetReference.Value.AtlasConfig.GlyphSize,
                    Parameters.FontRuntime.AssetReference.Value.AtlasConfig.Padding,
                    ref Parameters.Glyphs);

                // Obtain glyph positions in the atlas
                var packedCount = Parameters.FontRuntime.Atlas.PackGlyphs(Parameters.Glyphs.AsNativeArray());

                // Update glyph data into the glyph map
                for (int i = 0; i < Parameters.Glyphs.Count(); i++)
                {
                    var glyph = Parameters.Glyphs[i];
                    Parameters.FontRuntime.GlyphMap.Add(
                        glyph.CodePoint,
                        new GlyphRuntimeData(
                            glyph,
                            Parameters.FontRuntime.AssetReference.Value.AtlasConfig.Size));
                }
            }
        }

        /// <summary>
        /// The second job, responsible for rendering the glyphs into the atlas texture on the CPU
        /// After this job, the main thread needs to update the texture in the GPU by using texture.Apply()
        /// </summary>
        partial struct RenderAtlasJob : IJob
        {
            [NativeDisableUnsafePtrRestriction]
            public IntPtr PluginHandle;

            [NativeDisableUnsafePtrRestriction]
            public AtlasUpdateParameters Parameters;

            public void Execute()
            {
                FontLibrary.RenderGlyphsToAtlas(
                    PluginHandle,
                    Parameters.FontRuntime.Description.Handle,
                    Parameters.FontRuntime.AssetReference.Value.AtlasConfig,
                    Parameters.FontRuntime.AssetReference.Value.RenderConfig,
                    in Parameters.Glyphs,
                    ref Parameters.TextureBuffer);
            }
        }
    }
}