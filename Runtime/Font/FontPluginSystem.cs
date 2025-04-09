using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;

namespace Elfenlabs.Text
{
    public struct FontPluginRuntimeHandle : IComponentData
    {
        [NativeDisableUnsafePtrRestriction]
        public IntPtr Value;

        public FontPluginRuntimeHandle(IntPtr handle)
        {
            Value = handle;
        }
    }

    public partial struct FontPluginSystem : ISystem
    {
        void OnCreate(ref SystemState state)
        {
            FontLibrary.CreateContext(
                FontLibrary.UnityLog,
                FontLibrary.UnityAllocator,
                FontLibrary.UnityDisposer,
                out var pluginCtx);

            state.EntityManager.CreateSingleton(new FontPluginRuntimeHandle(pluginCtx));
        }

        void OnDestroy(ref SystemState state)
        {
            // var pluginHandle = SystemAPI.GetSingleton<FontPluginRuntimeHandle>();
            // FontLibrary.DestroyContext(pluginHandle.Value);
            // Debug.Log("FontPluginSystem: Destroyed context.");
        }
    }
}