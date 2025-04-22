using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Wildgrass
{
    public class WildgrassPatchSpawnConditions : ModSystem
    {
        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Server;
        }

        public override void AssetsFinalize(ICoreAPI api)
        {
            base.AssetsFinalize(api);

            if(api is not ICoreServerAPI sapi) return;
            WildgrassCore system = sapi.ModLoader.GetModSystem<WildgrassCore>();

            List<AssetLocation> grassCodes = new();
            foreach(var block in system.WildgrassBlocks) {
                grassCodes.Add(block.Code);
            }

            AssetLocation tallgrassCode = new("tallgrass-*");
            foreach(var entity in api.World.EntityTypes) {
                var worldgenBlocks = entity?.Server?.SpawnConditions?.Worldgen?.InsideBlockCodes;
                var runtimeBlocks = entity?.Server?.SpawnConditions?.Runtime?.InsideBlockCodes;

                if(worldgenBlocks != null && worldgenBlocks.Contains(tallgrassCode)) {
                    worldgenBlocks = worldgenBlocks.Append(grassCodes.ToArray());
                    entity.Server.SpawnConditions.Worldgen.InsideBlockCodes = worldgenBlocks;
                }
                if(runtimeBlocks != null && runtimeBlocks.Contains(tallgrassCode)) {
                    runtimeBlocks = runtimeBlocks.Append(grassCodes.ToArray());
                    entity.Server.SpawnConditions.Runtime.InsideBlockCodes = runtimeBlocks;
                }
            }
        }
    }
}