using biodiversity.src.BlockBehaviors;
using biodiversity.src.BlockBehaviors.Herbs;
using biodiversity.src.BlockEntities;
using biodiversity.src.BlockTypes;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace biodiversity
{
    public class biodiversityModSystem : ModSystem
    {
        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api)
        {
            var modID = Mod.Info.ModID;
            
            api.RegisterBlockEntityClass(modID + ".MelonVine", typeof(BEMelonVine));
            api.RegisterCropBehavior(modID + ".BlockCropVine", typeof(MelonCropBehavior));

            //Multiblock Vines
            api.RegisterBlockClass(modID + ".BlockMultiSideVines", typeof(BlockMultiSideVines));

            api.RegisterBlockEntityBehaviorClass(modID + ".HerbPlant", typeof(BEBehaviorHerbPlant));
            api.RegisterBlockEntityBehaviorClass(modID + ".HerbPlantMesh", typeof(BEBehaviorHerbPlantMesh));
            api.RegisterBlockBehaviorClass(modID + ".herbplant", typeof(BlockBehaviorHerbPlant));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            //Mod.Logger.Notification("Hello from template mod server side: " + Lang.Get("biodiversity:hello"));
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            //Mod.Logger.Notification("Hello from template mod client side: " + Lang.Get("biodiversity:hello"));
        }
    }
}
