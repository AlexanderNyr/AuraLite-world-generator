using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text;

namespace AuraLiteWorldGenerator.Editor.Export
{
    public static class WorldExporter
    {
        public static void ExportToFBX(GameObject root, string path)
        {
            Debug.Log($"Exporting {root.name} to {path} as OBJ (fallback)...");
            var pathDir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(pathDir) && !Directory.Exists(pathDir))
            {
                Directory.CreateDirectory(pathDir);
            }

            if (!path.EndsWith(".obj")) path = path.Replace(".fbx", ".obj").Replace(".FBX", ".obj");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"# Exported from AuraLite World Generator");
            
            MeshFilter[] mfs = root.GetComponentsInChildren<MeshFilter>();
            int vertexOffset = 0;
            
            foreach (var mf in mfs)
            {
                if (mf.sharedMesh == null) continue;
                var mesh = mf.sharedMesh;
                var transform = mf.transform;
                
                sb.AppendLine($"o {mf.gameObject.name}_{mf.GetInstanceID()}");
                
                var vertices = mesh.vertices;
                foreach (var v in vertices)
                {
                    Vector3 worldV = transform.TransformPoint(v);
                    sb.AppendLine($"v {-worldV.x} {worldV.y} {worldV.z}");
                }
                
                var normals = mesh.normals;
                if (normals != null && normals.Length > 0)
                {
                    foreach (var n in normals)
                    {
                        Vector3 worldN = transform.TransformDirection(n);
                        sb.AppendLine($"vn {-worldN.x} {worldN.y} {worldN.z}");
                    }
                }
                
                var uvs = mesh.uv;
                if (uvs != null && uvs.Length > 0)
                {
                    foreach (var uv in uvs)
                    {
                        sb.AppendLine($"vt {uv.x} {uv.y}");
                    }
                }
                
                for (int m = 0; m < mesh.subMeshCount; m++)
                {
                    var triangles = mesh.GetTriangles(m);
                    for (int i = 0; i < triangles.Length; i += 3)
                    {
                        int t0 = triangles[i] + 1 + vertexOffset;
                        int t1 = triangles[i + 1] + 1 + vertexOffset;
                        int t2 = triangles[i + 2] + 1 + vertexOffset;
                        
                        string v0 = (normals.Length > 0 && uvs.Length > 0) ? $"{t0}/{t0}/{t0}" : $"{t0}";
                        string v1 = (normals.Length > 0 && uvs.Length > 0) ? $"{t1}/{t1}/{t1}" : $"{t1}";
                        string v2 = (normals.Length > 0 && uvs.Length > 0) ? $"{t2}/{t2}/{t2}" : $"{t2}";
                        
                        sb.AppendLine($"f {v0} {v2} {v1}");
                    }
                }
                
                vertexOffset += vertices.Length;
            }
            
            File.WriteAllText(path, sb.ToString());
            AssetDatabase.Refresh();
        }

        public static void ExportHeightmap(UnityEngine.Terrain terrain, string path)
        {
            var pathDir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(pathDir) && !Directory.Exists(pathDir))
            {
                Directory.CreateDirectory(pathDir);
            }
            
            int res = terrain.terrainData.heightmapResolution;
            float[,] heights = terrain.terrainData.GetHeights(0, 0, res, res);
            
            string rawPath = path.EndsWith(".png") ? path.Replace(".png", ".raw") : path + ".raw";
            using (var stream = File.Open(rawPath, FileMode.Create))
            {
                using (var writer = new BinaryWriter(stream))
                {
                    for (int y = 0; y < res; y++)
                    {
                        for (int x = 0; x < res; x++)
                        {
                            ushort v = (ushort)(Mathf.Clamp01(heights[y, x]) * 65535f);
                            writer.Write(v);
                        }
                    }
                }
            }

            string pngPath = path.EndsWith(".png") ? path : path + ".png";
            Texture2D tex = new Texture2D(res, res, TextureFormat.R16, false);
            Color[] colors = new Color[res * res];
            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float h = heights[y, x];
                    colors[y * res + x] = new Color(h, h, h, 1f);
                }
            }
            tex.SetPixels(colors);
            tex.Apply();
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(pngPath, bytes);
            Object.DestroyImmediate(tex);
            
            AssetDatabase.Refresh();
        }
    }
}
