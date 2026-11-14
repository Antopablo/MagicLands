using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MagicLand.EditorTools
{
    /// <summary>
    /// Everything that brings the volcano world to life: floating ash motes, lizards and iguanas, a crystal tree with
    /// birds, fire butterflies, steam vents, lava bubbles, a big dragon in the sky and the gentle eruption.
    /// </summary>
    public static class VolcanoLifeBuilder
    {
        // ---------- Layout (content space), shared with the scenery so nothing is placed on top of these ----------

        public static readonly Vector2[][] LizardRoutes =
        {
            new[] { new Vector2(-11f, 4.2f), new Vector2(-5f, 2.8f), new Vector2(0f, 2.4f), new Vector2(4f, 2.6f), new Vector2(11f, 3.6f) },
            new[] { new Vector2(16f, 15.5f), new Vector2(9.8f, 15.2f), new Vector2(3f, 15.6f), new Vector2(-1.5f, 15.2f), new Vector2(-6f, 15.4f), new Vector2(-16f, 16f) },
            new[] { new Vector2(-13f, 8.2f), new Vector2(-8f, 8f), new Vector2(-2.5f, 8.6f), new Vector2(0.8f, 9.8f), new Vector2(5f, 10f), new Vector2(8.8f, 8.8f), new Vector2(14f, 8.5f) },
        };

        public static readonly Vector2[] IguanaRocks = { new Vector2(4.6f, 4.6f), new Vector2(-6.8f, 5.6f) };
        public static readonly Vector2[] Vents = { new Vector2(-9.5f, 6.5f), new Vector2(9.5f, 6f), new Vector2(-3f, 20f) };
        public static readonly Vector2 CrystalTree = new Vector2(-8.5f, 12f);

        const float IguanaRockRadius = 1.1f;
        const float VentRadius = 0.6f;
        const float TreeRadius = 1.3f;
        const float RouteHalfWidth = 0.35f;

        static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
            return (p - (a + ab * t)).magnitude;
        }

        /// <summary>Distance to the nearest lizard route.</summary>
        public static float DistanceToRoutes(float x, float z)
        {
            var p = new Vector2(x, z);
            float best = float.MaxValue;
            foreach (var route in LizardRoutes)
                for (int i = 1; i < route.Length; i++)
                    best = Mathf.Min(best, DistanceToSegment(p, route[i - 1], route[i]));
            return best;
        }

        /// <summary>True near the spots used by the animals and features of this builder (not the lizard routes).</summary>
        public static bool NearFeature(float x, float z, float margin)
        {
            var p = new Vector2(x, z);
            foreach (var r in IguanaRocks)
                if ((p - r).magnitude < IguanaRockRadius + margin)
                    return true;
            foreach (var v in Vents)
                if ((p - v).magnitude < VentRadius + margin)
                    return true;
            // The tree and the pecking spots in front of it.
            Vector2 toViewer = -CrystalTree.normalized;
            return (p - CrystalTree).magnitude < TreeRadius + margin || (p - (CrystalTree + toViewer * 1.4f)).magnitude < 1.1f + margin;
        }

        /// <summary>True where nothing else should be placed: features and lizard routes.</summary>
        public static bool Occupies(float x, float z, float margin) =>
            NearFeature(x, z, margin) || DistanceToRoutes(x, z) < RouteHalfWidth + margin * 0.5f;

        // ---------- Build ----------

        public struct Context
        {
            public Material nature, far, lava, spark, smoke, ember;
            public AudioClip[] birdSongs;
            public DragonAgent babyDragon;
            public ParticleSystem volcanoSmoke;
            public Transform craterGlow;
            public Renderer[] volcanoLava;
            public Transform[] glows;
            public Vector3 pondSurface;
        }

        static float R(System.Random rnd, float min, float max) => min + (float)rnd.NextDouble() * (max - min);

        static Vector3 OnGround(Vector2 p) => new Vector3(p.x, VolcanoGround.HeightAt(p.x, p.y), p.y);

        public static void Build(Transform parent, Context c)
        {
            string audio = MagicLandSetup.AudioFolder;
            var ashMat = MagicLandSetup.GlowMaterial("AshMote", new Color(1f, 0.95f, 0.88f), 1f, false);
            var dropMat = MagicLandSetup.GlowMaterial("LavaDrop", new Color(1f, 0.75f, 0.5f), 1.6f, false);
            var steamMat = MagicLandSetup.GlowMaterial("SteamPuff", new Color(1f, 0.82f, 0.9f), 0.35f, false);
            var meteorMat = MagicLandSetup.GlowMaterial("Shooting Star", new Color(0.85f, 0.92f, 1f), 2.5f, false);

            CreateAshMotes(parent, ashMat);
            CreateLizards(parent, c.nature);
            BirdBuilder.CreateCrystalTreeBirds(parent, c.nature, c.birdSongs, CrystalTree);
            CreateButterflies(parent, c.nature);
            CreateVents(parent, c.nature, c.lava, steamMat);
            CreateLavaBubbles(parent, c.lava, c.ember, c.pondSurface, AudioSynth.Bloop(audio));
            DragonBuilder.CreateSkyDragon(parent, c.far);
            MagicLandSetup.CreateShootingStars(parent, meteorMat);
            CreateEruption(parent, c, dropMat, AudioSynth.Rumble(audio), AudioSynth.Whoosh(audio), AudioSynth.Chime(audio));

            var pulse = new GameObject("Glow Pulse");
            pulse.transform.SetParent(parent, false);
            pulse.AddComponent<GlowPulse>().Configure(c.glows, 0.12f, 0.8f);
        }

        // ---------- Ash motes ----------

        static void CreateAshMotes(Transform parent, Material mat)
        {
            var go = new GameObject("Ash Motes");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, -0.2f, 6.5f);
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 12f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.04f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.93f, 0.82f, 0.55f), new Color(1f, 0.82f, 0.75f, 0.55f));
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 360;

            var emission = ps.emission;
            emission.rateOverTime = 28f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(14f, 3f, 12f);

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(0.03f, 0.1f);
            velocity.y = new ParticleSystem.MinMaxCurve(-0.02f, 0.06f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.04f, 0.04f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.25f;
            noise.frequency = 0.3f;
            noise.scrollSpeed = 0.1f;

            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;

            SetupRenderer(go, mat);
        }

        static void SetupRenderer(GameObject go, Material mat)
        {
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        // ---------- Lizards and iguanas ----------

        enum Pattern { Spots, Stripe, Bands }

        struct Look
        {
            public string name;
            public Color main, accent, belly, throat;
            public Pattern pattern;
            public bool iguana;
        }

        static readonly Look[] Lizards =
        {
            new Look { name = "Lime Lizard", main = new Color(0.55f, 0.85f, 0.3f), accent = new Color(1f, 0.9f, 0.35f), belly = new Color(0.92f, 0.96f, 0.62f), throat = new Color(1f, 0.75f, 0.4f), pattern = Pattern.Spots },
            new Look { name = "Turquoise Lizard", main = new Color(0.3f, 0.78f, 0.82f), accent = new Color(1f, 0.62f, 0.3f), belly = new Color(0.86f, 0.97f, 0.95f), throat = new Color(1f, 0.62f, 0.52f), pattern = Pattern.Stripe },
            new Look { name = "Coral Lizard", main = new Color(1f, 0.55f, 0.48f), accent = new Color(1f, 0.86f, 0.55f), belly = new Color(1f, 0.9f, 0.8f), throat = new Color(1f, 0.95f, 0.6f), pattern = Pattern.Bands },
        };

        static readonly Look[] Iguanas =
        {
            new Look { name = "Green Iguana", main = new Color(0.42f, 0.74f, 0.44f), accent = new Color(0.76f, 0.92f, 0.52f), belly = new Color(0.86f, 0.93f, 0.66f), throat = new Color(1f, 0.74f, 0.45f), pattern = Pattern.Bands, iguana = true },
            new Look { name = "Sun Iguana", main = new Color(0.96f, 0.7f, 0.32f), accent = new Color(1f, 0.88f, 0.52f), belly = new Color(1f, 0.93f, 0.72f), throat = new Color(1f, 0.56f, 0.42f), pattern = Pattern.Bands, iguana = true },
        };

        static readonly Color EyeDark = new Color(0.06f, 0.06f, 0.12f);
        static readonly Color EyeIris = new Color(0.55f, 0.35f, 0.12f);
        static readonly Color Mouth = new Color(0.3f, 0.14f, 0.16f);
        static readonly Color Claw = new Color(1f, 0.93f, 0.82f);

        const float TailSegment = 0.085f;
        const int TailSegments = 4;

        static Transform Part(string name, Transform parent, Vector3 position, Mesh mesh, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            if (mesh != null)
            {
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = material;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
            return go.transform;
        }

        static Mesh MakeMesh(string name, System.Action<MeshKit> build)
        {
            var kit = new MeshKit();
            build(kit);
            return MagicLandSetup.SaveMesh(kit.ToMesh(name), name);
        }

        static Color Skin(Look look, Vector3 p, float topY)
        {
            if (p.y < topY - 0.012f)
                return look.belly;
            switch (look.pattern)
            {
                case Pattern.Spots:
                    return MeshKit.Hash01(p * 40f, 3) > 0.72f ? look.accent : look.main;
                case Pattern.Stripe:
                    return Mathf.Abs(p.x) < 0.014f && p.y > topY ? look.accent : look.main;
                default:
                    return Mathf.Sin(p.z * 55f) > 0.45f && p.y > topY ? look.accent : look.main;
            }
        }

        /// <summary>Builds one lizard (feet at the root, facing +Z) and returns its agent.</summary>
        static LizardAgent CreateLizard(Transform parent, Look look, Material mat, LizardAgent.Mode mode, Vector3[] route, float delay, float speed, int seed)
        {
            var root = new GameObject(look.name);
            root.transform.SetParent(parent, false);
            string n = look.name;

            Transform body = Part("Body", root.transform, new Vector3(0f, 0.065f, 0f), MakeMesh(n + " Body", kit =>
            {
                kit.AddEllipsoid(Vector3.zero, new Vector3(0.05f, 0.035f, 0.13f), Quaternion.identity, 3, p => Skin(look, p, 0f));
                if (look.iguana)
                    for (int i = 0; i < 7; i++)
                    {
                        float z = 0.1f - i * 0.035f, dz = z / 0.13f;
                        float y = 0.035f * Mathf.Sqrt(Mathf.Max(0f, 1f - dz * dz)) - 0.004f;
                        kit.AddTube(new Vector3(0f, y, z), new Vector3(0f, y + 0.022f - i * 0.001f, z - 0.012f), 0.008f, 0f, 4, _ => look.accent, true, false);
                    }
            }), mat);

            Transform head = Part("Head", body, new Vector3(0f, 0.012f, 0.115f), MakeMesh(n + " Head", kit =>
            {
                var center = new Vector3(0f, 0.01f, 0.05f);
                kit.AddEllipsoid(center, new Vector3(0.045f, 0.035f, 0.06f), Quaternion.identity, 3, p => p.y < -0.004f ? look.belly : look.main);
                var snout = new Vector3(0f, 0.0f, 0.095f);
                kit.AddEllipsoid(snout, new Vector3(0.032f, 0.022f, 0.04f), Quaternion.identity, 2, p => p.y < -0.008f ? look.belly : look.main);
                // Little nostrils and a happy smile along each side of the mouth.
                for (int side = -1; side <= 1; side += 2)
                {
                    kit.AddEllipsoid(new Vector3(side * 0.011f, 0.014f, 0.13f), new Vector3(0.004f, 0.003f, 0.003f), Quaternion.identity, 0, _ => Mouth);
                    for (int i = 0; i <= 6; i++)
                    {
                        float u = i / 6f;
                        float z = Mathf.Lerp(0.125f, 0.065f, u);
                        float x = side * Mathf.Lerp(0.017f, 0.04f, u);
                        float y = -0.008f + 0.008f * u * u;
                        kit.AddEllipsoid(new Vector3(x, y, z), new Vector3(0.0035f, 0.003f, 0.006f), Quaternion.identity, 0, _ => Mouth);
                    }
                }
                if (look.iguana)
                    for (int i = 0; i < 3; i++)
                        kit.AddTube(new Vector3(0f, 0.04f, 0.02f - i * 0.022f), new Vector3(0f, 0.056f, 0.012f - i * 0.022f), 0.006f, 0f, 4, _ => look.accent, true, false);
            }), mat);

            Transform eyes = Part("Eyes", head, new Vector3(0f, 0.03f, 0.065f), MakeMesh(n + " Eyes", kit =>
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    var eye = new Vector3(side * 0.034f, 0f, 0f);
                    kit.AddEllipsoid(eye, new Vector3(0.019f, 0.021f, 0.02f), Quaternion.identity, 2,
                        p => Color.Lerp(EyeIris, EyeDark, Mathf.Clamp01((p.y - eye.y + 0.012f) / 0.02f)));
                    kit.AddEllipsoid(eye + new Vector3(side * 0.011f, 0.009f, 0.011f), Vector3.one * 0.006f, Quaternion.identity, 1, _ => Color.white);
                    kit.AddEllipsoid(eye + new Vector3(side * 0.014f, -0.006f, 0.008f), Vector3.one * 0.003f, Quaternion.identity, 0, _ => Color.white);
                }
            }), mat);

            Transform throat = Part("Throat", head, new Vector3(0f, -0.022f, 0.055f), MakeMesh(n + " Throat", kit =>
            {
                if (look.iguana)
                    kit.AddEllipsoid(new Vector3(0f, -0.014f, 0f), new Vector3(0.008f, 0.024f, 0.032f), Quaternion.identity, 2, _ => look.throat);
                else
                    kit.AddEllipsoid(new Vector3(0f, -0.003f, 0f), new Vector3(0.014f, 0.012f, 0.026f), Quaternion.identity, 2, _ => look.throat);
            }), mat);

            var tail = new Transform[TailSegments];
            Transform link = body;
            Vector3 at = new Vector3(0f, -0.002f, -0.11f);
            for (int i = 0; i < TailSegments; i++)
            {
                float r0 = 0.032f - i * 0.0068f, r1 = Mathf.Max(0.005f, r0 - 0.0068f);
                int index = i;
                tail[i] = Part("Tail " + i, link, at, MakeMesh(n + " Tail " + i, kit =>
                {
                    var end = new Vector3(0f, -0.012f, -TailSegment);
                    kit.AddEllipsoid(Vector3.zero, Vector3.one * r0, Quaternion.identity, 1, p => Skin(look, p + Vector3.forward * index, 0.004f));
                    kit.AddTube(Vector3.zero, end, r0, r1, 8, p => Skin(look, p - Vector3.forward * index * TailSegment, r0 * 0.3f), false, index == TailSegments - 1);
                    if (look.iguana && index < 3)
                        kit.AddTube(new Vector3(0f, r0 * 0.8f, -TailSegment * 0.5f), new Vector3(0f, r0 * 0.8f + 0.016f, -TailSegment * 0.5f - 0.01f), 0.006f, 0f, 4,
                            _ => look.accent, true, false);
                }), mat);
                link = tail[i];
                at = new Vector3(0f, -0.012f, -TailSegment);
            }

            var legs = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                bool front = i < 2;
                legs[i] = Part("Leg " + i, body, new Vector3(side * 0.035f, -0.008f, front ? 0.075f : -0.07f), MakeMesh(n + " Leg " + i, kit =>
                {
                    Vector3 knee = new Vector3(side * 0.045f, -0.012f, 0f), foot = new Vector3(side * 0.06f, -0.05f, front ? 0.012f : -0.006f);
                    kit.AddTube(Vector3.zero, knee, 0.016f, 0.012f, 6, _ => look.main, true, false);
                    kit.AddEllipsoid(knee, Vector3.one * 0.012f, Quaternion.identity, 1, _ => look.main);
                    kit.AddTube(knee, foot, 0.012f, 0.009f, 6, _ => look.main, false, false);
                    kit.AddEllipsoid(foot + new Vector3(0f, -0.004f, 0.006f), new Vector3(0.016f, 0.006f, 0.018f), Quaternion.identity, 1, _ => look.main);
                    for (int k = -1; k <= 1; k++)
                        kit.AddEllipsoid(foot + new Vector3(k * 0.011f, -0.006f, 0.024f), Vector3.one * 0.0045f, Quaternion.identity, 0, _ => Claw);
                }), mat);
            }

            var agent = root.AddComponent<LizardAgent>();
            agent.Configure(mode, body, head, eyes, throat, tail, legs, route, delay, speed, seed);
            return agent;
        }

        static void CreateLizards(Transform parent, Material mat)
        {
            var root = new GameObject("Lizards");
            root.transform.SetParent(parent, false);

            float[] delays = { 8f, 30f, 55f };
            float[] speeds = { 1.5f, 1.3f, 1.7f };
            for (int i = 0; i < LizardRoutes.Length; i++)
            {
                var route = new Vector3[LizardRoutes[i].Length];
                for (int k = 0; k < route.Length; k++)
                    route[k] = OnGround(LizardRoutes[i][k]);
                var lizard = CreateLizard(root.transform, Lizards[i % Lizards.Length], mat, LizardAgent.Mode.Scurry, route, delays[i], speeds[i], 10 + i);
                lizard.transform.localPosition = route[0];
            }

            // Iguanas basking on flat warm rocks, seen from the side.
            var rocks = new MeshKit();
            var rnd = new System.Random(55);
            for (int i = 0; i < IguanaRocks.Length; i++)
            {
                Vector2 spot = IguanaRocks[i];
                Vector3 c = OnGround(spot);
                float yaw = Mathf.Atan2(-spot.x, -spot.y) * Mathf.Rad2Deg;
                var rockColor = new Color(0.4f, 0.24f, 0.28f);
                rocks.AddEllipsoid(c, new Vector3(0.95f, 0.32f, 0.75f), Quaternion.Euler(0f, yaw, 0f), 2,
                    p => Color.Lerp(rockColor * 0.75f, rockColor * 1.15f, Mathf.Clamp01((p.y - c.y) / 0.32f)) * (0.92f + 0.16f * MeshKit.Hash01(p, 7)), 0.05f, rnd.Next());
                for (int k = 0; k < 3; k++)
                {
                    float a = R(rnd, 0f, Mathf.PI * 2f);
                    Vector3 pebble = OnGround(spot + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * R(rnd, 0.95f, 1.1f));
                    float s = R(rnd, 0.12f, 0.2f);
                    rocks.AddEllipsoid(pebble, new Vector3(s, s * 0.6f, s), Quaternion.Euler(0f, R(rnd, 0f, 360f), 0f), 1, _ => rockColor * 0.85f, 0.2f, rnd.Next());
                }

                var iguana = CreateLizard(root.transform, Iguanas[i % Iguanas.Length], mat, LizardAgent.Mode.Bask, new Vector3[0], 0f, 1f, 30 + i);
                iguana.transform.localPosition = c + Vector3.up * 0.29f;
                iguana.transform.localRotation = Quaternion.Euler(0f, yaw + (spot.x > 0f ? 100f : -100f), 0f);
                iguana.transform.localScale = Vector3.one * (i == 0 ? 1.7f : 1.6f);
            }
            var rockGo = new GameObject("Iguana Rocks");
            rockGo.transform.SetParent(root.transform, false);
            rockGo.AddComponent<MeshFilter>().sharedMesh = MagicLandSetup.SaveMesh(rocks.ToMesh("Iguana Rocks"), "Iguana Rocks");
            var rr = rockGo.AddComponent<MeshRenderer>();
            rr.sharedMaterial = mat;
            rr.shadowCastingMode = ShadowCastingMode.Off;
            rr.receiveShadows = false;
        }

        // ---------- Butterflies ----------

        static void CreateButterflies(Transform parent, Material bodyMat)
        {
            var wingTex = ButterflyBuilder.CreateWingTexture(MagicLandSetup.GeneratedFolder);
            Material[] wingMats =
            {
                MagicLandSetup.ButterflyMaterial("FireButterflyOrange", wingTex, new Color(1f, 0.45f, 0.2f), new Color(1f, 0.92f, 0.55f)),
                MagicLandSetup.ButterflyMaterial("FireButterflyGold", wingTex, new Color(1f, 0.8f, 0.3f), new Color(1f, 0.5f, 0.3f)),
                MagicLandSetup.ButterflyMaterial("FireButterflyPink", wingTex, new Color(1f, 0.5f, 0.7f), new Color(1f, 0.9f, 0.6f)),
                MagicLandSetup.ButterflyMaterial("FireButterflyTeal", wingTex, new Color(0.4f, 0.95f, 0.9f), Color.white),
            };
            ButterflyBuilder.Create(parent, bodyMat, wingMats, VolcanoGround.HeightAt, 10);
        }

        // ---------- Steam vents ----------

        static void CreateVents(Transform parent, Material natureMat, Material lavaMat, Material steamMat)
        {
            var root = new GameObject("Steam Vents");
            root.transform.SetParent(parent, false);
            var rocks = new MeshKit();
            var glow = new MeshKit { Wind = p => new Vector2(1f, p.x + p.z) };
            var rnd = new System.Random(61);
            float[] intervals = { 7f, 9.5f, 12f };
            for (int v = 0; v < Vents.Length; v++)
            {
                Vector3 c = OnGround(Vents[v]);
                // A ring of dark stones around a small glowing hole.
                for (int k = 0; k < 7; k++)
                {
                    float a = k * Mathf.PI * 2f / 7f + R(rnd, -0.15f, 0.15f);
                    Vector3 p = c + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * 0.3f;
                    p.y = VolcanoGround.HeightAt(p.x, p.z);
                    float s = R(rnd, 0.1f, 0.16f);
                    rocks.AddEllipsoid(p, new Vector3(s, s * 0.8f, s), Quaternion.Euler(0f, R(rnd, 0f, 360f), 0f), 1, _ => new Color(0.26f, 0.15f, 0.21f), 0.2f, rnd.Next());
                }
                glow.AddEllipsoid(c + Vector3.up * 0.02f, new Vector3(0.2f, 0.03f, 0.2f), Quaternion.identity, 1, _ => new Color(1f, 0.7f, 0.4f));

                var go = new GameObject("Vent " + v);
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = c + Vector3.up * 0.1f;
                go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                var ps = go.AddComponent<ParticleSystem>();
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = ps.main;
                main.loop = true;
                main.duration = intervals[v];
                main.startLifetime = new ParticleSystem.MinMaxCurve(2.8f, 3.8f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.7f, 1.1f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.55f);
                main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.95f), new Color(0.9f, 0.8f, 1f));
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.maxParticles = 40;
                var emission = ps.emission;
                emission.rateOverTime = 0f;
                // A little puff of a few balls, then a rest: like a kettle sighing.
                emission.SetBursts(new[]
                {
                    new ParticleSystem.Burst(R(rnd, 0.5f, 3f), 4, 6, 3, 0.25f),
                });
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 10f;
                shape.radius = 0.08f;
                var size = ps.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 2.2f));
                var limit = ps.limitVelocityOverLifetime;
                limit.enabled = true;
                limit.limit = 0.5f;
                limit.dampen = 0.08f;
                var rotation = ps.rotationOverLifetime;
                rotation.enabled = true;
                rotation.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
                var fade = ps.colorOverLifetime;
                fade.enabled = true;
                var gradient = new Gradient();
                gradient.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.85f, 0.8f, 1f), 1f) },
                    new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0.6f, 0.6f), new GradientAlphaKey(0f, 1f) });
                fade.color = gradient;
                SetupRenderer(go, steamMat);
            }
            MeshObject(root.transform, rocks, "Vent Rocks", natureMat);
            MeshObject(root.transform, glow, "Vent Glow", lavaMat);
        }

        static GameObject MeshObject(Transform parent, MeshKit kit, string name, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = MagicLandSetup.SaveMesh(kit.ToMesh(name), name);
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go;
        }

        // ---------- Lava pond bubbles ----------

        static void CreateLavaBubbles(Transform parent, Material lavaMat, Material sparkMat, Vector3 pond, AudioClip bloop)
        {
            var root = new GameObject("Lava Bubbles");
            root.transform.SetParent(parent, false);
            var mesh = MakeMesh("Lava Bubble", kit =>
            {
                kit.Wind = p => new Vector2(1f, p.y * 3f);
                kit.AddEllipsoid(Vector3.zero, Vector3.one, Quaternion.identity, 2,
                    p => Color.Lerp(new Color(1f, 0.55f, 0.25f), new Color(1f, 0.92f, 0.6f), Mathf.Clamp01(p.y)));
            });
            var bubbles = new Transform[5];
            for (int i = 0; i < bubbles.Length; i++)
                bubbles[i] = Part("Bubble " + i, root.transform, pond, mesh, lavaMat);

            var sparksGo = new GameObject("Pop Sparks");
            sparksGo.transform.SetParent(root.transform, false);
            var ps = sparksGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.6f, 0.25f), new Color(1f, 0.9f, 0.5f));
            main.gravityModifier = 0.25f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 80;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.05f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            SetupRenderer(sparksGo, sparkMat);

            var soundGo = new GameObject("Bloop Sound");
            soundGo.transform.SetParent(root.transform, false);
            soundGo.transform.localPosition = pond;
            var sound = soundGo.AddComponent<AudioSource>();
            sound.clip = bloop;
            sound.playOnAwake = false;
            sound.spatialBlend = 1f;
            sound.rolloffMode = AudioRolloffMode.Linear;
            sound.minDistance = 3f;
            sound.maxDistance = 30f;
            sound.dopplerLevel = 0f;
            sound.volume = 0.3f;

            var agent = root.AddComponent<LavaBubbles>();
            agent.Configure(bubbles, ps, sound, pond, VolcanoGround.LavaPondRadius * 0.6f);
        }

        // ---------- Eruption ----------

        static void CreateEruption(Transform parent, Context c, Material dropMat, AudioClip rumbleClip, AudioClip whooshClip, AudioClip chimeClip)
        {
            var root = new GameObject("Volcano Eruption");
            root.transform.SetParent(parent, false);
            Vector3 crater = c.volcanoSmoke.transform.localPosition;

            // Fountain of big glowing drops falling back on the slopes.
            var fountainGo = new GameObject("Lava Fountain");
            fountainGo.transform.SetParent(root.transform, false);
            fountainGo.transform.localPosition = crater;
            fountainGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var fountain = fountainGo.AddComponent<ParticleSystem>();
            var main = fountain.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 3.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(9f, 14f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            main.gravityModifier = 1f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.5f, 0.3f), new Color(1f, 0.85f, 0.45f));
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 220;
            var emission = fountain.emission;
            emission.rateOverTime = 0f;
            var shape = fountain.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 20f;
            shape.radius = 1.5f;
            var size = fountain.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.4f));
            var fade = fountain.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.6f, 0.8f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.08f), new GradientAlphaKey(0.85f, 0.7f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            SetupRenderer(fountainGo, dropMat);

            // Bursts of colorful sparkles high above the crater, like a little firework.
            var sparkleGo = new GameObject("Eruption Sparkles");
            sparkleGo.transform.SetParent(root.transform, false);
            sparkleGo.transform.localPosition = crater + Vector3.up * 9f;
            var sparkles = sparkleGo.AddComponent<ParticleSystem>();
            var sMain = sparkles.main;
            sMain.loop = true;
            sMain.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.4f);
            sMain.startSpeed = new ParticleSystem.MinMaxCurve(4f, 7f);
            sMain.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
            sMain.gravityModifier = 0.15f;
            var colors = new Gradient();
            colors.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.55f, 0.8f), 0f), new GradientColorKey(new Color(1f, 0.88f, 0.4f), 0.33f),
                    new GradientColorKey(new Color(0.45f, 0.95f, 0.9f), 0.66f), new GradientColorKey(new Color(0.75f, 0.6f, 1f), 1f),
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            sMain.startColor = new ParticleSystem.MinMaxGradient(colors) { mode = ParticleSystemGradientMode.RandomColor };
            sMain.simulationSpace = ParticleSystemSimulationSpace.Local;
            sMain.maxParticles = 450;
            var sEmission = sparkles.emission;
            sEmission.rateOverTime = 0f;
            var sShape = sparkles.shape;
            sShape.shapeType = ParticleSystemShapeType.Sphere;
            sShape.radius = 0.6f;
            var sSize = sparkles.sizeOverLifetime;
            sSize.enabled = true;
            sSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
            var sFade = sparkles.colorOverLifetime;
            sFade.enabled = true;
            var sGradient = new Gradient();
            sGradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            sFade.color = sGradient;
            SetupRenderer(sparkleGo, c.spark);

            AudioSource Source(string name, AudioClip clip, bool loop, float volume)
            {
                var go = new GameObject(name);
                go.transform.SetParent(root.transform, false);
                var s = go.AddComponent<AudioSource>();
                s.clip = clip;
                s.loop = loop;
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                s.volume = volume;
                return s;
            }

            var eruption = root.AddComponent<VolcanoEruption>();
            eruption.Configure(c.volcanoSmoke, fountain, sparkles, c.craterGlow, c.volcanoLava,
                Source("Rumble Sound", rumbleClip, true, 0.3f), Source("Whoosh Sound", whooshClip, false, 0.35f),
                Source("Chime Sound", chimeClip, false, 0.35f), c.babyDragon, 75f);
        }
    }
}
