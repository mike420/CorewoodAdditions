using CorewoodAdditions.Producers;
using HarmonyLib;

namespace CorewoodAdditions
{
    // Every Harmony patch in the mod. Each patch only forwards to the feature that owns the logic, so one game
    // method patched for several features (e.g. Container hover text for every producer) has a single patch here.
    internal static class GamePatches
    {
        #region Game / ObjectDB / Localization

        [HarmonyPatch(typeof(Game), nameof(Game.Awake))]
        private static class Game_Awake_Patch
        {
            private static void Postfix(Game __instance)
            {
                AddPrefabs.RegisterPortalPrefab(__instance);
            }
        }

        // A new ObjectDB means cached item lookups are stale.
        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        private static class ObjectDB_Awake_Patch
        {
            private static void Postfix()
            {
                BeeHive.OnObjectDBAwake();
                BirdHouse.OnObjectDBAwake();
                CompostBin.OnObjectDBAwake();
                FishTrap.OnObjectDBAwake();
            }
        }

        // A language change invalidates cached hover text.
        [HarmonyPatch(typeof(Localization), nameof(Localization.SetupLanguage))]
        private static class Localization_SetupLanguage_Patch
        {
            private static void Postfix()
            {
                BeeHive.OnLanguageChanged();
                BirdHouse.OnLanguageChanged();
                CompostBin.OnLanguageChanged();
                FishTrap.OnLanguageChanged();
            }
        }

        #endregion

        #region Producers (Container hover, fish trap placement)

        [HarmonyPatch(typeof(Container), nameof(Container.GetHoverName))]
        private static class Container_GetHoverName_Patch
        {
            private static void Postfix(Container __instance, ref string __result)
            {
                BeeHive.OnContainerHoverName(__instance, ref __result);
                BirdHouse.OnContainerHoverName(__instance, ref __result);
                CompostBin.OnContainerHoverName(__instance, ref __result);
                FishTrap.OnContainerHoverName(__instance, ref __result);
            }
        }

        [HarmonyPatch(typeof(Container), nameof(Container.GetHoverText))]
        private static class Container_GetHoverText_Patch
        {
            private static void Postfix(Container __instance, ref string __result)
            {
                BeeHive.OnContainerHoverText(__instance, ref __result);
                BirdHouse.OnContainerHoverText(__instance, ref __result);
                CompostBin.OnContainerHoverText(__instance, ref __result);
                FishTrap.OnContainerHoverText(__instance, ref __result);
            }
        }

        [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.IsWet))]
        private static class WearNTear_IsWet_Patch
        {
            private static void Postfix(WearNTear __instance, ref bool __result)
            {
                FishTrap.OnWearNTearIsWet(__instance, ref __result);
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacementGhost))]
        private static class Player_UpdatePlacementGhost_Patch
        {
            private static void Postfix(Player __instance)
            {
                FishTrap.OnUpdatePlacementGhost(__instance);
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
        private static class Player_TryPlacePiece_Patch
        {
            private static bool Prefix(Player __instance, Piece piece, ref bool __result)
            {
                return FishTrap.OnTryPlacePiece(__instance, piece, ref __result);
            }
        }

        // Widens the container auto-close distance for one call (ocean fish trap), then restores it.
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateContainer))]
        private static class InventoryGui_UpdateContainer_Patch
        {
            private static void Prefix(InventoryGui __instance, Player player, out float __state)
            {
                __state = __instance.m_autoCloseDistance;
                __instance.m_autoCloseDistance += FishTrap.GetExtraAutoCloseDistance(__instance.m_currentContainer, player);
            }

            private static void Postfix(InventoryGui __instance, float __state)
            {
                __instance.m_autoCloseDistance = __state;
            }
        }

        [HarmonyPatch(typeof(Minimap), nameof(Minimap.Update))]
        private static class Minimap_Update_Patch
        {
            private static void Postfix(Minimap __instance)
            {
                FishTrapPins.Tick(__instance);
                PortalPins.Tick(__instance);
            }
        }

        [HarmonyPatch(typeof(WispSpawner), nameof(WispSpawner.GetHoverText))]
        private static class WispSpawner_GetHoverText_Patch
        {
            private static void Postfix(WispSpawner __instance, ref string __result)
            {
                WispCollector.OnSpawnerHoverText(__instance, ref __result);
            }
        }

        #endregion

        #region Smelter and kiln

        [HarmonyPatch(typeof(Smelter), nameof(Smelter.UpdateSmelter))]
        private static class Smelter_UpdateSmelter_Patch
        {
            private static bool Prefix(Smelter __instance)
            {
                return StationSettings.OnUpdateSmelter(__instance);
            }

            private static void Postfix(Smelter __instance)
            {
                SmelterGroundFeed.OnUpdateSmelter(__instance);
            }
        }

        [HarmonyPatch(typeof(Smelter), nameof(Smelter.IsActive))]
        private static class Smelter_IsActive_Patch
        {
            private static void Postfix(Smelter __instance, ref bool __result)
            {
                StationSettings.OnIsActive(__instance, ref __result);
            }
        }

        [HarmonyPatch(typeof(Smelter), nameof(Smelter.OnAddOre))]
        private static class Smelter_OnAddOre_Patch
        {
            private static bool Prefix(Smelter __instance, Humanoid user, ref bool __result)
            {
                return StationSettings.AllowAdding(__instance, user, ref __result);
            }
        }

        [HarmonyPatch(typeof(Smelter), nameof(Smelter.OnAddFuel))]
        private static class Smelter_OnAddFuel_Patch
        {
            private static bool Prefix(Smelter __instance, Humanoid user, ref bool __result)
            {
                return StationSettings.AllowAdding(__instance, user, ref __result);
            }
        }

        [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.SlowUpdate))]
        private static class Fermenter_SlowUpdate_Patch
        {
            private static bool Prefix(Fermenter __instance, out bool __state)
            {
                __state = FermenterSettings.OnSlowUpdatePrefix(__instance);
                return __state;
            }

            private static void Postfix(Fermenter __instance, bool __state)
            {
                FermenterSettings.OnSlowUpdatePostfix(__instance, __state);
            }
        }

        [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.AddItem))]
        private static class Fermenter_AddItem_Patch
        {
            private static bool Prefix(Fermenter __instance, Humanoid user, ref bool __result)
            {
                return FermenterSettings.OnAddItem(__instance, user, ref __result);
            }
        }

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.UpdateCooking))]
        private static class CookingStation_UpdateCooking_Patch
        {
            private static bool Prefix(CookingStation __instance)
            {
                return CookingStationSettings.OnUpdateCooking(__instance);
            }

            private static void Postfix(CookingStation __instance)
            {
                CookingStationSettings.OnUpdateCookingPostfix(__instance);
            }
        }

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.OnUseItem))]
        private static class CookingStation_OnUseItem_Patch
        {
            private static bool Prefix(CookingStation __instance, Humanoid user, ref bool __result)
            {
                return CookingStationSettings.AllowAdding(__instance, user, ref __result);
            }
        }

        [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.Start))]
        private static class CraftingStation_Start_Patch
        {
            private static void Postfix(CraftingStation __instance)
            {
                MeadCauldronSettings.OnStart(__instance);
            }
        }

        [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.CheckUsable))]
        private static class CraftingStation_CheckUsable_Patch
        {
            private static bool Prefix(CraftingStation __instance, Player player, bool showMessage, ref bool __result)
            {
                return MeadCauldronSettings.CheckUsable(__instance, player, showMessage, ref __result);
            }
        }

        #endregion

        #region Portal

        [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.GetHoverText))]
        private static class TeleportWorld_GetHoverText_Patch
        {
            private static void Postfix(TeleportWorld __instance, ref string __result)
            {
                PortalTagging.OnGetHoverText(__instance, ref __result);
            }
        }

        [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Interact))]
        private static class TeleportWorld_Interact_Patch
        {
            private static bool Prefix(TeleportWorld __instance, Humanoid human, bool hold, ref bool __result)
            {
                return PortalTagging.OnInteract(__instance, human, hold, ref __result);
            }
        }

        [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.SetText))]
        private static class TeleportWorld_SetText_Patch
        {
            private static void Postfix(TeleportWorld __instance, string text)
            {
                PortalTagging.OnSetText(__instance, text);
            }
        }

        [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.TargetFound))]
        private static class TeleportWorld_TargetFound_Patch
        {
            private static void Postfix(TeleportWorld __instance, ref bool __result)
            {
                PortalSettings.OnTargetFound(__instance, ref __result);
            }
        }

        [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Teleport))]
        private static class TeleportWorld_Teleport_Patch
        {
            private static bool Prefix(TeleportWorld __instance, Player player, out bool __state)
            {
                __state = player != null && player.IsTeleporting();
                return PortalSettings.OnTeleport(__instance, player);
            }

            private static void Postfix(TeleportWorld __instance, Player player, bool __state)
            {
                PortalQuickTeleport.OnPortalTeleport(__instance, player, __state);
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.UpdateTeleport))]
        private static class Player_UpdateTeleport_Patch
        {
            private static void Prefix(Player __instance, ref float dt)
            {
                PortalQuickTeleport.OnUpdateTeleportPrefix(__instance, ref dt);
            }

            private static void Postfix(Player __instance)
            {
                PortalQuickTeleport.OnUpdateTeleportPostfix(__instance);
            }

            private static System.Exception Finalizer(System.Exception __exception)
            {
                PortalQuickTeleport.OnUpdateTeleportFinalizer();
                return __exception;
            }
        }

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.IsAreaReady))]
        private static class ZNetScene_IsAreaReady_Patch
        {
            private static void Postfix(ZNetScene __instance, ref bool __result)
            {
                PortalQuickTeleport.OnIsAreaReady(__instance, ref __result);
            }
        }

        [HarmonyPatch(typeof(Hud), nameof(Hud.GetFadeDuration))]
        private static class Hud_GetFadeDuration_Patch
        {
            private static void Postfix(Player player, ref float __result)
            {
                PortalQuickTeleport.OnGetFadeDuration(player, ref __result);
            }
        }

        #endregion
    }
}
