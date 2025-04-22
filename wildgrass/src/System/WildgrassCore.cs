using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using HarmonyLib;
using System.Collections.Generic;
using Vintagestory.API.Util;

namespace Wildgrass
{
    public class WildgrassCore : ModSystem
    {
        Harmony harmony;
        public ICoreAPI api;
        public static bool IsDev = false;
        
        public override void Start(ICoreAPI api)
        {
            IsDev = Mod.Info.Version.Contains("dev");
            this.api = api;
            if(!Harmony.HasAnyPatches(Mod.Info.ModID)) {
                harmony = new Harmony(Mod.Info.ModID);
                harmony.PatchAll();
            }

            api.RegisterBlockClass("wildgrass.BlockWildgrass", typeof(BlockWildgrass));

            api.RegisterItemClass("wildgrass.ItemGrassSeeds", typeof(ItemGrassSeeds));

            if(api.Side == EnumAppSide.Client) {
                VertexFlags flags = new();
                flags.All = VertexFlags.PackNormal(0, 1, 0);
                api.Logger.Debug($"Pack Normal (0,1,0) {flags.Normal}");
            }
        }

        public BlockWildgrass[] WildgrassBlocks => ObjectCacheUtil.GetOrCreate(api, "Wildgrass.WildgrassBlocks",  () => {
            List<BlockWildgrass> blocks = new();
            foreach(var block in api.World.Blocks) {
                if(block is BlockWildgrass) blocks.Add(block as BlockWildgrass);
            }
            return blocks.ToArray();
        });

        public override void Dispose()
        {
            harmony?.UnpatchAll(Mod.Info.ModID);
            ObjectCacheUtil.Delete(api, "Wildgrass.WildgrassBlocks");
        }

        public static WildgrassCore GetInstance(ICoreAPI api) {
            return api.ModLoader.GetModSystem<WildgrassCore>();
        }
    }
}
