using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Wildgrass;

public class ConfigData
{
    [JsonProperty]
    public float GenerateDensity = 1.0f;
    [JsonProperty]
    public Dictionary<AssetLocation, float> GenerateDensityPerSpecies;
    [JsonProperty]
    public float SeedMultiplier = 1.0f;
}

public class WildgrassConfig : ModSystem
{
    static ConfigData Data = new();

    public static float GenerateDensity => Data.GenerateDensity;

    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        ConfigData data = api.LoadModConfig<ConfigData>("wildgrass.json");
        if (data != null) {
            Data = data;
        }
    }

    public override void AssetsFinalize(ICoreAPI api)
    {
        base.AssetsFinalize(api);
        if (api is not ICoreServerAPI sapi) return;
        WildgrassCore wildgrass = WildgrassCore.GetInstance(api);
        foreach (BlockWildgrass block in wildgrass.WildgrassBlocks)
        {
            foreach (var drop in block.Drops)
            {
                if (drop.Code.BeginsWith("wildgrass", "seeds"))
                {
                    drop.Quantity.avg *= Data.SeedMultiplier;
                    drop.Quantity.var *= Data.SeedMultiplier;
                }
            }
        }

        WildgrassLayerConfig layerConfig = WildgrassLayerConfig.GetInstance(sapi);
        if (Data.GenerateDensityPerSpecies == null)
        {
            Data.GenerateDensityPerSpecies = new();
        }
        foreach (var species in layerConfig.Species)
        {
            if (!Data.GenerateDensityPerSpecies.ContainsKey(species.Code))
            {
                Data.GenerateDensityPerSpecies.Add(species.Code, 1.0f);
                continue;
            }
            species.Chance *= Data.GenerateDensityPerSpecies[species.Code];
        }
        api.StoreModConfig(Data, "wildgrass.json");
    }
}