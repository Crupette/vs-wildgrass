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
        public static WildgrassCore Instance;

        public WildgrassBiomesCompat BiomesCompatibility = null;
        
        public override void Start(ICoreAPI api)
        {
            IsDev = Mod.Info.Version.Contains("dev");
            Instance = this;

            if(api.ModLoader.IsModEnabled("biomes"))
            {
                BiomesCompatibility = new();
            }

            this.api = api;
            if(!Harmony.HasAnyPatches(Mod.Info.ModID)) {
                harmony = new Harmony(Mod.Info.ModID);
                harmony.PatchAll();
            }

            api.RegisterBlockClass("wildgrass.BlockWildgrass", typeof(BlockWildgrass));
            
            api.RegisterItemClass("wildgrass.ItemGrassSeeds", typeof(ItemGrassSeeds));
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
