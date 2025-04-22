using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

namespace Wildgrass
{
    [HarmonyPatch(typeof(GenBlockLayers))]
    class GenWildgrass : ModStdWorldGen {
        private ICoreServerAPI api;

        public LCGRandom rnd;
        public ClampedSimplexNoise[] grassHeight;
        public ClampedSimplexNoise[] grassDensity;

        public WildgrassLayerConfig wildgrass;

        public override double ExecuteOrder()
        {
            return 0.5;
        }

        public override bool ShouldLoad(EnumAppSide side)
        {
            return side == EnumAppSide.Server;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            this.api = api;

            api.Event.InitWorldGenerator(InitWildgrassGen, "standard");
            api.Event.InitWorldGenerator(InitWildgrassGen, "superflat");

            if(TerraGenConfig.DoDecorationPass) {
                api.Event.ChunkColumnGeneration(OnChunkColumnGeneration, EnumWorldGenPass.Terrain, "standard");
                if(WildgrassCore.IsDev) {
                    api.Event.ChunkColumnGeneration(OnChunkColumnGeneration, EnumWorldGenPass.Terrain, "superflat");
                }
            }
        }

        public void InitWildgrassGen()
        {
            LoadGlobalConfig(api);
            wildgrass = WildgrassLayerConfig.GetInstance(api);

            rnd = new LCGRandom(api.WorldManager.Seed);
            grassHeight = new ClampedSimplexNoise[wildgrass.Species.Length];
            grassDensity = new ClampedSimplexNoise[wildgrass.Species.Length];

            for(int i = 0; i < grassDensity.Length; i++) {
                var species = wildgrass.Species[i];
                grassDensity[i] = new ClampedSimplexNoise(new double[] { species.Chance }, new double[] { species.Spread }, rnd.NextInt() );
                grassHeight[i] = new ClampedSimplexNoise(new double[] { 1.5 }, new double[] { 0.5 }, rnd.NextInt() );
            }
        }

        private void OnChunkColumnGeneration(IChunkColumnGenerateRequest request)
        {
            var chunks = request.Chunks;
            int chunkX = request.ChunkX;
            int chunkZ = request.ChunkZ;

            rnd.InitPositionSeed(chunkX, chunkZ);

            IntDataMap2D forestMap = chunks[0].MapChunk.MapRegion.ForestMap;
            IntDataMap2D climateMap = chunks[0].MapChunk.MapRegion.ClimateMap;

            ushort[] heightMap = chunks[0].MapChunk.RainHeightMap;

            int regionChunkSize = api.WorldManager.RegionSize / chunksize;
            int rdx = chunkX % regionChunkSize;
            int rdz = chunkZ % regionChunkSize;

            // Amount of data points per chunk
            float climateStep = (float)climateMap.InnerSize / regionChunkSize;
            float forestStep = (float)forestMap.InnerSize / regionChunkSize;

            // Retrieves the map data on the chunk edges
            int forestUpLeft = forestMap.GetUnpaddedInt((int)(rdx * forestStep), (int)(rdz * forestStep));
            int forestUpRight = forestMap.GetUnpaddedInt((int)(rdx * forestStep + forestStep), (int)(rdz * forestStep));
            int forestBotLeft = forestMap.GetUnpaddedInt((int)(rdx * forestStep), (int)(rdz * forestStep + forestStep));
            int forestBotRight = forestMap.GetUnpaddedInt((int)(rdx * forestStep + forestStep), (int)(rdz * forestStep + forestStep));

            // increasing x -> left to right
            // increasing z -> top to bottom
            BlockPos herePos = new(0);

            for (int x = 0; x < chunksize; x++)
            {
                for (int z = 0; z < chunksize; z++)
                {
                    herePos.Set(chunkX * chunksize + x, 1, chunkZ * chunksize + z);

                    int posY = heightMap[z * chunksize + x];
                    if (posY >= api.WorldManager.MapSizeY) continue;

                    int climate = climateMap.GetUnpaddedColorLerped(
                        rdx * climateStep + climateStep * x / chunksize,
                        rdz * climateStep + climateStep * z / chunksize
                    );

                    int tempUnscaled = (climate >> 16) & 0xff;
                    float tempRel = Climate.GetAdjustedTemperature(tempUnscaled, posY - TerraGenConfig.seaLevel) / 255f;
                    float rainRel = Climate.GetRainFall((climate >> 8) & 0xff, posY) / 255f;
                    float forestRel = GameMath.BiLerp(forestUpLeft, forestUpRight, forestBotLeft, forestBotRight, (float)x / chunksize, (float)z / chunksize) / 255f;

                    herePos.Y = posY + 1;
                    PlaceWildGrass(herePos, x, posY + 1, z, chunks, rainRel, tempRel, forestRel);
                }
            }
        }

        void PlaceWildGrass(BlockPos worldPos, int x, int posY, int z, IServerChunk[] chunks, float rainRel, float tempRel, float forestRel)
        {
            if(posY >= api.WorldManager.MapSizeY - 1 || posY < 1) return;

            IServerChunk chunk = chunks[posY / chunksize];
            int chunki = (chunksize * (posY % chunksize) + z) * chunksize + x;
            int hereId = chunk.Data[chunki];
            
            if(!WildgrassCore.IsDev)
            if(api.World.Blocks[hereId] is not BlockTallGrass) return;

            WildgrassSpecies species = SpeciesForPos(worldPos, rainRel, tempRel, forestRel);
            if(species == null) {
                return;
            }
            if(rnd.NextDouble() > WildgrassConfig.GenerateDensity) return;

            var grassHeight = this.grassHeight[species.index];
            int gheight = (int)Math.Clamp(grassHeight.Noise(worldPos.X, worldPos.Z) * species.BlockCodes.Length, 0, species.BlockCodes.Length - 1);

            chunk.Data[chunki] = species.BlockIds[gheight];
        }

        public WildgrassSpecies SpeciesForPos(BlockPos pos, float rainRel, float tempRel, float forestRel)
        {
            (double, WildgrassSpecies) finalSpecies = (0.0, null);
            for(int i = 0; i < wildgrass.Species.Length; i++) {
                var species = wildgrass.Species[i];
                var density = grassDensity[i];

                double rndVal = (wildgrass.RndWeight * rnd.NextDouble()) + (wildgrass.PerlinWeight * density.Noise(pos.X, pos.Z, -0.5));
                rndVal = rndVal * rnd.NextDouble() / species.Threshold;
                if(forestRel >= species.MinForest &&
                   forestRel <= species.MaxForest &&
                   rainRel >= species.MinRain &&
                   rainRel <= species.MaxRain &&
                   tempRel >= species.MinTemp &&
                   tempRel <= species.MaxTemp) {
                    if(WildgrassBiomesCompat.IsBiomesEnabled)
                    if(!WildgrassBiomesCompat.WildgrassCanBeInBiome(api, pos, species)) 
                        continue;
                        
                    if(finalSpecies.Item1 < rndVal)
                        finalSpecies = (rndVal, species);
                }
            }
            return finalSpecies.Item2;
        }

        //[HarmonyPrefix]
        //[HarmonyPatch("PlaceTallGrass")]
        //static bool PlaceTallGrass_Prefix(GenBlockLayers __instance, int x, int posY, int z, IServerChunk[] chunks, float rainRel, float tempRel, float temp, float forestRel)
        //{
        //    return false;
        //}
    }
}