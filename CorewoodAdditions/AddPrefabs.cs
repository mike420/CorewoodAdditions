using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using CorewoodAdditions.Producers;
using UnityEngine;
using static CorewoodAdditions.PieceAssets;

namespace CorewoodAdditions
{
    internal class AddPrefabs
    {
        internal const string SmelterNewPrefabName = "mm_corewood_smelter_new";
        internal const string KilnPrefabName = "mm_corewood_kiln";
        internal const string FrostKilnPrefabName = "mm_corewood_frost_kiln";
        private const string VanillaFrostKilnPrefabName = "piece_FrostKiln";
        internal const string WindmillPrefabName = "mm_corewood_windmill";
        internal const string FermenterPrefabName = "mm_corewood_fermenter";
        internal const string StationPrefabName = "mm_corewood_cookingstation";
        internal const string CauldronPrefabName = "mm_corewood_mead_cauldron";
        private const string VanillaMeadCauldronPrefabName = "piece_MeadCauldron";
        internal const string WisplurePrefabName = "mm_corewood_wisplure";
        private const string VanillaWisplurePrefabName = "piece_wisplure";
        private const int WisplureMaxConcurrentSounds = 2;
        internal const string PortalPrefabName = "mm_corewood_portal";
        private const string VanillaPortalPrefabName = "portal_wood";

        public static void LoadPieces()
        {
            PieceAssets.Init();
            CorewoodAdditionsMod.TryAddPiece(AddSerpentBait);
            CorewoodAdditionsMod.TryAddPiece(AddCompostBin);
            CorewoodAdditionsMod.TryAddPiece(AddBeeHive);
            CorewoodAdditionsMod.TryAddPiece(AddFishTrap);
            CorewoodAdditionsMod.TryAddPiece(AddFishTrapLarge);
            CorewoodAdditionsMod.TryAddPiece(AddFishTrapOcean);
            CorewoodAdditionsMod.TryAddPiece(AddBirdHouse);

            CorewoodAdditionsMod.TryAddPiece(AddSmelterNew);
            CorewoodAdditionsMod.TryAddPiece(AddKilnNew);
            CorewoodAdditionsMod.TryAddPiece(AddWindmillNew);
            CorewoodAdditionsMod.TryAddPiece(AddFermenterNew);
            CorewoodAdditionsMod.TryAddPiece(AddCookingStationNew);
            CorewoodAdditionsMod.TryAddPiece(AddMeadCauldronNew);
            CorewoodAdditionsMod.TryAddPiece(AddWisplureNew);
            CorewoodAdditionsMod.TryAddPiece(AddFrostKilnNew);

            CorewoodAdditionsMod.TryAddPiece(AddPortalNew);

            CorewoodAdditionsMod.TryAddPiece(AddStoneWall);
        }

        public static void AddSerpentBait()
        {
            GameObject gameObject = CorewoodAdditionsMod.cwadditions.LoadAsset<GameObject>(FishTrap.SerpentBaitName);
            ItemManager.Instance.AddItem(new CustomItem(gameObject, fixReference: true));
        }

        public static void AddCompostBin()
        {
            GameObject gameObject = CorewoodAdditionsMod.cwadditions.LoadAsset<GameObject>(CompostBin.PrefabName);
            CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
            {
                CraftingStation = "piece_workbench",
                PieceTable = "_HammerPieceTable",
                Category = "CoreWood-Additions",
                Usage = new[] { PieceUsages.Building, PieceUsages.Crafting, PieceUsages.Storage },
                Requirements = new RequirementConfig[2]
                {
                    new RequirementConfig
                    {
                        Item = "RoundLog",
                        Amount = 4,
                        Recover = true
                    },
                    new RequirementConfig
                    {
                        Item = "Wood",
                        Amount = 1,
                        Recover = true
                    }
                }
            });
            gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
            WearNTear component = gameObject.GetComponent<WearNTear>();
            component.m_destroyedEffect = breakWood;
            component.m_hitEffect = hitWood;

            Container container = gameObject.GetComponentInChildren<Container>(true);
            if (container == null)
            {
                container = gameObject.AddComponent<Container>();
                container.m_name = "$piece_mm_corewood_compost_bin";
                container.m_width = 5;
                container.m_height = 2;
            }
            if (!gameObject.GetComponent<CompostBin>())
            {
                gameObject.AddComponent<CompostBin>();
            }
            PieceManager.Instance.AddPiece(customPiece);
        }

        public static void AddBeeHive()
        {
            GameObject gameObject = CorewoodAdditionsMod.cwadditions.LoadAsset<GameObject>(BeeHive.PrefabName);
            CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
            {
                CraftingStation = "piece_workbench",
                PieceTable = "_HammerPieceTable",
                Category = "CoreWood-Additions",
                Usage = new[] { PieceUsages.Building, PieceUsages.Crafting, PieceUsages.Storage },
                Requirements = new RequirementConfig[3]
                {
                    new RequirementConfig
                    {
                        Item = "RoundLog",
                        Amount = 4,
                        Recover = true
                    },
                    new RequirementConfig
                    {
                        Item = "Wood",
                        Amount = 4,
                        Recover = true
                    },
                    new RequirementConfig
                    {
                        Item = "QueenBee",
                        Amount = 1,
                        Recover = true
                    }
                }
            });
            gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
            WearNTear component = gameObject.GetComponent<WearNTear>();
            component.m_destroyedEffect = breakWood;
            component.m_hitEffect = hitWood;

            Container container = gameObject.GetComponentInChildren<Container>(true);
            if (container == null)
            {
                container = gameObject.AddComponent<Container>();
                container.m_name = "$piece_mm_corewood_beehive";
                container.m_width = 5;
                container.m_height = 2;
            }
            if (!gameObject.GetComponent<BeeHive>())
            {
                gameObject.AddComponent<BeeHive>();
            }
            ProducerVisuals.AssignSfxHash(gameObject, BeeHive.PrefabName + "_sfx");
            PieceManager.Instance.AddPiece(customPiece);
        }

        public static void AddFishTrap()
        {
            GameObject gameObject = CorewoodAdditionsMod.cwadditions.LoadAsset<GameObject>(FishTrap.PrefabName);
            CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
            {
                CraftingStation = "piece_workbench",
                PieceTable = "_HammerPieceTable",
                Category = "CoreWood-Additions",
                Usage = new[] { PieceUsages.Building, PieceUsages.Crafting, PieceUsages.Storage },
                Requirements = new RequirementConfig[2]
                {
                    new RequirementConfig
                    {
                        Item = "RoundLog",
                        Amount = 2,
                        Recover = true
                    },
                    new RequirementConfig
                    {
                        Item = "Wood",
                        Amount = 2,
                        Recover = true
                    }
                }
            });

            Piece piece = gameObject.GetComponent<Piece>();
            piece.m_placeEffect = buildWood;
            piece.m_waterPiece = false;
            piece.m_noInWater = false;
            piece.m_groundPiece = true;
            WearNTear component = gameObject.GetComponent<WearNTear>();
            component.m_destroyedEffect = breakWood;
            component.m_hitEffect = hitWood;

            Container container = gameObject.GetComponentInChildren<Container>(true);
            if (container == null)
            {
                container = gameObject.AddComponent<Container>();
                container.m_name = "$piece_mm_corewood_fishtrap";
                container.m_width = 5;
                container.m_height = 4;
            }
            if (!gameObject.GetComponent<FishTrap>())
            {
                gameObject.AddComponent<FishTrap>();
            }
            PieceManager.Instance.AddPiece(customPiece);
        }

        public static void AddFishTrapLarge()
        {
            GameObject gameObject = CorewoodAdditionsMod.cwadditions.LoadAsset<GameObject>(FishTrap.LargePrefabName);
            CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
            {
                CraftingStation = "piece_workbench",
                PieceTable = "_HammerPieceTable",
                Category = "CoreWood-Additions",
                Usage = new[] { PieceUsages.Building, PieceUsages.Crafting, PieceUsages.Storage },
                Requirements = new RequirementConfig[2]
                {
                    new RequirementConfig
                    {
                        Item = "RoundLog",
                        Amount = 4,
                        Recover = true
                    },
                    new RequirementConfig
                    {
                        Item = "LinenThread",
                        Amount = 2,
                        Recover = true
                    }
                }
            });

            Piece piece = gameObject.GetComponent<Piece>();
            piece.m_name = "$piece_mm_corewood_fishtrap_large";
            piece.m_description = "$piece_mm_corewood_fishtrap_large_desc";
            piece.m_placeEffect = buildWood;
            piece.m_waterPiece = false;
            piece.m_noInWater = false;
            piece.m_groundPiece = true;
            WearNTear component = gameObject.GetComponent<WearNTear>();
            component.m_destroyedEffect = breakWood;
            component.m_hitEffect = hitWood;

            Container container = gameObject.GetComponentInChildren<Container>(true);
            if (container == null)
            {
                container = gameObject.AddComponent<Container>();
                container.m_name = "$piece_mm_corewood_fishtrap_large";
                container.m_width = 5;
                container.m_height = 4;
            }
            if (!gameObject.GetComponent<FishTrap>())
            {
                gameObject.AddComponent<FishTrap>();
            }
            PieceManager.Instance.AddPiece(customPiece);
        }

        public static void AddFishTrapOcean()
        {
            GameObject gameObject = CorewoodAdditionsMod.cwadditions.LoadAsset<GameObject>(FishTrap.OceanPrefabName);
            CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
            {
                CraftingStation = "",
                PieceTable = "_HammerPieceTable",
                Category = "CoreWood-Additions",
                Usage = new[] { PieceUsages.Building, PieceUsages.Crafting, PieceUsages.Storage },
                Requirements = new RequirementConfig[2]
                {
                    new RequirementConfig
                    {
                        Item = "AskBladder",
                        Amount = 4,
                        Recover = true
                    },
                    new RequirementConfig
                    {
                        Item = "LinenThread",
                        Amount = 4,
                        Recover = true
                    }
                }
            });

            Piece piece = gameObject.GetComponent<Piece>();
            piece.m_placeEffect = buildWood;
            piece.m_waterPiece = true;
            piece.m_noInWater = false;
            piece.m_groundPiece = false;
            piece.m_groundOnly = false;
            WearNTear component = gameObject.GetComponent<WearNTear>();
            component.m_destroyedEffect = breakWood;
            component.m_hitEffect = hitWood;
            component.m_noSupportWear = false;

            Floating floating = gameObject.GetComponent<Floating>();
            if (floating != null)
            {
                Object.DestroyImmediate(floating, true);
            }
            if (!gameObject.GetComponent<WaveBobber>())
            {
                gameObject.AddComponent<WaveBobber>();
            }
            if (!gameObject.GetComponent<FishTrap>())
            {
                gameObject.AddComponent<FishTrap>();
            }
            PieceManager.Instance.AddPiece(customPiece);
        }

        public static void AddBirdHouse()
        {
            GameObject gameObject = CorewoodAdditionsMod.cwadditions.LoadAsset<GameObject>(BirdHouse.PrefabName);
            CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
            {
                CraftingStation = "piece_workbench",
                PieceTable = "_HammerPieceTable",
                Category = "CoreWood-Additions",
                Usage = new[] { PieceUsages.Building, PieceUsages.Crafting, PieceUsages.Storage },
                Requirements = new RequirementConfig[2]
                {
                    new RequirementConfig
                    {
                        Item = "RoundLog",
                        Amount = 2,
                        Recover = true
                    },
                    new RequirementConfig
                    {
                        Item = "Wood",
                        Amount = 4,
                        Recover = true
                    }
                }
            });
            gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
            WearNTear component = gameObject.GetComponent<WearNTear>();
            component.m_destroyedEffect = breakWood;
            component.m_hitEffect = hitWood;

            Container container = gameObject.GetComponentInChildren<Container>(true);
            if (container == null)
            {
                container = gameObject.AddComponent<Container>();
                container.m_name = "$piece_mm_corewood_birdhouse";
                container.m_width = 5;
                container.m_height = 4;
            }
            if (!gameObject.GetComponent<BirdHouse>())
            {
                gameObject.AddComponent<BirdHouse>();
            }
            ProducerVisuals.AssignSfxHash(gameObject, BirdHouse.PrefabName + "_sfx");
            PieceManager.Instance.AddPiece(customPiece);
        }


        public static void AddSmelterNew()
        {
            GameObject gameObject = CorewoodAdditionsMod.cwadditions.LoadAsset<GameObject>(SmelterNewPrefabName);
            CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
            {
                CraftingStation = "piece_workbench",
                PieceTable = "_HammerPieceTable",
                Category = "CoreWood-Additions",
                Usage = new[] { PieceUsages.Building, PieceUsages.Crafting },
                Requirements = new RequirementConfig[2]
                {
                    new RequirementConfig
                    {
                        Item = "Stone",
                        Amount = 20,
                        Recover = true
                    },
                    new RequirementConfig
                    {
                        Item = "SurtlingCore",
                        Amount = 1,
                        Recover = true
                    }
                }
            });
            gameObject.GetComponent<Piece>().m_placeEffect = buildStone;
            WearNTear component = gameObject.GetComponent<WearNTear>();
            component.m_destroyedEffect = breakStone;
            component.m_hitEffect = hitStone;
            Smelter component2 = gameObject.GetComponent<Smelter>();
            component2.m_fuelItem = fuelCoal;
            component2.m_oreAddedEffects = addOreEffects;
            component2.m_fuelAddedEffects = addFuelEffects;
            component2.m_produceEffects = produceEffects;
            StationSettings.Apply(component2, StationSettings.StationKind.Smelter);
            StationSettings.ApplyEnabled(gameObject);
            FixGuidePoints(gameObject, Smelters.Smelter);
            PieceManager.Instance.AddPiece(customPiece);
            AddVanillaConversions(SmelterNewPrefabName, Smelters.Smelter);
        }

        public static void AddKilnNew()
        {
            GameObject gameObject = CorewoodAdditionsMod.cwadditions.LoadAsset<GameObject>(KilnPrefabName);
            CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
            {
                CraftingStation = "piece_workbench",
                PieceTable = "_HammerPieceTable",
                Category = "CoreWood-Additions",
                Usage = new[] { PieceUsages.Building, PieceUsages.Crafting },
                Requirements = new RequirementConfig[2]
                {
                    new RequirementConfig
                    {
                        Item = "Stone",
                        Amount = 20,
                        Recover = true
                    },
                    new RequirementConfig
                    {
                        Item = "SurtlingCore",
                        Amount = 1,
                        Recover = true
                    }
                }
            });
            gameObject.GetComponent<Piece>().m_placeEffect = buildStone;
            WearNTear component = gameObject.GetComponent<WearNTear>();
            component.m_destroyedEffect = breakStone;
            component.m_hitEffect = hitStone;            
            Smelter component2 = gameObject.GetComponent<Smelter>();
            component2.m_oreAddedEffects = kilnAddOreEffects;
            component2.m_produceEffects = kilnProduceEffects;
            StationSettings.Apply(component2, StationSettings.StationKind.Kiln);
            StationSettings.ApplyEnabled(gameObject);
            FixGuidePoints(gameObject, Smelters.CharcoalKiln);
            PieceManager.Instance.AddPiece(customPiece);
            AddVanillaConversions(KilnPrefabName, Smelters.CharcoalKiln);
        }

        public static void AddWindmillNew()
        {
            GameObject gameObject = CorewoodAdditionsMod.cwadditions.LoadAsset<GameObject>(WindmillPrefabName);
            CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
            {
                CraftingStation = "piece_artisanstation",
                PieceTable = "_HammerPieceTable",
                Category = "CoreWood-Additions",
                Usage = new[] { PieceUsages.Building, PieceUsages.Crafting },
                Requirements = new RequirementConfig[3]
                {
                    new RequirementConfig
                    {
                        Item = "Stone",
                        Amount = 20,
                        Recover = true
                    },
                    new RequirementConfig
                    {
                        Item = "Wood",
                        Amount = 30,
                        Recover = true
                    },
                    new RequirementConfig
                    {
                        Item = "IronNails",
                        Amount = 30,
                        Recover = true
                    }
                }
            });
            gameObject.GetComponent<Piece>().m_placeEffect = buildStone;
            WearNTear component = gameObject.GetComponent<WearNTear>();
            component.m_destroyedEffect = breakStone;
            component.m_hitEffect = hitStone;
            // Like the vanilla windmill it burns no fuel (m_maxFuel is 0) and is powered by the wind instead.
            Smelter component2 = gameObject.GetComponent<Smelter>();
            component2.m_oreAddedEffects = windmillAddOreEffects;
            component2.m_produceEffects = windmillProduceEffects;
            StationSettings.Apply(component2, StationSettings.StationKind.Windmill);
            StationSettings.ApplyEnabled(gameObject);
            FixGuidePoints(gameObject, Smelters.Windmill);
            UseVanillaMixerGroup(gameObject, Smelters.Windmill);
            PieceManager.Instance.AddPiece(customPiece);
            AddVanillaConversions(WindmillPrefabName, Smelters.Windmill);
        }

        public static void AddFermenterNew()
        {
            GameObject gameObject = CorewoodAdditionsMod.cwadditions.LoadAsset<GameObject>(FermenterPrefabName);
            CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
            {
                CraftingStation = "forge",
                PieceTable = "_HammerPieceTable",
                Category = "CoreWood-Additions",
                Usage = new[] { PieceUsages.Building, PieceUsages.Crafting },
                Requirements = new RequirementConfig[3]
                {
                    new RequirementConfig
                    {
                        Item = "FineWood",
                        Amount = 30,
                        Recover = true
                    },
                    new RequirementConfig
                    {
                        Item = "Bronze",
                        Amount = 5,
                        Recover = true
                    },
                    new RequirementConfig
                    {
                        Item = "Resin",
                        Amount = 10,
                        Recover = true
                    }
                }
            });
            gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
            WearNTear component = gameObject.GetComponent<WearNTear>();
            component.m_destroyedEffect = breakWood;
            component.m_hitEffect = hitWood;

            Fermenter component2 = gameObject.GetComponent<Fermenter>();
            component2.m_addedEffects = fermenterAddEffects;
            component2.m_tapEffects = fermenterTapEffects;
            component2.m_spawnEffects = fermenterSpawnEffects;
            UseVanillaMixerGroup(gameObject, Fermenters.Fermenter);
            FermenterSettings.ApplyEnabled(gameObject);
            FermenterSettings.ApplyDuration(component2);
            PieceManager.Instance.AddPiece(customPiece);
            AddVanillaFermenterConversions(FermenterPrefabName, Fermenters.Fermenter);
        }

        public static void AddCookingStationNew()
        {
            GameObject gameObject = CorewoodAdditionsMod.cwadditions.LoadAsset<GameObject>(StationPrefabName);
            CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
            {
                CraftingStation = "",
                PieceTable = "_HammerPieceTable",
                Category = "CoreWood-Additions",
                Usage = new[] { PieceUsages.Crafting },
                Requirements = new RequirementConfig[1]
                {
                    new RequirementConfig
                    {
                        Item = "RoundLog",
                        Amount = 1,
                        Recover = true
                    }
                }
            });
            gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
            WearNTear component = gameObject.GetComponent<WearNTear>();
            component.m_destroyedEffect = breakWood;
            component.m_hitEffect = hitWood;
            CookingStation component2 = gameObject.GetComponent<CookingStation>();
            // Any conversion baked into the prefab with a null m_from/m_to makes vanilla
            // ObjectDB.GetAllCreatableFood throw. All real conversions are copied from vanilla below.
            component2.m_conversion.Clear();
            component2.m_overCookedItem = fuelCoal;
            CookingStationSettings.ApplyCanOvercook(component2);
            component2.m_addEffect = cookingEffects;
            component2.m_doneEffect = cookingEffects;
            component2.m_overcookedEffect = cookingEffects;
            component2.m_pickEffector = cookingPickEffects;
            CookingStationSettings.ApplyEnabled(gameObject);
            PieceManager.Instance.AddPiece(customPiece);
            AddVanillaCookingConversions(StationPrefabName, CookingStations.CookingStation);
        }

        public static void AddMeadCauldronNew()
        {
            GameObject gameObject = CorewoodAdditionsMod.cwadditions.LoadAsset<GameObject>(CauldronPrefabName);
            CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
            {
                CraftingStation = "forge",
                PieceTable = "_HammerPieceTable",
                Category = "CoreWood-Additions",
                Usage = new[] { PieceUsages.Crafting },
                Requirements = new RequirementConfig[3]
                {
                    new RequirementConfig
                    {
                        Item = "Tin",
                        Amount = 4,
                        Recover = true
                    },
                    new RequirementConfig
                    {
                        Item = "Copper",
                        Amount = 6,
                        Recover = true
                    },
                    new RequirementConfig
                    {
                        Item = "Iron",
                        Amount = 1,
                        Recover = true
                    }
                }
            });
            gameObject.GetComponent<Piece>().m_placeEffect = buildMetal;
            WearNTear component = gameObject.GetComponent<WearNTear>();
            component.m_destroyedEffect = breakMetal;
            component.m_hitEffect = hitMetal;

            // The station keeps the vanilla Mead Ketill's m_name ($piece_meadcauldron), so every vanilla mead recipe
            // can be crafted at it without registering any recipes of its own.
            CraftingStation component2 = gameObject.GetComponent<CraftingStation>();
            component2.m_craftItemEffects = cauldrenCraftEffects;
            component2.m_craftItemDoneEffects = cauldrenDoneEffects;
            UseVanillaMixerGroup(gameObject, VanillaMeadCauldronPrefabName);
            MeadCauldronSettings.ApplyEnabled(gameObject);
            MeadCauldronSettings.Apply(component2);
            ProducerVisuals.AssignSfxHash(gameObject, CauldronPrefabName + "_sfx");
            PieceManager.Instance.AddPiece(customPiece);
        }

        public static void AddWisplureNew()
        {
            GameObject gameObject = CorewoodAdditionsMod.cwadditions.LoadAsset<GameObject>(WisplurePrefabName);
            CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
            {
                CraftingStation = "piece_stonecutter",
                PieceTable = "_HammerPieceTable",
                Category = "CoreWood-Additions",
                Usage = new[] { PieceUsages.Building, PieceUsages.Crafting, PieceUsages.Storage },
                Requirements = new RequirementConfig[2]
                {
                    new RequirementConfig
                    {
                        Item = "Stone",
                        Amount = 10,
                        Recover = true
                    },
                    new RequirementConfig
                    {
                        Item = "YagluthDrop",
                        Amount = 1,
                        Recover = true
                    }
                }
            });
            gameObject.GetComponent<Piece>().m_placeEffect = buildStone;
            WearNTear component = gameObject.GetComponent<WearNTear>();
            component.m_destroyedEffect = breakStone;
            component.m_hitEffect = hitStone;
            WispSpawner spawner = gameObject.GetComponent<WispSpawner>();
            if (spawner.m_wispPrefab == null)
            {
                spawner.m_wispPrefab = PrefabManager.Instance.GetPrefab(VanillaWisplurePrefabName)?.GetComponent<WispSpawner>()?.m_wispPrefab;
                if (spawner.m_wispPrefab == null)
                {
                    Jotunn.Logger.LogWarning("[CoreWoodAdditions] Could not find the wisp prefab on " + VanillaWisplurePrefabName + "; " + WisplurePrefabName + " spawns no wisps.");
                }
            }
            FixGuidePoints(gameObject, VanillaWisplurePrefabName);

            Container container = gameObject.GetComponentInChildren<Container>(true);
            if (container == null)
            {
                container = gameObject.AddComponent<Container>();
                container.m_name = "$piece_wisplure";
                container.m_width = WispCollector.Columns;
                container.m_height = WispCollector.Rows;
            }
            if (!gameObject.GetComponent<WispCollector>())
            {
                gameObject.AddComponent<WispCollector>();
            }

            ProducerVisuals.FindChildRecursive(gameObject.transform, WispCollector.SfxChildName)?.SetActive(false);
            ProducerVisuals.AssignSfxHash(gameObject, WisplurePrefabName + "_sfx");

            foreach (ZSFX zsfx in gameObject.GetComponentsInChildren<ZSFX>(includeInactive: true))
            {
                zsfx.m_maxConcurrentSources = WisplureMaxConcurrentSounds;
            }
            PieceManager.Instance.AddPiece(customPiece);
        }

        public static void AddFrostKilnNew()
        {
            GameObject gameObject = CorewoodAdditionsMod.cwadditions.LoadAsset<GameObject>(FrostKilnPrefabName);
            CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
            {
                CraftingStation = "piece_stonecutter",
                PieceTable = "_HammerPieceTable",
                Category = "CoreWood-Additions",
                Usage = new[] { PieceUsages.Building, PieceUsages.Crafting },
                Requirements = new RequirementConfig[3]
                {
                    new RequirementConfig
                    {
                        Item = "Stone",
                        Amount = 20,
                        Recover = true
                    },
                    new RequirementConfig
                    {
                        Item = "FrostCore",
                        Amount = 10,
                        Recover = true
                    },
                    new RequirementConfig
                    {
                        Item = "Ice",
                        Amount = 5,
                        Recover = true
                    }
                }
            });
            gameObject.GetComponent<Piece>().m_placeEffect = buildStone;
            WearNTear component = gameObject.GetComponent<WearNTear>();
            component.m_destroyedEffect = breakStone;
            component.m_hitEffect = hitStone;
            Smelter component2 = gameObject.GetComponent<Smelter>();
            component2.m_oreAddedEffects = frostKilnAddOreEffects;
            component2.m_fuelAddedEffects = frostKilnAddOreEffects;
            component2.m_produceEffects = frostKilnProduceEffects;
            CopyVanillaFuelOnlyRecipe(component2, VanillaFrostKilnPrefabName);
            StationSettings.Apply(component2, StationSettings.StationKind.FrostKiln);
            StationSettings.ApplyEnabled(gameObject);
            FixGuidePoints(gameObject, VanillaFrostKilnPrefabName);
            UseVanillaMixerGroup(gameObject, VanillaFrostKilnPrefabName);
            PieceManager.Instance.AddPiece(customPiece);
        }


        public static void AddPortalNew()
        {
            GameObject gameObject = CorewoodAdditionsMod.cwadditions.LoadAsset<GameObject>(PortalPrefabName);
            CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
            {
                CraftingStation = "piece_workbench",
                PieceTable = "_HammerPieceTable",
                Category = "CoreWood-Additions",
                Usage = new[] { PieceUsages.Building },
                Requirements = new RequirementConfig[3]
                {
                    new RequirementConfig
                    {
                        Item = "GreydwarfEye",
                        Amount = 10,
                        Recover = true
                    },
                    new RequirementConfig
                    {
                        Item = "FineWood",
                        Amount = 20,
                        Recover = true
                    },
                    new RequirementConfig
                    {
                        Item = "SurtlingCore",
                        Amount = 1,
                        Recover = true
                    }
                }
            });
            gameObject.GetComponent<Piece>().m_placeEffect = buildWood;
            WearNTear component = gameObject.GetComponent<WearNTear>();
            component.m_destroyedEffect = breakWood;
            component.m_hitEffect = hitWood;
            TeleportWorld component2 = gameObject.GetComponent<TeleportWorld>();
            component2.m_connected = portalConnected;
            FixGuidePoints(gameObject, VanillaPortalPrefabName);
            UseVanillaMixerGroup(gameObject, VanillaPortalPrefabName);
            PortalSettings.ApplyEnabled(gameObject);
            PortalSettings.ApplySound(gameObject);
            PortalSettings.ApplyAllowAllItems(gameObject);
            if (!gameObject.GetComponent<PortalNameLabel>())
            {
                gameObject.AddComponent<PortalNameLabel>();
            }
            PieceManager.Instance.AddPiece(customPiece);
        }


        public static void AddStoneWall()
        {
            GameObject gameObject = CorewoodAdditionsMod.cwadditions.LoadAsset<GameObject>("mm_corewood_stone_wall");
            CustomPiece customPiece = new CustomPiece(gameObject, fixReference: true, new PieceConfig
            {
                CraftingStation = "piece_workbench",
                PieceTable = "_HammerPieceTable",
                Category = "CoreWood-Additions",
                Usage = new[] { PieceUsages.Building },
                Requirements = new RequirementConfig[1]
                {
                    new RequirementConfig
                    {
                        Item = "Stone",
                        Amount = 4,
                        Recover = true
                    }
                }
            });
            gameObject.GetComponent<Piece>().m_placeEffect = buildStone;
            WearNTear component = gameObject.GetComponent<WearNTear>();
            component.m_destroyedEffect = breakStone;
            component.m_hitEffect = hitStone;
            PieceManager.Instance.AddPiece(customPiece);
        }


        internal static void RegisterPortalPrefab(Game game)
        {
            int hash = PortalPrefabName.GetStableHashCode();
            if (!game.PortalPrefabHash.Contains(hash))
            {
                game.PortalPrefabHash.Add(hash);
            }
        }

        private static void UseVanillaMixerGroup(GameObject prefab, string vanillaPrefabName)
        {
            UnityEngine.Audio.AudioMixerGroup group = null;
            GameObject vanilla = PrefabManager.Instance.GetPrefab(vanillaPrefabName);
            if (vanilla != null)
            {
                foreach (AudioSource source in vanilla.GetComponentsInChildren<AudioSource>(true))
                {
                    if (source.outputAudioMixerGroup != null)
                    {
                        group = source.outputAudioMixerGroup;
                        break;
                    }
                }
            }
            if (group == null)
            {
                Jotunn.Logger.LogWarning("[CoreWoodAdditions] No audio mixer group found on " + vanillaPrefabName + "; " + prefab.name + " audio ignores the SFX volume.");
                return;
            }
            foreach (AudioSource source in prefab.GetComponentsInChildren<AudioSource>(true))
            {
                source.outputAudioMixerGroup = group;
            }
        }

        private static void FixGuidePoints(GameObject prefab, string vanillaPrefabName)
        {
            GuidePoint vanilla = PrefabManager.Instance.GetPrefab(vanillaPrefabName)?.GetComponentInChildren<GuidePoint>(true);
            foreach (GuidePoint guide in prefab.GetComponentsInChildren<GuidePoint>(true))
            {
                if (guide.m_ravenPrefab == null && vanilla != null)
                {
                    guide.m_ravenPrefab = vanilla.m_ravenPrefab;
                }
                if (guide.m_ravenPrefab == null)
                {
                    guide.enabled = false;
                }
            }
        }

        private static void AddVanillaConversions(string station, string vanillaPrefabName)
        {
            Smelter vanilla = PrefabManager.Instance.GetPrefab(vanillaPrefabName)?.GetComponent<Smelter>();
            if (vanilla == null)
            {
                Jotunn.Logger.LogWarning("[CoreWoodAdditions] Could not find " + vanillaPrefabName + "; " + station + " has no conversions.");
                return;
            }
            foreach (Smelter.ItemConversion conversion in vanilla.m_conversion)
            {
                if (conversion?.m_from == null || conversion.m_to == null)
                {
                    continue;
                }
                ItemManager.Instance.AddItemConversion(new CustomItemConversion(new SmelterConversionConfig
                {
                    Station = station,
                    FromItem = conversion.m_from.name,
                    ToItem = conversion.m_to.name
                }));
            }
        }

        // The vanilla frost kiln burns Ice as fuel and makes Frozen Fuel from nothing: its one conversion has no input,
        // which Jotunn's SmelterConversionConfig can't express, so the fuel item and conversion are copied directly.
        private static void CopyVanillaFuelOnlyRecipe(Smelter smelter, string vanillaPrefabName)
        {
            Smelter vanilla = PrefabManager.Instance.GetPrefab(vanillaPrefabName)?.GetComponent<Smelter>();
            if (vanilla == null || vanilla.m_fuelItem == null)
            {
                Jotunn.Logger.LogWarning("[CoreWoodAdditions] Could not find " + vanillaPrefabName + "; " + Utils.GetPrefabName(smelter.gameObject) + " has no fuel or recipe.");
                return;
            }
            smelter.m_fuelItem = vanilla.m_fuelItem;
            smelter.m_conversion = new System.Collections.Generic.List<Smelter.ItemConversion>();
            foreach (Smelter.ItemConversion conversion in vanilla.m_conversion)
            {
                if (conversion?.m_to != null)
                {
                    smelter.m_conversion.Add(new Smelter.ItemConversion { m_from = conversion.m_from, m_to = conversion.m_to });
                }
            }
        }

        private static void AddVanillaFermenterConversions(string station, string vanillaPrefabName)
        {
            Fermenter vanilla = PrefabManager.Instance.GetPrefab(vanillaPrefabName)?.GetComponent<Fermenter>();
            if (vanilla == null)
            {
                Jotunn.Logger.LogWarning("[CoreWoodAdditions] Could not find " + vanillaPrefabName + "; " + station + " has no conversions.");
                return;
            }
            foreach (Fermenter.ItemConversion conversion in vanilla.m_conversion)
            {
                if (conversion?.m_from == null || conversion.m_to == null)
                {
                    continue;
                }
                ItemManager.Instance.AddItemConversion(new CustomItemConversion(new FermenterConversionConfig
                {
                    Station = station,
                    FromItem = conversion.m_from.name,
                    ToItem = conversion.m_to.name,
                    ProducedItems = conversion.m_producedItems
                }));
            }
        }

        private static void AddVanillaCookingConversions(string station, string vanillaPrefabName)
        {
            CookingStation vanilla = PrefabManager.Instance.GetPrefab(vanillaPrefabName)?.GetComponent<CookingStation>();
            if (vanilla == null)
            {
                Jotunn.Logger.LogWarning("[CoreWoodAdditions] Could not find " + vanillaPrefabName + "; " + station + " has no conversions.");
                return;
            }
            foreach (CookingStation.ItemConversion conversion in vanilla.m_conversion)
            {
                if (conversion?.m_from == null || conversion.m_to == null)
                {
                    continue;
                }
                ItemManager.Instance.AddItemConversion(new CustomItemConversion(new CookingConversionConfig
                {
                    Station = station,
                    FromItem = conversion.m_from.name,
                    ToItem = conversion.m_to.name,
                    CookTime = CookingStationSettings.GetCookTime(conversion.m_from.name, conversion.m_cookTime)
                }));
            }
        }
    }
}
