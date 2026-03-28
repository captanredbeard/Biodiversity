using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;


namespace biodiversity.src.BlockTypes
{
    public class BlockMultiSideVines : Block
    {
        public BlockFacing VineFacing;

        public string extraSides;

        private int[] origWindMode;

        private BlockPos tmpPos = new BlockPos(0);

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            VineFacing = BlockFacing.FromCode(Variant["horizontalorientation"]);
            extraSides = Code.EndVariant();
        }
        /*
        public override void OnDecalTesselation(IWorldAccessor world, MeshData decalMesh, BlockPos pos)
        {
            int verticesCount = decalMesh.VerticesCount;
            IBlockAccessor blockAccessor = api.World.BlockAccessor;
            Block blockOnSide = blockAccessor.GetBlockOnSide(pos, VineFacing.Opposite);
            if (blockOnSide.Id != 0 && blockOnSide.CanAttachBlockAt(blockAccessor, this, tmpPos.Set(pos, pos.dimension).Add(VineFacing.Opposite), VineFacing) && !(blockOnSide is BlockLeaves))
            {
                for (int i = 0; i < verticesCount; i++)
                {
                    decalMesh.Flags[i] &= -503316481;
                }

                return;
            }

            int num = ((blockAccessor.GetBlockAbove(pos, 1, 1) is BlockMultiSideVines) ? 1 : 0) + ((blockAccessor.GetBlockAbove(pos, 2, 1) is BlockMultiSideVines) ? 1 : 0) + ((blockAccessor.GetBlockAbove(pos, 3, 1) is BlockMultiSideVines) ? 1 : 0);
            int windDatam = ((num != 3 || !(blockAccessor.GetBlockAbove(pos, 4, 1) is BlockMultiSideVines)) ? (Math.Max(0, num - 1) << 29) : (num << 29));
            num <<= 29;
            if (blockAccessor.GetBlockAbove(pos, 1, 1) is BlockMultiSideVines)
            {
                tmpPos.Set(pos, pos.dimension).Up().Add(VineFacing.Opposite);
                Block block = blockAccessor.GetBlock(tmpPos);
                if (block.Id != 0 && block.CanAttachBlockAt(blockAccessor, this, tmpPos, VineFacing) && !(blockOnSide is BlockLeaves))
                {
                    for (int j = 0; j < verticesCount; j++)
                    {
                        if ((double)decalMesh.xyz[j * 3 + 1] > 0.5)
                        {
                            decalMesh.Flags[j] &= -503316481;
                        }
                        else
                        {
                            decalMesh.Flags[j] = (decalMesh.Flags[j] & 0x1FFFFFF) | origWindMode[j] | num;
                        }
                    }

                    return;
                }
            }

            //otherwiseAllWave(decalMesh, verticesCount, num, windDatam);
        }
        */
        public override void OnJsonTesselation(ref MeshData sourceMesh, ref int[] lightRgbsByCorner, BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
        {
            if (origWindMode == null)
            {
                int flagsCount = sourceMesh.FlagsCount;
                origWindMode = (int[])sourceMesh.Flags.Clone();
                for (int i = 0; i < flagsCount; i++)
                {
                    origWindMode[i] &= 503316480;
                }
            }

            int verticesCount = sourceMesh.VerticesCount;
            bool num = ((lightRgbsByCorner[24] >> 24) & 0xFF) >= 159;
            Block block = chunkExtBlocks[extIndex3d + TileSideEnum.MoveIndex[VineFacing.Opposite.Index]];
            if (!num || (block.Id != 0 && block.CanAttachBlockAt(api.World.BlockAccessor, this, tmpPos.Set(pos, pos.dimension).Add(VineFacing.Opposite), VineFacing) && !(block is BlockLeaves)))
            {
                for (int j = 0; j < verticesCount; j++)
                {
                    sourceMesh.Flags[j] &= -503316481;
                }

                return;
            }

            int num2 = ((api.World.BlockAccessor.GetBlockAbove(pos, 1, 1) is BlockMultiSideVines) ? 1 : 0) + ((api.World.BlockAccessor.GetBlockAbove(pos, 2, 1) is BlockMultiSideVines) ? 1 : 0) + ((api.World.BlockAccessor.GetBlockAbove(pos, 3, 1) is BlockMultiSideVines) ? 1 : 0);
            int windDatam = ((num2 != 3 || !(api.World.BlockAccessor.GetBlockAbove(pos, 4, 1) is BlockMultiSideVines)) ? (Math.Max(0, num2 - 1) << 29) : (num2 << 29));
            num2 <<= 29;
            if (chunkExtBlocks[extIndex3d + TileSideEnum.MoveIndex[BlockFacing.UP.Index]] is BlockMultiSideVines)
            {
                Block block2 = chunkExtBlocks[extIndex3d + TileSideEnum.MoveIndex[VineFacing.Opposite.Index] + TileSideEnum.MoveIndex[BlockFacing.UP.Index]];
                if (block2.Id != 0 && block2.CanAttachBlockAt(api.World.BlockAccessor, this, tmpPos.Set(pos, pos.dimension).Up().Add(VineFacing.Opposite), VineFacing) && !(block is BlockLeaves))
                {
                    for (int k = 0; k < verticesCount; k++)
                    {
                        if ((double)sourceMesh.xyz[k * 3 + 1] > 0.5)
                        {
                            sourceMesh.Flags[k] &= -503316481;
                        }
                        else
                        {
                            sourceMesh.Flags[k] = (sourceMesh.Flags[k] & 0x1FFFFFF) | origWindMode[k] | num2;
                        }
                    }

                    return;
                }
            }

            //otherwiseAllWave(sourceMesh, verticesCount, num2, windDatam);
        }
        /*
        private void otherwiseAllWave(MeshData decalMesh, int verticesCount, int windData, int windDatam1)
        {
            for (int i = 0; i < verticesCount; i++)
            {
                if ((double)decalMesh.xyz[i * 3 + 1] > 0.5)
                {
                    decalMesh.Flags[i] = (decalMesh.Flags[i] & 0x1FFFFFF) | origWindMode[i] | windDatam1;
                }
                else
                {
                    decalMesh.Flags[i] = (decalMesh.Flags[i] & 0x1FFFFFF) | origWindMode[i] | windData;
                }
            }
        }
        
        public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldGenRand, BlockPatchAttributes attributes = null)
        {
            if (!blockAccessor.GetBlock(pos).IsReplacableBy(this))
            {
                return false;
            }

            if (onBlockFace.IsHorizontal && TryAttachTo(blockAccessor, pos, onBlockFace))
            {
                return true;
            }

            Block blockAbove = blockAccessor.GetBlockAbove(pos, 1, 1);
            if (blockAbove is BlockMultiSideVines)
            {
                BlockFacing vineFacing = ((BlockMultiSideVines)blockAbove).VineFacing;
                blockAccessor.SetBlock(blockAccessor.GetBlock(CodeWithParts(vineFacing.Code))?.BlockId ?? blockAbove.BlockId, pos);
                return true;
            }

            for (int i = 0; i < BlockFacing.HORIZONTALS.Length; i++)
            {
                if (TryAttachTo(blockAccessor, pos, BlockFacing.HORIZONTALS[i]))
                {
                    return true;
                }
            }

            return false;
        }
        */
        
        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
            {
                return false;
            }

            if (blockSel.Face.IsHorizontal && TryAttachTo(world.BlockAccessor, blockSel.Position, blockSel.Face))
            {
                return true;
            }

            Block blockAbove = world.BlockAccessor.GetBlockAbove(blockSel.Position, 1, 1);
            if (blockAbove is BlockMultiSideVines)
            {
                BlockFacing vineFacing = ((BlockMultiSideVines)blockAbove).VineFacing;
                Block block = world.BlockAccessor.GetBlock(CodeWithParts(vineFacing.Code));
                world.BlockAccessor.SetBlock(block?.BlockId ?? blockAbove.BlockId, blockSel.Position);
                return true;
            }

            failureCode = "requirevineattachable";
            return false;
        }
        
        public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
        {
            return GetHandbookDropsFromBreakDrops(handbookStack, forPlayer);
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            string[] array = Code.Path.Split('-');
            Block block = world.BlockAccessor.GetBlock(new AssetLocation(Code.ShortDomain()+":"+array[0] + "-" + array[^3].Replace("end", "section") + "-north-0"));
            return new ItemStack[1]
            {
            new ItemStack(block)
            };
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            string[] array = Code.Path.Split('-');
            return new ItemStack(world.BlockAccessor.GetBlock(new AssetLocation(Code.ShortDomain()+":"+array[0] + "-" + array[^3] + "-north-0")));
        }

        private bool TryAttachTo(IBlockAccessor blockAccessor, BlockPos blockpos, BlockFacing onBlockFace)
        {
            BlockPos pos = blockpos.AddCopy(onBlockFace.Opposite);
            if (blockAccessor.GetBlock(pos).CanAttachBlockAt(blockAccessor, this, pos, onBlockFace))
            {
                int blockId = blockAccessor.GetBlock(CodeWithParts(onBlockFace.Code)).BlockId;
                blockAccessor.SetBlock(blockId, blockpos);
                return true;
            }

            return false;
        }

        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            if (!CanVineStay(world, pos))
            {
                world.BlockAccessor.SetBlock(0, pos);
                world.BlockAccessor.TriggerNeighbourBlockUpdate(pos);
            }
        }

        private bool CanVineStay(IWorldAccessor world, BlockPos pos)
        {
            BlockPos pos2 = pos.AddCopy(VineFacing.Opposite);
            if (!world.BlockAccessor.GetBlock(pos2).CanAttachBlockAt(world.BlockAccessor, this, pos2, VineFacing))
            {
                return world.BlockAccessor.GetBlock(pos.UpCopy()) is BlockMultiSideVines;
            }

            return true;
        }

        public override AssetLocation GetRotatedBlockCode(int angle)
        {
            BlockFacing blockFacing = BlockFacing.FromCode(LastCodePart(1));
            int k = ((angle == 180) ? blockFacing.HorizontalAngleIndex : blockFacing.Opposite.HorizontalAngleIndex) + angle / 90;
            BlockFacing blockFacing2 = BlockFacing.HORIZONTALS_ANGLEORDER[GameMath.Mod(k, 4)];
            return CodeWithParts(blockFacing2.Code);
        }

        public override bool ShouldReceiveServerGameTicks(IWorldAccessor world, BlockPos pos, Random offThreadRandom, out object extra)
        {
            extra = null;
            if (offThreadRandom.NextDouble() > 0.1)
            {
                return false;
            }

            BlockFacing opposite = VineFacing.Opposite;
            BlockPos blockPos = pos.AddCopy(opposite);
            Block block = world.BlockAccessor.GetBlock(blockPos);
            if (block.CanAttachBlockAt(world.BlockAccessor, this, blockPos, VineFacing) || block is BlockLeaves)
            {
                return false;
            }

            blockPos.Set(pos);
            int i;
            for (i = 0; i < 5; i++)
            {
                blockPos.Y++;
                Block block2 = world.BlockAccessor.GetBlock(blockPos);
                if (block2 is BlockLeaves || block2.CanAttachBlockAt(world.BlockAccessor, this, blockPos, BlockFacing.DOWN))
                {
                    return false;
                }

                if (!(block2 is BlockMultiSideVines))
                {
                    break;
                }

                if (world.BlockAccessor.GetBlockOnSide(blockPos, opposite).CanAttachBlockAt(world.BlockAccessor, this, blockPos, VineFacing))
                {
                    return false;
                }
            }

            return i < 5;
        }

        public override void OnServerGameTick(IWorldAccessor world, BlockPos pos, object extra = null)
        {
            world.BlockAccessor.SetBlock(0, pos);
        }
    }
}
