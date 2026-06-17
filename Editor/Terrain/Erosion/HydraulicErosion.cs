using UnityEngine;
using AuraLiteWorldGenerator.Runtime;
using AuraLiteWorldGenerator.Editor.Core;

namespace AuraLiteWorldGenerator.Editor.Terrain.Erosion
{
    public class HydraulicErosion : ITerrainEroder
    {
        public void Erode(float[,] heightmap, ErosionSettings settings)
        {
            int width = heightmap.GetLength(1);
            int height = heightmap.GetLength(0);
            
            SeededRandom random = new SeededRandom(settings.Seed);
            float strengthMultiplier = settings.Strength;
            
            for (int i = 0; i < settings.Iterations; i++)
            {
                float posX = random.Range(0f, width - 2);
                float posY = random.Range(0f, height - 2);
                
                float dirX = 0f;
                float dirY = 0f;
                float speed = 1f;
                float water = 1f;
                float sediment = 0f;
                
                for (int step = 0; step < 60; step++)
                {
                    int nodeX = (int)posX;
                    int nodeY = (int)posY;
                    float u = posX - nodeX;
                    float v = posY - nodeY;

                    float h00 = heightmap[nodeY, nodeX];
                    float h10 = heightmap[nodeY, nodeX + 1];
                    float h01 = heightmap[nodeY + 1, nodeX];
                    float h11 = heightmap[nodeY + 1, nodeX + 1];

                    float gradX = (h10 - h00) * (1 - v) + (h11 - h01) * v;
                    float gradY = (h01 - h00) * (1 - u) + (h11 - h10) * u;

                    dirX = (dirX * 0.3f - gradX * 0.7f);
                    dirY = (dirY * 0.3f - gradY * 0.7f);
                    
                    float len = Mathf.Sqrt(dirX * dirX + dirY * dirY);
                    if (len > 0.0001f)
                    {
                        dirX /= len;
                        dirY /= len;
                    }
                    else
                    {
                        dirX = random.Range(-1f, 1f);
                        dirY = random.Range(-1f, 1f);
                    }

                    posX += dirX;
                    posY += dirY;

                    if (posX < 0 || posX >= width - 2 || posY < 0 || posY >= height - 2)
                        break;

                    int newNodeX = (int)posX;
                    int newNodeY = (int)posY;
                    float newU = posX - newNodeX;
                    float newV = posY - newNodeY;

                    float hNew00 = heightmap[newNodeY, newNodeX];
                    float hNew10 = heightmap[newNodeY, newNodeX + 1];
                    float hNew01 = heightmap[newNodeY + 1, newNodeX];
                    float hNew11 = heightmap[newNodeY + 1, newNodeX + 1];

                    float newHeight = hNew00 * (1 - newU) * (1 - newV) + hNew10 * newU * (1 - newV) + hNew01 * (1 - newU) * newV + hNew11 * newU * newV;
                    float oldHeight = h00 * (1 - u) * (1 - v) + h10 * u * (1 - v) + h01 * (1 - u) * v + h11 * u * v;

                    float deltaHeight = newHeight - oldHeight;
                    
                    float capacity = Mathf.Max(-deltaHeight * speed * water * 4f, 0.01f);

                    if (sediment > capacity || deltaHeight > 0)
                    {
                        float drop = (deltaHeight > 0) ? Mathf.Min(deltaHeight, sediment) : (sediment - capacity) * 0.2f;
                        sediment -= drop;
                        
                        heightmap[nodeY, nodeX] += drop * (1 - u) * (1 - v) * strengthMultiplier;
                        heightmap[nodeY, nodeX + 1] += drop * u * (1 - v) * strengthMultiplier;
                        heightmap[nodeY + 1, nodeX] += drop * (1 - u) * v * strengthMultiplier;
                        heightmap[nodeY + 1, nodeX + 1] += drop * u * v * strengthMultiplier;
                    }
                    else
                    {
                        float grab = Mathf.Min((capacity - sediment) * 0.2f, -deltaHeight);
                        sediment += grab;
                        
                        heightmap[nodeY, nodeX] -= grab * (1 - u) * (1 - v) * strengthMultiplier;
                        heightmap[nodeY, nodeX + 1] -= grab * u * (1 - v) * strengthMultiplier;
                        heightmap[nodeY + 1, nodeX] -= grab * (1 - u) * v * strengthMultiplier;
                        heightmap[nodeY + 1, nodeX + 1] -= grab * u * v * strengthMultiplier;
                    }
                    
                    speed = Mathf.Sqrt(Mathf.Max(0.1f, speed * speed + deltaHeight * 4f));
                    water *= 0.99f;
                }
            }
        }
    }
}
