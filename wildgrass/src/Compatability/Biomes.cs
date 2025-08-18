using System.Collections.Generic;
using System.Linq;
using Biomes;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Wildgrass
{
    public class WildgrassBiomesCompat : ModSystem
    {
        public static bool IsBiomesEnabled = false;

        public override bool ShouldLoad(ICoreAPI api)
        {
            if(!api.ModLoader.IsModEnabled("biomes")) return false;
            IsBiomesEnabled = true;
            return base.ShouldLoad(api);
        }

        public static bool WildgrassCanBeInBiome(ICoreAPI api, BlockPos pos, WildgrassSpecies species) {
            if(!IsBiomesEnabled) return true;

            BiomesModSystem biomesMod = api.ModLoader.GetModSystem<BiomesModSystem>();
            IMapChunk chunk = api.World.BlockAccessor.GetMapChunkAtBlockPos(pos);
            var chunkRealms = new List<string>();
            if(biomesMod.getModProperty(chunk, BiomesModSystem.MapRealmPropertyName, ref chunkRealms) == EnumCommandStatus.Error) {
                return true;
            }
            return species.biorealm.Intersect(chunkRealms).Any();
        }
    }
}