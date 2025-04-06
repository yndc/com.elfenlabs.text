using System;
using Unity.Entities;

namespace Elfenlabs.Text
{
    public struct FontPluginHandle : IComponentData
    {
        public IntPtr Handle;

        public FontPluginHandle(IntPtr handle)
        {
            Handle = handle;
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

            state.EntityManager.CreateSingleton(new FontPluginHandle(pluginCtx));
        }

        void OnDestroy(ref SystemState state)
        {
            var pluginHandle = SystemAPI.GetSingleton<FontPluginHandle>();
            FontLibrary.DestroyContext(pluginHandle.Handle);
        }
    }
}