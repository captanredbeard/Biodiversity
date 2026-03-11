using biodiversity.src.BlockBehaviors.Herbs;
using biodiversity.src.BlockTypes;
using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace biodiversity.src.BlockEntities;

#nullable disable

public class BEBehaviorHerbPlant : BlockEntityBehavior, ILongInteractable, IHarvestable
{
    protected static readonly float[] NoNutrients = new float[3];

    
    protected BlockEntitySoilNutrition BESoil => Api.World.BlockAccessor.GetBlockEntity<BlockEntitySoilNutrition>(soilPos);
    protected float[] HerbNutrients => BESoil?.Nutrients ?? NoNutrients;
    protected ICoreClientAPI capi;
    protected BlockPos soilPos;
    protected BlockBehaviorHerbPlant bhHerb;
    protected double lastCheckAtTotalDays = 0;
    protected double transitionHoursLeft = -1;
    public HerbPlantState HState;


    public BEBehaviorHerbPlant(BlockEntity blockentity) : base(blockentity)
    {
        HState = new HerbPlantState();
    }
    public override void Initialize(ICoreAPI api, JsonObject properties)
    {
        base.Initialize(api, properties);

        soilPos = Blockentity.Pos.DownCopy();
        capi = api as ICoreClientAPI;

        bhHerb = Block.GetBehavior<BlockBehaviorHerbPlant>();

        if (api is ICoreServerAPI)
        {
            if (transitionHoursLeft <= 0)
            {
                transitionHoursLeft = GetHoursForNextStage();
                lastCheckAtTotalDays = api.World.Calendar.TotalDays;
            }

            //api.ModLoader.GetModSystem<POIRegistry>().AddPOI((IPointOfInterest)this);

            if (Block.Variant["state"] == "grown")
            {
                var belowBlock = api.World.BlockAccessor.GetBlock(Pos.DownCopy());
                var be = Api.World.BlockAccessor.GetBlockEntity(Pos.DownCopy());
                if (be == null && belowBlock.Fertility > 0)
                {
                    // Can't spawn right away, chunk thread crashes then
                    Blockentity.RegisterDelayedCallback(spawnBe, 500 + api.World.Rand.Next(500));
                }
                else
                {
                    if (!(be is BlockEntitySoilNutrition))
                    {
                        // Cannot grow here
                        Api.World.BlockAccessor.SetBlock(0, Pos);
                    }
                }
            }
        }
    }

    private void spawnBe(float dt)
    {
        var belowBlock = Api.World.BlockAccessor.GetBlock(Pos.DownCopy());
        var be = Api.World.BlockAccessor.GetBlockEntity(Pos.DownCopy());
        if (be == null && belowBlock.Fertility > 0)
        {
            Api.World.BlockAccessor.SpawnBlockEntity("BerryBushFarmland", Pos.DownCopy());
            be = Api.World.BlockAccessor.GetBlockEntity(Pos.DownCopy());
            if (be is BlockEntitySoilNutrition besn)
            {
                besn.OnCreatedFromSoil(belowBlock);
            }
        }
    }

    public void OnGrownFromCutting(string traits)
    {
        HState.WildHerbState = null;
        if (traits != null)
        {
            HState.Traits = traits.Split(",");
        }
    }

    public FarmlandFastForwardUpdate onUpdate()
    {
        double totalDays = Api.World.Calendar.TotalDays;
        if (totalDays < HState.MatureTotalDays) return null;

        if (GetHealthState() != HState.PrevHealthState)
        {
            HState.MeshDirty = true;
            Blockentity.MarkDirty(true);
        }

        HState.PrevHealthState = GetHealthState();

        if (HState.Growthstate == EnumHerbPlantGrowthState.Young)
        {
            Blockentity.MarkDirty(true);
            HState.Growthstate = EnumHerbPlantGrowthState.Mature;
        }

        return (double hourIntervall, ClimateCondition conds, double lightGrowthSpeedFactor, bool growthPaused) =>
        {
            transitionHoursLeft -= hourIntervall;

            if (HState.Growthstate == EnumHerbPlantGrowthState.Dormant)
            {
                if (conds.Temperature > bhHerb.LeaveDormantAboveTemperature)
                {
                    setGrowthState(EnumHerbPlantGrowthState.Mature);
                }
                return;
            }

            bool goDormant = conds.Temperature < bhHerb.GoDormantBelowTemperature;
            if (goDormant)
            {
                setGrowthState(EnumHerbPlantGrowthState.Dormant);
                return;
            }

            bool pause = conds.Temperature < bhHerb.PauseGrowthBelowTemperature || conds.Temperature > bhHerb.PauseGrowthAboveTemperature;
            if (pause) return;

            if (transitionHoursLeft <= 0)
            {
                // Looping through 1,2 ...
                setGrowthState((EnumHerbPlantGrowthState)(1 + GameMath.Mod((int)HState.Growthstate, 1)));

                if (HState.Growthstate == EnumHerbPlantGrowthState.Mature)
                {
                    consumeNutrients();
                }

                transitionHoursLeft = GetHoursForNextStage();
            }
        };
    }

    protected void consumeNutrients()
    {
        if (HState.WildHerbState != null) return;

        var hs = GetHealthState();
        HerbNutrients amount = bhHerb.nutrientUseByHealthState[hs.ToString().ToLowerInvariant()].Clone();
        
        amount *= getNutrientUseMul();

        float yearsAlive = (float)(Api.World.Calendar.TotalDays - HState.MatureTotalDays) / Api.World.Calendar.DaysPerYear;

        amount *= (float)Math.Pow(0.85f, yearsAlive);

        if (BESoil == null)
        {
            HState.WildHerbState = Api.World.Rand.NextDouble() < 0.5 ? EnumHerbPlantHealthState.Healthy : EnumHerbPlantHealthState.Struggling;
            return;
        }
        BESoil.ConsumeNutrients(EnumSoilNutrient.N, amount.N);
        BESoil.ConsumeNutrients(EnumSoilNutrient.P, amount.P);
        BESoil.ConsumeNutrients(EnumSoilNutrient.K, amount.K);
    }

    protected void setGrowthState(EnumHerbPlantGrowthState state)
    {
        HState.Growthstate = state;
        Blockentity.MarkDirty(true);
    }

    public virtual double GetHoursForNextStage()
    {
        if (HState.Growthstate == EnumHerbPlantGrowthState.Dormant) return 0; // Handled differently

        // Safety Code to prevent crashes

        if (bhHerb?.growthStageMonths == null)
        {
            Api.World.Logger.Warning("Block {0}: growthStageMonths is null", Block.Code);
            return 24; // fallback 1 day
        }

        int stageIndex = (int)HState.Growthstate;

        if (stageIndex < 0 || stageIndex >= bhHerb.growthStageMonths.Length)
        {
            Api.World.Logger.Warning("Block {0}: Growthstate {1} out of range", Block.Code, HState.Growthstate);
            return 24; // fallback 1 day
        }

        var nf = bhHerb.growthStageMonths[stageIndex];
        if (nf == null)
        {
            Api.World.Logger.Warning("Block {0}: growthStageMonths[{1}] is null", Block.Code, stageIndex);
            return 24; // fallback 1 day
        }

        //var nf = bhHerb.growthStageMonths[(int)HState.Growthstate];
        // Multiplier
        var mul = HState.Growthstate == EnumHerbPlantGrowthState.Mature ? getRipeTimeMul() : 1 / bhHerb.GrowthRateMul;
        return mul * nf.nextFloat() * Api.World.Calendar.DaysPerMonth * Api.World.Calendar.HoursPerDay;
    }

    public override void OnBlockPlaced(ItemStack byItemStack = null)
    {
        HState.PlantedTotalDays = Api.World.Calendar.TotalDays;

        // When grown from a cutting
        if (byItemStack == null)
        {
            HState.MatureTotalDays = HState.PlantedTotalDays + bhHerb.growthStageMonths[0].nextFloat() * Api.World.Calendar.DaysPerMonth;
        } else
        {
            HState.MatureTotalDays = HState.PlantedTotalDays;
        }

        if (byItemStack == null || byItemStack.Block.Variant["state"] == "wild")
        {
            HState.WildHerbState = Api.World.Rand.NextDouble() < 0.5 ? EnumHerbPlantHealthState.Healthy : EnumHerbPlantHealthState.Struggling;
            HState.Growthstate = 1 + (EnumHerbPlantGrowthState)GameMath.MurmurHash3Mod(Pos.X, Pos.Y + 1, Pos.Z, 4);
            genTraits(Api.World);
        }
        else
        {
            HState.Growthstate = EnumHerbPlantGrowthState.Mature;
            HState.Traits = [];
        }
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        var healthState = GetHealthState();

        dsc.AppendLine(Lang.Get("Health state: {0}", Lang.Get("healthstate-" + healthState.ToString().ToLowerInvariant())));
        dsc.AppendLine(Lang.Get("Growth state: {0}", Lang.Get("growthstate-" + HState.Growthstate.ToString().ToLowerInvariant())));

        var bens = Api.World.BlockAccessor.GetBlockEntity<BlockEntitySoilNutrition>(Pos.DownCopy());
        if (bens != null)
        {
            bens.GetBlockInfo(forPlayer, dsc);
        }

        if (HState.Traits.Length > 0)
        {
            dsc.AppendLine();
        }
        int i = 0;
        foreach (var val in HState.Traits)
        {
            if (i++ > 0) dsc.Append(", ");
            dsc.Append(Lang.Get("{0}", Lang.Get("trait-" + val.ToLowerInvariant())));
        }
        if (HState.Traits.Length > 0)
        {
            dsc.AppendLine();
        }
    }


    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        base.FromTreeAttributes(tree, worldAccessForResolve);
        HState.FromTreeAttributes(tree, worldAccessForResolve);

        if (HState.Traits == null && worldAccessForResolve.Side == EnumAppSide.Server)
        {
            genTraits(worldAccessForResolve);
        }
    }

    private void genTraits(IWorldAccessor world)
    {
        HState.Traits = [];
        for (int i = 0; i < HerbPlantState.AllTraits.Length; i++)
        {
            var t = HerbPlantState.AllTraits[i];
            if (world.Rand.NextDouble() < 0.15)
            {
                // 60% chance of bad trat, 40% chance for good one
                HState.Traits = HState.Traits.Append(t[world.Rand.NextDouble() < 0.6 ? 0 : 1]);
            }
        }
    }

    protected virtual float getHarvestTimeMul()
    {
        if (HState.Traits.Contains("lethargic")) return 1.35f;
        if (HState.Traits.Contains("vigorous")) return 0.65f;
        return 1;
    }
    protected virtual float getYieldMul()
    {
        if (HState.Traits.Contains("lush")) return 1.15f;
        if (HState.Traits.Contains("sparse")) return 0.85f;
        return 1;
    }

    protected virtual float getNutrientUseMul()
    {
        if (HState.Traits.Contains("weakrooted")) return 1.3f;
        if (HState.Traits.Contains("strongrooted")) return 0.7f;
        return 1;
    }

    protected virtual float getRipeTimeMul()
    {
        return 1;
    }



    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        HState.ToTreeAttributes(tree);
    }

    internal EnumHerbPlantHealthState GetHealthState()
    {
        if (HState.WildHerbState != null) return (EnumHerbPlantHealthState)HState.WildHerbState;
        float avg = (HerbNutrients[0] + HerbNutrients[1] + HerbNutrients[2]) / 3f / 100f;
        if (avg < 0.1) return EnumHerbPlantHealthState.Barren;
        if (avg < 0.3) return EnumHerbPlantHealthState.Struggling;
        if (avg < 0.8) return EnumHerbPlantHealthState.Healthy;
        return EnumHerbPlantHealthState.Bountiful;
    }

    #region Interact

    public bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
    {
        var besn = world.BlockAccessor.GetBlockEntity<BlockEntitySoilNutrition>(Pos.DownCopy());
        if (HState.WildHerbState == null && besn?.OnBlockInteract(byPlayer) == true)
        {
            handling = EnumHandling.PreventDefault;
            Blockentity.MarkDirty(true);
            return true;
        }

        if (HState.Growthstate == EnumHerbPlantGrowthState.Mature)
        {
            handling = EnumHandling.PreventDefault;
            return true;
        }

        return false;
    }


    public bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
    {
        if (HState.Growthstate == EnumHerbPlantGrowthState.Mature)
        {
            handling = EnumHandling.PreventDefault;
            playHarvestEffects(byPlayer, blockSel, bhHerb.harvestedStacks[0].ResolvedItemstack);
            return world.Side == EnumAppSide.Client ? secondsUsed < bhHerb.harvestTime * getHarvestTimeMul() : true;
        }

        return false;
    }

    protected void playHarvestEffects(IPlayer byPlayer, BlockSelection blockSel, ItemStack particlestack)
    {
        IWorldAccessor world = Api.World;
        if (world.Rand.NextDouble() < 0.05)
        {
            world.PlaySoundAt(bhHerb.HarvestingSound, blockSel.Position, 0, byPlayer);
        }

        if (world.Side == EnumAppSide.Client)
        {
            (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
            if (world.Rand.NextDouble() < 0.25)
            {
                if (particlestack != null)
                {
                    world.SpawnCubeParticles(blockSel.Position.ToVec3d().Add(blockSel.HitPosition), particlestack, 0.25f, 1, 0.5f, byPlayer, new Vec3f(0, 1, 0));
                } else
                {
                    world.SpawnCubeParticles(Pos, Pos.ToVec3d(), 0.25f, 1, 0.5f, byPlayer, new Vec3f(0, 1, 0));
                }
            }
        }
    }

    float[] dropRates = [0f, 0.5f, 1f, 1.5f];

    public void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
    {
        if (HState.Growthstate == EnumHerbPlantGrowthState.Mature)
        {
            handling = EnumHandling.PreventDefault;
            if (secondsUsed > bhHerb.harvestTime * getHarvestTimeMul() - 0.05f && bhHerb.harvestedStacks != null && world.Side == EnumAppSide.Server)
            {
                float dropRate = getYieldMul();

                if (Block.Attributes?.IsTrue("forageStatAffected") == true)
                {
                    dropRate *= byPlayer.Entity.Stats.GetBlended("forageDropRate");
                }

                bhHerb.harvestedStacks.Foreach(harvestedStack =>
                {
                    ItemStack stack = harvestedStack.GetNextItemStack(dropRate);
                    if (stack == null) return;
                    var origStack = stack.Clone();

                    stack.StackSize = GameMath.RoundRandom(Api.World.Rand, stack.StackSize * dropRates[(int)GetHealthState()]);

                    var quantity = stack.StackSize;
                    if (!byPlayer.InventoryManager.TryGiveItemstack(stack))
                    {
                        world.SpawnItemEntity(stack, blockSel.Position);
                    }
                    world.Logger.Audit("{0} Took {1}x{2} from {3} at {4}.",
                        byPlayer.PlayerName,
                        quantity,
                        stack.Collectible.Code,
                        Block.Code,
                        blockSel.Position
                    );

                    TreeAttribute tree = new TreeAttribute();
                    tree["itemstack"] = new ItemstackAttribute(origStack.Clone());
                    tree["byentityid"] = new LongAttribute(byPlayer.Entity.EntityId);
                    world.Api.Event.PushEvent("onitemcollected", tree);
                });

                if (bhHerb.Tool != null)
                {
                    var toolSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
                    toolSlot.Itemstack?.Collectible.DamageItem(world, byPlayer.Entity, toolSlot);
                }

                world.PlaySoundAt(bhHerb.HarvestingSound, blockSel.Position, 0, byPlayer);

                HState.Growthstate = EnumHerbPlantGrowthState.Harvested;
                Blockentity.MarkDirty(true);
            }
        }
    }


    public bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
    {
        return false;
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
    {
        if (BESoil != null)
        {
            var tfMatrix = new Matrixf().Translate(0, -15 / 16f, 0).Values;
            mesher.AddMeshData(BESoil.FertilizerQuad, tfMatrix);
        }

        return base.OnTesselation(mesher, tessThreadTesselator);
    }


    #endregion

    #region Knife cutting
    public bool IsHarvestable(ItemSlot slot, Entity forEntity)
    {
        return slot.Itemstack?.Collectible.GetTool(slot) == EnumTool.Knife && (Api.World.Calendar.TotalDays - HState.LastCuttingTakenTotalDays) / Api.World.Calendar.DaysPerYear >= 1;
    }

    public AssetLocation HarvestableSound => bhHerb.HarvestingSound;

    public float GetHarvestDuration(ItemSlot slot, Entity forEntity)
    {
        // Happens to also get called during InteractStep
        var eplr = forEntity as EntityPlayer;
        playHarvestEffects(eplr?.Player, eplr.BlockSelection, null);

        return bhHerb.cuttingTime;
    }

    public void SetHarvested(IPlayer byPlayer, float dropQuantityMultiplier = 1)
    {
        var block = Api.World.GetBlock(AssetLocation.Create(Block.Attributes["cuttingBlockCode"].AsString(), Block.Code.Domain));
        var cuttingStack = new ItemStack(block);

        cuttingStack.Attributes.SetString("traits", string.Join(",", HState.Traits));

        if (!byPlayer.InventoryManager.TryGiveItemstack(cuttingStack))
        {
            Api.World.SpawnItemEntity(cuttingStack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
        }
        HState.LastCuttingTakenTotalDays = Api.World.Calendar.TotalDays;
        Blockentity.MarkDirty(true);
    }

    


    #endregion

}
