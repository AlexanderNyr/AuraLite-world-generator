using NUnit.Framework;
using AuraLiteWorldGenerator.Editor.Terrain.Erosion;
using AuraLiteWorldGenerator.Editor.Core;

namespace AuraLiteWorldGenerator.Tests
{
    public class ErosionTests
    {
        [Test]
        public void HydraulicErosion_ChangesHeightmap()
        {
            float[,] heightmap = new float[16, 16];
            heightmap[8, 8] = 1f; // Peak
            
            var eroder = new HydraulicErosion();
            var settings = new ErosionSettings { Seed = 42, Iterations = 100, Strength = 1.0f };
            eroder.Erode(heightmap, settings);
            
            // Just check that it doesn't crash and changes some values
            Assert.AreNotEqual(1f, heightmap[8, 8]);
        }
    }
}
