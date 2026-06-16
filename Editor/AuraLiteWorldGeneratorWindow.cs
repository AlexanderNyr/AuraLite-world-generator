#if UNITY_EDITOR
using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
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
        private GenerationSettingsAsset _settingsAsset;
        private bool _showAdvanced;
        private Vector2 _scroll;
        private CancellationTokenSource _buildCancellation;

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
            _settingsAsset = (GenerationSettingsAsset)EditorGUILayout.ObjectField("Settings Preset", _settingsAsset, typeof(GenerationSettingsAsset), false);

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = _settingsAsset != null;
            if (GUILayout.Button("Load from Asset", GUILayout.Height(22)) && _settingsAsset != null)
                _settings = _settingsAsset.settings.Clone();
            if (GUILayout.Button("Save to Asset", GUILayout.Height(22)) && _settingsAsset != null)
            {
                _settingsAsset.settings = _settings.Clone();
                EditorUtility.SetDirty(_settingsAsset);
                AssetDatabase.SaveAssets();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
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
            if (EditorCoroutineRunner.IsRunning)
            {
                EditorUtility.DisplayDialog("Generation Running", "A world is already being generated. Please wait or cancel it.", "OK");
                return;
            }

            _settings.ValidateAndClamp();
            _buildCancellation?.Dispose();
            _buildCancellation = new CancellationTokenSource();
            EditorCoroutineRunner.Start(BuildSceneCoroutine(_buildCancellation.Token));
        }

        private bool CheckCancellation(string info, float progress, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return true;
            if (EditorUtility.DisplayCancelableProgressBar(WindowTitle, info, progress))
            {
                _buildCancellation?.Cancel();
                return true;
            }
            return false;
        }

        private IEnumerator BuildSceneCoroutine(CancellationToken cancellationToken)
        {
            Stopwatch timer = Stopwatch.StartNew();
            BuildContext ctx = null;
            WorldLayout layout = null;
            Scene scene = new Scene();
            GameObject root = null;
            GameObject envRoot = null;
            GameObject roadsRoot = null;
            GameObject villageRoot = null;
            GameObject fieldsRoot = null;
            GameObject forestRoot = null;
            TerrainGrid grid = null;

            try
            {
                if (CheckCancellation("Preparing assets...", 0.03f, cancellationToken))
                    throw new System.OperationCanceledException("Generation cancelled by user.");
                ctx = BuildContextFactory.Prepare(_settings);
                yield return null;

                if (CheckCancellation("Generating world layout...", 0.08f, cancellationToken))
                    throw new System.OperationCanceledException("Generation cancelled by user.");
                layout = WorldLayoutGenerator.Generate(_settings);
                yield return null;

                if (CheckCancellation("Preparing scene...", 0.12f, cancellationToken))
                    throw new System.OperationCanceledException("Generation cancelled by user.");
                scene = PrepareScene();
                LightingAndEnvironment.ConfigureEnvironment(_settings);

                root = new GameObject("GeneratedRuralWorld_Root");
                envRoot = new GameObject("Environment");
                envRoot.transform.SetParent(root.transform);
                roadsRoot = new GameObject("Roads");
                roadsRoot.transform.SetParent(root.transform);
                villageRoot = new GameObject("Village");
                villageRoot.transform.SetParent(root.transform);
                fieldsRoot = new GameObject("Fields");
                fieldsRoot.transform.SetParent(root.transform);
                forestRoot = new GameObject("ForestFar");
                forestRoot.transform.SetParent(root.transform);
                yield return null;

                if (CheckCancellation("Generating tiled terrain...", 0.28f, cancellationToken))
                    throw new System.OperationCanceledException("Generation cancelled by user.");
                yield return TerrainGenerator.CreateTerrainGrid(ctx, layout, _settings, envRoot.transform, result => grid = result, cancellationToken);

                if (CheckCancellation("Painting terrain layers...", 0.42f, cancellationToken))
                    throw new System.OperationCanceledException("Generation cancelled by user.");
                yield return TerrainGenerator.PaintTerrainGrid(ctx, grid, layout, null, cancellationToken);

                if (CheckCancellation("Creating grass and wheat details...", 0.52f, cancellationToken))
                    throw new System.OperationCanceledException("Generation cancelled by user.");
                yield return TerrainGenerator.PopulateTerrainDetails(ctx, grid, layout, _settings, null, cancellationToken);

                if (CheckCancellation("Creating lake and river...", 0.56f, cancellationToken))
                    throw new System.OperationCanceledException("Generation cancelled by user.");
                WaterGenerator.CreateWaterSystem(ctx, grid, layout, _settings, envRoot.transform);
                yield return null;

                if (CheckCancellation("Building roads and village streets...", 0.62f, cancellationToken))
                    throw new System.OperationCanceledException("Generation cancelled by user.");
                RoadGenerator.CreateRoadMeshes(ctx, grid, layout, _settings, roadsRoot.transform);
                RoadGenerator.CreateBridge(ctx, grid, layout, roadsRoot.transform);
                RoadGenerator.CreateStreetFences(ctx, grid, layout, roadsRoot.transform);
                yield return null;

                if (CheckCancellation("Adding roadside details...", 0.71f, cancellationToken))
                    throw new System.OperationCanceledException("Generation cancelled by user.");
                VillagePropsGenerator.CreateRoadsideProps(ctx, grid, layout, _settings, roadsRoot.transform);
                yield return null;

                if (CheckCancellation("Building village houses...", 0.80f, cancellationToken))
                    throw new System.OperationCanceledException("Generation cancelled by user.");
                yield return CreateVillageBuildings(ctx, grid, layout, villageRoot.transform, cancellationToken);
                VillagePropsGenerator.CreateVillageStreetProps(ctx, grid, layout, _settings, villageRoot.transform);
                VillagePropsGenerator.CreateVillageGreenery(ctx, grid, layout, _settings, villageRoot.transform);
                VillagePropsGenerator.CreateLakeShoreProps(ctx, grid, layout, _settings, villageRoot.transform);
                yield return null;

                if (CheckCancellation("Adding field boundaries and props...", 0.89f, cancellationToken))
                    throw new System.OperationCanceledException("Generation cancelled by user.");
                FieldPropsGenerator.CreateFieldBoundaryProps(ctx, grid, layout, _settings, fieldsRoot.transform);
                FieldPropsGenerator.CreateFieldStonePiles(ctx, grid, layout, _settings, fieldsRoot.transform);
                WaterGenerator.CreateWaterVegetation(ctx, grid, layout, _settings, fieldsRoot.transform);
                FieldPropsGenerator.CreateFieldProps(ctx, grid, layout, _settings, fieldsRoot.transform);
                yield return null;

                if (CheckCancellation("Creating far forest...", 0.94f, cancellationToken))
                    throw new System.OperationCanceledException("Generation cancelled by user.");
                yield return ForestGenerator.CreateFarForest(ctx, grid, layout, _settings, forestRoot.transform, cancellationToken);

                if (CheckCancellation("Building HLOD and streaming chunks...", 0.975f, cancellationToken))
                    throw new System.OperationCanceledException("Generation cancelled by user.");
                OptimizationSystem.CreateWorldOptimizationSystem(ctx, layout, _settings, root, roadsRoot, villageRoot, fieldsRoot, forestRoot);
                yield return null;

                if (CheckCancellation("Final lighting and cameras...", 0.99f, cancellationToken))
                    throw new System.OperationCanceledException("Generation cancelled by user.");
                LightingAndEnvironment.CreateLightingRig(root.transform, layout.villageCenter);
                LightingAndEnvironment.CreateCloudSystem(ctx, layout, _settings, root.transform);
                LightingAndEnvironment.CreateMainCamera(layout, _settings);
                LightingAndEnvironment.CreateReflectionProbe(layout, root.transform);
                LightingAndEnvironment.CreateGlobalVolume(ctx, root.transform);
                yield return null;

                FinalizeScene(scene, root, layout);
                timer.Stop();
                Debug.Log($"AAA Rural World complete in {timer.Elapsed.TotalSeconds:0.0}s. " +
                    $"Area={_settings.mapAreaKm2:0.0} km^2, world={layout.worldSizeMeters:0} m, " +
                    $"tiles={layout.tileCount}x{layout.tileCount}, houses={layout.houses.Count}, roads={layout.roads.Count}");
            }
            catch (System.OperationCanceledException)
            {
                timer.Stop();
                Debug.LogWarning($"AAA Rural World generation cancelled after {timer.Elapsed.TotalSeconds:0.0}s.");
                EditorUtility.DisplayDialog("Generation Cancelled", "The generation was cancelled by the user.", "OK");
            }
            catch (Exception ex)
            {
                timer.Stop();
                Debug.LogError("AAA Rural World Builder failed: " + ex);
                EditorUtility.DisplayDialog("Generation Failed", ex.Message, "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                _buildCancellation?.Dispose();
                _buildCancellation = null;
            }
        }

        private Scene PrepareScene()
        {
            if (_settings.createNewScene)
                return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            return SceneManager.GetActiveScene();
        }

        private IEnumerator CreateVillageBuildings(BuildContext ctx, TerrainGrid grid, WorldLayout layout, Transform parent, CancellationToken cancellationToken)
        {
            GameObject housesRoot = new GameObject("Houses");
            housesRoot.transform.SetParent(parent);
            const int batchSize = 10;
            for (int i = 0; i < layout.houses.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                BuildingBuilder.Build(ctx, grid, layout.houses[i], housesRoot.transform, i, _settings.villageStyle, layout.random);
                if ((i + 1) % batchSize == 0)
                    yield return null;
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
