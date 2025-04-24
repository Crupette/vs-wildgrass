using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

namespace Wildgrass
{
    public class BlockWildgrass : Block, IDrawYAdjustable, IWithDrawnHeight
    {
        [JsonObject]
        public class WildgrassGrowth {
            [JsonProperty]
            public float growthChanceOnTick;
            [JsonProperty]
            public float? minTemp;
            [JsonProperty]
            public string[] months;
            [JsonProperty]
            public AssetLocation code;
        }

        Block tallGrassBlock;
        Block snowLayerBlock;

        public int drawnHeight { get; set; }
        public WildgrassGrowth Growth;
        public Block CutInto { get; private set; }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            drawnHeight = Attributes?["drawnHeight"]?.AsInt(48) ?? 48;
            Growth = Attributes?["growth"]?.AsObject<WildgrassGrowth>();
            CutInto = api.World.GetBlock(AssetLocation.Create(Attributes?["cutInto"]?.ToString()));

            if(api is ICoreClientAPI) {
                snowLayerBlock = api.World.GetBlock(new AssetLocation("snowlayer-1"));
                tallGrassBlock = api.World.GetBlock(new AssetLocation("tallgrass-tall-free"));
            }
        }

        public override bool ShouldReceiveServerGameTicks(IWorldAccessor world, BlockPos pos, Random offThreadRandom, out object extra)
        {
            extra = null;
            if(Growth == null) return false;
            if(world.BlockAccessor.GetRainMapHeightAt(pos) > pos.Y + 1) return false;


            float growthChanceOnTick = Growth.growthChanceOnTick;
            if(offThreadRandom.NextDouble() > growthChanceOnTick) return false;

            ClimateCondition climateCond = GetClimateAt(world.BlockAccessor, pos);
            if(Growth.minTemp != null)
                if(climateCond.Temperature < Growth.minTemp) return false;

            if(Growth.months?.Any() ?? false) {
                EnumMonth month = world.Calendar.MonthName;
                string monthString = month.ToString().ToLower();
                if(!Growth.months.Intersect(new string[] {monthString}).Any()) return false;
            }

            Block nextBlock = world.GetBlock(Growth.code);
            if(nextBlock != null) extra = nextBlock;
            return extra != null;
            
        }

        public override void OnServerGameTick(IWorldAccessor world, BlockPos pos, object extra = null)
        {
            base.OnServerGameTick(world, pos, extra);

            Block nextStageBlock = extra as Block;
            world.BlockAccessor.ExchangeBlock(nextStageBlock.Id, pos);
        }

        private static ClimateCondition GetClimateAt(IBlockAccessor blockAccessor, BlockPos pos)
        {
            var conds = blockAccessor.GetClimateAt(pos, EnumGetClimateMode.NowValues);
            return conds;
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
            if(CutInto == null) return;

            if(byPlayer?.InventoryManager.ActiveTool == EnumTool.Knife) {
                world.BlockAccessor.SetBlock(CutInto.Id, pos);
            }
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            if (Variant.ContainsKey("side"))
            {
                return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
            }

            if (CanPlantStay(world.BlockAccessor, blockSel.Position))
            {
                return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
            }

            failureCode = "requirefertileground";
            return false;
        }

        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            if (!CanPlantStay(world.BlockAccessor, pos))
            {
                //if (world.BlockAccessor.GetBlock(pos.DownCopy()).Id == 0) world.BlockAccessor.SetBlock(0, pos);
                world.BlockAccessor.BreakBlock(pos, null);
            }
            base.OnNeighbourBlockChange(world, pos, neibpos);
        }

        public virtual bool CanPlantStay(IBlockAccessor blockAccessor, BlockPos pos)
        {
            Block blockBelow = blockAccessor.GetBlock(pos.DownCopy());
            if (blockBelow.Fertility <= 0) return false;
            return true;
        }

        public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldgenRandom, BlockPatchAttributes attributes = null)
        {
            if(!CanPlantStay(blockAccessor, pos)) return false;
            return base.TryPlaceBlockForWorldGen(blockAccessor, pos, onBlockFace, worldgenRandom, attributes);
        }

        public float AdjustYPosition(BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
        {
            Block nblock = chunkExtBlocks[extIndex3d + TileSideEnum.MoveIndex[TileSideEnum.Down]];
            return nblock is BlockFarmland ? -0.0625f : 0f;
        }

        public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
        {
            if(snowLevel > 0) return snowLayerBlock.GetRandomColor(capi, pos, facing, rndIndex);
            return tallGrassBlock.GetRandomColor(capi, pos, facing, rndIndex);
        }

        public override int GetColor(ICoreClientAPI capi, BlockPos pos)
        {
            if(snowLevel > 0) return snowLayerBlock.GetColor(capi, pos);
            return tallGrassBlock.GetColor(capi, pos);
        }
    }
}