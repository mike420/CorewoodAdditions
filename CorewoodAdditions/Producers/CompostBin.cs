using System;
using System.Collections.Generic;
using UnityEngine;

namespace CorewoodAdditions.Producers;

internal sealed class CompostBin : MonoBehaviour
{
    internal const string PrefabName = "mm_corewood_compost_bin";
    private const string EnabledBinChildName = "enabled_bin";
    private const string ItemCraftedChildName = "item_crafted";

    private const string TokenWorking = "$text_compostbin_working";
    private const string TokenNoConsumable = "$text_compostbin_no_consumable";
    private const string TokenFull = "$text_compostbin_full";

    private static readonly int ZdoStartTimeHash = "mm_compostbin_starttime".GetStableHashCode();
    private static readonly int ZdoAccTimeHash = "mm_compostbin_acctime".GetStableHashCode();
    // Start time while the producer is idle: the clock is stopped and no progress builds up.
    private const long IdleStartTime = -1L;

    internal sealed class BaitDefinition
    {
        public readonly string ConfigLabel;
        public readonly string[] PrefabCandidates;
        public readonly string[] Keywords;
        public readonly string DefaultConsumable;
        public int Amount;
        public ItemDrop ItemPrefab;

        public BaitDefinition(string configLabel, string[] prefabCandidates, string[] keywords, string defaultConsumable, int defaultAmount)
        {
            ConfigLabel = configLabel;
            PrefabCandidates = prefabCandidates;
            Keywords = keywords;
            DefaultConsumable = defaultConsumable;
            Amount = defaultAmount;
        }
    }

    internal static readonly BaitDefinition[] Baits =
    {
        new BaitDefinition("Fishing Bait", new[] { "FishingBait" }, Array.Empty<string>(), "RawMeat", 10),
        new BaitDefinition("Mossy Fishing Bait", new[] { "FishingBaitForest", "BaitForest" }, new[] { "forest", "mossy" }, "DeerMeat", 10),
        new BaitDefinition("Sticky Fishing Bait", new[] { "FishingBaitSwamp", "BaitSwamp" }, new[] { "swamp", "sticky" }, "Entrails", 10),
        new BaitDefinition("Cold Fishing Bait", new[] { "FishingBaitCave", "FishingBaitMountain", "BaitCave", "BaitMountain" }, new[] { "cave", "mountain", "cold" }, "WolfMeat", 10),
        new BaitDefinition("Stingy Fishing Bait", new[] { "FishingBaitPlains", "BaitPlains" }, new[] { "plain", "stingy" }, "LoxMeat", 10),
        new BaitDefinition("Heavy Fishing Bait", new[] { "FishingBaitOcean", "BaitOcean" }, new[] { "ocean", "heavy" }, "SerpentMeat", 10),
        new BaitDefinition("Misty Fishing Bait", new[] { "FishingBaitMistlands", "BaitMistlands" }, new[] { "mist" }, "HareMeat", 10),
        new BaitDefinition("Hot Fishing Bait", new[] { "FishingBaitAshlands", "BaitAshlands" }, new[] { "ash", "hot" }, "AsksvinMeat", 10),
        new BaitDefinition("Frosty Fishing Bait", new[] { "FishingBaitDeepNorth", "BaitDeepNorth" }, new[] { "north", "frost" }, "SealBlubber", 10),
        // Our own item: no vanilla recipe to copy an amount from, so one bait per cycle. Used by the Ocean Fish Trap.
        new BaitDefinition("Serpent Bait", new[] { FishTrap.SerpentBaitName }, Array.Empty<string>(), "RottenMeat", 1)
    };

    private static string[] _cachedRaws = Array.Empty<string>();
    private static Dictionary<string, int> _foodToBait = new Dictionary<string, int>(StringComparer.Ordinal);
    private static readonly HashSet<string> BaitPrefabNames = new HashSet<string>(StringComparer.Ordinal);
    private static readonly int[] ScratchCounts = new int[Baits.Length];
    private static readonly Dictionary<string, string> DisplayCache = new Dictionary<string, string>(StringComparer.Ordinal);
    private static ObjectDB _resolvedDb;

    private ZNetView _nview;
    private Piece _piece;
    private Container _container;
    private string _name = "$piece_container";
    private bool _productionTickRunning;
    private GameObject _enabledBin;
    private GameObject _itemCrafted;
    private Inventory _subscribedInventory;
    private bool _visualsDirty;

    private static float SecPerCycle => Mathf.Max(0.1f, MainConfig.compostSecPerCycle.Value);
    private static int ConsumablePerCycle => Mathf.Max(1, MainConfig.compostConsumablePerCycle.Value);
    private static int MaxBait => Mathf.Max(1, MainConfig.compostMaxBait.Value);

    #region Game hooks (patched in GamePatches)

    internal static void OnContainerHoverName(Container container, ref string result)
    {
        CompostBin bin = container.GetComponentInParent<CompostBin>();
        if (bin != null)
        {
            result = bin._name;
        }
    }

    internal static void OnContainerHoverText(Container container, ref string result)
    {
        CompostBin bin = container.GetComponentInParent<CompostBin>();
        if (bin != null)
        {
            result = bin.BuildHoverText();
        }
    }

    internal static void OnObjectDBAwake()
    {
        _resolvedDb = null;
        BaitPrefabNames.Clear();
        DisplayCache.Clear();
    }

    internal static void OnLanguageChanged()
    {
        DisplayCache.Clear();
    }

    #endregion

    #region Bait / consumable resolution

    private static int AmountFor(int baitIndex)
    {
        return Mathf.Max(1, Baits[baitIndex].Amount);
    }

    private static int MinAmount()
    {
        int min = int.MaxValue;
        for (int i = 0; i < Baits.Length; i++)
        {
            min = Mathf.Min(min, AmountFor(i));
        }
        return min;
    }

    private static string[] ReadRaws()
    {
        string[] raws = new string[Baits.Length];
        for (int i = 0; i < raws.Length; i++)
        {
            string value = MainConfig.compostConsumables.Length > i && MainConfig.compostConsumables[i] != null
                ? MainConfig.compostConsumables[i].Value ?? string.Empty
                : string.Empty;
            // A disabled Serpent Bait reads as having no input, so it drops out of the food map and hover text.
            if (!MainConfig.serpentBaitEnabled.Value && Baits[i].PrefabCandidates[0] == FishTrap.SerpentBaitName)
            {
                value = string.Empty;
            }
            raws[i] = value + "#" + ConsumablePerCycle;
        }
        return raws;
    }

    private static void EnsureBaitsResolved()
    {
        ObjectDB db = ObjectDB.instance;
        if (db == null || db == _resolvedDb || db.m_items == null || db.m_items.Count == 0)
        {
            return;
        }
        _resolvedDb = db;
        BaitPrefabNames.Clear();

        List<ItemDrop> candidates = new List<ItemDrop>();
        foreach (GameObject item in db.m_items)
        {
            if (item != null && item.name.IndexOf("bait", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                ItemDrop drop = item.GetComponent<ItemDrop>();
                if (drop != null && !candidates.Contains(drop))
                {
                    candidates.Add(drop);
                }
            }
        }
        foreach (GameObject item in db.m_items)
        {
            if (item == null || !item.TryGetComponent<Fish>(out Fish fish))
            {
                continue;
            }
            foreach (Fish.BaitSetting bait in fish.m_baits)
            {
                if (bait.m_bait != null && !candidates.Contains(bait.m_bait))
                {
                    candidates.Add(bait.m_bait);
                }
            }
        }

        HashSet<ItemDrop> used = new HashSet<ItemDrop>();
        foreach (BaitDefinition def in Baits)
        {
            def.ItemPrefab = null;
            foreach (string name in def.PrefabCandidates)
            {
                def.ItemPrefab = candidates.Find(d => !used.Contains(d) && string.Equals(d.name, name, StringComparison.OrdinalIgnoreCase));
                if (def.ItemPrefab != null)
                {
                    break;
                }
            }
            if (def.ItemPrefab == null)
            {
                foreach (string keyword in def.Keywords)
                {
                    def.ItemPrefab = candidates.Find(d => !used.Contains(d) && d.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (def.ItemPrefab != null)
                    {
                        break;
                    }
                }
            }
            if (def.ItemPrefab == null)
            {
                continue;
            }

            used.Add(def.ItemPrefab);
            BaitPrefabNames.Add(def.ItemPrefab.name);

            // Match the output amount to the vanilla crafting recipe for this bait.
            if (db.m_recipes == null)
            {
                continue;
            }
            foreach (Recipe recipe in db.m_recipes)
            {
                if (recipe != null && recipe.m_item != null && recipe.m_item.name == def.ItemPrefab.name)
                {
                    if (recipe.m_amount > 0)
                    {
                        def.Amount = recipe.m_amount;
                    }
                    break;
                }
            }
        }
    }

    private static void RefreshFoodMapIfNeeded()
    {
        EnsureBaitsResolved();
        string[] raws = ReadRaws();
        bool unchanged = _cachedRaws.Length == raws.Length;
        for (int i = 0; unchanged && i < raws.Length; i++)
        {
            unchanged = string.Equals(_cachedRaws[i], raws[i], StringComparison.Ordinal);
        }
        if (unchanged)
        {
            return;
        }

        _cachedRaws = raws;
        Dictionary<string, int> map = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < raws.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(raws[i]))
            {
                continue;
            }
            string food = raws[i].Split('#')[0].Split(',')[0].Trim();
            if (!string.IsNullOrWhiteSpace(food) && !map.ContainsKey(food))
            {
                map[food] = i;
            }
        }
        _foodToBait = map;
    }

    private static bool TryGetBaitIndex(ItemDrop.ItemData item, out int baitIndex)
    {
        baitIndex = -1;
        return item.m_dropPrefab != null && _foodToBait.TryGetValue(item.m_dropPrefab.name, out baitIndex);
    }

    private static string GetLocalizedItemName(string prefabName)
    {
        string token = ObjectDB.instance?.GetItemPrefab(prefabName)?.GetComponent<ItemDrop>()?.m_itemData?.m_shared?.m_name;
        return string.IsNullOrEmpty(token) ? prefabName : Localization.instance.Localize(token);
    }

    private static string GetAcceptedLines()
    {
        RefreshFoodMapIfNeeded();
        string cacheKey = string.Join("|", _cachedRaws) + ":" + (_resolvedDb != null ? "1" : "0");
        if (DisplayCache.TryGetValue(cacheKey, out string cached))
        {
            return cached;
        }

        List<string> lines = new List<string>();
        for (int i = 0; i < Baits.Length; i++)
        {
            string food = null;
            foreach (KeyValuePair<string, int> pair in _foodToBait)
            {
                if (pair.Value == i)
                {
                    food = pair.Key;
                    break;
                }
            }
            if (food == null)
            {
                continue;
            }
            ItemDrop bait = Baits[i].ItemPrefab;
            string baitName = bait != null && !string.IsNullOrEmpty(bait.m_itemData.m_shared.m_name)
                ? Localization.instance.Localize(bait.m_itemData.m_shared.m_name)
                : Baits[i].ConfigLabel;
            lines.Add("<color=yellow>" + baitName + "</color>: " + ConsumablePerCycle + "x " + GetLocalizedItemName(food));
        }

        string result = lines.Count > 0 ? string.Join("\n", lines) : "-";
        DisplayCache[cacheKey] = result;
        return result;
    }

    #endregion

    #region Inventory helpers

    private static int CountAllConsumables(Inventory inventory)
    {
        long total = 0L;
        foreach (ItemDrop.ItemData item in inventory.GetAllItems())
        {
            if (TryGetBaitIndex(item, out _))
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

    // A bait with enough food for one cycle. With requireRoom it must also have room for its whole output, so one full
    // bait type doesn't stop the others being made.
    private static bool TryFindProducibleBait(Inventory inventory, bool requireRoom, out int baitIndex)
    {
        baitIndex = -1;
        RefreshFoodMapIfNeeded();
        int perCycle = ConsumablePerCycle;
        int roomLeft = requireRoom ? Mathf.Max(0, MaxBait - CountBait(inventory)) : int.MaxValue;
        Array.Clear(ScratchCounts, 0, ScratchCounts.Length);
        List<ItemDrop.ItemData> items = inventory.GetAllItems();
        foreach (ItemDrop.ItemData item in items)
        {
            if (TryGetBaitIndex(item, out int index) && index >= 0 && index < ScratchCounts.Length)
            {
                ScratchCounts[index] += item.m_stack;
            }
        }
        foreach (ItemDrop.ItemData item in items)
        {
            if (TryGetBaitIndex(item, out int index) && index >= 0 && index < ScratchCounts.Length &&
                ScratchCounts[index] >= perCycle && Baits[index].ItemPrefab != null &&
                (!requireRoom || CanFitBait(inventory, index, roomLeft)))
            {
                baitIndex = index;
                return true;
            }
        }
        return false;
    }

    private static bool TryConsumeFoodForBait(Inventory inventory, int baitIndex, int amountNeeded)
    {
        int remaining = amountNeeded;
        foreach (ItemDrop.ItemData item in new List<ItemDrop.ItemData>(inventory.GetAllItems()))
        {
            if (!TryGetBaitIndex(item, out int index) || index != baitIndex)
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

    private static bool TryAddBait(Inventory inventory, int baitIndex, int amount)
    {
        ItemDrop prefab = Baits[baitIndex].ItemPrefab;
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

    // Whether a whole cycle's bait fits under Max Bait and in the container. Inventory.AddItem fills what it can and
    // drops the rest, so checking for just any stack with room would lose part of it after the food was taken.
    private static bool CanFitBait(Inventory inventory, int baitIndex, int roomLeft)
    {
        ItemDrop prefab = Baits[baitIndex].ItemPrefab;
        int amount = AmountFor(baitIndex);
        return prefab != null && roomLeft >= amount && inventory.CanAddItem(prefab.m_itemData, amount);
    }

    private int GetBaitCount()
    {
        Inventory inventory = _container != null ? _container.GetInventory() : null;
        return inventory != null ? CountBait(inventory) : 0;
    }

    private static int CountBait(Inventory inventory)
    {
        int count = 0;
        foreach (ItemDrop.ItemData item in inventory.GetAllItems())
        {
            if (item.m_dropPrefab != null && BaitPrefabNames.Contains(item.m_dropPrefab.name))
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
        _enabledBin = ProducerVisuals.FindChildRecursive(transform, EnabledBinChildName);
        _itemCrafted = ProducerVisuals.FindChildRecursive(transform, ItemCraftedChildName);
        _subscribedInventory = _container != null ? _container.GetInventory() : null;
        if (_subscribedInventory != null)
        {
            _subscribedInventory.m_onChanged += MarkVisualsDirty;
        }
        UpdateEnabledBinVisual();
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
            UpdateEnabledBinVisual();
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

    internal static void RefreshAllItemCrafted()
    {
        foreach (CompostBin instance in FindObjectsByType<CompostBin>(FindObjectsSortMode.None))
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
        EnsureBaitsResolved();
        bool hasOutput = MainConfig.compostShowIcon.Value && GetBaitCount() > 0;
        if (_itemCrafted.activeSelf != hasOutput)
        {
            _itemCrafted.SetActive(hasOutput);
        }
    }

    private void UpdateEnabledBinVisual()
    {
        UpdateItemCraftedVisual();
        if (_enabledBin == null)
        {
            return;
        }
        Inventory inventory = _container != null ? _container.GetInventory() : null;
        bool hasItems = inventory != null && inventory.NrOfItems() > 0;
        if (_enabledBin.activeSelf != hasItems)
        {
            _enabledBin.SetActive(hasItems);
        }
    }

    private void UpdateOwnershipTick()
    {
        bool shouldRun = _nview != null && _nview.IsValid() && _nview.IsOwner();
        if (shouldRun && !_productionTickRunning)
        {
            InvokeRepeating(nameof(UpdateCompostBin), 1f, 1f);
            _productionTickRunning = true;
        }
        else if (!shouldRun && _productionTickRunning)
        {
            CancelInvoke(nameof(UpdateCompostBin));
            _productionTickRunning = false;
        }
    }

    private void UpdateCompostBin()
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
        if (!CanProduceOneCycleNow(inventory))
        {
            ResetProductionClock();
            return;
        }

        StartClockIfIdle();
        float acc = GetAccumulator() + (float)GetElapsedSinceStart();
        int byTime = acc / SecPerCycle >= int.MaxValue ? int.MaxValue : Mathf.FloorToInt(acc / SecPerCycle);
        int byFood = CountAllConsumables(inventory) / ConsumablePerCycle;
        int bySpace = Mathf.Max(0, MaxBait - GetBaitCount()) / MinAmount();
        int cycles = Mathf.Min(byTime, Mathf.Min(byFood, bySpace));
        if (cycles <= 0)
        {
            // Nothing is saved until something is produced. Every ZDO change makes the container reload its whole
            // inventory on every nearby client, so writing the timer each tick would do that every second.
            return;
        }

        int produced = 0;
        for (int i = 0; i < cycles; i++)
        {
            if (!TryFindProducibleBait(inventory, requireRoom: true, out int baitIndex) ||
                !TryConsumeFoodForBait(inventory, baitIndex, ConsumablePerCycle) ||
                !TryAddBait(inventory, baitIndex, AmountFor(baitIndex)))
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

        acc = Mathf.Max(0f, acc - produced * SecPerCycle);
        if (!CanProduceOneCycleNow(inventory))
        {
            ResetProductionClock();
        }
        else
        {
            RestartClock(acc);
        }
    }

    private static bool CanProduceOneCycleNow(Inventory inventory)
    {
        return TryFindProducibleBait(inventory, requireRoom: true, out _);
    }

    // Full when no bait at all would fit, whatever food is in the container.
    private static bool IsFullForProduction(Inventory inventory)
    {
        int roomLeft = Mathf.Max(0, MaxBait - CountBait(inventory));
        for (int i = 0; i < Baits.Length; i++)
        {
            if (CanFitBait(inventory, i, roomLeft))
            {
                return false;
            }
        }
        return true;
    }

    private string BuildHoverText()
    {
        string accepted = GetAcceptedLines();
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
        else if (!TryFindProducibleBait(inventory, requireRoom: false, out _))
        {
            status = TokenNoConsumable;
        }
        else if (!CanProduceOneCycleNow(inventory))
        {
            status = TokenFull;
        }
        else
        {
            status = TokenWorking;
            color = "orange";
        }
        string statusText = Localization.instance.Localize(status);
        if (status == TokenWorking)
        {
            statusText += " " + FormatCountdown(GetSecondsUntilNextCycle());
        }
        return $"{name} - ({GetBaitCount()}/{MaxBait})\n{accepted}\n<color={color}>{statusText}</color>\n{open}";
    }

    // Read-only view of the ZDO timer so every client (not just the owner) can show the countdown.
    private float GetSecondsUntilNextCycle()
    {
        if (_nview == null || !_nview.IsValid() || ZNet.instance == null)
        {
            return SecPerCycle;
        }
        DateTime now = ZNet.instance.GetTime();
        long ticks = _nview.GetZDO().GetLong(ZdoStartTimeHash, now.Ticks);
        double sinceLastTick = ticks >= DateTime.MinValue.Ticks && ticks <= DateTime.MaxValue.Ticks
            ? Math.Max(0.0, (now - new DateTime(ticks)).TotalSeconds)
            : 0.0;
        double progress = GetAccumulator() + sinceLastTick;
        return Mathf.Max(0f, SecPerCycle - (float)progress);
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
