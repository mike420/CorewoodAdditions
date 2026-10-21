using System;
using Jotunn.Managers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CorewoodAdditions
{
    // Config for the HD Fermenter: the Enable Fermenter switch and the output icon (item_crafted, shown while the
    // fermenter is ready to tap). Vanilla fermenters are not affected.
    internal static class FermenterSettings
    {
        private const string ItemCraftedChildName = "item_crafted";
        private const string TokenDisabled = "$msg_corewood_station_disabled";

        // Fermentation time (seconds) frozen while disabled; -1 when not paused.
        private static readonly int ZdoPausedTimeHash = "mm_fermenter_pausedtime".GetStableHashCode();

        private static bool Enabled => MainConfig.fermenterEnabled.Value;

        private static bool IsCorewoodFermenter(Fermenter fermenter)
        {
            return fermenter != null && Utils.GetPrefabName(fermenter.gameObject) == AddPrefabs.FermenterPrefabName;
        }

        #region Game hooks (patched in GamePatches)

        // A disabled HD Fermenter skips vanilla's SlowUpdate. Vanilla would switch the fermenting visuals back on every
        // 2s, and each time the looping bubble sound re-registers with AudioMan (it only drops entries on destroy), so
        // the list would keep growing. Skipping it also skips the cover check, which would reset an exposed brew.
        // Returns false to skip vanilla.
        internal static bool OnSlowUpdatePrefix(Fermenter fermenter)
        {
            if (!IsCorewoodFermenter(fermenter) || fermenter.m_nview == null || !fermenter.m_nview.IsValid())
            {
                return true;
            }
            if (Enabled)
            {
                ClearPause(fermenter);
                return true;
            }
            HoldPause(fermenter);
            ApplyDisabledVisuals(fermenter);
            UpdateItemCrafted(fermenter);
            return false;
        }

        // Runs after Fermenter.SlowUpdate (enabled fermenters only, as disabled ones skip it), which sets the
        // fermenting and ready visuals every 2s.
        internal static void OnSlowUpdatePostfix(Fermenter fermenter, bool ranVanilla)
        {
            if (ranVanilla && IsCorewoodFermenter(fermenter))
            {
                UpdateItemCrafted(fermenter);
            }
        }

        // Filling is blocked while disabled; a finished brew can still be tapped. Returns false to skip vanilla.
        internal static bool OnAddItem(Fermenter fermenter, Humanoid user, ref bool result)
        {
            if (Enabled || !IsCorewoodFermenter(fermenter))
            {
                return true;
            }
            user?.Message(MessageHud.MessageType.Center, TokenDisabled);
            result = false;
            return false;
        }

        #endregion

        public static void RefreshAll()
        {
            GameObject prefab = PrefabManager.Instance.GetPrefab(AddPrefabs.FermenterPrefabName);
            ApplyEnabled(prefab);
            ApplyDuration(prefab != null ? prefab.GetComponent<Fermenter>() : null);
            Player.m_localPlayer?.UpdateAvailablePiecesList();
            foreach (Fermenter fermenter in Object.FindObjectsByType<Fermenter>(FindObjectsSortMode.None))
            {
                if (IsCorewoodFermenter(fermenter) && fermenter.m_nview != null && fermenter.m_nview.IsValid())
                {
                    ApplyDuration(fermenter);
                    if (!Enabled)
                    {
                        ApplyDisabledVisuals(fermenter);
                    }
                    // When re-enabled, vanilla's next SlowUpdate (within 2s) restores the fermenting visuals.
                    UpdateItemCrafted(fermenter);
                }
            }
        }

        // Fermenter.GetStatus reads this on every check, so changing it applies live, including to brews in progress.
        public static void ApplyDuration(Fermenter fermenter)
        {
            if (fermenter != null)
            {
                fermenter.m_fermentationDuration = Mathf.Max(1f, MainConfig.fermenterDuration.Value);
            }
        }

        // Hides or shows the fermenter in the hammer's build menu.
        public static void ApplyEnabled(GameObject prefab)
        {
            Piece piece = prefab != null ? prefab.GetComponent<Piece>() : null;
            if (piece != null)
            {
                piece.m_enabled = Enabled;
            }
        }

        // While disabled the fermentation time is held where it was, so turning the option back on carries on from
        // there instead of finishing everything that fermented in the meantime. Vanilla tracks progress as a start
        // time, so the start time is moved forward to keep the elapsed time fixed; the frozen value is kept on the
        // ZDO so this also holds across the area being unloaded. Owner only.
        private static void HoldPause(Fermenter fermenter)
        {
            if (!fermenter.m_nview.IsOwner() || fermenter.GetStatus() != Fermenter.Status.Fermenting)
            {
                return;
            }
            ZDO zdo = fermenter.m_nview.GetZDO();
            float paused = zdo.GetFloat(ZdoPausedTimeHash, -1f);
            if (paused < 0f)
            {
                paused = (float)Math.Max(0.0, fermenter.GetFermentationTime());
                zdo.Set(ZdoPausedTimeHash, paused);
            }
            zdo.Set(ZDOVars.s_startTime, (ZNet.instance.GetTime() - TimeSpan.FromSeconds(paused)).Ticks);
        }

        private static void ClearPause(Fermenter fermenter)
        {
            if (!fermenter.m_nview.IsOwner())
            {
                return;
            }
            ZDO zdo = fermenter.m_nview.GetZDO();
            if (zdo.GetFloat(ZdoPausedTimeHash, -1f) >= 0f)
            {
                zdo.Set(ZdoPausedTimeHash, -1f);
            }
        }

        // Disabled fermenters go dark: no bubbles, steam or sound while fermentation is paused. A finished brew keeps
        // its ready visuals so players can see it can still be tapped. Only changes what differs, so nothing is
        // switched off and on again.
        private static void ApplyDisabledVisuals(Fermenter fermenter)
        {
            Fermenter.Status status = fermenter.GetStatus();
            SetActive(fermenter.m_fermentingObject, false);
            SetActive(fermenter.m_readyObject, status == Fermenter.Status.Ready);
            SetActive(fermenter.m_topObject, status != Fermenter.Status.Empty);
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active)
            {
                go.SetActive(active);
            }
        }

        private static void UpdateItemCrafted(Fermenter fermenter)
        {
            Transform itemCrafted = fermenter.transform.Find(ItemCraftedChildName);
            if (itemCrafted == null)
            {
                return;
            }
            bool show = MainConfig.fermenterShowIcon.Value && fermenter.GetStatus() == Fermenter.Status.Ready;
            if (itemCrafted.gameObject.activeSelf != show)
            {
                itemCrafted.gameObject.SetActive(show);
            }
        }
    }
}
