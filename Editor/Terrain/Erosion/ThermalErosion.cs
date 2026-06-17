using AuraLiteWorldGenerator.Runtime;
using AuraLiteWorldGenerator.Editor.Core;

namespace AuraLiteWorldGenerator.Editor.Terrain.Erosion
{
    /// <summary>
    /// Thermal erosion simulation: steep slopes collapse, depositing material below.
    /// Creates talus slopes and terraced terrain features.
    /// </summary>
    public class ThermalErosion : ITerrainEroder
    {
        public float TalusAngle { get; set; } = 0.65f; // Max slope before collapse (radians)
        public float DepositFraction { get; set; } = 0.5f; // How much material moves per iteration

        public void Erode(float[,] heightmap, ErosionSettings settings)
        {
            int width = heightmap.GetLength(1);
            int height = heightmap.GetLength(0);
            
            float talusThreshold = Mathf.Tan(TalusAngle) * settings.Strength;
            SeededRandom random = new SeededRandom(settings.Seed);

            for (int iter = 0; iter < settings.Iterations; iter++)
            {
                // Pick random point
                int cx = random.Range(1, width - 2);
                int cz = random.Range(1, height - 2);
                float centerH = heightmap[cz, cx];

                // Check all 8 neighbors for slope
                float maxDiff = 0f;
                int maxDiffX = cx;
                int maxDiffZ = cz;

                for (int dz = -1; dz <= 1; dz++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dz == 0) continue;
                        int nx = cx + dx;
                        int nz = cz + dz;
                        if (nx < 0 || nx >= width || nz < 0 || nz >= height) continue;

                        float diff = centerH - heightmap[nz, nx];
                        float distance = (dx != 0 && dz != 0) ? 1.414f : 1f;
                        float slope = diff / distance;

                        if (slope > maxDiff)
                        {
                            maxDiff = slope;
                            maxDiffX = nx;
                            maxDiffZ = nz;
                        }
                    }
                }

                // If slope exceeds talus angle, move material
                if (maxDiff > talusThreshold)
                {
                    float excess = (maxDiff - talusThreshold) * DepositFraction;
                    heightmap[cz, cx] -= excess;
                    heightmap[maxDiffZ, maxDiffX] += excess;
                }
            }
        }
    }
}
