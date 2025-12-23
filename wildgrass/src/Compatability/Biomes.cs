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
    public class WildgrassBiomesCompat
    {
        public Dictionary<string, BiomeData> SpeciesBiomeData = [];

        public bool WildgrassCanBeInBiome(BlockPos pos, WildgrassSpecies species) {
            if (!SpeciesBiomeData.TryGetValue(species.Code, out BiomeData speciesData))
            {
                speciesData = new(0);
                foreach (var realm in species.biorealm) speciesData.SetRealm(ExternalRegistry.RealmIndexes[realm], true);
                speciesData.SetFromBioRiver(BioRiver.Both);
                SpeciesBiomeData.Add(species.Code, speciesData);
            }
            return ExternalData.VegetationIsValid(pos, speciesData);
        }
    }
}