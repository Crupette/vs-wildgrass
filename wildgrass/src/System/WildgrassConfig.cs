using System;
using Vintagestory.API.Common;

namespace Wildgrass;

public class ConfigData
{
    public float GenerateDensity = 1.0f;
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
}