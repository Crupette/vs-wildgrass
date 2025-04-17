using System;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.ServerMods;

namespace Wildgrass
{
    [JsonObject(MemberSerialization.OptIn)]
    public class WildgrassSpecies {
        [JsonProperty]
        public string Code;
        [JsonProperty]
        public double Chance;
        [JsonProperty]
        public double Spread;
        [JsonProperty]
        public double Threshold;
        [JsonProperty]
        public int MinTemp;
        [JsonProperty]
        public int MaxTemp;
        [JsonProperty]
        public double MinRain;
        [JsonProperty]
        public double MaxRain;
        [JsonProperty]
        public double MinForest;
        [JsonProperty]
        public double MaxForest;
        [JsonProperty]
        public String[] biorealm;
        [JsonProperty]
        public String bioriver;
        [JsonProperty]
        public AssetLocation[] BlockCodes;
        public int[] BlockIds;
        public int index;
    }

    public class WildgrassLayerConfig
    {
        public float RndWeight;
        public float PerlinWeight;
        public WildgrassSpecies[] Species;

        public static readonly string cacheKey = "wildgrass.WildgrassLayerConfig";
        public static WildgrassLayerConfig GetInstance(ICoreServerAPI api)
        {
            if(api.ObjectCache.TryGetValue(cacheKey, out var value)) {
                return value as WildgrassLayerConfig;
            }

            var asset = api.Assets.Get("wildgrass:worldgen/wildgrasses.json");
            var blockLayerConfig = asset.ToObject<WildgrassLayerConfig>();
            
            for(int i = 0; i < blockLayerConfig.Species.Length; i++) {
                ref var species = ref(blockLayerConfig.Species[i]);
                species.BlockIds = new int[species.BlockCodes.Length];
                species.index = i;
                for(int j = 0; j < species.BlockCodes.Length; j++) {
                    species.BlockIds[j] = api.WorldManager.GetBlockId(species.BlockCodes[j]);
                }
            }

            api.ObjectCache[cacheKey] = blockLayerConfig;
            return blockLayerConfig;
        }
    }
}