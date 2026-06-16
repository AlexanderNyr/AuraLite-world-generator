using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static AuraLiteWorldGenerator.Runtime.WorldGeneratorConstants;

namespace AuraLiteWorldGenerator.Editor
{
    /// <summary>
    /// Creates and populates the tiled terrain grid (heightmaps, splatmaps, detail layers).
    /// </summary>
    public static class TerrainGenerator
    {
        public static IEnumerator CreateTerrainGrid(BuildContext ctx, WorldLayout layout, GenerationSettings settings, Transform parent, Action<TerrainGrid> onComplete, CancellationToken cancellationToken = default)
        {
            TerrainGrid grid = new TerrainGrid
            {
                tileCount = layout.tileCount,
                tileSize = layout.tileSizeMeters,
                terrains = new Terrain[layout.tileCount, layout.tileCount]
            };

            HouseSpatialCache houseCache = layout.houseCache ?? new HouseSpatialCache(layout);
            int heightRes = layout.tileCount <= 10 ? 1025 : (layout.tileCount <= 18 ? 513 : 257);
            int alphaRes = layout.tileCount <= 12 ? 512 : (layout.tileCount <= 18 ? 256 : 128);
            int detailRes = layout.tileCount <= 12 ? Mathf.RoundToInt(192 * settings.qualityBoost) : Mathf.RoundToInt(96 * settings.qualityBoost);
            int tileCount = layout.tileCount;

            // Compute all heightmaps in parallel on thread-pool workers. Mathf.PerlinNoise and the
            // layout mask helpers are pure math / read-only, so this is safe and much faster.
            float[][,] heights = new float[tileCount * tileCount][,];
            Task heightTask = Task.Run(() =>
            {
                Parallel.For(0, tileCount, new ParallelOptions { CancellationToken = cancellationToken }, z =>
                {
                    for (int x = 0; x < tileCount; x++)
                    {
                        int index = z * tileCount + x;
                        heights[index] = GenerateHeightsForTile(layout, x, z, heightRes, houseCache);
                    }
                });
            }, cancellationToken);

            while (!heightTask.IsCompleted)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return null;
            }

            if (heightTask.IsCanceled)
                throw new OperationCanceledException("Height computation was cancelled.");
            if (heightTask.IsFaulted)
                throw heightTask.Exception?.InnerException ?? heightTask.Exception ?? new Exception("Height computation task failed.");

            // TerrainData creation must stay on the main thread.
            for (int z = 0; z < tileCount; z++)
            {
                for (int x = 0; x < tileCount; x++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    int index = z * tileCount + x;
                    TerrainData td = new TerrainData();
                    td.heightmapResolution = heightRes;
                    td.alphamapResolution = alphaRes;
                    td.baseMapResolution = 1024;
                    td.SetDetailResolution(detailRes, 16);
                    td.size = new Vector3(layout.tileSizeMeters, layout.terrainHeightMeters, layout.tileSizeMeters);
                    td.terrainLayers = new[] { ctx.grassLayer, ctx.wheatLayer, ctx.dirtLayer, ctx.forestLayer };
                    td.detailPrototypes = new[] { CreateGrassDetailPrototype(ctx), CreateWheatDetailPrototype(ctx) };
                    td.SetHeights(0, 0, heights[index]);

                    string tdPath = ctx.terrainFolder + $"/TerrainData_{x}_{z}.asset";
                    AssetFactory.DeleteExistingAsset<TerrainData>(tdPath);
                    AssetDatabase.CreateAsset(td, tdPath);

                    GameObject terrainGO = Terrain.CreateTerrainGameObject(td);
                    terrainGO.name = $"Terrain_{x}_{z}";
                    terrainGO.transform.SetParent(parent);
                    terrainGO.transform.position = new Vector3(x * layout.tileSizeMeters, 0f, z * layout.tileSizeMeters);

                    Terrain terrain = terrainGO.GetComponent<Terrain>();
                    terrain.drawInstanced = true;
                    terrain.heightmapPixelError = Mathf.Lerp(2.5f, 1.1f, Mathf.InverseLerp(1f, 3f, settings.qualityBoost));
                    terrain.basemapDistance = 9000f;
                    terrain.shadowCastingMode = ShadowCastingMode.On;
                    terrain.materialTemplate = ctx.terrainMat;
                    terrain.detailObjectDistance = 220f + settings.qualityBoost * 90f;
                    terrain.detailObjectDensity = Mathf.Lerp(1f, 1.7f, Mathf.InverseLerp(1f, 3f, settings.qualityBoost));
                    terrain.treeDistance = 0f;
                    grid.terrains[x, z] = terrain;
                    yield return null;
                }
            }

            SetTerrainNeighbors(grid, layout);
            onComplete?.Invoke(grid);
        }

        private static void SetTerrainNeighbors(TerrainGrid grid, WorldLayout layout)
        {
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
        }

        private static float[,] GenerateHeightsForTile(WorldLayout layout, int tileX, int tileZ, int resolution, HouseSpatialCache houseCache)
        {
            float[,] heights = new float[resolution, resolution];
            float worldX0 = tileX * layout.tileSizeMeters;
            float worldZ0 = tileZ * layout.tileSizeMeters;
            float invRes = 1f / (resolution - 1);

            for (int z = 0; z < resolution; z++)
            {
                float worldZ = worldZ0 + z * invRes * layout.tileSizeMeters;
                for (int x = 0; x < resolution; x++)
                {
                    float worldX = worldX0 + x * invRes * layout.tileSizeMeters;
                    heights[z, x] = Mathf.Clamp01(EvaluateTerrainHeightMeters(layout, worldX, worldZ, houseCache) / layout.terrainHeightMeters);
                }
            }
            return heights;
        }

        public static float EvaluateTerrainHeightMeters(WorldLayout layout, float worldX, float worldZ, HouseSpatialCache houseCache = null)
        {
            float broad = 16f + (GeometryHelpers.FBM(worldX * 0.00045f + layout.seed * 0.011f, worldZ * 0.00045f + 9.1f, 4, 0.5f, 2f) - 0.5f) * 20f;
            float detail = (GeometryHelpers.FBM(worldX * 0.00165f + 54f, worldZ * 0.00165f + 18f, 3, 0.5f, 2.1f) - 0.5f) * 5f;
            float micro = (Mathf.PerlinNoise(worldX * 0.0041f + 103f, worldZ * 0.0041f + 41f) - 0.5f) * 1.8f;
            float forestRise = WorldLayoutGenerator.ComputeForestMask(layout, worldX, worldZ) * (10f + GeometryHelpers.FBM(worldX * 0.0009f, worldZ * 0.0009f, 2, 0.5f, 2f) * 16f);

            float h = broad + detail + micro + forestRise;

            float lakeMask = WorldLayoutGenerator.ComputeLakeMask(layout, worldX, worldZ);
            if (lakeMask > 0.001f)
            {
                float lakeBed = layout.waterLevel - Mathf.Lerp(2.8f, 6.5f, lakeMask);
                h = Mathf.Lerp(h, lakeBed, lakeMask);
            }

            float riverMask = WorldLayoutGenerator.ComputeRiverMask(layout, worldX, worldZ);
            if (riverMask > 0.001f)
            {
                float riverBed = layout.waterLevel - Mathf.Lerp(1.0f, 3.0f, riverMask);
                h = Mathf.Lerp(h, riverBed, riverMask * 0.95f);
            }

            float villageMask = WorldLayoutGenerator.ComputeVillageMask(layout, worldX, worldZ);
            if (villageMask > 0.001f)
            {
                float villageBase = 15f + (Mathf.PerlinNoise(worldX * 0.00035f + 12f, worldZ * 0.00035f + 31f) - 0.5f) * 2.5f;
                h = Mathf.Lerp(h, villageBase, villageMask * 0.92f);
            }

            float roadMask = ComputeRoadMask(layout, worldX, worldZ);
            if (roadMask > 0.001f)
            {
                float roadBase = 15f + (Mathf.PerlinNoise(worldX * 0.00022f + 5f, worldZ * 0.00022f + 7f) - 0.5f) * 1.4f;
                h = Mathf.Lerp(h, roadBase, roadMask * 0.9f);
            }

            if (houseCache != null)
            {
                List<HouseSpec> nearby = houseCache.GetNearby(worldX, worldZ);
                if (nearby != null)
                {
                    for (int i = 0; i < nearby.Count; i++)
                    {
                        HouseSpec house = nearby[i];
                        float radius = Mathf.Max(house.footprint.x, house.footprint.y) * 0.95f;
                        float dx = worldX - house.position.x;
                        float dz = worldZ - house.position.z;
                        float sqrDist = dx * dx + dz * dz;
                        float sqrRadius = radius * radius;
                        if (sqrDist < sqrRadius)
                        {
                            float d = Mathf.Sqrt(sqrDist);
                            float t = 1f - Mathf.Clamp01(d / radius);
                            float padBase = 15f + (Mathf.PerlinNoise(house.position.x * 0.0003f, house.position.z * 0.0003f) - 0.5f) * 1.2f;
                            h = Mathf.Lerp(h, padBase, t * 0.95f);
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < layout.houses.Count; i++)
                {
                    HouseSpec house = layout.houses[i];
                    float radius = Mathf.Max(house.footprint.x, house.footprint.y) * 0.95f;
                    float dx = worldX - house.position.x;
                    float dz = worldZ - house.position.z;
                    float sqrDist = dx * dx + dz * dz;
                    float sqrRadius = radius * radius;
                    if (sqrDist < sqrRadius)
                    {
                        float d = Mathf.Sqrt(sqrDist);
                        float t = 1f - Mathf.Clamp01(d / radius);
                        float padBase = 15f + (Mathf.PerlinNoise(house.position.x * 0.0003f, house.position.z * 0.0003f) - 0.5f) * 1.2f;
                        h = Mathf.Lerp(h, padBase, t * 0.95f);
                    }
                }
            }

            float shoreSoftening = Mathf.Max(lakeMask * 0.75f, riverMask * 0.72f);
            if (shoreSoftening > 0.001f)
            {
                float shoreBase = layout.waterLevel + 1.0f + (Mathf.PerlinNoise(worldX * 0.0012f + 9f, worldZ * 0.0012f + 17f) - 0.5f) * 0.9f;
                h = Mathf.Lerp(h, shoreBase, shoreSoftening * 0.45f);
            }

            float fieldSoftening = WorldLayoutGenerator.ComputeFieldSofteningMask(layout, worldX, worldZ);
            if (fieldSoftening > 0.001f)
            {
                float fieldBase = 15f + (GeometryHelpers.FBM(worldX * 0.00028f + 82f, worldZ * 0.00028f + 12f, 2, 0.5f, 2f) - 0.5f) * 3f;
                h = Mathf.Lerp(h, fieldBase, fieldSoftening * 0.28f);
            }

            return Mathf.Clamp(h, 4f, layout.terrainHeightMeters * 0.72f);
        }

        private static float ComputeRoadMask(WorldLayout layout, float worldX, float worldZ)
        {
            float mask = 0f;
            Vector3 p = new Vector3(worldX, 0f, worldZ);
            for (int i = 0; i < layout.roads.Count; i++)
            {
                float d = GeometryHelpers.DistancePointPolylineXZ(p, layout.roads[i]);
                float influence = layout.roads[i].width * 2.1f;
                if (d < influence)
                {
                    float t = 1f - Mathf.Clamp01(d / influence);
                    mask = Mathf.Max(mask, t);
                }
            }
            return mask;
        }

        public static IEnumerator PaintTerrainGrid(BuildContext ctx, TerrainGrid grid, WorldLayout layout, Action onComplete = null, CancellationToken cancellationToken = default)
        {
            for (int tz = 0; tz < grid.tileCount; tz++)
            {
                for (int tx = 0; tx < grid.tileCount; tx++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Terrain terrain = grid.terrains[tx, tz];
                    if (terrain == null) continue;
                    TerrainData td = terrain.terrainData;
                    int w = td.alphamapWidth;
                    int h = td.alphamapHeight;
                    float[,,] map = new float[h, w, 4];
                    float x0 = tx * grid.tileSize;
                    float z0 = tz * grid.tileSize;
                    float invW = 1f / (w - 1);
                    float invH = 1f / (h - 1);

                    for (int y = 0; y < h; y++)
                    {
                        float wz = z0 + y * invH * grid.tileSize;
                        for (int x = 0; x < w; x++)
                        {
                            float wx = x0 + x * invW * grid.tileSize;
                            ComputeSplatWeights(layout, wx, wz, out float grass, out float wheat, out float dirt, out float forest);

                            float sum = grass + wheat + dirt + forest;
                            if (sum < 0.0001f)
                            {
                                grass = 1f;
                                sum = 1f;
                            }

                            map[y, x, 0] = grass / sum;
                            map[y, x, 1] = wheat / sum;
                            map[y, x, 2] = dirt / sum;
                            map[y, x, 3] = forest / sum;
                        }
                    }

                    td.SetAlphamaps(0, 0, map);
                    yield return null;
                }
            }
            onComplete?.Invoke();
        }

        public static void ComputeSplatWeights(WorldLayout layout, float wx, float wz, out float grass, out float wheat, out float dirt, out float forest)
        {
            forest = WorldLayoutGenerator.ComputeForestMask(layout, wx, wz);
            float village = WorldLayoutGenerator.ComputeVillageMask(layout, wx, wz);
            float road = WorldLayoutGenerator.ComputeRoadMask(layout, wx, wz);
            float house = WorldLayoutGenerator.ComputeHouseMask(layout, wx, wz);
            float lake = WorldLayoutGenerator.ComputeLakeMask(layout, wx, wz);
            float river = WorldLayoutGenerator.ComputeRiverMask(layout, wx, wz);
            float waterEdge = Mathf.Max(lake, river);
            float wheatLayer = WorldLayoutGenerator.ComputeWheatFieldMask(layout, wx, wz, out float fieldBorder);

            grass = 1f;
            dirt = Mathf.Max(road * 0.98f, house * 0.55f);
            dirt = Mathf.Max(dirt, fieldBorder * 0.38f * (1f - forest));
            dirt = Mathf.Max(dirt, village * 0.10f);
            dirt = Mathf.Max(dirt, waterEdge * 0.42f);
            wheatLayer *= (1f - road) * (1f - house) * (1f - forest) * (1f - waterEdge);
            forest *= (1f - road * 0.7f) * (1f - lake);

            grass *= 1f - dirt * 0.75f;
            grass *= 1f - wheatLayer * 0.72f;
            grass *= 1f - forest * 0.86f;
            grass *= 1f - waterEdge * 0.45f;
            grass = Mathf.Max(grass, village * 0.14f);

            wheat = wheatLayer;
        }

        public static IEnumerator PopulateTerrainDetails(BuildContext ctx, TerrainGrid grid, WorldLayout layout, GenerationSettings settings, Action onComplete = null, CancellationToken cancellationToken = default)
        {
            for (int tz = 0; tz < grid.tileCount; tz++)
            {
                for (int tx = 0; tx < grid.tileCount; tx++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Terrain terrain = grid.terrains[tx, tz];
                    if (terrain == null) continue;
                    TerrainData td = terrain.terrainData;
                    int res = td.detailWidth;
                    int[,] grass = new int[res, res];
                    int[,] wheat = new int[res, res];
                    float x0 = tx * grid.tileSize;
                    float z0 = tz * grid.tileSize;
                    float invRes = 1f / (res - 1);

                    for (int z = 0; z < res; z++)
                    {
                        float wz = z0 + z * invRes * grid.tileSize;
                        for (int x = 0; x < res; x++)
                        {
                            float wx = x0 + x * invRes * grid.tileSize;
                            float forest = WorldLayoutGenerator.ComputeForestMask(layout, wx, wz);
                            float village = WorldLayoutGenerator.ComputeVillageMask(layout, wx, wz);
                            float road = WorldLayoutGenerator.ComputeRoadMask(layout, wx, wz);
                            float house = WorldLayoutGenerator.ComputeHouseMask(layout, wx, wz);
                            float lake = WorldLayoutGenerator.ComputeLakeMask(layout, wx, wz);
                            float river = WorldLayoutGenerator.ComputeRiverMask(layout, wx, wz);
                            float water = Mathf.Max(lake, river);
                            if (forest > 0.20f || road > 0.08f || house > 0.10f || water > 0.04f)
                                continue;

                            float fieldBorder;
                            float wheatMask = WorldLayoutGenerator.ComputeWheatFieldMask(layout, wx, wz, out fieldBorder);
                            float localNoise = GeometryHelpers.Hash01(Mathf.FloorToInt(wx * 0.5f), Mathf.FloorToInt(wz * 0.5f), layout.seed + 801);
                            float dxVillage = wx - layout.villageCenter.x;
                            float dzVillage = wz - layout.villageCenter.z;
                            bool nearVillage = dxVillage * dxVillage + dzVillage * dzVillage < 950f * 950f;

                            if (wheatMask > 0.52f)
                            {
                                float densityMul = nearVillage ? 0.35f : 1f;
                                wheat[z, x] = Mathf.RoundToInt(Mathf.Lerp(3f, 10f + settings.qualityBoost * 2f, wheatMask) * densityMul * (0.7f + localNoise * 0.6f));
                            }
                            else
                            {
                                float meadow = Mathf.Max(0.16f, 1f - village * 0.55f) * (1f - fieldBorder * 0.65f);
                                grass[z, x] = Mathf.RoundToInt(Mathf.Lerp(2f, 8f + settings.qualityBoost * 2f, meadow) * (0.7f + localNoise * 0.6f));
                            }
                        }
                    }

                    td.SetDetailLayer(0, 0, 0, grass);
                    td.SetDetailLayer(0, 0, 1, wheat);
                    yield return null;
                }
            }
            onComplete?.Invoke();
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

    }
}
