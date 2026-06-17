using NUnit.Framework;
using UnityEngine;
using AuraLiteWorldGenerator.Editor;

namespace AuraLiteWorldGenerator.Tests
{
    public class GeometryHelpersTests
    {
        [Test]
        public void FBM_ReturnsValueIn01Range()
        {
            float val = GeometryHelpers.FBM(1.5f, 2.5f, 3, 0.5f, 2f);
            Assert.IsTrue(val >= 0f && val <= 1f);
        }

        [Test]
        public void DistancePointSegmentXZ_ReturnsCorrectDistance()
        {
            Vector3 p = new Vector3(2f, 0f, 0f);
            Vector3 a = new Vector3(0f, 0f, 0f);
            Vector3 b = new Vector3(4f, 0f, 0f);
            Assert.AreEqual(0f, GeometryHelpers.DistancePointSegmentXZ(p, a, b));
            
            Vector3 p2 = new Vector3(2f, 0f, 3f);
            Assert.AreEqual(3f, GeometryHelpers.DistancePointSegmentXZ(p2, a, b));
        }

        [Test]
        public void IntersectRects_ReturnsTrueWhenOverlapping()
        {
            var p1 = new Vector2(0f, 0f);
            var s1 = new Vector2(2f, 2f);
            var p2 = new Vector2(1f, 1f);
            var s2 = new Vector2(2f, 2f);
            Assert.IsTrue(GeometryHelpers.IntersectRects(p1, s1, 0f, p2, s2, 0f));
        }
    }
}
