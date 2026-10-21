using System;
using System.Collections.Generic;
using CorewoodAdditions.Producers;
using Jotunn.Managers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CorewoodAdditions
{
    // Config for the HD Cooking Station: the Enable Cooking Station switch and the output icon (item_crafted, shown
    // while it holds cooked or burnt food to take off). Vanilla cooking stations are not affected.
    internal static class CookingStationSettings
    {
        private const string ItemCraftedChildName = "item_crafted";
        private const string TokenDisabled = "$msg_corewood_station_disabled";

        private static bool Enabled => MainConfig.cookingStationEnabled.Value;

        // Raw item prefab name -> the vanilla cook time it was registered with, so the multiplier always scales from it.
        private static readonly Dictionary<string, float> BaseCookTimes = new Dictionary<string, float>(StringComparer.Ordinal);

        private static bool IsCorewoodStation(CookingStation station)
        {
            return station != null && Utils.GetPrefabName(station.gameObject) == AddPrefabs.StationPrefabName;
        }

        private static bool IsDisabledStation(CookingStation station)
        {
            return !Enabled && IsCorewoodStation(station);
        }

        #region Game hooks (patched in GamePatches)

        // While disabled, placed stations stop cooking and show their food as off the fire. Consuming the elapsed
        // time keeps food from cooking or burning through the whole disabled period when the option is turned back
        // on. Returns false to skip the vanilla update.
        internal static bool OnUpdateCooking(CookingStation station)
        {
            if (!IsDisabledStation(station))
            {
                return true;
            }
            if (station.m_nview == null || !station.m_nview.IsValid())
            {
                return false;
            }
            if (station.m_nview.IsOwner())
            {
                station.GetDeltaTime();
            }
            station.UpdateVisual(false);
            return false;
        }

        // Runs after every UpdateCooking (once a second, on every client, including while disabled).
        internal static void OnUpdateCookingPostfix(CookingStation station)
        {
            if (IsCorewoodStation(station))
            {
                AutoPop(station);
                UpdateItemCrafted(station);
            }
        }

        // Drops cooked food off the station as soon as it is done, the same way taking it by hand does, so it can't
        // burn. Owner only, as the owner runs the cooking. Needs a local player (SpawnItem records the craft in their
        // stats), so a dedicated server owning the station leaves the food for the next client owner.
        private static void AutoPop(CookingStation station)
        {
            if (!MainConfig.cookingStationAutoPop.Value || !Enabled || Player.m_localPlayer == null || ObjectDB.instance == null)
            {
                return;
            }
            ZNetView nview = station.m_nview;
            if (nview == null || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }
            for (int i = 0; i < station.m_slots.Length; i++)
            {
                station.GetSlot(i, out string itemName, out _, out CookingStation.Status status, out bool cheated);
                if (status != CookingStation.Status.Done || string.IsNullOrEmpty(itemName) || ObjectDB.instance.GetItemPrefab(itemName) == null)
                {
                    continue;
                }
                // SpawnItem throws the food towards this point (or uses the station's spawn point if it has one), so
                // aim in front of the slot rather than letting it drop into the fire.
                station.SpawnItem(itemName, i, station.m_slots[i].position + station.transform.forward, cheated);
                station.SetSlot(i, "", 0f, CookingStation.Status.NotDone, cheated: false);
                nview.InvokeRPC(ZNetView.Everybody, "RPC_SetSlotVisual", i, "");
            }
        }

        // Taking cooked food off is left alone so players can still get their food back. Returns false to skip vanilla.
        internal static bool AllowAdding(CookingStation station, Humanoid user, ref bool result)
        {
            if (!IsDisabledStation(station))
            {
                return true;
            }
            user?.Message(MessageHud.MessageType.Center, TokenDisabled);
            result = false;
            return false;
        }

        #endregion

        public static void RefreshAllItemCrafted()
        {
            foreach (CookingStation station in Object.FindObjectsByType<CookingStation>(FindObjectsSortMode.None))
            {
                if (IsCorewoodStation(station))
                {
                    UpdateItemCrafted(station);
                }
            }
        }

        public static void RefreshAll()
        {
            CookingStation prefab = PrefabManager.Instance.GetPrefab(AddPrefabs.StationPrefabName)?.GetComponent<CookingStation>();
            ApplyCanOvercook(prefab);
            ApplyCookTime(prefab);
            foreach (CookingStation station in Object.FindObjectsByType<CookingStation>(FindObjectsSortMode.None))
            {
                if (IsCorewoodStation(station))
                {
                    ApplyCanOvercook(station);
                    ApplyCookTime(station);
                }
            }
        }

        internal static float GetCookTime(string fromItem, float vanillaCookTime)
        {
            BaseCookTimes[fromItem] = vanillaCookTime;
            return Scale(vanillaCookTime);
        }

        private static float Scale(float cookTime)
        {
            return Mathf.Max(0.1f, cookTime * MainConfig.cookingStationCookTimeMultiplier.Value);
        }

        // Each station has its own copy of the conversion list, so the prefab and every placed station are updated.
        // UpdateCooking reads the cook time every tick, so this applies live, including to food already cooking.
        private static void ApplyCookTime(CookingStation station)
        {
            if (station == null || station.m_conversion == null)
            {
                return;
            }
            foreach (CookingStation.ItemConversion conversion in station.m_conversion)
            {
                if (conversion?.m_from != null && BaseCookTimes.TryGetValue(conversion.m_from.name, out float cookTime))
                {
                    conversion.m_cookTime = Scale(cookTime);
                }
            }
        }

        // UpdateCooking reads this every cooking tick, so updating it applies live.
        public static void ApplyCanOvercook(CookingStation station)
        {
            if (station == null)
            {
                return;
            }
            station.m_canOvercookItems = MainConfig.cookingStationCanOvercook.Value;
        }

        public static void RefreshEnabled()
        {
            ApplyEnabled(PrefabManager.Instance.GetPrefab(AddPrefabs.StationPrefabName));
            Player.m_localPlayer?.UpdateAvailablePiecesList();
        }

        // Hides or shows the cooking station in the hammer's build menu.
        public static void ApplyEnabled(GameObject prefab)
        {
            Piece piece = prefab != null ? prefab.GetComponent<Piece>() : null;
            if (piece != null)
            {
                piece.m_enabled = Enabled;
            }
        }

        private static void UpdateItemCrafted(CookingStation station)
        {
            if (station.m_nview == null || !station.m_nview.IsValid())
            {
                return;
            }
            GameObject itemCrafted = ProducerVisuals.FindChildRecursive(station.transform, ItemCraftedChildName);
            if (itemCrafted == null)
            {
                return;
            }
            // Hidden while Auto Drop Cooked Food is doing the collecting, as food doesn't stay on the station.
            bool autoDropping = MainConfig.cookingStationAutoPop.Value && Enabled;
            bool show = MainConfig.cookingStationShowIcon.Value && !autoDropping && station.HaveDoneItem();
            if (itemCrafted.activeSelf != show)
            {
                itemCrafted.SetActive(show);
            }
        }
    }
}
