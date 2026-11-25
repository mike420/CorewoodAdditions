using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace CorewoodAdditions
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency("com.jotunn.jotunn", BepInDependency.DependencyFlags.HardDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Major)]
    internal class CorewoodAdditionsMod : BaseUnityPlugin
    {
        internal const string PluginName = "CorewoodAdditions";
        internal const string PluginVersion = "1.1.5";
        internal const string PluginAuthor = "MagicMike";
        internal const string PluginGUID = "MagicMike.CorewoodAdditions";
        internal const string PluginLink = "https://discord.gg/mnr36G6J4x";

        public static CorewoodAdditionsMod plugininstance;
        private readonly Harmony harmony = new Harmony(PluginGUID);

        public static AssetBundle cwadditions;
        public static string additionsBundle = "corewood_additions";

        internal static ConfigFile ConfigFile;
        internal static ConfigFileWatcher ConfigFileWatcher;

        public void Awake()
        {
            plugininstance = this;
            ConfigFile = base.Config;
            ConfigFileWatcher = new ConfigFileWatcher(base.Config, 1000L);
            base.Config.SaveOnConfigSet = false;
            MainConfig.InitConfigs(base.Config);
            MagicMike.ModSettings.ModSettings.Init(this);
            UpdatePlugin();

            if (MainConfig.modEnabled.Value)
            {
                Translations.Load(this);

                LoadAssetBundles();
                //AddFXAssets();

                PrefabManager.OnVanillaPrefabsAvailable += LoadCoreWoodAdditions;
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                ConfigFileWatcher.OnConfigFileReloaded += delegate
                {
                    UpdatePlugin(saveConfig: false);
                };
                SynchronizationManager.OnConfigurationWindowClosed += delegate
                {
                    UpdatePlugin();
                };
                SynchronizationManager.OnConfigurationSynchronized += delegate
                {
                    UpdatePlugin();
                };

                Game.isModded = true;
            }
        }

        public void Update()
        {
            Translations.Tick();
            PortalNameLabel.CheckToggleKey();
        }

        public void OnDestroy()
        {
            Translations.Unload();
        }

        public static void LoadAssetBundles()
        {
            cwadditions = AssetUtils.LoadAssetBundleFromResources(additionsBundle, Assembly.GetExecutingAssembly());
            if (cwadditions == null)
            {
                Jotunn.Logger.LogError("Failed to load asset bundle with name: " + additionsBundle);
            }
        }

        private void LoadCoreWoodAdditions()
        {
            try
            {
                AddPrefabs.LoadPieces();
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogError("Exception caught while loading CoreWood Additions: " + ex.Message);
            }
            finally
            {
                PrefabManager.OnVanillaPrefabsAvailable -= LoadCoreWoodAdditions;
            }
        }


        private void UpdatePlugin(bool saveConfig = true)
        {
            if (saveConfig)
            {
                base.Config.Save();
            }
        }

        internal static void TryAddPiece(Action addPiece)
        {
            try
            {
                addPiece();
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogError("Exception caught while adding " + addPiece.Method.Name + ": " + ex);
            }
        }
    }
}
