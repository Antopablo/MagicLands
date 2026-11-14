using System.Collections.Generic;
using UnityEngine;

namespace MagicLand.EditorTools
{
    /// <summary>Generates the ground, the trees and the small details (grass, flowers, mushrooms, rocks, bushes).</summary>
    public static class ForestBuilder
    {
        const int Sectors = 8;

        static readonly Color Bark = new Color(0.38f, 0.26f, 0.18f);
        static readonly Color BarkDark = new Color(0.27f, 0.19f, 0.14f);
        static readonly Color[] Leaves =
        {
            new Color(0.24f, 0.47f, 0.22f), new Color(0.31f, 0.56f, 0.25f),
            new Color(0.38f, 0.60f, 0.28f), new Color(0.22f, 0.44f, 0.30f),
        };
        static readonly Color[] Pines = { new Color(0.13f, 0.33f, 0.24f), new Color(0.17f, 0.38f, 0.27f) };
        static readonly Color[] Blossoms =
        {
            new Color(0.97f, 0.68f, 0.80f), new Color(0.95f, 0.60f, 0.75f), new Color(1f, 0.80f, 0.88f),
        };

        public struct Result
        {
            public List<Vector3> blueMushrooms;
        }

        static float R(System.Random rnd, float min, float max) => min + (float)rnd.NextDouble() * (max - min);

        static Color Shade(Color c, System.Random rnd, float amount) =>
            new Color(c.r * (1f + R(rnd, -amount, amount)), c.g * (1f + R(rnd, -amount, amount)), c.b * (1f + R(rnd, -amount, amount)));

        static Vector3 OnGround(float x, float z) => new Vector3(x, ForestGround.HeightAt(x, z), z);

        public static Result Build(Transform parent, Material opaque, Material twoSided)
        {
            var rnd = new System.Random(42);
            var root = new GameObject("Forest");
            root.transform.SetParent(parent, false);

            BuildGround(root.transform, opaque);
            BuildTrees(root.transform, opaque, rnd);
            return BuildDetails(root.transform, opaque, twoSided, rnd);
        }

        static void CreateMeshObject(Transform parent, MeshKit kit, string name, Material mat)
        {
            if (kit.VertexCount == 0)
                return;
            var mesh = MagicLandSetup.SaveMesh(kit.ToMesh(name), name);
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        // ---------- Ground ----------

        static void BuildGround(Transform parent, Material mat)
        {
            var kit = new MeshKit();
            const int rings = 30, segments = 56;
            const float maxRadius = 70f;

            Vector3 P(int ring, int seg)
            {
                float r = maxRadius * Mathf.Pow(ring / (float)rings, 1.7f);
                float a = seg * Mathf.PI * 2f / segments + (ring % 2) * Mathf.PI / segments;
                return OnGround(Mathf.Sin(a) * r, Mathf.Cos(a) * r);
            }

            Color GroundColor(Vector3 p)
            {
                float n = Mathf.PerlinNoise(p.x * 0.12f + 5f, p.z * 0.12f + 5f);
                var c = Color.Lerp(new Color(0.30f, 0.50f, 0.22f), new Color(0.42f, 0.60f, 0.27f), n);
                float r = new Vector2(p.x, p.z).magnitude;
                return Color.Lerp(c, new Color(0.22f, 0.36f, 0.20f), Mathf.InverseLerp(14f, 30f, r));
            }

            void Face(Vector3 a, Vector3 b, Vector3 c)
            {
                Vector3 centroid = (a + b + c) / 3f;
                kit.AddFace(a, b, c, centroid - Vector3.up, GroundColor(centroid));
            }

            for (int s = 0; s < segments; s++)
                Face(P(0, 0), P(1, s), P(1, s + 1));
            for (int ring = 1; ring < rings; ring++)
            {
                for (int s = 0; s < segments; s++)
                {
                    Vector3 a = P(ring, s), b = P(ring, s + 1), c = P(ring + 1, s), d = P(ring + 1, s + 1);
                    Face(a, b, c);
                    Face(b, d, c);
                }
            }

            CreateMeshObject(parent, kit, "Ground", mat);
        }

        // ---------- Trees ----------

        static void BuildTrees(Transform parent, Material mat, System.Random rnd)
        {
            var kits = new MeshKit[Sectors];
            for (int i = 0; i < Sectors; i++)
                kits[i] = new MeshKit();
            var placed = new List<Vector2>();

            void Place(float x, float z, int type, float height)
            {
                float angle = Mathf.Atan2(x, z) + Mathf.PI;
                int sector = Mathf.Clamp((int)(angle / (Mathf.PI * 2f) * Sectors), 0, Sectors - 1);
                bool detailed = new Vector2(x, z).magnitude < 28f;
                AddTree(kits[sector], OnGround(x, z), type, height, rnd, detailed);
                placed.Add(new Vector2(x, z));
            }

            // Framing trees close to the viewer, their crowns arching over the top of the view.
            Place(-3.4f, 1.4f, 0, 7.5f);
            Place(3.9f, 0.4f, 2, 6.5f);
            Place(-2.8f, -3.6f, 1, 8.5f);
            Place(3.2f, -4.2f, 0, 7f);

            int attempts = 0;
            while (placed.Count < 150 && attempts++ < 8000)
            {
                float r = Mathf.Sqrt(R(rnd, 25f, 48f * 48f));
                float a = R(rnd, 0f, Mathf.PI * 2f);
                float x = Mathf.Sin(a) * r, z = Mathf.Cos(a) * r;
                if (ForestGround.InClearing(x, z) || ForestGround.NearWaterfall(x, z, 5.5f) || ForestGround.NearPond(x, z, 4f)
                    || ForestGround.BlocksWaterfallView(x, z, 2.2f) || ForestGround.NearBirdTree(x, z, 3.2f))
                    continue;

                float spacing = r < 15f ? 2.6f : 3.3f;
                bool free = true;
                foreach (var p in placed)
                {
                    if ((p - new Vector2(x, z)).sqrMagnitude < spacing * spacing)
                    {
                        free = false;
                        break;
                    }
                }
                if (!free)
                    continue;

                double pick = rnd.NextDouble();
                int type = pick < 0.55 ? 0 : pick < 0.85 ? 1 : 2;
                float height = type == 1 ? R(rnd, 7f, 11f) : type == 2 ? R(rnd, 4.5f, 6.5f) : R(rnd, 5f, 8f);
                if (r > 25f)
                    height *= 1.2f;
                Place(x, z, type, height);
            }

            for (int i = 0; i < Sectors; i++)
                CreateMeshObject(parent, kits[i], "Trees " + i, mat);
        }

        /// <summary>Tree types: 0 leafy, 1 pine, 2 pink blossom.</summary>
        internal static void AddTree(MeshKit kit, Vector3 basePos, int type, float height, System.Random rnd, bool detailed)
        {
            float phase = R(rnd, 0f, 6.28f);
            var lean = new Vector3(R(rnd, -0.07f, 0.07f), 1f, R(rnd, -0.07f, 0.07f)).normalized;
            float trunkRadius = height * (type == 1 ? 0.035f : 0.045f);
            float scale = height / 5f;
            int subdiv = detailed ? 1 : 0;
            Color bark = Color.Lerp(Bark, BarkDark, R(rnd, 0f, 1f));

            MeshKit.WindFn trunkWind = p =>
            {
                float h = Mathf.Clamp01((p.y - basePos.y) / height);
                return new Vector2(0.06f * h * h * scale, phase);
            };

            if (type == 1)
            {
                kit.Wind = trunkWind;
                kit.AddTube(basePos - Vector3.up * 0.3f, basePos + lean * height * 0.92f, trunkRadius, trunkRadius * 0.3f, 6, _ => bark, false, false);

                Color baseColor = Pines[rnd.Next(Pines.Length)];
                const int tiers = 4;
                for (int k = 0; k < tiers; k++)
                {
                    float y0 = height * (0.22f + k * 0.17f);
                    float coneHeight = height * (0.38f - k * 0.04f);
                    float radius = height * (0.3f - k * 0.055f);
                    Vector3 b = basePos + lean * y0, t = basePos + lean * (y0 + coneHeight);
                    Color c = Shade(baseColor, rnd, 0.08f);
                    float tierPhase = phase + k * 0.3f;
                    kit.Wind = p =>
                    {
                        float h = Mathf.Clamp01((p.y - basePos.y) / height);
                        return new Vector2(0.11f * h * scale, tierPhase);
                    };
                    kit.AddTube(b, t, radius, 0f, 7, p => Color.Lerp(c * 0.7f, c * 1.15f, Mathf.Clamp01((p.y - b.y) / coneHeight)),
                        true, false, R(rnd, 0f, 1f));
                }
                return;
            }

            Vector3 crown = basePos + lean * height * 0.72f;
            float crownRadius = height * 0.28f;
            kit.Wind = trunkWind;
            kit.AddTube(basePos - Vector3.up * 0.3f, crown, trunkRadius, trunkRadius * 0.5f, 6, _ => bark, false, true);

            Color[] palette = type == 2 ? Blossoms : Leaves;
            Color leafColor = palette[rnd.Next(palette.Length)];
            int clusters = detailed ? rnd.Next(5, 8) : 4;
            for (int k = 0; k < clusters; k++)
            {
                Vector3 offset = k == 0
                    ? Vector3.zero
                    : new Vector3(R(rnd, -1f, 1f) * crownRadius * 0.9f, R(rnd, -0.35f, 0.55f) * crownRadius, R(rnd, -1f, 1f) * crownRadius * 0.9f);
                float radius = k == 0 ? crownRadius : crownRadius * R(rnd, 0.5f, 0.75f);
                Vector3 center = crown + offset + Vector3.up * crownRadius * 0.3f;

                if (k > 0 && detailed)
                {
                    kit.Wind = trunkWind;
                    kit.AddTube(basePos + lean * height * R(rnd, 0.45f, 0.6f), center, trunkRadius * 0.4f, trunkRadius * 0.2f, 5, _ => bark, false, false);
                }

                Color col = Shade(leafColor, rnd, 0.1f);
                float clusterPhase = phase + k * 0.5f;
                kit.Wind = p =>
                {
                    float h = Mathf.Clamp01((p.y - basePos.y) / height);
                    return new Vector2(0.12f * scale * (0.6f + 0.4f * h), clusterPhase);
                };
                float squash = R(rnd, 0.75f, 0.9f);
                kit.AddEllipsoid(center, new Vector3(radius, radius * squash, radius), Quaternion.Euler(0f, R(rnd, 0f, 360f), 0f), subdiv,
                    p => Color.Lerp(col * 0.68f, col * 1.12f, Mathf.Clamp01((p.y - center.y) / radius * 0.5f + 0.5f)),
                    0.22f, rnd.Next());
            }
            kit.Wind = null;
        }

        // ---------- Details ----------

        static Result BuildDetails(Transform parent, Material opaque, Material twoSided, System.Random rnd)
        {
            var grass = new MeshKit();
            var props = new MeshKit();
            var result = new Result { blueMushrooms = new List<Vector3>() };

            // Grass tufts, denser around the viewer and in the clearing.
            for (int i = 0; i < 1400; i++)
            {
                float r = i < 300 ? R(rnd, 0.6f, 5f) : Mathf.Sqrt(R(rnd, 0.6f * 0.6f, 20f * 20f));
                float a = R(rnd, 0f, Mathf.PI * 2f);
                float x = Mathf.Sin(a) * r, z = Mathf.Cos(a) * r;
                if (ForestGround.NearWaterfall(x, z, 2.5f) || ForestGround.NearPond(x, z, 2.8f))
                    continue;
                float y = ForestGround.HeightAt(x, z);
                float phase = R(rnd, 0f, 6.28f);
                grass.Wind = p => new Vector2(Mathf.Clamp01((p.y - y) / 0.4f) * 0.05f, phase);

                int blades = rnd.Next(3, 6);
                for (int b = 0; b < blades; b++)
                {
                    float angle = R(rnd, 0f, Mathf.PI);
                    float h = R(rnd, 0.15f, 0.35f) * (ForestGround.InMouseArea(x, z, 0.3f) ? 0.45f : 1f);
                    var root = new Vector3(x + R(rnd, -0.08f, 0.08f), y - 0.02f, z + R(rnd, -0.08f, 0.08f));
                    var side = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.04f;
                    var tip = root + new Vector3(R(rnd, -0.08f, 0.08f), h, R(rnd, -0.08f, 0.08f));
                    grass.AddBlade(root, side, tip, new Color(0.2f, 0.38f, 0.17f),
                        Color.Lerp(new Color(0.45f, 0.68f, 0.3f), new Color(0.62f, 0.74f, 0.35f), R(rnd, 0f, 1f)));
                }
            }

            // Flowers in the clearing.
            Color[] petals =
            {
                new Color(1f, 0.55f, 0.75f), new Color(1f, 0.88f, 0.35f), new Color(0.97f, 0.97f, 1f),
                new Color(0.75f, 0.6f, 1f), new Color(0.5f, 0.75f, 1f),
            };
            for (int i = 0, count = 0; count < 220 && i < 2000; i++)
            {
                float x = R(rnd, -9f, 9f), z = R(rnd, 1.2f, 19f);
                if ((!ForestGround.InClearing(x, z) && rnd.NextDouble() < 0.8) || ForestGround.NearWaterfall(x, z, 2.6f)
                    || ForestGround.NearPond(x, z, 2.8f))
                    continue;
                count++;
                float y = ForestGround.HeightAt(x, z);
                float h = R(rnd, 0.12f, 0.3f);
                float phase = R(rnd, 0f, 6.28f);
                MeshKit.WindFn wind = p => new Vector2(Mathf.Clamp01((p.y - y) / h) * 0.04f, phase);
                var root = new Vector3(x, y - 0.02f, z);
                var top = root + new Vector3(R(rnd, -0.03f, 0.03f), h, R(rnd, -0.03f, 0.03f));
                grass.Wind = wind;
                grass.AddBlade(root, new Vector3(0.015f, 0f, 0f), top, new Color(0.2f, 0.4f, 0.18f), new Color(0.3f, 0.55f, 0.22f));
                props.Wind = wind;
                Color petal = petals[rnd.Next(petals.Length)];
                float size = R(rnd, 0.8f, 1.3f);
                props.AddEllipsoid(top, new Vector3(0.035f, 0.018f, 0.035f) * size, Quaternion.Euler(R(rnd, -20f, 20f), R(rnd, 0f, 360f), 0f), 0,
                    p => p.y > top.y ? petal : petal * 0.8f, 0.15f, i);
            }
            props.Wind = null;

            // Mushrooms around the clearing border, some of them glowing blue.
            for (int i = 0; i < 16; i++)
            {
                float z = R(rnd, 2.5f, 16f);
                float x = (rnd.NextDouble() < 0.5 ? -1f : 1f) * (2.5f + z * 0.45f + R(rnd, -0.8f, 0.6f));
                if (ForestGround.NearWaterfall(x, z, 3.2f) || ForestGround.NearPond(x, z, 2.8f)
                    || ForestGround.NearBirdTree(x, z, 1.6f))
                    continue;
                Vector3 baseP = OnGround(x, z);
                bool blue = i % 3 == 0;
                int count = rnd.Next(1, 4);
                for (int k = 0; k < count; k++)
                {
                    Vector3 p0 = baseP + new Vector3(R(rnd, -0.2f, 0.2f), 0f, R(rnd, -0.2f, 0.2f));
                    float s = R(rnd, 0.7f, 1.4f);
                    Vector3 capCenter = p0 + Vector3.up * 0.13f * s;
                    props.AddTube(p0 - Vector3.up * 0.03f, capCenter, 0.025f * s, 0.02f * s, 6, _ => new Color(0.95f, 0.9f, 0.8f), false, false);
                    Color cap = blue ? new Color(0.35f, 0.6f, 1f) : new Color(0.85f, 0.2f, 0.17f);
                    int seed = rnd.Next();
                    props.AddEllipsoid(capCenter, new Vector3(0.075f, 0.045f, 0.075f) * s, Quaternion.identity, 1,
                        p => p.y < capCenter.y - 0.01f * s ? new Color(0.9f, 0.85f, 0.75f)
                            : MeshKit.Hash01(p, seed) < 0.2f ? Color.white : cap, 0.1f, seed);
                    if (blue && k == 0)
                        result.blueMushrooms.Add(capCenter);
                }
            }

            // Rocks.
            for (int i = 0; i < 12; i++)
            {
                float z = R(rnd, 3f, 18f);
                float x = (rnd.NextDouble() < 0.5 ? -1f : 1f) * (1.5f + z * 0.45f + R(rnd, -1f, 1.5f));
                if (ForestGround.NearWaterfall(x, z, 3.2f) || ForestGround.NearPond(x, z, 2.8f)
                    || ForestGround.NearBirdTree(x, z, 1.6f))
                    continue;
                Vector3 p0 = OnGround(x, z);
                float s = R(rnd, 0.2f, 0.6f);
                props.AddEllipsoid(p0, new Vector3(s, s * 0.6f, s * R(rnd, 0.7f, 1.2f)), Quaternion.Euler(0f, R(rnd, 0f, 360f), 0f), 1,
                    p => p.y > p0.y + s * 0.3f ? new Color(0.35f, 0.5f, 0.28f) : new Color(0.52f, 0.52f, 0.55f), 0.3f, i);
            }

            // Bushes along the clearing border.
            for (int i = 0; i < 36; i++)
            {
                float z = R(rnd, 2f, 21f);
                float x = (rnd.NextDouble() < 0.5 ? -1f : 1f) * (2.5f + z * 0.45f + R(rnd, 0f, 1.8f));
                if (ForestGround.NearWaterfall(x, z, 3.2f) || ForestGround.NearPond(x, z, 2.8f)
                    || ForestGround.NearBirdTree(x, z, 2.8f) || ForestGround.BlocksPondView(x, z, 1.4f))
                    continue;
                Vector3 p0 = OnGround(x, z);
                float phase = R(rnd, 0f, 6.28f);
                props.Wind = p => new Vector2(Mathf.Clamp01((p.y - p0.y) / 1.2f) * 0.06f, phase);
                Color baseColor = Leaves[rnd.Next(Leaves.Length)] * 0.9f;
                int lumps = rnd.Next(2, 4);
                for (int k = 0; k < lumps; k++)
                {
                    float s = R(rnd, 0.4f, 0.8f);
                    Vector3 c = p0 + new Vector3(R(rnd, -0.5f, 0.5f), s * 0.5f, R(rnd, -0.5f, 0.5f));
                    Color col = Shade(baseColor, rnd, 0.1f);
                    props.AddEllipsoid(c, new Vector3(s, s * 0.8f, s), Quaternion.Euler(0f, R(rnd, 0f, 360f), 0f), 1,
                        p => Color.Lerp(col * 0.7f, col * 1.1f, Mathf.Clamp01((p.y - c.y) / s * 0.5f + 0.5f)), 0.2f, rnd.Next());
                }
            }
            props.Wind = null;

            CreateMeshObject(parent, grass, "Grass and Stems", twoSided);
            CreateMeshObject(parent, props, "Flowers Mushrooms Rocks Bushes", opaque);
            return result;
        }
    }
}
