using Unity.Entities;

namespace Elfenlabs.Text
{
    public struct FontState : IComponentData
    {
        public int Index;
    }

    public partial struct FontAssetSystem : ISystem
    {
        void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FontPluginHandle>();
        }

        void OnUpdate(ref SystemState state)
        {
            // var query = SystemAPI.QueryBuilder()
            //     .WithAll<FontConfig>()
            //     .WithAbsent<FontState>()
            //     .Build();


        }

        // partial struct FontLoadJob : IJobEntity
        // {
        //     public FontPluginHandle PluginHandle;

        //     public void Execute(Entity entity, in FontConfig fontConfig)
        //     {
        //         // FontLibrary.LoadFont()
        //     }
        // }
    }
}