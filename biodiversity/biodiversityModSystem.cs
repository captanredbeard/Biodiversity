using biodiversity.src.BlockBehaviors;
using biodiversity.src.BlockBehaviors.Herbs;
using biodiversity.src.BlockEntities;
using biodiversity.src.BlockTypes;
using biodiversity.src.System;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace biodiversity
{
    public class biodiversityModSystem : ModSystem
    {

        public static ClientConfig cConfig = new ClientConfig();
        
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
        override public void StartClientSide(ICoreClientAPI api)
        {
            TryToLoadClientConfig(api);
        }

        private void TryToLoadClientConfig(ICoreAPI api)
        {
            try
            {
                cConfig = api.LoadModConfig<ClientConfig>("biodiversity/client.json");
                if (cConfig == null)
                {
                    cConfig = new ClientConfig();
                }
                api.StoreModConfig<ClientConfig>(cConfig, "biodiversity/client.json");
            }
            catch (Exception e)
            {
                Mod.Logger.Error("Could not load client config! Loading default settings instead.");
                Mod.Logger.Error(e);
                cConfig = new ClientConfig();
            }
        }
    }
}
