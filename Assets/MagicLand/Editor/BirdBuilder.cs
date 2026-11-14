using System.Collections.Generic;
using UnityEngine;

namespace MagicLand.EditorTools
{
    /// <summary>Builds little birds (BirdRig), the bird tree with its perches, and the flock crossing the sky.</summary>
    public static class BirdBuilder
    {
        struct Palette
        {
            public string name;
            public Color back, breast, belly, head, wing, tail, cheek, beak;
        }

        static readonly Palette[] Palettes =
        {
            new Palette
            {
                name = "Robin", back = new Color(0.50f, 0.40f, 0.30f), breast = new Color(0.96f, 0.50f, 0.24f),
                belly = new Color(0.95f, 0.91f, 0.84f), head = new Color(0.50f, 0.40f, 0.30f), wing = new Color(0.42f, 0.33f, 0.25f),
                tail = new Color(0.40f, 0.31f, 0.24f), cheek = new Color(0.96f, 0.50f, 0.24f), beak = new Color(0.25f, 0.2f, 0.18f),
            },
            new Palette
            {
                name = "Blue Tit", back = new Color(0.55f, 0.68f, 0.40f), breast = new Color(1f, 0.86f, 0.30f),
                belly = new Color(0.98f, 0.88f, 0.42f), head = new Color(0.30f, 0.55f, 0.95f), wing = new Color(0.32f, 0.52f, 0.92f),
                tail = new Color(0.30f, 0.48f, 0.85f), cheek = new Color(0.98f, 0.98f, 0.96f), beak = new Color(0.2f, 0.2f, 0.22f),
            },
            new Palette
            {
                name = "Finch", back = new Color(1f, 0.82f, 0.22f), breast = new Color(1f, 0.88f, 0.28f),
                belly = new Color(1f, 0.93f, 0.55f), head = new Color(1f, 0.84f, 0.2f), wing = new Color(0.22f, 0.2f, 0.18f),
                tail = new Color(0.22f, 0.2f, 0.18f), cheek = new Color(1f, 0.84f, 0.2f), beak = new Color(1f, 0.62f, 0.35f),
            },
            new Palette
            {
                name = "Swallow", back = new Color(0.22f, 0.26f, 0.42f), breast = new Color(0.98f, 0.95f, 0.92f),
                belly = new Color(0.98f, 0.96f, 0.94f), head = new Color(0.22f, 0.26f, 0.42f), wing = new Color(0.2f, 0.24f, 0.38f),
                tail = new Color(0.2f, 0.24f, 0.38f), cheek = new Color(0.9f, 0.45f, 0.35f), beak = new Color(0.15f, 0.15f, 0.18f),
            },
        };

        static readonly Color Eye = new Color(0.03f, 0.03f, 0.04f);
        static readonly Color Leg = new Color(0.55f, 0.42f, 0.38f);
        static readonly Color Bark = new Color(0.36f, 0.25f, 0.18f);
        static readonly Color Leaf = new Color(0.30f, 0.55f, 0.25f);

        // Pivots (root at the feet, z forward).
        static readonly Vector3 BodyPivot = new Vector3(0f, 0.03f, 0f);
        static readonly Vector3 HeadPivot = new Vector3(0f, 0.045f, 0.03f);   // body-local
        static readonly Vector3 TailPivot = new Vector3(0f, 0.035f, -0.04f);  // body-local
        static readonly Vector3 WingPivot = new Vector3(0.03f, 0.05f, 0.01f); // body-local, right side

        static readonly Dictionary<string, Mesh[]> s_Meshes = new Dictionary<string, Mesh[]>();

        static float R(System.Random rnd, float min, float max) => min + (float)rnd.NextDouble() * (max - min);

        static Transform Part(string name, Transform parent, Vector3 localPos, Quaternion localRot, Mesh mesh, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            if (mesh != null)
            {
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
            return go.transform;
        }

        static Mesh[] Meshes(Palette pal)
        {
            if (s_Meshes.TryGetValue(pal.name, out var cached) && cached[0] != null)
                return cached;

            // Body (relative to the body pivot) with the little legs.
            var body = new MeshKit();
            Vector3 c = new Vector3(0f, 0.04f, 0f);
            body.AddEllipsoid(c, new Vector3(0.032f, 0.034f, 0.05f), Quaternion.Euler(-18f, 0f, 0f), 2, p =>
            {
                if (p.y > c.y + 0.012f)
                    return pal.back;
                return p.z > -0.005f ? Color.Lerp(pal.belly, pal.breast, Mathf.Clamp01((p.z + 0.005f) / 0.03f)) : pal.belly;
            });
            for (int s = -1; s <= 1; s += 2)
            {
                body.AddTube(new Vector3(s * 0.011f, 0.015f, 0f), new Vector3(s * 0.011f, -0.03f, 0.004f), 0.0035f, 0.003f, 4, _ => Leg, false, true);
                body.AddEllipsoid(new Vector3(s * 0.011f, -0.029f, 0.012f), new Vector3(0.005f, 0.003f, 0.012f), Quaternion.identity, 0, _ => Leg);
            }

            var head = new MeshKit();
            Vector3 hc = new Vector3(0f, 0.014f, 0.012f);
            head.AddEllipsoid(hc, new Vector3(0.026f, 0.025f, 0.027f), Quaternion.identity, 2, p =>
                p.y > hc.y + 0.006f ? pal.head : Mathf.Abs(p.x) > 0.012f && p.y > hc.y - 0.012f ? pal.cheek : pal.breast);
            head.AddTube(new Vector3(0f, 0.011f, 0.034f), new Vector3(0f, 0.007f, 0.056f), 0.0075f, 0f, 5, _ => pal.beak, true, false);
            for (int s = -1; s <= 1; s += 2)
            {
                head.AddEllipsoid(new Vector3(s * 0.019f, 0.02f, 0.026f), new Vector3(0.006f, 0.0065f, 0.005f), Quaternion.identity, 1, _ => Eye);
                head.AddEllipsoid(new Vector3(s * 0.0215f, 0.0225f, 0.029f), new Vector3(0.0017f, 0.0017f, 0.0012f), Quaternion.identity, 0, _ => Color.white);
            }

            var wing = new MeshKit();
            wing.AddEllipsoid(new Vector3(0.03f, 0f, -0.004f), new Vector3(0.038f, 0.004f, 0.02f), Quaternion.identity, 1, p =>
                p.x > 0.042f ? pal.wing * 0.8f : pal.wing);

            var tail = new MeshKit();
            tail.AddEllipsoid(new Vector3(0f, 0f, -0.032f), new Vector3(0.016f, 0.004f, 0.036f), Quaternion.identity, 1, _ => pal.tail);

            var meshes = new[]
            {
                MagicLandSetup.SaveMesh(body.ToMesh("Bird Body " + pal.name), "Bird Body " + pal.name),
                MagicLandSetup.SaveMesh(head.ToMesh("Bird Head " + pal.name), "Bird Head " + pal.name),
                MagicLandSetup.SaveMesh(wing.ToMesh("Bird Wing " + pal.name), "Bird Wing " + pal.name),
                MagicLandSetup.SaveMesh(tail.ToMesh("Bird Tail " + pal.name), "Bird Tail " + pal.name),
            };
            s_Meshes[pal.name] = meshes;
            return meshes;
        }

        static Quaternion Mirror(Quaternion q) => new Quaternion(q.x, -q.y, -q.z, q.w);

        static BirdRig CreateBird(Transform parent, string name, Palette pal, float scale, Material mat, bool withVoice)
        {
            var meshes = Meshes(pal);
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localScale = Vector3.one * scale;

            var rig = new BirdRig { root = root.transform };
            rig.body = Part("Body", root.transform, BodyPivot, Quaternion.identity, meshes[0], mat);
            rig.head = Part("Head", rig.body, HeadPivot, Quaternion.identity, meshes[1], mat);
            rig.tail = Part("Tail", rig.body, TailPivot, Quaternion.Euler(12f, 0f, 0f), meshes[3], mat);

            // Folded wing lies against the flank: span pointing back and slightly down, flat side facing out.
            var folded = Quaternion.LookRotation(new Vector3(0f, -0.97f, 0.24f), Vector3.right);
            rig.wingRight = Part("Wing R", rig.body, WingPivot, folded, meshes[2], mat);
            rig.wingLeft = Part("Wing L", rig.body, new Vector3(-WingPivot.x, WingPivot.y, WingPivot.z), Mirror(folded), meshes[2], mat);
            rig.wingLeft.localScale = new Vector3(-1f, 1f, 1f);

            if (withVoice)
            {
                var voice = root.AddComponent<AudioSource>();
                voice.playOnAwake = false;
                voice.loop = false;
                voice.spatialBlend = 1f;
                voice.rolloffMode = AudioRolloffMode.Linear;
                voice.minDistance = 2f;
                voice.maxDistance = 30f;
                voice.volume = 0.35f;
                rig.voice = voice;
            }
            return rig;
        }

        // ---------- Bird tree ----------

        public static TreeBirds CreateTreeBirds(Transform parent, Material mat, AudioClip[] songs)
        {
            s_Meshes.Clear();
            var rnd = new System.Random(77);
            Vector2 t = ForestGround.BirdTree;
            float g = ForestGround.HeightAt(t.x, t.y);
            Vector3 basePos = new Vector3(t.x, g, t.y);
            Vector3 toViewer = new Vector3(-t.x, 0f, -t.y).normalized;

            var kit = new MeshKit();
            const float height = 4.6f;
            float phase = 1.3f;
            kit.Wind = p => new Vector2(0.04f * Mathf.Clamp01((p.y - g) / height), phase);
            kit.AddTube(basePos - Vector3.up * 0.3f, basePos + Vector3.up * height, 0.2f, 0.09f, 7, _ => Bark, false, true);
            // Roots flaring at the bottom.
            kit.Wind = null;
            for (int i = 0; i < 4; i++)
            {
                Vector3 d = Quaternion.Euler(0f, i * 90f + 30f, 0f) * Vector3.forward;
                kit.AddTube(basePos + Vector3.up * 0.35f, basePos + d * 0.45f - Vector3.up * 0.05f, 0.09f, 0.03f, 5, _ => Bark * 0.9f, false, true);
            }

            // Crown.
            Vector3 crown = basePos + Vector3.up * 4.2f;
            for (int k = 0; k < 7; k++)
            {
                Vector3 offset = k == 0 ? Vector3.zero : new Vector3(R(rnd, -1.3f, 1.3f), R(rnd, -0.6f, 0.8f), R(rnd, -1.3f, 1.3f));
                float radius = k == 0 ? 1.5f : R(rnd, 0.8f, 1.1f);
                Vector3 center = crown + offset;
                Color col = Leaf * R(rnd, 0.85f, 1.12f);
                float clusterPhase = phase + k * 0.5f;
                kit.Wind = p => new Vector2(0.12f * (0.6f + 0.4f * Mathf.Clamp01((p.y - g) / height)), clusterPhase);
                kit.AddEllipsoid(center, new Vector3(radius, radius * 0.82f, radius), Quaternion.Euler(0f, R(rnd, 0f, 360f), 0f), 1,
                    p => Color.Lerp(col * 0.68f, col * 1.12f, Mathf.Clamp01((p.y - center.y) / radius * 0.5f + 0.5f)), 0.22f, rnd.Next());
            }

            // Bare perch branches reaching towards the viewer, with a leaf tuft at the tip.
            var perches = new List<Vector3>();
            var ground = new List<bool>();
            float[] heights = { 1.5f, 2.1f, 2.7f };
            float[] angles = { -38f, 30f, -5f };
            for (int b = 0; b < heights.Length; b++)
            {
                Vector3 dir = Quaternion.Euler(0f, angles[b], 0f) * toViewer;
                float length = R(rnd, 1.3f, 1.7f);
                Vector3 from = basePos + Vector3.up * heights[b];
                Vector3 to = from + dir * length + Vector3.up * 0.25f;
                const float r0 = 0.055f, r1 = 0.025f;
                kit.Wind = null;
                kit.AddTube(from - dir * 0.05f, to, r0, r1, 6, _ => Bark, false, true);
                float tuftPhase = phase + b;
                kit.Wind = p => new Vector2(0.05f, tuftPhase);
                Vector3 tip = to + dir * 0.15f + Vector3.up * 0.08f;
                kit.AddEllipsoid(tip, new Vector3(0.32f, 0.22f, 0.32f), Quaternion.identity, 1,
                    p => Color.Lerp(Leaf * 0.75f, Leaf * 1.15f, Mathf.Clamp01((p.y - tip.y) / 0.44f + 0.5f)), 0.2f, rnd.Next());
                foreach (float f in new[] { 0.42f, 0.72f })
                {
                    perches.Add(Vector3.Lerp(from, to, f) + Vector3.up * Mathf.Lerp(r0, r1, f) * 0.9f);
                    ground.Add(false);
                }
            }
            kit.Wind = null;

            // Pecking spots on the grass in front of the tree.
            Vector3 side = Vector3.Cross(Vector3.up, toViewer);
            foreach (float s in new[] { -0.6f, 0.2f, 0.9f })
            {
                Vector3 p = basePos + toViewer * 1.6f + side * s;
                p.y = ForestGround.HeightAt(p.x, p.z);
                perches.Add(p);
                ground.Add(true);
            }

            var root = new GameObject("Bird Tree");
            root.transform.SetParent(parent, false);
            var mesh = MagicLandSetup.SaveMesh(kit.ToMesh("Bird Tree"), "Bird Tree");
            var treeGo = new GameObject("Tree");
            treeGo.transform.SetParent(root.transform, false);
            treeGo.AddComponent<MeshFilter>().sharedMesh = mesh;
            var tr = treeGo.AddComponent<MeshRenderer>();
            tr.sharedMaterial = mat;
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tr.receiveShadows = false;

            var rigs = new BirdRig[4];
            for (int i = 0; i < rigs.Length; i++)
            {
                rigs[i] = CreateBird(root.transform, "Bird " + i, Palettes[i % 3], 2.4f, mat, true);
                rigs[i].root.localPosition = perches[i * 2];
            }

            var birds = root.AddComponent<TreeBirds>();
            birds.Configure(rigs, perches.ToArray(), ground.ToArray(), songs ?? new AudioClip[0]);
            return birds;
        }

        // ---------- Flock ----------

        public static BirdFlock CreateFlock(Transform parent, Material mat, float delay)
        {
            var root = new GameObject("Bird Flock");
            root.transform.SetParent(parent, false);
            var rigs = new BirdRig[7];
            for (int i = 0; i < rigs.Length; i++)
            {
                rigs[i] = CreateBird(root.transform, "Flock Bird " + i, Palettes[3], 6f, mat, false);
                rigs[i].root.localPosition = new Vector3(-60f, 12f, 28f);
            }
            var flock = root.AddComponent<BirdFlock>();
            flock.Configure(rigs, delay);
            return flock;
        }

        /// <summary>Forgets the cached bird meshes, so the next birds are saved again (in the current mesh folder).</summary>
        internal static void ResetCache() => s_Meshes.Clear();

        // ---------- Soaring birds ----------

        static readonly Palette[] SoarPalettes =
        {
            new Palette
            {
                name = "Ember Bird", back = new Color(0.95f, 0.42f, 0.25f), breast = new Color(1f, 0.78f, 0.35f),
                belly = new Color(1f, 0.9f, 0.6f), head = new Color(0.95f, 0.4f, 0.3f), wing = new Color(0.9f, 0.32f, 0.28f),
                tail = new Color(1f, 0.6f, 0.25f), cheek = new Color(1f, 0.8f, 0.4f), beak = new Color(0.35f, 0.22f, 0.2f),
            },
            new Palette
            {
                name = "Lilac Bird", back = new Color(0.62f, 0.5f, 0.9f), breast = new Color(0.95f, 0.8f, 0.95f),
                belly = new Color(0.98f, 0.92f, 0.98f), head = new Color(0.55f, 0.45f, 0.88f), wing = new Color(0.5f, 0.4f, 0.82f),
                tail = new Color(0.55f, 0.45f, 0.85f), cheek = new Color(1f, 0.7f, 0.85f), beak = new Color(1f, 0.75f, 0.45f),
            },
        };

        static readonly Palette[] CrystalTreePalettes =
        {
            new Palette
            {
                name = "Fire Finch", back = new Color(0.95f, 0.36f, 0.22f), breast = new Color(1f, 0.72f, 0.3f),
                belly = new Color(1f, 0.9f, 0.62f), head = new Color(0.98f, 0.3f, 0.25f), wing = new Color(0.55f, 0.22f, 0.3f),
                tail = new Color(0.6f, 0.22f, 0.3f), cheek = new Color(1f, 0.85f, 0.4f), beak = new Color(0.3f, 0.2f, 0.2f),
            },
            new Palette
            {
                name = "Turquoise Tanager", back = new Color(0.25f, 0.78f, 0.78f), breast = new Color(1f, 0.85f, 0.35f),
                belly = new Color(0.98f, 0.94f, 0.7f), head = new Color(0.2f, 0.62f, 0.85f), wing = new Color(0.18f, 0.4f, 0.62f),
                tail = new Color(0.18f, 0.42f, 0.65f), cheek = new Color(0.98f, 0.98f, 0.96f), beak = new Color(0.22f, 0.2f, 0.24f),
            },
            new Palette
            {
                name = "Rose Sparrow", back = new Color(0.82f, 0.48f, 0.62f), breast = new Color(1f, 0.7f, 0.8f),
                belly = new Color(1f, 0.92f, 0.95f), head = new Color(0.62f, 0.38f, 0.7f), wing = new Color(0.55f, 0.36f, 0.6f),
                tail = new Color(0.5f, 0.32f, 0.58f), cheek = new Color(1f, 0.55f, 0.7f), beak = new Color(1f, 0.72f, 0.4f),
            },
        };

        /// <summary>
        /// A dark twisted tree whose branches end in pastel crystals, in the volcano world, with little birds
        /// hopping between its branches and the warm ground.
        /// </summary>
        public static TreeBirds CreateCrystalTreeBirds(Transform parent, Material mat, AudioClip[] songs, Vector2 spot)
        {
            var rnd = new System.Random(91);
            float g = VolcanoGround.HeightAt(spot.x, spot.y);
            Vector3 basePos = new Vector3(spot.x, g, spot.y);
            Vector3 toViewer = new Vector3(-spot.x, 0f, -spot.y).normalized;
            Color bark = new Color(0.3f, 0.19f, 0.26f), barkLight = new Color(0.44f, 0.28f, 0.36f);
            Color Bark(Vector3 p) => Color.Lerp(bark, barkLight, MeshKit.Hash01(p * 3f, 5) * 0.6f);

            var kit = new MeshKit();
            // Slightly bent trunk made of three pieces, with flaring roots.
            Vector3 k1 = basePos + new Vector3(0.12f, 1.4f, 0.05f), k2 = basePos + new Vector3(-0.1f, 2.7f, 0f), top = basePos + new Vector3(0.05f, 3.6f, -0.05f);
            kit.AddTube(basePos - Vector3.up * 0.3f, k1, 0.2f, 0.15f, 7, Bark, false, false);
            kit.AddTube(k1, k2, 0.15f, 0.11f, 7, Bark, false, false);
            kit.AddTube(k2, top, 0.11f, 0.06f, 7, Bark, false, true);
            for (int i = 0; i < 4; i++)
            {
                Vector3 d = Quaternion.Euler(0f, i * 90f + 20f, 0f) * Vector3.forward;
                kit.AddTube(basePos + Vector3.up * 0.3f, basePos + d * 0.45f - Vector3.up * 0.05f, 0.09f, 0.03f, 5, Bark, false, true);
            }

            // Crystal crown on top and side branches reaching out, each ending in a crystal cluster.
            VolcanoBuilder.AddCrystalCluster(kit, top - Vector3.up * 0.05f, 0.75f, 1, rnd);
            for (int b = 0; b < 3; b++)
            {
                Vector3 dir = Quaternion.Euler(0f, 70f + b * 115f, 0f) * toViewer;
                Vector3 from = Vector3.Lerp(k1, k2, 0.3f + b * 0.3f);
                Vector3 to = from + dir * 0.8f + Vector3.up * 0.55f;
                kit.AddTube(from, to, 0.06f, 0.03f, 6, Bark, false, true);
                VolcanoBuilder.AddCrystalCluster(kit, to, 0.42f, b + 2, rnd);
            }

            // Bare perch branches reaching towards the viewer, with a small crystal at the tip.
            var perches = new List<Vector3>();
            var ground = new List<bool>();
            float[] heights = { 1.2f, 1.8f, 2.4f };
            float[] angles = { -40f, 32f, -4f };
            for (int b = 0; b < heights.Length; b++)
            {
                Vector3 dir = Quaternion.Euler(0f, angles[b], 0f) * toViewer;
                float length = R(rnd, 1.1f, 1.4f);
                Vector3 from = basePos + Vector3.up * heights[b];
                Vector3 to = from + dir * length + Vector3.up * 0.25f;
                const float r0 = 0.055f, r1 = 0.025f;
                kit.AddTube(from - dir * 0.05f, to, r0, r1, 6, Bark, false, true);
                VolcanoBuilder.AddCrystalCluster(kit, to + dir * 0.05f, 0.28f, b, rnd);
                foreach (float f in new[] { 0.42f, 0.72f })
                {
                    perches.Add(Vector3.Lerp(from, to, f) + Vector3.up * Mathf.Lerp(r0, r1, f) * 0.9f);
                    ground.Add(false);
                }
            }

            // Pecking spots on the warm ground in front of the tree.
            Vector3 side = Vector3.Cross(Vector3.up, toViewer);
            foreach (float s in new[] { -0.7f, 0.1f, 0.8f })
            {
                Vector3 p = basePos + toViewer * 1.4f + side * s;
                p.y = VolcanoGround.HeightAt(p.x, p.z);
                perches.Add(p);
                ground.Add(true);
            }

            var root = new GameObject("Crystal Tree");
            root.transform.SetParent(parent, false);
            var treeGo = new GameObject("Tree");
            treeGo.transform.SetParent(root.transform, false);
            treeGo.AddComponent<MeshFilter>().sharedMesh = MagicLandSetup.SaveMesh(kit.ToMesh("Crystal Tree"), "Crystal Tree");
            var tr = treeGo.AddComponent<MeshRenderer>();
            tr.sharedMaterial = mat;
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tr.receiveShadows = false;

            var rigs = new BirdRig[4];
            for (int i = 0; i < rigs.Length; i++)
            {
                rigs[i] = CreateBird(root.transform, "Crystal Bird " + i, CrystalTreePalettes[i % CrystalTreePalettes.Length], 2.4f, mat, true);
                rigs[i].root.localPosition = perches[i * 2];
            }

            var birds = root.AddComponent<TreeBirds>();
            birds.Configure(rigs, perches.ToArray(), ground.ToArray(), songs ?? new AudioClip[0], true);
            return birds;
        }

        /// <summary>Three colorful birds gliding in wide circles in front of the viewer.</summary>
        public static SoaringBirds CreateSoaringBirds(Transform parent, Material mat)
        {
            var root = new GameObject("Soaring Birds");
            root.transform.SetParent(parent, false);
            var rigs = new BirdRig[3];
            for (int i = 0; i < rigs.Length; i++)
                rigs[i] = CreateBird(root.transform, "Soaring Bird " + i, SoarPalettes[i % SoarPalettes.Length], 5f, mat, false);
            var birds = root.AddComponent<SoaringBirds>();
            birds.Configure(rigs,
                new[] { new Vector3(-9f, 8.5f, 24f), new Vector3(8f, 10.5f, 29f), new Vector3(1f, 13f, 38f) },
                new[] { 7f, 9f, 6f },
                new[] { 0.12f, -0.1f, 0.15f });
            // Put them on their circles right away (otherwise they sit on the viewer until the first frame).
            birds.Step(0f);
            return birds;
        }
    }
}
