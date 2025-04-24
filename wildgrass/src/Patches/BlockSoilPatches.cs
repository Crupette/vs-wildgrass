using System;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Wildgrass
{
    [HarmonyPatch(typeof(BlockSoil))]
    public class BlockSoilPatches {

        //Make it so that new grass is of the neighboring species.
        [HarmonyPrefix]
        [HarmonyPatch("getTallGrassBlock")]
        private static bool getTallGrassBlock_Prefix(BlockSoil __instance, ref Block __result, IWorldAccessor world, BlockPos abovePos, Random offthreadRandom)
        {
            var traverse = Traverse.Create(__instance);

            float tallGrassGrowthChance = (float)traverse.Field("tallGrassGrowthChance").GetValue();
            var genWildgrassSystem = world.Api.ModLoader.GetModSystem<GenWildgrass>();
            if(genWildgrassSystem == null) throw new NullReferenceException("Could not find GenWildgrass system");

            if(offthreadRandom.NextDouble() > tallGrassGrowthChance) {
                __result = null;
                return false;
            }

            Block aboveBlock = world.BlockAccessor.GetBlock(abovePos);
            if(aboveBlock is BlockWildgrass wildgrass) {
                __result = world.GetBlock(wildgrass.Growth.code);
                return false;
            }

            var climate = world.BlockAccessor.GetClimateAt(abovePos, EnumGetClimateMode.WorldGenValues);

            float rainRel = climate.Rainfall;
            float tempRel = climate.Temperature;
            float forestRel = climate.ForestDensity;
            var species = genWildgrassSystem.SpeciesForPos(abovePos, rainRel, tempRel, forestRel);

            if(species == null) {
                return true;
            }

            var asset = AssetLocation.Create(species.Code).WithPathAppendix("-0-free");
            __result = world.GetBlock(asset);
            return false;
        }
    }
}