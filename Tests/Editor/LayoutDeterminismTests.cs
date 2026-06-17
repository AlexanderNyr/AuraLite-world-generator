using NUnit.Framework;
using AuraLiteWorldGenerator.Editor;
using AuraLiteWorldGenerator.Runtime;

namespace AuraLiteWorldGenerator.Tests
{
    public class LayoutDeterminismTests
    {
        [Test]
        public void GenerateLayout_WithSameSeed_ReturnsIdenticalLayouts()
        {
            var settings = new GenerationSettings { seed = 12345 };
            var layout1 = WorldLayoutGenerator.Generate(settings);
            
            var settings1 = new GenerationSettings { seed = 12345 };
            var layout2 = WorldLayoutGenerator.Generate(settings1);
            
            Assert.AreEqual(layout1.houses.Count, layout2.houses.Count);
            if (layout1.houses.Count > 0)
            {
                Assert.AreEqual(layout1.houses[0].position, layout2.houses[0].position);
                Assert.AreEqual(layout1.houses[0].kind, layout2.houses[0].kind);
            }
        }
    }
}
