using System.Collections.Generic;
using System.Linq;
using Biomes;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Wildgrass
{
    public class BiomesCompat : ModSystem
    {
        public static bool IsBiomesEnabled;

        public override bool ShouldLoad(ICoreAPI api)
        {
            if(!api.ModLoader.IsModEnabled("biomes")) return false;
            IsBiomesEnabled = true;
            return base.ShouldLoad(api);
        }

        public static bool WildgrassCanBeInBiome(ICoreAPI api, BlockPos pos, WildgrassSpecies species) {
            BiomesModSystem biomesMod = api.ModLoader.GetModSystem<BiomesModSystem>();
            IMapChunk chunk = api.World.BlockAccessor.GetMapChunkAtBlockPos(pos);
            var chunkRealms = new List<string>();
            if(biomesMod.getModProperty(chunk, BiomesModSystem.MapRealmPropertyName, ref chunkRealms) == EnumCommandStatus.Error) {
                return true;
            }
            return species.biorealm.Intersect(chunkRealms).Any() && biomesMod.CheckRiver(chunk, species.bioriver, pos);
        }
    }
}