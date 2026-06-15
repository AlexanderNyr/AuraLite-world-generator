using System;
using System.Collections.Generic;
using UnityEngine;

namespace AuraLiteWorldGenerator.Runtime
{
    /// <summary>
    /// Simple runtime streaming component: toggles detailed roots on/off and replaces
    /// them with low-resolution HLOD proxies based on distance to the active camera.
    /// </summary>
    [AddComponentMenu("AuraLite/World Generator/Distance Chunk Activator")]
    public sealed class DistanceChunkActivator : MonoBehaviour
    {
        [Serializable]
        public class ChunkRule
        {
            public string name = "Chunk";
            [Tooltip("Detailed root shown when the camera is close.")] public GameObject nearRoot;
            [Tooltip("Low-poly proxy shown when the camera is far.")] public GameObject farRoot;
            [Tooltip("Distance at which the switch happens.")] public float switchDistance = 2000f;
            [Tooltip("Hysteresis to avoid flickering near the threshold.")] public float hysteresis = 100f;
        }

        public List<ChunkRule> chunks = new List<ChunkRule>();
        public float updateInterval = 0.25f;
        public bool drawGizmos = false;

        private readonly Dictionary<ChunkRule, Bounds> _boundsCache = new Dictionary<ChunkRule, Bounds>();
        private float _timer;

        private void Start()
        {
            RebuildBounds();
            ApplyImmediate();
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer < updateInterval)
                return;
            _timer = 0f;
            ApplyImmediate();
        }

        public void RebuildBounds()
        {
            _boundsCache.Clear();
            foreach (ChunkRule rule in chunks)
            {
                if (rule.nearRoot != null)
                    _boundsCache[rule] = CalculateBounds(rule.nearRoot);
                else if (rule.farRoot != null)
                    _boundsCache[rule] = CalculateBounds(rule.farRoot);
            }
        }

        public void ApplyImmediate()
        {
            Camera cam = Camera.main;
            if (cam == null)
                return;

            Vector3 cameraPosition = cam.transform.position;
            foreach (ChunkRule rule in chunks)
            {
                if (rule.nearRoot == null && rule.farRoot == null)
                    continue;

                float distance = EstimateDistanceToChunk(rule, cameraPosition);
                bool currentlyFar = rule.farRoot != null && rule.farRoot.activeSelf;
                float threshold = currentlyFar ? rule.switchDistance - rule.hysteresis : rule.switchDistance + rule.hysteresis;
                bool useFar = distance > threshold;

                if (rule.nearRoot != null)
                    rule.nearRoot.SetActive(!useFar);
                if (rule.farRoot != null)
                    rule.farRoot.SetActive(useFar);
            }
        }

        private float EstimateDistanceToChunk(ChunkRule rule, Vector3 cameraPosition)
        {
            if (_boundsCache.TryGetValue(rule, out Bounds bounds))
                return bounds.SqrDistance(cameraPosition) > 0f ? Mathf.Sqrt(bounds.SqrDistance(cameraPosition)) : 0f;

            if (rule.nearRoot != null)
                return Vector3.Distance(cameraPosition, rule.nearRoot.transform.position);
            if (rule.farRoot != null)
                return Vector3.Distance(cameraPosition, rule.farRoot.transform.position);
            return float.MaxValue;
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Bounds(root.transform.position, Vector3.one * 10f);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos)
                return;

            foreach (ChunkRule rule in chunks)
            {
                if (!_boundsCache.TryGetValue(rule, out Bounds bounds))
                    continue;

                Gizmos.color = rule.farRoot != null && rule.farRoot.activeSelf ? Color.gray : Color.green;
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }
        }
    }
}
