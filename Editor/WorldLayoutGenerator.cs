using AuraLiteWorldGenerator.Runtime;
using System.Collections.Generic;
using UnityEngine;
using static AuraLiteWorldGenerator.Runtime.WorldGeneratorConstants;

namespace AuraLiteWorldGenerator.Editor
{
    /// <summary>
    /// Generates the high-level world layout (roads, river, village, houses) from settings.
    /// </summary>
    public static class WorldLayoutGenerator
    {
        public static WorldLayout Generate(GenerationSettings settings)
        {
            WorldLayout layout = new WorldLayout
            {
                tileSizeMeters = settings.TileSizeMeters,
                terrainHeightMeters = settings.terrainHeightMeters,
                waterLevel = WaterLevel,
                seed = settings.seed,
                wheatRatio = settings.wheatRatio
            };

            float targetSide = settings.TargetSideMeters;
            layout.tileCount = Mathf.CeilToInt(targetSide / layout.tileSizeMeters);
            layout.worldSizeMeters = layout.tileCount * layout.tileSizeMeters;

            layout.villageLengthMeters = Mathf.Clamp(settings.villageLengthMeters, MinVillageLength, Mathf.Min(MaxVillageLength, layout.worldSizeMeters * 0.22f));
            layout.villageHalfWidthMeters = Mathf.Clamp(layout.villageLengthMeters * 0.38f, 150f, 320f);
            layout.farmlandCellSize = Mathf.Clamp(layout.worldSizeMeters / 15f, 220f, 480f);
            layout.villageCenter = new Vector3(layout.worldSizeMeters * 0.38f, 0f, layout.worldSizeMeters * 0.42f);

            layout.lakeCenter = layout.villageCenter + new Vector3(-layout.villageLengthMeters * 0.62f, 0f, -layout.villageHalfWidthMeters * 1.65f);
            layout.lakeRadiusX = Mathf.Clamp(layout.villageLengthMeters * 0.22f, 120f, 220f);
            layout.lakeRadiusZ = Mathf.Clamp(layout.villageLengthMeters * 0.17f, 90f, 160f);
            layout.riverWidth = Mathf.Clamp(layout.villageLengthMeters * 0.014f, 9f, 16f);
            layout.forestStartZ = Mathf.Clamp(layout.villageCenter.z + Mathf.Max(1400f, layout.worldSizeMeters * 0.24f), layout.worldSizeMeters * 0.60f, layout.worldSizeMeters * 0.80f);

            CreateRiverPath(layout);
            CreateRoadNetwork(layout, settings);
            PopulateVillageHouses(layout, settings);
            return layout;
        }

        private static void CreateRiverPath(WorldLayout layout)
        {
            Vector3 start = layout.lakeCenter + new Vector3(layout.lakeRadiusX * 0.75f, 0f, layout.lakeRadiusZ * 0.10f);
            layout.riverPoints.Add(start);
            layout.riverPoints.Add(layout.villageCenter + new Vector3(-layout.villageLengthMeters * 0.24f, 0f, -layout.villageHalfWidthMeters * 0.92f));
            layout.riverPoints.Add(layout.villageCenter + new Vector3(layout.villageLengthMeters * 0.18f, 0f, -layout.villageHalfWidthMeters * 0.55f));
            layout.riverPoints.Add(layout.villageCenter + new Vector3(layout.villageLengthMeters * 0.52f, 0f, -layout.villageHalfWidthMeters * 0.18f));
            layout.riverPoints.Add(new Vector3(layout.worldSizeMeters * 0.78f, 0f, layout.villageCenter.z - layout.villageHalfWidthMeters * 0.16f));
            layout.riverPoints.Add(new Vector3(layout.worldSizeMeters - 120f, 0f, layout.villageCenter.z + layout.villageHalfWidthMeters * 0.05f));
        }

        private static void CreateRoadNetwork(WorldLayout layout, GenerationSettings settings)
        {
            float mainHalf = layout.villageLengthMeters * 0.62f;
            RoadPath mainRoad = new RoadPath
            {
                name = "MainStreet",
                width = settings.mainStreetWidthMeters,
                mainRoad = true,
                hasStoneShoulders = true
            };

            int mainSamples = Mathf.RoundToInt(8f * Mathf.Lerp(1f, 1.6f, Mathf.InverseLerp(1f, 3f, settings.qualityBoost)));
            for (int i = 0; i < mainSamples; i++)
            {
                float t = i / (float)Mathf.Max(1, mainSamples - 1);
                float x = layout.villageCenter.x - mainHalf + t * mainHalf * 2f;
                float z = layout.villageCenter.z + Mathf.Sin(t * Mathf.PI * 1.2f + 0.5f) * 12f + (Mathf.PerlinNoise(settings.seed * 0.01f + t * 2.1f, 0.24f) - 0.5f) * 10f;
                mainRoad.points.Add(new Vector3(x, 0f, z));
            }
            layout.roads.Add(mainRoad);

            RoadPath crossRoad = new RoadPath
            {
                name = "CrossRoad",
                width = settings.laneWidthMeters + 0.9f,
                mainRoad = false,
                hasStoneShoulders = true
            };
            for (int i = 0; i < 6; i++)
            {
                float t = i / 5f;
                float z = layout.villageCenter.z - layout.villageHalfWidthMeters * 1.02f + t * layout.villageHalfWidthMeters * 2.04f;
                float x = layout.villageCenter.x + Mathf.Sin(t * Mathf.PI) * 16f + (Mathf.PerlinNoise(settings.seed * 0.012f + t * 2.0f, 0.54f) - 0.5f) * 10f;
                crossRoad.points.Add(new Vector3(x, 0f, z));
            }
            layout.roads.Add(crossRoad);

            AddLane(layout, "NorthLane", settings.laneWidthMeters, false,
                new Vector3(-0.48f, 0f, 0.45f),
                new Vector3(-0.16f, 0f, 0.68f),
                new Vector3(0.20f, 0f, 0.60f),
                new Vector3(0.50f, 0f, 0.38f));

            AddLane(layout, "SouthLane", settings.laneWidthMeters, false,
                new Vector3(-0.44f, 0f, -0.55f),
                new Vector3(-0.12f, 0f, -0.70f),
                new Vector3(0.15f, 0f, -0.68f),
                new Vector3(0.44f, 0f, -0.42f));

            for (int i = 0; i < 4; i++)
            {
                AddSpurAndTrack(layout, settings, i);
            }
        }

        private static void AddLane(WorldLayout layout, string name, float width, bool shoulders, params Vector3[] offsets)
        {
            RoadPath lane = new RoadPath
            {
                name = name,
                width = width,
                mainRoad = false,
                hasStoneShoulders = shoulders
            };
            foreach (Vector3 offset in offsets)
                lane.points.Add(layout.villageCenter + new Vector3(offset.x * layout.villageLengthMeters, 0f, offset.z * layout.villageHalfWidthMeters));
            layout.roads.Add(lane);
        }

        private static void AddSpurAndTrack(WorldLayout layout, GenerationSettings settings, int index)
        {
            RoadPath spur = new RoadPath
            {
                name = "Lane_" + index,
                width = settings.laneWidthMeters,
                mainRoad = false,
                hasStoneShoulders = index % 2 == 0
            };

            float xOff = Mathf.Lerp(-0.40f, 0.42f, index / 3f) * layout.villageLengthMeters;
            Vector3 anchor = layout.villageCenter + new Vector3(xOff, 0f, (index % 2 == 0 ? 1f : -1f) * 18f);
            float dir = index % 2 == 0 ? 1f : -1f;
            float length = Mathf.Lerp(170f, 280f, Mathf.Abs(Mathf.Sin(settings.seed * 0.11f + index * 1.7f)));

            spur.points.Add(anchor);
            spur.points.Add(anchor + new Vector3(Random.Range(-12f, 12f), 0f, dir * length * 0.38f));
            spur.points.Add(anchor + new Vector3(Random.Range(-24f, 24f), 0f, dir * length * 0.76f));
            spur.points.Add(anchor + new Vector3(Random.Range(-30f, 30f), 0f, dir * length));
            layout.roads.Add(spur);

            RoadPath track = new RoadPath
            {
                name = "FarmTrack_" + index,
                width = Mathf.Max(3.2f, settings.laneWidthMeters * 0.78f),
                mainRoad = false,
                hasStoneShoulders = false
            };
            Vector3 trackStart = spur.points[spur.points.Count - 1];
            float trackDir = dir;
            float trackLength = Mathf.Lerp(340f, 760f, Mathf.Abs(Mathf.Sin(settings.seed * 0.073f + index * 2.7f)));
            track.points.Add(trackStart);
            track.points.Add(trackStart + new Vector3(Random.Range(-70f, 70f), 0f, trackDir * trackLength * 0.46f));
            track.points.Add(trackStart + new Vector3(Random.Range(-140f, 140f), 0f, trackDir * trackLength));
            layout.roads.Add(track);
        }

        private static void PopulateVillageHouses(WorldLayout layout, GenerationSettings settings)
        {
            float densitySpacing = Mathf.Lerp(1.28f, 0.78f, Mathf.InverseLerp(0.65f, 1.35f, settings.houseDensity));
            densitySpacing *= Mathf.Lerp(1f, 0.82f, Mathf.InverseLerp(1f, 3f, settings.qualityBoost));

            for (int i = 0; i < layout.roads.Count; i++)
            {
                RoadPath road = layout.roads[i];
                float length = GeometryHelpers.GetPathLength(road);
                if (length <= 0f)
                    continue;

                if (road.name == "MainStreet")
                {
                    PlaceHousesAlongRoad(layout, settings, road, length * 0.05f, length * 0.95f, 24f * densitySpacing, 14f, true, BuildingKind.Farmhouse, BuildingKind.Cottage, BuildingKind.Workshop, BuildingKind.LongHouse);
                }
                else if (road.name == "CrossRoad")
                {
                    PlaceHousesAlongRoad(layout, settings, road, length * 0.08f, length * 0.92f, 26f * densitySpacing, 13f, true, BuildingKind.Cottage, BuildingKind.Farmhouse, BuildingKind.Workshop);
                }
                else if (road.name == "NorthLane" || road.name == "SouthLane")
                {
                    PlaceHousesAlongRoad(layout, settings, road, length * 0.05f, length * 0.96f, 27f * densitySpacing, 12f, false, BuildingKind.Cottage, BuildingKind.Farmhouse, BuildingKind.Barn);
                }
                else if (road.name.StartsWith("Lane_"))
                {
                    PlaceHousesAlongRoad(layout, settings, road, length * 0.10f, length * 0.92f, 31f * densitySpacing, 12f, false, BuildingKind.Cottage, BuildingKind.Barn, BuildingKind.Farmhouse);
                }
                else if (road.name.StartsWith("FarmTrack"))
                {
                    PlaceHousesAlongRoad(layout, settings, road, length * 0.10f, length * 0.40f, 78f * densitySpacing, 16f, false, BuildingKind.Farmhouse, BuildingKind.Barn);
                }
            }

            AddRandomBarns(layout, settings);
            AddSpecialBuildings(layout, settings);
        }

        private static void AddRandomBarns(WorldLayout layout, GenerationSettings settings)
        {
            for (int i = 0; i < 8; i++)
            {
                float x = layout.villageCenter.x + Random.Range(-layout.villageLengthMeters * 0.38f, layout.villageLengthMeters * 0.38f);
                float z = layout.villageCenter.z + Random.Range(-layout.villageHalfWidthMeters * 0.85f, layout.villageHalfWidthMeters * 0.85f);
                if (ComputeRiverMask(layout, x, z) > 0.12f)
                    continue;
                HouseSpec barn = BuildingFactory.CreateHouseSpec(new Vector3(x, 0f, z), Random.Range(0f, 360f), BuildingKind.Barn);
                barn.fenced = false;
                barn.garden = false;
                if (CanPlaceHouse(layout, barn.position, 24f))
                    layout.houses.Add(barn);
            }
        }

        private static void AddSpecialBuildings(WorldLayout layout, GenerationSettings settings)
        {
            AddSpecialBuilding(layout, new Vector3(layout.villageCenter.x + layout.villageLengthMeters * 0.24f, 0f, layout.villageCenter.z - layout.villageHalfWidthMeters * 0.74f), 12f, BuildingKind.Manor, true, true, true, 30f);
            AddSpecialBuilding(layout, new Vector3(layout.villageCenter.x - layout.villageLengthMeters * 0.10f, 0f, layout.villageCenter.z + layout.villageHalfWidthMeters * 0.14f), -6f, BuildingKind.Chapel, false, false, false, 26f);
            AddSpecialBuilding(layout, new Vector3(layout.villageCenter.x + layout.villageLengthMeters * 0.06f, 0f, layout.villageCenter.z - layout.villageHalfWidthMeters * 0.10f), 18f, BuildingKind.Forge, false, false, true, 24f);
            AddSpecialBuilding(layout, layout.lakeCenter + new Vector3(layout.lakeRadiusX * 0.92f, 0f, layout.lakeRadiusZ * 0.16f), -10f, BuildingKind.Mill, false, false, false, 34f);
            AddSpecialBuilding(layout, new Vector3(layout.villageCenter.x - layout.villageLengthMeters * 0.02f, 0f, layout.villageCenter.z - layout.villageHalfWidthMeters * 0.04f), 8f, BuildingKind.Tavern, false, false, true, 26f);
            AddSpecialBuilding(layout, new Vector3(layout.villageCenter.x + layout.villageLengthMeters * 0.38f, 0f, layout.villageCenter.z + layout.villageHalfWidthMeters * 0.52f), 22f, BuildingKind.Stable, true, false, false, 24f);
            AddSpecialBuilding(layout, new Vector3(layout.villageCenter.x - layout.villageLengthMeters * 0.32f, 0f, layout.villageCenter.z - layout.villageHalfWidthMeters * 0.52f), -18f, BuildingKind.Granary, false, false, false, 24f);
            AddSpecialBuilding(layout, layout.lakeCenter + new Vector3(layout.lakeRadiusX * 0.34f, 0f, layout.lakeRadiusZ * 0.92f), 164f, BuildingKind.Boathouse, false, false, false, 22f);
        }

        private static void AddSpecialBuilding(WorldLayout layout, Vector3 position, float yaw, BuildingKind kind, bool fenced, bool garden, bool annex, float minDistance)
        {
            HouseSpec spec = BuildingFactory.CreateHouseSpec(position, yaw, kind);
            spec.fenced = fenced;
            spec.garden = garden;
            spec.annex = annex;
            if (CanPlaceHouse(layout, spec.position, minDistance))
                layout.houses.Add(spec);
        }

        private static void PlaceHousesAlongRoad(WorldLayout layout, GenerationSettings settings, RoadPath road, float startDistance, float endDistance, float spacingBase, float setback, bool allowLargeMix, params BuildingKind[] palette)
        {
            float d = startDistance;
            int sideToggle = 0;
            while (d < endDistance)
            {
                float spacing = spacingBase * Mathf.Lerp(0.9f, 1.3f, Mathf.Abs(Mathf.Sin((settings.seed + d) * 0.013f)));
                Vector3 center = GeometryHelpers.SamplePath(road, d);
                Vector3 forward = GeometryHelpers.DirectionOnPath(road, d);
                Vector3 side = Vector3.Cross(Vector3.up, forward).normalized;

                bool placeLeft = sideToggle % 2 == 0 || Random.value > 0.4f;
                bool placeRight = sideToggle % 3 != 0 || Random.value > 0.62f;
                if (!placeLeft && !placeRight)
                    placeLeft = true;

                if (placeLeft)
                    TryAddHouse(layout, center + side * (road.width * 0.5f + setback + Random.Range(1f, 5f)), Quaternion.LookRotation(-side, Vector3.up).eulerAngles.y, palette, allowLargeMix);
                if (placeRight)
                    TryAddHouse(layout, center - side * (road.width * 0.5f + setback + Random.Range(1f, 5f)), Quaternion.LookRotation(side, Vector3.up).eulerAngles.y, palette, allowLargeMix);

                sideToggle++;
                d += spacing;
            }
        }

        private static void TryAddHouse(WorldLayout layout, Vector3 pos, float yaw, BuildingKind[] palette, bool allowLargeMix)
        {
            if (Mathf.Abs(pos.z - layout.villageCenter.z) > layout.villageHalfWidthMeters + 150f && Random.value > 0.55f)
                return;
            if (ComputeLakeMask(layout, pos.x, pos.z) > 0.06f || ComputeRiverMask(layout, pos.x, pos.z) > 0.08f)
                return;

            BuildingKind kind = palette[Random.Range(0, palette.Length)];
            if (!allowLargeMix && kind == BuildingKind.LongHouse && Random.value > 0.25f)
                kind = BuildingKind.Cottage;

            HouseSpec spec = BuildingFactory.CreateHouseSpec(pos, yaw, kind);
            if (CanPlaceHouse(layout, pos, Mathf.Max(spec.footprint.x, spec.footprint.y) * 1.55f))
                layout.houses.Add(spec);
        }

        public static bool CanPlaceHouse(WorldLayout layout, Vector3 pos, float minDistance)
        {
            Vector2 p = new Vector2(pos.x, pos.z);
            for (int i = 0; i < layout.houses.Count; i++)
            {
                if (Vector2.Distance(p, new Vector2(layout.houses[i].position.x, layout.houses[i].position.z)) < minDistance)
                    return false;
            }
            return true;
        }

        #region Masks

        public static float ComputeVillageMask(WorldLayout layout, float worldX, float worldZ)
        {
            float dx = (worldX - layout.villageCenter.x) / (layout.villageLengthMeters * 0.72f);
            float dz = (worldZ - layout.villageCenter.z) / (layout.villageHalfWidthMeters * 1.7f);
            float d = Mathf.Sqrt(dx * dx + dz * dz);
            return 1f - Mathf.Clamp01((d - 0.55f) / 0.55f);
        }

        public static float ComputeForestMask(WorldLayout layout, float worldX, float worldZ)
        {
            float boundary = layout.forestStartZ + (Mathf.PerlinNoise(worldX * 0.00035f + layout.seed * 0.011f, 0.15f) - 0.5f) * 420f;
            return Mathf.Clamp01((worldZ - (boundary - 180f)) / 480f);
        }

        public static float ComputeLakeMask(WorldLayout layout, float worldX, float worldZ)
        {
            float dx = (worldX - layout.lakeCenter.x) / layout.lakeRadiusX;
            float dz = (worldZ - layout.lakeCenter.z) / layout.lakeRadiusZ;
            float d = Mathf.Sqrt(dx * dx + dz * dz);
            float shoreNoise = (Mathf.PerlinNoise(worldX * 0.0018f + 12f, worldZ * 0.0018f + 41f) - 0.5f) * 0.08f;
            d += shoreNoise;
            return 1f - Mathf.Clamp01((d - 0.76f) / 0.30f);
        }

        public static float ComputeRiverMask(WorldLayout layout, float worldX, float worldZ)
        {
            if (layout.riverPoints.Count < 2)
                return 0f;
            float d = GeometryHelpers.DistancePointPolylineXZ(new Vector3(worldX, 0f, worldZ), layout.riverPoints);
            return 1f - Mathf.Clamp01((d - layout.riverWidth * 0.42f) / (layout.riverWidth * 1.15f));
        }

        public static float ComputeRoadMask(WorldLayout layout, float worldX, float worldZ)
        {
            float mask = 0f;
            Vector3 p = new Vector3(worldX, 0f, worldZ);
            for (int i = 0; i < layout.roads.Count; i++)
            {
                float d = GeometryHelpers.DistancePointPolylineXZ(p, layout.roads[i]);
                float t = 1f - Mathf.Clamp01((d - layout.roads[i].width * 0.28f) / (layout.roads[i].width * 1.1f));
                mask = Mathf.Max(mask, t);
            }
            return mask;
        }

        public static float ComputeHouseMask(WorldLayout layout, float worldX, float worldZ)
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

        public static float ComputeFieldSofteningMask(WorldLayout layout, float worldX, float worldZ)
        {
            if (ComputeForestMask(layout, worldX, worldZ) > 0.15f)
                return 0f;
            if (ComputeVillageMask(layout, worldX, worldZ) > 0.20f)
                return 0f;
            if (ComputeLakeMask(layout, worldX, worldZ) > 0.08f || ComputeRiverMask(layout, worldX, worldZ) > 0.10f)
                return 0f;
            return 1f;
        }

        public static float ComputeWheatFieldMask(WorldLayout layout, float worldX, float worldZ, out float fieldBorder)
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

            float parcelHash = GeometryHelpers.Hash01(cx, cz, layout.seed + 71);
            return parcelHash < layout.wheatRatio ? inside : 0f;
        }

        #endregion
    }
}
