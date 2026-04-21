using Cairo;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.ServerMods;
using Vintagestory.ServerMods.NoObf;

namespace biodiversity.src.System
{
    public class TreeGenCommands : ModSystem
    {
        private ICoreServerAPI api;

        private int _regionSize;

        private long _seed = 1239123912L;

        private int _chunksize;

        private WorldGenStructuresConfig _scfg;

        private WorldGenVillageConfig _vcfg;

        private int _regionChunkSize;

        TreeGeneratorsUtil treeGenerators;

        internal TreeVariant[] treeGenProps;

        public override bool ShouldLoad(EnumAppSide side)
        {
            return side == EnumAppSide.Server;
        }

        public override double ExecuteOrder()
        {
            return 0.34;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            this.api = api;

            treeGenerators = new TreeGeneratorsUtil(api);
            var treeGenProperties = api.Assets.Get("worldgen/treengenproperties.json").ToObject<TreeGenProperties>();

            treeGenProps = treeGenProperties.TreeGens.Concat(treeGenProperties.ShrubGens).ToArray();



            api.Event.SaveGameLoaded += OnGameWorldLoaded;
            if (this.api.Server.CurrentRunPhase == EnumServerRunPhase.RunGame)
            {
                OnGameWorldLoaded();
            }
            /*
            if (TerraGenConfig.DoDecorationPass)
            {
                api.Event.InitWorldGenerator(InitWorldGen, "standard");
            }
            */
            this.api.Event.ServerRunPhase(EnumServerRunPhase.RunGame, delegate
            {
                CommandArgumentParsers parsers = api.ChatCommands.Parsers;
                string[] array = api.World.TreeGenerators.Keys.Select((AssetLocation a) => a.Path).ToArray();
                string[] array2 = array.Prepend("*").ToArray();

                api.ChatCommands.GetOrCreate("tgen").RequiresPrivilege(Privilege.controlserver)
                    .BeginSubCommand("single")
                    .WithDescription("Generates a line of all tree variants of the treeWorldPropertyCode within it's worldgen size params")
                    .RequiresPlayer()
                    .WithArgs(parsers.Word("treeWorldPropertyCode", array), parsers.OptionalFloat("sizeincrement", 0.2f), parsers.OptionalFloat("horizontalspacing", 12), parsers.OptionalFloat("climatesuitability", 0f))
                    .HandleWith(OnCmdTreelineup)
                    .EndSubCommand()

                    .BeginSubCommand("multi")
                    .WithArgs(parsers.Word("treeWorldPropertyFilter", array2), parsers.OptionalFloat("sizeincrement", 0.2f), parsers.OptionalFloat("verticalspacing", 12), parsers.OptionalFloat("horizontalspacing", 12), parsers.OptionalFloat("climatesuitability", 0f))
                    .WithDescription("Generates all tree variants loaded ingame in a grid")
                    .RequiresPlayer()
                    .HandleWith(OnCmdMultiTreeVariants)
                    .EndSubCommand();
            });
        }

        private void OnGameWorldLoaded()
        {
            _regionSize = api.WorldManager.RegionSize;
        }

        private TextCommandResult OnCmdMultiTreeVariants(TextCommandCallingArgs args)
        {
            string filter = args[0] as string;
            float sizeincrement = (float)args[1];
            float verticalspacing = (float)args[2];
            float horizontalspacing = (float)args[3];
            float climatesuitability = (float)args[4];

            IServerPlayer player = args.Caller.Player as IServerPlayer;
            return MultiTreeVariants(player, filter, sizeincrement, verticalspacing, horizontalspacing, climatesuitability);
        }

        private TextCommandResult OnCmdTreelineup(TextCommandCallingArgs args)
        {
            string asset = args[0] as string;
            float sizeincrement = (float)args[1];
            float horizontalspacing = (float)args[2];
            float climatesuitability = (float)args[3];

            IServerPlayer player = args.Caller.Player as IServerPlayer;
            return SingleTreeVariant(player, asset, sizeincrement, player.Entity.Pos.HorizontalAheadCopy(25.0).AsBlockPos, horizontalspacing, climatesuitability);
        }

        private TextCommandResult SingleTreeVariant(IServerPlayer player, string asset, float sizeincrement, BlockPos asBlockPos, float horizontalspacing,float climateSuitability)
        {

            TreeVariant[] treeVariants = treeGenProps.Where(t => t.Generator.Equals(asset)).ToArray();

            var num2 = 0;

            //BlockPos asBlockPos = 

            IBlockAccessor blockAccessorBulkUpdate = api.World.GetBlockAccessorBulkUpdate(synchronize: true, relight: true, debug: true);
            AssetLocation treeName = new AssetLocation(asset);

            var rnd = new LCGRandom();

            int num = 12;
            for (int i = -2 * num; i < 2 * num; i++)
            {
                for (int j = -num; j < num; j++)
                {
                    for (int k = 0; k < 2 * num; k++)
                    {
                        blockAccessorBulkUpdate.SetBlock(0, asBlockPos.AddCopy(i, k, j));
                    }
                }
            }

            for (int i = 0; i < treeVariants.Length; i++)
            {
                var treeProps = treeVariants[i];
                var treeNum = (int)((treeProps.MaxSize + treeProps.SuitabilitySizeBonus - treeProps.MinSize) / sizeincrement);

                var s = 0f;
                //default sizes
                /*
                public float MinSize = 0.2f;
                public float MaxSize = 1f;
                public float SuitabilitySizeBonus = 0.5f;
                */

                treeGenerators.ReloadTreeGenerators();
                var realMaxSize = getSize(treeProps, 1f, climateSuitability);

                while (s >= 0 && s <= realMaxSize)
                {
                    treeGenerators.RunGenerator(treeName, blockAccessorBulkUpdate, asBlockPos.AddCopy(num2 * horizontalspacing, -1, 0), new TreeGenParams
                    {
                        size = getSize(treeProps, s, climateSuitability),
                    });
                    s += sizeincrement;
                    num2++;
                }
            }



            blockAccessorBulkUpdate.Commit();
            return TextCommandResult.Success();

        }

        private TextCommandResult MultiTreeVariants(IServerPlayer player, string filter, float sizeincrement, float verticalspacing, float horizontalspacing, float climateSuitability)
        {
            //Get a Deduplicated list of treekeys
            List<string> treeKeys = treeGenProps.Select(t => t.Generator.ToString()).Distinct().ToList();


            treeGenerators.LoadTreeGenerators();
            int i = 0;

            //get start position
            var asBlockPos = player.Entity.Pos.AsBlockPos;



            //remove entries except in fiter
            if (!filter.Equals("*") && filter != null)
            {
                treeKeys = treeKeys.Where(k => k.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            //offset the start position to leave the player at roughly the center.
            var offset = (treeKeys.Count / 2) * 12;
            asBlockPos.Add(0, 0, -offset);


            //Blacklist of common terms in treegencodes
            var blacklist = new[] { "large", "dead", "viney", "small", "dwarf" };

            var sortedBySimularity = treeKeys
                .OrderByDescending(key => treeKeys.Count(other => other != key &&
                AreStringsSimilar(
                  RemoveBlacklistedTerms(key, blacklist),
                  RemoveBlacklistedTerms(other, blacklist)
                )))
                .ToList();


            for (int j = 0; j < sortedBySimularity.Count; j++)
            {
                SingleTreeVariant(player, sortedBySimularity[j], sizeincrement, asBlockPos.AddCopy(0, 0, j * verticalspacing), horizontalspacing,climateSuitability);
            }
            if (filter == "*")
            {
                return TextCommandResult.Success(string.Concat("All possible variants generated."));
            }
            return TextCommandResult.Success(string.Concat("tree variants with filter {0} generated.", filter));
        }


        // Helper method to remove blacklisted substrings
        private static string RemoveBlacklistedTerms(string input, string[] blacklist)
        {
            var result = input;
            foreach (var term in blacklist)
            {
                result = result.Replace(term, "", StringComparison.OrdinalIgnoreCase);
            }
            return result;
        }

        // Helper method for similarity (simple substring matching)
        private static bool AreStringsSimilar(string a, string b)
        {
            return a.Contains(b, StringComparison.OrdinalIgnoreCase) ||
                   b.Contains(a, StringComparison.OrdinalIgnoreCase);
        }

        //VintageStory.Servermods.WgenTreeSupplier.GetRandomGenForClimate
        private static float getSize(TreeVariant treeGen, float random, float climateSuitability)
        {
            return treeGen.MinSize + random * (treeGen.MaxSize - treeGen.MinSize) + (GameMath.Clamp(0.7f - climateSuitability, 0f, 0.7f) * 1f / 0.7f * treeGen.SuitabilitySizeBonus);
        }
    }
}