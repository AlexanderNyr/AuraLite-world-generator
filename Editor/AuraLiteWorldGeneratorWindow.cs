#if UNITY_EDITOR
using System;
using AuraLiteWorldGenerator.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static AuraLiteWorldGenerator.Runtime.WorldGeneratorConstants;

namespace AuraLiteWorldGenerator.Editor
{
    /// <summary>
    /// Main editor window for AuraLite Rural World Generator (URP).
    /// Generates large procedural rural scenes with villages, forests, fields, roads, and water.
    /// </summary>
    public class AuraLiteWorldGeneratorWindow : EditorWindow
    {
        private GenerationSettings _settings = new GenerationSettings();
        private bool _showAdvanced;
        private Vector2 _scroll;

        [MenuItem(MenuPath + "Build AAA Rural World (URP)")]
        public static void OpenWindow()
        {
            GetWindow<AuraLiteWorldGeneratorWindow>(WindowTitle);
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("AAA Rural World Builder (URP)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Generates a large rural scene in meters: a village along streets, normal roads, a distant forest, " +
                "and the rest of the territory as fields/meadows. Map scale is set in square kilometers.",
                MessageType.Info);

            DrawBasicSettings();
            DrawEnvironmentSettings();
            DrawOutputSettings();
            DrawComputedInfo();
            DrawBuildButton();

            EditorGUILayout.EndScrollView();
        }

        private void DrawBasicSettings()
        {
            EditorGUILayout.LabelField("Basic Settings", EditorStyles.boldLabel);
            _settings.sceneName = EditorGUILayout.TextField("Scene Name", _settings.sceneName);
            _settings.seed = EditorGUILayout.IntField("Seed", _settings.seed);
            _settings.createNewScene = EditorGUILayout.Toggle("Create New Scene", _settings.createNewScene);
            _settings.saveSceneAsset = EditorGUILayout.Toggle("Save Scene Asset", _settings.saveSceneAsset);

            EditorGUILayout.Space(4);
            _settings.mapAreaKm2 = EditorGUILayout.Slider("Map Area (km^2)", _settings.mapAreaKm2, MinMapAreaKm2, MaxMapAreaKm2);
            _settings.terrainHeightMeters = EditorGUILayout.Slider("Terrain Height Range (m)", _settings.terrainHeightMeters, MinTerrainHeight, MaxTerrainHeight);
            _settings.villageLengthMeters = EditorGUILayout.Slider("Village Length (m)", _settings.villageLengthMeters, MinVillageLength, MaxVillageLength);
            _settings.mainStreetWidthMeters = EditorGUILayout.Slider("Main Street Width (m)", _settings.mainStreetWidthMeters, MinMainStreetWidth, MaxMainStreetWidth);
            _settings.laneWidthMeters = EditorGUILayout.Slider("Village Lane Width (m)", _settings.laneWidthMeters, MinLaneWidth, MaxLaneWidth);
            _settings.houseDensity = EditorGUILayout.Slider("House Density", _settings.houseDensity, MinHouseDensity, MaxHouseDensity);
            _settings.wheatRatio = EditorGUILayout.Slider("Wheat Field Ratio", _settings.wheatRatio, MinWheatRatio, MaxWheatRatio);
            _settings.qualityBoost = EditorGUILayout.Slider("Quality Boost", _settings.qualityBoost, MinQualityBoost, MaxQualityBoost);
            _settings.villageStyle = (VillageStyle)EditorGUILayout.EnumPopup("Village Style", _settings.villageStyle);
        }

        private void DrawEnvironmentSettings()
        {
            EditorGUILayout.Space(4);
            _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "Environment & Rendering");
            if (_showAdvanced)
            {
                EditorGUI.indentLevel++;
                _settings.fogStartKm = EditorGUILayout.Slider("Fog Start (km)", _settings.fogStartKm, MinFogStartKm, MaxFogStartKm);
                _settings.fogEndKm = EditorGUILayout.Slider("Fog End (km)", _settings.fogEndKm, MinFogEndKm, MaxFogEndKm);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawOutputSettings()
        {
            EditorGUILayout.Space(4);
            _settings.outputRoot = EditorGUILayout.TextField("Output Root", _settings.outputRoot);
        }

        private void DrawComputedInfo()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Computed Scale", EditorStyles.boldLabel);
            float sideMeters = _settings.TargetSideMeters;
            float tileSize = _settings.TileSizeMeters;
            int tileCount = Mathf.CeilToInt(sideMeters / tileSize);
            float actualSide = tileCount * tileSize;
            float actualArea = (actualSide * actualSide) / 1000000f;

            EditorGUILayout.LabelField($"Target side length: {sideMeters:0} m");
            EditorGUILayout.LabelField($"Terrain grid: {tileCount} x {tileCount} tiles, tile size {tileSize:0} m");
            EditorGUILayout.LabelField($"Actual world side: {actualSide:0} m  |  actual area: {actualArea:0.0} km^2");
            EditorGUILayout.LabelField($"Quality boost: x{_settings.qualityBoost:0.0}");
        }

        private void DrawBuildButton()
        {
            EditorGUILayout.Space(10);
            GUI.backgroundColor = new Color(0.78f, 0.92f, 0.78f);
            if (GUILayout.Button("Build AAA Rural World", GUILayout.Height(36)))
            {
                BuildScene();
            }
            GUI.backgroundColor = Color.white;
        }

        private void BuildScene()
        {
            _settings.ValidateAndClamp();
            UnityEngine.Random.InitState(_settings.seed);

            try
            {
                EditorUtility.DisplayProgressBar(WindowTitle, "Preparing assets...", 0.03f);
                BuildContext ctx = BuildContextFactory.Prepare(_settings);

                EditorUtility.DisplayProgressBar(WindowTitle, "Generating world layout...", 0.08f);
                WorldLayout layout = WorldLayoutGenerator.Generate(_settings);

                EditorUtility.DisplayProgressBar(WindowTitle, "Preparing scene...", 0.12f);
                Scene scene = PrepareScene();
                LightingAndEnvironment.ConfigureEnvironment(_settings);

                GameObject root = new GameObject("GeneratedRuralWorld_Root");
                GameObject envRoot = new GameObject("Environment");
                envRoot.transform.SetParent(root.transform);
                GameObject roadsRoot = new GameObject("Roads");
                roadsRoot.transform.SetParent(root.transform);
                GameObject villageRoot = new GameObject("Village");
                villageRoot.transform.SetParent(root.transform);
                GameObject fieldsRoot = new GameObject("Fields");
                fieldsRoot.transform.SetParent(root.transform);
                GameObject forestRoot = new GameObject("ForestFar");
                forestRoot.transform.SetParent(root.transform);

                EditorUtility.DisplayProgressBar(WindowTitle, "Generating tiled terrain...", 0.28f);
                TerrainGrid grid = TerrainGenerator.CreateTerrainGrid(ctx, layout, _settings, envRoot.transform);

                EditorUtility.DisplayProgressBar(WindowTitle, "Painting terrain layers...", 0.42f);
                TerrainGenerator.PaintTerrainGrid(ctx, grid, layout);

                EditorUtility.DisplayProgressBar(WindowTitle, "Creating grass and wheat details...", 0.52f);
                TerrainGenerator.PopulateTerrainDetails(ctx, grid, layout, _settings);

                EditorUtility.DisplayProgressBar(WindowTitle, "Creating lake and river...", 0.56f);
                WaterGenerator.CreateWaterSystem(ctx, grid, layout, _settings, envRoot.transform);

                EditorUtility.DisplayProgressBar(WindowTitle, "Building roads and village streets...", 0.62f);
                RoadGenerator.CreateRoadMeshes(ctx, grid, layout, _settings, roadsRoot.transform);
                RoadGenerator.CreateBridge(ctx, grid, layout, roadsRoot.transform);
                RoadGenerator.CreateStreetFences(ctx, grid, layout, roadsRoot.transform);

                EditorUtility.DisplayProgressBar(WindowTitle, "Adding roadside details...", 0.71f);
                VillagePropsGenerator.CreateRoadsideProps(ctx, grid, layout, _settings, roadsRoot.transform);

                EditorUtility.DisplayProgressBar(WindowTitle, "Building village houses...", 0.80f);
                CreateVillageBuildings(ctx, grid, layout, villageRoot.transform);
                VillagePropsGenerator.CreateVillageStreetProps(ctx, grid, layout, _settings, villageRoot.transform);
                VillagePropsGenerator.CreateVillageGreenery(ctx, grid, layout, _settings, villageRoot.transform);
                VillagePropsGenerator.CreateLakeShoreProps(ctx, grid, layout, _settings, villageRoot.transform);

                EditorUtility.DisplayProgressBar(WindowTitle, "Adding field boundaries and props...", 0.89f);
                FieldPropsGenerator.CreateFieldBoundaryProps(ctx, grid, layout, _settings, fieldsRoot.transform);
                FieldPropsGenerator.CreateFieldStonePiles(ctx, grid, layout, _settings, fieldsRoot.transform);
                WaterGenerator.CreateWaterVegetation(ctx, grid, layout, _settings, fieldsRoot.transform);
                FieldPropsGenerator.CreateFieldProps(ctx, grid, layout, _settings, fieldsRoot.transform);

                EditorUtility.DisplayProgressBar(WindowTitle, "Creating far forest...", 0.94f);
                ForestGenerator.CreateFarForest(ctx, grid, layout, _settings, forestRoot.transform);

                EditorUtility.DisplayProgressBar(WindowTitle, "Building HLOD and streaming chunks...", 0.975f);
                OptimizationSystem.CreateWorldOptimizationSystem(ctx, layout, _settings, root, roadsRoot, villageRoot, fieldsRoot, forestRoot);

                EditorUtility.DisplayProgressBar(WindowTitle, "Final lighting and cameras...", 0.99f);
                LightingAndEnvironment.CreateLightingRig(root.transform, layout.villageCenter);
                LightingAndEnvironment.CreateCloudSystem(ctx, layout, _settings, root.transform);
                LightingAndEnvironment.CreateMainCamera(layout, _settings);
                LightingAndEnvironment.CreateReflectionProbe(layout, root.transform);
                LightingAndEnvironment.CreateGlobalVolume(ctx, root.transform);

                FinalizeScene(scene, root, layout);
                Debug.Log($"AAA Rural World complete. Area={_settings.mapAreaKm2:0.0} km^2, world={layout.worldSizeMeters:0} m, tiles={layout.tileCount}x{layout.tileCount}, houses={layout.houses.Count}, roads={layout.roads.Count}");
            }
            catch (Exception ex)
            {
                Debug.LogError("AAA Rural World Builder failed: " + ex);
                EditorUtility.DisplayDialog("Generation Failed", ex.Message, "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private Scene PrepareScene()
        {
            if (_settings.createNewScene)
                return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            return SceneManager.GetActiveScene();
        }

        private void CreateVillageBuildings(BuildContext ctx, TerrainGrid grid, WorldLayout layout, Transform parent)
        {
            GameObject housesRoot = new GameObject("Houses");
            housesRoot.transform.SetParent(parent);
            for (int i = 0; i < layout.houses.Count; i++)
            {
                BuildingBuilder.Build(ctx, grid, layout.houses[i], housesRoot.transform, i, _settings.villageStyle);
            }
        }

        private void FinalizeScene(Scene scene, GameObject root, WorldLayout layout)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (_settings.saveSceneAsset)
            {
                string scenePath = _settings.outputRoot + "/" + _settings.sceneName + ".unity";
                AssetFactory.DeleteExistingAsset<SceneAsset>(scenePath);
                EditorSceneManager.SaveScene(scene, scenePath);
            }

            Selection.activeGameObject = root;
            FocusSceneView(layout);
        }

        private static void FocusSceneView(WorldLayout layout)
        {
            if (SceneView.lastActiveSceneView == null)
                return;
            SceneView.lastActiveSceneView.LookAt(layout.villageCenter + new Vector3(0f, 30f, 0f), Quaternion.Euler(26f, 38f, 0f), layout.villageLengthMeters * 1.25f, true, true);
            SceneView.lastActiveSceneView.Repaint();
        }
    }
}
#endif
