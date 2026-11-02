using BepInEx.Configuration;
using CorewoodAdditions.Producers;
using HarmonyLib;
using Jotunn.Extensions;
using System.Collections.Generic;
using UnityEngine;

namespace CorewoodAdditions
{
    internal class MainConfig
    {
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> fishtrapShowPins;
        public static ConfigEntry<bool> serpentBaitEnabled;
        public static ConfigEntry<bool> smelterKilnEnabled;
        public static ConfigEntry<bool> windmillEnabled;
        public static ConfigEntry<bool> frostKilnEnabled;
        public static ConfigEntry<bool> cookingStationEnabled;
        public static ConfigEntry<bool> meadCauldronEnabled;
        public static ConfigEntry<bool> portalsEnabled;
        public static ConfigEntry<bool> portalAllowAllItems;
        public static ConfigEntry<bool> wisplureCollectWisps;

        public static ConfigEntry<float> compostSecPerCycle;
        public static ConfigEntry<int> compostConsumablePerCycle;
        public static ConfigEntry<int> compostMaxBait;
        public static ConfigEntry<string>[] compostConsumables = new ConfigEntry<string>[0];
        public static ConfigEntry<bool> compostShowIcon;

        public static ConfigEntry<float> beehiveSecPerBatch;
        public static ConfigEntry<int> beehiveHoneyPerBatch;
        public static ConfigEntry<int> beehiveConsumablePerBatch;
        public static ConfigEntry<int> beehiveMaxHoney;
        public static ConfigEntry<string> beehiveConsumables;
        public static ConfigEntry<bool> beehiveSoundEnabled;
        public static ConfigEntry<float> beehiveSoundVolume;
        public static ConfigEntry<bool> beehiveShowIcon;

        public static ConfigEntry<float> birdhouseSecPerBatch;
        public static ConfigEntry<int> birdhouseFeathersPerBatch;
        public static ConfigEntry<int> birdhouseConsumablePerBatch;
        public static ConfigEntry<int> birdhouseMaxFeathers;
        public static ConfigEntry<string> birdhouseConsumables;
        public static ConfigEntry<bool> birdhouseSoundEnabled;
        public static ConfigEntry<float> birdhouseSoundVolume;
        public static ConfigEntry<bool> birdhouseShowIcon;

        public static ConfigEntry<float> fishtrapSecPerFish;
        public static ConfigEntry<int> fishtrapMaxFish;
        public static ConfigEntry<float> fishtrapQualityChance;
        public static ConfigEntry<bool> fishtrapShowIcon;

        public static ConfigEntry<float> fishtrapLargeSecPerFish;
        public static ConfigEntry<int> fishtrapLargeMaxFish;
        public static ConfigEntry<float> fishtrapLargeQualityChance;
        public static ConfigEntry<bool> fishtrapLargeShowIcon;

        public static ConfigEntry<float> fishtrapOceanSecPerFish;
        public static ConfigEntry<int> fishtrapOceanMaxFish;
        public static ConfigEntry<float> fishtrapOceanQualityChance;
        public static ConfigEntry<bool> fishtrapOceanShowIcon;

        public static ConfigEntry<int> smelterMaxOre;
        public static ConfigEntry<int> smelterMaxFuel;
        public static ConfigEntry<int> smelterFuelPerProduct;
        public static ConfigEntry<float> smelterSecPerProduct;
        public static ConfigEntry<bool> smelterSmokeEnabled;
        public static ConfigEntry<bool> smelterPullFromGround;
        public static ConfigEntry<float> smelterPullRadius;

        public static ConfigEntry<int> kilnMaxOre;
        public static ConfigEntry<float> kilnSecPerProduct;
        public static ConfigEntry<bool> kilnSmokeEnabled;

        public static ConfigEntry<int> windmillMaxOre;
        public static ConfigEntry<float> windmillSecPerProduct;

        public static ConfigEntry<int> frostKilnMaxFuel;
        public static ConfigEntry<int> frostKilnFuelPerProduct;
        public static ConfigEntry<float> frostKilnSecPerProduct;
        public static ConfigEntry<bool> frostKilnSmokeEnabled;

        public static ConfigEntry<bool> fermenterEnabled;
        public static ConfigEntry<float> fermenterDuration;
        public static ConfigEntry<bool> fermenterShowIcon;

        public static ConfigEntry<float> cookingStationCookTimeMultiplier;
        public static ConfigEntry<bool> cookingStationCanOvercook;
        public static ConfigEntry<bool> cookingStationAutoPop;
        public static ConfigEntry<bool> cookingStationShowIcon;
        public static ConfigEntry<float> meadCauldronSoundVolume;
        public static ConfigEntry<float> meadCauldronDiscoverRange;
        public static ConfigEntry<float> meadCauldronBuildRange;

        public static ConfigEntry<int> wisplureContainerColumns;
        public static ConfigEntry<int> wisplureContainerRows;
        public static ConfigEntry<bool> wisplureShowIcon;
        public static ConfigEntry<bool> wisplureSoundEnabled;
        public static ConfigEntry<float> wisplureSoundVolume;

        public static ConfigEntry<bool> portalShowNames;
        public static ConfigEntry<bool> portalShowPins;
        public static ConfigEntry<bool> portalSoundEnabled;
        public static ConfigEntry<float> portalSoundVolume;
        public static ConfigEntry<float> portalNameDistance;
        public static ConfigEntry<bool> portalNamesNeedSight;
        public static ConfigEntry<KeyboardShortcut> portalNamesToggleKey;
        public static ConfigEntry<KeyboardShortcut> portalTagBothKey;
        public static ConfigEntry<bool> portalQuickTeleport;
        public static ConfigEntry<float> portalFadeTime;
        public static ConfigEntry<float> portalLoadSafetyDelay;

        public static void InitConfigs(ConfigFile config)
        {
            modEnabled = config.BindConfigInOrder("General Settings", "Enable Plugin", defaultValue: true, "Enable the CorewoodAdditions plugin.\n(Restart required. It is read before connecting to a server, so it is a local setting).\n", synced: false, sectionOrder: true, settingOrder: true);
            fishtrapShowPins = config.BindConfigInOrder("General Settings", "Show Fish Trap Map Pins", defaultValue: true, "Show a map pin for every fish trap you know about. Local setting.", synced: false, sectionOrder: true, settingOrder: true);
            serpentBaitEnabled = config.BindConfigInOrder("General Settings", "Enable Serpent Bait", defaultValue: true, "Allow the Compost Bin to make Serpent Bait from Rotten Meat, and the Ocean Fish Trap to catch Serpent Meat with it.", synced: true, sectionOrder: true, settingOrder: true);
            smelterKilnEnabled = config.BindConfigInOrder("General Settings", "Enable Smelter And Kiln", defaultValue: true, "Enable the HD Smelter and HD Charcoal Kiln. When off they are hidden from the build menu, and placed ones stop working and cannot be filled (they can still be emptied).", synced: true, sectionOrder: true, settingOrder: true);
            smelterKilnEnabled.SettingChanged += (_, _) => StationSettings.RefreshEnabled();
            windmillEnabled = config.BindConfigInOrder("General Settings", "Enable Windmill", defaultValue: true, "Enable the HD Windmill. When off it is hidden from the build menu, and placed ones stop grinding and cannot be filled (they can still be emptied).", synced: true, sectionOrder: true, settingOrder: true);
            windmillEnabled.SettingChanged += (_, _) => StationSettings.RefreshEnabled();
            frostKilnEnabled = config.BindConfigInOrder("General Settings", "Enable Frost Kiln", defaultValue: true, "Enable the HD Frost Kiln. When off it is hidden from the build menu, and placed ones stop working and cannot be filled (they can still be emptied).", synced: true, sectionOrder: true, settingOrder: true);
            frostKilnEnabled.SettingChanged += (_, _) => StationSettings.RefreshEnabled();
            fermenterEnabled = config.BindConfigInOrder("General Settings", "Enable Fermenter", defaultValue: true, "Enable the HD Fermenter. When off it is hidden from the build menu, and placed ones pause fermenting and cannot be filled (a finished brew can still be tapped).", synced: true, sectionOrder: true, settingOrder: true);
            fermenterEnabled.SettingChanged += (_, _) => FermenterSettings.RefreshAll();
            cookingStationEnabled = config.BindConfigInOrder("General Settings", "Enable Cooking Station", defaultValue: true, "Enable the HD Cooking Station. When off it is hidden from the build menu, and placed ones stop cooking and cannot be filled (cooked food can still be taken off).", synced: true, sectionOrder: true, settingOrder: true);
            cookingStationEnabled.SettingChanged += (_, _) => CookingStationSettings.RefreshEnabled();
            meadCauldronEnabled = config.BindConfigInOrder("General Settings", "Enable Mead Ketill", defaultValue: true, "Enable the HD Mead Ketill. When off it is hidden from the build menu, and placed ones cannot be used to craft. Vanilla mead ketills are not affected.", synced: true, sectionOrder: true, settingOrder: true);
            meadCauldronEnabled.SettingChanged += (_, _) => MeadCauldronSettings.RefreshEnabled();
            portalsEnabled = config.BindConfigInOrder("General Settings", "Enable Portals", defaultValue: true, "Enable the Corewood portal. When off it is hidden from the build menu, and placed ones go dark and stop teleporting (they keep their tag and link). Vanilla portals are not affected.", synced: true, sectionOrder: true, settingOrder: true);
            portalsEnabled.SettingChanged += (_, _) => PortalSettings.RefreshAll();
            portalAllowAllItems = config.BindConfigInOrder("General Settings", "Portal Allows All Items", defaultValue: false, "Let players take any item, including ore and metals, through the Corewood portal. Vanilla portals are not affected.", synced: true, sectionOrder: true, settingOrder: true);
            portalAllowAllItems.SettingChanged += (_, _) => PortalSettings.RefreshAll();
            wisplureCollectWisps = config.BindConfigInOrder("General Settings", "Wisp Fountain Collects Wisps", defaultValue: true, "Let the Corewood wisp fountain gather the wisps it lures into its container. When off, wisps hover around it to be picked by hand as in vanilla, and its container can still be emptied. Vanilla wisp fountains are not affected.", synced: true, sectionOrder: true, settingOrder: true);

            compostSecPerCycle = config.BindConfigInOrder("Compost Bin", "Seconds Per Cycle", defaultValue: 300f, "Seconds required to complete one production cycle.", synced: true, sectionOrder: true, settingOrder: true);
            compostConsumablePerCycle = config.BindConfigInOrder("Compost Bin", "Consumable Per Cycle", defaultValue: 1, "Amount of the consumable item consumed per production cycle.", synced: true, sectionOrder: true, settingOrder: true);
            compostMaxBait = config.BindConfigInOrder("Compost Bin", "Max Bait", defaultValue: 250, "Maximum bait (all types combined) stored inside the compost bin container.", synced: true, sectionOrder: true, settingOrder: true);

            compostConsumables = new ConfigEntry<string>[CompostBin.Baits.Length];
            for (int i = 0; i < CompostBin.Baits.Length; i++)
            {
                CompostBin.BaitDefinition bait = CompostBin.Baits[i];
                compostConsumables[i] = config.BindConfigInOrder("Compost Bin", bait.ConfigLabel + " Consumable", bait.DefaultConsumable, "Prefab name of the consumable item that produces " + bait.ConfigLabel + ". Accepts 1 item.", synced: true, sectionOrder: true, settingOrder: true);
            }
            compostShowIcon = config.BindConfigInOrder("Compost Bin", "Show Output Icon", defaultValue: true, "Show the rotating icon above the compost bin while it holds output. Local setting.", synced: false, sectionOrder: true, settingOrder: true);
            compostShowIcon.SettingChanged += (_, _) => CompostBin.RefreshAllItemCrafted();

            beehiveSecPerBatch = config.BindConfigInOrder("Beehive", "Seconds Per Cycle", defaultValue: 600f, "Seconds required to complete one production cycle.", synced: true, sectionOrder: true, settingOrder: true);
            beehiveHoneyPerBatch = config.BindConfigInOrder("Beehive", "Honey Per Cycle", defaultValue: 1, "Honey produced per production cycle.", synced: true, sectionOrder: true, settingOrder: true);
            beehiveConsumablePerBatch = config.BindConfigInOrder("Beehive", "Consumable Per Cycle", defaultValue: 1, "Amount of valid consumable items consumed per production cycle.", synced: true, sectionOrder: true, settingOrder: true);
            beehiveMaxHoney = config.BindConfigInOrder("Beehive", "Max Honey", defaultValue: 250, "Maximum honey stored inside the beehive container.", synced: true, sectionOrder: true, settingOrder: true);
            beehiveConsumables = config.BindConfigInOrder("Beehive", "Consumable Prefabs", defaultValue: "Dandelion", "Prefab names of valid consumable items for the beehive, separated by commas. Supports up to 10 items.", synced: true, sectionOrder: true, settingOrder: true);
            beehiveSoundEnabled = config.BindConfigInOrder("Beehive", "Enable Sound", defaultValue: true, "Play the buzzing sound while the beehive is working. Local setting.", synced: false, sectionOrder: true, settingOrder: true);
            beehiveSoundEnabled.SettingChanged += (_, _) => BeeHive.RefreshAllSfx();
            beehiveSoundVolume = config.BindConfigInOrder("Beehive", "Sound Volume", defaultValue: 1f, "Volume of the beehive's buzzing, from 0 (silent) to 1 (full). Follows the game's sound effects volume too. Local setting.", synced: false, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 1f));
            beehiveSoundVolume.SettingChanged += (_, _) => BeeHive.RefreshAllSfx();
            beehiveShowIcon = config.BindConfigInOrder("Beehive", "Show Output Icon", defaultValue: true, "Show the rotating icon above the beehive while it holds output. Local setting.", synced: false, sectionOrder: true, settingOrder: true);
            beehiveShowIcon.SettingChanged += (_, _) => BeeHive.RefreshAllItemCrafted();

            birdhouseSecPerBatch = config.BindConfigInOrder("Birdhouse", "Seconds Per Cycle", defaultValue: 300f, "Seconds required to complete one production cycle.", synced: true, sectionOrder: true, settingOrder: true);
            birdhouseFeathersPerBatch = config.BindConfigInOrder("Birdhouse", "Feathers Per Cycle", defaultValue: 5, "Feathers produced per production cycle.", synced: true, sectionOrder: true, settingOrder: true);
            birdhouseConsumablePerBatch = config.BindConfigInOrder("Birdhouse", "Consumable Per Cycle", defaultValue: 1, "Amount of valid consumable items consumed per production cycle.", synced: true, sectionOrder: true, settingOrder: true);
            birdhouseMaxFeathers = config.BindConfigInOrder("Birdhouse", "Max Feathers", defaultValue: 250, "Maximum feathers stored inside the birdhouse container.", synced: true, sectionOrder: true, settingOrder: true);
            birdhouseConsumables = config.BindConfigInOrder("Birdhouse", "Consumable Prefabs", defaultValue: "TurnipSeeds,CarrotSeeds,OnionSeeds", "Prefab names of valid consumable items for the birdhouse, separated by commas. Supports up to 10 items.", synced: true, sectionOrder: true, settingOrder: true);
            birdhouseSoundEnabled = config.BindConfigInOrder("Birdhouse", "Enable Sound", defaultValue: true, "Play the bird sound while the birdhouse is working. Local setting.", synced: false, sectionOrder: true, settingOrder: true);
            birdhouseSoundEnabled.SettingChanged += (_, _) => BirdHouse.RefreshAllSfx();
            birdhouseSoundVolume = config.BindConfigInOrder("Birdhouse", "Sound Volume", defaultValue: 1f, "Volume of the birdhouse's bird sound, from 0 (silent) to 1 (full). Follows the game's sound effects volume too. Local setting.", synced: false, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 1f));
            birdhouseSoundVolume.SettingChanged += (_, _) => BirdHouse.RefreshAllSfx();
            birdhouseShowIcon = config.BindConfigInOrder("Birdhouse", "Show Output Icon", defaultValue: true, "Show the rotating icon above the birdhouse while it holds output. Local setting.", synced: false, sectionOrder: true, settingOrder: true);
            birdhouseShowIcon.SettingChanged += (_, _) => BirdHouse.RefreshAllItemCrafted();

            fishtrapSecPerFish = config.BindConfigInOrder("Fish Trap", "Seconds Per Fish", defaultValue: 600f, "Seconds required to catch one fish.", synced: true, sectionOrder: true, settingOrder: true);
            fishtrapMaxFish = config.BindConfigInOrder("Fish Trap", "Max Fish", defaultValue: 100, "Maximum fish stored inside the trap container.", synced: true, sectionOrder: true, settingOrder: true);
            fishtrapQualityChance = config.BindConfigInOrder("Fish Trap", "Chance To Increase Quality", defaultValue: 0.2f, "Chance to increase fish quality by one step per roll. Range: 0.0 to 1.0.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 1f));
            fishtrapShowIcon = config.BindConfigInOrder("Fish Trap", "Show Output Icon", defaultValue: true, "Show the rotating icon above the fish trap while it holds output. Local setting.", synced: false, sectionOrder: true, settingOrder: true);
            fishtrapShowIcon.SettingChanged += (_, _) => FishTrap.RefreshAllItemCrafted();

            fishtrapLargeSecPerFish = config.BindConfigInOrder("Large Fish Trap", "Seconds Per Fish", defaultValue: 600f, "Seconds required to catch one fish.", synced: true, sectionOrder: true, settingOrder: true);
            fishtrapLargeMaxFish = config.BindConfigInOrder("Large Fish Trap", "Max Fish", defaultValue: 150, "Maximum fish stored inside the trap container.", synced: true, sectionOrder: true, settingOrder: true);
            fishtrapLargeQualityChance = config.BindConfigInOrder("Large Fish Trap", "Chance To Increase Quality", defaultValue: 0.2f, "Chance to increase fish quality by one step per roll. Range: 0.0 to 1.0.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 1f));
            fishtrapLargeShowIcon = config.BindConfigInOrder("Large Fish Trap", "Show Output Icon", defaultValue: true, "Show the rotating icon above the large fish trap while it holds output. Local setting.", synced: false, sectionOrder: true, settingOrder: true);
            fishtrapLargeShowIcon.SettingChanged += (_, _) => FishTrap.RefreshAllItemCrafted();

            fishtrapOceanSecPerFish = config.BindConfigInOrder("Ocean Fish Trap", "Seconds Per Fish", defaultValue: 600f, "Seconds required to catch one fish.", synced: true, sectionOrder: true, settingOrder: true);
            fishtrapOceanMaxFish = config.BindConfigInOrder("Ocean Fish Trap", "Max Fish", defaultValue: 200, "Maximum fish stored inside the trap container.", synced: true, sectionOrder: true, settingOrder: true);
            fishtrapOceanQualityChance = config.BindConfigInOrder("Ocean Fish Trap", "Chance To Increase Quality", defaultValue: 0.2f, "Chance to increase fish quality by one step per roll. Range: 0.0 to 1.0.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 1f));
            fishtrapOceanShowIcon = config.BindConfigInOrder("Ocean Fish Trap", "Show Output Icon", defaultValue: true, "Show the rotating icon above the ocean fish trap while it holds output. Local setting.", synced: false, sectionOrder: true, settingOrder: true);
            fishtrapOceanShowIcon.SettingChanged += (_, _) => FishTrap.RefreshAllItemCrafted();

            smelterMaxOre = config.BindConfigInOrder("HD Smelter", "Max Ore", defaultValue: 10, "Maximum ore the smelter can hold.", synced: true, sectionOrder: true, settingOrder: true);
            smelterMaxFuel = config.BindConfigInOrder("HD Smelter", "Max Fuel", defaultValue: 20, "Maximum coal the smelter can hold.", synced: true, sectionOrder: true, settingOrder: true);
            smelterFuelPerProduct = config.BindConfigInOrder("HD Smelter", "Fuel Per Product", defaultValue: 2, "Coal used for each bar produced.", synced: true, sectionOrder: true, settingOrder: true);
            smelterSecPerProduct = config.BindConfigInOrder("HD Smelter", "Seconds Per Product", defaultValue: 30f, "Seconds needed to produce one bar.", synced: true, sectionOrder: true, settingOrder: true);
            smelterSmokeEnabled = config.BindConfigInOrder("HD Smelter", "Enable Smoke", defaultValue: true, "Show the smoke rising from the smelter while it works. Visual only: a blocked chimney still stops it. Local setting.", synced: false, sectionOrder: true, settingOrder: true);
            smelterPullFromGround = config.BindConfigInOrder("HD Smelter", "Pull Items From Ground", defaultValue: true, "Let the smelter take coal and ore lying on the ground near its coal and ore inputs, one of each per second while it has room.", synced: true, sectionOrder: true, settingOrder: true);
            smelterPullRadius = config.BindConfigInOrder("HD Smelter", "Pull Radius", defaultValue: 4f, "How far from the smelter's coal and ore inputs, in metres, ground items are pulled in from.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0.5f, 10f));

            kilnMaxOre = config.BindConfigInOrder("HD Charcoal Kiln", "Max Wood", defaultValue: 25, "Maximum wood the kiln can hold.", synced: true, sectionOrder: true, settingOrder: true);
            kilnSecPerProduct = config.BindConfigInOrder("HD Charcoal Kiln", "Seconds Per Product", defaultValue: 15f, "Seconds needed to produce one coal.", synced: true, sectionOrder: true, settingOrder: true);
            kilnSmokeEnabled = config.BindConfigInOrder("HD Charcoal Kiln", "Enable Smoke", defaultValue: true, "Show the smoke rising from the kiln while it works. Visual only: a blocked chimney still stops it. Local setting.", synced: false, sectionOrder: true, settingOrder: true);

            windmillMaxOre = config.BindConfigInOrder("HD Windmill", "Max Barley", defaultValue: 50, "Maximum barley the windmill can hold.", synced: true, sectionOrder: true, settingOrder: true);
            windmillSecPerProduct = config.BindConfigInOrder("HD Windmill", "Seconds Per Product", defaultValue: 10f, "Seconds needed to grind one barley flour at full wind.", synced: true, sectionOrder: true, settingOrder: true);

            frostKilnMaxFuel = config.BindConfigInOrder("HD Frost Kiln", "Max Ice", defaultValue: 25, "Maximum ice the frost kiln can hold.", synced: true, sectionOrder: true, settingOrder: true);
            frostKilnFuelPerProduct = config.BindConfigInOrder("HD Frost Kiln", "Ice Per Product", defaultValue: 5, "Ice used for each Frozen Fuel produced.", synced: true, sectionOrder: true, settingOrder: true);
            frostKilnSecPerProduct = config.BindConfigInOrder("HD Frost Kiln", "Seconds Per Product", defaultValue: 30f, "Seconds needed to produce one Frozen Fuel.", synced: true, sectionOrder: true, settingOrder: true);
            frostKilnSmokeEnabled = config.BindConfigInOrder("HD Frost Kiln", "Enable Smoke", defaultValue: true, "Show the smoke rising from the frost kiln while it works. Visual only: a blocked chimney still stops it. Local setting.", synced: false, sectionOrder: true, settingOrder: true);

            smelterMaxOre.SettingChanged += (_, _) => StationSettings.RefreshAll();
            smelterMaxFuel.SettingChanged += (_, _) => StationSettings.RefreshAll();
            smelterFuelPerProduct.SettingChanged += (_, _) => StationSettings.RefreshAll();
            smelterSecPerProduct.SettingChanged += (_, _) => StationSettings.RefreshAll();
            kilnMaxOre.SettingChanged += (_, _) => StationSettings.RefreshAll();
            kilnSecPerProduct.SettingChanged += (_, _) => StationSettings.RefreshAll();
            smelterSmokeEnabled.SettingChanged += (_, _) => StationSettings.RefreshAll();
            kilnSmokeEnabled.SettingChanged += (_, _) => StationSettings.RefreshAll();
            windmillMaxOre.SettingChanged += (_, _) => StationSettings.RefreshAll();
            windmillSecPerProduct.SettingChanged += (_, _) => StationSettings.RefreshAll();
            frostKilnMaxFuel.SettingChanged += (_, _) => StationSettings.RefreshAll();
            frostKilnFuelPerProduct.SettingChanged += (_, _) => StationSettings.RefreshAll();
            frostKilnSecPerProduct.SettingChanged += (_, _) => StationSettings.RefreshAll();
            frostKilnSmokeEnabled.SettingChanged += (_, _) => StationSettings.RefreshAll();

            fermenterDuration = config.BindConfigInOrder("HD Fermenter", "Fermentation Duration", defaultValue: 2400f, "Seconds a brew takes to ferment (vanilla is 2400, 40 minutes). Applies to brews already fermenting.", synced: true, sectionOrder: true, settingOrder: true);
            fermenterDuration.SettingChanged += (_, _) => FermenterSettings.RefreshAll();
            fermenterShowIcon = config.BindConfigInOrder("HD Fermenter", "Show Output Icon", defaultValue: true, "Show the icon above the fermenter while it is ready to tap. Local setting.", synced: false, sectionOrder: true, settingOrder: true);
            fermenterShowIcon.SettingChanged += (_, _) => FermenterSettings.RefreshAll();

            cookingStationCookTimeMultiplier = config.BindConfigInOrder("HD Cooking Station", "Cook Time Multiplier", defaultValue: 1f, "Multiplies the vanilla cook time of every recipe (0.5 cooks twice as fast, 2 takes twice as long). Food burns once it has been on for twice its cook time. Applies to food already cooking, so lowering it can finish or burn that food straight away.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0.1f, 10f));
            cookingStationCookTimeMultiplier.SettingChanged += (_, _) => CookingStationSettings.RefreshAll();
            cookingStationCanOvercook = config.BindConfigInOrder("HD Cooking Station", "Can Overcook Items", defaultValue: true, "Let food burn into coal when it is left on the cooking station too long, as in vanilla. When off, cooked food stays cooked until it is taken off.", synced: true, sectionOrder: true, settingOrder: true);
            cookingStationCanOvercook.SettingChanged += (_, _) => CookingStationSettings.RefreshAll();
            cookingStationAutoPop = config.BindConfigInOrder("HD Cooking Station", "Auto Drop Cooked Food", defaultValue: false, "Drop food off the cooking station onto the ground as soon as it is cooked, so it can't burn. It does not raise your Cooking skill, as taking it by hand does. The output icon is not shown while this is on.", synced: true, sectionOrder: true, settingOrder: true);
            cookingStationAutoPop.SettingChanged += (_, _) => CookingStationSettings.RefreshAllItemCrafted();
            cookingStationShowIcon = config.BindConfigInOrder("HD Cooking Station", "Show Output Icon", defaultValue: true, "Show the icon above the cooking station while it holds cooked or burnt food to take off. Not shown while Auto Drop Cooked Food is on. Local setting.", synced: false, sectionOrder: true, settingOrder: true);
            cookingStationShowIcon.SettingChanged += (_, _) => CookingStationSettings.RefreshAllItemCrafted();

            meadCauldronSoundVolume = config.BindConfigInOrder("HD Mead Ketill", "Sound Volume", defaultValue: 1f, "Volume of the mead ketill's bubbling, from 0 (silent) to 1 (full). Follows the game's sound effects volume too. Local setting.", synced: false, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 1f));
            meadCauldronSoundVolume.SettingChanged += (_, _) => MeadCauldronSettings.RefreshAll();
            meadCauldronDiscoverRange = config.BindConfigInOrder("HD Mead Ketill", "Discover Range", defaultValue: 4f, "Metres you need to be from the mead ketill for its recipes to be discovered.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(1f, 50f));
            meadCauldronDiscoverRange.SettingChanged += (_, _) => MeadCauldronSettings.RefreshAll();
            meadCauldronBuildRange = config.BindConfigInOrder("HD Mead Ketill", "Build Range", defaultValue: 20f, "Radius in metres around the mead ketill in which pieces that need it as their crafting station can be built.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(1f, 100f));
            meadCauldronBuildRange.SettingChanged += (_, _) => MeadCauldronSettings.RefreshAll();

            wisplureContainerColumns = config.BindConfigInOrder("Wisp Fountain", "Container Columns", defaultValue: 5, "Width of the wisp fountain's storage. Empty it before lowering this, as items in removed columns can be lost.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<int>(1, WispCollector.MaxColumns));
            wisplureContainerColumns.SettingChanged += (_, _) => WispCollector.RefreshAllSizes();
            wisplureContainerRows = config.BindConfigInOrder("Wisp Fountain", "Container Rows", defaultValue: 2, "Height of the wisp fountain's storage.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<int>(1, WispCollector.MaxRows));
            wisplureContainerRows.SettingChanged += (_, _) => WispCollector.RefreshAllSizes();
            wisplureShowIcon = config.BindConfigInOrder("Wisp Fountain", "Show Output Icon", defaultValue: true, "Show the icon above the wisp fountain while it holds collected wisps. Local setting.", synced: false, sectionOrder: true, settingOrder: true);
            wisplureShowIcon.SettingChanged += (_, _) => WispCollector.RefreshAllVisuals();
            wisplureSoundEnabled = config.BindConfigInOrder("Wisp Fountain", "Enable Sound", defaultValue: true, "Play the wisp fountain's sound while it holds collected wisps. Local setting.", synced: false, sectionOrder: true, settingOrder: true);
            wisplureSoundEnabled.SettingChanged += (_, _) => WispCollector.RefreshAllVisuals();
            wisplureSoundVolume = config.BindConfigInOrder("Wisp Fountain", "Sound Volume", defaultValue: 1f, "Volume of the wisp fountain's sound, from 0 (silent) to 1 (full). Follows the game's sound effects volume too. Local setting.", synced: false, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 1f));
            wisplureSoundVolume.SettingChanged += (_, _) => WispCollector.RefreshAllVisuals();

            portalShowNames = config.BindConfigInOrder("Portal", "Show Portal Names", defaultValue: true, "Show each Corewood portal's tag as text above it. Vanilla portals are not affected. Local setting.", synced: false, sectionOrder: true, settingOrder: true);
            portalShowPins = config.BindConfigInOrder("Portal", "Show Portal Map Pins", defaultValue: true, "Show a map pin, labelled with the portal's tag, for every Corewood portal you know about. Vanilla portals are not affected. Local setting.", synced: false, sectionOrder: true, settingOrder: true);
            portalSoundEnabled = config.BindConfigInOrder("Portal", "Enable Portal Sound", defaultValue: true, "Play the hum of a connected Corewood portal. Local setting.", synced: false, sectionOrder: true, settingOrder: true);
            portalSoundEnabled.SettingChanged += (_, _) => PortalSettings.RefreshAll();
            portalSoundVolume = config.BindConfigInOrder("Portal", "Portal Sound Volume", defaultValue: 1f, "Volume of a connected Corewood portal's hum, from 0 (silent) to 1 (full). Follows the game's sound effects volume too. Local setting.", synced: false, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 1f));
            portalSoundVolume.SettingChanged += (_, _) => PortalSettings.RefreshAll();
            portalNameDistance = config.BindConfigInOrder("Portal", "Portal Name Distance", defaultValue: 30f, "Hide portal names farther than this many metres away. 0 = always show. Local setting.", synced: false, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 200f));
            portalNamesNeedSight = config.BindConfigInOrder("Portal", "Portal Names Need Line Of Sight", defaultValue: true, "Hide a portal's name when a wall or other solid object is between you and the portal. Local setting.", synced: false, sectionOrder: true, settingOrder: true);
            portalNamesToggleKey = config.BindConfigInOrder("Portal", "Portal Names Toggle Key", defaultValue: new KeyboardShortcut(KeyCode.PageUp), "Key to show or hide portal names in game. Set to None to disable the hotkey. Local setting.", synced: false, sectionOrder: true, settingOrder: true);
            portalTagBothKey = config.BindConfigInOrder("Portal", "Tag Both Portals Key", defaultValue: new KeyboardShortcut(KeyCode.LeftAlt), "Hold this key and press Use on a connected Corewood portal to rename it and the portal it links to. Set to None to turn this off. Local setting.", synced: false, sectionOrder: true, settingOrder: true);
            portalQuickTeleport = config.BindConfigInOrder("Portal", "Fast Teleport", defaultValue: true, "Speed up teleporting through a Corewood portal: a short fade, and you arrive as soon as the destination has loaded instead of after the vanilla 8 seconds. Vanilla portals are not affected. Has no effect while QuickTeleport is installed, as that already speeds up every portal.", synced: true, sectionOrder: true, settingOrder: true);
            portalFadeTime = config.BindConfigInOrder("Portal", "Fast Teleport Fade Time", defaultValue: 0.25f, "Seconds the screen takes to fade out and back in during a fast teleport. Local setting.", synced: false, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 1f));
            portalLoadSafetyDelay = config.BindConfigInOrder("Portal", "Fast Teleport Safety Delay", defaultValue: 0.5f, "Extra seconds to wait once the destination has loaded, so buildings there are in place before you arrive. Raise it if you arrive before a base has appeared. Local setting.", synced: false, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<float>(0f, 5f));

            // Decimal settings rounded (and kept in range) whichever editor sets them
            MagicMike.ModSettings.ConfigRounding.RoundAll(config);
        }
    }
}
