using System.Collections.Generic;
using Jotunn.Managers;
using UnityEngine;

namespace CorewoodAdditions.Producers;

// Collects the wisps the Corewood wisp fountain lures into its container. Vanilla lured wisps fly to the fountain's
// spawn point and hover there until picked; the owner of the fountain takes any that reach it instead. When the
// container is full they are left hovering, so the fountain reports itself full as it does in vanilla.
internal sealed class WispCollector : MonoBehaviour
{
    private const string WispItemName = "Wisp";
    private const string ItemCraftedChildName = "item_crafted";
    internal const string SfxChildName = "SFX";
    private const float CollectInterval = 1f;

    private static readonly List<LuredWisp> Nearby = new List<LuredWisp>();

    private ZNetView _nview;
    private WispSpawner _spawner;
    private Container _container;
    private GameObject _itemCrafted;
    private GameObject _sfx;
    private Inventory _subscribedInventory;
    private bool _visualsDirty;

    #region Game hooks (patched in GamePatches)

    // WispSpawner is the fountain's Hoverable, so the container's open prompt and wisp count are added to its text.
    internal static void OnSpawnerHoverText(WispSpawner spawner, ref string result)
    {
        WispCollector collector = spawner.GetComponent<WispCollector>();
        if (collector == null || collector._container == null)
        {
            return;
        }
        if (!PrivateArea.CheckAccess(spawner.transform.position, 0f, flash: false))
        {
            result += Localization.instance.Localize("\n$piece_noaccess");
            return;
        }
        int count = collector.GetWispCount();
        result += Localization.instance.Localize("\n" + GetWispItemName() + ": " + count + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_container_open");
    }

    #endregion

    private void Awake()
    {
        _nview = GetComponent<ZNetView>();
        _spawner = GetComponent<WispSpawner>();
        _container = GetComponent<Container>();
        if (_nview != null && _nview.IsValid() && _spawner != null && _spawner.m_spawnPoint != null && _container != null)
        {
            InvokeRepeating(nameof(CollectWisps), CollectInterval, CollectInterval);
        }
    }

    private void Start()
    {
        // Subscribe in Start: Container.Awake assigns (not adds to) m_onChanged, so hooking in our Awake could be overwritten.
        _itemCrafted = ProducerVisuals.FindChildRecursive(transform, ItemCraftedChildName);
        _sfx = ProducerVisuals.FindChildRecursive(transform, SfxChildName);
        ProducerVisuals.RouteAudioToSfx(_sfx);
        _subscribedInventory = _container != null ? _container.GetInventory() : null;
        if (_subscribedInventory != null)
        {
            _subscribedInventory.m_onChanged += MarkVisualsDirty;
        }
        ApplySize();
        UpdateVisuals();
    }

    // Also updates the prefab: Container.Awake builds the inventory from its size, so fountains that load after the
    // server's values arrive start at the synced size instead of this player's local one.
    internal static void RefreshAllSizes()
    {
        Container prefabContainer = PrefabManager.Instance.GetPrefab(AddPrefabs.WisplurePrefabName)?.GetComponent<Container>();
        if (prefabContainer != null)
        {
            prefabContainer.m_width = Columns;
            prefabContainer.m_height = Rows;
        }
        foreach (WispCollector instance in FindObjectsByType<WispCollector>(FindObjectsSortMode.None))
        {
            instance.ApplySize();
        }
    }

    // Writes the configured size to the vanilla Container settings and to the live inventory.
    private void ApplySize()
    {
        if (_container == null)
        {
            return;
        }
        _container.m_width = Columns;
        _container.m_height = Rows;
        Inventory inventory = _container.GetInventory();
        if (inventory != null)
        {
            inventory.m_width = Columns;
            inventory.SetHeight(Rows);
        }
    }

    internal static int Columns => Mathf.Clamp(MainConfig.wisplureContainerColumns.Value, 1, MaxColumns);
    internal static int Rows => Mathf.Clamp(MainConfig.wisplureContainerRows.Value, 1, MaxRows);
    internal const int MaxColumns = 8;
    internal const int MaxRows = 4;

    // Container.Load re-adds items one at a time and each add fires m_onChanged, so apply once per frame instead.
    private void MarkVisualsDirty()
    {
        _visualsDirty = true;
    }

    private void LateUpdate()
    {
        if (_visualsDirty)
        {
            _visualsDirty = false;
            UpdateVisuals();
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

    internal static void RefreshAllVisuals()
    {
        foreach (WispCollector instance in FindObjectsByType<WispCollector>(FindObjectsSortMode.None))
        {
            instance.UpdateVisuals();
        }
    }

    // The item_crafted icon and the SFX loop are on only while the container holds wisps.
    private void UpdateVisuals()
    {
        bool hasWisps = GetWispCount() > 0;
        if (_itemCrafted != null)
        {
            bool showIcon = MainConfig.wisplureShowIcon.Value && hasWisps;
            if (_itemCrafted.activeSelf != showIcon)
            {
                _itemCrafted.SetActive(showIcon);
            }
        }
        ProducerVisuals.SetSfxVolume(_sfx, MainConfig.wisplureSoundVolume.Value);
        ProducerVisuals.SetSfxEnabled(_sfx, MainConfig.wisplureSoundEnabled.Value && hasWisps);
    }

    private int GetWispCount()
    {
        Inventory inventory = _container != null ? _container.GetInventory() : null;
        // Every stored wisp counts, including ones collected before a world level change.
        return inventory != null ? inventory.CountItems(GetWispItemName(), matchWorldLevel: false) : 0;
    }

    private void CollectWisps()
    {
        if (!MainConfig.wisplureCollectWisps.Value || !_nview.IsValid() || !_nview.IsOwner())
        {
            return;
        }
        Inventory inventory = _container.GetInventory();
        if (inventory == null)
        {
            return;
        }
        Vector3 point = _spawner.m_spawnPoint.position;
        float rangeSqr = _spawner.m_nearbyTreshold * _spawner.m_nearbyTreshold;
        Nearby.Clear();
        foreach (LuredWisp wisp in LuredWisp.m_wisps)
        {
            if (wisp != null && Utils.DistanceSqr(point, wisp.transform.position) < rangeSqr)
            {
                Nearby.Add(wisp);
            }
        }
        foreach (LuredWisp wisp in Nearby)
        {
            if (!TryCollect(wisp, inventory))
            {
                break;
            }
        }
        Nearby.Clear();
    }

    // Returns false once the container has no room, so the rest are left for the next tick.
    private static bool TryCollect(LuredWisp wisp, Inventory inventory)
    {
        ZNetView wispView = wisp.m_nview;
        if (wispView == null || !wispView.IsValid())
        {
            return true;
        }
        GetDrop(wisp, out GameObject itemPrefab, out int amount);
        ItemDrop itemDrop = itemPrefab != null ? itemPrefab.GetComponent<ItemDrop>() : null;
        if (itemDrop == null)
        {
            return false;
        }
        ItemDrop.ItemData item = itemDrop.m_itemData.Clone();
        item.m_dropPrefab = itemPrefab;
        item.m_stack = amount;
        item.m_quality = 1;
        if (!inventory.CanAddItem(item, amount) || !inventory.AddItem(item))
        {
            return false;
        }
        wisp.m_despawnEffects.Create(wisp.transform.position, wisp.transform.rotation);
        wispView.ClaimOwnership();
        wispView.Destroy();
        return true;
    }

    // What picking the wisp by hand gives, falling back to one Wisp.
    private static void GetDrop(LuredWisp wisp, out GameObject itemPrefab, out int amount)
    {
        Pickable pickable = wisp.GetComponent<Pickable>();
        if (pickable != null && pickable.m_itemPrefab != null)
        {
            itemPrefab = pickable.m_itemPrefab;
            amount = Mathf.Max(1, pickable.m_amount);
            return;
        }
        itemPrefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(WispItemName) : null;
        amount = 1;
    }

    private static string GetWispItemName()
    {
        string token = ObjectDB.instance?.GetItemPrefab(WispItemName)?.GetComponent<ItemDrop>()?.m_itemData?.m_shared?.m_name;
        return string.IsNullOrEmpty(token) ? WispItemName : token;
    }
}
