using System.Collections.Generic;
using AuraLiteWorldGenerator.Editor.Core;
using UnityEngine;

namespace AuraLiteWorldGenerator.Editor.Roads
{
    public class OrganicRoadStrategy : IRoadNetworkStrategy
    {
        public RoadNetwork Generate(RoadNetworkRequest request)
        {
            var network = new RoadNetwork();
            // A* pathfinding on cost map logic
            return network;
        }
    }
}
