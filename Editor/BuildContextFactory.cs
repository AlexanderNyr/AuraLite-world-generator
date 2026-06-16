using UnityEngine;
using static AuraLiteWorldGenerator.Runtime.WorldGeneratorConstants;

namespace AuraLiteWorldGenerator.Editor
{
    /// <summary>
    /// Prepares all asset folders and generated assets for the world builder.
    /// </summary>
    public static class BuildContextFactory
    {
        public static BuildContext Prepare(GenerationSettings settings)
        {
            if (settings == null)
                throw new System.ArgumentNullException(nameof(settings));

            BuildContext ctx = new BuildContext
            {
                rootFolder = settings.outputRoot,
                materialsFolder = settings.outputRoot + "/Materials",
                texturesFolder = settings.outputRoot + "/Textures",
                meshesFolder = settings.outputRoot + "/Meshes",
                volumesFolder = settings.outputRoot + "/Volumes",
                terrainFolder = settings.outputRoot + "/Terrain"
            };

            AssetFactory.EnsureFolder(ctx.rootFolder);
            AssetFactory.EnsureFolder(ctx.materialsFolder);
            AssetFactory.EnsureFolder(ctx.texturesFolder);
            AssetFactory.EnsureFolder(ctx.meshesFolder);
            AssetFactory.EnsureFolder(ctx.volumesFolder);
            AssetFactory.EnsureFolder(ctx.terrainFolder);

            ctx.litShader = AssetFactory.FindBestLitShader();
            ctx.terrainShader = AssetFactory.FindBestTerrainShader();

            if (ctx.litShader == null)
                throw new System.InvalidOperationException("No suitable Lit shader found. Ensure URP or Standard is available.");

            int texSize = settings.qualityBoost >= 2f ? 512 : 256;

            ctx.grassTex = AssetFactory.CreateOrReplaceTextureAsset(ctx.texturesFolder + "/T_Grass.asset", texSize, texSize, new Color(0.28f, 0.50f, 0.22f), new Color(0.35f, 0.58f, 0.24f), 0.18f, 5.6f, false);
            ctx.wheatTex = AssetFactory.CreateOrReplaceTextureAsset(ctx.texturesFolder + "/T_Wheat.asset", texSize, texSize, new Color(0.61f, 0.53f, 0.20f), new Color(0.75f, 0.66f, 0.26f), 0.10f, 7.2f, false);
            ctx.dirtTex = AssetFactory.CreateOrReplaceTextureAsset(ctx.texturesFolder + "/T_Dirt.asset", texSize, texSize, new Color(0.39f, 0.30f, 0.20f), new Color(0.49f, 0.38f, 0.26f), 0.15f, 6.4f, false);
            ctx.forestTex = AssetFactory.CreateOrReplaceTextureAsset(ctx.texturesFolder + "/T_Forest.asset", texSize, texSize, new Color(0.17f, 0.29f, 0.13f), new Color(0.22f, 0.35f, 0.15f), 0.16f, 6.1f, true);
            ctx.cloudTex = AssetFactory.CreateOrReplaceCloudTextureAsset(ctx.texturesFolder + "/T_Clouds.asset", texSize, texSize);

            Shader shader = ctx.terrainShader != null ? ctx.terrainShader : ctx.litShader;
            ctx.terrainMat = AssetFactory.CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_TerrainURP.mat", Color.white, 0f, 0.02f, shader);
            ctx.roadMat = AssetFactory.CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_Road.mat", new Color(0.42f, 0.35f, 0.26f), 0f, 0.06f, ctx.litShader);
            ctx.shoulderMat = AssetFactory.CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_Shoulder.mat", new Color(0.55f, 0.55f, 0.54f), 0f, 0.18f, ctx.litShader);
            ctx.grassPreviewMat = AssetFactory.CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_GrassPreview.mat", new Color(0.31f, 0.53f, 0.25f), 0f, 0.04f, ctx.litShader);
            ctx.wheatPreviewMat = AssetFactory.CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_WheatPreview.mat", new Color(0.74f, 0.66f, 0.28f), 0f, 0.04f, ctx.litShader);
            ctx.dirtMat = AssetFactory.CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_Dirt.mat", new Color(0.42f, 0.33f, 0.22f), 0f, 0.03f, ctx.litShader);
            ctx.wallCreamMat = AssetFactory.CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_WallCream.mat", new Color(0.82f, 0.77f, 0.68f), 0f, 0.18f, ctx.litShader);
            ctx.wallWarmMat = AssetFactory.CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_WallWarm.mat", new Color(0.72f, 0.63f, 0.53f), 0f, 0.16f, ctx.litShader);
            ctx.timberMat = AssetFactory.CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_Timber.mat", new Color(0.29f, 0.18f, 0.10f), 0f, 0.22f, ctx.litShader);
            ctx.roofRedMat = AssetFactory.CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_RoofRed.mat", new Color(0.47f, 0.19f, 0.11f), 0f, 0.28f, ctx.litShader);
            ctx.roofDarkMat = AssetFactory.CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_RoofDark.mat", new Color(0.21f, 0.19f, 0.18f), 0f, 0.24f, ctx.litShader);
            ctx.stoneMat = AssetFactory.CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_Stone.mat", new Color(0.56f, 0.54f, 0.52f), 0f, 0.20f, ctx.litShader);
            ctx.barkMat = AssetFactory.CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_Bark.mat", new Color(0.24f, 0.16f, 0.10f), 0f, 0.16f, ctx.litShader);
            ctx.pineMat = AssetFactory.CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_Pine.mat", new Color(0.14f, 0.28f, 0.15f), 0f, 0.10f, ctx.litShader);
            ctx.leafMat = AssetFactory.CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_Leaf.mat", new Color(0.24f, 0.44f, 0.19f), 0f, 0.10f, ctx.litShader);
            ctx.glassMat = AssetFactory.CreateTransparentMaterialAsset(ctx.materialsFolder + "/M_Glass.mat", new Color(0.72f, 0.83f, 0.92f, 0.42f), 0f, 0.68f, ctx.litShader);
            ctx.wheatBaleMat = AssetFactory.CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_WheatBale.mat", new Color(0.75f, 0.66f, 0.30f), 0f, 0.08f, ctx.litShader);
            ctx.waterMat = AssetFactory.CreateTransparentMaterialAsset(ctx.materialsFolder + "/M_Water.mat", new Color(0.22f, 0.41f, 0.58f, 0.78f), 0f, 0.86f, ctx.litShader);
            ctx.grassBladeMat = AssetFactory.CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_GrassBlade.mat", new Color(0.31f, 0.56f, 0.25f), 0f, 0.02f, ctx.litShader);
            ctx.wheatBladeMat = AssetFactory.CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_WheatBlade.mat", new Color(0.77f, 0.69f, 0.30f), 0f, 0.03f, ctx.litShader);
            ctx.logWallMat = AssetFactory.CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_LogWall.mat", new Color(0.34f, 0.23f, 0.13f), 0f, 0.18f, ctx.litShader);
            ctx.copperRoofMat = AssetFactory.CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_CopperRoof.mat", new Color(0.26f, 0.46f, 0.33f), 0f, 0.22f, ctx.litShader);
            ctx.forgeFireMat = AssetFactory.CreateOpaqueMaterialAsset(ctx.materialsFolder + "/M_ForgeFire.mat", new Color(0.95f, 0.44f, 0.10f), 0f, 0.78f, ctx.litShader);
            ctx.cloudMat = AssetFactory.CreateTransparentMaterialAsset(ctx.materialsFolder + "/M_Cloud.mat", new Color(1f, 1f, 1f, 0.78f), 0f, 0.10f, ctx.litShader);

            AssetFactory.EnableEmission(ctx.glassMat, new Color(0.08f, 0.12f, 0.15f));
            AssetFactory.EnableEmission(ctx.forgeFireMat, new Color(1.2f, 0.35f, 0.05f));
            AssetFactory.SetTextureSafe(ctx.cloudMat, "_BaseMap", ctx.cloudTex);
            AssetFactory.SetTextureSafe(ctx.cloudMat, "_MainTex", ctx.cloudTex);
            AssetFactory.EnableInstancing(
                ctx.terrainMat, ctx.roadMat, ctx.shoulderMat, ctx.grassPreviewMat, ctx.wheatPreviewMat, ctx.dirtMat,
                ctx.wallCreamMat, ctx.wallWarmMat, ctx.timberMat, ctx.roofRedMat, ctx.roofDarkMat, ctx.stoneMat,
                ctx.barkMat, ctx.pineMat, ctx.leafMat, ctx.glassMat, ctx.wheatBaleMat, ctx.waterMat, ctx.grassBladeMat,
                ctx.wheatBladeMat, ctx.logWallMat, ctx.copperRoofMat, ctx.forgeFireMat, ctx.cloudMat);

            ctx.grassLayer = AssetFactory.CreateOrReplaceTerrainLayer(settings.outputRoot + "/TerrainLayer_Grass.terrainlayer", ctx.grassTex, new Vector2(32f, 32f), 0.02f);
            ctx.wheatLayer = AssetFactory.CreateOrReplaceTerrainLayer(settings.outputRoot + "/TerrainLayer_Wheat.terrainlayer", ctx.wheatTex, new Vector2(44f, 44f), 0.02f);
            ctx.dirtLayer = AssetFactory.CreateOrReplaceTerrainLayer(settings.outputRoot + "/TerrainLayer_Dirt.terrainlayer", ctx.dirtTex, new Vector2(18f, 18f), 0.02f);
            ctx.forestLayer = AssetFactory.CreateOrReplaceTerrainLayer(settings.outputRoot + "/TerrainLayer_Forest.terrainlayer", ctx.forestTex, new Vector2(26f, 26f), 0.02f);

            ctx.roofMesh = AssetFactory.CreateOrReplaceMeshAsset(ctx.meshesFolder + "/MESH_RoofPrism.asset", MeshFactory.CreateRoofPrismMesh());
            ctx.coneMesh = AssetFactory.CreateOrReplaceMeshAsset(ctx.meshesFolder + "/MESH_Cone.asset", MeshFactory.CreateConeMesh(18));
            ctx.grassBladeMesh = AssetFactory.CreateOrReplaceMeshAsset(ctx.meshesFolder + "/MESH_GrassBlade.asset", MeshFactory.CreateGrassBladeMesh());
            ctx.wheatBladeMesh = AssetFactory.CreateOrReplaceMeshAsset(ctx.meshesFolder + "/MESH_WheatBlade.asset", MeshFactory.CreateWheatBladeMesh());
            ctx.quadMesh = AssetFactory.CreateOrReplaceMeshAsset(ctx.meshesFolder + "/MESH_Quad.asset", MeshFactory.CreateQuadMesh());

            ctx.grassDetailPrefab = AssetFactory.CreateOrReplaceDetailPrefab(ctx.rootFolder + "/PF_GrassDetail.prefab", ctx.grassBladeMesh, ctx.grassBladeMat);
            ctx.wheatDetailPrefab = AssetFactory.CreateOrReplaceDetailPrefab(ctx.rootFolder + "/PF_WheatDetail.prefab", ctx.wheatBladeMesh, ctx.wheatBladeMat);
            ctx.globalVolumeProfile = AssetFactory.CreateOrReplaceVolumeProfile(ctx.volumesFolder + "/AAA_Rural_GlobalProfile.asset");
            AssetFactory.SetupURPVolumeProfile(ctx.globalVolumeProfile);

            return ctx;
        }
    }
}
