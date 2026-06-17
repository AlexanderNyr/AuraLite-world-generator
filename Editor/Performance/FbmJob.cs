using Unity.Jobs;
using UnityEngine;

namespace AuraLiteWorldGenerator.Editor.Performance
{
    /// <summary>
    /// Plain (non-Burst) parallel FBM noise job. Kept here as a ready-to-use building block
    /// for future parallel procedural work without forcing Burst to scan the Editor assembly.
    /// Intentionally avoids [BurstCompile] so that the Burst entry-point scanner does not
    /// have to resolve AuraLiteWorldGenerator.Editor types when compiling other packages.
    /// </summary>
    public struct FbmJob : IJobParallelFor
    {
        public int Width;
        public int Height;
        public float Scale;
        public float Seed;
        public int Octaves;
        public float Persistence;
        public float Lacunarity;

        public float[] Results;

        public void Execute(int index)
        {
            int x = index % Width;
            int y = index / Width;

            float amplitude = 1f;
            float frequency = Scale;
            float noiseValue = 0f;
            float maxVal = 0f;

            for (int i = 0; i < Octaves; i++)
            {
                float nx = x * frequency + Seed;
                float ny = y * frequency + Seed;

                noiseValue += Mathf.PerlinNoise(nx, ny) * amplitude;
                maxVal += amplitude;

                amplitude *= Persistence;
                frequency *= Lacunarity;
            }

            Results[index] = maxVal > 0f ? (noiseValue / maxVal) : 0f;
        }
    }
}
