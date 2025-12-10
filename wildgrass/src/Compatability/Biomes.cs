using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Biomes.Api;
using Microsoft.VisualBasic.FileIO;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Wildgrass
{
    public class WildgrassBiomesCompat : ModSystem
    {
        private static WildgrassBiomesCompat biomesCompat;
        public static bool IsBiomesEnabled = false;
        public Dictionary<string, BiomeData> SpeciesBiomeData = [];

        public override bool ShouldLoad(ICoreAPI api)
        {
            if (!api.ModLoader.IsModEnabled("biomes")) return false;
            IsBiomesEnabled = true;
            return base.ShouldLoad(api);
        }

        public override void Start(ICoreAPI api)
        {
            biomesCompat = this;
        }

        public static bool WildgrassCanBeInBiome(BlockPos pos, WildgrassSpecies species) {
            if (!IsBiomesEnabled) return true;
            if (!biomesCompat.SpeciesBiomeData.TryGetValue(species.Code, out BiomeData speciesData))
            {
                speciesData = new(0);
                foreach (var realm in species.biorealm) speciesData.SetRealm(ExternalRegistry.RealmIndexes[realm], true);
                speciesData.SetFromBioRiver(BioRiver.Both);
                biomesCompat.SpeciesBiomeData.Add(species.Code, speciesData);
            }
            return ExternalData.VegetationIsValid(pos, speciesData);
        }
    }
}