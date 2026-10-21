using System;
using System.Collections.Generic;
using UnityEngine;

namespace CorewoodAdditions.Producers;

internal sealed class BirdHouse : MonoBehaviour
{
    internal const string PrefabName = "mm_corewood_birdhouse";
    private const string EnabledBirdboxChildName = "enabled_birdbox";
    private const string ItemCraftedChildName = "item_crafted";
    private const string SfxChildName = "SFX";
    private const string FeatherPrefabName = "Feathers";
    private const string DefaultConsumable = "TurnipSeeds";
    private const int MaxConsumableTypes = 10;

    private const string TokenWorking = "$text_birdhouse_working";
    private const string TokenNoConsumable = "$text_birdhouse_no_consumable";
    private const string TokenFull = "$text_birdhouse_full";

    private static readonly int ZdoStartTimeHash = "mm_birdhouse_starttime".GetStableHashCode();
    private static readonly int ZdoAccTimeHash = "mm_birdhouse_acctime".GetStableHashCode();
    // Start time while the producer is idle: the clock is stopped and no progress builds up.
    private const long IdleStartTime = -1L;

    private static ItemDrop _featherItemPrefab;
    private static ObjectDB _resolvedDb;
    private static string _cachedConsumablesRaw;
    private static HashSet<string> _consumablePrefabNames = new HashSet<string>(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> DisplayCache = new Dictionary<string, string>(StringComparer.Ordinal);

    private ZNetView _nview;
    private Piece _piece;
    private Container _container;
    private string _name = "$piece_container";
    private bool _productionTickRunning;
    private GameObject _enabledBirdbox;
    private GameObject _itemCrafted;
    private GameObject _sfx;
    private Inventory _subscribedInventory;
    private bool _visualsDirty;

    private static float SecPerBatch => Mathf.Max(0.1f, MainConfig.birdhouseSecPerBatch.Value);
    private static int ConsumablePerBatch => Mathf.Max(1, MainConfig.birdhouseConsumablePerBatch.Value);
    private static int FeathersPerBatch => Mathf.Max(1, MainConfig.birdhouseFeathersPerBatch.Value);
    private static int MaxFeathers => Mathf.Max(1, MainConfig.birdhouseMaxFeathers.Value);

    #region Game hooks (patched in GamePatches)

    internal static void OnContainerHoverName(Container container, ref string result)
    {
        BirdHouse birdhouse = container.GetComponentInParent<BirdHouse>();
        if (birdhouse != null)
        {
            result = birdhouse._name;
        }
    }

    internal static void OnContainerHoverText(Container container, ref string result)
    {
        BirdHouse birdhouse = container.GetComponentInParent<BirdHouse>();
        if (birdhouse != null)
        {
            result = birdhouse.BuildHoverText();
        }
    }

    internal static void OnObjectDBAwake()
    {
        _resolvedDb = null;
        _featherItemPrefab = null;
        DisplayCache.Clear();
    }

    internal static void OnLanguageChanged()
    {
        DisplayCache.Clear();
    }

    #endregion

    #region Feather / consumable resolution

    private static ItemDrop GetFeatherPrefab()
    {
        ObjectDB db = ObjectDB.instance;
        if (db != null && db != _resolvedDb)
        {
            _resolvedDb = db;
            _featherItemPrefab = db.GetItemPrefab(FeatherPrefabName)?.GetComponent<ItemDrop>();
        }
        return _featherItemPrefab;
    }

    private static HashSet<string> GetConsumablePrefabNames()
    {
        string raw = MainConfig.birdhouseConsumables?.Value ?? string.Empty;
        if (string.Equals(_cachedConsumablesRaw, raw, StringComparison.Ordinal))
        {
            return _consumablePrefabNames;
        }

        HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
        foreach (string entry in raw.Split(','))
        {
            string name = entry.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }
            names.Add(name);
            if (names.Count >= MaxConsumableTypes)
            {
                break;
            }
        }
        if (names.Count == 0)
        {
            names.Add(DefaultConsumable);
        }

        _cachedConsumablesRaw = raw;
        _consumablePrefabNames = names;
        return names;
    }

    private static bool IsConsumable(ItemDrop.ItemData item)
    {
        return item.m_dropPrefab != null && GetConsumablePrefabNames().Contains(item.m_dropPrefab.name);
    }

    private static bool IsFeather(ItemDrop.ItemData item)
    {
        return item.m_dropPrefab != null && item.m_dropPrefab.name == FeatherPrefabName;
    }

    private static string GetLocalizedItemName(string prefabName)
    {
        string token = ObjectDB.instance?.GetItemPrefab(prefabName)?.GetComponent<ItemDrop>()?.m_itemData?.m_shared?.m_name;
        return string.IsNullOrEmpty(token) ? prefabName : Localization.instance.Localize(token);
    }

    private static string GetAcceptedLine()
    {
        HashSet<string> consumables = GetConsumablePrefabNames();
        string cacheKey = _cachedConsumablesRaw + "#" + ConsumablePerBatch + ":" + (ObjectDB.instance != null ? "1" : "0");
        if (DisplayCache.TryGetValue(cacheKey, out string cached))
        {
            return cached;
        }

        List<string> names = new List<string>();
        foreach (string prefab in consumables)
        {
            names.Add(GetLocalizedItemName(prefab));
        }
        string result = "<color=yellow>" + GetLocalizedItemName(FeatherPrefabName) + "</color>: " + ConsumablePerBatch + "x " + string.Join(", ", names);
        DisplayCache[cacheKey] = result;
        return result;
    }

    #endregion

    #region Inventory helpers

    private static int GetConsumableCount(Inventory inventory)
    {
        long total = 0L;
        foreach (ItemDrop.ItemData item in inventory.GetAllItems())
        {
            if (IsConsumable(item))
            {
                total += item.m_stack;
                if (total >= int.MaxValue)
                {
                    return int.MaxValue;
                }
            }
        }
        return (int)total;
    }

    private static bool HasEnoughConsumableForBatch(Inventory inventory)
    {
        return GetConsumableCount(inventory) >= ConsumablePerBatch;
    }

    private static bool TryConsumeConsumable(Inventory inventory, int amountNeeded)
    {
        int remaining = amountNeeded;
        foreach (ItemDrop.ItemData item in new List<ItemDrop.ItemData>(inventory.GetAllItems()))
        {
            if (!IsConsumable(item))
            {
                continue;
            }
            int take = Math.Min(item.m_stack, remaining);
            if (take > 0 && inventory.RemoveItem(item, take))
            {
                remaining -= take;
            }
            if (remaining <= 0)
            {
                return true;
            }
        }
        return false;
    }

    private static bool TryAddFeathers(Inventory inventory, int amount)
    {
        ItemDrop prefab = GetFeatherPrefab();
        if (prefab == null)
        {
            return false;
        }
        ItemDrop.ItemData data = prefab.m_itemData.Clone();
        data.m_dropPrefab = prefab.gameObject;
        data.m_stack = amount;
        data.m_quality = 1;
        return inventory.AddItem(data);
    }

    // Whether a whole batch fits. Inventory.AddItem fills what it can and drops the rest, so checking for just any
    // stack with room would lose part of the batch after its consumables were taken.
    private static bool CanFitFeathers(Inventory inventory)
    {
        ItemDrop prefab = GetFeatherPrefab();
        return prefab == null || inventory.CanAddItem(prefab.m_itemData, FeathersPerBatch);
    }

    private int GetFeatherCount()
    {
        Inventory inventory = _container != null ? _container.GetInventory() : null;
        if (inventory == null)
        {
            return 0;
        }
        int count = 0;
        foreach (ItemDrop.ItemData item in inventory.GetAllItems())
        {
            if (IsFeather(item))
            {
                count += item.m_stack;
            }
        }
        return count;
    }

    #endregion

    #region Unity lifecycle / production

    private void Awake()
    {
        _nview = GetComponent<ZNetView>();
        _piece = GetComponent<Piece>();
        _container = GetComponent<Container>();
        if (_container == null)
        {
            _container = GetComponentInChildren<Container>(includeInactive: true);
        }
        if (_piece != null && !string.IsNullOrEmpty(_piece.m_name))
        {
            _name = _piece.m_name;
        }
        if (_nview != null && _nview.IsValid() && _container != null && _container.GetInventory() != null)
        {
            InitializePersistentTimer();
            InvokeRepeating(nameof(UpdateOwnershipTick), 1f, 1f);
        }
    }

    private void Start()
    {
        // Subscribe in Start: Container.Awake assigns (not adds to) m_onChanged, so hooking in our Awake could be overwritten.
        _enabledBirdbox = ProducerVisuals.FindChildRecursive(transform, EnabledBirdboxChildName);
        _itemCrafted = ProducerVisuals.FindChildRecursive(transform, ItemCraftedChildName);
        ProducerVisuals.RouteAudioToSfx(_enabledBirdbox);
        _sfx = _enabledBirdbox != null ? ProducerVisuals.FindChildRecursive(_enabledBirdbox.transform, SfxChildName) : null;
        ApplySfxSetting();
        _subscribedInventory = _container != null ? _container.GetInventory() : null;
        if (_subscribedInventory != null)
        {
            _subscribedInventory.m_onChanged += MarkVisualsDirty;
        }
        UpdateEnabledBirdboxVisual();
    }

    // Container.Load re-adds items one at a time and each add fires m_onChanged, so reacting immediately sees a
    // half-loaded inventory (e.g. no output yet) and flips the visuals off and on. Apply once per frame instead.
    private void MarkVisualsDirty()
    {
        _visualsDirty = true;
    }

    private void LateUpdate()
    {
        if (_visualsDirty)
        {
            _visualsDirty = false;
            UpdateEnabledBirdboxVisual();
        }
    }

    private void OnDestroy()
    {
        if (_subscribedInventory != null)
        {
            _subscribedInventory.m_onChanged -= MarkVisualsDirty;
            _subscribedInventory = null;
        }
    }

    internal static void RefreshAllSfx()
    {
        foreach (BirdHouse instance in FindObjectsByType<BirdHouse>(FindObjectsSortMode.None))
        {
            instance.ApplySfxSetting();
        }
    }

    private void ApplySfxSetting()
    {
        ProducerVisuals.SetSfxVolume(_sfx, MainConfig.birdhouseSoundVolume.Value);
        ProducerVisuals.SetSfxEnabled(_sfx, MainConfig.birdhouseSoundEnabled.Value);
    }

    internal static void RefreshAllItemCrafted()
    {
        foreach (BirdHouse instance in FindObjectsByType<BirdHouse>(FindObjectsSortMode.None))
        {
            instance.UpdateItemCraftedVisual();
        }
    }

    // Shows the item_crafted meshes only while the container holds produced output.
    private void UpdateItemCraftedVisual()
    {
        if (_itemCrafted == null)
        {
            return;
        }
        bool hasOutput = MainConfig.birdhouseShowIcon.Value && GetFeatherCount() > 0;
        if (_itemCrafted.activeSelf != hasOutput)
        {
            _itemCrafted.SetActive(hasOutput);
        }
    }

    private void UpdateEnabledBirdboxVisual()
    {
        UpdateItemCraftedVisual();
        if (_enabledBirdbox == null)
        {
            return;
        }
        Inventory inventory = _container != null ? _container.GetInventory() : null;
        bool hasItems = inventory != null && inventory.NrOfItems() > 0;
        if (_enabledBirdbox.activeSelf != hasItems)
        {
            if (!hasItems)
            {
                ProducerVisuals.UnregisterLoopingSfx(_enabledBirdbox);
            }
            _enabledBirdbox.SetActive(hasItems);
        }
    }

    private void UpdateOwnershipTick()
    {
        bool shouldRun = _nview != null && _nview.IsValid() && _nview.IsOwner();
        if (shouldRun && !_productionTickRunning)
        {
            InvokeRepeating(nameof(UpdateBirdHouse), 1f, 1f);
            _productionTickRunning = true;
        }
        else if (!shouldRun && _productionTickRunning)
        {
            CancelInvoke(nameof(UpdateBirdHouse));
            _productionTickRunning = false;
        }
    }

    private void UpdateBirdHouse()
    {
        if (!_nview.IsValid() || !_nview.IsOwner())
        {
            return;
        }
        Inventory inventory = _container.GetInventory();
        if (inventory == null)
        {
            return;
        }
        if (!CanProduceOneBatchNow(inventory))
        {
            ResetProductionClock();
            return;
        }

        StartClockIfIdle();
        float acc = GetAccumulator() + (float)GetElapsedSinceStart();
        int byTime = acc / SecPerBatch >= int.MaxValue ? int.MaxValue : Mathf.FloorToInt(acc / SecPerBatch);
        int byFood = GetConsumableCount(inventory) / ConsumablePerBatch;
        int bySpace = Mathf.Max(0, MaxFeathers - GetFeatherCount()) / FeathersPerBatch;
        int batches = Mathf.Min(byTime, Mathf.Min(byFood, bySpace));
        if (batches <= 0)
        {
            // Nothing is saved until something is produced. Every ZDO change makes the container reload its whole
            // inventory on every nearby client, so writing the timer each tick would do that every second.
            return;
        }

        int produced = 0;
        for (int i = 0; i < batches; i++)
        {
            if (!CanProduceOneBatchNow(inventory) ||
                !TryConsumeConsumable(inventory, ConsumablePerBatch) ||
                !TryAddFeathers(inventory, FeathersPerBatch))
            {
                break;
            }
            produced++;
        }
        if (produced <= 0)
        {
            ResetProductionClock();
            return;
        }

        acc = Mathf.Max(0f, acc - produced * SecPerBatch);
        if (!CanProduceOneBatchNow(inventory))
        {
            ResetProductionClock();
        }
        else
        {
            RestartClock(acc);
        }
    }

    private bool CanProduceOneBatchNow(Inventory inventory)
    {
        return HasEnoughConsumableForBatch(inventory) && !IsFullForProduction(inventory) && GetFeatherPrefab() != null;
    }

    private bool IsFullForProduction(Inventory inventory)
    {
        if (Mathf.Max(0, MaxFeathers - GetFeatherCount()) < FeathersPerBatch)
        {
            return true;
        }
        return !CanFitFeathers(inventory);
    }

    private string BuildHoverText()
    {
        string accepted = GetAcceptedLine();
        string name = Localization.instance.Localize(_name);
        if (!PrivateArea.CheckAccess(transform.position, 0f, flash: false))
        {
            return Localization.instance.Localize(name + "\n" + accepted + "\n$piece_noaccess");
        }

        string open = Localization.instance.Localize("[<color=yellow><b>$KEY_Use</b></color>] $piece_container_open");
        Inventory inventory = _container != null ? _container.GetInventory() : null;
        string status;
        string color = "red";
        if (inventory == null)
        {
            status = TokenNoConsumable;
        }
        else if (IsFullForProduction(inventory))
        {
            status = TokenFull;
        }
        else if (!HasEnoughConsumableForBatch(inventory))
        {
            status = TokenNoConsumable;
        }
        else
        {
            status = TokenWorking;
            color = "orange";
        }
        string statusText = Localization.instance.Localize(status);
        if (status == TokenWorking)
        {
            statusText += " " + FormatCountdown(GetSecondsUntilNextBatch());
        }
        return $"{name} - ({GetFeatherCount()}/{MaxFeathers})\n{accepted}\n<color={color}>{statusText}</color>\n{open}";
    }

    // Read-only view of the ZDO timer so every client (not just the owner) can show the countdown.
    private float GetSecondsUntilNextBatch()
    {
        if (_nview == null || !_nview.IsValid() || ZNet.instance == null)
        {
            return SecPerBatch;
        }
        DateTime now = ZNet.instance.GetTime();
        long ticks = _nview.GetZDO().GetLong(ZdoStartTimeHash, now.Ticks);
        double sinceLastTick = ticks >= DateTime.MinValue.Ticks && ticks <= DateTime.MaxValue.Ticks
            ? Math.Max(0.0, (now - new DateTime(ticks)).TotalSeconds)
            : 0.0;
        double progress = GetAccumulator() + sinceLastTick;
        return Mathf.Max(0f, SecPerBatch - (float)progress);
    }

    private static string FormatCountdown(float seconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(Mathf.CeilToInt(seconds));
        return time.TotalHours >= 1.0
            ? $"({(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00})"
            : $"({time.Minutes:00}:{time.Seconds:00})";
    }

    #endregion

    #region Persistent timer (ZDO backed so production continues while unloaded)

    private void InitializePersistentTimer()
    {
        if (!_nview.IsOwner())
        {
            return;
        }
        ZDO zdo = _nview.GetZDO();
        if (ZNet.instance != null && zdo.GetLong(ZdoStartTimeHash, 0L) == 0L)
        {
            zdo.Set(ZdoStartTimeHash, ZNet.instance.GetTime().Ticks);
        }
        float acc = zdo.GetFloat(ZdoAccTimeHash);
        if (float.IsNaN(acc) || float.IsInfinity(acc) || acc < 0f)
        {
            zdo.Set(ZdoAccTimeHash, 0f);
        }
    }

    // Seconds since the clock was last started; read-only, so the tick doesn't write the ZDO.
    private double GetElapsedSinceStart()
    {
        if (_nview == null || !_nview.IsValid() || ZNet.instance == null)
        {
            return 0.0;
        }
        DateTime now = ZNet.instance.GetTime();
        long ticks = _nview.GetZDO().GetLong(ZdoStartTimeHash, now.Ticks);
        DateTime start = ticks >= DateTime.MinValue.Ticks && ticks <= DateTime.MaxValue.Ticks ? new DateTime(ticks) : now;
        return Math.Max(0.0, (now - start).TotalSeconds);
    }

    private void StartClockIfIdle()
    {
        if (_nview != null && _nview.IsValid() && _nview.IsOwner() && ZNet.instance != null
            && _nview.GetZDO().GetLong(ZdoStartTimeHash) == IdleStartTime)
        {
            _nview.GetZDO().Set(ZdoStartTimeHash, ZNet.instance.GetTime().Ticks);
        }
    }

    private void RestartClock(float acc)
    {
        if (_nview == null || !_nview.IsValid() || !_nview.IsOwner() || ZNet.instance == null)
        {
            return;
        }
        SetAccumulator(acc);
        _nview.GetZDO().Set(ZdoStartTimeHash, ZNet.instance.GetTime().Ticks);
    }

    private float GetAccumulator()
    {
        if (_nview == null || !_nview.IsValid())
        {
            return 0f;
        }
        float acc = _nview.GetZDO().GetFloat(ZdoAccTimeHash);
        return float.IsNaN(acc) || float.IsInfinity(acc) || acc < 0f ? 0f : acc;
    }

    private void SetAccumulator(float value)
    {
        if (_nview != null && _nview.IsValid() && _nview.IsOwner())
        {
            _nview.GetZDO().Set(ZdoAccTimeHash, float.IsNaN(value) || float.IsInfinity(value) || value < 0f ? 0f : value);
        }
    }

    // Stops the clock while the producer can't work. ZDO.Set skips unchanged values, so after the first idle tick this
    // writes nothing.
    private void ResetProductionClock()
    {
        if (_nview == null || !_nview.IsValid() || !_nview.IsOwner())
        {
            return;
        }
        SetAccumulator(0f);
        _nview.GetZDO().Set(ZdoStartTimeHash, IdleStartTime);
    }

    #endregion
}
