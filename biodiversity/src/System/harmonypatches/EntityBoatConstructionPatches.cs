using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
namespace biodiversity.src.System.harmony
{
    internal class EntityBoatConstruction_Patches
    {
        /*
        [HarmonyPatch(typeof(EntityBoatConstruction), "OnTesselation")]
        class OnTesselation
        {

            
            public override void OnTesselation(EntityBoatConstruction __instance, RightClickConstruction ___rcc, ref Shape entityShape, string shapePathForLogging)
            {
                if (base.Properties.Client.Renderer is EntityShapeRenderer entityShapeRenderer)
                {
                    entityShapeRenderer.OverrideSelectiveElements = ___rcc.getShapeElements();
                }
                if (__instance.Api is ICoreClientAPI && ___rcc.StoredWildCards.TryGetValue("wood", out var value))
                {
                    setTexture("debarked", new AssetLocation($"block/wood/debarked/{value}"));
                    setTexture("planks", new AssetLocation($"block/wood/planks/{value}1"));
                }
                base.OnTesselation(ref entityShape, shapePathForLogging);
            }
            /*
            public override void OnTesselation(ref Shape entityShape, string shapePathForLogging)
            {
                if (base.Properties.Client.Renderer is EntityShapeRenderer entityShapeRenderer)
                {
                    entityShapeRenderer.OverrideSelectiveElements = rcc.getShapeElements();
                }
                if (Api is ICoreClientAPI && rcc.StoredWildCards.TryGetValue("wood", out var value))
                {
                    setTexture("debarked", new AssetLocation($"block/wood/debarked/{value}"));
                    setTexture("planks", new AssetLocation($"block/wood/planks/{value}1"));
                }
                base.OnTesselation(ref entityShape, shapePathForLogging);
            }
            */
        /*     
        }

        [HarmonyPatch(typeof(EntityBoatConstruction), "Spawn")]
        class Spawn
        {
            private void Prefix(EntityBoatConstruction __instance, RightClickConstruction ___rcc, RightClickConstruction ___)
            {
                if (___rcc.StoredWildCards.TryGetValue("wood", out var value))
                {
                    Vec3f centerPos = getCenterPos();
                    Vec3f vec3f = ((centerPos == null) ? new Vec3f() : (centerPos - launchStartPos));
                    EntityProperties entityType = __instance.World.GetEntityType(new AssetLocation("boat-sailed-" + value));
                    Entity entity = __instance.World.ClassRegistry.CreateEntity(entityType);
                    if ((int)Math.Abs(base.Pos.Yaw * (180f / (float)Math.PI)) == 90 || (int)Math.Abs(base.Pos.Yaw * (180f / (float)Math.PI)) == 270)
                    {
                        vec3f.X *= 1.1f;
                    }
                    vec3f.Y = 0.5f;
                    entity.Pos.SetFrom(base.Pos).Add(vec3f);
                    entity.Pos.Motion.Add((double)vec3f.X / 50.0, 0.0, (double)vec3f.Z / 50.0);
                    IPlayer player = (__instance.launchingEntity as EntityPlayer)?.Player;
                    if (player != null)
                    {
                        entity.WatchedAttributes.SetString("createdByPlayername", player.PlayerName);
                        entity.WatchedAttributes.SetString("createdByPlayerUID", player.PlayerUID);
                    }
                    __instance.World.SpawnEntity(entity);
                }
            }
            */
        /*
        private void Spawn()
        {
            if (rcc.StoredWildCards.TryGetValue("wood", out var value))
            {
                Vec3f centerPos = getCenterPos();
                Vec3f vec3f = ((centerPos == null) ? new Vec3f() : (centerPos - launchStartPos));
                EntityProperties entityType = World.GetEntityType(new AssetLocation("boat-sailed-" + value));
                Entity entity = World.ClassRegistry.CreateEntity(entityType);
                if ((int)Math.Abs(base.Pos.Yaw * (180f / (float)Math.PI)) == 90 || (int)Math.Abs(base.Pos.Yaw * (180f / (float)Math.PI)) == 270)
                {
                    vec3f.X *= 1.1f;
                }
                vec3f.Y = 0.5f;
                entity.Pos.SetFrom(base.Pos).Add(vec3f);
                entity.Pos.Motion.Add((double)vec3f.X / 50.0, 0.0, (double)vec3f.Z / 50.0);
                IPlayer player = (launchingEntity as EntityPlayer)?.Player;
                if (player != null)
                {
                    entity.WatchedAttributes.SetString("createdByPlayername", player.PlayerName);
                    entity.WatchedAttributes.SetString("createdByPlayerUID", player.PlayerUID);
                }
                World.SpawnEntity(entity);
            }
        }
        */
    }
}
