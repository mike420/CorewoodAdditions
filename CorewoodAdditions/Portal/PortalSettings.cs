using Jotunn.Managers;
using UnityEngine;

namespace CorewoodAdditions
{
    // Config for the Corewood portal: its sound and the Enable Portals switch. Vanilla portals are not affected.
    internal static class PortalSettings
    {
        private const string TokenDisabled = "$msg_corewood_portal_disabled";

        // The portal hum's volume as set in the bundle; -1 until read from the prefab.
        private static float _humBaseVolume = -1f;

        private static bool IsCorewoodPortal(TeleportWorld portal)
        {
            return portal != null && Utils.GetPrefabName(portal.gameObject) == AddPrefabs.PortalPrefabName;
        }

        private static bool IsDisabled(TeleportWorld portal)
        {
            return !MainConfig.portalsEnabled.Value && IsCorewoodPortal(portal);
        }

        // A disabled portal reports no target: it goes dark and won't teleport, but keeps its link and tag. A vanilla
        // portal linked to a disabled Corewood portal also reports no target, so it can't send players there.
        internal static void OnTargetFound(TeleportWorld portal, ref bool result)
        {
            if (result && !MainConfig.portalsEnabled.Value && (IsCorewoodPortal(portal) || LinksToCorewoodPortal(portal)))
            {
                result = false;
            }
        }

        private static bool LinksToCorewoodPortal(TeleportWorld portal)
        {
            ZDO zdo = portal.m_nview != null ? portal.m_nview.GetZDO() : null;
            ZDO partner = zdo != null ? ZDOMan.instance.GetZDO(zdo.GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal)) : null;
            return partner != null && partner.GetPrefab() == PortalHash;
        }

        private static readonly int PortalHash = AddPrefabs.PortalPrefabName.GetStableHashCode();

        // Returns false to block the teleport.
        internal static bool OnTeleport(TeleportWorld portal, Player player)
        {
            if (!IsDisabled(portal))
            {
                return true;
            }
            player?.Message(MessageHud.MessageType.Center, TokenDisabled);
            return false;
        }

        public static void RefreshAll()
        {
            GameObject prefab = PrefabManager.Instance.GetPrefab(AddPrefabs.PortalPrefabName);
            ApplyEnabled(prefab);
            ApplySound(prefab);
            ApplyAllowAllItems(prefab);
            foreach (TeleportWorld portal in Object.FindObjectsByType<TeleportWorld>(FindObjectsSortMode.None))
            {
                if (IsCorewoodPortal(portal))
                {
                    ApplySound(portal.gameObject);
                    ApplyAllowAllItems(portal.gameObject);
                }
            }
            Player.m_localPlayer?.UpdateAvailablePiecesList();
        }

        // TeleportWorld reads this from the placed portal each time someone uses it, so updating it applies live.
        public static void ApplyAllowAllItems(GameObject root)
        {
            TeleportWorld portal = root != null ? root.GetComponent<TeleportWorld>() : null;
            if (portal != null)
            {
                portal.m_allowAllItems = MainConfig.portalAllowAllItems.Value;
            }
        }

        // Hides or shows the portal in the hammer's build menu.
        public static void ApplyEnabled(GameObject prefab)
        {
            Piece piece = prefab != null ? prefab.GetComponent<Piece>() : null;
            if (piece != null)
            {
                piece.m_enabled = MainConfig.portalsEnabled.Value;
            }
        }

        // EffectFade drives the portal hum through AudioSource.volume, so mute is used to silence it instead, and the
        // volume is set through the base volume EffectFade fades towards. New portals copy the prefab's AudioSource
        // volume, which EffectFade takes as its base when it wakes.
        public static void ApplySound(GameObject root)
        {
            if (root == null)
            {
                return;
            }
            bool mute = !MainConfig.portalSoundEnabled.Value;
            foreach (AudioSource source in root.GetComponentsInChildren<AudioSource>(includeInactive: true))
            {
                // The first call is on the untouched prefab at registration, so this is the volume set in the bundle.
                if (_humBaseVolume < 0f)
                {
                    _humBaseVolume = source.volume;
                }
                source.mute = mute;
            }
            float volume = Mathf.Max(0f, _humBaseVolume) * Mathf.Clamp01(MainConfig.portalSoundVolume.Value);
            bool faded = false;
            foreach (EffectFade fade in root.GetComponentsInChildren<EffectFade>(includeInactive: true))
            {
                if (fade.m_audioSource != null)
                {
                    fade.m_baseVolume = volume;
                    faded = true;
                }
            }
            if (!faded)
            {
                // The prefab, whose EffectFade has not woken.
                foreach (AudioSource source in root.GetComponentsInChildren<AudioSource>(includeInactive: true))
                {
                    source.volume = volume;
                }
            }
        }
    }
}
