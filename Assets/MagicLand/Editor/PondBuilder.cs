using System.Collections.Generic;
using UnityEngine;

namespace MagicLand.EditorTools
{
    /// <summary>Creates a small pond on the left of the clearing with ducks, frogs and reeds.</summary>
    public static class PondBuilder
    {
        const float RadiusX = 2.35f;
        const float RadiusZ = 1.7f;

        static Mesh MakeMesh(string name, System.Action<MeshKit> build)
        {
            var kit = new MeshKit();
            build(kit);
            return MagicLandSetup.SaveMesh(kit.ToMesh(name), name);
        }

        static Transform Part(string name, Transform parent, Vector3 position, Mesh mesh, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go.transform;
        }

        public static void Create(Transform parent, Material water, Material nature, AudioClip frogCall)
        {
            Vector2 center = ForestGround.PondCenter;
            float groundY = ForestGround.HeightAt(center.x, center.y);
            var root = new GameObject("Quiet Pond");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(center.x, groundY, center.y);

            float surface = SurfaceHeight(center, groundY);
            Mesh waterMesh = WaterSurface(surface);
            var waterGo = new GameObject("Pond Water");
            waterGo.transform.SetParent(root.transform, false);
            waterGo.AddComponent<MeshFilter>().sharedMesh = waterMesh;
            var renderer = waterGo.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = water;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var shore = MakeMesh("Pond Shore", kit =>
            {
                var rnd = new System.Random(55);
                for (int i = 0; i < 24; i++)
                {
                    float a = i / 24f * Mathf.PI * 2f;
                    float x = Mathf.Cos(a) * (RadiusX + 0.12f);
                    float z = Mathf.Sin(a) * (RadiusZ + 0.12f);
                    float size = 0.18f + (float)rnd.NextDouble() * 0.18f;
                    float y = ForestGround.HeightAt(center.x + x, center.y + z) - groundY;
                    Vector3 p = new Vector3(x, y, z);
                    Color stone = Color.Lerp(new Color(0.38f, 0.42f, 0.35f), new Color(0.58f, 0.57f, 0.47f), (float)rnd.NextDouble());
                    kit.AddEllipsoid(p, new Vector3(size, size * 0.52f, size * 0.72f),
                        Quaternion.Euler(0f, a * Mathf.Rad2Deg, 0f), 1, _ => stone, 0.12f, i);
                }

                for (int i = 0; i < 13; i++)
                {
                    float a = (i + 0.35f) / 13f * Mathf.PI * 2f;
                    float x = Mathf.Cos(a) * (RadiusX + 0.22f);
                    float z = Mathf.Sin(a) * (RadiusZ + 0.2f);
                    float y = ForestGround.HeightAt(center.x + x, center.y + z) - groundY;
                    Vector3 basePoint = new Vector3(x, y, z);
                    Color reed = new Color(0.34f, 0.49f, 0.23f);
                    for (int blade = 0; blade < 3; blade++)
                    {
                        float angle = a + (blade - 1) * 0.28f;
                        Vector3 tip = basePoint + new Vector3(Mathf.Cos(angle) * 0.18f, 0.5f + 0.12f * (blade % 2), Mathf.Sin(angle) * 0.18f);
                        kit.AddTube(basePoint, tip, 0.025f, 0.008f, 5, _ => reed, false, false);
                    }
                }
            });
            Part("Reeds and Shore Stones", root.transform, Vector3.zero, shore, nature);

            Transform[] ducks = new Transform[2];
            for (int i = 0; i < ducks.Length; i++)
            {
                float x = i == 0 ? -0.62f : 0.52f;
                float z = i == 0 ? -0.12f : 0.28f;
                ducks[i] = Part("Duck " + (i + 1), root.transform, new Vector3(x, surface + 0.08f, z),
                    DuckMesh(i), nature);
            }

            Transform[] frogs = new Transform[3];
            Vector3[] frogPositions =
            {
                new Vector3(-1.45f, 0.04f, 1.1f), new Vector3(1.65f, 0.04f, 0.52f), new Vector3(-0.1f, 0.04f, -1.42f)
            };
            for (int i = 0; i < frogs.Length; i++)
                frogs[i] = Part("Frog " + (i + 1), root.transform, frogPositions[i], FrogMesh(i), nature);

            root.AddComponent<PondLife>().Configure(ducks, frogs, frogCall);
        }

        static Mesh WaterSurface(float y)
        {
            const int rings = 5, segments = 48;
            var vertices = new List<Vector3> { new Vector3(0f, y, 0f) };
            var uvs = new List<Vector2> { new Vector2(0.5f, 0.45f) };
            var triangles = new List<int>();
            for (int ring = 1; ring <= rings; ring++)
            {
                float radius = ring / (float)rings;
                for (int segment = 0; segment < segments; segment++)
                {
                    float angle = segment * Mathf.PI * 2f / segments;
                    float x = Mathf.Cos(angle) * RadiusX * radius;
                    float z = Mathf.Sin(angle) * RadiusZ * radius;
                    vertices.Add(new Vector3(x, y, z));
                    uvs.Add(new Vector2(0.5f + x / (2f * RadiusX) * 0.7f, 0.45f + z / (2f * RadiusZ) * 0.5f));
                }
            }

            for (int i = 0; i < segments; i++)
                triangles.AddRange(new[] { 0, 1 + i, 1 + (i + 1) % segments });
            for (int ring = 1; ring < rings; ring++)
            {
                int inner = 1 + (ring - 1) * segments;
                int outer = inner + segments;
                for (int segment = 0; segment < segments; segment++)
                {
                    int next = (segment + 1) % segments;
                    triangles.AddRange(new[] { inner + segment, outer + segment, inner + next,
                        inner + next, outer + segment, outer + next });
                }
            }
            var mesh = new Mesh { name = "Pond Water" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return MagicLandSetup.SaveMesh(mesh, "Pond Water");
        }

        static float SurfaceHeight(Vector2 center, float groundY)
        {
            float highest = 0f;
            for (int ring = 0; ring <= 4; ring++)
            {
                float radius = ring / 4f;
                for (int segment = 0; segment < 32; segment++)
                {
                    float angle = segment * Mathf.PI * 2f / 32f;
                    float x = Mathf.Cos(angle) * RadiusX * radius;
                    float z = Mathf.Sin(angle) * RadiusZ * radius;
                    highest = Mathf.Max(highest, ForestGround.HeightAt(center.x + x, center.y + z) - groundY);
                }
            }
            return highest + 0.06f;
        }

        static Mesh DuckMesh(int index)
        {
            Color grey = new Color(0.72f, 0.72f, 0.66f);
            Color breast = new Color(0.38f, 0.19f, 0.12f);
            Color green = index == 0 ? new Color(0.12f, 0.44f, 0.24f) : new Color(0.13f, 0.39f, 0.28f);
            Color dark = new Color(0.1f, 0.12f, 0.13f);
            Color bill = new Color(0.98f, 0.76f, 0.18f);
            return MakeMesh("Pond Duck " + index, kit =>
            {
                kit.AddEllipsoid(new Vector3(0f, 0.055f, -0.045f), new Vector3(0.19f, 0.16f, 0.32f),
                    Quaternion.Euler(-5f, 0f, 0f), 2,
                    p => p.z > 0.15f ? breast : Color.Lerp(grey, new Color(0.42f, 0.4f, 0.34f), Mathf.Clamp01((p.y - 0.06f) * 3f)));
                kit.AddEllipsoid(new Vector3(0f, 0.125f, 0.18f), new Vector3(0.14f, 0.14f, 0.135f),
                    Quaternion.Euler(-12f, 0f, 0f), 2, _ => breast);

                kit.AddTube(new Vector3(0f, 0.2f, 0.2f), new Vector3(0f, 0.37f, 0.235f),
                    0.068f, 0.073f, 12, _ => green, false, false);
                kit.AddTube(new Vector3(0f, 0.27f, 0.214f), new Vector3(0f, 0.294f, 0.219f),
                    0.077f, 0.077f, 12, _ => new Color(0.96f, 0.95f, 0.87f), false, false);
                kit.AddEllipsoid(new Vector3(0f, 0.405f, 0.245f), new Vector3(0.1f, 0.105f, 0.115f),
                    Quaternion.identity, 2, p => Color.Lerp(green, new Color(0.22f, 0.57f, 0.32f), Mathf.Clamp01((p.y - 0.4f) * 4f)));

                // A broad, thin bill is essential to the duck silhouette; a cone reads as a beak on a songbird.
                kit.AddEllipsoid(new Vector3(0f, 0.356f, 0.401f), new Vector3(0.065f, 0.009f, 0.095f),
                    Quaternion.identity, 2, _ => new Color(0.53f, 0.37f, 0.1f));
                kit.AddEllipsoid(new Vector3(0f, 0.372f, 0.402f), new Vector3(0.069f, 0.021f, 0.103f),
                    Quaternion.Euler(3f, 0f, 0f), 2, _ => bill);
                kit.AddEllipsoid(new Vector3(0f, 0.369f, 0.499f), new Vector3(0.021f, 0.008f, 0.009f),
                    Quaternion.identity, 1, _ => dark);

                for (int side = -1; side <= 1; side += 2)
                {
                    kit.AddEllipsoid(new Vector3(side * 0.166f, 0.09f, -0.08f), new Vector3(0.037f, 0.1f, 0.22f),
                        Quaternion.Euler(0f, side * 5f, 0f), 2, _ => new Color(0.53f, 0.51f, 0.44f));
                    kit.AddEllipsoid(new Vector3(side * 0.198f, 0.09f, -0.16f), new Vector3(0.012f, 0.047f, 0.11f),
                        Quaternion.identity, 1, _ => new Color(0.95f, 0.94f, 0.87f));
                    kit.AddEllipsoid(new Vector3(side * 0.207f, 0.092f, -0.16f), new Vector3(0.009f, 0.031f, 0.093f),
                        Quaternion.identity, 1, _ => new Color(0.2f, 0.32f, 0.74f));

                    kit.AddEllipsoid(new Vector3(side * 0.082f, 0.43f, 0.3f), new Vector3(0.014f, 0.016f, 0.013f),
                        Quaternion.identity, 1, _ => dark);
                    kit.AddEllipsoid(new Vector3(side * 0.089f, 0.435f, 0.306f), Vector3.one * 0.004f,
                        Quaternion.identity, 1, _ => Color.white);
                    kit.AddEllipsoid(new Vector3(side * 0.03f, 0.39f, 0.39f), new Vector3(0.006f, 0.003f, 0.009f),
                        Quaternion.identity, 1, _ => new Color(0.35f, 0.29f, 0.1f));
                }

                kit.AddEllipsoid(new Vector3(0f, 0.075f, -0.345f), new Vector3(0.105f, 0.044f, 0.115f),
                    Quaternion.Euler(12f, 0f, 0f), 1, _ => new Color(0.87f, 0.86f, 0.75f));
                kit.AddEllipsoid(new Vector3(0f, 0.13f, -0.28f), new Vector3(0.115f, 0.06f, 0.1f),
                    Quaternion.identity, 1, _ => dark);
                kit.AddTube(new Vector3(0f, 0.145f, -0.32f), new Vector3(0f, 0.2f, -0.42f),
                    0.019f, 0.016f, 6, _ => dark, false, false);
                kit.AddTube(new Vector3(0f, 0.2f, -0.42f), new Vector3(0f, 0.215f, -0.385f),
                    0.016f, 0.007f, 6, _ => dark, false, true);
            });
        }

        static Mesh FrogMesh(int index)
        {
            Color green = index == 1 ? new Color(0.45f, 0.68f, 0.38f) : new Color(0.34f, 0.59f, 0.32f);
            return MakeMesh("Pond Frog " + index, kit =>
            {
                kit.AddEllipsoid(new Vector3(0f, 0.09f, 0f), new Vector3(0.17f, 0.12f, 0.16f),
                    Quaternion.identity, 2, p => p.y < 0.03f ? Color.Lerp(green, new Color(0.78f, 0.82f, 0.57f), 0.6f) : green);
                for (int side = -1; side <= 1; side += 2)
                {
                    kit.AddEllipsoid(new Vector3(side * 0.105f, 0.2f, 0.07f), new Vector3(0.065f, 0.075f, 0.065f),
                        Quaternion.identity, 1, _ => green);
                    kit.AddEllipsoid(new Vector3(side * 0.108f, 0.215f, 0.12f), new Vector3(0.027f, 0.034f, 0.022f),
                        Quaternion.identity, 1, _ => new Color(0.95f, 0.9f, 0.63f));
                    kit.AddEllipsoid(new Vector3(side * 0.11f, 0.218f, 0.14f), new Vector3(0.012f, 0.02f, 0.011f),
                        Quaternion.identity, 1, _ => new Color(0.14f, 0.12f, 0.1f));
                    kit.AddEllipsoid(new Vector3(side * 0.18f, 0.035f, 0.02f), new Vector3(0.09f, 0.04f, 0.11f),
                        Quaternion.Euler(0f, side * 12f, 0f), 1, _ => green * 0.9f);
                }
                kit.AddEllipsoid(new Vector3(0f, 0.1f, 0.15f), new Vector3(0.055f, 0.023f, 0.015f),
                    Quaternion.identity, 1, _ => new Color(0.26f, 0.31f, 0.2f));
            });
        }
    }
}
