using System.IO;
using UnityEditor;
using UnityEngine;

namespace MagicLand.EditorTools
{
    /// <summary>Builds the wing mask texture, wing/body meshes and the butterfly swarm.</summary>
    public static class ButterflyBuilder
    {
        const float WingSpan = 0.11f;
        const float WingDepth = 0.16f;

        public static Texture2D CreateWingTexture(string folder)
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            Vector2[] spots = { new Vector2(0.8f, 0.82f), new Vector2(0.9f, 0.64f), new Vector2(0.64f, 0.92f), new Vector2(0.58f, 0.18f) };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size, v = (y + 0.5f) / size;
                    // Forewing (front, larger) and hindwing (back, rounder), as ellipses in UV space.
                    float fore = Ellipse(u, v, new Vector2(0.48f, 0.66f), new Vector2(0.5f, 0.3f), 18f);
                    float hind = Ellipse(u, v, new Vector2(0.36f, 0.3f), new Vector2(0.36f, 0.27f), -15f);
                    float e = Mathf.Min(fore, hind);
                    float alpha = Mathf.Clamp01((1f - e) * 12f);
                    float tint = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 0.85f, e));
                    tint *= Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(u * 6f));
                    float spot = 0f;
                    foreach (var s in spots)
                        spot = Mathf.Max(spot, 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.025f, 0.045f, Vector2.Distance(new Vector2(u, v), s))));
                    spot *= 1f - tint * 0.8f;
                    tex.SetPixel(x, y, new Color(tint, spot, 0f, alpha));
                }
            }
            tex.Apply();

            string path = folder + "/ButterflyWing.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.sRGBTexture = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static float Ellipse(float u, float v, Vector2 center, Vector2 radii, float angleDeg)
        {
            float a = angleDeg * Mathf.Deg2Rad;
            float du = u - center.x, dv = v - center.y;
            float ru = du * Mathf.Cos(a) + dv * Mathf.Sin(a);
            float rv = -du * Mathf.Sin(a) + dv * Mathf.Cos(a);
            return Mathf.Sqrt(ru * ru / (radii.x * radii.x) + rv * rv / (radii.y * radii.y));
        }

        static Mesh WingMesh()
        {
            var mesh = new Mesh { name = "Butterfly Wing" };
            mesh.vertices = new[]
            {
                new Vector3(0f, 0f, -WingDepth * 0.5f), new Vector3(WingSpan, 0f, -WingDepth * 0.5f),
                new Vector3(WingSpan, 0f, WingDepth * 0.5f), new Vector3(0f, 0f, WingDepth * 0.5f),
            };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            return MagicLandSetup.SaveMesh(mesh, "Butterfly Wing");
        }

        static Mesh BodyMesh()
        {
            var kit = new MeshKit();
            var dark = new Color(0.15f, 0.12f, 0.16f);
            kit.AddEllipsoid(new Vector3(0f, 0f, -0.01f), new Vector3(0.006f, 0.006f, 0.03f), Quaternion.identity, 1, _ => dark);
            kit.AddEllipsoid(new Vector3(0f, 0.002f, 0.026f), Vector3.one * 0.007f, Quaternion.identity, 0, _ => dark);
            for (int side = -1; side <= 1; side += 2)
                kit.AddTube(new Vector3(side * 0.003f, 0.005f, 0.03f), new Vector3(side * 0.014f, 0.016f, 0.06f), 0.0015f, 0.001f, 3, _ => dark, false, true);
            return MagicLandSetup.SaveMesh(kit.ToMesh("Butterfly Body"), "Butterfly Body");
        }

        public static Transform Create(Transform parent, Material bodyMat, Material[] wingMats)
        {
            var wingMesh = WingMesh();
            var bodyMesh = BodyMesh();
            var root = new GameObject("Butterflies");
            root.transform.SetParent(parent, false);
            root.AddComponent<ButterflyGuide>();

            var rnd = new System.Random(11);
            float R(float min, float max) => min + (float)rnd.NextDouble() * (max - min);

            for (int i = 0; i < 16; i++)
            {
                bool near = i < 4;
                Vector3 pos;
                if (near)
                {
                    pos = new Vector3(R(-0.7f, 0.7f), R(-0.35f, 0.3f), R(1.3f, 2.3f));
                }
                else
                {
                    float z = R(3f, 11f);
                    float x = R(-1f, 1f) * (2f + z * 0.35f);
                    pos = new Vector3(x, ForestGround.HeightAt(x, z) + R(0.5f, 1.6f), z);
                }

                var go = new GameObject("Butterfly " + i);
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = pos;
                go.transform.localRotation = Quaternion.Euler(0f, R(0f, 360f), 0f);

                var body = new GameObject("Body");
                body.transform.SetParent(go.transform, false);
                body.AddComponent<MeshFilter>().sharedMesh = bodyMesh;
                SetupRenderer(body.AddComponent<MeshRenderer>(), bodyMat);

                Material wingMat = wingMats[i % wingMats.Length];
                var right = Wing("Wing R", go.transform, wingMesh, wingMat, 1f);
                var left = Wing("Wing L", go.transform, wingMesh, wingMat, -1f);

                float scale = near ? 0.75f : R(0.85f, 1.15f);
                go.transform.localScale = Vector3.one * scale;
                go.AddComponent<Butterfly>().Configure(left, right, near ? 0.8f : R(1.2f, 2f), 100 + i, near ? 0.3f : 0.15f, near ? 0.9f : 0.7f);
            }
            return root.transform;
        }

        static Transform Wing(string name, Transform parent, Mesh mesh, Material mat, float side)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(side * 0.004f, 0.002f, 0f);
            go.transform.localScale = new Vector3(side, 1f, 1f);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            SetupRenderer(go.AddComponent<MeshRenderer>(), mat);
            return go.transform;
        }

        static void SetupRenderer(MeshRenderer r, Material mat)
        {
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
    }
}
