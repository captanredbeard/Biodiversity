using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace biodiversity.src.BlockTypes;


public interface ICrop
{
    bool Mature { get; }
    EnumSoilNutrient RequiredNutrient { get; }
}

public enum EnumHerbPlantHealthState
{
    Barren,
    Struggling,
    Healthy,
    Bountiful
}

public enum EnumHerbPlantGrowthState
{
    Young = 0,
    Mature = 1,
    Harvested = 2,
    Dormant = 3
}

public class HerbPlantState
{
    public static string[][] AllTraits = new string[][] {
        ["sparse", "lush"],
        ["weakrooted", "strongrooted"],
        ["lethargic", "vigorous"]
    };

    /// <summary>
    /// When the herb was planted
    /// </summary>
    public double PlantedTotalDays;
    /// <summary>
    /// When it matured or when it will mature
    /// </summary>
    public double MatureTotalDays;
    /// <summary>
    /// What growth state the bush is in
    /// </summary>
    EnumHerbPlantGrowthState growthstate;
        /// <summary>
    /// What traits this bush has
    /// </summary>
    public string[]? Traits;

    public EnumHerbPlantHealthState PrevHealthState;

    public EnumHerbPlantGrowthState Growthstate
    {
        get { return growthstate; }
        set
        {
            if (value != growthstate) { MeshDirty = true; }
            growthstate = value;
        }
    }
    /// <summary>
    /// For wild herbs: What (always static) health state it is in
    /// </summary>
    EnumHerbPlantHealthState? wildHerbState;
    public EnumHerbPlantHealthState? WildHerbState {
        get { return wildHerbState; }
        set
        {
            if (value != wildHerbState) { MeshDirty = true; }
            wildHerbState = value;
        }
    }

    public bool MeshDirty = false;
    public double LastCuttingTakenTotalDays = -99999;

    public void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        PlantedTotalDays = tree.GetDouble("plantedTotalDays");
        MatureTotalDays = tree.GetDouble("matureTotalDays");
        LastCuttingTakenTotalDays = tree.GetDouble("lastCuttingTakenTotalDays", -99999);
        Growthstate = (EnumHerbPlantGrowthState)tree.GetInt("growthState");
        if (tree.HasAttribute("traits"))
        {
            if (tree.GetString("traits").Length == 0) Traits = [];
            Traits = tree.GetString("traits").Split(",");
        }
        else Traits = [];

        WildHerbState = null;
        if (tree.HasAttribute("wildHerbState")) WildHerbState = (EnumHerbPlantHealthState)tree.GetInt("wildHerbState");
    }

    public void ToTreeAttributes(ITreeAttribute tree)
    {
        tree.SetDouble("plantedTotalDays", PlantedTotalDays);
        tree.SetDouble("matureTotalDays", MatureTotalDays);
        tree.SetDouble("lastCuttingTakenTotalDays", LastCuttingTakenTotalDays);
        tree.SetInt("growthState", (int)Growthstate);
        if (Traits != null && Traits.Length > 0) tree.SetString("traits", string.Join(",", Traits));
        if (WildHerbState != null) tree.SetInt("wildHerbState", (int)WildHerbState);
    }
}
