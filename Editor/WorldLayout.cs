using System;
using System.Collections.Generic;
using AuraLiteWorldGenerator.Runtime;
using UnityEngine;
using UnityEngine.Rendering;

namespace AuraLiteWorldGenerator.Editor
{
    [Serializable]
    public class HouseSpec
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

    [Serializable]
    public class RoadPath
    {
        public string name;
        public float width;
        public bool mainRoad;
        public bool hasStoneShoulders;
        public readonly List<Vector3> points = new List<Vector3>();
    }

    [Serializable]
    public class WorldLayout
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
        public int seed;
        public float wheatRatio;
        [System.NonSerialized]
        public SeededRandom random;
        [System.NonSerialized]
        public HouseSpatialCache houseCache;
        public readonly List<RoadPath> roads = new List<RoadPath>();
        public readonly List<Vector3> riverPoints = new List<Vector3>();
        public readonly List<HouseSpec> houses = new List<HouseSpec>();
    }

    [Serializable]
    public class TerrainGrid
    {
        public float tileSize;
        public int tileCount;
        public Terrain[,] terrains;
    }

    /// <summary>
    /// Holds all asset references created by the generator (materials, meshes, textures, etc.).
    /// </summary>
    [Serializable]
    public class BuildContext
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
}
