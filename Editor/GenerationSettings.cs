using System;
using AuraLiteWorldGenerator.Runtime;
using UnityEngine;
using static AuraLiteWorldGenerator.Runtime.WorldGeneratorConstants;

namespace AuraLiteWorldGenerator.Editor
{
    /// <summary>
    /// Serializable settings for the rural world generator. Can be drawn in the editor window
    /// and validated before generation begins.
    /// </summary>
    [Serializable]
    public class GenerationSettings
    {
        public string sceneName = DefaultSceneName;
        public int seed = DefaultSeed;
        public bool createNewScene = true;
        public bool saveSceneAsset = true;

        [Range(MinMapAreaKm2, MaxMapAreaKm2)]
        public float mapAreaKm2 = DefaultMapAreaKm2;

        [Range(MinTerrainHeight, MaxTerrainHeight)]
        public float terrainHeightMeters = DefaultTerrainHeight;

        [Range(MinVillageLength, MaxVillageLength)]
        public float villageLengthMeters = DefaultVillageLength;

        [Range(MinMainStreetWidth, MaxMainStreetWidth)]
        public float mainStreetWidthMeters = DefaultMainStreetWidth;

        [Range(MinLaneWidth, MaxLaneWidth)]
        public float laneWidthMeters = DefaultLaneWidth;

        [Range(MinHouseDensity, MaxHouseDensity)]
        public float houseDensity = 1.0f;

        [Range(MinWheatRatio, MaxWheatRatio)]
        public float wheatRatio = 0.58f;

        [Range(MinQualityBoost, MaxQualityBoost)]
        public float qualityBoost = 2.0f;

        public VillageStyle villageStyle = VillageStyle.European;

        [Range(MinFogStartKm, MaxFogStartKm)]
        public float fogStartKm = 30f;

        [Range(MinFogEndKm, MaxFogEndKm)]
        public float fogEndKm = 45f;

        public string outputRoot = DefaultOutputRoot;

        /// <summary>
        /// Clamps all values to valid ranges and ensures the output path starts with Assets.
        /// </summary>
        public void ValidateAndClamp()
        {
            mapAreaKm2 = Mathf.Clamp(mapAreaKm2, MinMapAreaKm2, MaxMapAreaKm2);
            terrainHeightMeters = Mathf.Clamp(terrainHeightMeters, MinTerrainHeight, MaxTerrainHeight);
            villageLengthMeters = Mathf.Clamp(villageLengthMeters, MinVillageLength, MaxVillageLength);
            mainStreetWidthMeters = Mathf.Clamp(mainStreetWidthMeters, MinMainStreetWidth, MaxMainStreetWidth);
            laneWidthMeters = Mathf.Clamp(laneWidthMeters, MinLaneWidth, MaxLaneWidth);
            houseDensity = Mathf.Clamp(houseDensity, MinHouseDensity, MaxHouseDensity);
            wheatRatio = Mathf.Clamp(wheatRatio, MinWheatRatio, MaxWheatRatio);
            qualityBoost = Mathf.Clamp(qualityBoost, MinQualityBoost, MaxQualityBoost);
            fogStartKm = Mathf.Clamp(fogStartKm, MinFogStartKm, MaxFogStartKm);
            fogEndKm = Mathf.Clamp(fogEndKm, MinFogEndKm, MaxFogEndKm);

            if (string.IsNullOrWhiteSpace(sceneName))
                sceneName = DefaultSceneName;

            sceneName = sceneName.Trim().Replace(" ", "_");

            if (string.IsNullOrWhiteSpace(outputRoot) || !outputRoot.StartsWith("Assets"))
                outputRoot = DefaultOutputRoot;

            outputRoot = outputRoot.Trim().Replace('\\', '/').TrimEnd('/');

            fogEndKm = Mathf.Max(fogEndKm, fogStartKm + 1f);
        }

        public float TargetSideMeters => Mathf.Sqrt(mapAreaKm2) * 1000f;

        public float TileSizeMeters => TargetSideMeters > LargeWorldThreshold ? TerrainTileSizeLarge : TerrainTileSizeSmall;

        /// <summary>Creates a deep-enough copy for storing as a preset.</summary>
        public GenerationSettings Clone()
        {
            return new GenerationSettings
            {
                sceneName = sceneName,
                seed = seed,
                createNewScene = createNewScene,
                saveSceneAsset = saveSceneAsset,
                mapAreaKm2 = mapAreaKm2,
                terrainHeightMeters = terrainHeightMeters,
                villageLengthMeters = villageLengthMeters,
                mainStreetWidthMeters = mainStreetWidthMeters,
                laneWidthMeters = laneWidthMeters,
                houseDensity = houseDensity,
                wheatRatio = wheatRatio,
                qualityBoost = qualityBoost,
                villageStyle = villageStyle,
                fogStartKm = fogStartKm,
                fogEndKm = fogEndKm,
                outputRoot = outputRoot
            };
        }
    }
}
