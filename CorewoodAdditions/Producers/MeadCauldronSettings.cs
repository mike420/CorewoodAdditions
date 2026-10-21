using CorewoodAdditions.Producers;
using Jotunn.Managers;
using UnityEngine;

namespace CorewoodAdditions
{
    // Config for the HD Mead Ketill: the Enable Mead Ketill switch, its sound volume, discover range and build range.
    // Vanilla mead ketills are not affected.
    internal static class MeadCauldronSettings
    {
        private const string TokenDisabled = "$msg_corewood_station_disabled";

        private static bool Enabled => MainConfig.meadCauldronEnabled.Value;

        private static bool IsCorewoodCauldron(CraftingStation station)
        {
            return station != null && Utils.GetPrefabName(station.gameObject) == AddPrefabs.CauldronPrefabName;
        }

        #region Game hooks (patched in GamePatches)

        // The ZSFX volume modifier isn't serialized, so it is set on each ketill as it spawns rather than on the prefab.
        internal static void OnStart(CraftingStation station)
        {
            if (IsCorewoodCauldron(station))
            {
                Apply(station);
            }
        }

        // Vanilla checks this when the station is opened and again before every craft, so a disabled ketill can't be
        // opened, and one already open stops crafting. Returns false to skip vanilla.
        internal static bool CheckUsable(CraftingStation station, Player player, bool showMessage, ref bool result)
        {
            if (Enabled || !IsCorewoodCauldron(station))
            {
                return true;
            }
            if (showMessage)
            {
                player?.Message(MessageHud.MessageType.Center, TokenDisabled);
            }
            result = false;
            return false;
        }

        #endregion

        public static void RefreshAll()
        {
            Apply(PrefabManager.Instance.GetPrefab(AddPrefabs.CauldronPrefabName)?.GetComponent<CraftingStation>());
            foreach (CraftingStation station in CraftingStation.m_allStations)
            {
                if (IsCorewoodCauldron(station))
                {
                    Apply(station);
                }
            }
        }

        // The ranges are read on every check (the build range within 2 seconds), so updating them applies live.
        public static void Apply(CraftingStation station)
        {
            if (station == null)
            {
                return;
            }
            station.m_discoverRange = MainConfig.meadCauldronDiscoverRange.Value;
            station.m_rangeBuild = MainConfig.meadCauldronBuildRange.Value;
            ProducerVisuals.SetSfxVolume(station.gameObject, MainConfig.meadCauldronSoundVolume.Value);
        }

        public static void RefreshEnabled()
        {
            ApplyEnabled(PrefabManager.Instance.GetPrefab(AddPrefabs.CauldronPrefabName));
            Player.m_localPlayer?.UpdateAvailablePiecesList();
        }

        // Hides or shows the mead ketill in the hammer's build menu.
        public static void ApplyEnabled(GameObject prefab)
        {
            Piece piece = prefab != null ? prefab.GetComponent<Piece>() : null;
            if (piece != null)
            {
                piece.m_enabled = Enabled;
            }
        }
    }
}
