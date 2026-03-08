using biodiversity.src.BlockEntities;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace biodiversity.src.BlockBehaviors
{
    public class MelonCropBehavior : CropBehavior
    {
        private int vineGrowthStage = 3;

        private float vineGrowthQuantity;

        private AssetLocation vineBlockLocation;

        private NatFloat vineGrowthQuantityGen;

        public string melonBlockCode;
        public string domainCode;

        public MelonCropBehavior(Block block)
            : base(block)
        {
        }

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);

            melonBlockCode = properties["melonBlockCode"].AsString();
            domainCode = properties["domainCode"].AsString();

            vineGrowthStage = properties["vineGrowthStage"].AsInt();
            vineGrowthQuantityGen = properties["vineGrowthQuantity"].AsObject<NatFloat>();
            vineBlockLocation = new AssetLocation(domainCode+":"+melonBlockCode+"-vine-1-normal");
        }

        public override void OnPlanted(ICoreAPI api, ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel)
        {
            vineGrowthQuantity = vineGrowthQuantityGen.nextFloat(1f, api.World.Rand);
        }

        public override bool TryGrowCrop(ICoreAPI api, IFarmlandBlockEntity farmland, double currentTotalHours, int newGrowthStage, ref EnumHandling handling)
        {
            if (vineGrowthQuantity == 0f)
            {
                vineGrowthQuantity = farmland.CropAttributes.GetFloat("vineGrowthQuantity", vineGrowthQuantityGen.nextFloat(1f, api.World.Rand));
                farmland.CropAttributes.SetFloat("vineGrowthQuantity", vineGrowthQuantity);
            }

            handling = EnumHandling.PassThrough;
            if (newGrowthStage >= vineGrowthStage)
            {
                if (newGrowthStage == 8)
                {
                    bool flag = true;
                    BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
                    foreach (BlockFacing facing in hORIZONTALS)
                    {
                        Block block = api.World.BlockAccessor.GetBlock(farmland.Pos.AddCopy(facing).Up());
                        if (block.Code.PathStartsWith(melonBlockCode + "-vine"))
                        {
                            flag &= block.LastCodePart() == "withered";
                        }
                    }

                    if (!flag)
                    {
                        handling = EnumHandling.PreventDefault;
                    }

                    return false;
                }

                if (api.World.Rand.NextDouble() < (double)vineGrowthQuantity)
                {
                    return TrySpawnVine(api, farmland, currentTotalHours);
                }
            }

            return false;
        }

        private bool TrySpawnVine(ICoreAPI api, IFarmlandBlockEntity farmland, double currentTotalHours)
        {
            BlockPos upPos = farmland.UpPos;
            BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
            foreach (BlockFacing facing in hORIZONTALS)
            {
                BlockPos blockPos = upPos.AddCopy(facing);
                Block block = api.World.BlockAccessor.GetBlock(blockPos);
                if (CanReplace(block) && CanSupportMelon(api, blockPos.DownCopy()))
                {
                    DoSpawnVine(api, blockPos, upPos, facing, currentTotalHours);
                    return true;
                }
            }

            return false;
        }

        private void DoSpawnVine(ICoreAPI api, BlockPos vinePos, BlockPos motherplantPos, BlockFacing facing, double currentTotalHours)
        {
            Block block = api.World.GetBlock(vineBlockLocation);
            api.World.BlockAccessor.SetBlock(block.BlockId, vinePos);
            if (api.World is IServerWorldAccessor)
            {
                BlockEntity blockEntity = api.World.BlockAccessor.GetBlockEntity(vinePos);
                if (blockEntity is BEMelonVine)
                {
                    ((BEMelonVine)blockEntity).CreatedFromParent(motherplantPos, facing, currentTotalHours);
                }
            }
        }

        private bool CanReplace(Block block)
        {
            if (block == null)
            {
                return true;
            }

            if (block.Replaceable >= 6000)
            {
                return !block.Code.GetName().Contains(melonBlockCode);
            }

            return false;
        }

        public static bool CanSupportMelon(ICoreAPI api, BlockPos pos)
        {
            if (api.World.BlockAccessor.GetBlock(pos, 2).IsLiquid())
            {
                return false;
            }

            return api.World.BlockAccessor.GetBlock(pos).Replaceable <= 5000;
        }
    }
}