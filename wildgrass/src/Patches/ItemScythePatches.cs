using HarmonyLib;
using Microsoft.VisualBasic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Wildgrass
{
    [HarmonyPatch(typeof(ItemScythe))]
    class ItemScythePatches {
    
        [HarmonyPrefix]
        [HarmonyPatch("breakMultiBlock")]
        static bool breakMultiBlock_Prefix(ItemScythe __instance, BlockPos pos, IPlayer plr)
        {
            var traverse = Traverse.Create(__instance);
            bool trimMode = plr?.InventoryManager.ActiveHotbarSlot?.Itemstack?.Attributes?.GetInt("toolMode", 0) == 0;
            if(!trimMode) return true;

            var api = traverse.Field("api").GetValue() as ICoreAPI;
            Block block = api.World.BlockAccessor.GetBlock(pos);
            if(block is not BlockWildgrass wildgrass) return true;

            if(wildgrass.CutInto == null) return true;
            Block trimmedBlock = wildgrass.CutInto;
            if(trimmedBlock == null) return true;

            api.World.BlockAccessor.BreakBlock(pos, plr);
            api.World.BlockAccessor.MarkBlockDirty(pos);

            api.World.BlockAccessor.SetBlock(trimmedBlock.BlockId, pos);
            return false;
        }
    }
}