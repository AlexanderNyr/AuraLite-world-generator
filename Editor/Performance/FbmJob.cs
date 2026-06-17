using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace AuraLiteWorldGenerator.Editor.Performance
{
#if UNITY_BURST
    [BurstCompile]
#endif
    public struct FBMJob : IJobParallelFor
    {
        public int Width;
        public int Height;
        public float Scale;
        public float Seed;
        public int Octaves;
        public float Persistence;
        public float Lacunarity;
        
        [WriteOnly] public NativeArray<float> Results;

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
                
                noiseValue += (noise.snoise(new float2(nx, ny)) * amplitude);
                maxVal += amplitude;
                
                amplitude *= Persistence;
                frequency *= Lacunarity;
            }
            
            Results[index] = ((noiseValue / maxVal) + 1f) * 0.5f;
        }
    }
}
