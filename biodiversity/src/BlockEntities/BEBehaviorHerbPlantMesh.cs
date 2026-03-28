using biodiversity.src.BlockBehaviors;
using biodiversity.src.BlockTypes;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace biodiversity.src.BlockEntities;

public class BEBehaviorHerbPlantMesh : BlockEntityBehavior, ITexPositionSource
{
    protected ICoreClientAPI capi;
    public Size2i AtlasSize => capi.BlockTextureAtlas.Size;
    public TextureAtlasPosition this[string textureCode]
    {
        get
        {
            if (!textureMapping.TryGetValue(textureCode, out var textureTemplate))
            {
                // Key not found, log warning and skip
                textureTemplate = "missing"; // or some default texture path
                capi.Logger.Warning("Texture code '{0}' not found in block '{1}'", textureCode, Block.Code);
            }

            var texturePathHerb = textureMapping[textureCode]
                .Replace("{type}", Block.Variant["type"])
                .Replace("{variant}", textureVariant)
                .Replace("{herbstage}", herbStage)
                .Replace("{healthstate}", healthState)
            ;
            var loc = AssetLocation.Create(texturePathHerb, Block.Code.Domain);
            capi.BlockTextureAtlas.GetOrInsertTexture(loc, out _, out var texPos);
            return texPos;
        }
    }

    public HerbPlantState HState => Blockentity.GetBehavior<BEBehaviorHerbPlant>().HState;
    protected string textureVariant;
    protected string herbStage;
    protected string healthState;

    public BEBehaviorHerbPlantMesh(BlockEntity blockentity) : base(blockentity)
    {
        // Defensive check at runtime
        ArgumentNullException.ThrowIfNull(blockentity);

    }

    public override void Initialize(ICoreAPI api, JsonObject properties)
    {
        base.Initialize(api, properties);
        capi = api as ICoreClientAPI;
    }


    MeshData? herbMesh;
    Dictionary<string, string>? textureMapping;

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
    {
        ensureMeshExists();
        mesher.AddMeshData(herbMesh);
        return true;
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        base.FromTreeAttributes(tree, worldAccessForResolve);     

        if (HState.MeshDirty)
        {
            HState.MeshDirty = false;
            herbMesh = null;
            Blockentity.MarkDirty(true);
        }
    }

    protected virtual string meshCacheKey => Block.Code + "-" + HState.Growthstate + "-" + healthState + "-" + textureVariant;
    protected void ensureMeshExists()
    {
        if (herbMesh != null) return;
        textureVariant = "" + (1+GameMath.MurmurHash3Mod(Pos.X, Pos.Y, Pos.Z, Block.Attributes["textureVariants"].AsInt()));
        var healthState = Blockentity.GetBehavior<BEBehaviorHerbPlant>().GetHealthState();
        this.healthState = healthState.ToString().ToLowerInvariant();

        herbMesh = ObjectCacheUtil.GetOrCreate(capi, meshCacheKey, () =>
        {
            string[]? ignoreElements = null;

            switch (HState.Growthstate)
            {
                case EnumHerbPlantGrowthState.Harvested: herbStage = "Harvested"; break;
                case EnumHerbPlantGrowthState.Mature: herbStage = "Mature"; break;
                default: ignoreElements = new string[] { "Herbs/*" }; break;
            }

            if (healthState == EnumHerbPlantHealthState.Barren) ignoreElements = new string[] { "Herbs/*" };

            textureMapping = Block.Attributes["textureMapping"].AsObject<Dictionary<string, string>>();

            var loc = Block.Shape.Base;
            var shape = capi.Assets.Get<Shape>(loc.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json"));

            capi.Tesselator.TesselateShape(new TesselationMetaData()
            {
                TexSource = this,
                UsesColorMap = true,
                IgnoreElements = ignoreElements
            }, shape, out var herbMesh);

            for (int i = 0; i < herbMesh.ColorMapIdsCount; i++)
            {
                if (herbMesh.ClimateColorMapIds[i] > 0) herbMesh.ClimateColorMapIds[i] = (byte)(Block.ClimateColorMapResolved.RectIndex + 1);
                if (herbMesh.SeasonColorMapIds[i] > 0) herbMesh.SeasonColorMapIds[i] = (byte)(Block.SeasonColorMapResolved.RectIndex + 1);
            }

            return herbMesh;
        });
    }




}
