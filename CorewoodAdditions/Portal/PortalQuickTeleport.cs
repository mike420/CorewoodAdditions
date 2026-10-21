using System.Collections.Generic;
using BepInEx.Bootstrap;
using UnityEngine;

namespace CorewoodAdditions
{
    // Faster teleports through the Corewood portal. Vanilla fades for 1s, moves the player after 2s and then waits
    // until at least 8s have passed and the destination has loaded. Here the fade is shorter, the player is moved as
    // soon as the screen is black, and the teleport ends once the destination has loaded and its objects have stopped
    // changing for a short safety delay. After the vanilla 8s window the vanilla rules apply again. Vanilla's 15s
    // fallback for a loaded area with no floor still applies, about 7s after the move as the timer starts at 8s.
    // Vanilla portals are not affected. Stands aside when QuickTeleport is installed, as that already speeds up
    // every portal and both holding the same teleport can leave it stuck loading.
    internal static class PortalQuickTeleport
    {
        private const float VanillaWindow = 8f;
        private const float MinFadeTime = 0.01f;
        private const string QuickTeleportGuid = "elg.QuickTeleport";

        private sealed class Session
        {
            public Player Player;
            public float StartTime;
            public float Elapsed;
            public bool Moved;
            public bool JustMoved;
            public bool ReadyObserved;
            public float StableSince;
            public int ObjectCount;
            public int ObjectHash;
        }

        private static Session _session;
        // The session whose UpdateTeleport is running, so IsAreaReady knows the call is ours.
        private static Session _updating;
        // After arrival the screen fades back in with the same short fade.
        private static Player _fadingInPlayer;

        private static bool? _quickTeleportInstalled;

        private static bool Enabled => MainConfig.portalQuickTeleport.Value && !QuickTeleportInstalled;
        private static bool QuickTeleportInstalled => _quickTeleportInstalled ??= Chainloader.PluginInfos.ContainsKey(QuickTeleportGuid);
        private static float FadeTime => Mathf.Max(MinFadeTime, MainConfig.portalFadeTime.Value);
        private static float SafetyDelay => Mathf.Max(0f, MainConfig.portalLoadSafetyDelay.Value);

        #region Game hooks (patched in GamePatches)

        // Called after TeleportWorld.Teleport; wasTeleporting is the player's state before the call.
        internal static void OnPortalTeleport(TeleportWorld portal, Player player, bool wasTeleporting)
        {
            if (!Enabled || player == null || player != Player.m_localPlayer || wasTeleporting || !player.IsTeleporting())
            {
                return;
            }
            if (Utils.GetPrefabName(portal.gameObject) != AddPrefabs.PortalPrefabName)
            {
                return;
            }
            _session = new Session { Player = player, StartTime = Time.time };
            _fadingInPlayer = null;
        }

        internal static void OnUpdateTeleportPrefix(Player player, ref float dt)
        {
            Session session = _session;
            if (session == null || session.Player != player)
            {
                return;
            }
            if (!Enabled || !player.m_teleporting)
            {
                // Turned off mid-teleport, or the teleport ended some other way: vanilla carries on from here.
                _session = null;
                return;
            }
            // Timed from the clock rather than dt, which other teleport mods may zero to hold the vanilla timer.
            session.Elapsed = Time.time - session.StartTime;
            if (!session.Moved)
            {
                if (!IsScreenBlack())
                {
                    // Hold the vanilla timer until the fade has finished.
                    player.m_teleportTimer = 0f;
                    dt = 0f;
                    return;
                }
                // Past the 2s move and the 8s wait, so vanilla moves the player this call and checks the destination.
                player.m_teleportTimer = Mathf.Max(player.m_teleportTimer, VanillaWindow);
                session.Moved = true;
                session.JustMoved = true;
            }
            _updating = session;
        }

        internal static void OnUpdateTeleportPostfix(Player player)
        {
            Session session = _updating;
            _updating = null;
            if (session == null || session.Player != player)
            {
                return;
            }
            if (session.JustMoved)
            {
                session.JustMoved = false;
                AnnouncePosition(player);
            }
            if (!player.m_teleporting)
            {
                _session = null;
                _fadingInPlayer = player;
            }
        }

        internal static void OnUpdateTeleportFinalizer()
        {
            _updating = null;
        }

        // Vanilla reports the area ready once its zone is loaded and every object this client knows of there exists.
        // Right after the move the server may not have sent the destination's objects yet, so also wait until that
        // set has stayed the same for the safety delay.
        internal static void OnIsAreaReady(ZNetScene scene, ref bool result)
        {
            Session session = _updating;
            if (session == null || !session.Moved)
            {
                return;
            }
            if (!result)
            {
                session.ReadyObserved = false;
                return;
            }
            GetObjectSet(scene.m_tempCurrentObjects, out int count, out int hash);
            if (!session.ReadyObserved || count != session.ObjectCount || hash != session.ObjectHash)
            {
                session.ReadyObserved = true;
                session.ObjectCount = count;
                session.ObjectHash = hash;
                session.StableSince = session.Elapsed;
            }
            result = session.Elapsed >= VanillaWindow || session.Elapsed - session.StableSince >= SafetyDelay;
        }

        internal static void OnGetFadeDuration(Player player, ref float result)
        {
            if (player == null || !Enabled)
            {
                return;
            }
            if (_session != null && _session.Player == player)
            {
                result = FadeTime;
                return;
            }
            if (_fadingInPlayer == player)
            {
                if (Hud.instance != null && Hud.instance.m_loadingScreen != null && Hud.instance.m_loadingScreen.alpha > 0f)
                {
                    result = FadeTime;
                    return;
                }
                _fadingInPlayer = null;
            }
        }

        #endregion

        private static bool IsScreenBlack()
        {
            return Hud.instance == null || Hud.instance.m_loadingScreen == null || Hud.instance.m_loadingScreen.alpha >= 0.999f;
        }

        // The client only tells the server where it is every 2s, and the server only sends objects near that point.
        // Sending the new position straight away gets the destination's objects here without waiting for that tick.
        private static void AnnouncePosition(Player player)
        {
            ZNet net = ZNet.instance;
            if (net == null)
            {
                return;
            }
            Vector3 position = player.transform.position;
            net.SetReferencePosition(position);
            net.m_periodicSendTimer = 2f;
            ZDO zdo = player.m_nview != null ? player.m_nview.GetZDO() : null;
            zdo?.SetPosition(position);
        }

        private static void GetObjectSet(List<ZDO> objects, out int count, out int hash)
        {
            count = objects.Count;
            hash = 0;
            foreach (ZDO zdo in objects)
            {
                int id = zdo.m_uid.GetHashCode();
                // Rotate by a per-object amount so swapped objects don't cancel out in the XOR.
                hash ^= (id << (id & 15)) | (int)((uint)id >> (32 - (id & 15)));
            }
        }
    }
}
