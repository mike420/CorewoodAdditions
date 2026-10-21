using Splatform;
using UnityEngine;

namespace CorewoodAdditions
{
    // Tag Both Portals Key (Left Alt by default) + Use on a connected Corewood portal renames it and the portal it links to, so the pair stays connected
    // under the new tag without walking to the other end. Vanilla portals are left alone.
    internal static class PortalTagging
    {
        private const string TokenTagBoth = "$msg_corewood_portal_tag_both";
        private const int MaxTagLength = 10;

        // The portal whose tag dialog was opened with the modifier, and the partner it was linked to at that time.
        private static TeleportWorld _pendingSource;
        private static ZDOID _pendingPartner = ZDOID.None;

        private static bool IsCorewoodPortal(TeleportWorld portal)
        {
            return portal != null && Utils.GetPrefabName(portal.gameObject) == AddPrefabs.PortalPrefabName;
        }

        private static bool ModifierHeld()
        {
            KeyCode key = MainConfig.portalTagBothKey.Value.MainKey;
            return key != KeyCode.None && Input.GetKey(key);
        }

        internal static void OnGetHoverText(TeleportWorld portal, ref string result)
        {
            KeyCode key = MainConfig.portalTagBothKey.Value.MainKey;
            if (key == KeyCode.None || !IsCorewoodPortal(portal) || !portal.HaveTarget())
            {
                return;
            }
            result += Localization.instance.Localize("\n[<color=yellow><b>" + key + " + $KEY_Use</b></color>] " + TokenTagBoth);
        }

        // Returns false when the modifier path handled the interaction.
        internal static bool OnInteract(TeleportWorld portal, Humanoid human, bool hold, ref bool result)
        {
            // Any interaction starts fresh, so a cancelled dialog can't rename a partner on a later plain rename.
            _pendingSource = null;
            _pendingPartner = ZDOID.None;
            if (hold || !IsCorewoodPortal(portal) || !ModifierHeld() || !portal.HaveTarget())
            {
                return true;
            }
            if (!PrivateArea.CheckAccess(portal.transform.position))
            {
                human.Message(MessageHud.MessageType.Center, "$piece_noaccess");
                result = true;
                return false;
            }
            _pendingSource = portal;
            _pendingPartner = portal.m_nview.GetZDO().GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal);
            TextInput.instance.RequestText(portal, TokenTagBoth, MaxTagLength);
            result = true;
            return false;
        }

        internal static void OnSetText(TeleportWorld portal, string text)
        {
            if (portal != _pendingSource || _pendingPartner == ZDOID.None)
            {
                return;
            }
            ZDOID partner = _pendingPartner;
            _pendingSource = null;
            _pendingPartner = ZDOID.None;
            RetagPartner(partner, text);
        }

        private static void RetagPartner(ZDOID partner, string text)
        {
            // A loaded partner goes through its own SetText, the same RPC path as tagging it by hand.
            GameObject instance = ZNetScene.instance != null ? ZNetScene.instance.FindInstance(partner) : null;
            TeleportWorld partnerPortal = instance != null ? instance.GetComponent<TeleportWorld>() : null;
            if (partnerPortal != null)
            {
                partnerPortal.SetText(text);
                return;
            }

            // Out of range: write the tag to its ZDO and clear its link so the portals reconnect under the new tag.
            ZDO zdo = ZDOMan.instance != null ? ZDOMan.instance.GetZDO(partner) : null;
            if (zdo == null || !zdo.IsValid())
            {
                Player.m_localPlayer?.Message(MessageHud.MessageType.Center, "$msg_corewood_portal_partner_missing");
                return;
            }
            // Take ownership first, as vanilla's Game.ConnectPortals does before it changes a portal it doesn't own.
            zdo.SetOwner(ZDOMan.GetSessionID());
            zdo.Set(ZDOVars.s_tag, text);
            zdo.Set(ZDOVars.s_tagauthor, PlatformManager.DistributionPlatform.LocalUser.PlatformUserID.ToString());
            zdo.UpdateConnection(ZDOExtraData.ConnectionType.Portal, ZDOID.None);
        }
    }
}
