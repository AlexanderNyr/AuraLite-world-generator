using System.Collections.Generic;
using UnityEngine;

namespace AuraLiteWorldGenerator.Editor.Core
{
    public class RoadNetworkRequest
    {
        public Vector3 Bounds;
        public int Seed;
        public Vector2Int GridSize;
        public float[,] CostMap;
        public Vector2Int Start;
        public Vector2Int End;
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
