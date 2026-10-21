using Jotunn.Managers;
using UnityEngine;

namespace CorewoodAdditions;

internal static class PieceAssets
{
    public static EffectList buildWood;
    public static EffectList breakWood;
    public static EffectList hitWood;
    public static EffectList buildStone;
    public static EffectList breakStone;
    public static EffectList hitStone;
    public static EffectList buildMetal;
    public static EffectList breakMetal;
    public static EffectList hitMetal;
    public static EffectList breakGlass;
    public static EffectList doorOpen;
    public static EffectList doorClose;
    public static EffectList gateOpen;
    public static EffectList gateClose;
    public static EffectList chestOpen;
    public static EffectList chestClose;
    public static EffectList bridgeOC;
    public static EffectList towerOC;
    public static EffectList elevator;
    public static EffectList elevatorLong;
    public static EffectList m_activateSFX;
    public static EffectList m_deactivateSFX;
    public static EffectList placeWorkbench;
    public static EffectList addEffects;
    public static EffectList harvestEffects;
    public static EffectList spawnEffects;
    public static EffectList addFuelEffects;
    public static EffectList addOreEffects;
    public static EffectList produceEffects;
    public static EffectList kilnAddOreEffects;
    public static EffectList kilnProduceEffects;
    public static EffectList frostKilnAddOreEffects;
    public static EffectList frostKilnProduceEffects;
    public static EffectList windmillAddOreEffects;
    public static EffectList windmillProduceEffects;
    public static EffectList fermenterAddEffects;
    public static EffectList fermenterTapEffects;
    public static EffectList fermenterSpawnEffects;
    public static EffectList cookingEffects;
    public static EffectList cookingPickEffects;
    public static EffectList cauldrenCraftEffects;
    public static EffectList cauldrenDoneEffects;


    public static EffectList portalConnected;

    public static ItemDrop fuelCoal;
    public static ItemDrop fuelResin;
    public static ItemDrop fuelIce;

    private static bool s_built;

    public static void Init()
    {
        if (s_built)
        {
            return;
        }

        buildWood = Effects("sfx_build_hammer_wood");
        breakWood = Effects("sfx_wood_break");
        hitWood = Effects("sfx_wood_hit");

        buildStone = Effects("sfx_build_hammer_stone");
        breakStone = Effects("sfx_rock_destroyed");
        hitStone = Effects("sfx_rock_hit");

        buildMetal = Effects("sfx_build_hammer_metal");
        breakMetal = Effects("sfx_metal_blocked");
        hitMetal = Effects("sfx_metal_blocked");

        breakGlass = Effects("sfx_break_glass_mm");

        doorOpen = Effects("sfx_door_open");
        doorClose = Effects("sfx_door_close");

        gateOpen = Effects("sfx_open_gate_mm");
        gateClose = Effects("sfx_close_gate_mm");

        chestOpen = Effects("sfx_chest_open");
        chestClose = Effects("sfx_chest_close");

        bridgeOC = Effects("sfx_open_bridge_mm");
        towerOC = Effects("sfx_tower_bridge_mm");
        elevator = Effects("sfx_elevator_mm");
        elevatorLong = Effects("sfx_elevator_long_mm");

        m_activateSFX = Effects("fx_guardstone_activate");
        m_deactivateSFX = Effects("fx_guardstone_deactivate");
        placeWorkbench = Effects("vfx_Place_workbench");

        addEffects = Effects("vfx_fermenter_add");
        harvestEffects = Effects("sfx_pickable_pick");
        spawnEffects = Effects("vfx_lootspawn", "sfx_lootspawn");

        addFuelEffects = Effects("vfx_walltorch_addFuel", "sfx_FireAddFuel");
        addOreEffects = Effects("vfx_smelter_addore", "sfx_smelter_add");
        produceEffects = Effects("vfx_smelter_produce", "sfx_smelter_produce");
        kilnAddOreEffects = Effects("vfx_kiln_addore", "sfx_kiln_addore");
        kilnProduceEffects = Effects("vfx_kiln_produce", "sfx_kiln_produce");
        frostKilnAddOreEffects = Effects("vfx_frostkiln_addore", "sfx_kiln_addore");
        frostKilnProduceEffects = Effects("vfx_frostkiln_produce", "sfx_kiln_produce");
        windmillAddOreEffects = Effects("vfx_mill_add", "sfx_mill_add");
        windmillProduceEffects = Effects("vfx_mill_produce", "sfx_mill_produce");

        fermenterAddEffects = Effects("vfx_fermenter_add", "sfx_fermenter_add");
        fermenterTapEffects = Effects("vfx_fermenter_tap", "sfx_fermenter_tap");
        fermenterSpawnEffects = Effects("vfx_lootspawn", "sfx_lootspawn");

        cookingEffects = Effects("vfx_cooking_station_transform", "sfx_cooking_station_done");
        cookingPickEffects = Effects("vfx_cooking_station_transform", "sfx_cooking_station_take");

        cauldrenCraftEffects = Effects("sfx_gui_craftitem_cauldron");
        cauldrenDoneEffects = Effects("sfx_gui_craftitem_cauldron_end");

        portalConnected = Effects("fx_portal_connected");

        fuelCoal = PrefabManager.Cache.GetPrefab<GameObject>("Coal").GetComponent<ItemDrop>();
        fuelResin = PrefabManager.Cache.GetPrefab<GameObject>("Resin").GetComponent<ItemDrop>();
        fuelIce = PrefabManager.Cache.GetPrefab<GameObject>("Ice").GetComponent<ItemDrop>();

        s_built = true;
    }

    public static EffectList Effects(params string[] prefabNames)
    {
        EffectList.EffectData[] effects = new EffectList.EffectData[prefabNames.Length];
        for (int i = 0; i < prefabNames.Length; i++)
        {
            effects[i] = new EffectList.EffectData { m_prefab = PrefabManager.Cache.GetPrefab<GameObject>(prefabNames[i]) };
        }
        return new EffectList { m_effectPrefabs = effects };
    }
}