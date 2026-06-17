using NUnit.Framework;
using AuraLiteWorldGenerator.Runtime;

namespace AuraLiteWorldGenerator.Tests
{
    public class SeededRandomTests
    {
        [Test]
        public void SeededRandom_IsDeterministic()
        {
            int seed = 12345;
            var rand1 = new SeededRandom(seed);
            var rand2 = new SeededRandom(seed);
            
            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(rand1.Value, rand2.Value);
            }
        }

        [Test]
        public void SeededRandom_Range_IsWithinBounds()
        {
            var rand = new SeededRandom(1337);
            for (int i = 0; i < 1000; i++)
            {
                float val = rand.Range(10f, 20f);
                Assert.GreaterOrEqual(val, 10f);
                Assert.Less(val, 20f);
            }
        }
    }
}
