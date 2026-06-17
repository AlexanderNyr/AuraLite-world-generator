using NUnit.Framework;
using UnityEngine;
using AuraLiteWorldGenerator.Editor.Roads;
using AuraLiteWorldGenerator.Editor.Core;

namespace AuraLiteWorldGenerator.Tests
{
    public class RoadStrategyTests
    {
        [Test]
        public void OrganicRoadStrategy_GeneratesPath()
        {
            var costMap = new float[10, 10];
            var request = new RoadNetworkRequest
            {
                CostMap = costMap,
                GridSize = new Vector2Int(10, 10),
                Start = new Vector2Int(1, 1),
                End = new Vector2Int(8, 8),
                Seed = 1
            };
            
            var strategy = new OrganicRoadStrategy();
            var network = strategy.Generate(request);
            
            Assert.AreEqual(1, network.Roads.Count);
            Assert.Greater(network.Roads[0].points.Count, 0);
        }
    }
}
