using UnityEngine;
using UnityEngine.Rendering;

namespace MagicLand.EditorTools
{
    /// <summary>Builds a chubby, friendly baby dragon and the rocks it hides behind in the volcano world.</summary>
    public static class DragonBuilder
    {
        static readonly Color Scales = new Color(0.45f, 0.8f, 0.7f);
        static readonly Color ScalesLight = new Color(0.62f, 0.9f, 0.8f);
        static readonly Color Belly = new Color(1f, 0.92f, 0.74f);
        static readonly Color Spike = new Color(1f, 0.68f, 0.45f);
        static readonly Color Horn = new Color(1f, 0.93f, 0.8f);
        static readonly Color Membrane = new Color(0.82f, 0.66f, 1f);
        static readonly Color WingBone = new Color(0.58f, 0.44f, 0.86f);
        static readonly Color Cheek = new Color(1f, 0.6f, 0.68f);
        static readonly Color EyeDark = new Color(0.07f, 0.07f, 0.16f);
        static readonly Color EyeIris = new Color(0.2f, 0.36f, 0.5f);
        static readonly Color Mouth = new Color(0.32f, 0.12f, 0.16f);
        static readonly Color Rock = new Color(0.3f, 0.18f, 0.25f);
        static readonly Color RockLight = new Color(0.45f, 0.27f, 0.33f);

        static readonly Vector3 HeadPivot = new Vector3(0f, 0.66f, 0.03f);
        static readonly Vector3 EyePivot = new Vector3(0f, 0.25f, 0.18f);
        static readonly Vector3 WingPivot = new Vector3(0.13f, 0.56f, -0.13f);
        static readonly Vector3 TailPivot = new Vector3(0f, 0.2f, -0.17f);
        static readonly Vector3 MouthPoint = new Vector3(0f, 0.09f, 0.3f);

        // Head: a big round ball with a soft muzzle in front.
        static readonly Vector3 HeadCenter = new Vector3(0f, 0.2f, 0f);
        static readonly Vector3 HeadRadii = new Vector3(0.25f, 0.22f, 0.23f);
        static readonly Vector3 MuzzleCenter = new Vector3(0f, 0.1f, 0.16f);
        static readonly Vector3 MuzzleRadii = new Vector3(0.15f, 0.1f, 0.12f);

        static Transform Part(string name, Transform parent, Vector3 position, Mesh mesh, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            if (mesh != null)
            {
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            return go.transform;
        }

        static Mesh MakeMesh(string name, System.Action<MeshKit> build)
        {
            var kit = new MeshKit();
            build(kit);
            return MagicLandSetup.SaveMesh(kit.ToMesh(name), name);
        }

        static Vector3 Ground(Vector3 p) => new Vector3(p.x, VolcanoGround.HeightAt(p.x, p.z), p.z);

        /// <summary>Front surface depth of an ellipsoid at (x, y), or its center depth when outside.</summary>
        static float Front(Vector3 center, Vector3 radii, float x, float y)
        {
            float dx = (x - center.x) / radii.x, dy = (y - center.y) / radii.y;
            return center.z + radii.z * Mathf.Sqrt(Mathf.Max(0f, 1f - dx * dx - dy * dy));
        }

        static float FaceFront(float x, float y) =>
            Mathf.Max(Front(HeadCenter, HeadRadii, x, y), Front(MuzzleCenter, MuzzleRadii, x, y));

        /// <summary>Adds a triangle visible from both sides.</summary>
        static void DoubleFace(MeshKit kit, Vector3 a, Vector3 b, Vector3 c, Color color)
        {
            Vector3 n = Vector3.Cross(b - a, c - a).normalized * 0.05f;
            Vector3 centroid = (a + b + c) / 3f;
            kit.AddFace(a, b, c, centroid - n, color);
            kit.AddFace(a, b, c, centroid + n, color);
        }

        public static DragonAgent Create(Transform parent, Material material, Material sparkle, AudioClip chirp)
        {
            var hideouts = CreateHideouts(parent, material);

            var root = new GameObject("Baby Dragon");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Ground(hideouts[0].hidden);

            Part("Body", root.transform, Vector3.zero, MakeMesh("Dragon Body", BuildBody), material);
            Transform tail = Part("Tail", root.transform, TailPivot, MakeMesh("Dragon Tail", BuildTail), material);
            Transform head = Part("Head", root.transform, HeadPivot, MakeMesh("Dragon Head", BuildHead), material);
            Transform eyes = Part("Eyes", head, EyePivot, MakeMesh("Dragon Eyes", BuildEyes), material);
            Transform rightWing = Part("Right Wing", root.transform, WingPivot,
                MakeMesh("Dragon Right Wing", kit => BuildWing(kit, 1f)), material);
            Transform leftWing = Part("Left Wing", root.transform, Vector3.Scale(WingPivot, new Vector3(-1f, 1f, 1f)),
                MakeMesh("Dragon Left Wing", kit => BuildWing(kit, -1f)), material);
            rightWing.localRotation = Quaternion.Euler(0f, 58f, -22f);
            leftWing.localRotation = Quaternion.Euler(0f, -58f, 22f);

            var puff = CreatePuff(head, sparkle);
            var voice = root.AddComponent<AudioSource>();
            voice.clip = chirp;
            voice.playOnAwake = false;
            voice.loop = false;
            voice.spatialBlend = 1f;
            voice.rolloffMode = AudioRolloffMode.Linear;
            voice.minDistance = 3f;
            voice.maxDistance = 30f;
            voice.dopplerLevel = 0f;
            voice.volume = 0.55f;

            var agent = root.AddComponent<DragonAgent>();
            agent.Configure(head, eyes, leftWing, rightWing, tail, puff, voice, hideouts, 20f);
            return agent;
        }

        static void BuildBody(MeshKit kit)
        {
            // Pear-shaped body with a cream belly patch.
            var center = new Vector3(0f, 0.36f, 0f);
            kit.AddEllipsoid(center, new Vector3(0.25f, 0.3f, 0.23f), Quaternion.identity, 3, p =>
            {
                float bx = p.x / 0.17f, by = (p.y - 0.33f) / 0.26f;
                return p.z > 0.06f && bx * bx + by * by < 1f ? Belly : Scales;
            });

            for (int side = -1; side <= 1; side += 2)
            {
                // Stubby feet with three little claws.
                var foot = new Vector3(side * 0.13f, 0.07f, 0.06f);
                kit.AddEllipsoid(foot, new Vector3(0.095f, 0.075f, 0.12f), Quaternion.Euler(0f, side * 12f, 0f), 2, _ => Scales);
                for (int k = -1; k <= 1; k++)
                    kit.AddEllipsoid(foot + new Vector3(k * 0.035f + side * 0.01f, -0.03f, 0.11f), new Vector3(0.018f, 0.016f, 0.02f),
                        Quaternion.identity, 1, _ => Horn);

                // Tiny arms held against the tummy.
                var arm = new Vector3(side * 0.19f, 0.42f, 0.13f);
                kit.AddEllipsoid(arm, new Vector3(0.055f, 0.1f, 0.055f), Quaternion.Euler(25f, 0f, side * 28f), 2, _ => Scales);
                kit.AddEllipsoid(arm + new Vector3(-side * 0.035f, -0.075f, 0.035f), new Vector3(0.04f, 0.035f, 0.04f),
                    Quaternion.identity, 1, _ => ScalesLight);
            }

            // Soft spikes along the back.
            for (int i = 0; i < 4; i++)
            {
                float y = 0.58f - i * 0.13f;
                float dy = (y - center.y) / 0.3f;
                var basePoint = new Vector3(0f, y, -0.23f * Mathf.Sqrt(Mathf.Max(0f, 1f - dy * dy)) + 0.02f);
                float size = 0.05f - i * 0.006f;
                kit.AddTube(basePoint, basePoint + new Vector3(0f, 0.045f, -size * 2.4f), size * 0.85f, 0f, 6, _ => Spike, true, false);
            }
        }

        static void BuildTail(MeshKit kit)
        {
            Vector3[] points =
            {
                new Vector3(0f, 0f, 0.04f), new Vector3(0f, -0.08f, -0.14f), new Vector3(0f, -0.08f, -0.29f),
                new Vector3(0f, -0.01f, -0.4f), new Vector3(0f, 0.08f, -0.46f),
            };
            float[] radii = { 0.1f, 0.075f, 0.055f, 0.038f, 0.026f };
            for (int i = 0; i < points.Length; i++)
            {
                kit.AddEllipsoid(points[i], Vector3.one * radii[i], Quaternion.identity, 2, _ => Scales);
                if (i + 1 < points.Length)
                    kit.AddTube(points[i], points[i + 1], radii[i], radii[i + 1], 10, _ => Scales, false, false);
            }
            for (int i = 1; i < 4; i++)
            {
                Vector3 p = points[i] + Vector3.up * radii[i] * 0.85f;
                kit.AddTube(p, p + new Vector3(0f, 0.035f, -0.045f), radii[i] * 0.45f, 0f, 5, _ => Spike, true, false);
            }

            // Spade-shaped tail tip.
            Vector3 tip = points[4] + new Vector3(0f, 0.05f, -0.02f);
            kit.AddEllipsoid(tip, new Vector3(0.022f, 0.065f, 0.05f), Quaternion.Euler(-35f, 0f, 0f), 2, _ => Spike);
            kit.AddTube(tip + new Vector3(0f, 0.03f, -0.02f), tip + new Vector3(0f, 0.09f, -0.06f), 0.03f, 0f, 6, _ => Spike, false, false);
        }

        static void BuildHead(MeshKit kit)
        {
            kit.AddEllipsoid(HeadCenter, HeadRadii, Quaternion.identity, 3, _ => Scales);
            kit.AddEllipsoid(MuzzleCenter, MuzzleRadii, Quaternion.identity, 3, _ => ScalesLight);

            for (int side = -1; side <= 1; side += 2)
            {
                float nx = side * 0.045f, ny = 0.15f;
                kit.AddEllipsoid(new Vector3(nx, ny, FaceFront(nx, ny) - 0.004f), new Vector3(0.016f, 0.011f, 0.01f),
                    Quaternion.identity, 1, _ => Mouth);

                float cx = side * 0.165f, cy = 0.12f;
                kit.AddEllipsoid(new Vector3(cx, cy, FaceFront(cx, cy) - 0.012f), new Vector3(0.05f, 0.03f, 0.02f),
                    Quaternion.Euler(0f, side * 40f, 0f), 2, _ => Cheek);

                // Little cream horns pointing back.
                var hornBase = new Vector3(side * 0.1f, 0.37f, -0.05f);
                kit.AddTube(hornBase, hornBase + new Vector3(side * 0.035f, 0.13f, -0.09f), 0.042f, 0f, 8, _ => Horn, true, false);

                // Soft ear fins.
                kit.AddEllipsoid(new Vector3(side * 0.245f, 0.27f, -0.05f), new Vector3(0.025f, 0.085f, 0.06f),
                    Quaternion.Euler(0f, 0f, -side * 38f), 2, _ => Membrane);
            }

            // Big closed-mouth smile.
            for (int i = -10; i <= 10; i++)
            {
                float u = i / 10f;
                float x = 0.075f * u, y = 0.078f + 0.03f * u * u;
                kit.AddEllipsoid(new Vector3(x, y, FaceFront(x, y) - 0.003f), new Vector3(0.012f, 0.008f, 0.01f),
                    Quaternion.identity, 1, _ => Mouth);
            }
            // One tiny tooth peeking out.
            kit.AddTube(new Vector3(0.035f, 0.084f, FaceFront(0.035f, 0.084f) - 0.002f),
                new Vector3(0.035f, 0.06f, FaceFront(0.035f, 0.06f) + 0.004f), 0.011f, 0f, 5, _ => Color.white, true, false);

            // Crest of small spikes over the head.
            Vector3[] crest = { new Vector3(0f, 0.42f, 0.02f), new Vector3(0f, 0.39f, -0.09f), new Vector3(0f, 0.31f, -0.18f) };
            for (int i = 0; i < crest.Length; i++)
                kit.AddTube(crest[i], crest[i] + new Vector3(0f, 0.05f, -0.055f), 0.036f - i * 0.005f, 0f, 6, _ => Spike, true, false);
        }

        static void BuildEyes(MeshKit kit)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 0.1f, y = 0.255f;
                float surface = FaceFront(x, y) - 0.025f;
                Vector3 eye = new Vector3(x, y, surface) - EyePivot;
                kit.AddEllipsoid(eye, new Vector3(0.068f, 0.082f, 0.048f), Quaternion.identity, 2,
                    p => Color.Lerp(EyeIris, EyeDark, Mathf.Clamp01((p.y - eye.y + 0.06f) / 0.07f)));
                kit.AddEllipsoid(eye + new Vector3(-0.018f, 0.03f, 0.042f), Vector3.one * 0.022f, Quaternion.identity, 1, _ => Color.white);
                kit.AddEllipsoid(eye + new Vector3(0.022f, -0.025f, 0.04f), Vector3.one * 0.01f, Quaternion.identity, 1, _ => Color.white);
            }
        }

        static void BuildWing(MeshKit kit, float side)
        {
            Vector3 S(float x, float y, float z) => new Vector3(x * side, y, z);
            Vector3 root = S(0f, 0f, 0f), wrist = S(0.17f, 0.13f, -0.02f), top = S(0.35f, 0.2f, -0.05f);
            Vector3 finger1 = S(0.39f, 0.03f, -0.06f), finger2 = S(0.28f, -0.08f, -0.05f);
            kit.AddTube(root, wrist, 0.026f, 0.02f, 6, _ => WingBone, true, false);
            kit.AddTube(wrist, top, 0.02f, 0.01f, 6, _ => WingBone, false, true);
            kit.AddTube(wrist, finger1, 0.014f, 0.006f, 5, _ => WingBone, false, true);
            kit.AddTube(wrist, finger2, 0.014f, 0.006f, 5, _ => WingBone, false, true);
            kit.AddEllipsoid(top, Vector3.one * 0.016f, Quaternion.identity, 1, _ => Horn);

            // Scalloped membrane between the fingers, lighter towards its edge.
            Vector3[] outline =
            {
                root, top, S(0.33f, 0.09f, -0.055f), finger1, S(0.29f, 0.01f, -0.05f), finger2,
                S(0.15f, -0.03f, -0.03f), S(0.03f, -0.07f, 0f),
            };
            Vector3 hub = S(0.18f, 0.06f, -0.03f);
            for (int i = 0; i < outline.Length; i++)
            {
                Vector3 a = outline[i], b = outline[(i + 1) % outline.Length];
                Color c = Color.Lerp(Membrane * 0.9f, Color.Lerp(Membrane, Color.white, 0.25f), Mathf.Clamp01(((a + b) * 0.5f - hub).magnitude / 0.2f));
                c.a = 1f;
                DoubleFace(kit, hub, a, b, c);
            }
        }

        static ParticleSystem CreatePuff(Transform head, Material sparkle)
        {
            var go = new GameObject("Sparkle Puff");
            go.transform.SetParent(head, false);
            go.transform.localPosition = MouthPoint;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 1.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.075f);
            var colors = new Gradient();
            colors.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.6f, 0.85f), 0f), new GradientColorKey(new Color(1f, 0.9f, 0.45f), 0.5f),
                    new GradientColorKey(new Color(0.7f, 0.8f, 1f), 1f),
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            main.startColor = new ParticleSystem.MinMaxGradient(colors) { mode = ParticleSystemGradientMode.RandomColor };
            main.gravityModifier = -0.04f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 80;

            var emission = ps.emission;
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 28f;
            shape.radius = 0.02f;

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.15f;
            noise.frequency = 1.2f;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = sparkle;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return ps;
        }

        // ---------- Parent dragon flying in the sky ----------

        /// <summary>The baby's parent: a long, gentle dragon gliding in wide loops around the volcano.</summary>
        public static SkyDragon CreateSkyDragon(Transform parent, Material material)
        {
            var root = new GameObject("Sky Dragon");
            root.transform.SetParent(parent, false);
            var body = Part("Body", root.transform, Vector3.zero, MakeMesh("Sky Dragon Body", BuildSkyBody), material);
            Transform head = Part("Head", body, new Vector3(0f, 0.2f, 0.66f), MakeMesh("Sky Dragon Head", BuildSkyHead), material);

            // Tail made of chained segments so it can ripple.
            var tail = new Transform[4];
            Transform link = body;
            Vector3 at = new Vector3(0f, -0.02f, -0.36f);
            for (int i = 0; i < tail.Length; i++)
            {
                float length = 0.3f - i * 0.03f, r0 = 0.13f - i * 0.03f, r1 = Mathf.Max(0.025f, r0 - 0.03f);
                bool last = i == tail.Length - 1;
                tail[i] = Part("Tail " + i, link, at, MakeMesh("Sky Dragon Tail " + i, kit => BuildSkyTail(kit, length, r0, r1, last)), material);
                link = tail[i];
                at = new Vector3(0f, 0f, -length);
            }

            const float wingScale = 3.2f;
            var rightWing = Part("Right Wing", body, new Vector3(0.12f, 0.1f, 0.12f), MakeMesh("Sky Dragon Right Wing", kit => BuildWing(kit, 1f)), material);
            var leftWing = Part("Left Wing", body, new Vector3(-0.12f, 0.1f, 0.12f), MakeMesh("Sky Dragon Left Wing", kit => BuildWing(kit, -1f)), material);
            rightWing.localScale = leftWing.localScale = Vector3.one * wingScale;

            root.transform.localScale = Vector3.one * 3f;
            var dragon = root.AddComponent<SkyDragon>();
            dragon.Configure(head, leftWing, rightWing, tail);
            dragon.Step(0f);
            return dragon;
        }

        static void BuildSkyBody(MeshKit kit)
        {
            // Long body along z with a cream belly, tucked legs and soft spikes along the back.
            kit.AddEllipsoid(Vector3.zero, new Vector3(0.2f, 0.18f, 0.42f), Quaternion.identity, 3, p => p.y < -0.06f ? Belly : Scales);
            kit.AddTube(new Vector3(0f, 0.02f, 0.3f), new Vector3(0f, 0.17f, 0.6f), 0.13f, 0.085f, 10, p => p.y < 0.04f + (p.z - 0.3f) * 0.4f ? Belly : Scales, false, false);
            for (int side = -1; side <= 1; side += 2)
            {
                foreach (float z in new[] { 0.2f, -0.22f })
                {
                    var leg = new Vector3(side * 0.12f, -0.15f, z);
                    kit.AddEllipsoid(leg, new Vector3(0.055f, 0.05f, 0.1f), Quaternion.Euler(-20f, 0f, 0f), 2, _ => Scales);
                    kit.AddEllipsoid(leg + new Vector3(0f, -0.02f, -0.09f), new Vector3(0.04f, 0.025f, 0.04f), Quaternion.identity, 1, _ => ScalesLight);
                }
            }
            for (int i = 0; i < 6; i++)
            {
                float z = 0.5f - i * 0.17f;
                float dz = z / 0.42f;
                float y = i == 0 ? 0.16f : 0.18f * Mathf.Sqrt(Mathf.Max(0f, 1f - dz * dz)) - 0.01f;
                kit.AddTube(new Vector3(0f, y, z), new Vector3(0f, y + 0.07f, z - 0.08f), 0.045f, 0f, 6, _ => Spike, true, false);
            }
        }

        static void BuildSkyHead(MeshKit kit)
        {
            kit.AddEllipsoid(Vector3.zero, new Vector3(0.13f, 0.12f, 0.14f), Quaternion.identity, 3, _ => Scales);
            var muzzle = new Vector3(0f, -0.03f, 0.13f);
            kit.AddEllipsoid(muzzle, new Vector3(0.09f, 0.07f, 0.1f), Quaternion.identity, 2, _ => ScalesLight);
            for (int side = -1; side <= 1; side += 2)
            {
                // Big friendly eyes with a highlight, rosy cheeks, cream horns and soft ear fins.
                var eye = new Vector3(side * 0.085f, 0.04f, 0.085f);
                kit.AddEllipsoid(eye, new Vector3(0.045f, 0.052f, 0.04f), Quaternion.identity, 2, _ => EyeDark);
                kit.AddEllipsoid(eye + new Vector3(side * 0.012f, 0.02f, 0.03f), Vector3.one * 0.014f, Quaternion.identity, 1, _ => Color.white);
                kit.AddEllipsoid(new Vector3(side * 0.1f, -0.04f, 0.1f), new Vector3(0.035f, 0.022f, 0.02f), Quaternion.Euler(0f, side * 40f, 0f), 1, _ => Cheek);
                kit.AddEllipsoid(muzzle + new Vector3(side * 0.03f, 0.03f, 0.09f), new Vector3(0.01f, 0.008f, 0.006f), Quaternion.identity, 1, _ => Mouth);
                var hornBase = new Vector3(side * 0.06f, 0.1f, -0.06f);
                kit.AddTube(hornBase, hornBase + new Vector3(side * 0.03f, 0.08f, -0.12f), 0.03f, 0f, 8, _ => Horn, true, false);
                kit.AddEllipsoid(new Vector3(side * 0.13f, 0.05f, -0.05f), new Vector3(0.015f, 0.05f, 0.04f), Quaternion.Euler(0f, 0f, -side * 40f), 2, _ => Membrane);
            }
            for (int i = 0; i < 2; i++)
                kit.AddTube(new Vector3(0f, 0.12f - i * 0.03f, -0.04f - i * 0.08f), new Vector3(0f, 0.15f - i * 0.03f, -0.1f - i * 0.08f),
                    0.025f, 0f, 6, _ => Spike, true, false);
        }

        static void BuildSkyTail(MeshKit kit, float length, float r0, float r1, bool last)
        {
            var end = new Vector3(0f, 0f, -length);
            kit.AddEllipsoid(Vector3.zero, Vector3.one * r0, Quaternion.identity, 2, _ => Scales);
            kit.AddTube(Vector3.zero, end, r0, r1, 10, _ => Scales, false, false);
            kit.AddTube(new Vector3(0f, r0 * 0.85f, -length * 0.4f), new Vector3(0f, r0 * 0.85f + 0.05f, -length * 0.4f - 0.07f), r0 * 0.4f, 0f, 5,
                _ => Spike, true, false);
            if (!last)
                return;
            kit.AddEllipsoid(end, Vector3.one * r1, Quaternion.identity, 1, _ => Scales);
            // Spade-shaped tip, flat like a little fin.
            kit.AddEllipsoid(end + new Vector3(0f, 0f, -0.07f), new Vector3(0.09f, 0.02f, 0.08f), Quaternion.identity, 2, _ => Spike);
        }

        /// <summary>Horizontal direction across the line of sight that points back toward the middle.</summary>
        static Vector3 TowardMiddle(Vector3 spot)
        {
            Vector3 side = Vector3.Cross(spot.normalized, Vector3.up);
            return side.x * spot.x > 0f ? -side : side;
        }

        /// <summary>Spots of the hiding rocks, shared with the scenery so nothing else is placed on them.</summary>
        public static readonly Vector2 BigRock = new Vector2(3.4f, 7.2f);
        public static readonly Vector2 CrystalMound = new Vector2(-2.5f, 5.2f);
        public static readonly Vector2 RockPillar = new Vector2(-5.4f, 11.5f);
        public const float PillarHeight = 2.2f;

        public static bool NearHideout(float x, float z, float margin)
        {
            var p = new Vector2(x, z);
            return (p - BigRock).magnitude < 1.8f + margin || (p - CrystalMound).magnitude < 1.4f + margin
                || (p - RockPillar).magnitude < 2.2f + margin;
        }

        static void AddRock(MeshKit kit, Vector3 center, Vector3 radii, float yaw, int seed)
        {
            kit.AddEllipsoid(center, radii, Quaternion.Euler(0f, yaw, 0f), 2,
                p => Color.Lerp(Rock, RockLight, Mathf.Clamp01((p.y - center.y) / radii.y * 0.5f + 0.5f) * 0.8f
                                                   + MeshKit.Hash01(p, seed) * 0.2f), 0.16f, seed);
        }

        /// <summary>Three hiding places: beside a big rock, behind a crystal mound, behind a tall rock pillar.</summary>
        static DragonAgent.Hideout[] CreateHideouts(Transform parent, Material material)
        {
            var kit = new MeshKit();

            Vector3 rock = new Vector3(BigRock.x, 0f, BigRock.y);
            Vector3 rockBase = Ground(rock);
            AddRock(kit, rockBase + new Vector3(0f, 0.3f, 0f), new Vector3(1.05f, 1.15f, 0.9f), 25f, 3);
            AddRock(kit, rockBase + new Vector3(0.75f, 0f, 0.35f), new Vector3(0.5f, 0.55f, 0.45f), 60f, 4);
            VolcanoBuilder.AddCrystalCluster(kit, rockBase + new Vector3(-0.25f, 1.3f, -0.1f), 0.55f, 0, new System.Random(5));
            Vector3 toViewer = -rock.normalized;
            var bigRock = new DragonAgent.Hideout
            {
                entrance = DragonAgent.Entrance.StepAside,
                hidden = rock - toViewer * 0.95f,
                shown = rock + TowardMiddle(rock) * 1.5f + toViewer * 0.7f,
            };

            Vector3 mound = new Vector3(CrystalMound.x, 0f, CrystalMound.y);
            Vector3 moundBase = Ground(mound);
            AddRock(kit, moundBase, new Vector3(0.8f, 0.5f, 0.6f), -15f, 6);
            AddRock(kit, moundBase + new Vector3(-0.55f, 0f, 0.2f), new Vector3(0.4f, 0.32f, 0.36f), 30f, 7);
            VolcanoBuilder.AddCrystalCluster(kit, moundBase + new Vector3(0.1f, 0.35f, 0f), 0.7f, 1, new System.Random(8));
            Vector3 behindMound = mound + mound.normalized * 0.9f;
            var crystalMound = new DragonAgent.Hideout
            {
                entrance = DragonAgent.Entrance.PopUp,
                hidden = behindMound,
                shown = behindMound,
            };

            Vector3 pillar = new Vector3(RockPillar.x, 0f, RockPillar.y);
            Vector3 pillarBase = Ground(pillar);
            var rnd = new System.Random(21);
            for (int k = 0; k < 5; k++)
            {
                float a = k * Mathf.PI * 2f / 5f + 0.4f;
                float h = PillarHeight * (k == 0 ? 1f : 0.7f + 0.25f * (float)rnd.NextDouble());
                Vector3 b = pillarBase + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (k == 0 ? 0f : 0.42f) + Vector3.down * 0.3f;
                Color col = Color.Lerp(Rock, RockLight, 0.2f + 0.15f * k);
                kit.AddTube(b, b + Vector3.up * (h + 0.3f), 0.38f, 0.3f, 6, p => Color.Lerp(col * 0.8f, col, (p.y - b.y) / h), false, true, a);
            }
            VolcanoBuilder.AddCrystalCluster(kit, pillarBase + new Vector3(0.6f, 0f, -0.45f), 0.6f, 2, new System.Random(9));
            Vector3 pillarToViewer = -pillar.normalized;
            var rockPillar = new DragonAgent.Hideout
            {
                entrance = DragonAgent.Entrance.FlyOver,
                hidden = pillar - pillarToViewer * 1.1f,
                shown = pillar + TowardMiddle(pillar) * 2.1f + pillarToViewer * 0.9f,
            };

            var mesh = MagicLandSetup.SaveMesh(kit.ToMesh("Dragon Hideouts"), "Dragon Hideouts");
            Part("Dragon Hideouts", parent, Vector3.zero, mesh, material);
            return new[] { bigRock, crystalMound, rockPillar };
        }
    }
}
