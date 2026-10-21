using System;
using System.Collections.Generic;
using UnityEngine;

namespace CorewoodAdditions.Producers;

internal sealed class FishTrap : MonoBehaviour
{
    internal const string PrefabName = "mm_corewood_fishtrap";
    internal const string LargePrefabName = "mm_corewood_fishtrap_large";
    internal const string OceanPrefabName = "mm_corewood_fishtrap_ocean";
    // The ocean trap can also catch serpents: Serpent Bait (made in the compost bin) yields Serpent Meat.
    internal const string SerpentBaitName = "SerpentBait";
    private const string SerpentMeatName = "SerpentMeat";
    private const string EnabledBaitChildName = "enabled_bait";
    private const string ItemCraftedChildName = "item_crafted";

    private const string TokenInvalidWater = "$text_fishtrap_invalid_water";
    private const string TokenInvalidOcean = "$text_fishtrap_invalid_ocean";
    private const string TokenTooClose = "$text_fishtrap_too_close";
    private const string TokenWorking = "$text_fishtrap_working";
    private const string TokenWrongBiome = "$text_fishtrap_wrong_biome";
    private const string TokenNoBait = "$text_fishtrap_no_bait";
    private const string TokenFull = "$text_fishtrap_full";

    // The trap must sit between these depths below the water surface.
    private const float MinWaterDepth = 0.5f;
    private const float MaxWaterDepth = 1.5f;
    // The ocean trap floats on the surface and needs this much water under it for the net.
    private const float MinOceanDepth = 4f;

    // How far two traps' collider bounds may overlap, so traps can still be built side by side.
    private const float OverlapTolerance = 0.1f;

    // Placed traps (not placement ghosts), for the overlap check when building another.
    private static readonly HashSet<FishTrap> PlacedTraps = new HashSet<FishTrap>();

    private static readonly int ZdoPlacedBiomeHash = "mm_fishtrap_biome".GetStableHashCode();
    private static readonly int ZdoStartTimeHash = "mm_fishtrap_starttime".GetStableHashCode();
    private static readonly int ZdoAccTimeHash = "mm_fishtrap_acctime".GetStableHashCode();
    // Start time while the producer is idle: the clock is stopped and no progress builds up.
    private const long IdleStartTime = -1L;

    private static readonly Dictionary<Heightmap.Biome, HashSet<string>> FishByBiome = new Dictionary<Heightmap.Biome, HashSet<string>>
    {
        [Heightmap.Biome.Meadows] = new HashSet<string>(StringComparer.Ordinal) { "Fish1", "Fish2" },
        [Heightmap.Biome.BlackForest] = new HashSet<string>(StringComparer.Ordinal) { "Fish1", "Fish2", "Fish5" },
        [Heightmap.Biome.Swamp] = new HashSet<string>(StringComparer.Ordinal) { "Fish2", "Fish6", "Fish4_cave" },
        [Heightmap.Biome.Mountain] = new HashSet<string>(StringComparer.Ordinal) { "Fish4_cave" },
        [Heightmap.Biome.Plains] = new HashSet<string>(StringComparer.Ordinal) { "Fish7", "Fish8" },
        [Heightmap.Biome.Ocean] = new HashSet<string>(StringComparer.Ordinal) { "Fish3", "Fish8", "Fish12" },
        [Heightmap.Biome.Mistlands] = new HashSet<string>(StringComparer.Ordinal) { "Fish9", "Fish12" },
        [Heightmap.Biome.AshLands] = new HashSet<string>(StringComparer.Ordinal) { "Fish11", "Fish12" },
        [Heightmap.Biome.DeepNorth] = new HashSet<string>(StringComparer.Ordinal) { "Fish10", "Fish12" }
    };

    // Bait prefab name -> fish that bite on it (built from each fish's Fish.m_baits).
    private static readonly Dictionary<string, List<ItemDrop>> BaitToFish = new Dictionary<string, List<ItemDrop>>(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> DisplayCache = new Dictionary<string, string>(StringComparer.Ordinal);
    private static ObjectDB _resolvedDb;

    private ZNetView _nview;
    private Piece _piece;
    private Container _container;
    private string _name = "$piece_container";
    private bool _productionTickRunning;
    private GameObject _enabledBait;
    private GameObject _itemCrafted;
    private Inventory _subscribedInventory;
    private bool _visualsDirty;

    private enum TrapKind
    {
        Small,
        Large,
        Ocean
    }

    // All trap variants share this component but read their own config section.
    private TrapKind _kind;
    private Collider[] _colliders = new Collider[0];

    private float SecPerFish => Mathf.Max(0.1f, (_kind switch
    {
        TrapKind.Large => MainConfig.fishtrapLargeSecPerFish,
        TrapKind.Ocean => MainConfig.fishtrapOceanSecPerFish,
        _ => MainConfig.fishtrapSecPerFish
    }).Value);
    private int MaxFish => Mathf.Max(1, (_kind switch
    {
        TrapKind.Large => MainConfig.fishtrapLargeMaxFish,
        TrapKind.Ocean => MainConfig.fishtrapOceanMaxFish,
        _ => MainConfig.fishtrapMaxFish
    }).Value);
    private bool CatchesSerpents => _kind == TrapKind.Ocean && MainConfig.serpentBaitEnabled.Value;

    private bool ShowIcon => (_kind switch
    {
        TrapKind.Large => MainConfig.fishtrapLargeShowIcon,
        TrapKind.Ocean => MainConfig.fishtrapOceanShowIcon,
        _ => MainConfig.fishtrapShowIcon
    }).Value;
    private float ChanceToIncreaseQuality => Mathf.Clamp01((_kind switch
    {
        TrapKind.Large => MainConfig.fishtrapLargeQualityChance,
        TrapKind.Ocean => MainConfig.fishtrapOceanQualityChance,
        _ => MainConfig.fishtrapQualityChance
    }).Value);

    #region Game hooks (patched in GamePatches)

    internal static void OnContainerHoverName(Container container, ref string result)
    {
        FishTrap trap = container.GetComponentInParent<FishTrap>();
        if (trap != null)
        {
            result = trap._name;
        }
    }

    internal static void OnContainerHoverText(Container container, ref string result)
    {
        FishTrap trap = container.GetComponentInParent<FishTrap>();
        if (trap != null)
        {
            result = trap.BuildHoverText();
        }
    }

    internal static void OnObjectDBAwake()
    {
        _resolvedDb = null;
        BaitToFish.Clear();
        DisplayCache.Clear();
    }

    internal static void OnLanguageChanged()
    {
        DisplayCache.Clear();
    }

    // The trap lives in water, so stop WearNTear treating it as wet (which would rot it).
    internal static void OnWearNTearIsWet(WearNTear wearNTear, ref bool result)
    {
        if (result && wearNTear.TryGetComponent<FishTrap>(out _))
        {
            result = false;
        }
    }

    internal static void OnUpdatePlacementGhost(Player player)
    {
        GameObject ghost = player.m_placementGhost;
        if (ghost == null || !ghost.TryGetComponent<FishTrap>(out _))
        {
            return;
        }
        string prefabName = Utils.GetPrefabName(ghost);
        if (prefabName == OceanPrefabName && ghost.activeSelf && ZoneSystem.instance != null)
        {
            // Vanilla lifts water pieces 3m above the surface; sit the floats on the waterline instead.
            Vector3 position = ghost.transform.position;
            position.y = ZoneSystem.instance.m_waterLevel - WaveBobber.GetFloatLineOffset(ghost);
            ghost.transform.position = position;
        }
        if (!IsValidPlacement(prefabName, ghost.transform.position) || OverlapsPlacedTrap(ghost))
        {
            player.m_placementStatus = Player.PlacementStatus.Invalid;
            ghost.GetComponent<Piece>()?.SetInvalidPlacementHeightlight(enabled: true);
        }
    }

    // InventoryGui closes a container once the player is 4m from its pivot. The ocean trap's pivot sits below the
    // waterline, so for that trap measure from the nearest point of its colliders by widening the limit for the call.
    // Returns the extra distance to add; GamePatches restores the original limit afterwards.
    internal static float GetExtraAutoCloseDistance(Container container, Player player)
    {
        if (container == null || player == null || !container.TryGetComponent(out FishTrap trap) || trap._kind != TrapKind.Ocean)
        {
            return 0f;
        }
        Vector3 playerPosition = player.transform.position;
        float pivotDistance = Vector3.Distance(container.transform.position, playerPosition);
        return Mathf.Max(0f, pivotDistance - trap.GetNearestColliderDistance(playerPosition));
    }

    // Returns false to block placement.
    internal static bool OnTryPlacePiece(Player player, Piece piece, ref bool result)
    {
        if (piece == null || !piece.TryGetComponent<FishTrap>(out _))
        {
            return true;
        }
        GameObject ghost = player.m_placementGhost;
        Vector3 position = ghost != null ? ghost.transform.position : piece.transform.position;
        string prefabName = Utils.GetPrefabName(piece.gameObject);
        bool overlaps = ghost != null && OverlapsPlacedTrap(ghost);
        if (IsValidPlacement(prefabName, position) && !overlaps)
        {
            return true;
        }
        // No highlight here: piece is the build-menu prefab, and highlighting it registers the prefab with MaterialMan,
        // which copies MaterialMan's notifier onto every trap built afterwards ("Can't unregister renderer" when they
        // unload). OnUpdatePlacementGhost already shows the ghost as invalid.
        player.Message(MessageHud.MessageType.Center, Localization.instance.Localize(overlaps ? TokenTooClose : GetInvalidPlacementToken(prefabName)));
        result = false;
        return false;
    }

    #endregion

    #region Bait / fish resolution

    private static void EnsureBaitsResolved()
    {
        ObjectDB db = ObjectDB.instance;
        if (db == null || db == _resolvedDb || db.m_items == null || db.m_items.Count == 0)
        {
            return;
        }
        _resolvedDb = db;
        BaitToFish.Clear();
        foreach (ItemDrop fishItem in db.GetAllItems(ItemDrop.ItemData.ItemType.Fish, ""))
        {
            if (!fishItem.TryGetComponent<Fish>(out Fish fish))
            {
                continue;
            }
            foreach (Fish.BaitSetting bait in fish.m_baits)
            {
                if (bait.m_bait == null || string.IsNullOrEmpty(bait.m_bait.name))
                {
                    continue;
                }
                if (!BaitToFish.TryGetValue(bait.m_bait.name, out List<ItemDrop> fishes))
                {
                    fishes = new List<ItemDrop>();
                    BaitToFish[bait.m_bait.name] = fishes;
                }
                if (!fishes.Contains(fishItem))
                {
                    fishes.Add(fishItem);
                }
            }
        }
    }

    private static bool IsFishInBiome(ItemDrop fish, Heightmap.Biome biome)
    {
        return fish != null && FishByBiome.TryGetValue(biome, out HashSet<string> names) && names.Contains(fish.name);
    }

    private static bool BaitWorksInBiome(string baitName, Heightmap.Biome biome)
    {
        EnsureBaitsResolved();
        if (baitName == null || !BaitToFish.TryGetValue(baitName, out List<ItemDrop> fishes))
        {
            return false;
        }
        return fishes.Exists(f => IsFishInBiome(f, biome));
    }

    private static bool BiomeHasAnyFish(Heightmap.Biome biome)
    {
        EnsureBaitsResolved();
        foreach (string bait in BaitToFish.Keys)
        {
            if (BaitWorksInBiome(bait, biome))
            {
                return true;
            }
        }
        return false;
    }

    private static ItemDrop.ItemData TakeRandomFish(string baitName, Heightmap.Biome biome, float qualityChance)
    {
        if (!BaitToFish.TryGetValue(baitName, out List<ItemDrop> fishes))
        {
            return null;
        }
        List<ItemDrop> candidates = fishes.FindAll(f => IsFishInBiome(f, biome));
        if (candidates.Count == 0)
        {
            return null;
        }
        ItemDrop prefab = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        ItemDrop.ItemData data = prefab.m_itemData.Clone();
        data.m_dropPrefab = prefab.gameObject;
        data.m_stack = 1;
        data.m_quality = RollQuality(data, qualityChance);
        return data;
    }

    private static int RollQuality(ItemDrop.ItemData item, float qualityChance)
    {
        int maxQuality = Mathf.Max(1, item.m_shared.m_maxQuality);
        int quality = 1;
        while (quality < maxQuality && UnityEngine.Random.value <= qualityChance)
        {
            quality++;
        }
        return quality;
    }

    private static string GetLocalizedItemName(string prefabName)
    {
        string token = ObjectDB.instance?.GetItemPrefab(prefabName)?.GetComponent<ItemDrop>()?.m_itemData?.m_shared?.m_name;
        return string.IsNullOrEmpty(token) ? prefabName : Localization.instance.Localize(token);
    }

    // One line per fish available in the biome, listing the baits that catch it.
    private static string GetFishBaitLines(Heightmap.Biome biome)
    {
        EnsureBaitsResolved();
        string cacheKey = biome + ":" + (_resolvedDb != null ? "1" : "0");
        if (DisplayCache.TryGetValue(cacheKey, out string cached))
        {
            return cached;
        }

        string result = "-";
        if (FishByBiome.TryGetValue(biome, out HashSet<string> fishNames) && fishNames.Count > 0)
        {
            List<string> lines = new List<string>();
            foreach (string fishName in fishNames)
            {
                List<string> baits = new List<string>();
                foreach (KeyValuePair<string, List<ItemDrop>> pair in BaitToFish)
                {
                    if (pair.Value.Exists(f => f.name == fishName))
                    {
                        baits.Add(GetLocalizedItemName(pair.Key));
                    }
                }
                lines.Add("<color=yellow>" + GetLocalizedItemName(fishName) + "</color>: " + (baits.Count > 0 ? string.Join(", ", baits) : "-"));
            }
            result = string.Join("\n", lines);
        }
        DisplayCache[cacheKey] = result;
        return result;
    }

    private static bool IsValidPlacement(string prefabName, Vector3 position)
    {
        if (prefabName == OceanPrefabName)
        {
            return Heightmap.FindBiome(position) == Heightmap.Biome.Ocean && IsValidOceanPlacement(position);
        }
        return IsValidWaterPlacement(position);
    }

    // Vanilla's clipping test doesn't stop traps being built inside each other (the large trap has it off, and the
    // small trap's single collider rarely clips by enough), so compare the ghost's collider bounds with each placed
    // trap's instead.
    private static bool OverlapsPlacedTrap(GameObject ghost)
    {
        if (!TryGetColliderBounds(ghost.GetComponentsInChildren<Collider>(), out Bounds ghostBounds))
        {
            return false;
        }
        ghostBounds.Expand(-OverlapTolerance * 2f);
        foreach (FishTrap trap in PlacedTraps)
        {
            if (trap != null && TryGetColliderBounds(trap._colliders, out Bounds trapBounds) && ghostBounds.Intersects(trapBounds))
            {
                return true;
            }
        }
        return false;
    }

    private static bool TryGetColliderBounds(Collider[] colliders, out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        foreach (Collider collider in colliders)
        {
            // Inactive colliders report empty bounds at the world origin, which would stretch the box there.
            if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy || collider.isTrigger)
            {
                continue;
            }
            if (found)
            {
                bounds.Encapsulate(collider.bounds);
            }
            else
            {
                bounds = collider.bounds;
                found = true;
            }
        }
        return found;
    }

    private static string GetInvalidPlacementToken(string prefabName)
    {
        return prefabName == OceanPrefabName ? TokenInvalidOcean : TokenInvalidWater;
    }

    // Only x/z matter, so this stays valid while the placed trap bobs on the waves.
    private static bool IsValidOceanPlacement(Vector3 position)
    {
        if (ZoneSystem.instance == null)
        {
            return false;
        }
        return ZoneSystem.instance.m_waterLevel - ZoneSystem.instance.GetGroundHeight(position) >= MinOceanDepth;
    }

    private float GetNearestColliderDistance(Vector3 point)
    {
        float nearest = Vector3.Distance(transform.position, point);
        foreach (Collider collider in _colliders)
        {
            if (collider != null && collider.enabled && !collider.isTrigger)
            {
                nearest = Mathf.Min(nearest, Vector3.Distance(collider.ClosestPoint(point), point));
            }
        }
        return nearest;
    }

    private bool IsPlacementValid()
    {
        return _kind == TrapKind.Ocean ? IsValidOceanPlacement(transform.position) : IsValidWaterPlacement(transform.position);
    }

    private static bool IsValidWaterPlacement(Vector3 position)
    {
        if (ZoneSystem.instance == null)
        {
            return false;
        }
        float depth = ZoneSystem.instance.m_waterLevel - position.y;
        return depth >= MinWaterDepth && depth <= MaxWaterDepth;
    }

    #endregion

    #region Inventory helpers

    private Heightmap.Biome GetPlacedBiome()
    {
        Heightmap.Biome current = Heightmap.FindBiome(transform.position);
        if (_nview == null || !_nview.IsValid())
        {
            return current;
        }
        return (Heightmap.Biome)_nview.GetZDO().GetInt(ZdoPlacedBiomeHash, (int)current);
    }

    private static bool IsSerpentBait(ItemDrop.ItemData item)
    {
        return item.m_dropPrefab != null && item.m_dropPrefab.name == SerpentBaitName && GetSerpentMeatPrefab() != null;
    }

    private static ItemDrop GetSerpentMeatPrefab()
    {
        return ObjectDB.instance?.GetItemPrefab(SerpentMeatName)?.GetComponent<ItemDrop>();
    }

    private static bool IsUsableBait(ItemDrop.ItemData item, Heightmap.Biome biome, bool serpents)
    {
        return item.m_dropPrefab != null && (BaitWorksInBiome(item.m_dropPrefab.name, biome) || (serpents && IsSerpentBait(item)));
    }

    private static ItemDrop.ItemData FindBait(Inventory inventory, Heightmap.Biome biome, bool serpents)
    {
        foreach (ItemDrop.ItemData item in inventory.GetAllItems())
        {
            if (IsUsableBait(item, biome, serpents))
            {
                return item;
            }
        }
        return null;
    }

    private static int GetBaitCount(Inventory inventory, Heightmap.Biome biome, bool serpents)
    {
        long total = 0L;
        foreach (ItemDrop.ItemData item in inventory.GetAllItems())
        {
            if (IsUsableBait(item, biome, serpents))
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

    private int GetFishCount()
    {
        Inventory inventory = _container != null ? _container.GetInventory() : null;
        if (inventory == null)
        {
            return 0;
        }
        int count = 0;
        foreach (ItemDrop.ItemData item in inventory.GetAllItems())
        {
            if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Fish ||
                (CatchesSerpents && item.m_dropPrefab != null && item.m_dropPrefab.name == SerpentMeatName))
            {
                count += item.m_stack;
            }
        }
        return count;
    }

    private static ItemDrop.ItemData MakeSerpentMeat()
    {
        ItemDrop prefab = GetSerpentMeatPrefab();
        if (prefab == null)
        {
            return null;
        }
        ItemDrop.ItemData data = prefab.m_itemData.Clone();
        data.m_dropPrefab = prefab.gameObject;
        data.m_stack = 1;
        return data;
    }

    private bool TryProduceOneFish(Inventory inventory)
    {
        Heightmap.Biome biome = GetPlacedBiome();
        ItemDrop.ItemData bait = FindBait(inventory, biome, CatchesSerpents);
        if (bait == null)
        {
            return false;
        }
        ItemDrop.ItemData fish = CatchesSerpents && IsSerpentBait(bait)
            ? MakeSerpentMeat()
            : TakeRandomFish(bait.m_dropPrefab.name, biome, ChanceToIncreaseQuality);
        if (fish == null || !inventory.AddItem(fish))
        {
            return false;
        }
        inventory.RemoveOneItem(bait);
        return true;
    }

    #endregion

    #region Unity lifecycle / production

    private void Awake()
    {
        string prefabName = Utils.GetPrefabName(gameObject);
        _kind = prefabName == LargePrefabName ? TrapKind.Large : prefabName == OceanPrefabName ? TrapKind.Ocean : TrapKind.Small;
        _colliders = GetComponentsInChildren<Collider>(includeInactive: true);
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
        if (_nview != null && _nview.IsValid())
        {
            PlacedTraps.Add(this);
        }
        if (_nview != null && _nview.IsValid() && _container != null && _container.GetInventory() != null)
        {
            InitializePlacedBiome();
            InitializePersistentTimer();
            InvokeRepeating(nameof(UpdateOwnershipTick), 1f, 1f);
        }
    }

    private void Start()
    {
        // Subscribe in Start: Container.Awake assigns (not adds to) m_onChanged, so hooking in our Awake could be overwritten.
        _enabledBait = ProducerVisuals.FindChildRecursive(transform, EnabledBaitChildName);
        _itemCrafted = ProducerVisuals.FindChildRecursive(transform, ItemCraftedChildName);
        _subscribedInventory = _container != null ? _container.GetInventory() : null;
        if (_subscribedInventory != null)
        {
            _subscribedInventory.m_onChanged += MarkVisualsDirty;
        }
        UpdateEnabledBaitVisual();
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
            UpdateEnabledBaitVisual();
        }
    }

    private void OnDestroy()
    {
        PlacedTraps.Remove(this);
        if (_subscribedInventory != null)
        {
            _subscribedInventory.m_onChanged -= MarkVisualsDirty;
            _subscribedInventory = null;
        }
    }

    internal static void RefreshAllItemCrafted()
    {
        foreach (FishTrap instance in FindObjectsByType<FishTrap>(FindObjectsSortMode.None))
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
        bool hasOutput = ShowIcon && GetFishCount() > 0;
        if (_itemCrafted.activeSelf != hasOutput)
        {
            _itemCrafted.SetActive(hasOutput);
        }
    }

    // Shows the bait meshes only while the trap holds bait that catches fish in its placed biome.
    private void UpdateEnabledBaitVisual()
    {
        UpdateItemCraftedVisual();
        if (_enabledBait == null)
        {
            return;
        }
        Inventory inventory = _container != null ? _container.GetInventory() : null;
        bool hasBait = inventory != null && FindBait(inventory, GetPlacedBiome(), CatchesSerpents) != null;
        if (_enabledBait.activeSelf != hasBait)
        {
            _enabledBait.SetActive(hasBait);
        }
    }

    // The biome is captured once at placement so fish tables don't shift at biome borders.
    private void InitializePlacedBiome()
    {
        if (_nview.IsOwner() && _nview.GetZDO().GetInt(ZdoPlacedBiomeHash) == 0)
        {
            _nview.GetZDO().Set(ZdoPlacedBiomeHash, (int)Heightmap.FindBiome(transform.position));
        }
    }

    private void UpdateOwnershipTick()
    {
        bool shouldRun = _nview != null && _nview.IsValid() && _nview.IsOwner();
        if (shouldRun && !_productionTickRunning)
        {
            InvokeRepeating(nameof(UpdateFishTrap), 1f, 1f);
            _productionTickRunning = true;
        }
        else if (!shouldRun && _productionTickRunning)
        {
            CancelInvoke(nameof(UpdateFishTrap));
            _productionTickRunning = false;
        }
    }

    private void UpdateFishTrap()
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
        if (!CanProduceOneFishNow(inventory))
        {
            ResetProductionClock();
            return;
        }

        StartClockIfIdle();
        float acc = GetAccumulator() + (float)GetElapsedSinceStart();
        int byTime = acc / SecPerFish >= int.MaxValue ? int.MaxValue : Mathf.FloorToInt(acc / SecPerFish);
        int byBait = GetBaitCount(inventory, GetPlacedBiome(), CatchesSerpents);
        int bySpace = Mathf.Max(0, MaxFish - GetFishCount());
        int catches = Mathf.Min(byTime, Mathf.Min(byBait, bySpace));
        if (catches <= 0)
        {
            // Nothing is saved until something is produced. Every ZDO change makes the container reload its whole
            // inventory on every nearby client, so writing the timer each tick would do that every second.
            return;
        }

        int produced = 0;
        for (int i = 0; i < catches; i++)
        {
            if (!CanProduceOneFishNow(inventory) || !TryProduceOneFish(inventory))
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

        acc = Mathf.Max(0f, acc - produced * SecPerFish);
        if (!CanProduceOneFishNow(inventory))
        {
            ResetProductionClock();
        }
        else
        {
            RestartClock(acc);
        }
    }

    private bool CanProduceOneFishNow(Inventory inventory)
    {
        return ObjectDB.instance != null
            && IsPlacementValid()
            && !IsFullForProduction(inventory)
            && FindBait(inventory, GetPlacedBiome(), CatchesSerpents) != null;
    }

    private bool IsFullForProduction(Inventory inventory)
    {
        return GetFishCount() >= MaxFish || !inventory.HaveEmptySlot();
    }

    private string BuildHoverText()
    {
        Heightmap.Biome biome = GetPlacedBiome();
        string lines = GetFishBaitLines(biome);
        if (CatchesSerpents && GetSerpentMeatPrefab() != null)
        {
            lines += "\n<color=yellow>" + GetLocalizedItemName(SerpentMeatName) + "</color>: " + GetLocalizedItemName(SerpentBaitName);
        }
        string name = Localization.instance.Localize(_name);
        if (!PrivateArea.CheckAccess(transform.position, 0f, flash: false))
        {
            return Localization.instance.Localize(name + "\n" + lines + "\n$piece_noaccess");
        }

        string open = Localization.instance.Localize("[<color=yellow><b>$KEY_Use</b></color>] $piece_container_open");
        Inventory inventory = _container != null ? _container.GetInventory() : null;
        string status;
        string color = "red";
        if (!IsPlacementValid())
        {
            status = _kind == TrapKind.Ocean ? TokenInvalidOcean : TokenInvalidWater;
        }
        else if (!BiomeHasAnyFish(biome))
        {
            status = TokenWrongBiome;
        }
        else if (inventory != null && IsFullForProduction(inventory))
        {
            status = TokenFull;
        }
        else if (inventory == null || FindBait(inventory, biome, CatchesSerpents) == null)
        {
            status = TokenNoBait;
        }
        else
        {
            status = TokenWorking;
            color = "orange";
        }
        string statusText = Localization.instance.Localize(status);
        if (status == TokenWorking)
        {
            statusText += " " + FormatCountdown(GetSecondsUntilNextFish());
        }
        return $"{name} - ({GetFishCount()}/{MaxFish})\n{lines}\n<color={color}>{statusText}</color>\n{open}";
    }

    // Read-only view of the ZDO timer so every client (not just the owner) can show the countdown.
    private float GetSecondsUntilNextFish()
    {
        if (_nview == null || !_nview.IsValid() || ZNet.instance == null)
        {
            return SecPerFish;
        }
        DateTime now = ZNet.instance.GetTime();
        long ticks = _nview.GetZDO().GetLong(ZdoStartTimeHash, now.Ticks);
        double sinceLastTick = ticks >= DateTime.MinValue.Ticks && ticks <= DateTime.MaxValue.Ticks
            ? Math.Max(0.0, (now - new DateTime(ticks)).TotalSeconds)
            : 0.0;
        double progress = GetAccumulator() + sinceLastTick;
        return Mathf.Max(0f, SecPerFish - (float)progress);
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
