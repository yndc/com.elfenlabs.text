using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Elfenlabs.Text
{
    [UpdateBefore(typeof(TextGlyphInitializationSystem))]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct TextStringChangeSystem : ISystem
    {
        void OnUpdate(ref SystemState state)
        {
            var changedTextQuery = SystemAPI.QueryBuilder()
                .WithPresent<TextGlyphRequireUpdate>()
                .WithAll<TextStringBuffer>()
                .WithAll<TextGlyphBuffer>()
                .Build();

            changedTextQuery.SetChangedVersionFilter(ComponentType.ReadOnly<TextStringBuffer>());

            if (changedTextQuery.IsEmpty)
                return;

            state.EntityManager.SetComponentEnabled<TextGlyphRequireUpdate>(changedTextQuery, true);
        }
    }
}