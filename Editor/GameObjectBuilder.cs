using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AuraLiteWorldGenerator.Editor
{
    /// <summary>
    /// Helper methods for creating primitive GameObjects, meshes and managing hierarchy/shadows.
    /// </summary>
    public static class GameObjectBuilder
    {
        public static GameObject CreateCubeChild(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material material)
        {
            return CreateCubeChild(parent, name, localPos, Quaternion.identity, localScale, material);
        }

        public static GameObject CreateCubeChild(Transform parent, string name, Vector3 localPos, Quaternion localRot, Vector3 localScale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = localScale;
            RemoveCollider(go);
            ApplyRenderer(go, material);
            return go;
        }

        public static GameObject CreateSphereChild(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            RemoveCollider(go);
            ApplyRenderer(go, material);
            return go;
        }

        public static GameObject CreateCylinderChild(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material material)
        {
            return CreateCylinderChild(parent, name, localPos, Quaternion.identity, localScale, material);
        }

        public static GameObject CreateCylinderChild(Transform parent, string name, Vector3 localPos, Quaternion localRot, Vector3 localScale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = localScale;
            RemoveCollider(go);
            ApplyRenderer(go, material);
            return go;
        }

        public static GameObject CreateMeshChild(Transform parent, string name, Mesh mesh, Vector3 localPos, Vector3 localScale, Material material)
        {
            GameObject go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = localScale;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            mr.shadowCastingMode = ShadowCastingMode.On;
            mr.receiveShadows = true;
            return go;
        }

        public static void RemoveCollider(GameObject go)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null)
                Object.DestroyImmediate(col);
        }

        public static void ApplyRenderer(GameObject go, Material material)
        {
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr == null) return;
            mr.sharedMaterial = material;
            mr.shadowCastingMode = ShadowCastingMode.On;
            mr.receiveShadows = true;
        }

        public static void SetShadowsRecursive(GameObject root, ShadowCastingMode castMode, bool receive)
        {
            MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].shadowCastingMode = castMode;
                renderers[i].receiveShadows = receive;
            }
        }

        public static void MoveAllChildrenExcept(Transform root, Transform target, Transform except)
        {
            List<Transform> children = new List<Transform>();
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child != target && child != except)
                    children.Add(child);
            }
            for (int i = 0; i < children.Count; i++)
                children[i].SetParent(target, false);
        }
    }
}
