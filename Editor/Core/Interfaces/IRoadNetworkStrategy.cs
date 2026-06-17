using System.Collections.Generic;
using UnityEngine;

namespace AuraLiteWorldGenerator.Editor.Core
{
    public class RoadNetworkRequest
    {
        public Vector3 Bounds;
        public int Seed;
        // ... other parameters
    }

    public class RoadNetwork
    {
        public List<RoadPath> Roads = new List<RoadPath>();
    }

    public interface IRoadNetworkStrategy
    {
        RoadNetwork Generate(RoadNetworkRequest request);
    }
}
