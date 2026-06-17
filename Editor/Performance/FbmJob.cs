using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace AuraLiteWorldGenerator.Editor.Performance
{
    [BurstCompile]
    public struct FBMJob : IJobParallelFor
    {
        public int Width;
        public int Height;
        public float Scale;
        public float Seed;
        [WriteOnly] public NativeArray<float> Results;

        public void Execute(int index)
        {
            int x = index % Width;
            int y = index / Width;
            
            float nx = x * Scale + Seed;
            float ny = y * Scale + Seed;
            
            float noise = noise.snoise(new float2(nx, ny));
            Results[index] = (noise + 1f) * 0.5f;
        }
    }
}
