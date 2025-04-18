using System;
using Newtonsoft.Json;
using Vintagestory.API.Common;

namespace Wildgrass;

public class ConfigData
{
    [JsonProperty]
    public float GenerateDensity = 1.0f;
    [JsonProperty]
    public float SeedMultiplier = 1.0f;
}

public class WildgrassConfig : ModSystem
{
    static ConfigData Data;

    public static float GenerateDensity => Data.GenerateDensity;

    public WildgrassConfig()
    {
        Data = new();
    }

    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        ConfigData data = api.LoadModConfig<ConfigData>("wildgrass.json");
        if(data != null) {
            Data = data;
        }
        api.StoreModConfig(Data, "wildgrass.json");
    }

    public override void AssetsFinalize(ICoreAPI api)
    {
        base.AssetsFinalize(api);
        WildgrassCore wildgrass = WildgrassCore.GetInstance(api);
        foreach(BlockWildgrass block in wildgrass.WildgrassBlocks) {
            foreach(var drop in block.Drops) {
                if(drop.Code.BeginsWith("wildgrass", "seeds")) {
                    drop.Quantity.avg *= Data.SeedMultiplier;
                    drop.Quantity.var *= Data.SeedMultiplier;
                }
            }
        }
    }
}