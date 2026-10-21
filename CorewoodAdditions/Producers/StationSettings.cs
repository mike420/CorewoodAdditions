using Jotunn.Managers;
using UnityEngine;

namespace CorewoodAdditions
{
    internal static class StationSettings
    {
        internal enum StationKind
        {
            Smelter,
            Kiln,
            Windmill,
            FrostKiln
        }

        private const string TokenDisabled = "$msg_corewood_station_disabled";

        private static readonly (string PrefabName, StationKind Kind)[] Stations =
        {
            (AddPrefabs.SmelterNewPrefabName, StationKind.Smelter),
            (AddPrefabs.KilnPrefabName, StationKind.Kiln),
            (AddPrefabs.WindmillPrefabName, StationKind.Windmill),
            (AddPrefabs.FrostKilnPrefabName, StationKind.FrostKiln)
        };

        private static bool TryGetKind(GameObject go, out StationKind kind)
        {
            string prefabName = Utils.GetPrefabName(go);
            foreach ((string name, StationKind stationKind) in Stations)
            {
                if (prefabName == name)
                {
                    kind = stationKind;
                    return true;
                }
            }
            kind = default;
            return false;
        }

        // The windmill and frost kiln have their own switches; the smelter and kiln share one.
        private static bool IsEnabled(StationKind kind)
        {
            return kind switch
            {
                StationKind.Windmill => MainConfig.windmillEnabled.Value,
                StationKind.FrostKiln => MainConfig.frostKilnEnabled.Value,
                _ => MainConfig.smelterKilnEnabled.Value
            };
        }

        private static bool IsDisabledStation(Smelter smelter)
        {
            return smelter != null && TryGetKind(smelter.gameObject, out StationKind kind) && !IsEnabled(kind);
        }

        // While disabled, placed stations stop producing and go dark. Consuming the elapsed time keeps them from
        // catching up on the whole disabled period (up to an hour) when the option is turned back on.
        // Returns false to skip the vanilla update.
        internal static bool OnUpdateSmelter(Smelter smelter)
        {
            if (!IsDisabledStation(smelter))
            {
                return true;
            }
            if (smelter.m_nview == null || !smelter.m_nview.IsValid())
            {
                return false;
            }
            // UpdateState reads IsActive (see OnIsActive), so this switches the fire, smoke and animation off.
            smelter.UpdateState();
            if (smelter.m_nview.IsOwner())
            {
                smelter.GetDeltaTime();
            }
            return false;
        }

        internal static void OnIsActive(Smelter smelter, ref bool result)
        {
            if (result && IsDisabledStation(smelter))
            {
                result = false;
            }
        }

        // Emptying is left alone so players can still get their materials back out.
        internal static bool AllowAdding(Smelter smelter, Humanoid user, ref bool result)
        {
            if (!IsDisabledStation(smelter))
            {
                return true;
            }
            user?.Message(MessageHud.MessageType.Center, TokenDisabled);
            result = false;
            return false;
        }

        public static void RefreshAll()
        {
            foreach ((string name, StationKind kind) in Stations)
            {
                Apply(PrefabManager.Instance.GetPrefab(name)?.GetComponent<Smelter>(), kind);
            }
            foreach (Smelter smelter in Object.FindObjectsByType<Smelter>(FindObjectsSortMode.None))
            {
                if (TryGetKind(smelter.gameObject, out StationKind kind))
                {
                    Apply(smelter, kind);
                }
            }
        }

        public static void RefreshEnabled()
        {
            foreach ((string name, StationKind _) in Stations)
            {
                ApplyEnabled(PrefabManager.Instance.GetPrefab(name));
            }
            Player.m_localPlayer?.UpdateAvailablePiecesList();
        }

        public static void ApplyEnabled(GameObject prefab)
        {
            Piece piece = prefab != null ? prefab.GetComponent<Piece>() : null;
            if (piece != null && TryGetKind(prefab, out StationKind kind))
            {
                piece.m_enabled = IsEnabled(kind);
            }
        }

        public static void Apply(Smelter smelter, StationKind kind)
        {
            if (smelter == null)
            {
                return;
            }
            switch (kind)
            {
                case StationKind.Kiln:
                    smelter.m_maxOre = Mathf.Max(1, MainConfig.kilnMaxOre.Value);
                    smelter.m_secPerProduct = Mathf.Max(0.1f, MainConfig.kilnSecPerProduct.Value);
                    SetSmoke(smelter, MainConfig.kilnSmokeEnabled.Value);
                    break;
                case StationKind.Windmill:
                    smelter.m_maxOre = Mathf.Max(1, MainConfig.windmillMaxOre.Value);
                    smelter.m_secPerProduct = Mathf.Max(0.1f, MainConfig.windmillSecPerProduct.Value);
                    break;
                case StationKind.FrostKiln:
                    // Like the vanilla frost kiln it takes no ore (m_maxOre stays 0): it burns Ice as fuel.
                    smelter.m_maxFuel = Mathf.Max(1, MainConfig.frostKilnMaxFuel.Value);
                    smelter.m_fuelPerProduct = Mathf.Max(1, MainConfig.frostKilnFuelPerProduct.Value);
                    smelter.m_secPerProduct = Mathf.Max(0.1f, MainConfig.frostKilnSecPerProduct.Value);
                    SetSmoke(smelter, MainConfig.frostKilnSmokeEnabled.Value);
                    break;
                default:
                    smelter.m_maxOre = Mathf.Max(1, MainConfig.smelterMaxOre.Value);
                    smelter.m_maxFuel = Mathf.Max(1, MainConfig.smelterMaxFuel.Value);
                    smelter.m_fuelPerProduct = Mathf.Max(1, MainConfig.smelterFuelPerProduct.Value);
                    smelter.m_secPerProduct = Mathf.Max(0.1f, MainConfig.smelterSecPerProduct.Value);
                    SetSmoke(smelter, MainConfig.smelterSmokeEnabled.Value);
                    break;
            }
        }

        // An inactive SmokeSpawner still answers IsBlocked() with a physics test, so a blocked chimney keeps
        // stopping the station with the smoke hidden.
        private static void SetSmoke(Smelter smelter, bool enabled)
        {
            if (smelter.m_smokeSpawner != null && smelter.m_smokeSpawner.gameObject.activeSelf != enabled)
            {
                smelter.m_smokeSpawner.gameObject.SetActive(enabled);
            }
        }
    }
}
