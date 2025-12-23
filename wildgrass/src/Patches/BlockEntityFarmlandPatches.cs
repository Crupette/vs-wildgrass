using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Wildgrass;

[HarmonyPatch(typeof(BlockEntityFarmland), "Update")]
static class BlockEntityFarmland_UpdatePatch
{
    static void WildgrassWeed(IBlockAccessor blockAccessor, int blockId, BlockPos abovePos)
    {
        Block block = blockAccessor.GetBlock(blockId);
        if(block is BlockTallGrass) {
            var api = Traverse.Create(block).Field("api").GetValue<ICoreAPI>();
            var genWildgrassSystem = api.ModLoader.GetModSystem<GenWildgrass>();

            var climate = blockAccessor.GetClimateAt(abovePos, EnumGetClimateMode.WorldGenValues);

            float rainRel = climate.Rainfall;
            float tempRel = climate.Temperature;
            float forestRel = climate.ForestDensity;
            var species = genWildgrassSystem.SpeciesForPos(abovePos, rainRel, tempRel, forestRel);

            if(species != null) {
                blockAccessor.SetBlock(species.BlockIds[0], abovePos);
                return;
            }
        }
        blockAccessor.SetBlock(blockId, abovePos);
    }

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        try {
            var codeMatcher = new CodeMatcher(instructions).Start();
            var pos = codeMatcher
                .MatchStartForward(
                    CodeMatch.Calls(() => default(IBlockAccessor).SetBlock(default, default))
                )
                .ThrowIfInvalid("Failed patch BlockEntityFarmland.Update")
                .Repeat((cm) => {
                        cm.RemoveInstruction();
                        cm.InsertAndAdvance(
                            CodeInstruction.Call(() => WildgrassWeed(default, default, default)));
                    }
                );
            
            var cminstructions = codeMatcher.Instructions();
            return cminstructions;
        } catch(Exception e) {
            WildgrassCore.Instance.api.Logger.Error($"Exception patching BlockEntityFarmland.Update : {e}");
            return instructions;
        }
    }
}