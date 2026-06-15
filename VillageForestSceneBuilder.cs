#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class VillageForestSceneBuilder : EditorWindow
{
    private enum BuildingKind
    {
        Cottage,
        Farmhouse,
        Barn,
        LongHouse,
        Workshop,
        Manor,
        Chapel,
        Forge,
        Mill,
        Tavern,
        Stable,
        Granary,
        Boathouse
    }

    private enum VillageStyle
    {
        European,
        Russian
    }

    [Serializable]
    private class HouseSpec
    {
        public Vector3 position;
        public float yaw;
        public Vector2 footprint;
        public float height;
        public BuildingKind kind;
        public bool fenced;
        public bool garden;
        public bool annex;
    }

    private class RoadPath
    {
        public string name;
        public float width;
        public bool mainRoad;
        public bool hasStoneShoulders;
        public readonly List<Vector3> points = new List<Vector3>();
    }

    private class WorldLayout
    {
        public float worldSizeMeters;
        public float tileSizeMeters;
        public int tileCount;
        public float villageLengthMeters;
        public float villageHalfWidthMeters;
        public float terrainHeightMeters;
        public float forestStartZ;
        public float farmlandCellSize;
        public float waterLevel;
        public float lakeRadiusX;
        public float lakeRadiusZ;
        public float riverWidth;
        public Vector3 villageCenter;
        public Vector3 lakeCenter;
        public readonly List<RoadPath> roads = new List<RoadPath>();
        public readonly List<Vector3> riverPoints = new List<Vector3>();
        public readonly List<HouseSpec> houses = new List<HouseSpec>();
    }

    private class TerrainGrid
    {
        public float tileSize;
        public int tileCount;
        public Terrain[,] terrains;
    }

    private class BuildContext
    {
        public string rootFolder;
        public string materialsFolder;
        public string texturesFolder;
        public string meshesFolder;
        public string volumesFolder;
        public string terrainFolder;

        public Shader litShader;
        public Shader terrainShader;

        public Material terrainMat;
        public Material roadMat;
        public Material shoulderMat;
        public Material grassPreviewMat;
        public Material wheatPreviewMat;
        public Material dirtMat;
        public Material wallCreamMat;
        public Material wallWarmMat;
        public Material timberMat;
        public Material roofRedMat;
        public Material roofDarkMat;
        public Material stoneMat;
        public Material barkMat;
        public Material pineMat;
        public Material leafMat;
        public Material glassMat;
        public Material wheatBaleMat;
        public Material waterMat;
        public Material grassBladeMat;
        public Material wheatBladeMat;
        public Material logWallMat;
        public Material copperRoofMat;
        public Material forgeFireMat;
        public Material cloudMat;

        public Texture2D grassTex;
        public Texture2D wheatTex;
        public Texture2D dirtTex;
        public Texture2D forestTex;
        public Texture2D cloudTex;

        public TerrainLayer grassLayer;
        public TerrainLayer wheatLayer;
        public TerrainLayer dirtLayer;
        public TerrainLayer forestLayer;

        public Mesh roofMesh;
        public Mesh coneMesh;
        public Mesh grassBladeMesh;
        public Mesh wheatBladeMesh;
        public Mesh quadMesh;

        public GameObject grassDetailPrefab;
        public GameObject wheatDetailPrefab;

        public VolumeProfile globalVolumeProfile;
    }

    private string sceneName = "AAA_RuralVillage_URP";
    private int seed = 240615;
    private bool createNewScene = true;
    private bool saveSceneAsset = true;
    private float mapAreaKm2 = 40f;
    private float terrainHeightMeters = 180f;
    private float villageLengthMeters = 700f;
    private float mainStreetWidthMeters = 7f;
    private float laneWidthMeters = 4.6f;
    private float houseDensity = 1.0f;
    private float wheatRatio = 0.58f;
    private float qualityBoost = 2.0f;
    private VillageStyle villageStyle = VillageStyle.European;
    private float fogStartKm = 30f;
    private float fogEndKm = 45f;
    private string outputRoot = "Assets/GeneratedVillageScene";

    [MenuItem("Tools/Procedural Scenes/Build AAA Rural World (URP)")]
    public static void OpenWindow()
    {
        GetWindow<VillageForestSceneBuilder>("AAA Rural World URP");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("AAA Rural World Builder (URP)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Генерирует большую сельскую сцену в метрах: обычная деревня вдоль улиц, нормальные дороги, один дальний лес и остальная территория как поля/луга. " +
            "Масштаб карты задаётся в квадратных километрах.",
            MessageType.Info);

        sceneName = EditorGUILayout.TextField("Scene Name", sceneName);
        seed = EditorGUILayout.IntField("Seed", seed);
        createNewScene = EditorGUILayout.Toggle("Create New Scene", createNewScene);
        saveSceneAsset = EditorGUILayout.Toggle("Save Scene Asset", saveSceneAsset);

        mapAreaKm2 = EditorGUILayout.Slider("Map Area (km²)", mapAreaKm2, 20f, 150f);
        terrainHeightMeters = EditorGUILayout.Slider("Terrain Height Range (m)", terrainHeightMeters, 80f, 320f);
        villageLengthMeters = EditorGUILayout.Slider("Village Length (m)", villageLengthMeters, 350f, 1400f);
        mainStreetWidthMeters = EditorGUILayout.Slider("Main Street Width (m)", mainStreetWidthMeters, 5.5f, 9.5f);
        laneWidthMeters = EditorGUILayout.Slider("Village Lane Width (m)", laneWidthMeters, 3.2f, 6f);
        houseDensity = EditorGUILayout.Slider("House Density", houseDensity, 0.65f, 1.35f);
        wheatRatio = EditorGUILayout.Slider("Wheat Field Ratio", wheatRatio, 0.2f, 0.85f);
        qualityBoost = EditorGUILayout.Slider("Quality Boost", qualityBoost, 1.0f, 3.0f);
        villageStyle = (VillageStyle)EditorGUILayout.EnumPopup("Village Style", villageStyle);
        fogStartKm = EditorGUILayout.Slider("Fog Start (km)", fogStartKm, 5f, 30f);
        fogEndKm = EditorGUILayout.Slider("Fog End (km)", fogEndKm, 10f, 60f);
        outputRoot = EditorGUILayout.TextField("Output Root", outputRoot);

        float sideMeters = Mathf.Sqrt(mapAreaKm2) * 1000f;
        float tileSize = DetermineTileSize(sideMeters);
        int tileCount = Mathf.CeilToInt(sideMeters / tileSize);
        float actualSide = tileCount * tileSize;
        float actualArea = (actualSide * actualSide) / 1000000f;

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Computed Scale", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Target side length: {sideMeters:0} m");
        EditorGUILayout.LabelField($"Terrain grid: {tileCount} x {tileCount} tiles, tile size {tileSize:0} m");
        EditorGUILayout.LabelField($"Actual world side: {actualSide:0} m  |  actual area: {actualArea:0.0} km²");
        EditorGUILayout.LabelField($"Quality boost: x{qualityBoost:0.0}");

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
        try
        {
            UnityEngine.Random.InitState(seed);
            EditorUtility.DisplayProgressBar("AAA Rural World Builder", "Preparing assets...", 0.03f);
            BuildContext ctx = PrepareAssets();

            WorldLayout layout = GenerateWorldLayout();

            EditorUtility.DisplayProgressBar("AAA Rural World Builder", "Preparing scene...", 0.08f);
            Scene scene;
            if (createNewScene)
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
            else
            {
                scene = SceneManager.GetActiveScene();
            }

            ConfigureEnvironment();

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

            EditorUtility.DisplayProgressBar("AAA Rural World Builder", "Generating tiled terrain...", 0.28f);
            TerrainGrid grid = CreateTerrainGrid(ctx, layout, envRoot.transform);

            EditorUtility.DisplayProgressBar("AAA Rural World Builder", "Painting terrain layers...", 0.42f);
            PaintTerrainGrid(ctx, grid, layout);

            EditorUtility.DisplayProgressBar("AAA Rural World Builder", "Creating grass and wheat details...", 0.52f);
            PopulateTerrainDetails(ctx, grid, layout);

            EditorUtility.DisplayProgressBar("AAA Rural World Builder", "Creating lake and river...", 0.56f);
            CreateWaterSystem(ctx, grid, layout, envRoot.transform);

            EditorUtility.DisplayProgressBar("AAA Rural World Builder", "Building roads and village streets...", 0.62f);
            CreateRoadMeshes(ctx, grid, layout, roadsRoot.transform);
            CreateBridge(ctx, grid, layout, roadsRoot.transform);
            CreateStreetFences(ctx, grid, layout, roadsRoot.transform);

            EditorUtility.DisplayProgressBar("AAA Rural World Builder", "Adding roadside details...", 0.71f);
            CreateRoadsideProps(ctx, grid, layout, roadsRoot.transform);

            EditorUtility.DisplayProgressBar("AAA Rural World Builder", "Building village houses...", 0.80f);
            CreateVillageBuildings(ctx, grid, layout, villageRoot.transform);
            CreateVillageStreetProps(ctx, grid, layout, villageRoot.transform);
            CreateVillageGreenery(ctx, grid, layout, villageRoot.transform);
            CreateLakeShoreProps(ctx, grid, layout, villageRoot.transform);

            EditorUtility.DisplayProgressBar("AAA Rural World Builder", "Adding field boundaries and props...", 0.89f);
            CreateFieldBoundaryProps(ctx, grid, layout, fieldsRoot.transform);
            CreateFieldStonePiles(ctx, grid, layout, fieldsRoot.transform);
            CreateWaterVegetation(ctx, grid, layout, fieldsRoot.transform);
            CreateFieldProps(ctx, grid, layout, fieldsRoot.transform);

            EditorUtility.DisplayProgressBar("AAA Rural World Builder", "Creating far forest...", 0.94f);
            CreateFarForest(ctx, grid, layout, forestRoot.transform);

            EditorUtility.DisplayProgressBar("AAA Rural World Builder", "Building HLOD and streaming chunks...", 0.975f);
            CreateWorldOptimizationSystem(ctx, layout, root, roadsRoot, villageRoot, fieldsRoot, forestRoot);

            EditorUtility.DisplayProgressBar("AAA Rural World Builder", "Final lighting and cameras...", 0.99f);
            CreateLightingRig(root.transform, layout.villageCenter);
            CreateCloudSystem(ctx, layout, root.transform);
            CreateMainCamera(layout);
            CreateReflectionProbe(layout, root.transform);
            CreateGlobalVolume(ctx, root.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (saveSceneAsset)
            {
                string scenePath = outputRoot + "/" + sceneName + ".unity";
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null)
                    AssetDatabase.DeleteAsset(scenePath);
                EditorSceneManager.SaveScene(scene, scenePath);
            }

            Selection.activeGameObject = root;
            FocusSceneView(layout);
            Debug.Log($"AAA Rural World complete. Area={mapAreaKm2:0.0} km², world={layout.worldSizeMeters:0} m, tiles={layout.tileCount}x{layout.tileCount}, houses={layout.houses.Count}, roads={layout.roads.Count}");
        }
        catch (Exception ex)
        {
            Debug.LogError("AAA Rural World Builder failed: " + ex);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private WorldLayout GenerateWorldLayout()
    {
        WorldLayout layout = new WorldLayout();
        float targetSide = Mathf.Sqrt(mapAreaKm2) * 1000f;
        layout.tileSizeMeters = DetermineTileSize(targetSide);
        layout.tileCount = Mathf.CeilToInt(targetSide / layout.tileSizeMeters);
        layout.worldSizeMeters = layout.tileCount * layout.tileSizeMeters;
        layout.terrainHeightMeters = terrainHeightMeters;
        layout.villageLengthMeters = Mathf.Clamp(villageLengthMeters, 350f, Mathf.Min(1400f, layout.worldSizeMeters * 0.22f));
        layout.villageHalfWidthMeters = Mathf.Clamp(layout.villageLengthMeters * 0.38f, 150f, 320f);
        layout.farmlandCellSize = Mathf.Clamp(layout.worldSizeMeters / 15f, 220f, 480f);
        layout.villageCenter = new Vector3(layout.worldSizeMeters * 0.38f, 0f, layout.worldSizeMeters * 0.42f);
        layout.waterLevel = 12.4f;
        layout.lakeCenter = layout.villageCenter + new Vector3(-layout.villageLengthMeters * 0.62f, 0f, -layout.villageHalfWidthMeters * 1.65f);
        layout.lakeRadiusX = Mathf.Clamp(layout.villageLengthMeters * 0.22f, 120f, 220f);
        layout.lakeRadiusZ = Mathf.Clamp(layout.villageLengthMeters * 0.17f, 90f, 160f);
        layout.riverWidth = Mathf.Clamp(layout.villageLengthMeters * 0.014f, 9f, 16f);
        layout.forestStartZ = Mathf.Clamp(layout.villageCenter.z + Mathf.Max(1400f, layout.worldSizeMeters * 0.24f), layout.worldSizeMeters * 0.60f, layout.worldSizeMeters * 0.80f);

        CreateRiverPath(layout);
        CreateRoadNetwork(layout);
        PopulateVillageHouses(layout);
        return layout;
    }

    private void CreateRiverPath(WorldLayout layout)
    {
        Vector3 start = layout.lakeCenter + new Vector3(layout.lakeRadiusX * 0.75f, 0f, layout.lakeRadiusZ * 0.10f);
        layout.riverPoints.Add(start);
        layout.riverPoints.Add(layout.villageCenter + new Vector3(-layout.villageLengthMeters * 0.24f, 0f, -layout.villageHalfWidthMeters * 0.92f));
        layout.riverPoints.Add(layout.villageCenter + new Vector3(layout.villageLengthMeters * 0.18f, 0f, -layout.villageHalfWidthMeters * 0.55f));
        layout.riverPoints.Add(layout.villageCenter + new Vector3(layout.villageLengthMeters * 0.52f, 0f, -layout.villageHalfWidthMeters * 0.18f));
        layout.riverPoints.Add(new Vector3(layout.worldSizeMeters * 0.78f, 0f, layout.villageCenter.z - layout.villageHalfWidthMeters * 0.16f));
        layout.riverPoints.Add(new Vector3(layout.worldSizeMeters - 120f, 0f, layout.villageCenter.z + layout.villageHalfWidthMeters * 0.05f));
    }

    private void CreateRoadNetwork(WorldLayout layout)
    {
        float mainHalf = layout.villageLengthMeters * 0.62f;
        RoadPath mainRoad = new RoadPath
        {
            name = "MainStreet",
            width = mainStreetWidthMeters,
            mainRoad = true,
            hasStoneShoulders = true
        };

        for (int i = 0; i < Mathf.RoundToInt(8f * Mathf.Lerp(1f, 1.6f, Mathf.InverseLerp(1f, 3f, qualityBoost))); i++)
        {
            float t = i / 7f;
            float x = layout.villageCenter.x - mainHalf + t * mainHalf * 2f;
            float z = layout.villageCenter.z + Mathf.Sin(t * Mathf.PI * 1.2f + 0.5f) * 12f + (Mathf.PerlinNoise(seed * 0.01f + t * 2.1f, 0.24f) - 0.5f) * 10f;
            mainRoad.points.Add(new Vector3(x, 0f, z));
        }
        layout.roads.Add(mainRoad);

        RoadPath crossRoad = new RoadPath
        {
            name = "CrossRoad",
            width = laneWidthMeters + 0.9f,
            mainRoad = false,
            hasStoneShoulders = true
        };
        for (int i = 0; i < 6; i++)
        {
            float t = i / 5f;
            float z = layout.villageCenter.z - layout.villageHalfWidthMeters * 1.02f + t * layout.villageHalfWidthMeters * 2.04f;
            float x = layout.villageCenter.x + Mathf.Sin(t * Mathf.PI) * 16f + (Mathf.PerlinNoise(seed * 0.012f + t * 2.0f, 0.54f) - 0.5f) * 10f;
            crossRoad.points.Add(new Vector3(x, 0f, z));
        }
        layout.roads.Add(crossRoad);

        RoadPath northLane = new RoadPath
        {
            name = "NorthLane",
            width = laneWidthMeters,
            mainRoad = false,
            hasStoneShoulders = false
        };
        northLane.points.Add(layout.villageCenter + new Vector3(-layout.villageLengthMeters * 0.48f, 0f, layout.villageHalfWidthMeters * 0.45f));
        northLane.points.Add(layout.villageCenter + new Vector3(-layout.villageLengthMeters * 0.16f, 0f, layout.villageHalfWidthMeters * 0.68f));
        northLane.points.Add(layout.villageCenter + new Vector3(layout.villageLengthMeters * 0.20f, 0f, layout.villageHalfWidthMeters * 0.60f));
        northLane.points.Add(layout.villageCenter + new Vector3(layout.villageLengthMeters * 0.50f, 0f, layout.villageHalfWidthMeters * 0.38f));
        layout.roads.Add(northLane);

        RoadPath southLane = new RoadPath
        {
            name = "SouthLane",
            width = laneWidthMeters,
            mainRoad = false,
            hasStoneShoulders = false
        };
        southLane.points.Add(layout.villageCenter + new Vector3(-layout.villageLengthMeters * 0.44f, 0f, -layout.villageHalfWidthMeters * 0.55f));
        southLane.points.Add(layout.villageCenter + new Vector3(-layout.villageLengthMeters * 0.12f, 0f, -layout.villageHalfWidthMeters * 0.70f));
        southLane.points.Add(layout.villageCenter + new Vector3(layout.villageLengthMeters * 0.15f, 0f, -layout.villageHalfWidthMeters * 0.68f));
        southLane.points.Add(layout.villageCenter + new Vector3(layout.villageLengthMeters * 0.44f, 0f, -layout.villageHalfWidthMeters * 0.42f));
        layout.roads.Add(southLane);

        for (int i = 0; i < 4; i++)
        {
            RoadPath spur = new RoadPath
            {
                name = "Lane_" + i,
                width = laneWidthMeters,
                mainRoad = false,
                hasStoneShoulders = i % 2 == 0
            };

            float xOff = Mathf.Lerp(-0.40f, 0.42f, i / 3f) * layout.villageLengthMeters;
            Vector3 anchor = layout.villageCenter + new Vector3(xOff, 0f, (i % 2 == 0 ? 1f : -1f) * 18f);
            float dir = i % 2 == 0 ? 1f : -1f;
            float length = Mathf.Lerp(170f, 280f, Mathf.Abs(Mathf.Sin(seed * 0.11f + i * 1.7f)));

            spur.points.Add(anchor);
            spur.points.Add(anchor + new Vector3(UnityEngine.Random.Range(-12f, 12f), 0f, dir * length * 0.38f));
            spur.points.Add(anchor + new Vector3(UnityEngine.Random.Range(-24f, 24f), 0f, dir * length * 0.76f));
            spur.points.Add(anchor + new Vector3(UnityEngine.Random.Range(-30f, 30f), 0f, dir * length));
            layout.roads.Add(spur);

            RoadPath track = new RoadPath
            {
                name = "FarmTrack_" + i,
                width = Mathf.Max(3.2f, laneWidthMeters * 0.78f),
                mainRoad = false,
                hasStoneShoulders = false
            };
            Vector3 trackStart = spur.points[spur.points.Count - 1];
            float trackDir = dir;
            float trackLength = Mathf.Lerp(340f, 760f, Mathf.Abs(Mathf.Sin(seed * 0.073f + i * 2.7f)));
            track.points.Add(trackStart);
            track.points.Add(trackStart + new Vector3(UnityEngine.Random.Range(-70f, 70f), 0f, trackDir * trackLength * 0.46f));
            track.points.Add(trackStart + new Vector3(UnityEngine.Random.Range(-140f, 140f), 0f, trackDir * trackLength));
            layout.roads.Add(track);
        }
    }

    private void PopulateVillageHouses(WorldLayout layout)
    {
        float densitySpacing = Mathf.Lerp(1.28f, 0.78f, Mathf.InverseLerp(0.65f, 1.35f, houseDensity));
        densitySpacing *= Mathf.Lerp(1f, 0.82f, Mathf.InverseLerp(1f, 3f, qualityBoost));

        for (int i = 0; i < layout.roads.Count; i++)
        {
            RoadPath road = layout.roads[i];
            float length = GetPathLength(road);

            if (road.name == "MainStreet")
            {
                PlaceHousesAlongRoad(layout, road, length * 0.05f, length * 0.95f, 24f * densitySpacing, 14f, true, BuildingKind.Farmhouse, BuildingKind.Cottage, BuildingKind.Workshop, BuildingKind.LongHouse);
            }
            else if (road.name == "CrossRoad")
            {
                PlaceHousesAlongRoad(layout, road, length * 0.08f, length * 0.92f, 26f * densitySpacing, 13f, true, BuildingKind.Cottage, BuildingKind.Farmhouse, BuildingKind.Workshop);
            }
            else if (road.name == "NorthLane" || road.name == "SouthLane")
            {
                PlaceHousesAlongRoad(layout, road, length * 0.05f, length * 0.96f, 27f * densitySpacing, 12f, false, BuildingKind.Cottage, BuildingKind.Farmhouse, BuildingKind.Barn);
            }
            else if (road.name.StartsWith("Lane_"))
            {
                PlaceHousesAlongRoad(layout, road, length * 0.10f, length * 0.92f, 31f * densitySpacing, 12f, false, BuildingKind.Cottage, BuildingKind.Barn, BuildingKind.Farmhouse);
            }
            else if (road.name.StartsWith("FarmTrack"))
            {
                PlaceHousesAlongRoad(layout, road, length * 0.10f, length * 0.40f, 78f * densitySpacing, 16f, false, BuildingKind.Farmhouse, BuildingKind.Barn);
            }
        }

        for (int i = 0; i < 8; i++)
        {
            float x = layout.villageCenter.x + UnityEngine.Random.Range(-layout.villageLengthMeters * 0.38f, layout.villageLengthMeters * 0.38f);
            float z = layout.villageCenter.z + UnityEngine.Random.Range(-layout.villageHalfWidthMeters * 0.85f, layout.villageHalfWidthMeters * 0.85f);
            if (ComputeRiverMask(layout, x, z) > 0.12f)
                continue;
            HouseSpec barn = CreateHouseSpec(new Vector3(x, 0f, z), UnityEngine.Random.Range(0f, 360f), BuildingKind.Barn);
            barn.fenced = false;
            barn.garden = false;
            if (CanPlaceHouse(layout, barn.position, 24f))
                layout.houses.Add(barn);
        }

        HouseSpec manor = CreateHouseSpec(
            new Vector3(layout.villageCenter.x + layout.villageLengthMeters * 0.24f, 0f, layout.villageCenter.z - layout.villageHalfWidthMeters * 0.74f),
            12f,
            BuildingKind.Manor);
        manor.fenced = true;
        manor.garden = true;
        manor.annex = true;
        if (CanPlaceHouse(layout, manor.position, 30f))
            layout.houses.Add(manor);

        HouseSpec chapel = CreateHouseSpec(
            new Vector3(layout.villageCenter.x - layout.villageLengthMeters * 0.10f, 0f, layout.villageCenter.z + layout.villageHalfWidthMeters * 0.14f),
            -6f,
            BuildingKind.Chapel);
        chapel.fenced = false;
        chapel.garden = false;
        chapel.annex = false;
        if (CanPlaceHouse(layout, chapel.position, 26f))
            layout.houses.Add(chapel);

        HouseSpec forge = CreateHouseSpec(
            new Vector3(layout.villageCenter.x + layout.villageLengthMeters * 0.06f, 0f, layout.villageCenter.z - layout.villageHalfWidthMeters * 0.10f),
            18f,
            BuildingKind.Forge);
        forge.fenced = false;
        forge.garden = false;
        forge.annex = true;
        if (CanPlaceHouse(layout, forge.position, 24f))
            layout.houses.Add(forge);

        HouseSpec mill = CreateHouseSpec(
            layout.lakeCenter + new Vector3(layout.lakeRadiusX * 0.92f, 0f, layout.lakeRadiusZ * 0.16f),
            -10f,
            BuildingKind.Mill);
        mill.fenced = false;
        mill.garden = false;
        mill.annex = false;
        if (CanPlaceHouse(layout, mill.position, 34f))
            layout.houses.Add(mill);

        HouseSpec tavern = CreateHouseSpec(
            new Vector3(layout.villageCenter.x - layout.villageLengthMeters * 0.02f, 0f, layout.villageCenter.z - layout.villageHalfWidthMeters * 0.04f),
            8f,
            BuildingKind.Tavern);
        tavern.fenced = false;
        tavern.garden = false;
        tavern.annex = true;
        if (CanPlaceHouse(layout, tavern.position, 26f))
            layout.houses.Add(tavern);

        HouseSpec stable = CreateHouseSpec(
            new Vector3(layout.villageCenter.x + layout.villageLengthMeters * 0.38f, 0f, layout.villageCenter.z + layout.villageHalfWidthMeters * 0.52f),
            22f,
            BuildingKind.Stable);
        stable.fenced = true;
        stable.garden = false;
        stable.annex = false;
        if (CanPlaceHouse(layout, stable.position, 24f))
            layout.houses.Add(stable);

        HouseSpec granary = CreateHouseSpec(
            new Vector3(layout.villageCenter.x - layout.villageLengthMeters * 0.32f, 0f, layout.villageCenter.z - layout.villageHalfWidthMeters * 0.52f),
            -18f,
            BuildingKind.Granary);
        granary.fenced = false;
        granary.garden = false;
        granary.annex = false;
        if (CanPlaceHouse(layout, granary.position, 24f))
            layout.houses.Add(granary);

        HouseSpec boathouse = CreateHouseSpec(
            layout.lakeCenter + new Vector3(layout.lakeRadiusX * 0.34f, 0f, layout.lakeRadiusZ * 0.92f),
            164f,
            BuildingKind.Boathouse);
        boathouse.fenced = false;
        boathouse.garden = false;
        boathouse.annex = false;
        if (CanPlaceHouse(layout, boathouse.position, 22f))
            layout.houses.Add(boathouse);
    }

    private void PlaceHousesAlongRoad(WorldLayout layout, RoadPath road, float startDistance, float endDistance, float spacingBase, float setback, bool allowLargeMix, params BuildingKind[] palette)
    {
        float d = startDistance;
        int sideToggle = 0;
        while (d < endDistance)
        {
            float spacing = spacingBase * Mathf.Lerp(0.9f, 1.3f, Mathf.Abs(Mathf.Sin((seed + d) * 0.013f)));
            Vector3 center = SamplePath(road, d);
            Vector3 forward = DirectionOnPath(road, d);
            Vector3 side = Vector3.Cross(Vector3.up, forward).normalized;

            bool placeLeft = sideToggle % 2 == 0 || UnityEngine.Random.value > 0.4f;
            bool placeRight = sideToggle % 3 != 0 || UnityEngine.Random.value > 0.62f;
            if (!placeLeft && !placeRight)
                placeLeft = true;

            if (placeLeft)
                TryAddHouse(layout, center + side * (road.width * 0.5f + setback + UnityEngine.Random.Range(1f, 5f)), Quaternion.LookRotation(-side, Vector3.up).eulerAngles.y, palette, allowLargeMix);
            if (placeRight)
                TryAddHouse(layout, center - side * (road.width * 0.5f + setback + UnityEngine.Random.Range(1f, 5f)), Quaternion.LookRotation(side, Vector3.up).eulerAngles.y, palette, allowLargeMix);

            sideToggle++;
            d += spacing;
        }
    }

    private void TryAddHouse(WorldLayout layout, Vector3 pos, float yaw, BuildingKind[] palette, bool allowLargeMix)
    {
        if (Mathf.Abs(pos.z - layout.villageCenter.z) > layout.villageHalfWidthMeters + 150f && UnityEngine.Random.value > 0.55f)
            return;
        if (ComputeLakeMask(layout, pos.x, pos.z) > 0.06f || ComputeRiverMask(layout, pos.x, pos.z) > 0.08f)
            return;

        BuildingKind kind = palette[UnityEngine.Random.Range(0, palette.Length)];
        if (!allowLargeMix && kind == BuildingKind.LongHouse && UnityEngine.Random.value > 0.25f)
            kind = BuildingKind.Cottage;

        HouseSpec spec = CreateHouseSpec(pos, yaw, kind);
        if (CanPlaceHouse(layout, pos, Mathf.Max(spec.footprint.x, spec.footprint.y) * 1.55f))
            layout.houses.Add(spec);
    }

    private HouseSpec CreateHouseSpec(Vector3 pos, float yaw, BuildingKind kind)
    {
        HouseSpec spec = new HouseSpec
        {
            position = pos,
            yaw = yaw,
            kind = kind,
            fenced = UnityEngine.Random.value > 0.34f,
            garden = UnityEngine.Random.value > 0.30f,
            annex = UnityEngine.Random.value > 0.52f
        };

        switch (kind)
        {
            case BuildingKind.Cottage:
                spec.footprint = new Vector2(UnityEngine.Random.Range(7.2f, 8.9f), UnityEngine.Random.Range(6.8f, 8.5f));
                spec.height = UnityEngine.Random.Range(4.3f, 5.5f);
                break;
            case BuildingKind.Farmhouse:
                spec.footprint = new Vector2(UnityEngine.Random.Range(8.4f, 10.6f), UnityEngine.Random.Range(7.6f, 9.8f));
                spec.height = UnityEngine.Random.Range(4.8f, 6.1f);
                break;
            case BuildingKind.Barn:
                spec.footprint = new Vector2(UnityEngine.Random.Range(9.5f, 13.5f), UnityEngine.Random.Range(8.6f, 11.4f));
                spec.height = UnityEngine.Random.Range(4.2f, 5.3f);
                spec.fenced = false;
                spec.garden = false;
                break;
            case BuildingKind.LongHouse:
                spec.footprint = new Vector2(UnityEngine.Random.Range(11.5f, 15.5f), UnityEngine.Random.Range(8.2f, 10.4f));
                spec.height = UnityEngine.Random.Range(5.3f, 6.6f);
                break;
            case BuildingKind.Workshop:
                spec.footprint = new Vector2(UnityEngine.Random.Range(8.8f, 10.8f), UnityEngine.Random.Range(7.4f, 9.2f));
                spec.height = UnityEngine.Random.Range(4.7f, 5.8f);
                spec.garden = false;
                break;
            case BuildingKind.Chapel:
                spec.footprint = new Vector2(9.5f, 16.0f);
                spec.height = 8.4f;
                spec.fenced = false;
                spec.garden = false;
                spec.annex = false;
                break;
            case BuildingKind.Forge:
                spec.footprint = new Vector2(10.6f, 8.8f);
                spec.height = 5.6f;
                spec.fenced = false;
                spec.garden = false;
                spec.annex = true;
                break;
            case BuildingKind.Mill:
                spec.footprint = new Vector2(11.8f, 11.8f);
                spec.height = 9.2f;
                spec.fenced = false;
                spec.garden = false;
                spec.annex = false;
                break;
            case BuildingKind.Tavern:
                spec.footprint = new Vector2(13.2f, 10.4f);
                spec.height = 6.4f;
                spec.fenced = false;
                spec.garden = false;
                spec.annex = true;
                break;
            case BuildingKind.Stable:
                spec.footprint = new Vector2(12.8f, 9.8f);
                spec.height = 5.2f;
                spec.fenced = true;
                spec.garden = false;
                spec.annex = false;
                break;
            case BuildingKind.Granary:
                spec.footprint = new Vector2(9.2f, 8.8f);
                spec.height = 6.0f;
                spec.fenced = false;
                spec.garden = false;
                spec.annex = false;
                break;
            case BuildingKind.Boathouse:
                spec.footprint = new Vector2(10.8f, 8.4f);
                spec.height = 4.8f;
                spec.fenced = false;
                spec.garden = false;
                spec.annex = false;
                break;
            default:
                spec.footprint = new Vector2(16f, 12f);
                spec.height = 7.2f;
                spec.fenced = true;
                spec.garden = true;
                spec.annex = true;
                break;
        }

        return spec;
    }

    private bool CanPlaceHouse(WorldLayout layout, Vector3 pos, float minDistance)
    {
        for (int i = 0; i < layout.houses.Count; i++)
        {
            if (Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(layout.houses[i].position.x, layout.houses[i].position.z)) < minDistance)
                return false;
        }
        return true;
    }

    private TerrainGrid CreateTerrainGrid(BuildContext ctx, WorldLayout layout, Transform parent)
    {
        TerrainGrid grid = new TerrainGrid();
        grid.tileCount = layout.tileCount;
        grid.tileSize = layout.tileSizeMeters;
        grid.terrains = new Terrain[layout.tileCount, layout.tileCount];

        int heightRes = layout.tileCount <= 10 ? 1025 : (layout.tileCount <= 18 ? 513 : 257);
        int alphaRes = layout.tileCount <= 12 ? 512 : (layout.tileCount <= 18 ? 256 : 128);

        for (int z = 0; z < layout.tileCount; z++)
        {
            for (int x = 0; x < layout.tileCount; x++)
            {
                TerrainData td = new TerrainData();
                td.heightmapResolution = heightRes;
                td.alphamapResolution = alphaRes;
                td.baseMapResolution = 1024;
                td.SetDetailResolution(layout.tileCount <= 12 ? Mathf.RoundToInt(192 * qualityBoost) : Mathf.RoundToInt(96 * qualityBoost), 16);
                td.size = new Vector3(layout.tileSizeMeters, layout.terrainHeightMeters, layout.tileSizeMeters);
                td.terrainLayers = new[] { ctx.grassLayer, ctx.wheatLayer, ctx.dirtLayer, ctx.forestLayer };
                td.detailPrototypes = new[] { CreateGrassDetailPrototype(ctx), CreateWheatDetailPrototype(ctx) };

                float[,] heights = GenerateHeightsForTile(layout, x, z, heightRes);
                td.SetHeights(0, 0, heights);

                string tdPath = ctx.terrainFolder + $"/TerrainData_{x}_{z}.asset";
                if (AssetDatabase.LoadAssetAtPath<TerrainData>(tdPath) != null)
                    AssetDatabase.DeleteAsset(tdPath);
                AssetDatabase.CreateAsset(td, tdPath);

                GameObject terrainGO = Terrain.CreateTerrainGameObject(td);
                terrainGO.name = $"Terrain_{x}_{z}";
                terrainGO.transform.SetParent(parent);
                terrainGO.transform.position = new Vector3(x * layout.tileSizeMeters, 0f, z * layout.tileSizeMeters);

                Terrain terrain = terrainGO.GetComponent<Terrain>();
                terrain.drawInstanced = true;
                terrain.heightmapPixelError = Mathf.Lerp(2.5f, 1.1f, Mathf.InverseLerp(1f, 3f, qualityBoost));
                terrain.basemapDistance = 9000f;
                terrain.shadowCastingMode = ShadowCastingMode.On;
                terrain.materialTemplate = ctx.terrainMat;
                terrain.detailObjectDistance = 220f + qualityBoost * 90f;
                terrain.detailObjectDensity = Mathf.Lerp(1f, 1.7f, Mathf.InverseLerp(1f, 3f, qualityBoost));
                terrain.treeDistance = 0f;
                grid.terrains[x, z] = terrain;
            }
        }

        for (int z = 0; z < layout.tileCount; z++)
        {
            for (int x = 0; x < layout.tileCount; x++)
            {
                Terrain left = x > 0 ? grid.terrains[x - 1, z] : null;
                Terrain right = x < layout.tileCount - 1 ? grid.terrains[x + 1, z] : null;
                Terrain top = z > 0 ? grid.terrains[x, z - 1] : null;
                Terrain bottom = z < layout.tileCount - 1 ? grid.terrains[x, z + 1] : null;
                grid.terrains[x, z].SetNeighbors(left, top, right, bottom);
            }
        }

        return grid;
    }

    private float[,] GenerateHeightsForTile(WorldLayout layout, int tileX, int tileZ, int resolution)
    {
        float[,] heights = new float[resolution, resolution];
        float worldX0 = tileX * layout.tileSizeMeters;
        float worldZ0 = tileZ * layout.tileSizeMeters;

        for (int z = 0; z < resolution; z++)
        {
            float nz = z / (float)(resolution - 1);
            float worldZ = worldZ0 + nz * layout.tileSizeMeters;
            for (int x = 0; x < resolution; x++)
            {
                float nx = x / (float)(resolution - 1);
                float worldX = worldX0 + nx * layout.tileSizeMeters;
                float meters = EvaluateTerrainHeightMeters(layout, worldX, worldZ);
                heights[z, x] = Mathf.Clamp01(meters / layout.terrainHeightMeters);
            }
        }

        return heights;
    }

    private float EvaluateTerrainHeightMeters(WorldLayout layout, float worldX, float worldZ)
    {
        float broad = 16f + (FBM(worldX * 0.00045f + seed * 0.011f, worldZ * 0.00045f + 9.1f, 4, 0.5f, 2f) - 0.5f) * 20f;
        float detail = (FBM(worldX * 0.00165f + 54f, worldZ * 0.00165f + 18f, 3, 0.5f, 2.1f) - 0.5f) * 5f;
        float micro = (Mathf.PerlinNoise(worldX * 0.0041f + 103f, worldZ * 0.0041f + 41f) - 0.5f) * 1.8f;
        float forestRise = ComputeForestMask(layout, worldX, worldZ) * (10f + FBM(worldX * 0.0009f, worldZ * 0.0009f, 2, 0.5f, 2f) * 16f);

        float h = broad + detail + micro + forestRise;

        float lakeMask = ComputeLakeMask(layout, worldX, worldZ);
        if (lakeMask > 0.001f)
        {
            float lakeBed = layout.waterLevel - Mathf.Lerp(2.8f, 6.5f, lakeMask);
            h = Mathf.Lerp(h, lakeBed, lakeMask);
        }

        float riverMask = ComputeRiverMask(layout, worldX, worldZ);
        if (riverMask > 0.001f)
        {
            float riverBed = layout.waterLevel - Mathf.Lerp(1.0f, 3.0f, riverMask);
            h = Mathf.Lerp(h, riverBed, riverMask * 0.95f);
        }

        float villageMask = ComputeVillageMask(layout, worldX, worldZ);
        if (villageMask > 0.001f)
        {
            float villageBase = 15f + (Mathf.PerlinNoise(worldX * 0.00035f + 12f, worldZ * 0.00035f + 31f) - 0.5f) * 2.5f;
            h = Mathf.Lerp(h, villageBase, villageMask * 0.92f);
        }

        float roadMask = 0f;
        for (int i = 0; i < layout.roads.Count; i++)
        {
            float d = DistancePointPolylineXZ(new Vector3(worldX, 0f, worldZ), layout.roads[i]);
            float influence = layout.roads[i].width * 2.1f;
            if (d < influence)
            {
                float t = 1f - Mathf.Clamp01(d / influence);
                roadMask = Mathf.Max(roadMask, t);
            }
        }
        if (roadMask > 0.001f)
        {
            float roadBase = 15f + (Mathf.PerlinNoise(worldX * 0.00022f + 5f, worldZ * 0.00022f + 7f) - 0.5f) * 1.4f;
            h = Mathf.Lerp(h, roadBase, roadMask * 0.9f);
        }

        for (int i = 0; i < layout.houses.Count; i++)
        {
            HouseSpec house = layout.houses[i];
            float radius = Mathf.Max(house.footprint.x, house.footprint.y) * 0.95f;
            float d = Vector2.Distance(new Vector2(worldX, worldZ), new Vector2(house.position.x, house.position.z));
            if (d < radius)
            {
                float t = 1f - Mathf.Clamp01(d / radius);
                float padBase = 15f + (Mathf.PerlinNoise(house.position.x * 0.0003f, house.position.z * 0.0003f) - 0.5f) * 1.2f;
                h = Mathf.Lerp(h, padBase, t * 0.95f);
            }
        }

        float shoreSoftening = Mathf.Max(lakeMask * 0.75f, riverMask * 0.72f);
        if (shoreSoftening > 0.001f)
        {
            float shoreBase = layout.waterLevel + 1.0f + (Mathf.PerlinNoise(worldX * 0.0012f + 9f, worldZ * 0.0012f + 17f) - 0.5f) * 0.9f;
            h = Mathf.Lerp(h, shoreBase, shoreSoftening * 0.45f);
        }

        float fieldSoftening = ComputeFieldSofteningMask(layout, worldX, worldZ);
        if (fieldSoftening > 0.001f)
        {
            float fieldBase = 15f + (FBM(worldX * 0.00028f + 82f, worldZ * 0.00028f + 12f, 2, 0.5f, 2f) - 0.5f) * 3f;
            h = Mathf.Lerp(h, fieldBase, fieldSoftening * 0.28f);
        }

        return Mathf.Clamp(h, 4f, layout.terrainHeightMeters * 0.72f);
    }

    private void PaintTerrainGrid(BuildContext ctx, TerrainGrid grid, WorldLayout layout)
    {
        for (int tz = 0; tz < grid.tileCount; tz++)
        {
            for (int tx = 0; tx < grid.tileCount; tx++)
            {
                Terrain terrain = grid.terrains[tx, tz];
                TerrainData td = terrain.terrainData;
                int w = td.alphamapWidth;
                int h = td.alphamapHeight;
                float[,,] map = new float[h, w, 4];
                float x0 = tx * grid.tileSize;
                float z0 = tz * grid.tileSize;

                for (int y = 0; y < h; y++)
                {
                    float nz = y / (float)(h - 1);
                    float wz = z0 + nz * grid.tileSize;
                    for (int x = 0; x < w; x++)
                    {
                        float nx = x / (float)(w - 1);
                        float wx = x0 + nx * grid.tileSize;
                        float forest = ComputeForestMask(layout, wx, wz);
                        float village = ComputeVillageMask(layout, wx, wz);
                        float road = ComputeRoadMask(layout, wx, wz);
                        float house = ComputeHouseMask(layout, wx, wz);
                        float lake = ComputeLakeMask(layout, wx, wz);
                        float river = ComputeRiverMask(layout, wx, wz);
                        float waterEdge = Mathf.Max(lake, river);
                        float fieldBorder;
                        float wheat = ComputeWheatFieldMask(layout, wx, wz, out fieldBorder);

                        float grass = 1f;
                        float dirt = Mathf.Max(road * 0.98f, house * 0.55f);
                        dirt = Mathf.Max(dirt, fieldBorder * 0.38f * (1f - forest));
                        dirt = Mathf.Max(dirt, village * 0.10f);
                        dirt = Mathf.Max(dirt, waterEdge * 0.42f);
                        float wheatLayer = wheat * (1f - road) * (1f - house) * (1f - forest) * (1f - waterEdge);
                        float forestLayer = forest * (1f - road * 0.7f) * (1f - lake);

                        grass *= 1f - dirt * 0.75f;
                        grass *= 1f - wheatLayer * 0.72f;
                        grass *= 1f - forestLayer * 0.86f;
                        grass *= 1f - waterEdge * 0.45f;
                        grass = Mathf.Max(grass, village * 0.14f);

                        float sum = grass + wheatLayer + dirt + forestLayer;
                        if (sum < 0.0001f)
                        {
                            grass = 1f;
                            sum = 1f;
                        }

                        map[y, x, 0] = grass / sum;
                        map[y, x, 1] = wheatLayer / sum;
                        map[y, x, 2] = dirt / sum;
                        map[y, x, 3] = forestLayer / sum;
                    }
                }

                td.SetAlphamaps(0, 0, map);
            }
        }
    }

    private void PopulateTerrainDetails(BuildContext ctx, TerrainGrid grid, WorldLayout layout)
    {
        for (int tz = 0; tz < grid.tileCount; tz++)
        {
            for (int tx = 0; tx < grid.tileCount; tx++)
            {
                Terrain terrain = grid.terrains[tx, tz];
                TerrainData td = terrain.terrainData;
                int res = td.detailWidth;
                int[,] grass = new int[res, res];
                int[,] wheat = new int[res, res];
                float x0 = tx * grid.tileSize;
                float z0 = tz * grid.tileSize;

                for (int z = 0; z < res; z++)
                {
                    float wz = z0 + (z / (float)(res - 1)) * grid.tileSize;
                    for (int x = 0; x < res; x++)
                    {
                        float wx = x0 + (x / (float)(res - 1)) * grid.tileSize;
                        float forest = ComputeForestMask(layout, wx, wz);
                        float village = ComputeVillageMask(layout, wx, wz);
                        float road = ComputeRoadMask(layout, wx, wz);
                        float house = ComputeHouseMask(layout, wx, wz);
                        float lake = ComputeLakeMask(layout, wx, wz);
                        float river = ComputeRiverMask(layout, wx, wz);
                        float water = Mathf.Max(lake, river);
                        if (forest > 0.20f || road > 0.08f || house > 0.10f || water > 0.04f)
                            continue;

                        float border;
                        float wheatMask = ComputeWheatFieldMask(layout, wx, wz, out border);
                        float localNoise = Hash01(Mathf.FloorToInt(wx * 0.5f), Mathf.FloorToInt(wz * 0.5f), seed + 801);

                        float nearVillage = Vector2.Distance(new Vector2(wx, wz), new Vector2(layout.villageCenter.x, layout.villageCenter.z));
                        if (wheatMask > 0.52f)
                        {
                            float densityMul = nearVillage < 950f ? 0.35f : 1f;
                            wheat[z, x] = Mathf.RoundToInt(Mathf.Lerp(3f, 10f + qualityBoost * 2f, wheatMask) * densityMul * (0.7f + localNoise * 0.6f));
                        }
                        else
                        {
                            float meadow = Mathf.Max(0.16f, 1f - village * 0.55f) * (1f - border * 0.65f);
                            grass[z, x] = Mathf.RoundToInt(Mathf.Lerp(2f, 8f + qualityBoost * 2f, meadow) * (0.7f + localNoise * 0.6f));
                        }
                    }
                }

                td.SetDetailLayer(0, 0, 0, grass);
                td.SetDetailLayer(0, 0, 1, wheat);
            }
        }
    }

    private void CreateWaterSystem(BuildContext ctx, TerrainGrid grid, WorldLayout layout, Transform parent)
    {
        GameObject waterRoot = new GameObject("Water");
        waterRoot.transform.SetParent(parent);
        CreateLakeMesh(ctx, grid, layout, waterRoot.transform);
        CreateRiverMesh(ctx, grid, layout, waterRoot.transform);
    }

    private void CreateLakeMesh(BuildContext ctx, TerrainGrid grid, WorldLayout layout, Transform parent)
    {
        GameObject lake = new GameObject("Lake", typeof(MeshFilter), typeof(MeshRenderer));
        lake.transform.SetParent(parent);
        lake.transform.position = new Vector3(layout.lakeCenter.x, layout.waterLevel, layout.lakeCenter.z);
        lake.GetComponent<MeshFilter>().sharedMesh = CreateDiscMesh(48, 1f);
        lake.GetComponent<MeshRenderer>().sharedMaterial = ctx.waterMat;
        lake.transform.localScale = new Vector3(layout.lakeRadiusX * 2f, 1f, layout.lakeRadiusZ * 2f);
    }

    private void CreateRiverMesh(BuildContext ctx, TerrainGrid grid, WorldLayout layout, Transform parent)
    {
        GameObject riverRoot = new GameObject("River");
        riverRoot.transform.SetParent(parent);
        for (int i = 0; i < layout.riverPoints.Count - 1; i++)
        {
            Vector3 a = layout.riverPoints[i];
            Vector3 b = layout.riverPoints[i + 1];
            float len = Vector3.Distance(a, b);
            int pieces = Mathf.Max(3, Mathf.CeilToInt(len / Mathf.Lerp(16f, 8f, Mathf.InverseLerp(1f, 3f, qualityBoost))));
            for (int p = 0; p < pieces; p++)
            {
                float t0 = p / (float)pieces;
                float t1 = (p + 1) / (float)pieces;
                Vector3 p0 = Vector3.Lerp(a, b, t0);
                Vector3 p1 = Vector3.Lerp(a, b, t1);
                Vector3 mid = (p0 + p1) * 0.5f;
                mid.y = layout.waterLevel + 0.03f;
                float segLen = Vector3.Distance(p0, p1);
                float wobble = 0.88f + Mathf.PerlinNoise(mid.x * 0.004f, mid.z * 0.004f) * 0.34f;
                float width = layout.riverWidth * Mathf.Lerp(1.18f, 0.86f, i / Mathf.Max(1f, layout.riverPoints.Count - 2f)) * wobble;
                CreateCubeChild(riverRoot.transform, "RiverSegment", mid, Quaternion.LookRotation((p1 - p0).normalized, Vector3.up), new Vector3(width, 0.05f, segLen + 0.25f), ctx.waterMat);
            }
        }
    }

    private void CreateRoadMeshes(BuildContext ctx, TerrainGrid grid, WorldLayout layout, Transform parent)
    {
        for (int i = 0; i < layout.roads.Count; i++)
        {
            CreateRoadPathMeshes(ctx, grid, parent, layout.roads[i]);
        }
    }

    private void CreateRoadPathMeshes(BuildContext ctx, TerrainGrid grid, Transform parent, RoadPath road)
    {
        GameObject roadRoot = new GameObject(road.name);
        roadRoot.transform.SetParent(parent);

        for (int seg = 0; seg < road.points.Count - 1; seg++)
        {
            Vector3 a = road.points[seg];
            Vector3 b = road.points[seg + 1];
            float length = Vector3.Distance(a, b);
            int pieces = Mathf.Max(3, Mathf.CeilToInt(length / Mathf.Lerp(8f, 4.5f, Mathf.InverseLerp(1f, 3f, qualityBoost))));

            for (int i = 0; i < pieces; i++)
            {
                float t0 = i / (float)pieces;
                float t1 = (i + 1) / (float)pieces;
                Vector3 p0 = Vector3.Lerp(a, b, t0);
                Vector3 p1 = Vector3.Lerp(a, b, t1);
                Vector3 mid = (p0 + p1) * 0.5f;
                float segLen = Vector3.Distance(p0, p1);
                mid.y = SampleTerrainHeight(grid, mid) + 0.04f;

                Quaternion rot = Quaternion.LookRotation((p1 - p0).normalized, Vector3.up);
                CreateCubeChild(roadRoot.transform, "Road", mid, rot, new Vector3(road.width, 0.12f, segLen + 0.18f), ctx.roadMat);

                CreateCubeChild(roadRoot.transform, "RutL", mid + (Vector3.Cross(Vector3.up, (p1 - p0).normalized).normalized * (road.width * 0.16f)), rot, new Vector3(road.width * 0.14f, 0.04f, segLen + 0.06f), ctx.dirtMat);
                CreateCubeChild(roadRoot.transform, "RutR", mid - (Vector3.Cross(Vector3.up, (p1 - p0).normalized).normalized * (road.width * 0.16f)), rot, new Vector3(road.width * 0.14f, 0.04f, segLen + 0.06f), ctx.dirtMat);

                if (road.hasStoneShoulders)
                {
                    Vector3 side = Vector3.Cross(Vector3.up, (p1 - p0).normalized).normalized;
                    Vector3 left = mid + side * (road.width * 0.52f);
                    Vector3 right = mid - side * (road.width * 0.52f);
                    left.y = SampleTerrainHeight(grid, left) + 0.06f;
                    right.y = SampleTerrainHeight(grid, right) + 0.06f;
                    CreateCubeChild(roadRoot.transform, "ShoulderL", left, rot, new Vector3(0.24f, 0.16f, segLen + 0.2f), ctx.shoulderMat);
                    CreateCubeChild(roadRoot.transform, "ShoulderR", right, rot, new Vector3(0.24f, 0.16f, segLen + 0.2f), ctx.shoulderMat);
                }
            }
        }

        MarkStaticRecursive(roadRoot);
    }

    private void CreateBridge(BuildContext ctx, TerrainGrid grid, WorldLayout layout, Transform parent)
    {
        if (layout.riverPoints.Count < 3)
            return;

        Vector3 a = layout.riverPoints[1];
        Vector3 b = layout.riverPoints[2];
        Vector3 center = Vector3.Lerp(a, b, 0.52f);
        Vector3 tangent = (b - a).normalized;
        Vector3 cross = Vector3.Cross(Vector3.up, tangent).normalized;
        center.y = layout.waterLevel + 1.45f;

        GameObject bridge = new GameObject("VillageBridge");
        bridge.transform.SetParent(parent);
        bridge.transform.position = center;
        bridge.transform.rotation = Quaternion.LookRotation(cross, Vector3.up);

        float deckWidth = 5.2f;
        float deckLength = 18f;
        CreateCubeChild(bridge.transform, "Deck", new Vector3(0f, 0f, 0f), new Vector3(deckWidth, 0.32f, deckLength), ctx.timberMat);
        CreateCubeChild(bridge.transform, "RailL", new Vector3(-deckWidth * 0.43f, 0.75f, 0f), new Vector3(0.18f, 0.22f, deckLength), ctx.timberMat);
        CreateCubeChild(bridge.transform, "RailR", new Vector3(deckWidth * 0.43f, 0.75f, 0f), new Vector3(0.18f, 0.22f, deckLength), ctx.timberMat);
        for (int i = 0; i < 4; i++)
        {
            float z = -deckLength * 0.34f + i * (deckLength * 0.23f);
            CreateCubeChild(bridge.transform, "PostL_" + i, new Vector3(-deckWidth * 0.43f, 0.42f, z), new Vector3(0.16f, 0.84f, 0.16f), ctx.timberMat);
            CreateCubeChild(bridge.transform, "PostR_" + i, new Vector3(deckWidth * 0.43f, 0.42f, z), new Vector3(0.16f, 0.84f, 0.16f), ctx.timberMat);
        }
        CreateCubeChild(bridge.transform, "SupportA", new Vector3(0f, -1.0f, -deckLength * 0.26f), new Vector3(0.6f, 2.0f, 0.6f), ctx.stoneMat);
        CreateCubeChild(bridge.transform, "SupportB", new Vector3(0f, -1.0f, deckLength * 0.26f), new Vector3(0.6f, 2.0f, 0.6f), ctx.stoneMat);

        MarkStaticRecursive(bridge);
    }

    private void CreateStreetFences(BuildContext ctx, TerrainGrid grid, WorldLayout layout, Transform parent)
    {
        GameObject root = new GameObject("StreetFences");
        root.transform.SetParent(parent);

        for (int r = 0; r < layout.roads.Count; r++)
        {
            RoadPath road = layout.roads[r];
            if (!(road.name == "MainStreet" || road.name == "CrossRoad" || road.name == "NorthLane" || road.name == "SouthLane"))
                continue;

            float length = GetPathLength(road);
            float spacing = 9f;
            float offset = road.width * 0.82f + 8.5f;
            for (float d = 12f; d < length - 12f; d += spacing)
            {
                Vector3 p = SamplePath(road, d);
                Vector3 dir = DirectionOnPath(road, d);
                Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;
                for (int s = -1; s <= 1; s += 2)
                {
                    Vector3 postPos = p + side * offset * s;
                    if (ComputeVillageMask(layout, postPos.x, postPos.z) < 0.12f || ComputeRiverMask(layout, postPos.x, postPos.z) > 0.05f)
                        continue;
                    postPos.y = SampleTerrainHeight(grid, postPos);
                    GameObject post = new GameObject("StreetFencePost");
                    post.transform.SetParent(root.transform);
                    post.transform.position = postPos;
                    post.transform.rotation = Quaternion.LookRotation(dir);
                    CreateCubeChild(post.transform, "Post", new Vector3(0f, 0.58f, 0f), new Vector3(0.12f, 1.16f, 0.12f), ctx.timberMat);
                    CreateCubeChild(post.transform, "RailA", new Vector3(0f, 0.74f, 0f), new Vector3(0.10f, 0.08f, spacing * 1.02f), ctx.timberMat);
                    CreateCubeChild(post.transform, "RailB", new Vector3(0f, 0.38f, 0f), new Vector3(0.10f, 0.08f, spacing * 1.02f), ctx.timberMat);
                }
            }
        }
        MarkStaticRecursive(root);
    }

    private void CreateVillageGreenery(BuildContext ctx, TerrainGrid grid, WorldLayout layout, Transform parent)
    {
        GameObject root = new GameObject("VillageGreenery");
        root.transform.SetParent(parent);

        for (int i = 0; i < Mathf.RoundToInt(28f * qualityBoost); i++)
        {
            Vector3 pos = layout.villageCenter + new Vector3(UnityEngine.Random.Range(-layout.villageLengthMeters * 0.50f, layout.villageLengthMeters * 0.50f), 0f, UnityEngine.Random.Range(-layout.villageHalfWidthMeters * 1.10f, layout.villageHalfWidthMeters * 1.10f));
            if (ComputeHouseMask(layout, pos.x, pos.z) > 0.08f || ComputeRoadMask(layout, pos.x, pos.z) > 0.10f || ComputeRiverMask(layout, pos.x, pos.z) > 0.06f || ComputeLakeMask(layout, pos.x, pos.z) > 0.04f)
                continue;
            pos.y = SampleTerrainHeight(grid, pos);
            GameObject tree = new GameObject("VillageTree_" + i);
            tree.transform.SetParent(root.transform);
            tree.transform.position = pos;
            tree.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.58f, 0.92f);
            BuildBroadleafTree(ctx, tree.transform);
        }

        for (int i = 0; i < Mathf.RoundToInt(12f * qualityBoost); i++)
        {
            Vector3 pos = layout.villageCenter + new Vector3(UnityEngine.Random.Range(-layout.villageLengthMeters * 0.42f, layout.villageLengthMeters * 0.42f), 0f, UnityEngine.Random.Range(-layout.villageHalfWidthMeters * 0.92f, layout.villageHalfWidthMeters * 0.92f));
            if (ComputeHouseMask(layout, pos.x, pos.z) > 0.08f || ComputeRoadMask(layout, pos.x, pos.z) > 0.10f || ComputeRiverMask(layout, pos.x, pos.z) > 0.06f)
                continue;
            pos.y = SampleTerrainHeight(grid, pos);
            GameObject orchard = new GameObject("OrchardCluster_" + i);
            orchard.transform.SetParent(root.transform);
            orchard.transform.position = pos;
            for (int t = 0; t < 3; t++)
            {
                GameObject tree = new GameObject("Tree_" + t);
                tree.transform.SetParent(orchard.transform, false);
                tree.transform.localPosition = new Vector3(UnityEngine.Random.Range(-6f, 6f), 0f, UnityEngine.Random.Range(-6f, 6f));
                tree.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.48f, 0.68f);
                BuildBroadleafTree(ctx, tree.transform);
            }
        }

        MarkStaticRecursive(root);
    }

    private void CreateWaterVegetation(BuildContext ctx, TerrainGrid grid, WorldLayout layout, Transform parent)
    {
        GameObject root = new GameObject("WaterVegetation");
        root.transform.SetParent(parent);

        for (int i = 0; i < Mathf.RoundToInt(180f * qualityBoost); i++)
        {
            float angle = Hash01(i, 1, seed + 901) * Mathf.PI * 2f;
            float radius = Mathf.Lerp(layout.lakeRadiusX * 0.78f, layout.lakeRadiusX * 1.08f, Hash01(i, 2, seed + 911));
            Vector3 pos = layout.lakeCenter + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius * (layout.lakeRadiusZ / layout.lakeRadiusX));
            if (ComputeLakeMask(layout, pos.x, pos.z) < 0.10f || ComputeLakeMask(layout, pos.x, pos.z) > 0.48f)
                continue;
            pos.y = SampleTerrainHeight(grid, pos);
            CreateReedCluster(ctx, root.transform, pos, Hash01(i, 3, seed + 921));
        }

        float riverLengthApprox = 0f;
        for (int i = 0; i < layout.riverPoints.Count - 1; i++)
            riverLengthApprox += Vector3.Distance(layout.riverPoints[i], layout.riverPoints[i + 1]);
        for (float d = 12f; d < riverLengthApprox; d += Mathf.Lerp(18f, 10f, Mathf.InverseLerp(1f, 3f, qualityBoost)))
        {
            Vector3 p = SamplePolyline(layout.riverPoints, d);
            Vector3 dir = DirectionOnPolyline(layout.riverPoints, d);
            Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 pos = p + side * (layout.riverWidth * (0.75f + Hash01((int)d, s, seed + 931) * 0.55f) * s);
                if (ComputeRiverMask(layout, pos.x, pos.z) > 0.42f)
                    continue;
                pos.y = SampleTerrainHeight(grid, pos);
                CreateReedCluster(ctx, root.transform, pos, Hash01((int)d, s, seed + 941));
            }
        }

        MarkStaticRecursive(root);
    }

    private void CreateReedCluster(BuildContext ctx, Transform parent, Vector3 worldPos, float variant)
    {
        GameObject root = new GameObject("Reed");
        root.transform.SetParent(parent);
        root.transform.position = worldPos;
        root.transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.25f, variant);
        int dockSections = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(5f, 8f, Mathf.InverseLerp(1f, 3f, qualityBoost))), 5, 8);
        for (int i = 0; i < dockSections; i++)
        {
            float a = i * 72f + variant * 20f;
            CreateMeshChild(root.transform, "Blade_" + i, ctx.wheatBladeMesh, new Vector3((i - 2) * 0.05f, 0f, 0f), new Vector3(0.7f, 1.1f + i * 0.06f, 0.7f), ctx.grassBladeMat).transform.localRotation = Quaternion.Euler(0f, a, 0f);
        }
    }

    private void CreateRoadsideProps(BuildContext ctx, TerrainGrid grid, WorldLayout layout, Transform parent)
    {
        GameObject polesRoot = new GameObject("TelegraphPoles");
        polesRoot.transform.SetParent(parent);
        GameObject treesRoot = new GameObject("RoadsideTrees");
        treesRoot.transform.SetParent(parent);

        for (int r = 0; r < layout.roads.Count; r++)
        {
            RoadPath road = layout.roads[r];
            float length = GetPathLength(road);
            float spacing = road.mainRoad ? Mathf.Lerp(44f, 26f, Mathf.InverseLerp(1f, 3f, qualityBoost)) : (road.name.StartsWith("Lane") ? 30f : 42f);
            float sideOffset = road.width * 0.72f + (road.mainRoad ? 3.4f : 2.2f);

            for (float d = spacing; d < length - 10f; d += spacing)
            {
                Vector3 p = SamplePath(road, d);
                Vector3 dir = DirectionOnPath(road, d);
                Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;
                bool left = Hash01(r, Mathf.RoundToInt(d), seed + 501) > 0.5f;
                Vector3 place = p + side * sideOffset * (left ? 1f : -1f);
                place.y = SampleTerrainHeight(grid, place);

                GameObject pole = new GameObject($"Pole_{r}_{Mathf.RoundToInt(d)}");
                pole.transform.SetParent(polesRoot.transform);
                pole.transform.position = place;
                pole.transform.rotation = Quaternion.LookRotation(dir);
                CreateCylinderChild(pole.transform, "Post", new Vector3(0f, 3.2f, 0f), new Vector3(0.12f, 3.2f, 0.12f), ctx.timberMat);
                CreateCubeChild(pole.transform, "Cross", new Vector3(0f, 6.0f, 0f), new Vector3(1.1f, 0.08f, 0.08f), ctx.timberMat);

                if (road.name.StartsWith("Lane") && Hash01(r, Mathf.RoundToInt(d), seed + 551) > 0.62f)
                {
                    Vector3 treePos = p - side * sideOffset * 1.55f;
                    treePos.y = SampleTerrainHeight(grid, treePos);
                    GameObject tree = new GameObject($"RoadTree_{r}_{Mathf.RoundToInt(d)}");
                    tree.transform.SetParent(treesRoot.transform);
                    tree.transform.position = treePos;
                    tree.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.68f, 0.92f);
                    BuildBroadleafTree(ctx, tree.transform);
                }

                if (road.mainRoad && Hash01(r, Mathf.RoundToInt(d), seed + 561) > 0.82f)
                {
                    GameObject sign = new GameObject($"Sign_{r}_{Mathf.RoundToInt(d)}");
                    sign.transform.SetParent(polesRoot.transform);
                    sign.transform.position = place + dir * 2.2f;
                    sign.transform.rotation = Quaternion.LookRotation(dir);
                    CreateCubeChild(sign.transform, "Post", new Vector3(0f, 0.8f, 0f), new Vector3(0.10f, 1.6f, 0.10f), ctx.timberMat);
                    CreateCubeChild(sign.transform, "Board", new Vector3(0.35f, 1.55f, 0f), new Vector3(0.70f, 0.34f, 0.08f), ctx.wallCreamMat);
                }
            }
        }

        MarkStaticRecursive(polesRoot);
        MarkStaticRecursive(treesRoot);
    }

    private void CreateVillageStreetProps(BuildContext ctx, TerrainGrid grid, WorldLayout layout, Transform parent)
    {
        GameObject root = new GameObject("StreetProps");
        root.transform.SetParent(parent);

        Vector3 commons = layout.villageCenter + new Vector3(layout.villageLengthMeters * 0.04f, 0f, 0f);
        commons.y = SampleTerrainHeight(grid, commons);
        CreateCylinderChild(root.transform, "WellRing", commons + new Vector3(0f, 0.42f, 0f), new Vector3(2.2f, 0.42f, 2.2f), ctx.stoneMat);
        CreateCubeChild(root.transform, "WellPostL", commons + new Vector3(-1.1f, 2.0f, 0f), new Vector3(0.16f, 2.8f, 0.16f), ctx.timberMat);
        CreateCubeChild(root.transform, "WellPostR", commons + new Vector3(1.1f, 2.0f, 0f), new Vector3(0.16f, 2.8f, 0.16f), ctx.timberMat);
        CreateMeshChild(root.transform, "WellRoof", ctx.roofMesh, commons + new Vector3(0f, 3.1f, 0f), new Vector3(2.8f, 1.2f, 2.2f), ctx.roofDarkMat);

        int benchCount = Mathf.RoundToInt(Mathf.Lerp(4f, 8f, Mathf.InverseLerp(1f, 3f, qualityBoost)));
        for (int i = 0; i < benchCount; i++)
        {
            float ang = i / (float)benchCount * Mathf.PI * 2f;
            Vector3 p = layout.villageCenter + new Vector3(Mathf.Cos(ang) * 18f, 0f, Mathf.Sin(ang) * 14f);
            p.y = SampleTerrainHeight(grid, p);
            Quaternion benchRot = Quaternion.Euler(0f, ang * Mathf.Rad2Deg + 90f, 0f);
            CreateCubeChild(root.transform, "BenchSeat_" + i, p + new Vector3(0f, 0.42f, 0f), benchRot, new Vector3(1.7f, 0.14f, 0.42f), ctx.timberMat);
            CreateCubeChild(root.transform, "BenchLegA_" + i, p + new Vector3(-0.60f, 0.22f, 0f), benchRot, new Vector3(0.12f, 0.44f, 0.12f), ctx.timberMat);
            CreateCubeChild(root.transform, "BenchLegB_" + i, p + new Vector3(0.60f, 0.22f, 0f), benchRot, new Vector3(0.12f, 0.44f, 0.12f), ctx.timberMat);
        }

        int cartCount = Mathf.RoundToInt(Mathf.Lerp(3f, 6f, Mathf.InverseLerp(1f, 3f, qualityBoost)));
        for (int i = 0; i < cartCount; i++)
        {
            Vector3 p = layout.villageCenter + new Vector3(-10f + i * 9f, 0f, -18f + i * 3f);
            p.y = SampleTerrainHeight(grid, p);
            CreateCubeChild(root.transform, "CartBase_" + i, p + new Vector3(0f, 0.48f, 0f), Quaternion.Euler(0f, 20f + i * 40f, 0f), new Vector3(2.0f, 0.38f, 1.2f), ctx.timberMat);
            CreateCylinderChild(root.transform, "WheelA_" + i, p + new Vector3(0.9f, 0.34f, 0.62f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.36f, 0.12f, 0.36f), ctx.stoneMat);
            CreateCylinderChild(root.transform, "WheelB_" + i, p + new Vector3(-0.9f, 0.34f, 0.62f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.36f, 0.12f, 0.36f), ctx.stoneMat);
        }
        for (int i = 0; i < Mathf.RoundToInt(Mathf.Lerp(4f, 8f, Mathf.InverseLerp(1f, 3f, qualityBoost))); i++)
        {
            float angLamp = i / Mathf.Max(1f, Mathf.RoundToInt(Mathf.Lerp(4f, 8f, Mathf.InverseLerp(1f, 3f, qualityBoost)))) * Mathf.PI * 2f;
            Vector3 p = layout.villageCenter + new Vector3(Mathf.Cos(angLamp) * 24f, 0f, Mathf.Sin(angLamp) * 20f);
            p.y = SampleTerrainHeight(grid, p);
            CreateCylinderChild(root.transform, "LampPost_" + i, p + new Vector3(0f, 1.8f, 0f), new Vector3(0.11f, 1.8f, 0.11f), ctx.timberMat);
            CreateSphereChild(root.transform, "LampGlow_" + i, p + new Vector3(0f, 3.4f, 0f), Vector3.one * 0.28f, ctx.glassMat);
        }
        MarkStaticRecursive(root);
    }

    private void CreateLakeShoreProps(BuildContext ctx, TerrainGrid grid, WorldLayout layout, Transform parent)
    {
        GameObject root = new GameObject("LakeShoreProps");
        root.transform.SetParent(parent);

        Vector3 dockPos = layout.lakeCenter + new Vector3(layout.lakeRadiusX * 0.24f, 0f, layout.lakeRadiusZ * 0.82f);
        dockPos.y = layout.waterLevel + 0.35f;
        GameObject dock = new GameObject("Dock");
        dock.transform.SetParent(root.transform);
        dock.transform.position = dockPos;
        dock.transform.rotation = Quaternion.Euler(0f, 18f, 0f);
        int dockSections = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(5f, 9f, Mathf.InverseLerp(1f, 3f, qualityBoost))), 5, 9);
        for (int i = 0; i < dockSections; i++)
        {
            float z = i * 2.1f;
            CreateCubeChild(dock.transform, "Deck_" + i, new Vector3(0f, 0f, z), new Vector3(2.4f, 0.16f, 1.8f), ctx.timberMat);
            CreateCubeChild(dock.transform, "PostL_" + i, new Vector3(-0.9f, -0.65f, z), new Vector3(0.16f, 1.5f, 0.16f), ctx.timberMat);
            CreateCubeChild(dock.transform, "PostR_" + i, new Vector3(0.9f, -0.65f, z), new Vector3(0.16f, 1.5f, 0.16f), ctx.timberMat);
        }
        CreateCubeChild(dock.transform, "Boat", new Vector3(2.6f, -0.08f, 6.8f), Quaternion.Euler(0f, 28f, 0f), new Vector3(1.2f, 0.26f, 3.4f), ctx.timberMat);

        int shorelineRockCount = Mathf.RoundToInt(Mathf.Lerp(20f, 36f, Mathf.InverseLerp(1f, 3f, qualityBoost)));
        for (int i = 0; i < shorelineRockCount; i++)
        {
            float a = i / (float)shorelineRockCount * Mathf.PI * 2f;
            Vector3 p = layout.lakeCenter + new Vector3(Mathf.Cos(a) * layout.lakeRadiusX * 1.04f, 0f, Mathf.Sin(a) * layout.lakeRadiusZ * 1.02f);
            p.y = SampleTerrainHeight(grid, p) + 0.2f;
            CreateSphereChild(root.transform, "Rock_" + i, p, Vector3.one * UnityEngine.Random.Range(0.8f, 1.8f), ctx.stoneMat);
        }
        MarkStaticRecursive(root);
    }

    private void CreateVillageBuildings(BuildContext ctx, TerrainGrid grid, WorldLayout layout, Transform parent)
    {
        GameObject housesRoot = new GameObject("Houses");
        housesRoot.transform.SetParent(parent);

        for (int i = 0; i < layout.houses.Count; i++)
        {
            HouseSpec spec = layout.houses[i];
            CreateBuilding(ctx, grid, spec, housesRoot.transform, i);
        }
    }

    private void CreateBuilding(BuildContext ctx, TerrainGrid grid, HouseSpec spec, Transform parent, int index)
    {
        GameObject root = new GameObject(spec.kind + "_" + index.ToString("00"));
        root.transform.SetParent(parent);
        Vector3 pos = spec.position;
        pos.y = SampleTerrainHeight(grid, pos);
        root.transform.position = pos;
        root.transform.rotation = Quaternion.Euler(0f, spec.yaw, 0f);

        float w = spec.footprint.x;
        float d = spec.footprint.y;
        float h = spec.height;
        float roofH = Mathf.Lerp(1.8f, 3.2f, h / 7.2f);

        bool russian = villageStyle == VillageStyle.Russian;
        Material wall = russian ? ctx.logWallMat : ((spec.kind == BuildingKind.Barn || spec.kind == BuildingKind.Workshop || spec.kind == BuildingKind.Forge || spec.kind == BuildingKind.Mill) ? ctx.wallWarmMat : ctx.wallCreamMat);
        Material roof = russian ? (UnityEngine.Random.value > 0.35f ? ctx.roofDarkMat : ctx.roofRedMat) : ((spec.kind == BuildingKind.Barn || UnityEngine.Random.value > 0.55f) ? ctx.roofDarkMat : ctx.roofRedMat);

        if (spec.kind == BuildingKind.LongHouse)
        {
            w *= 1.18f;
            roofH += 0.25f;
        }
        else if (spec.kind == BuildingKind.Manor)
        {
            w *= 1.25f;
            d *= 1.18f;
            h *= 1.18f;
            roofH += 0.45f;
        }
        else if (spec.kind == BuildingKind.Chapel)
        {
            w *= 0.94f;
            d *= 1.18f;
            h *= 1.22f;
            roof = russian ? ctx.copperRoofMat : ctx.roofDarkMat;
            wall = ctx.wallCreamMat;
        }
        else if (spec.kind == BuildingKind.Forge)
        {
            w *= 1.08f;
            d *= 1.04f;
            wall = russian ? ctx.logWallMat : ctx.wallWarmMat;
            roof = ctx.roofDarkMat;
        }
        else if (spec.kind == BuildingKind.Mill)
        {
            w *= 1.06f;
            d *= 1.06f;
            h *= 1.22f;
            roof = russian ? ctx.roofDarkMat : ctx.roofRedMat;
            wall = russian ? ctx.logWallMat : ctx.wallCreamMat;
        }
        else if (spec.kind == BuildingKind.Tavern)
        {
            w *= 1.18f;
            d *= 1.12f;
            h *= 1.08f;
            roofH += 0.35f;
            wall = russian ? ctx.logWallMat : ctx.wallCreamMat;
            roof = ctx.roofDarkMat;
        }
        else if (spec.kind == BuildingKind.Stable)
        {
            d *= 1.08f;
            wall = russian ? ctx.logWallMat : ctx.wallWarmMat;
            roof = ctx.roofDarkMat;
        }
        else if (spec.kind == BuildingKind.Granary)
        {
            h *= 1.12f;
            roof = ctx.roofDarkMat;
            wall = russian ? ctx.logWallMat : ctx.wallWarmMat;
        }
        else if (spec.kind == BuildingKind.Boathouse)
        {
            d *= 1.18f;
            wall = russian ? ctx.logWallMat : ctx.wallWarmMat;
            roof = ctx.roofDarkMat;
        }

        CreateCubeChild(root.transform, "StoneBase", new Vector3(0f, 0.22f, 0f), new Vector3(w * 1.05f, 0.44f, d * 1.05f), ctx.stoneMat);
        CreateCubeChild(root.transform, "Body", new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, d), wall);
        CreateMeshChild(root.transform, "Roof", ctx.roofMesh, new Vector3(0f, h + 0.02f, 0f), new Vector3(w * 1.14f, roofH, d * 1.15f), roof);
        CreateCubeChild(root.transform, "SillBand", new Vector3(0f, h * 0.08f, d * 0.51f), new Vector3(w * 1.02f, 0.12f, 0.08f), ctx.timberMat);

        if (spec.annex && spec.kind != BuildingKind.Barn)
        {
            float side = UnityEngine.Random.value > 0.5f ? -1f : 1f;
            float aw = w * 0.42f;
            float ad = d * 0.52f;
            float ah = h * 0.72f;
            CreateCubeChild(root.transform, "Annex", new Vector3(side * (w * 0.52f), ah * 0.5f, -d * 0.04f), new Vector3(aw, ah, ad), wall);
            CreateMeshChild(root.transform, "AnnexRoof", ctx.roofMesh, new Vector3(side * (w * 0.52f), ah + 0.02f, -d * 0.04f), new Vector3(aw * 1.12f, roofH * 0.7f, ad * 1.15f), roof);
        }

        float doorW = spec.kind == BuildingKind.Barn ? 2.6f : (spec.kind == BuildingKind.Manor ? 1.55f : (spec.kind == BuildingKind.Chapel ? 1.4f : (spec.kind == BuildingKind.Tavern ? 1.55f : 1.2f)));
        float doorH = spec.kind == BuildingKind.Barn ? 3.0f : (spec.kind == BuildingKind.Chapel ? 2.9f : (spec.kind == BuildingKind.Tavern ? 2.65f : 2.45f));
        CreateCubeChild(root.transform, "Door", new Vector3(0f, doorH * 0.5f, d * 0.505f), new Vector3(doorW, doorH, 0.18f), ctx.timberMat);
        CreateCubeChild(root.transform, "DoorLintel", new Vector3(0f, doorH + 0.08f, d * 0.51f), new Vector3(doorW + 0.16f, 0.16f, 0.16f), ctx.stoneMat);

        bool tallWindows = spec.kind == BuildingKind.Manor || spec.kind == BuildingKind.LongHouse || spec.kind == BuildingKind.Tavern;
        AddWindows(ctx, root.transform, w, h, d, tallWindows, spec.kind == BuildingKind.Barn);
        if (spec.kind != BuildingKind.Barn && spec.kind != BuildingKind.Chapel)
            AddTimberFraming(ctx, root.transform, w, h, d, spec.kind != BuildingKind.Cottage);
        AddChimney(ctx, root.transform, w, h, roofH);

        if (spec.kind == BuildingKind.Workshop)
            CreateWorkshopSign(ctx, root.transform, w, h, d);
        if (spec.kind == BuildingKind.Barn)
            CreateBarnProps(ctx, root.transform, w, d);
        if (spec.kind == BuildingKind.Chapel)
            CreateChapelDetails(ctx, root.transform, w, h, d);
        if (spec.kind == BuildingKind.Forge)
            CreateForgeDetails(ctx, root.transform, w, h, d);
        if (spec.kind == BuildingKind.Mill)
            CreateMillDetails(ctx, root.transform, w, h, d);
        if (spec.kind == BuildingKind.Tavern)
            CreateTavernDetails(ctx, root.transform, w, h, d);
        if (spec.kind == BuildingKind.Stable)
            CreateStableDetails(ctx, root.transform, w, h, d);
        if (spec.kind == BuildingKind.Granary)
            CreateGranaryDetails(ctx, root.transform, w, h, d);
        if (spec.kind == BuildingKind.Boathouse)
            CreateBoathouseDetails(ctx, root.transform, w, h, d);

        CreateYardProps(ctx, root.transform, spec, w, d);

        if (spec.fenced)
        {
            CreateFenceLoop(ctx, root.transform, new Vector3(0f, 0f, -d * 0.06f), w * 1.9f, d * 2.1f, 3.0f);
            if (spec.garden)
                CreateGardenShrubs(ctx, root.transform, w, d);
        }

        SetupBuildingLOD(ctx, root, spec, wall, roof, w, h, d, roofH);
        MarkStaticRecursive(root);
    }

    private void AddWindows(BuildContext ctx, Transform root, float w, float h, float d, bool tall, bool barn)
    {
        if (root.name.StartsWith("Chapel"))
        {
            CreateCubeChild(root, "ChapelWindowFront", new Vector3(0f, h * 0.64f, d * 0.505f), new Vector3(0.85f, 1.8f, 0.08f), ctx.glassMat);
            CreateCubeChild(root, "ChapelWindowBack", new Vector3(0f, h * 0.64f, -d * 0.505f), new Vector3(0.85f, 1.8f, 0.08f), ctx.glassMat);
            CreateCubeChild(root, "ChapelWindowSideL", new Vector3(-w * 0.505f, h * 0.58f, 0f), new Vector3(0.08f, 1.55f, 0.9f), ctx.glassMat);
            CreateCubeChild(root, "ChapelWindowSideR", new Vector3(w * 0.505f, h * 0.58f, 0f), new Vector3(0.08f, 1.55f, 0.9f), ctx.glassMat);
            return;
        }

        if (barn)
        {
            CreateCubeChild(root, "LoftDoor", new Vector3(0f, h * 0.72f, d * 0.505f), new Vector3(1.8f, 1.2f, 0.12f), ctx.timberMat);
            CreateCubeChild(root, "SideWindowL", new Vector3(-w * 0.505f, h * 0.56f, d * 0.10f), new Vector3(0.1f, 0.9f, 1.0f), ctx.glassMat);
            CreateCubeChild(root, "SideWindowR", new Vector3(w * 0.505f, h * 0.56f, -d * 0.12f), new Vector3(0.1f, 0.9f, 1.0f), ctx.glassMat);
            return;
        }

        int frontCount = tall ? 3 : 2;
        float y1 = h * 0.54f;
        float spacing = w / (frontCount + 1f);
        for (int i = 0; i < frontCount; i++)
        {
            float x = -w * 0.5f + spacing * (i + 1);
            CreateCubeChild(root, "WindowFront_" + i, new Vector3(x, y1, d * 0.505f), new Vector3(0.95f, 1.0f, 0.08f), ctx.glassMat);
            CreateCubeChild(root, "WindowBack_" + i, new Vector3(x, y1, -d * 0.505f), new Vector3(0.95f, 1.0f, 0.08f), ctx.glassMat);
            if (tall)
                CreateCubeChild(root, "WindowTop_" + i, new Vector3(x, h * 0.75f, d * 0.505f), new Vector3(0.84f, 0.86f, 0.08f), ctx.glassMat);
        }

        CreateCubeChild(root, "WindowSideL", new Vector3(-w * 0.505f, y1, d * 0.15f), new Vector3(0.08f, 0.95f, 1.0f), ctx.glassMat);
        CreateCubeChild(root, "WindowSideR", new Vector3(w * 0.505f, y1, -d * 0.15f), new Vector3(0.08f, 0.95f, 1.0f), ctx.glassMat);
    }

    private void AddTimberFraming(BuildContext ctx, Transform root, float w, float h, float d, bool heavy)
    {
        float beam = heavy ? 0.18f : 0.14f;
        CreateCubeChild(root, "BeamTopFront", new Vector3(0f, h * 0.92f, d * 0.51f), new Vector3(w * 1.02f, beam, 0.10f), ctx.timberMat);
        CreateCubeChild(root, "BeamMidFront", new Vector3(0f, h * 0.48f, d * 0.51f), new Vector3(w * 1.02f, beam * 0.9f, 0.10f), ctx.timberMat);
        CreateCubeChild(root, "BeamVL", new Vector3(-w * 0.40f, h * 0.47f, d * 0.51f), new Vector3(beam, h * 0.88f, 0.10f), ctx.timberMat);
        CreateCubeChild(root, "BeamVR", new Vector3(w * 0.40f, h * 0.47f, d * 0.51f), new Vector3(beam, h * 0.88f, 0.10f), ctx.timberMat);
        if (heavy)
            CreateCubeChild(root, "BeamCenter", new Vector3(0f, h * 0.47f, d * 0.51f), new Vector3(beam, h * 0.88f, 0.10f), ctx.timberMat);
    }

    private void AddChimney(BuildContext ctx, Transform root, float w, float h, float roofH)
    {
        float x = UnityEngine.Random.value > 0.5f ? w * 0.22f : -w * 0.22f;
        CreateCubeChild(root, "Chimney", new Vector3(x, h + roofH * 0.8f, 0f), new Vector3(0.72f, 2.0f, 0.72f), ctx.stoneMat);
    }

    private void CreateWorkshopSign(BuildContext ctx, Transform root, float w, float h, float d)
    {
        GameObject sign = new GameObject("WorkshopSign");
        sign.transform.SetParent(root, false);
        sign.transform.localPosition = new Vector3(-w * 0.34f, h * 0.74f, d * 0.60f);
        CreateCubeChild(sign.transform, "Arm", Vector3.zero, new Vector3(0.14f, 1.1f, 0.14f), ctx.timberMat);
        CreateCubeChild(sign.transform, "Board", new Vector3(0.42f, -0.08f, 0f), new Vector3(0.78f, 0.48f, 0.08f), ctx.wheatBaleMat);
    }

    private void CreateBarnProps(BuildContext ctx, Transform root, float w, float d)
    {
        CreateCylinderChild(root, "BaleA", new Vector3(-w * 0.30f, 0.45f, d * 0.76f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.55f, 0.32f, 0.55f), ctx.wheatBaleMat);
        CreateCylinderChild(root, "BaleB", new Vector3(w * 0.28f, 0.42f, d * 0.72f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.48f, 0.28f, 0.48f), ctx.wheatBaleMat);
    }

    private void CreateChapelDetails(BuildContext ctx, Transform root, float w, float h, float d)
    {
        CreateCubeChild(root, "BellTower", new Vector3(0f, h + 1.7f, -d * 0.18f), new Vector3(1.4f, 3.0f, 1.4f), ctx.wallCreamMat);
        if (villageStyle == VillageStyle.Russian)
        {
            CreateSphereChild(root, "OnionBase", new Vector3(0f, h + 3.55f, -d * 0.18f), new Vector3(1.25f, 1.45f, 1.25f), ctx.copperRoofMat);
            CreateMeshChild(root, "Spire", ctx.coneMesh, new Vector3(0f, h + 4.55f, -d * 0.18f), new Vector3(0.75f, 1.45f, 0.75f), ctx.copperRoofMat);
        }
        else
        {
            CreateMeshChild(root, "TowerRoof", ctx.roofMesh, new Vector3(0f, h + 3.1f, -d * 0.18f), new Vector3(1.7f, 1.9f, 1.7f), ctx.roofDarkMat);
        }
        CreateCubeChild(root, "CrossV", new Vector3(0f, h + 5.2f, -d * 0.18f), new Vector3(0.10f, 0.9f, 0.10f), ctx.stoneMat);
        CreateCubeChild(root, "CrossH", new Vector3(0f, h + 5.05f, -d * 0.18f), new Vector3(0.44f, 0.10f, 0.10f), ctx.stoneMat);
    }

    private void CreateYardProps(BuildContext ctx, Transform root, HouseSpec spec, float w, float d)
    {
        if (spec.kind == BuildingKind.Barn || spec.kind == BuildingKind.Chapel)
            return;

        if (spec.kind == BuildingKind.Farmhouse || spec.kind == BuildingKind.LongHouse || spec.kind == BuildingKind.Manor || spec.kind == BuildingKind.Tavern)
        {
            CreateCubeChild(root, "WoodPile", new Vector3(-w * 0.42f, 0.30f, -d * 0.58f), new Vector3(1.2f, 0.6f, 0.7f), ctx.timberMat);
            if (spec.kind != BuildingKind.Manor)
                CreateCubeChild(root, "Shed", new Vector3(w * 0.72f, 1.05f, -d * 0.38f), new Vector3(2.1f, 2.1f, 2.0f), ctx.wallWarmMat);
            if (spec.kind == BuildingKind.Manor || UnityEngine.Random.value > 0.48f)
            {
                GameObject tree = new GameObject("YardTree");
                tree.transform.SetParent(root, false);
                tree.transform.localPosition = new Vector3(-w * 0.55f, 0f, d * 0.95f);
                tree.transform.localScale = Vector3.one * (spec.kind == BuildingKind.Manor ? 0.85f : 0.62f);
                BuildBroadleafTree(ctx, tree.transform);
            }
        }
        else if (spec.kind == BuildingKind.Workshop || spec.kind == BuildingKind.Forge)
        {
            CreateCubeChild(root, "Bench", new Vector3(w * 0.60f, 0.36f, d * 0.72f), new Vector3(1.8f, 0.18f, 0.6f), ctx.timberMat);
            CreateCylinderChild(root, "Barrel", new Vector3(-w * 0.55f, 0.42f, d * 0.70f), new Vector3(0.32f, 0.42f, 0.32f), ctx.timberMat);
        }
        else if (spec.kind == BuildingKind.Stable)
        {
            CreateCylinderChild(root, "Trough", new Vector3(0f, 0.30f, d * 0.94f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.24f, 1.0f, 0.24f), ctx.stoneMat);
            CreateCubeChild(root, "HayRack", new Vector3(-w * 0.52f, 0.46f, d * 0.72f), new Vector3(1.3f, 0.92f, 0.5f), ctx.wheatBaleMat);
        }
        else if (spec.kind == BuildingKind.Granary)
        {
            CreateCubeChild(root, "CrateA", new Vector3(w * 0.56f, 0.32f, d * 0.56f), new Vector3(0.72f, 0.64f, 0.72f), ctx.timberMat);
            CreateCubeChild(root, "CrateB", new Vector3(w * 0.72f, 0.34f, d * 0.78f), new Vector3(0.64f, 0.68f, 0.64f), ctx.timberMat);
        }
        else if (spec.kind == BuildingKind.Boathouse)
        {
            CreateCubeChild(root, "PierPostA", new Vector3(-w * 0.28f, -0.22f, d * 1.42f), new Vector3(0.18f, 1.1f, 0.18f), ctx.timberMat);
            CreateCubeChild(root, "PierPostB", new Vector3(w * 0.28f, -0.22f, d * 1.42f), new Vector3(0.18f, 1.1f, 0.18f), ctx.timberMat);
        }
        else if (spec.kind == BuildingKind.Cottage && UnityEngine.Random.value > 0.45f)
        {
            CreateCubeChild(root, "Bench", new Vector3(-w * 0.44f, 0.34f, d * 0.76f), new Vector3(1.1f, 0.16f, 0.38f), ctx.timberMat);
        }
    }

    private void CreateForgeDetails(BuildContext ctx, Transform root, float w, float h, float d)
    {
        CreateCubeChild(root, "ForgeCanopy", new Vector3(-w * 0.60f, h * 0.52f, d * 0.10f), new Vector3(2.2f, 0.18f, 2.6f), ctx.timberMat);
        CreateCubeChild(root, "ForgePostA", new Vector3(-w * 0.98f, h * 0.26f, d * 0.85f), new Vector3(0.14f, h * 0.52f, 0.14f), ctx.timberMat);
        CreateCubeChild(root, "ForgePostB", new Vector3(-w * 0.22f, h * 0.26f, d * 0.85f), new Vector3(0.14f, h * 0.52f, 0.14f), ctx.timberMat);
        CreateCubeChild(root, "AnvilBase", new Vector3(-w * 0.60f, 0.55f, d * 0.58f), new Vector3(0.7f, 1.1f, 0.7f), ctx.stoneMat);
        CreateCubeChild(root, "AnvilTop", new Vector3(-w * 0.60f, 1.16f, d * 0.58f), new Vector3(0.9f, 0.20f, 0.38f), ctx.stoneMat);
        CreateCubeChild(root, "ForgeFire", new Vector3(-w * 0.22f, 0.44f, d * 0.46f), new Vector3(0.75f, 0.22f, 0.75f), ctx.forgeFireMat);
        CreateCubeChild(root, "ForgeChimney", new Vector3(w * 0.28f, h + 1.5f, -d * 0.08f), new Vector3(0.92f, 3.2f, 0.92f), ctx.stoneMat);
    }

    private void CreateMillDetails(BuildContext ctx, Transform root, float w, float h, float d)
    {
        CreateCylinderChild(root, "MillTower", new Vector3(0f, h * 0.58f, 0f), new Vector3(w * 0.12f, h * 0.58f, d * 0.12f), ctx.wallCreamMat);
        CreateMeshChild(root, "MillTopRoof", ctx.roofMesh, new Vector3(0f, h * 1.18f, 0f), new Vector3(w * 0.62f, 2.2f, d * 0.62f), ctx.roofDarkMat);
        GameObject sails = new GameObject("Sails");
        sails.transform.SetParent(root, false);
        sails.transform.localPosition = new Vector3(0f, h * 0.82f, d * 0.58f);
        for (int i = 0; i < 4; i++)
        {
            GameObject sail = new GameObject("Sail_" + i);
            sail.transform.SetParent(sails.transform, false);
            sail.transform.localRotation = Quaternion.Euler(0f, 0f, i * 90f);
            CreateCubeChild(sail.transform, "Beam", new Vector3(0f, 1.5f, 0f), new Vector3(0.14f, 3.0f, 0.14f), ctx.timberMat);
            CreateCubeChild(sail.transform, "Cloth", new Vector3(0.30f, 1.5f, 0f), new Vector3(0.62f, 2.2f, 0.06f), ctx.wheatBladeMat);
        }
    }

    private void CreateTavernDetails(BuildContext ctx, Transform root, float w, float h, float d)
    {
        CreateCubeChild(root, "Awning", new Vector3(0f, h * 0.62f, d * 0.72f), new Vector3(w * 0.46f, 0.16f, 1.0f), ctx.roofRedMat);
        CreateCubeChild(root, "SignPost", new Vector3(-w * 0.46f, h * 0.58f, d * 0.66f), new Vector3(0.12f, 1.4f, 0.12f), ctx.timberMat);
        CreateCubeChild(root, "SignBoard", new Vector3(-w * 0.12f, h * 0.72f, d * 0.67f), new Vector3(1.0f, 0.56f, 0.08f), ctx.wallCreamMat);
        CreateCylinderChild(root, "BarrelA", new Vector3(w * 0.46f, 0.44f, d * 0.72f), new Vector3(0.34f, 0.44f, 0.34f), ctx.timberMat);
        CreateCylinderChild(root, "BarrelB", new Vector3(w * 0.62f, 0.42f, d * 0.58f), new Vector3(0.30f, 0.42f, 0.30f), ctx.timberMat);
    }

    private void CreateStableDetails(BuildContext ctx, Transform root, float w, float h, float d)
    {
        CreateCubeChild(root, "Canopy", new Vector3(0f, h * 0.46f, d * 0.72f), new Vector3(w * 0.70f, 0.14f, 1.4f), ctx.timberMat);
        CreateCubeChild(root, "PostL", new Vector3(-w * 0.56f, h * 0.22f, d * 1.10f), new Vector3(0.16f, h * 0.46f, 0.16f), ctx.timberMat);
        CreateCubeChild(root, "PostR", new Vector3(w * 0.56f, h * 0.22f, d * 1.10f), new Vector3(0.16f, h * 0.46f, 0.16f), ctx.timberMat);
        CreateCylinderChild(root, "WaterTrough", new Vector3(0f, 0.34f, d * 1.12f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.36f, 1.05f, 0.36f), ctx.stoneMat);
    }

    private void CreateGranaryDetails(BuildContext ctx, Transform root, float w, float h, float d)
    {
        for (int i = -1; i <= 1; i += 2)
            CreateCubeChild(root, "Stilt_" + i, new Vector3(i * w * 0.28f, 0.55f, 0f), new Vector3(0.38f, 1.1f, 0.38f), ctx.stoneMat);
        CreateCubeChild(root, "Ladder", new Vector3(-w * 0.50f, 1.1f, d * 0.58f), Quaternion.Euler(65f, 0f, 0f), new Vector3(0.14f, 2.4f, 0.70f), ctx.timberMat);
    }

    private void CreateBoathouseDetails(BuildContext ctx, Transform root, float w, float h, float d)
    {
        CreateCubeChild(root, "Ramp", new Vector3(0f, -0.10f, d * 0.82f), Quaternion.Euler(12f, 0f, 0f), new Vector3(w * 0.72f, 0.14f, 4.4f), ctx.timberMat);
        CreateCubeChild(root, "Boat", new Vector3(0f, 0.18f, d * 1.24f), new Vector3(2.8f, 0.36f, 5.2f), ctx.timberMat);
    }

    private void CreateFenceLoop(BuildContext ctx, Transform parent, Vector3 centerOffset, float width, float depth, float gateWidth)
    {
        GameObject root = new GameObject("Fence");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = centerOffset;

        float hw = width * 0.5f;
        float hd = depth * 0.5f;
        float y = 0.55f;

        CreateFenceSegment(ctx, root.transform, new Vector3(-hw, y, -hd), new Vector3(hw, y, -hd));
        CreateFenceSegment(ctx, root.transform, new Vector3(-hw, y, hd), new Vector3(-gateWidth * 0.5f, y, hd));
        CreateFenceSegment(ctx, root.transform, new Vector3(gateWidth * 0.5f, y, hd), new Vector3(hw, y, hd));
        CreateFenceSegment(ctx, root.transform, new Vector3(-hw, y, -hd), new Vector3(-hw, y, hd));
        CreateFenceSegment(ctx, root.transform, new Vector3(hw, y, -hd), new Vector3(hw, y, hd));
    }

    private void CreateFenceSegment(BuildContext ctx, Transform parent, Vector3 a, Vector3 b)
    {
        Vector3 dir = b - a;
        float len = dir.magnitude;
        int posts = Mathf.Max(2, Mathf.RoundToInt(len / 1.8f) + 1);
        for (int i = 0; i < posts; i++)
        {
            float t = i / (float)(posts - 1);
            Vector3 p = Vector3.Lerp(a, b, t);
            CreateCubeChild(parent, "Post", p, new Vector3(0.14f, 1.08f, 0.14f), ctx.timberMat);
        }

        Quaternion rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        Vector3 mid = (a + b) * 0.5f;
        CreateCubeChild(parent, "RailA", mid + Vector3.up * 0.14f, rot, new Vector3(0.10f, 0.10f, len), ctx.timberMat);
        CreateCubeChild(parent, "RailB", mid - Vector3.up * 0.22f, rot, new Vector3(0.10f, 0.10f, len), ctx.timberMat);
    }

    private void CreateGardenShrubs(BuildContext ctx, Transform houseRoot, float w, float d)
    {
        GameObject shrubs = new GameObject("Garden");
        shrubs.transform.SetParent(houseRoot, false);
        int count = UnityEngine.Random.Range(4, 8);
        for (int i = 0; i < count; i++)
        {
            float x = UnityEngine.Random.Range(-w * 0.7f, w * 0.7f);
            float z = UnityEngine.Random.Range(-d * 0.9f, -d * 0.18f);
            CreateShrubCluster(ctx, shrubs.transform, new Vector3(x, 0f, z), UnityEngine.Random.Range(0.8f, 1.3f));
        }
    }

    private void CreateShrubCluster(BuildContext ctx, Transform parent, Vector3 localPos, float scale)
    {
        GameObject root = new GameObject("Shrub");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localPos;
        root.transform.localScale = Vector3.one * scale;
        CreateSphereChild(root.transform, "A", new Vector3(-0.25f, 0.45f, 0f), new Vector3(0.7f, 0.7f, 0.7f), ctx.leafMat);
        CreateSphereChild(root.transform, "B", new Vector3(0.24f, 0.46f, 0.07f), new Vector3(0.74f, 0.72f, 0.74f), ctx.leafMat);
        CreateSphereChild(root.transform, "C", new Vector3(0f, 0.60f, -0.16f), new Vector3(0.78f, 0.72f, 0.78f), ctx.leafMat);
    }

    private void CreateFieldBoundaryProps(BuildContext ctx, TerrainGrid grid, WorldLayout layout, Transform parent)
    {
        GameObject hedges = new GameObject("Hedgerows");
        hedges.transform.SetParent(parent);
        GameObject crops = new GameObject("NearFieldRows");
        crops.transform.SetParent(parent);

        float cell = layout.farmlandCellSize;
        int radiusCells = Mathf.CeilToInt((1400f + qualityBoost * 320f) / cell);
        int centerX = Mathf.FloorToInt(layout.villageCenter.x / cell);
        int centerZ = Mathf.FloorToInt(layout.villageCenter.z / cell);

        for (int cz = centerZ - radiusCells; cz <= centerZ + radiusCells; cz++)
        {
            for (int cx = centerX - radiusCells; cx <= centerX + radiusCells; cx++)
            {
                if (cx < 0 || cz < 0)
                    continue;

                float parcelCenterX = (cx + 0.5f) * cell;
                float parcelCenterZ = (cz + 0.5f) * cell;
                if (parcelCenterX > layout.worldSizeMeters || parcelCenterZ > layout.worldSizeMeters)
                    continue;
                if (ComputeForestMask(layout, parcelCenterX, parcelCenterZ) > 0.18f)
                    continue;
                if (ComputeVillageMask(layout, parcelCenterX, parcelCenterZ) > 0.26f || ComputeRoadMask(layout, parcelCenterX, parcelCenterZ) > 0.08f)
                    continue;

                bool hedgeX = Hash01(cx, cz, seed + 601) > Mathf.Lerp(0.58f, 0.42f, Mathf.InverseLerp(1f, 3f, qualityBoost));
                bool hedgeZ = Hash01(cx, cz, seed + 611) > Mathf.Lerp(0.58f, 0.42f, Mathf.InverseLerp(1f, 3f, qualityBoost));

                if (hedgeX)
                {
                    Vector3 a = new Vector3(cx * cell, 0f, cz * cell);
                    Vector3 b = new Vector3(cx * cell, 0f, (cz + 1) * cell);
                    CreateHedgeSegment(ctx, grid, hedges.transform, a, b, 1.8f);
                }
                if (hedgeZ)
                {
                    Vector3 a = new Vector3(cx * cell, 0f, cz * cell);
                    Vector3 b = new Vector3((cx + 1) * cell, 0f, cz * cell);
                    CreateHedgeSegment(ctx, grid, hedges.transform, a, b, 1.8f);
                }

                float border;
                float wheat = ComputeWheatFieldMask(layout, parcelCenterX, parcelCenterZ, out border);
                if (wheat > 0.45f && Vector2.Distance(new Vector2(parcelCenterX, parcelCenterZ), new Vector2(layout.villageCenter.x, layout.villageCenter.z)) < 900f && Hash01(cx, cz, seed + 621) > 0.52f)
                {
                    CreateCropRows(ctx, grid, crops.transform, parcelCenterX, parcelCenterZ, cell * 0.78f, cell * 0.56f, Hash01(cx, cz, seed + 631) > 0.5f);
                }
            }
        }

        MarkStaticRecursive(hedges);
        MarkStaticRecursive(crops);
    }

    private void CreateHedgeSegment(BuildContext ctx, TerrainGrid grid, Transform parent, Vector3 a, Vector3 b, float width)
    {
        Vector3 dir = (b - a).normalized;
        float len = Vector3.Distance(a, b);
        int segments = Mathf.Max(2, Mathf.CeilToInt(len / 12f));
        for (int i = 0; i < segments; i++)
        {
            float t0 = i / (float)segments;
            float t1 = (i + 1) / (float)segments;
            Vector3 p0 = Vector3.Lerp(a, b, t0);
            Vector3 p1 = Vector3.Lerp(a, b, t1);
            Vector3 mid = (p0 + p1) * 0.5f;
            float segLen = Vector3.Distance(p0, p1);
            mid.y = SampleTerrainHeight(grid, mid) + 0.45f;
            Quaternion rot = Quaternion.LookRotation(dir);
            CreateCubeChild(parent, "Hedge", mid, rot, new Vector3(width, 0.9f, segLen + 0.2f), ctx.leafMat);
            if (i % 2 == 0)
            {
                Vector3 treePos = mid + Vector3.up * 0.1f;
                CreateCylinderChild(parent, "HedgeTrunk", new Vector3(treePos.x, SampleTerrainHeight(grid, treePos) + 0.65f, treePos.z), new Vector3(0.10f, 0.65f, 0.10f), ctx.barkMat);
            }
        }
    }

    private void CreateCropRows(BuildContext ctx, TerrainGrid grid, Transform parent, float centerX, float centerZ, float width, float depth, bool alongX)
    {
        GameObject rows = new GameObject($"CropRows_{Mathf.RoundToInt(centerX)}_{Mathf.RoundToInt(centerZ)}");
        rows.transform.SetParent(parent);
        rows.transform.position = new Vector3(centerX, 0f, centerZ);
        rows.transform.rotation = Quaternion.Euler(0f, alongX ? 0f : 90f, 0f);

        int rowCount = Mathf.Clamp(Mathf.RoundToInt(width / Mathf.Lerp(4.6f, 2.7f, Mathf.InverseLerp(1f, 3f, qualityBoost))), 10, 30);
        int strips = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(3f, 6f, Mathf.InverseLerp(1f, 3f, qualityBoost))), 3, 6);
        float rowSpacing = width / rowCount;
        for (int i = 0; i < rowCount; i++)
        {
            float offset = -width * 0.5f + rowSpacing * (i + 0.5f);
            for (int s = 0; s < strips; s++)
            {
                float z = -depth * 0.5f + depth * (s + 0.5f) / strips;
                Vector3 local = new Vector3(offset, 0f, z);
                Vector3 world = rows.transform.TransformPoint(local);
                world.y = SampleTerrainHeight(grid, world) + 0.30f;
                local = rows.transform.InverseTransformPoint(world);
                CreateCubeChild(rows.transform, "Row", local, new Vector3(rowSpacing * 0.50f, 0.65f + 0.03f * s, depth / strips * 0.70f), ctx.wheatBaleMat);
            }
        }
    }

    private void CreateFieldStonePiles(BuildContext ctx, TerrainGrid grid, WorldLayout layout, Transform parent)
    {
        GameObject root = new GameObject("FieldStonePiles");
        root.transform.SetParent(parent);
        float cell = layout.farmlandCellSize;
        int count = Mathf.RoundToInt(14f * qualityBoost);
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = new Vector3(UnityEngine.Random.Range(layout.villageCenter.x - 1200f, layout.villageCenter.x + 1200f), 0f, UnityEngine.Random.Range(layout.villageCenter.z - 1200f, layout.villageCenter.z + 1200f));
            if (ComputeVillageMask(layout, pos.x, pos.z) > 0.18f || ComputeRoadMask(layout, pos.x, pos.z) > 0.10f || ComputeLakeMask(layout, pos.x, pos.z) > 0.06f || ComputeRiverMask(layout, pos.x, pos.z) > 0.08f)
                continue;
            pos.y = SampleTerrainHeight(grid, pos);
            GameObject pile = new GameObject("Pile_" + i);
            pile.transform.SetParent(root.transform);
            pile.transform.position = pos;
            int stones = UnityEngine.Random.Range(4, 8);
            for (int s = 0; s < stones; s++)
            {
                CreateSphereChild(pile.transform, "Stone_" + s, new Vector3(UnityEngine.Random.Range(-1.2f, 1.2f), UnityEngine.Random.Range(0.1f, 0.5f), UnityEngine.Random.Range(-1.0f, 1.0f)), Vector3.one * UnityEngine.Random.Range(0.35f, 0.9f), ctx.stoneMat);
            }
        }
        MarkStaticRecursive(root);
    }

    private void CreateFieldProps(BuildContext ctx, TerrainGrid grid, WorldLayout layout, Transform parent)
    {
        GameObject hayRoot = new GameObject("HayBales");
        hayRoot.transform.SetParent(parent);

        float cell = layout.farmlandCellSize;
        float fieldRadius = 900f + qualityBoost * 300f;
        int minX = Mathf.Max(0, Mathf.FloorToInt((layout.villageCenter.x - fieldRadius) / cell) - 1);
        int maxX = Mathf.Min(Mathf.CeilToInt(layout.worldSizeMeters / cell), Mathf.CeilToInt((layout.villageCenter.x + fieldRadius) / cell) + 1);
        int minZ = Mathf.Max(0, Mathf.FloorToInt((layout.villageCenter.z - fieldRadius) / cell) - 1);
        int maxZ = Mathf.Min(Mathf.CeilToInt(layout.worldSizeMeters / cell), Mathf.CeilToInt((layout.villageCenter.z + fieldRadius) / cell) + 1);

        for (int cz = minZ; cz <= maxZ; cz++)
        {
            for (int cx = minX; cx <= maxX; cx++)
            {
                float centerX = (cx + 0.5f) * cell;
                float centerZ = (cz + 0.5f) * cell;
                if (ComputeForestMask(layout, centerX, centerZ) > 0.2f)
                    continue;
                if (ComputeVillageMask(layout, centerX, centerZ) > 0.18f)
                    continue;

                float border;
                float wheat = ComputeWheatFieldMask(layout, centerX, centerZ, out border);
                if (wheat < 0.5f)
                    continue;
                if (Hash01(cx, cz, seed + 99) > Mathf.Lerp(0.42f, 0.22f, Mathf.InverseLerp(1f, 3f, qualityBoost)))
                    continue;

                Vector3 pos = new Vector3(centerX + (Hash01(cx, cz, seed + 11) - 0.5f) * cell * 0.32f, 0f, centerZ + (Hash01(cx, cz, seed + 21) - 0.5f) * cell * 0.28f);
                pos.y = SampleTerrainHeight(grid, pos);

                GameObject cluster = new GameObject($"HayCluster_{cx}_{cz}");
                cluster.transform.SetParent(hayRoot.transform);
                cluster.transform.position = pos;
                cluster.transform.rotation = Quaternion.Euler(0f, Hash01(cx, cz, seed + 31) * 360f, 0f);

                CreateCylinderChild(cluster.transform, "BaleA", new Vector3(0f, 0.42f, 0f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.62f, 0.34f, 0.62f), ctx.wheatBaleMat);
                if (Hash01(cx, cz, seed + 41) > 0.4f)
                    CreateCylinderChild(cluster.transform, "BaleB", new Vector3(0.76f, 0.42f, 0.18f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.58f, 0.32f, 0.58f), ctx.wheatBaleMat);
                if (Hash01(cx, cz, seed + 51) > 0.65f)
                    CreateCylinderChild(cluster.transform, "BaleC", new Vector3(-0.55f, 0.42f, -0.22f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.52f, 0.30f, 0.52f), ctx.wheatBaleMat);
                if (qualityBoost > 1.4f && Hash01(cx, cz, seed + 61) > 0.58f)
                    CreateCylinderChild(cluster.transform, "BaleD", new Vector3(0.22f, 0.78f, -0.12f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.50f, 0.28f, 0.50f), ctx.wheatBaleMat);
            }
        }

        MarkStaticRecursive(hayRoot);
    }

    private void CreateFarForest(BuildContext ctx, TerrainGrid grid, WorldLayout layout, Transform parent)
    {
        GameObject edgeRoot = new GameObject("ForestEdge");
        edgeRoot.transform.SetParent(parent);
        GameObject pines = new GameObject("Pines");
        pines.transform.SetParent(parent);
        GameObject broadleaf = new GameObject("Broadleaf");
        broadleaf.transform.SetParent(parent);

        float startZ = layout.forestStartZ - 260f;
        float endZ = layout.worldSizeMeters - 80f;
        float step = layout.worldSizeMeters > 9000f ? Mathf.Lerp(38f, 26f, Mathf.InverseLerp(1f, 3f, qualityBoost)) : Mathf.Lerp(30f, 18f, Mathf.InverseLerp(1f, 3f, qualityBoost));
        int created = 0;

        for (float z = startZ; z <= endZ; z += step)
        {
            for (float x = 80f; x <= layout.worldSizeMeters - 80f; x += step)
            {
                float forest = ComputeForestMask(layout, x, z);
                if (forest < 0.48f)
                    continue;
                float hash = Hash01(Mathf.FloorToInt(x / step), Mathf.FloorToInt(z / step), seed + 301);
                if (hash > Mathf.Lerp(0.28f, 0.64f, forest))
                    continue;

                Vector3 pos = new Vector3(x + (Hash01((int)x, (int)z, seed + 17) - 0.5f) * step * 0.6f, 0f, z + (Hash01((int)x, (int)z, seed + 27) - 0.5f) * step * 0.6f);
                pos.y = SampleTerrainHeight(grid, pos);
                bool edgeBand = forest < 0.72f;
                bool pine = edgeBand ? (Hash01((int)x, (int)z, seed + 37) > 0.52f) : (Hash01((int)x, (int)z, seed + 37) > 0.24f);

                GameObject tree = new GameObject((pine ? "Pine_" : "Tree_") + created++);
                tree.transform.SetParent(edgeBand ? edgeRoot.transform : (pine ? pines.transform : broadleaf.transform));
                tree.transform.position = pos;
                tree.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(-2f, 2f), Hash01((int)x, (int)z, seed + 47) * 360f, UnityEngine.Random.Range(-2f, 2f));
                tree.transform.localScale = Vector3.one * (pine ? Mathf.Lerp(0.95f, 1.70f, forest) : Mathf.Lerp(0.88f, 1.55f, forest));

                if (pine)
                    BuildPineTree(ctx, tree.transform);
                else
                    BuildBroadleafTree(ctx, tree.transform);

                if (!pine && qualityBoost > 1.25f && Hash01((int)x, (int)z, seed + 333) > 0.65f)
                {
                    CreateShrubCluster(ctx, tree.transform, new Vector3(0.8f, 0f, -0.4f), 0.7f);
                    CreateShrubCluster(ctx, tree.transform, new Vector3(-0.6f, 0f, 0.5f), 0.6f);
                }

                MarkStaticRecursive(tree);
            }
        }
    }

    private void BuildPineTree(BuildContext ctx, Transform root)
    {
        CreateCylinderChild(root, "Trunk", new Vector3(0f, 2.3f, 0f), new Vector3(0.23f, 2.3f, 0.23f), ctx.barkMat);
        CreateMeshChild(root, "ConeA", ctx.coneMesh, new Vector3(0f, 2.7f, 0f), new Vector3(2.2f, 2.5f, 2.2f), ctx.pineMat);
        CreateMeshChild(root, "ConeB", ctx.coneMesh, new Vector3(0f, 3.9f, 0f), new Vector3(1.8f, 2.1f, 1.8f), ctx.pineMat);
        CreateMeshChild(root, "ConeC", ctx.coneMesh, new Vector3(0f, 5.0f, 0f), new Vector3(1.2f, 1.6f, 1.2f), ctx.pineMat);
        SetupTreeLOD(ctx, root.gameObject, true);
    }

    private void BuildBroadleafTree(BuildContext ctx, Transform root)
    {
        CreateCylinderChild(root, "Trunk", new Vector3(0f, 2.5f, 0f), new Vector3(0.28f, 2.5f, 0.28f), ctx.barkMat);
        CreateSphereChild(root, "Leaf1", new Vector3(-0.42f, 4.3f, 0f), new Vector3(2.1f, 2.0f, 2.0f), ctx.leafMat);
        CreateSphereChild(root, "Leaf2", new Vector3(0.52f, 4.55f, 0.18f), new Vector3(2.0f, 1.9f, 2.0f), ctx.leafMat);
        CreateSphereChild(root, "Leaf3", new Vector3(0f, 5.1f, -0.24f), new Vector3(2.2f, 2.0f, 2.2f), ctx.leafMat);
        SetupTreeLOD(ctx, root.gameObject, false);
    }

    private void CreateWorldOptimizationSystem(BuildContext ctx, WorldLayout layout, GameObject worldRoot, GameObject roadsRoot, GameObject villageRoot, GameObject fieldsRoot, GameObject forestRoot)
    {
        GameObject optimizationRoot = new GameObject("Optimization");
        optimizationRoot.transform.SetParent(worldRoot.transform);

        GameObject hlodRoot = new GameObject("HLOD");
        hlodRoot.transform.SetParent(optimizationRoot.transform);

        GameObject villageProxy = CreateVillageHLODProxy(ctx, layout, hlodRoot.transform);
        GameObject forestProxy = CreateForestHLODProxy(ctx, layout, hlodRoot.transform);
        GameObject roadsProxy = CreateRoadHLODProxy(ctx, layout, hlodRoot.transform);
        GameObject fieldProxy = CreateFieldHLODProxy(ctx, layout, hlodRoot.transform);

        List<GameObject> roadChunks = ChunkizeChildren(roadsRoot, 700f, "RoadChunk");
        List<GameObject> fieldChunks = ChunkizeChildren(fieldsRoot, 900f, "FieldChunk");
        List<GameObject> forestChunks = ChunkizeForestChildren(forestRoot, 1200f, "ForestChunk");

        GameObject streamingGO = new GameObject("Distance Streaming");
        streamingGO.transform.SetParent(optimizationRoot.transform);
        DistanceChunkActivator activator = streamingGO.AddComponent<DistanceChunkActivator>();
        activator.updateInterval = 0.25f;
        activator.drawGizmos = false;

        float villageSwitch = 1800f + qualityBoost * 180f;
        float roadsSwitch = 2400f + qualityBoost * 220f;
        float fieldSwitch = 2700f + qualityBoost * 240f;
        float forestSwitch = 3400f + qualityBoost * 320f;

        AddChunkRule(activator, "Village_HLOD", villageRoot, villageProxy, villageSwitch, 220f);
        AddChunkRule(activator, "Forest_HLOD", forestRoot, forestProxy, forestSwitch, 260f);
        AddChunkRule(activator, "Road_HLOD", roadsRoot, roadsProxy, roadsSwitch, 220f);
        AddChunkRule(activator, "Field_HLOD", fieldsRoot, fieldProxy, fieldSwitch, 220f);

        for (int i = 0; i < roadChunks.Count; i++)
            AddChunkRule(activator, "RoadChunk_" + i, roadChunks[i], null, roadsSwitch + 180f, 140f);
        for (int i = 0; i < fieldChunks.Count; i++)
            AddChunkRule(activator, "FieldChunk_" + i, fieldChunks[i], null, fieldSwitch + 220f, 160f);
        for (int i = 0; i < forestChunks.Count; i++)
            AddChunkRule(activator, "ForestChunk_" + i, forestChunks[i], null, forestSwitch + 320f, 220f);

        activator.RebuildBounds();
        activator.ApplyImmediate();
    }

    private void AddChunkRule(DistanceChunkActivator activator, string name, GameObject nearRoot, GameObject farRoot, float distance, float hysteresis)
    {
        DistanceChunkActivator.ChunkRule rule = new DistanceChunkActivator.ChunkRule
        {
            name = name,
            nearRoot = nearRoot,
            farRoot = farRoot,
            switchDistance = distance,
            hysteresis = hysteresis
        };
        activator.chunks.Add(rule);
        if (farRoot != null)
            farRoot.SetActive(false);
    }

    private GameObject CreateVillageHLODProxy(BuildContext ctx, WorldLayout layout, Transform parent)
    {
        GameObject root = new GameObject("VillageProxy");
        root.transform.SetParent(parent);
        for (int i = 0; i < layout.houses.Count; i++)
        {
            HouseSpec spec = layout.houses[i];
            GameObject proxy = new GameObject("B_" + i);
            proxy.transform.SetParent(root.transform);
            proxy.transform.position = spec.position;
            proxy.transform.rotation = Quaternion.Euler(0f, spec.yaw, 0f);
            float w = spec.footprint.x;
            float d = spec.footprint.y;
            float h = spec.height;
            Material wall = villageStyle == VillageStyle.Russian ? ctx.logWallMat : ctx.wallCreamMat;
            Material roof = (spec.kind == BuildingKind.Chapel && villageStyle == VillageStyle.Russian) ? ctx.copperRoofMat : ctx.roofDarkMat;
            CreateCubeChild(proxy.transform, "Body", new Vector3(0f, h * 0.46f, 0f), new Vector3(w * 0.94f, h * 0.88f, d * 0.94f), wall);
            CreateMeshChild(proxy.transform, "Roof", ctx.roofMesh, new Vector3(0f, h * 0.88f, 0f), new Vector3(w, Mathf.Lerp(1.2f, 2.6f, h / 10f), d), roof);
            if (spec.kind == BuildingKind.Chapel || spec.kind == BuildingKind.Mill)
                CreateCubeChild(proxy.transform, "Tower", new Vector3(0f, h * 0.95f, 0f), new Vector3(w * 0.18f, h * 0.9f, d * 0.18f), wall);
        }
        SetShadowsRecursive(root, ShadowCastingMode.Off, false);
        return root;
    }

    private GameObject CreateForestHLODProxy(BuildContext ctx, WorldLayout layout, Transform parent)
    {
        GameObject root = new GameObject("ForestProxy");
        root.transform.SetParent(parent);
        float bandStart = layout.forestStartZ - 120f;
        int rows = Mathf.RoundToInt(Mathf.Lerp(8f, 14f, Mathf.InverseLerp(1f, 3f, qualityBoost)));
        int cols = Mathf.RoundToInt(Mathf.Lerp(24f, 42f, Mathf.InverseLerp(1f, 3f, qualityBoost)));
        for (int z = 0; z < rows; z++)
        {
            for (int x = 0; x < cols; x++)
            {
                float px = Mathf.Lerp(120f, layout.worldSizeMeters - 120f, x / Mathf.Max(1f, cols - 1f));
                float pz = bandStart + z * (layout.worldSizeMeters - bandStart - 120f) / Mathf.Max(1f, rows - 1f);
                GameObject card = new GameObject("F_" + x + "_" + z, typeof(MeshFilter), typeof(MeshRenderer), typeof(CameraFacingBillboard));
                card.transform.SetParent(root.transform);
                card.transform.position = new Vector3(px, 34f + z * 1.8f, pz);
                card.transform.localScale = new Vector3(90f, 70f, 1f);
                card.GetComponent<MeshFilter>().sharedMesh = ctx.quadMesh;
                bool pine = (x + z) % 3 != 0;
                card.GetComponent<MeshRenderer>().sharedMaterial = pine ? ctx.pineMat : ctx.leafMat;
                card.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                card.GetComponent<MeshRenderer>().receiveShadows = false;
                CameraFacingBillboard bb = card.GetComponent<CameraFacingBillboard>();
                bb.yOnly = true;
                bb.yawOffset = 180f;
            }
        }
        return root;
    }

    private GameObject CreateRoadHLODProxy(BuildContext ctx, WorldLayout layout, Transform parent)
    {
        GameObject root = new GameObject("RoadProxy");
        root.transform.SetParent(parent);
        for (int r = 0; r < layout.roads.Count; r++)
        {
            RoadPath road = layout.roads[r];
            for (int i = 0; i < road.points.Count - 1; i++)
            {
                Vector3 a = road.points[i];
                Vector3 b = road.points[i + 1];
                Vector3 mid = (a + b) * 0.5f;
                float len = Vector3.Distance(a, b);
                CreateCubeChild(root.transform, "RoadProxy_" + r + "_" + i, mid + Vector3.up * 0.05f, Quaternion.LookRotation((b - a).normalized), new Vector3(road.width * 0.8f, 0.06f, len), ctx.dirtMat);
            }
        }
        SetShadowsRecursive(root, ShadowCastingMode.Off, false);
        return root;
    }

    private GameObject CreateFieldHLODProxy(BuildContext ctx, WorldLayout layout, Transform parent)
    {
        GameObject root = new GameObject("FieldProxy");
        root.transform.SetParent(parent);
        float cell = layout.farmlandCellSize;
        int radius = Mathf.RoundToInt(Mathf.Lerp(6f, 10f, Mathf.InverseLerp(1f, 3f, qualityBoost)));
        int centerX = Mathf.FloorToInt(layout.villageCenter.x / cell);
        int centerZ = Mathf.FloorToInt(layout.villageCenter.z / cell);
        for (int z = centerZ - radius; z <= centerZ + radius; z++)
        {
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                if (x < 0 || z < 0) continue;
                float px = (x + 0.5f) * cell;
                float pz = (z + 0.5f) * cell;
                if (px > layout.worldSizeMeters || pz > layout.worldSizeMeters) continue;
                if (ComputeVillageMask(layout, px, pz) > 0.16f || ComputeForestMask(layout, px, pz) > 0.16f) continue;

                float border;
                float wheat = ComputeWheatFieldMask(layout, px, pz, out border);
                GameObject card = new GameObject("FieldCard_" + x + "_" + z, typeof(MeshFilter), typeof(MeshRenderer), typeof(CameraFacingBillboard));
                card.transform.SetParent(root.transform);
                card.transform.position = new Vector3(px, 14f, pz);
                card.transform.localScale = new Vector3(cell * 0.88f, cell * 0.24f, 1f);
                card.GetComponent<MeshFilter>().sharedMesh = ctx.quadMesh;
                card.GetComponent<MeshRenderer>().sharedMaterial = wheat > 0.45f ? ctx.wheatBladeMat : ctx.grassBladeMat;
                card.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                card.GetComponent<MeshRenderer>().receiveShadows = false;
                CameraFacingBillboard bb = card.GetComponent<CameraFacingBillboard>();
                bb.yOnly = true;
                bb.yawOffset = 180f;
            }
        }
        SetShadowsRecursive(root, ShadowCastingMode.Off, false);
        return root;
    }

    private List<GameObject> ChunkizeForestChildren(GameObject sourceRoot, float chunkSize, string prefix)
    {
        List<GameObject> result = new List<GameObject>();
        Dictionary<string, GameObject> buckets = new Dictionary<string, GameObject>();
        List<Transform> units = new List<Transform>();
        Transform[] all = sourceRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == sourceRoot.transform)
                continue;
            if (all[i].GetComponent<LODGroup>() != null)
                units.Add(all[i]);
        }

        for (int i = 0; i < units.Count; i++)
        {
            Transform child = units[i];
            Bounds b = CalculateHierarchyBounds(child.gameObject);
            Vector3 c = b.center;
            int cx = Mathf.FloorToInt(c.x / chunkSize);
            int cz = Mathf.FloorToInt(c.z / chunkSize);
            string key = cx + "_" + cz;
            if (!buckets.TryGetValue(key, out GameObject bucket))
            {
                bucket = new GameObject(prefix + "_" + key);
                bucket.transform.SetParent(sourceRoot.transform);
                buckets[key] = bucket;
                result.Add(bucket);
            }
            child.SetParent(bucket.transform, true);
        }
        return result;
    }

    private List<GameObject> ChunkizeChildren(GameObject sourceRoot, float chunkSize, string prefix)
    {
        List<GameObject> result = new List<GameObject>();
        Dictionary<string, GameObject> buckets = new Dictionary<string, GameObject>();
        List<Transform> children = new List<Transform>();
        for (int i = 0; i < sourceRoot.transform.childCount; i++)
            children.Add(sourceRoot.transform.GetChild(i));

        for (int i = 0; i < children.Count; i++)
        {
            Transform child = children[i];
            Bounds b = CalculateHierarchyBounds(child.gameObject);
            Vector3 c = b.center;
            int cx = Mathf.FloorToInt(c.x / chunkSize);
            int cz = Mathf.FloorToInt(c.z / chunkSize);
            string key = cx + "_" + cz;
            if (!buckets.TryGetValue(key, out GameObject bucket))
            {
                bucket = new GameObject(prefix + "_" + key);
                bucket.transform.SetParent(sourceRoot.transform);
                buckets[key] = bucket;
                result.Add(bucket);
            }
            child.SetParent(bucket.transform, true);
        }
        return result;
    }

    private Bounds CalculateHierarchyBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(root.transform.position, Vector3.one * 10f);
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);
        return b;
    }

    private void CreateCloudSystem(BuildContext ctx, WorldLayout layout, Transform parent)
    {
        GameObject root = new GameObject("SkyClouds");
        root.transform.SetParent(parent);
        float baseHeight = Mathf.Max(900f, layout.terrainHeightMeters * 5.5f);
        int layers = Mathf.RoundToInt(Mathf.Lerp(10f, 18f, Mathf.InverseLerp(1f, 3f, qualityBoost)));
        for (int i = 0; i < layers; i++)
        {
            float ring = i % 3;
            float angle = i / (float)Mathf.Max(1, layers) * Mathf.PI * 2f + Hash01(i, 1, seed + 1501) * 0.4f;
            float radius = Mathf.Lerp(layout.villageLengthMeters * 1.4f, layout.worldSizeMeters * 0.34f, ring / 2f);
            Vector3 pos = layout.villageCenter + new Vector3(Mathf.Cos(angle) * radius, baseHeight + ring * 120f + Hash01(i, 2, seed + 1511) * 90f, Mathf.Sin(angle) * radius);
            GameObject cloud = new GameObject("Cloud_" + i, typeof(MeshFilter), typeof(MeshRenderer), typeof(CameraFacingBillboard));
            cloud.transform.SetParent(root.transform);
            cloud.transform.position = pos;
            cloud.transform.localScale = new Vector3(Mathf.Lerp(900f, 1800f, Hash01(i, 3, seed + 1521)), Mathf.Lerp(240f, 420f, Hash01(i, 4, seed + 1531)), 1f);
            cloud.GetComponent<MeshFilter>().sharedMesh = ctx.quadMesh;
            cloud.GetComponent<MeshRenderer>().sharedMaterial = ctx.cloudMat;
            cloud.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            cloud.GetComponent<MeshRenderer>().receiveShadows = false;
            CameraFacingBillboard bb = cloud.GetComponent<CameraFacingBillboard>();
            bb.yOnly = false;
            bb.yawOffset = 180f;
        }
    }

    private void CreateLightingRig(Transform parent, Vector3 center)
    {
        GameObject lighting = new GameObject("LightingRig");
        lighting.transform.SetParent(parent);

        GameObject sunGO = new GameObject("Sun");
        sunGO.transform.SetParent(lighting.transform);
        sunGO.transform.rotation = Quaternion.Euler(34f, -24f, 0f);
        Light sun = sunGO.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.20f;
        sun.color = new Color(1f, 0.96f, 0.91f);
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.92f;
        sun.shadowBias = 0.03f;
        sun.shadowNormalBias = 0.4f;
        RenderSettings.sun = sun;

        GameObject fillGO = new GameObject("SkyFill");
        fillGO.transform.SetParent(lighting.transform);
        fillGO.transform.position = center + new Vector3(0f, 80f, 0f);
        Light fill = fillGO.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.intensity = 0.10f;
        fill.color = new Color(0.70f, 0.80f, 1f);
        fill.shadows = LightShadows.None;
        fill.transform.rotation = Quaternion.Euler(342f, 150f, 0f);
    }

    private void CreateMainCamera(WorldLayout layout)
    {
        GameObject cameraGO = new GameObject("Main Camera");
        Camera cam = cameraGO.AddComponent<Camera>();
        UniversalAdditionalCameraData urp = cameraGO.AddComponent<UniversalAdditionalCameraData>();
        cameraGO.tag = "MainCamera";
        cameraGO.AddComponent<AudioListener>();

        cam.allowHDR = true;
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.fieldOfView = Mathf.Lerp(52f, 47f, Mathf.InverseLerp(1f, 3f, qualityBoost));
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = Mathf.Max(60000f, fogEndKm * 1000f + 5000f);

        urp.renderPostProcessing = true;
        urp.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        urp.antialiasingQuality = AntialiasingQuality.High;
        urp.stopNaN = true;
        urp.dithering = true;

        cameraGO.transform.position = layout.villageCenter + new Vector3(-layout.villageLengthMeters * 0.45f, 42f, -layout.villageLengthMeters * 0.62f);
        cameraGO.transform.rotation = Quaternion.Euler(15f, 38f, 0f);

        GameObject panorama = new GameObject("Panorama Camera");
        Camera panoCam = panorama.AddComponent<Camera>();
        UniversalAdditionalCameraData panoUrp = panorama.AddComponent<UniversalAdditionalCameraData>();
        panoCam.enabled = false;
        panoCam.allowHDR = true;
        panoCam.fieldOfView = 46f;
        panoCam.farClipPlane = Mathf.Max(60000f, fogEndKm * 1000f + 5000f);
        panoUrp.renderPostProcessing = true;
        panoUrp.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        panoUrp.antialiasingQuality = AntialiasingQuality.High;
        panoUrp.dithering = true;
        panorama.transform.position = layout.villageCenter + new Vector3(-layout.villageLengthMeters * 0.10f, 28f, -layout.villageLengthMeters * 0.12f);
        panorama.transform.rotation = Quaternion.Euler(11f, 22f, 0f);
    }

    private void CreateReflectionProbe(WorldLayout layout, Transform parent)
    {
        GameObject probeGO = new GameObject("ReflectionProbe");
        probeGO.transform.SetParent(parent);
        probeGO.transform.position = layout.villageCenter + Vector3.up * 18f;
        ReflectionProbe probe = probeGO.AddComponent<ReflectionProbe>();
        probe.mode = ReflectionProbeMode.Realtime;
        probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
        probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
        probe.size = new Vector3(layout.villageLengthMeters * 2.4f, 120f, layout.villageLengthMeters * 2.4f);
        probe.intensity = 1f;
    }

    private void ConfigureEnvironment()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.72f, 0.80f, 0.90f);
        RenderSettings.ambientEquatorColor = new Color(0.51f, 0.58f, 0.49f);
        RenderSettings.ambientGroundColor = new Color(0.21f, 0.23f, 0.18f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = fogStartKm * 1000f;
        RenderSettings.fogEndDistance = Mathf.Max(RenderSettings.fogStartDistance + 1000f, fogEndKm * 1000f);
        RenderSettings.fogColor = new Color(0.68f, 0.75f, 0.82f);
        QualitySettings.shadowCascades = 4;
        QualitySettings.shadowDistance = 420f + qualityBoost * 140f;
        QualitySettings.lodBias = 3.1f + qualityBoost * 1.1f;
        QualitySettings.enableLODCrossFade = true;
        QualitySettings.antiAliasing = 8;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        QualitySettings.globalTextureMipmapLimit = 0;
    }

    private BuildContext PrepareAssets()
    {
        BuildContext ctx = new BuildContext();
        ctx.rootFolder = outputRoot;
        ctx.materialsFolder = outputRoot + "/Materials";
        ctx.texturesFolder = outputRoot + "/Textures";
        ctx.meshesFolder = outputRoot + "/Meshes";
        ctx.volumesFolder = outputRoot + "/Volumes";
        ctx.terrainFolder = outputRoot + "/Terrain";

        EnsureFolder(ctx.rootFolder);
        EnsureFolder(ctx.materialsFolder);
        EnsureFolder(ctx.texturesFolder);
        EnsureFolder(ctx.meshesFolder);
        EnsureFolder(ctx.volumesFolder);
        EnsureFolder(ctx.terrainFolder);

        ctx.litShader = FindBestLitShader();
        ctx.terrainShader = FindBestTerrainShader();

        int texSize = qualityBoost >= 2f ? 512 : 256;
        ctx.grassTex = CreateOrReplaceTextureAsset(ctx.texturesFolder + "/T_Grass.asset", texSize, texSize, new Color(0.28f, 0.50f, 0.22f), new Color(0.35f, 0.58f, 0.24f), 0.18f, 5.6f, false);
        ctx.wheatTex = CreateOrReplaceTextureAsset(ctx.texturesFolder + "/T_Wheat.asset", texSize, texSize, new Color(0.61f, 0.53f, 0.20f), new Color(0.75f, 0.66f, 0.26f), 0.10f, 7.2f, false);
        ctx.dirtTex = CreateOrReplaceTextureAsset(ctx.texturesFolder + "/T_Dirt.asset", texSize, texSize, new Color(0.39f, 0.30f, 0.20f), new Color(0.49f, 0.38f, 0.26f), 0.15f, 6.4f, false);
        ctx.forestTex = CreateOrReplaceTextureAsset(ctx.texturesFolder + "/T_Forest.asset", texSize, texSize, new Color(0.17f, 0.29f, 0.13f), new Color(0.22f, 0.35f, 0.15f), 0.16f, 6.1f, true);
        ctx.cloudTex = CreateOrReplaceCloudTextureAsset(ctx.texturesFolder + "/T_Clouds.asset", texSize, texSize);

        ctx.terrainMat = CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_TerrainURP.mat", Color.white, 0f, 0.02f, ctx.terrainShader != null ? ctx.terrainShader : ctx.litShader);
        ctx.roadMat = CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_Road.mat", new Color(0.42f, 0.35f, 0.26f), 0f, 0.06f, ctx.litShader);
        ctx.shoulderMat = CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_Shoulder.mat", new Color(0.55f, 0.55f, 0.54f), 0f, 0.18f, ctx.litShader);
        ctx.grassPreviewMat = CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_GrassPreview.mat", new Color(0.31f, 0.53f, 0.25f), 0f, 0.04f, ctx.litShader);
        ctx.wheatPreviewMat = CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_WheatPreview.mat", new Color(0.74f, 0.66f, 0.28f), 0f, 0.04f, ctx.litShader);
        ctx.dirtMat = CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_Dirt.mat", new Color(0.42f, 0.33f, 0.22f), 0f, 0.03f, ctx.litShader);
        ctx.wallCreamMat = CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_WallCream.mat", new Color(0.82f, 0.77f, 0.68f), 0f, 0.18f, ctx.litShader);
        ctx.wallWarmMat = CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_WallWarm.mat", new Color(0.72f, 0.63f, 0.53f), 0f, 0.16f, ctx.litShader);
        ctx.timberMat = CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_Timber.mat", new Color(0.29f, 0.18f, 0.10f), 0f, 0.22f, ctx.litShader);
        ctx.roofRedMat = CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_RoofRed.mat", new Color(0.47f, 0.19f, 0.11f), 0f, 0.28f, ctx.litShader);
        ctx.roofDarkMat = CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_RoofDark.mat", new Color(0.21f, 0.19f, 0.18f), 0f, 0.24f, ctx.litShader);
        ctx.stoneMat = CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_Stone.mat", new Color(0.56f, 0.54f, 0.52f), 0f, 0.20f, ctx.litShader);
        ctx.barkMat = CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_Bark.mat", new Color(0.24f, 0.16f, 0.10f), 0f, 0.16f, ctx.litShader);
        ctx.pineMat = CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_Pine.mat", new Color(0.14f, 0.28f, 0.15f), 0f, 0.10f, ctx.litShader);
        ctx.leafMat = CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_Leaf.mat", new Color(0.24f, 0.44f, 0.19f), 0f, 0.10f, ctx.litShader);
        ctx.glassMat = CreateTransparentMaterialAsset(ctx.materialsFolder + "/M_Glass.mat", new Color(0.72f, 0.83f, 0.92f, 0.42f), 0f, 0.68f, ctx.litShader);
        ctx.wheatBaleMat = CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_WheatBale.mat", new Color(0.75f, 0.66f, 0.30f), 0f, 0.08f, ctx.litShader);
        ctx.waterMat = CreateTransparentMaterialAsset(ctx.materialsFolder + "/M_Water.mat", new Color(0.22f, 0.41f, 0.58f, 0.78f), 0f, 0.86f, ctx.litShader);
        ctx.grassBladeMat = CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_GrassBlade.mat", new Color(0.31f, 0.56f, 0.25f), 0f, 0.02f, ctx.litShader);
        ctx.wheatBladeMat = CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_WheatBlade.mat", new Color(0.77f, 0.69f, 0.30f), 0f, 0.03f, ctx.litShader);
        ctx.logWallMat = CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_LogWall.mat", new Color(0.34f, 0.23f, 0.13f), 0f, 0.18f, ctx.litShader);
        ctx.copperRoofMat = CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_CopperRoof.mat", new Color(0.26f, 0.46f, 0.33f), 0f, 0.22f, ctx.litShader);
        ctx.forgeFireMat = CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_ForgeFire.mat", new Color(0.95f, 0.44f, 0.10f), 0f, 0.78f, ctx.litShader);
        ctx.cloudMat = CreateTransparentMaterialAsset(ctx.materialsFolder + "/M_Cloud.mat", new Color(1f, 1f, 1f, 0.78f), 0f, 0.10f, ctx.litShader);

        EnableEmission(ctx.glassMat, new Color(0.08f, 0.12f, 0.15f));
        EnableEmission(ctx.forgeFireMat, new Color(1.2f, 0.35f, 0.05f));
        SetTextureSafe(ctx.cloudMat, "_BaseMap", ctx.cloudTex);
        SetTextureSafe(ctx.cloudMat, "_MainTex", ctx.cloudTex);
        EnableInstancing(
            ctx.terrainMat, ctx.roadMat, ctx.shoulderMat, ctx.grassPreviewMat, ctx.wheatPreviewMat, ctx.dirtMat,
            ctx.wallCreamMat, ctx.wallWarmMat, ctx.timberMat, ctx.roofRedMat, ctx.roofDarkMat, ctx.stoneMat,
            ctx.barkMat, ctx.pineMat, ctx.leafMat, ctx.glassMat, ctx.wheatBaleMat, ctx.waterMat, ctx.grassBladeMat, ctx.wheatBladeMat, ctx.logWallMat, ctx.copperRoofMat, ctx.forgeFireMat, ctx.cloudMat);

        ctx.grassLayer = CreateOrReplaceTerrainLayer(outputRoot + "/TerrainLayer_Grass.terrainlayer", ctx.grassTex, new Vector2(32f, 32f), 0.02f);
        ctx.wheatLayer = CreateOrReplaceTerrainLayer(outputRoot + "/TerrainLayer_Wheat.terrainlayer", ctx.wheatTex, new Vector2(44f, 44f), 0.02f);
        ctx.dirtLayer = CreateOrReplaceTerrainLayer(outputRoot + "/TerrainLayer_Dirt.terrainlayer", ctx.dirtTex, new Vector2(18f, 18f), 0.02f);
        ctx.forestLayer = CreateOrReplaceTerrainLayer(outputRoot + "/TerrainLayer_Forest.terrainlayer", ctx.forestTex, new Vector2(26f, 26f), 0.02f);

        ctx.roofMesh = CreateOrReplaceMeshAsset(ctx.meshesFolder + "/MESH_RoofPrism.asset", CreateRoofPrismMesh());
        ctx.coneMesh = CreateOrReplaceMeshAsset(ctx.meshesFolder + "/MESH_Cone.asset", CreateConeMesh(18));
        ctx.grassBladeMesh = CreateOrReplaceMeshAsset(ctx.meshesFolder + "/MESH_GrassBlade.asset", CreateGrassBladeMesh());
        ctx.wheatBladeMesh = CreateOrReplaceMeshAsset(ctx.meshesFolder + "/MESH_WheatBlade.asset", CreateWheatBladeMesh());
        ctx.quadMesh = CreateOrReplaceMeshAsset(ctx.meshesFolder + "/MESH_Quad.asset", CreateQuadMesh());
        ctx.grassDetailPrefab = CreateOrReplaceDetailPrefab(ctx.rootFolder + "/PF_GrassDetail.prefab", ctx.grassBladeMesh, ctx.grassBladeMat);
        ctx.wheatDetailPrefab = CreateOrReplaceDetailPrefab(ctx.rootFolder + "/PF_WheatDetail.prefab", ctx.wheatBladeMesh, ctx.wheatBladeMat);
        ctx.globalVolumeProfile = CreateOrReplaceVolumeProfile(ctx.volumesFolder + "/AAA_Rural_GlobalProfile.asset");
        SetupURPVolumeProfile(ctx.globalVolumeProfile);

        return ctx;
    }

    private float ComputeVillageMask(WorldLayout layout, float worldX, float worldZ)
    {
        float dx = (worldX - layout.villageCenter.x) / (layout.villageLengthMeters * 0.72f);
        float dz = (worldZ - layout.villageCenter.z) / (layout.villageHalfWidthMeters * 1.7f);
        float d = Mathf.Sqrt(dx * dx + dz * dz);
        return 1f - Mathf.Clamp01((d - 0.55f) / 0.55f);
    }

    private float ComputeForestMask(WorldLayout layout, float worldX, float worldZ)
    {
        float boundary = layout.forestStartZ + (Mathf.PerlinNoise(worldX * 0.00035f + seed * 0.011f, 0.15f) - 0.5f) * 420f;
        return Mathf.Clamp01((worldZ - (boundary - 180f)) / 480f);
    }

    private float ComputeLakeMask(WorldLayout layout, float worldX, float worldZ)
    {
        float dx = (worldX - layout.lakeCenter.x) / layout.lakeRadiusX;
        float dz = (worldZ - layout.lakeCenter.z) / layout.lakeRadiusZ;
        float d = Mathf.Sqrt(dx * dx + dz * dz);
        float shoreNoise = (Mathf.PerlinNoise(worldX * 0.0018f + 12f, worldZ * 0.0018f + 41f) - 0.5f) * 0.08f;
        d += shoreNoise;
        return 1f - Mathf.Clamp01((d - 0.76f) / 0.30f);
    }

    private float ComputeRiverMask(WorldLayout layout, float worldX, float worldZ)
    {
        if (layout.riverPoints.Count < 2)
            return 0f;
        float d = DistancePointPolylineXZ(new Vector3(worldX, 0f, worldZ), layout.riverPoints);
        return 1f - Mathf.Clamp01((d - layout.riverWidth * 0.42f) / (layout.riverWidth * 1.15f));
    }

    private float ComputeRoadMask(WorldLayout layout, float worldX, float worldZ)
    {
        float mask = 0f;
        Vector3 p = new Vector3(worldX, 0f, worldZ);
        for (int i = 0; i < layout.roads.Count; i++)
        {
            float d = DistancePointPolylineXZ(p, layout.roads[i]);
            float t = 1f - Mathf.Clamp01((d - layout.roads[i].width * 0.28f) / (layout.roads[i].width * 1.1f));
            mask = Mathf.Max(mask, t);
        }
        return mask;
    }

    private float ComputeHouseMask(WorldLayout layout, float worldX, float worldZ)
    {
        float mask = 0f;
        Vector2 p = new Vector2(worldX, worldZ);
        for (int i = 0; i < layout.houses.Count; i++)
        {
            float rr = Mathf.Max(layout.houses[i].footprint.x, layout.houses[i].footprint.y) * 0.9f;
            float d = Vector2.Distance(p, new Vector2(layout.houses[i].position.x, layout.houses[i].position.z));
            float t = 1f - Mathf.Clamp01((d - rr * 0.35f) / (rr * 0.85f));
            mask = Mathf.Max(mask, t);
        }
        return mask;
    }

    private float ComputeFieldSofteningMask(WorldLayout layout, float worldX, float worldZ)
    {
        if (ComputeForestMask(layout, worldX, worldZ) > 0.15f)
            return 0f;
        if (ComputeVillageMask(layout, worldX, worldZ) > 0.20f)
            return 0f;
        if (ComputeLakeMask(layout, worldX, worldZ) > 0.08f || ComputeRiverMask(layout, worldX, worldZ) > 0.10f)
            return 0f;
        return 1f;
    }

    private float ComputeWheatFieldMask(WorldLayout layout, float worldX, float worldZ, out float fieldBorder)
    {
        fieldBorder = 0f;
        if (ComputeForestMask(layout, worldX, worldZ) > 0.2f)
            return 0f;
        if (ComputeVillageMask(layout, worldX, worldZ) > 0.18f)
            return 0f;
        if (ComputeLakeMask(layout, worldX, worldZ) > 0.06f || ComputeRiverMask(layout, worldX, worldZ) > 0.10f)
            return 0f;

        float cell = layout.farmlandCellSize;
        int cx = Mathf.FloorToInt(worldX / cell);
        int cz = Mathf.FloorToInt(worldZ / cell);
        float fracX = Mathf.Abs((worldX / cell) - cx - 0.5f) * 2f;
        float fracZ = Mathf.Abs((worldZ / cell) - cz - 0.5f) * 2f;
        float inside = 1f - Mathf.Clamp01((Mathf.Max(fracX, fracZ) - 0.82f) / 0.18f);
        fieldBorder = 1f - inside;

        float parcelHash = Hash01(cx, cz, seed + 71);
        return parcelHash < wheatRatio ? inside : 0f;
    }

    private float SampleTerrainHeight(TerrainGrid grid, Vector3 worldPos)
    {
        int tx = Mathf.Clamp(Mathf.FloorToInt(worldPos.x / grid.tileSize), 0, grid.tileCount - 1);
        int tz = Mathf.Clamp(Mathf.FloorToInt(worldPos.z / grid.tileSize), 0, grid.tileCount - 1);
        Terrain terrain = grid.terrains[tx, tz];
        if (terrain == null)
            return 0f;
        return terrain.SampleHeight(worldPos);
    }

    private static float DetermineTileSize(float targetSideMeters)
    {
        return targetSideMeters > 7000f ? 2048f : 1024f;
    }

    private static float GetPathLength(RoadPath road)
    {
        float length = 0f;
        for (int i = 0; i < road.points.Count - 1; i++)
            length += Vector3.Distance(road.points[i], road.points[i + 1]);
        return length;
    }

    private static Vector3 SamplePath(RoadPath road, float distance)
    {
        if (road.points.Count == 0)
            return Vector3.zero;
        if (road.points.Count == 1)
            return road.points[0];

        float remaining = Mathf.Max(0f, distance);
        for (int i = 0; i < road.points.Count - 1; i++)
        {
            Vector3 a = road.points[i];
            Vector3 b = road.points[i + 1];
            float len = Vector3.Distance(a, b);
            if (remaining <= len)
                return Vector3.Lerp(a, b, len > 0.0001f ? remaining / len : 0f);
            remaining -= len;
        }

        return road.points[road.points.Count - 1];
    }

    private static Vector3 DirectionOnPath(RoadPath road, float distance)
    {
        if (road.points.Count < 2)
            return Vector3.forward;

        float remaining = Mathf.Max(0f, distance);
        for (int i = 0; i < road.points.Count - 1; i++)
        {
            Vector3 a = road.points[i];
            Vector3 b = road.points[i + 1];
            float len = Vector3.Distance(a, b);
            if (remaining <= len)
                return (b - a).normalized;
            remaining -= len;
        }
        return (road.points[road.points.Count - 1] - road.points[road.points.Count - 2]).normalized;
    }

    private static Vector3 SamplePolyline(List<Vector3> points, float distance)
    {
        if (points == null || points.Count == 0)
            return Vector3.zero;
        if (points.Count == 1)
            return points[0];
        float remaining = Mathf.Max(0f, distance);
        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 a = points[i];
            Vector3 b = points[i + 1];
            float len = Vector3.Distance(a, b);
            if (remaining <= len)
                return Vector3.Lerp(a, b, len > 0.0001f ? remaining / len : 0f);
            remaining -= len;
        }
        return points[points.Count - 1];
    }

    private static Vector3 DirectionOnPolyline(List<Vector3> points, float distance)
    {
        if (points == null || points.Count < 2)
            return Vector3.forward;
        float remaining = Mathf.Max(0f, distance);
        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 a = points[i];
            Vector3 b = points[i + 1];
            float len = Vector3.Distance(a, b);
            if (remaining <= len)
                return (b - a).normalized;
            remaining -= len;
        }
        return (points[points.Count - 1] - points[points.Count - 2]).normalized;
    }

    private static float DistancePointPolylineXZ(Vector3 point, List<Vector3> points)
    {
        float best = float.MaxValue;
        for (int i = 0; i < points.Count - 1; i++)
        {
            float d = DistancePointSegmentXZ(point, points[i], points[i + 1]);
            if (d < best)
                best = d;
        }
        return best;
    }

    private static float DistancePointPolylineXZ(Vector3 point, RoadPath road)
    {
        float best = float.MaxValue;
        for (int i = 0; i < road.points.Count - 1; i++)
        {
            float d = DistancePointSegmentXZ(point, road.points[i], road.points[i + 1]);
            if (d < best)
                best = d;
        }
        return best;
    }

    private static float DistancePointSegmentXZ(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector2 pp = new Vector2(p.x, p.z);
        Vector2 aa = new Vector2(a.x, a.z);
        Vector2 bb = new Vector2(b.x, b.z);
        Vector2 ab = bb - aa;
        float len = ab.sqrMagnitude;
        if (len < 0.0001f)
            return Vector2.Distance(pp, aa);
        float t = Mathf.Clamp01(Vector2.Dot(pp - aa, ab) / len);
        return Vector2.Distance(pp, aa + ab * t);
    }

    private static float FBM(float x, float y, int octaves, float persistence, float lacunarity)
    {
        float amplitude = 1f;
        float frequency = 1f;
        float sum = 0f;
        float max = 0f;
        for (int i = 0; i < octaves; i++)
        {
            sum += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
            max += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }
        return max > 0f ? sum / max : 0f;
    }

    private static float Hash01(int x, int z, int s)
    {
        uint h = (uint)(x * 374761393 + z * 668265263 + s * 224682251);
        h = (h ^ (h >> 13)) * 1274126177u;
        return (h & 0x00FFFFFF) / 16777215f;
    }

    private static Shader FindBestLitShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Diffuse");
        return shader;
    }

    private static Shader FindBestTerrainShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
        if (shader == null) shader = Shader.Find("Nature/Terrain/Standard");
        if (shader == null) shader = Shader.Find("Standard");
        return shader;
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        if (parts.Length == 0 || parts[0] != "Assets")
            throw new Exception("Path must start with Assets: " + path);

        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static Material CreateOpaqueMaterialAsset(string path, Color color, float metallic, float smoothness, Shader shader)
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
            AssetDatabase.DeleteAsset(path);

        Material mat = new Material(shader);
        mat.name = Path.GetFileNameWithoutExtension(path);
        SetColorSafe(mat, "_BaseColor", color);
        SetColorSafe(mat, "_Color", color);
        SetFloatSafe(mat, "_Metallic", metallic);
        SetFloatSafe(mat, "_Smoothness", smoothness);
        SetFloatSafe(mat, "_Glossiness", smoothness);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static Material CreateTransparentMaterialAsset(string path, Color color, float metallic, float smoothness, Shader shader)
    {
        Material mat = CreateOpaqueMaterialAsset(path, color, metallic, smoothness, shader);
        ConfigureURPTransparent(mat, color);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void ConfigureURPTransparent(Material mat, Color color)
    {
        if (mat == null) return;
        SetColorSafe(mat, "_BaseColor", color);
        SetColorSafe(mat, "_Color", color);
        SetFloatSafe(mat, "_Surface", 1f);
        SetFloatSafe(mat, "_Blend", 0f);
        SetFloatSafe(mat, "_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        SetFloatSafe(mat, "_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        SetFloatSafe(mat, "_ZWrite", 0f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = (int)RenderQueue.Transparent;
    }

    private static Texture2D CreateOrReplaceCloudTextureAsset(string path, int width, int height)
    {
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null)
            AssetDatabase.DeleteAsset(path);

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true)
        {
            name = Path.GetFileNameWithoutExtension(path),
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = x / (float)(width - 1);
                float ny = y / (float)(height - 1);
                float fx = nx * 2f - 1f;
                float fy = ny * 2f - 1f;
                float radial = 1f - Mathf.Clamp01(Mathf.Sqrt(fx * fx + fy * fy));
                float n = FBM(nx * 5.2f + 14f, ny * 5.2f + 27f, 4, 0.5f, 2f);
                float alpha = Mathf.Clamp01((n - 0.42f) * 2.1f) * Mathf.Pow(radial, 1.3f);
                pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply(true, false);
        AssetDatabase.CreateAsset(tex, path);
        return tex;
    }

    private static Texture2D CreateOrReplaceTextureAsset(string path, int width, int height, Color a, Color b, float noiseAmount, float scale, bool highContrast)
    {
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null)
            AssetDatabase.DeleteAsset(path);

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true)
        {
            name = Path.GetFileNameWithoutExtension(path),
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear
        };

        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = x / (float)(width - 1);
                float ny = y / (float)(height - 1);
                float n1 = Mathf.PerlinNoise(nx * scale, ny * scale);
                float n2 = Mathf.PerlinNoise(nx * scale * 2.1f + 23.1f, ny * scale * 2.1f + 57.8f);
                float n = Mathf.Lerp(n1, n2, 0.45f);
                if (highContrast)
                    n = Mathf.Pow(n, 1.35f);
                Color c = Color.Lerp(a, b, n);
                c += new Color(noiseAmount * (n2 - 0.5f), noiseAmount * (n1 - 0.5f), noiseAmount * (n - 0.5f), 0f);
                pixels[y * width + x] = c;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(true, false);
        AssetDatabase.CreateAsset(tex, path);
        return tex;
    }

    private static TerrainLayer CreateOrReplaceTerrainLayer(string path, Texture2D diffuse, Vector2 tileSize, float smoothness)
    {
        TerrainLayer existing = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
        if (existing != null)
            AssetDatabase.DeleteAsset(path);

        TerrainLayer layer = new TerrainLayer
        {
            diffuseTexture = diffuse,
            tileSize = tileSize,
            tileOffset = Vector2.zero,
            specular = Color.black,
            metallic = 0f,
            smoothness = smoothness,
            diffuseRemapMax = Color.white,
            diffuseRemapMin = Color.black
        };
        AssetDatabase.CreateAsset(layer, path);
        return layer;
    }

    private static Mesh CreateOrReplaceMeshAsset(string path, Mesh mesh)
    {
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
            AssetDatabase.DeleteAsset(path);
        mesh.name = Path.GetFileNameWithoutExtension(path);
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    private static VolumeProfile CreateOrReplaceVolumeProfile(string path)
    {
        VolumeProfile existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (existing != null)
            AssetDatabase.DeleteAsset(path);

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = Path.GetFileNameWithoutExtension(path);
        AssetDatabase.CreateAsset(profile, path);
        return profile;
    }

    private static void SetupURPVolumeProfile(VolumeProfile profile)
    {
        if (profile == null)
            return;

        Bloom bloom = profile.Add<Bloom>(true);
        bloom.active = true;
        bloom.threshold.overrideState = true;
        bloom.threshold.value = 0.95f;
        bloom.intensity.overrideState = true;
        bloom.intensity.value = 0.42f;
        bloom.scatter.overrideState = true;
        bloom.scatter.value = 0.72f;

        ColorAdjustments colorAdjustments = profile.Add<ColorAdjustments>(true);
        colorAdjustments.active = true;
        colorAdjustments.postExposure.overrideState = true;
        colorAdjustments.postExposure.value = 0.18f;
        colorAdjustments.contrast.overrideState = true;
        colorAdjustments.contrast.value = 12f;
        colorAdjustments.saturation.overrideState = true;
        colorAdjustments.saturation.value = 8f;

        Tonemapping tonemapping = profile.Add<Tonemapping>(true);
        tonemapping.active = true;
        tonemapping.mode.overrideState = true;
        tonemapping.mode.value = TonemappingMode.ACES;

        Vignette vignette = profile.Add<Vignette>(true);
        vignette.active = true;
        vignette.intensity.overrideState = true;
        vignette.intensity.value = 0.12f;
        vignette.smoothness.overrideState = true;
        vignette.smoothness.value = 0.40f;

        WhiteBalance whiteBalance = profile.Add<WhiteBalance>(true);
        whiteBalance.active = true;
        whiteBalance.temperature.overrideState = true;
        whiteBalance.temperature.value = 2f;
        whiteBalance.tint.overrideState = true;
        whiteBalance.tint.value = -1f;
    }

    private static void CreateGlobalVolume(BuildContext ctx, Transform parent)
    {
        GameObject go = new GameObject("Global Volume");
        go.transform.SetParent(parent);
        Volume volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        volume.sharedProfile = ctx.globalVolumeProfile;
    }

    private static DetailPrototype CreateGrassDetailPrototype(BuildContext ctx)
    {
        return new DetailPrototype
        {
            prototype = ctx.grassDetailPrefab,
            usePrototypeMesh = true,
            renderMode = DetailRenderMode.VertexLit,
            minWidth = 0.9f,
            maxWidth = 1.4f,
            minHeight = 0.8f,
            maxHeight = 1.35f,
            noiseSpread = 0.2f,
            healthyColor = Color.white,
            dryColor = Color.white,
            useInstancing = true
        };
    }

    private static DetailPrototype CreateWheatDetailPrototype(BuildContext ctx)
    {
        return new DetailPrototype
        {
            prototype = ctx.wheatDetailPrefab,
            usePrototypeMesh = true,
            renderMode = DetailRenderMode.VertexLit,
            minWidth = 1.0f,
            maxWidth = 1.25f,
            minHeight = 1.2f,
            maxHeight = 1.6f,
            noiseSpread = 0.25f,
            healthyColor = Color.white,
            dryColor = Color.white,
            useInstancing = true
        };
    }

    private static GameObject CreateOrReplaceDetailPrefab(string path, Mesh mesh, Material material)
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
            AssetDatabase.DeleteAsset(path);

        GameObject temp = new GameObject(Path.GetFileNameWithoutExtension(path), typeof(MeshFilter), typeof(MeshRenderer));
        temp.GetComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer mr = temp.GetComponent<MeshRenderer>();
        mr.sharedMaterial = material;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = true;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
        GameObject.DestroyImmediate(temp);
        return prefab;
    }

    private static void EnableEmission(Material material, Color emissionColor)
    {
        if (material == null) return;
        material.EnableKeyword("_EMISSION");
        SetColorSafe(material, "_EmissionColor", emissionColor);
    }

    private static void EnableInstancing(params Material[] materials)
    {
        foreach (Material mat in materials)
            if (mat != null)
                mat.enableInstancing = true;
    }

    private static void SetColorSafe(Material mat, string property, Color value)
    {
        if (mat != null && mat.HasProperty(property))
            mat.SetColor(property, value);
    }

    private static void SetFloatSafe(Material mat, string property, float value)
    {
        if (mat != null && mat.HasProperty(property))
            mat.SetFloat(property, value);
    }

    private static void SetTextureSafe(Material mat, string property, Texture value)
    {
        if (mat != null && mat.HasProperty(property))
            mat.SetTexture(property, value);
    }

    private static Mesh CreateGrassBladeMesh()
    {
        Mesh mesh = new Mesh();
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        AddCrossQuad(verts, tris, 0.05f, 0.75f, 0f, 0f, 0f);
        AddCrossQuad(verts, tris, 0.04f, 0.58f, 0.12f, 18f, 0.08f);
        AddCrossQuad(verts, tris, 0.035f, 0.52f, -0.10f, -16f, -0.06f);
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh CreateWheatBladeMesh()
    {
        Mesh mesh = new Mesh();
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        AddCrossQuad(verts, tris, 0.04f, 1.1f, 0f, 0f, 0f);
        AddCrossQuad(verts, tris, 0.035f, 0.95f, 0.08f, 14f, 0.02f);
        AddCrossQuad(verts, tris, 0.035f, 0.92f, -0.07f, -12f, -0.02f);
        AddEarQuad(verts, tris, new Vector3(0f, 1.08f, 0.02f), 0.10f, 0.28f, 0f);
        AddEarQuad(verts, tris, new Vector3(0f, 1.00f, -0.02f), 0.08f, 0.22f, 18f);
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh CreateQuadMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, 0f, 0f),
            new Vector3(0.5f, 0f, 0f),
            new Vector3(-0.5f, 1f, 0f),
            new Vector3(0.5f, 1f, 0f)
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };
        mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh CreateDiscMesh(int segments, float radius)
    {
        Mesh mesh = new Mesh();
        List<Vector3> verts = new List<Vector3> { Vector3.zero };
        List<int> tris = new List<int>();
        for (int i = 0; i < segments; i++)
        {
            float a = i / (float)segments * Mathf.PI * 2f;
            verts.Add(new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
        }
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            tris.Add(0); tris.Add(next + 1); tris.Add(i + 1);
        }
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void AddCrossQuad(List<Vector3> verts, List<int> tris, float halfWidth, float height, float xOffset, float yawDeg, float zOffset)
    {
        AddQuad(verts, tris, new Vector3(xOffset, 0f, zOffset), halfWidth, height, yawDeg);
        AddQuad(verts, tris, new Vector3(xOffset, 0f, zOffset), halfWidth, height, yawDeg + 90f);
    }

    private static void AddEarQuad(List<Vector3> verts, List<int> tris, Vector3 center, float halfWidth, float height, float yawDeg)
    {
        AddQuad(verts, tris, center, halfWidth, height, yawDeg);
    }

    private static void AddQuad(List<Vector3> verts, List<int> tris, Vector3 center, float halfWidth, float height, float yawDeg)
    {
        Quaternion rot = Quaternion.Euler(0f, yawDeg, 0f);
        Vector3 right = rot * Vector3.right * halfWidth;
        int start = verts.Count;
        verts.Add(center - right);
        verts.Add(center + right);
        verts.Add(center + Vector3.up * height + right * 0.15f);
        verts.Add(center + Vector3.up * height - right * 0.15f);
        tris.Add(start + 0); tris.Add(start + 2); tris.Add(start + 1);
        tris.Add(start + 0); tris.Add(start + 3); tris.Add(start + 2);
        tris.Add(start + 1); tris.Add(start + 2); tris.Add(start + 0);
        tris.Add(start + 2); tris.Add(start + 3); tris.Add(start + 0);
    }

    private static Mesh CreateRoofPrismMesh()
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices =
        {
            new Vector3(-0.5f, 0f, -0.5f),
            new Vector3(0f, 1f, -0.5f),
            new Vector3(0.5f, 0f, -0.5f),
            new Vector3(-0.5f, 0f, 0.5f),
            new Vector3(0f, 1f, 0.5f),
            new Vector3(0.5f, 0f, 0.5f)
        };
        int[] triangles = { 0,1,2, 5,4,3, 0,3,4, 0,4,1, 1,4,5, 1,5,2 };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh CreateConeMesh(int segments)
    {
        Mesh mesh = new Mesh();
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        verts.Add(new Vector3(0f, 1f, 0f));
        for (int i = 0; i < segments; i++)
        {
            float a = i / (float)segments * Mathf.PI * 2f;
            verts.Add(new Vector3(Mathf.Cos(a) * 0.5f, 0f, Mathf.Sin(a) * 0.5f));
        }

        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            tris.Add(0); tris.Add(i + 1); tris.Add(next + 1);
        }

        int center = verts.Count;
        verts.Add(Vector3.zero);
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            tris.Add(center); tris.Add(next + 1); tris.Add(i + 1);
        }

        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static GameObject CreateCubeChild(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material material)
    {
        return CreateCubeChild(parent, name, localPos, Quaternion.identity, localScale, material);
    }

    private static GameObject CreateCubeChild(Transform parent, string name, Vector3 localPos, Quaternion localRot, Vector3 localScale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        go.transform.localScale = localScale;
        Collider col = go.GetComponent<Collider>();
        if (col != null) DestroyImmediate(col);
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = material;
        mr.shadowCastingMode = ShadowCastingMode.On;
        mr.receiveShadows = true;
        return go;
    }

    private static GameObject CreateSphereChild(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        Collider col = go.GetComponent<Collider>();
        if (col != null) DestroyImmediate(col);
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = material;
        mr.shadowCastingMode = ShadowCastingMode.On;
        mr.receiveShadows = true;
        return go;
    }

    private static GameObject CreateCylinderChild(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material material)
    {
        return CreateCylinderChild(parent, name, localPos, Quaternion.identity, localScale, material);
    }

    private static GameObject CreateCylinderChild(Transform parent, string name, Vector3 localPos, Quaternion localRot, Vector3 localScale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        go.transform.localScale = localScale;
        Collider col = go.GetComponent<Collider>();
        if (col != null) DestroyImmediate(col);
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = material;
        mr.shadowCastingMode = ShadowCastingMode.On;
        mr.receiveShadows = true;
        return go;
    }

    private static GameObject CreateMeshChild(Transform parent, string name, Mesh mesh, Vector3 localPos, Vector3 localScale, Material material)
    {
        GameObject go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = localScale;
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = material;
        mr.shadowCastingMode = ShadowCastingMode.On;
        mr.receiveShadows = true;
        return go;
    }

    private void SetupBuildingLOD(BuildContext ctx, GameObject root, HouseSpec spec, Material wall, Material roof, float w, float h, float d, float roofH)
    {
        GameObject lod0 = new GameObject("LOD0");
        lod0.transform.SetParent(root.transform, false);
        MoveAllChildrenExcept(root.transform, lod0.transform, null);

        GameObject lod1 = new GameObject("LOD1");
        lod1.transform.SetParent(root.transform, false);
        CreateCubeChild(lod1.transform, "Body", new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, d), wall);
        CreateMeshChild(lod1.transform, "Roof", ctx.roofMesh, new Vector3(0f, h + 0.02f, 0f), new Vector3(w * 1.10f, roofH * 0.86f, d * 1.10f), roof);
        if (spec.kind == BuildingKind.Chapel)
            CreateCubeChild(lod1.transform, "Tower", new Vector3(0f, h + 1.3f, -d * 0.18f), new Vector3(1.0f, 2.1f, 1.0f), wall);
        else if (spec.kind == BuildingKind.Mill)
            CreateCylinderChild(lod1.transform, "Tower", new Vector3(0f, h * 0.60f, 0f), new Vector3(w * 0.10f, h * 0.60f, d * 0.10f), wall);

        GameObject lod2 = new GameObject("LOD2");
        lod2.transform.SetParent(root.transform, false);
        CreateCubeChild(lod2.transform, "Silhouette", new Vector3(0f, h * 0.5f, 0f), new Vector3(w * 0.96f, h * 0.92f, d * 0.96f), wall);
        CreateCubeChild(lod2.transform, "Top", new Vector3(0f, h + roofH * 0.34f, 0f), new Vector3(w * 0.72f, roofH * 0.68f, d * 0.72f), roof);
        SetShadowsRecursive(lod1, ShadowCastingMode.Off, false);
        SetShadowsRecursive(lod2, ShadowCastingMode.Off, false);

        ApplyLODGroup(root, lod0, lod1, lod2, 0.78f, 0.34f, 0.09f);
    }

    private void SetupTreeLOD(BuildContext ctx, GameObject root, bool pine)
    {
        if (root.GetComponent<LODGroup>() != null)
            return;

        GameObject lod0 = new GameObject("LOD0");
        lod0.transform.SetParent(root.transform, false);
        MoveAllChildrenExcept(root.transform, lod0.transform, null);

        GameObject lod1 = new GameObject("LOD1");
        lod1.transform.SetParent(root.transform, false);
        if (pine)
        {
            CreateCylinderChild(lod1.transform, "Trunk", new Vector3(0f, 2.0f, 0f), new Vector3(0.18f, 2.0f, 0.18f), ctx.barkMat);
            CreateMeshChild(lod1.transform, "Cone", ctx.coneMesh, new Vector3(0f, 3.0f, 0f), new Vector3(1.6f, 2.8f, 1.6f), ctx.pineMat);
        }
        else
        {
            CreateCylinderChild(lod1.transform, "Trunk", new Vector3(0f, 2.0f, 0f), new Vector3(0.20f, 2.0f, 0.20f), ctx.barkMat);
            CreateSphereChild(lod1.transform, "Crown", new Vector3(0f, 4.0f, 0f), new Vector3(2.0f, 2.0f, 2.0f), ctx.leafMat);
        }

        GameObject lod2 = new GameObject("LOD2");
        lod2.transform.SetParent(root.transform, false);
        CreateCubeChild(lod2.transform, "Trunk", new Vector3(0f, 1.8f, 0f), new Vector3(0.12f, 3.6f, 0.12f), ctx.barkMat);
        if (pine)
            CreateMeshChild(lod2.transform, "Top", ctx.coneMesh, new Vector3(0f, 3.2f, 0f), new Vector3(1.1f, 2.1f, 1.1f), ctx.pineMat);
        else
            CreateSphereChild(lod2.transform, "Top", new Vector3(0f, 3.8f, 0f), new Vector3(1.6f, 1.6f, 1.6f), ctx.leafMat);

        SetShadowsRecursive(lod1, ShadowCastingMode.Off, false);
        SetShadowsRecursive(lod2, ShadowCastingMode.Off, false);
        ApplyLODGroup(root, lod0, lod1, lod2, 0.58f, 0.24f, 0.06f);
    }

    private static void ApplyLODGroup(GameObject root, GameObject lod0, GameObject lod1, GameObject lod2, float h0, float h1, float h2)
    {
        LODGroup group = root.GetComponent<LODGroup>();
        if (group == null)
            group = root.AddComponent<LODGroup>();
        group.fadeMode = LODFadeMode.CrossFade;
        group.animateCrossFading = true;
        LOD[] lods = new LOD[3];
        lods[0] = new LOD(h0, CollectRenderersForGroup(lod0, group));
        lods[1] = new LOD(h1, CollectRenderersForGroup(lod1, group));
        lods[2] = new LOD(h2, CollectRenderersForGroup(lod2, group));
        group.SetLODs(lods);
        group.RecalculateBounds();
    }

    private static Renderer[] CollectRenderersForGroup(GameObject root, LODGroup owner)
    {
        List<Renderer> renderers = new List<Renderer>();
        MeshRenderer[] mrs = root.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < mrs.Length; i++)
        {
            LODGroup nearest = mrs[i].GetComponentInParent<LODGroup>();
            if (nearest == owner)
                renderers.Add(mrs[i]);
        }
        return renderers.ToArray();
    }

    private static void MoveAllChildrenExcept(Transform root, Transform target, Transform except)
    {
        List<Transform> children = new List<Transform>();
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child != target && child != except)
                children.Add(child);
        }
        for (int i = 0; i < children.Count; i++)
            children[i].SetParent(target, false);
    }

    private static void SetShadowsRecursive(GameObject root, ShadowCastingMode castMode, bool receive)
    {
        MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].shadowCastingMode = castMode;
            renderers[i].receiveShadows = receive;
        }
    }

    private static void FocusSceneView(WorldLayout layout)
    {
        if (SceneView.lastActiveSceneView == null)
            return;
        SceneView.lastActiveSceneView.LookAt(layout.villageCenter + new Vector3(0f, 30f, 0f), Quaternion.Euler(26f, 38f, 0f), layout.villageLengthMeters * 1.25f, true, true);
        SceneView.lastActiveSceneView.Repaint();
    }

    private static void MarkStaticRecursive(GameObject root)
    {
        GameObjectUtility.SetStaticEditorFlags(root,
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.ContributeGI |
            StaticEditorFlags.OccluderStatic |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.ReflectionProbeStatic);

        for (int i = 0; i < root.transform.childCount; i++)
            MarkStaticRecursive(root.transform.GetChild(i).gameObject);
    }
}
#endif
