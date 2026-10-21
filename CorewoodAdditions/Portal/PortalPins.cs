using System.Collections.Generic;
using UnityEngine;

namespace CorewoodAdditions
{
    // Shows a map pin, labelled with the portal's tag, for every Corewood portal this client knows about. Portals are
    // found from their ZDOs rather than their loaded GameObjects, so the pins stay on the map when the portals are far
    // away. Vanilla portals are not affected.
    internal static class PortalPins
    {
        private const string IconName = "portal_icon";
        private const string UntaggedName = "$piece_portal";
        private const float ScanInterval = 3f;

        private static readonly Dictionary<ZDOID, Minimap.PinData> Pins = new Dictionary<ZDOID, Minimap.PinData>();
        private static readonly List<ZDO> Found = new List<ZDO>();
        private static readonly HashSet<ZDOID> Seen = new HashSet<ZDOID>();
        private static readonly List<ZDOID> Stale = new List<ZDOID>();

        private static Minimap _minimap;
        private static Sprite _icon;
        private static float _nextScanTime;
        private static bool _scanning;
        private static int _sectorIndex;

        // Runs after every Minimap.Update (patched in GamePatches).
        internal static void Tick(Minimap minimap)
        {
            if (minimap != _minimap)
            {
                // A new Minimap (new world or relog) starts without our pins.
                _minimap = minimap;
                Pins.Clear();
                _scanning = false;
            }
            if (ZDOMan.instance == null || ZNet.instance == null)
            {
                return;
            }
            if (!_scanning)
            {
                if (Time.time < _nextScanTime)
                {
                    return;
                }
                _scanning = true;
                _sectorIndex = 0;
                Seen.Clear();
                Found.Clear();
            }
            ScanStep();
        }

        // One batch of sectors per frame so a large world never stalls a single frame.
        private static void ScanStep()
        {
            // With pins turned off nothing is collected, so the stale pass below removes every pin.
            bool enabled = MainConfig.portalShowPins.Value;
            if (enabled && !ZDOMan.instance.GetAllZDOsWithPrefabIterative(AddPrefabs.PortalPrefabName, Found, ref _sectorIndex))
            {
                return;
            }
            if (enabled)
            {
                foreach (ZDO zdo in Found)
                {
                    Seen.Add(zdo.m_uid);
                    UpdatePin(zdo);
                }
            }
            Found.Clear();
            RemoveStalePins();
            _scanning = false;
            _nextScanTime = Time.time + ScanInterval;
        }

        private static void UpdatePin(ZDO zdo)
        {
            Vector3 position = zdo.GetPosition();
            string name = GetPinName(zdo);
            if (Pins.TryGetValue(zdo.m_uid, out Minimap.PinData pin) && _minimap.m_pins.Contains(pin))
            {
                if (pin.m_name == name)
                {
                    pin.m_pos = position;
                    return;
                }
                // Re-added on a tag change, as the pin's label text is only set when the pin is created.
                _minimap.RemovePin(pin);
            }
            pin = _minimap.AddPin(position, Minimap.PinType.Icon4, name, save: false, isChecked: false);
            Sprite icon = GetIcon();
            if (icon != null)
            {
                pin.m_icon = icon;
            }
            Pins[zdo.m_uid] = pin;
        }

        private static string GetPinName(ZDO zdo)
        {
            string tag = zdo.GetString(ZDOVars.s_tag).RemoveRichTextTags().Trim();
            return Localization.instance.Localize(tag.Length > 0 ? tag : UntaggedName);
        }

        private static void RemoveStalePins()
        {
            Stale.Clear();
            foreach (KeyValuePair<ZDOID, Minimap.PinData> entry in Pins)
            {
                if (!Seen.Contains(entry.Key))
                {
                    Stale.Add(entry.Key);
                }
            }
            foreach (ZDOID id in Stale)
            {
                _minimap.RemovePin(Pins[id]);
                Pins.Remove(id);
            }
        }

        private static Sprite GetIcon()
        {
            if (_icon == null && CorewoodAdditionsMod.cwadditions != null)
            {
                _icon = CorewoodAdditionsMod.cwadditions.LoadAsset<Sprite>(IconName);
            }
            return _icon;
        }
    }
}
