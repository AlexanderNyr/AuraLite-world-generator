namespace AuraLiteWorldGenerator.Runtime
{
    /// <summary>
    /// Shared constants used by both the editor generator and runtime components.
    /// </summary>
    public static class WorldGeneratorConstants
    {
        public const string MenuPath = "Tools/Procedural Scenes/";
        public const string WindowTitle = "AAA Rural World URP";
        public const string DefaultOutputRoot = "Assets/GeneratedVillageScene";
        public const string DefaultSceneName = "AAA_RuralVillage_URP";
        public const int DefaultSeed = 240615;

        public const float DefaultMapAreaKm2 = 40f;
        public const float MinMapAreaKm2 = 20f;
        public const float MaxMapAreaKm2 = 150f;

        public const float DefaultTerrainHeight = 180f;
        public const float MinTerrainHeight = 80f;
        public const float MaxTerrainHeight = 320f;

        public const float DefaultVillageLength = 700f;
        public const float MinVillageLength = 350f;
        public const float MaxVillageLength = 1400f;

        public const float DefaultMainStreetWidth = 7f;
        public const float MinMainStreetWidth = 5.5f;
        public const float MaxMainStreetWidth = 9.5f;

        public const float DefaultLaneWidth = 4.6f;
        public const float MinLaneWidth = 3.2f;
        public const float MaxLaneWidth = 6f;

        public const float MinHouseDensity = 0.65f;
        public const float MaxHouseDensity = 1.35f;

        public const float MinWheatRatio = 0.2f;
        public const float MaxWheatRatio = 0.85f;

        public const float MinQualityBoost = 1f;
        public const float MaxQualityBoost = 50f;

        public const float MinFogStartKm = 5f;
        public const float MaxFogStartKm = 30f;
        public const float MinFogEndKm = 10f;
        public const float MaxFogEndKm = 60f;

        public const float WaterLevel = 12.4f;
        public const float WorldMinClearance = 80f;

        public const float TerrainTileSizeSmall = 1024f;
        public const float TerrainTileSizeLarge = 2048f;
        public const float LargeWorldThreshold = 7000f;
    }

    public enum BuildingKind
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
        Boathouse,
        Inn,
        Windmill,
        Watermill,
        School,
        Warehouse,
        Greenhouse,
        Watchtower
    }

    public enum VillageStyle
    {
        European,
        Russian
    }
}
