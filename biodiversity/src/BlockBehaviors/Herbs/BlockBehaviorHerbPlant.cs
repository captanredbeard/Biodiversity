using System.Collections.Generic;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace biodiversity.src.BlockBehaviors.Herbs;

public class HerbNutrients {
    public float N; public float P; public float K;
    public HerbNutrients(float n, float p, float k) { N = n; P = p; K = k; }
    public HerbNutrients() { }
    public HerbNutrients Clone() => new HerbNutrients(N,P,K);
    public static HerbNutrients operator *(HerbNutrients left, float right) => new HerbNutrients(left.N * right, left.P * right, left.K * right);
}

public class BlockBehaviorHerbPlant(Block block) : BlockBehavior(block)
{
    /// <summary>
    /// The amount of time, in seconds, it takes to harvest this block.
    /// </summary>
    [DocumentAsJson("Recommended", "0")]
    internal float harvestTime;

    /// <summary>
    /// The amount of time, in seconds, to take a cutting
    /// </summary>
    [DocumentAsJson("Recommended", "0")]
    internal float cuttingTime;

    /// <summary>
    /// An array of drops for when the block is harvested. If only using a single drop you can use <see cref="harvestedStack"/>, otherwise this property is required.
    /// </summary>
    [DocumentAsJson("Required")]
    internal BlockDropItemStack[]? harvestedStacks;

    /// <summary>
    /// The block required to harvest the block.
    /// </summary>
    [DocumentAsJson("Optional", "None")]
    public EnumTool? Tool;


    public float PauseGrowthBelowTemperature;
    public float PauseGrowthAboveTemperature;
    public float ResetGrowthBelowTemperature;
    public float ResetGrowthAboveTemperature;
    public float GoDormantBelowTemperature;
    public float LeaveDormantAboveTemperature;

    public JsonObject? GrowthProperties;
    public float GrowthRateMul = 1f;
    public AssetLocation? HarvestingSound;
    public required Dictionary<string, HerbNutrients> nutrientUseByHealthState;

    /// <summary>
    /// Sorted by growthstage index
    /// Young = 0, Mature = 1, Harvested = 2, Dormant = 3
    /// </summary>
    public required NatFloat[] growthStageMonths;

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        GrowthRateMul = (float)api.World.Config.GetDecimal("cropGrowthRateMul", GrowthRateMul);
        var attr = block.Attributes["growthProperties"][block.Variant["type"]];
        GrowthProperties = attr.Exists ? attr : block.Attributes["growthProperties"]["*"];
        PauseGrowthBelowTemperature = GrowthProperties["pauseGrowthBelowTemperature"].AsFloat(-999);
        PauseGrowthAboveTemperature = GrowthProperties["pauseGrowthAboveTemperature"].AsFloat(999);
        ResetGrowthBelowTemperature = GrowthProperties["resetGrowthBelowTemperature"].AsFloat(-999);
        ResetGrowthAboveTemperature = GrowthProperties["resetGrowthAboveTemperature"].AsFloat(999);
        GoDormantBelowTemperature = GrowthProperties["goDormantBelowTemperature"].AsFloat(-999);
        LeaveDormantAboveTemperature = GrowthProperties["leaveDormantAboveTemperature"].AsFloat(999);
        string? code = GrowthProperties["harvestingSound"].AsString("game:sounds/block/leafy-picking");
        if (code != null)
        {
            HarvestingSound = AssetLocation.Create(code, block.Code.Domain);
        }

        harvestTime = block.Attributes["harvestTime"].AsFloat(0.5f);
        cuttingTime = block.Attributes["cuttingTime"].AsFloat(2f);
        harvestedStacks = block.Attributes["harvestedStacks"].AsObject<BlockDropItemStack[]>(null);
        foreach (var hstack in harvestedStacks) hstack.Resolve(api.World, "harvested stack of herb plant", code);
        Tool = block.Attributes["harvestTool"].AsObject<EnumTool?>(null);

        growthStageMonths = new NatFloat[]
        {
            GrowthProperties["youngStageMonths"].AsObject<NatFloat>(),
            GrowthProperties["matureStageMonths"].AsObject<NatFloat>(),
            GrowthProperties["harvestedStageMonths"].AsObject<NatFloat>(),
            null
        };

        nutrientUseByHealthState = GrowthProperties["nutrientUseByHealthState"].AsObject<Dictionary<string, HerbNutrients>>();
    }



}
