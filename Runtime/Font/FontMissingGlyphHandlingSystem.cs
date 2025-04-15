using Unity.Collections;
using Unity.Entities;

namespace Elfenlabs.Text
{
    public partial struct FontMissingGlyphHandlingSystem : ISystem
    {
        void OnCreate(ref SystemState state)
        {
            state.EntityManager.CreateSingleton(new MissingGlyphSet
            {
                Value = new NativeParallelHashSet<int>(32, Allocator.Persistent)
            });
        }

        void OnDestroy(ref SystemState state)
        {
            SystemAPI.GetSingleton<MissingGlyphSet>().Value.Dispose();
            state.EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<MissingGlyphSet>());
        }

        void OnUpdate(ref SystemState state)
        {
            var missingGlyphSet = SystemAPI.GetSingleton<MissingGlyphSet>().Value;

            if (!missingGlyphSet.IsEmpty)
            {
                foreach (var missingGlyph in missingGlyphSet)
                {
                    UnityEngine.Debug.LogWarning($"Missing glyph: {missingGlyph}");
                }
            }

            missingGlyphSet.Clear();
        }

        public struct MissingGlyphSet : IComponentData
        {
            public NativeParallelHashSet<int> Value;
        }
    }
}