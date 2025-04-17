using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using HarmonyLib;

namespace Wildgrass
{
    public class Core : ModSystem
    {
        public static AssetLocation[] WildgrassBlockCodes;

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
        }

        public override void Dispose()
        {
            harmony?.UnpatchAll(Mod.Info.ModID);
        }
    }
}
