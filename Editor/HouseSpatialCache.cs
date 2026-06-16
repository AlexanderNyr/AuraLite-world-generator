using System.Collections.Generic;
using UnityEngine;

namespace AuraLiteWorldGenerator.Editor
{
    /// <summary>
    /// Uniform grid spatial index for HouseSpec instances. Stores a house only in cells
    /// that its influence radius overlaps, so queries for nearby houses are O(1) per cell.
    /// </summary>
    public sealed class HouseSpatialCache
    {
        private readonly List<HouseSpec>[,] _grid;
        private readonly int _resolution;
        private readonly float _cellSize;
        private readonly float _worldSize;

        public HouseSpatialCache(WorldLayout layout, float cellSize = 120f)
        {
            if (layout == null)
                throw new System.ArgumentNullException(nameof(layout));

            _worldSize = layout.worldSizeMeters;
            _cellSize = cellSize;
            _resolution = Mathf.Max(1, Mathf.CeilToInt(_worldSize / _cellSize));
            _grid = new List<HouseSpec>[_resolution, _resolution];

            for (int i = 0; i < layout.houses.Count; i++)
            {
                HouseSpec h = layout.houses[i];
                // Radius covers both the terrain flattening (0.95 * footprint) and the house mask
                // falloff (0.9 * footprint * 1.2). A small margin is added to avoid edge cases.
                float radius = Mathf.Max(h.footprint.x, h.footprint.y) * 1.25f;
                int minX = Mathf.FloorToInt((h.position.x - radius) / _cellSize);
                int maxX = Mathf.CeilToInt((h.position.x + radius) / _cellSize);
                int minZ = Mathf.FloorToInt((h.position.z - radius) / _cellSize);
                int maxZ = Mathf.CeilToInt((h.position.z + radius) / _cellSize);

                for (int z = Mathf.Max(0, minZ); z <= Mathf.Min(_resolution - 1, maxZ); z++)
                {
                    for (int x = Mathf.Max(0, minX); x <= Mathf.Min(_resolution - 1, maxX); x++)
                    {
                        if (_grid[x, z] == null)
                            _grid[x, z] = new List<HouseSpec>();
                        _grid[x, z].Add(h);
                    }
                }
            }
        }

        /// <summary>Returns the list of houses whose influence radius overlaps the cell at (worldX, worldZ), or null.</summary>
        public List<HouseSpec> GetNearby(float worldX, float worldZ)
        {
            int x = Mathf.FloorToInt(worldX / _cellSize);
            int z = Mathf.FloorToInt(worldZ / _cellSize);
            if (x < 0 || x >= _resolution || z < 0 || z >= _resolution)
                return null;
            return _grid[x, z];
        }
    }
}
