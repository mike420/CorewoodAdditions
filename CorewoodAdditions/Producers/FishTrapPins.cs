using System.Collections.Generic;
using UnityEngine;

namespace CorewoodAdditions.Producers;

// Shows a map pin for every fish trap this client knows about. Traps are found from their ZDOs rather than their
// loaded GameObjects, so the pins stay on the map when the traps are far away. Clients keep every ZDO they have
// received, so on a dedicated server a player sees the traps they have been near this session.
internal static class FishTrapPins
{
    private const string IconName = "fishtrap_icon";
    private const float ScanInterval = 3f;

    private sealed class TrapType
    {
        public string PrefabName;
        public string PieceName;
    }

    private static TrapType[] _trapTypes;
    private static readonly Dictionary<ZDOID, Minimap.PinData> Pins = new Dictionary<ZDOID, Minimap.PinData>();
    private static readonly List<ZDO> Found = new List<ZDO>();
    private static readonly HashSet<ZDOID> Seen = new HashSet<ZDOID>();
    private static readonly List<ZDOID> Stale = new List<ZDOID>();

    private static Minimap _minimap;
    private static Sprite _icon;
    private static float _nextScanTime;
    private static bool _scanning;
    private static int _typeIndex;
    private static int _sectorIndex;

    private static TrapType[] TrapTypes => _trapTypes ??= new[]
    {
        new TrapType { PrefabName = FishTrap.PrefabName, PieceName = "$piece_mm_corewood_fishtrap" },
        new TrapType { PrefabName = FishTrap.LargePrefabName, PieceName = "$piece_mm_corewood_fishtrap_large" },
        new TrapType { PrefabName = FishTrap.OceanPrefabName, PieceName = "$piece_mm_corewood_fishtrap_ocean" }
    };

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
            _typeIndex = 0;
            _sectorIndex = 0;
            Seen.Clear();
            Found.Clear();
        }
        ScanStep();
    }

    // One batch of sectors per frame so a large world never stalls a single frame.
    private static void ScanStep()
    {
        if (_typeIndex < TrapTypes.Length)
        {
            TrapType type = TrapTypes[_typeIndex];
            // With pins turned off nothing is collected, so the stale pass below removes every pin.
            bool enabled = MainConfig.fishtrapShowPins.Value;
            if (enabled && !ZDOMan.instance.GetAllZDOsWithPrefabIterative(type.PrefabName, Found, ref _sectorIndex))
            {
                return;
            }
            if (enabled)
            {
                foreach (ZDO zdo in Found)
                {
                    Seen.Add(zdo.m_uid);
                    UpdatePin(zdo, type);
                }
            }
            Found.Clear();
            _sectorIndex = 0;
            _typeIndex++;
            return;
        }
        RemoveStalePins();
        _scanning = false;
        _nextScanTime = Time.time + ScanInterval;
    }

    private static void UpdatePin(ZDO zdo, TrapType type)
    {
        Vector3 position = zdo.GetPosition();
        if (Pins.TryGetValue(zdo.m_uid, out Minimap.PinData pin) && _minimap.m_pins.Contains(pin))
        {
            pin.m_pos = position;
            return;
        }
        pin = _minimap.AddPin(position, Minimap.PinType.Icon3, Localization.instance.Localize(type.PieceName), save: false, isChecked: false);
        Sprite icon = GetIcon();
        if (icon != null)
        {
            pin.m_icon = icon;
        }
        Pins[zdo.m_uid] = pin;
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
