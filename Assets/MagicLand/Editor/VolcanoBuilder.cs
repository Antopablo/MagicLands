using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace MagicLand.EditorTools
{
    /// <summary>
    /// Generates the volcano world: warm red ground with glowing cracks, a big friendly volcano in the
    /// distance, pastel crystals, fire flowers, birds and a baby dragon who comes to say hello.
    /// </summary>
    public static class VolcanoBuilder
    {
        public const string ScenePath = "Assets/Scenes/Volcano.unity";
        public const string SceneName = "Volcano";
        const string MeshFolderName = "Meshes Volcano";

        internal static readonly Color Horizon = new Color(0.96f, 0.6f, 0.5f);

        static readonly Color[] CrystalColors =
        {
            new Color(1f, 0.62f, 0.85f), new Color(0.76f, 0.62f, 1f), new Color(0.52f, 0.95f, 0.92f), new Color(1f, 0.85f, 0.45f),
        };

        static readonly Color LavaEdge = new Color(0.95f, 0.3f, 0.12f);
        static readonly Color LavaCore = new Color(1f, 0.84f, 0.42f);

        public static void Build()
        {
            MagicLandSetup.EnsureFolder("Assets/MagicLand", "Generated");
            AssetDatabase.DeleteAsset(MagicLandSetup.GeneratedFolder + "/" + MeshFolderName);
            // Separate mesh folder: rebuilding one world never deletes the meshes of another.
            MagicLandSetup.UseMeshFolder(MeshFolderName);
            BirdBuilder.ResetCache();
            try
            {
                BuildScene();
            }
            finally
            {
                MagicLandSetup.UseMeshFolder(null);
                BirdBuilder.ResetCache();
            }
        }

        static Material Nature(string name, float fogStart, float fogEnd, float fogMax, Color ambient, Color light)
        {
            return MagicLandSetup.CreateMaterial(name, "MagicLand/Nature", m =>
            {
                m.SetColor("_FogColor", Horizon);
                m.SetFloat("_FogStart", fogStart);
                m.SetFloat("_FogEnd", fogEnd);
                m.SetFloat("_FogMax", fogMax);
                m.SetColor("_Ambient", ambient);
                m.SetColor("_LightColor", light);
                m.SetFloat("_WindAmount", 1f);
                m.SetFloat("_Cull", (float)CullMode.Back);
            });
        }

        static Material Lava(string name, float intensity, float fogStart, float fogEnd, float fogMax)
        {
            return MagicLandSetup.CreateMaterial(name, "MagicLand/Lava", m =>
            {
                m.SetFloat("_Intensity", intensity);
                m.SetFloat("_Speed", 1.3f);
                m.SetColor("_FogColor", Horizon);
                m.SetFloat("_FogStart", fogStart);
                m.SetFloat("_FogEnd", fogEnd);
                m.SetFloat("_FogMax", fogMax);
            });
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

        static Vector3 OnGround(float x, float z) => new Vector3(x, VolcanoGround.HeightAt(x, z), z);

        static float R(System.Random rnd, float min, float max) => min + (float)rnd.NextDouble() * (max - min);

        static void BuildScene()
        {
            MagicLandSetup.EnsureFolder(MagicLandSetup.GeneratedFolder, "Audio");

            var skyMat = MagicLandSetup.CreateMaterial("VolcanoSky", "MagicLand/Sky", m =>
            {
                m.SetColor("_TopColor", new Color(0.2f, 0.13f, 0.38f));
                m.SetColor("_MidColor", Horizon);
                m.SetColor("_BottomColor", new Color(0.5f, 0.3f, 0.36f));
                m.SetColor("_AuroraColor", new Color(0.5f, 0.25f, 0.4f));
                m.SetFloat("_StarBrightness", 0.7f);
            });
            var sparkMat = MagicLandSetup.GlowMaterial("Sparkle", Color.white, 1.6f, false);
            var fingerMat = MagicLandSetup.GlowMaterial("Fingertip", new Color(0.7f, 0.9f, 1f), 2f, false);
            var moonMat = MagicLandSetup.GlowMaterial("VolcanoMoon", new Color(1f, 0.88f, 0.86f), 0.85f, true);
            var craterGlowMat = MagicLandSetup.GlowMaterial("VolcanoCraterGlow", new Color(1f, 0.5f, 0.25f), 0.8f, true);
            var smokeMat = MagicLandSetup.GlowMaterial("VolcanoSmoke", new Color(0.75f, 0.55f, 0.8f), 0.22f, false);
            var emberMat = MagicLandSetup.GlowMaterial("Ember", new Color(1f, 0.7f, 0.35f), 1.8f, false);
            var pondGlowMat = MagicLandSetup.GlowMaterial("LavaPondGlow", new Color(1f, 0.55f, 0.25f), 0.6f, true);
            var crystalGlowMats = new Material[CrystalColors.Length];
            for (int i = 0; i < CrystalColors.Length; i++)
                crystalGlowMats[i] = MagicLandSetup.GlowMaterial("CrystalGlow " + i, CrystalColors[i], 0.5f, true);

            var natureMat = Nature("VolcanoNature", 12f, 60f, 0.85f, new Color(0.5f, 0.4f, 0.55f), new Color(1f, 0.8f, 0.62f));
            var farMat = Nature("VolcanoFar", 30f, 120f, 0.5f, new Color(0.55f, 0.42f, 0.55f), new Color(0.95f, 0.7f, 0.55f));
            var lavaMat = Lava("VolcanoLava", 1f, 14f, 60f, 0.6f);
            var lavaFarMat = Lava("VolcanoLavaFar", 1f, 30f, 120f, 0.35f);

            string audio = MagicLandSetup.AudioFolder;
            var windClip = AudioSynth.Wind(audio);
            var birdClips = AudioSynth.Birds(audio);
            var crackleClip = AudioSynth.Crackle(audio);
            var chirpClip = AudioSynth.DragonChirp(audio);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.black;

            var camGo = MagicLandSetup.CreateRig(Horizon, fingerMat);
            var content = new GameObject("Content Root");
            content.AddComponent<ViewRecenter>().SetHead(camGo.transform);
            var nature = content.AddComponent<NatureEnvironment>();

            var sky = MagicLandSetup.CreateMeshObject("Sky", PrimitiveType.Sphere, content.transform, Vector3.zero, 150f, skyMat);
            sky.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            MagicLandSetup.CreateMeshObject("Moon", PrimitiveType.Sphere, content.transform, new Vector3(-22f, 17f, 50f), 4f, moonMat);

            var cracks = CreateCracks();
            BuildGround(content.transform, natureMat, cracks);
            BuildCrackMesh(content.transform, lavaMat, cracks);
            BuildVolcano(content.transform, farMat, lavaFarMat, craterGlowMat);
            var smoke = CreateSmoke(content.transform, smokeMat);
            BuildScenery(content.transform, natureMat, lavaMat, crystalGlowMats, pondGlowMat);

            var babyDragon = DragonBuilder.Create(content.transform, natureMat, sparkMat, chirpClip);
            BirdBuilder.CreateFlock(content.transform, natureMat, 15f);
            BirdBuilder.CreateSoaringBirds(content.transform, natureMat);
            CreateEmbers(content.transform, emberMat);

            var volcano = content.transform.Find("Volcano");
            var glows = new List<Transform>();
            foreach (Transform glow in content.transform.Find("Crystal Glows"))
                glows.Add(glow);
            glows.Add(content.transform.Find("Lava Pond Glow"));
            VolcanoLifeBuilder.Build(content.transform, new VolcanoLifeBuilder.Context
            {
                nature = natureMat, far = farMat, lava = lavaMat, spark = sparkMat, smoke = smokeMat, ember = emberMat,
                birdSongs = birdClips,
                babyDragon = babyDragon,
                volcanoSmoke = smoke,
                craterGlow = volcano.Find("Crater Glow"),
                volcanoLava = new[] { volcano.Find("Volcano Lava").GetComponent<Renderer>() },
                glows = glows.ToArray(),
                pondSurface = PondSurface,
            });

            // Ambient sounds: soft wind, gentle crackling of the warm ground, distant birds.
            var windGo = new GameObject("Wind Sound");
            windGo.transform.SetParent(content.transform, false);
            var windSource = windGo.AddComponent<AudioSource>();
            windSource.clip = windClip;
            windSource.loop = true;
            windSource.playOnAwake = true;
            windSource.spatialBlend = 0f;
            windSource.volume = 0.3f;
            nature.Configure(windSource);

            var crackleGo = new GameObject("Crackle Sound");
            crackleGo.transform.SetParent(content.transform, false);
            var crackle = crackleGo.AddComponent<AudioSource>();
            crackle.clip = crackleClip;
            crackle.loop = true;
            crackle.playOnAwake = true;
            crackle.spatialBlend = 0f;
            crackle.volume = 0.28f;

            var birds = new GameObject("Birds");
            birds.transform.SetParent(content.transform, false);
            birds.AddComponent<BirdSongs>().Configure(birdClips);

            MagicLandSetup.CreateReturnToMenu();

            WorldSelectBuilder.CaptureCard(content.transform, SceneName);
            EditorSceneManager.SaveScene(scene, ScenePath);
            MagicLandSetup.RegisterScenes();
            AssetDatabase.SaveAssets();
        }

        // ---------- Ground and glowing cracks ----------

        /// <summary>Wandering crack lines on the ground around the viewer (horizontal points).</summary>
        static List<List<Vector2>> CreateCracks()
        {
            var cracks = new List<List<Vector2>>();
            var rnd = new System.Random(13);

            void Grow(Vector2 start, float heading, float length, bool canBranch)
            {
                var line = new List<Vector2> { start };
                Vector2 p = start;
                for (float s = 0f; s < length; s += 0.3f)
                {
                    heading += R(rnd, -0.45f, 0.45f);
                    p += new Vector2(Mathf.Sin(heading), Mathf.Cos(heading)) * 0.3f;
                    if (VolcanoGround.NearLavaPond(p.x, p.y, 0.4f) || DragonBuilder.NearHideout(p.x, p.y, 0f) || p.magnitude < 1.6f
                        || p.magnitude > 18f)
                        break;
                    line.Add(p);
                    if (canBranch && rnd.NextDouble() < 0.07)
                        Grow(p, heading + (rnd.NextDouble() < 0.5 ? -1f : 1f) * R(rnd, 0.6f, 1.1f), R(rnd, 0.8f, 2f), false);
                }
                if (line.Count > 2)
                    cracks.Add(line);
            }

            for (int i = 0; i < 22; i++)
            {
                float r = 2.4f + 15f * Mathf.Pow((float)rnd.NextDouble(), 1.3f);
                float a = R(rnd, -1.9f, 1.9f);
                var start = new Vector2(Mathf.Sin(a) * r, Mathf.Cos(a) * r);
                if (VolcanoGround.NearLavaPond(start.x, start.y, 0.6f) || DragonBuilder.NearHideout(start.x, start.y, 0.2f))
                    continue;
                Grow(start, R(rnd, 0f, Mathf.PI * 2f), R(rnd, 1.5f, 5f), true);
            }
            return cracks;
        }

        static float DistanceToCracks(Vector2 p, List<List<Vector2>> cracks)
        {
            float best = float.MaxValue;
            foreach (var line in cracks)
                foreach (var q in line)
                    best = Mathf.Min(best, (q - p).sqrMagnitude);
            return Mathf.Sqrt(best);
        }

        static void BuildGround(Transform parent, Material mat, List<List<Vector2>> cracks)
        {
            var kit = new MeshKit();
            const int rings = 32, segments = 60;
            const float maxRadius = 72f;

            Vector3 P(int ring, int seg)
            {
                float r = maxRadius * Mathf.Pow(ring / (float)rings, 1.7f);
                float a = seg * Mathf.PI * 2f / segments + (ring % 2) * Mathf.PI / segments;
                return OnGround(Mathf.Sin(a) * r, Mathf.Cos(a) * r);
            }

            Color GroundColor(Vector3 p)
            {
                float n = Mathf.PerlinNoise(p.x * 0.13f + 2f, p.z * 0.13f + 8f);
                var c = Color.Lerp(new Color(0.6f, 0.22f, 0.17f), new Color(0.75f, 0.33f, 0.24f), n);
                float patch = Mathf.PerlinNoise(p.x * 0.07f + 30f, p.z * 0.07f + 12f);
                c = Color.Lerp(c, new Color(0.45f, 0.2f, 0.23f), Mathf.SmoothStep(0f, 1f, (patch - 0.58f) * 6f) * 0.7f);
                float r = new Vector2(p.x, p.z).magnitude;
                c = Color.Lerp(c, new Color(0.44f, 0.2f, 0.25f), Mathf.InverseLerp(14f, 34f, r));
                // Warm reflection of the lava on the ground around the cracks.
                float d = DistanceToCracks(new Vector2(p.x, p.z), cracks);
                return Color.Lerp(c, new Color(1f, 0.52f, 0.3f), Mathf.Clamp01(1f - d / 0.9f) * 0.4f);
            }

            void Face(Vector3 a, Vector3 b, Vector3 c)
            {
                Vector3 centroid = (a + b + c) / 3f;
                kit.AddFace(a, b, c, centroid - Vector3.up, GroundColor(centroid));
            }

            for (int s = 0; s < segments; s++)
                Face(P(0, 0), P(1, s), P(1, s + 1));
            for (int ring = 1; ring < rings; ring++)
                for (int s = 0; s < segments; s++)
                {
                    Vector3 a = P(ring, s), b = P(ring, s + 1), c = P(ring + 1, s), d = P(ring + 1, s + 1);
                    Face(a, b, c);
                    Face(b, d, c);
                }
            MeshObject(parent, kit, "Volcano Ground", mat);
        }

        /// <summary>Thin glowing ribbons laid on the ground: bright in the middle, deep orange at the edges.</summary>
        static void BuildCrackMesh(Transform parent, Material mat, List<List<Vector2>> cracks)
        {
            var kit = new MeshKit { Wind = p => new Vector2(1f, (p.x * 0.9f + p.z * 1.3f) * 1.5f) };
            var rnd = new System.Random(29);
            foreach (var line in cracks)
            {
                float width = R(rnd, 0.07f, 0.14f);
                int n = line.Count;
                var left = new Vector3[n];
                var middle = new Vector3[n];
                var right = new Vector3[n];
                for (int i = 0; i < n; i++)
                {
                    Vector2 dir = (line[Mathf.Min(i + 1, n - 1)] - line[Mathf.Max(i - 1, 0)]).normalized;
                    Vector2 side = new Vector2(dir.y, -dir.x);
                    float taper = Mathf.Lerp(0.25f, 1f, Mathf.Sin(Mathf.PI * i / (n - 1)));
                    float w = width * taper * (0.75f + 0.5f * Mathf.PerlinNoise(i * 0.7f, n));
                    Vector2 p = line[i];
                    float lift = 0.025f + p.magnitude * 0.003f;
                    Vector3 Lift(Vector2 q) => OnGround(q.x, q.y) + Vector3.up * lift;
                    left[i] = Lift(p - side * w);
                    middle[i] = Lift(p);
                    right[i] = Lift(p + side * w);
                }
                for (int i = 0; i + 1 < n; i++)
                {
                    kit.AddRaw(left[i], middle[i], left[i + 1], Vector3.up, LavaEdge, LavaCore, LavaEdge);
                    kit.AddRaw(middle[i], middle[i + 1], left[i + 1], Vector3.up, LavaCore, LavaCore, LavaEdge);
                    kit.AddRaw(middle[i], right[i], middle[i + 1], Vector3.up, LavaCore, LavaEdge, LavaCore);
                    kit.AddRaw(right[i], right[i + 1], middle[i + 1], Vector3.up, LavaEdge, LavaEdge, LavaCore);
                }
            }
            MeshObject(parent, kit, "Glowing Cracks", mat);
        }

        // ---------- Volcano ----------

        const float VolcanoBase = -3f;

        static float VolcanoTop => VolcanoBase + VolcanoGround.VolcanoHeight;

        /// <summary>Point on the volcano flank. s: 0 at the crater rim, 1 at the foot. a: 0 faces the viewer.</summary>
        static Vector3 VolcanoSurface(float s, float a)
        {
            Vector2 c = VolcanoGround.VolcanoCenter;
            float ridges = 1f + 0.07f * Mathf.Sin(a * 5f + s * 3f) + 0.08f * (Mathf.PerlinNoise(a * 1.3f + 5f, s * 3f) - 0.5f);
            float radius = Mathf.Lerp(VolcanoGround.CraterRadius, VolcanoGround.VolcanoBaseRadius, s) * (s > 0f ? ridges : 1f);
            float height = VolcanoBase + VolcanoGround.VolcanoHeight * Mathf.Pow(1f - s, 1.7f)
                           + 0.6f * (Mathf.PerlinNoise(a * 2f, s * 4f + 7f) - 0.5f) * s;
            return new Vector3(c.x + Mathf.Sin(a) * radius, height, c.y - Mathf.Cos(a) * radius);
        }

        static void BuildVolcano(Transform parent, Material mat, Material lavaMat, Material glowMat)
        {
            var kit = new MeshKit();
            const int rings = 16, segments = 44;
            Vector3 center = new Vector3(VolcanoGround.VolcanoCenter.x, VolcanoTop, VolcanoGround.VolcanoCenter.y);

            Color RockColor(Vector3 p)
            {
                float f = Mathf.InverseLerp(VolcanoBase, VolcanoTop, p.y);
                var c = Color.Lerp(new Color(0.5f, 0.26f, 0.34f), new Color(0.36f, 0.19f, 0.3f), f);
                c = Color.Lerp(c, new Color(0.88f, 0.42f, 0.32f), Mathf.SmoothStep(0f, 1f, (f - 0.82f) * 5.5f));
                return c * (0.92f + 0.16f * MeshKit.Hash01(p, 17));
            }

            Vector3 V(int ring, int seg) => VolcanoSurface(ring / (float)rings, seg * Mathf.PI * 2f / segments);
            for (int ring = 0; ring < rings; ring++)
                for (int s = 0; s < segments; s++)
                {
                    Vector3 a = V(ring, s), b = V(ring, s + 1), c = V(ring + 1, s), d = V(ring + 1, s + 1);
                    Vector3 inside = new Vector3(center.x, a.y - 2f, center.z);
                    kit.AddFace(a, b, c, inside, RockColor((a + b + c) / 3f));
                    kit.AddFace(b, d, c, inside, RockColor((b + d + c) / 3f));
                }

            // Crater: a short inner wall around a glowing lava lake.
            var lava = new MeshKit { Wind = p => new Vector2(1f, -p.y * 0.8f + p.x * 0.2f) };
            float lakeY = VolcanoTop - 1f;
            for (int s = 0; s < segments; s++)
            {
                float a0 = s * Mathf.PI * 2f / segments, a1 = (s + 1) * Mathf.PI * 2f / segments;
                Vector3 r0 = VolcanoSurface(0f, a0), r1 = VolcanoSurface(0f, a1);
                Vector3 i0 = Vector3.Lerp(new Vector3(center.x, lakeY, center.z), r0, 0.85f), i1 = Vector3.Lerp(new Vector3(center.x, lakeY, center.z), r1, 0.85f);
                i0.y = i1.y = lakeY;
                Vector3 behindWall = r0 + new Vector3(r0.x - center.x, 0f, r0.z - center.z).normalized * 3f + Vector3.down * 2f;
                kit.AddFace(r0, r1, i0, behindWall, new Color(0.7f, 0.32f, 0.3f));
                kit.AddFace(r1, i1, i0, behindWall, new Color(0.7f, 0.32f, 0.3f));
                lava.AddRaw(new Vector3(center.x, lakeY, center.z), i0, i1, Vector3.up, LavaCore, LavaEdge, LavaEdge);
            }

            // Three lava streams running down the side facing the viewer.
            float[] starts = { -0.38f, 0.1f, 0.52f };
            float[] lengths = { 0.55f, 0.78f, 0.42f };
            for (int k = 0; k < starts.Length; k++)
            {
                const int steps = 36;
                Vector3 prevL = Vector3.zero, prevM = Vector3.zero, prevR = Vector3.zero;
                for (int i = 0; i <= steps; i++)
                {
                    float s = lengths[k] * i / steps;
                    float a = starts[k] + 0.11f * Mathf.Sin(s * 7f + k * 2f);
                    float halfWidth = Mathf.Lerp(0.9f, 0.3f, i / (float)steps) / Mathf.Lerp(VolcanoGround.CraterRadius, VolcanoGround.VolcanoBaseRadius, s);
                    Vector3 m = VolcanoSurface(s, a), l = VolcanoSurface(s, a - halfWidth), r = VolcanoSurface(s, a + halfWidth);
                    Vector3 outward = (new Vector3(m.x - center.x, 0f, m.z - center.z).normalized + Vector3.up * 0.6f).normalized * 0.22f;
                    m += outward;
                    l += outward;
                    r += outward;
                    if (i > 0)
                    {
                        lava.AddRaw(prevL, prevM, l, Vector3.up, LavaEdge, LavaCore, LavaEdge);
                        lava.AddRaw(prevM, m, l, Vector3.up, LavaCore, LavaCore, LavaEdge);
                        lava.AddRaw(prevM, prevR, m, Vector3.up, LavaCore, LavaEdge, LavaCore);
                        lava.AddRaw(prevR, r, m, Vector3.up, LavaEdge, LavaEdge, LavaCore);
                    }
                    prevL = l;
                    prevM = m;
                    prevR = r;
                }
            }

            var root = new GameObject("Volcano");
            root.transform.SetParent(parent, false);
            MeshObject(root.transform, kit, "Volcano Mountain", mat);
            MeshObject(root.transform, lava, "Volcano Lava", lavaMat);
            MagicLandSetup.CreateMeshObject("Crater Glow", PrimitiveType.Sphere, root.transform, center + Vector3.up * 1.2f, 11f, glowMat);
        }

        static ParticleSystem CreateSmoke(Transform parent, Material mat)
        {
            var go = new GameObject("Volcano Smoke");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(VolcanoGround.VolcanoCenter.x, VolcanoTop + 0.5f, VolcanoGround.VolcanoCenter.y);
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(14f, 20f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.7f, 1.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(4f, 7f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.85f, 0.6f, 0.9f), new Color(1f, 0.65f, 0.7f));
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 60;

            var emission = ps.emission;
            emission.rateOverTime = 2.5f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 2.5f;

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
            velocity.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.8f));

            var rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);

            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.8f, 0.7f, 1f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0.7f, 0.6f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return ps;
        }

        // ---------- Scenery ----------

        /// <summary>Adds a cluster of pointed pastel crystals growing from a base point.</summary>
        internal static void AddCrystalCluster(MeshKit kit, Vector3 basePoint, float size, int colorIndex, System.Random rnd)
        {
            Color color = CrystalColors[colorIndex % CrystalColors.Length];
            int count = 3 + rnd.Next(3);
            for (int i = 0; i < count; i++)
            {
                float tilt = i == 0 ? R(rnd, 0f, 8f) : R(rnd, 18f, 42f);
                var rotation = Quaternion.Euler(0f, R(rnd, 0f, 360f), 0f) * Quaternion.Euler(tilt, 0f, 0f);
                Vector3 dir = rotation * Vector3.up;
                float height = size * (i == 0 ? 1f : R(rnd, 0.45f, 0.75f));
                float radius = height * R(rnd, 0.16f, 0.2f);
                Vector3 b = basePoint + new Vector3(R(rnd, -0.1f, 0.1f), -0.05f, R(rnd, -0.1f, 0.1f)) * size;
                Vector3 shoulder = b + dir * height * 0.78f, tip = b + dir * height;
                float spin = R(rnd, 0f, 1f);
                kit.AddTube(b, shoulder, radius, radius, 6,
                    p => Color.Lerp(color * 0.75f, color, Mathf.Clamp01(Vector3.Dot(p - b, dir) / height)), false, false, spin);
                kit.AddTube(shoulder, tip, radius, 0f, 6, _ => Color.Lerp(color, Color.white, 0.35f), false, false, spin);
            }
        }

        static bool Blocked(float x, float z, float margin) =>
            VolcanoGround.InClearing(x, z) || VolcanoGround.NearLavaPond(x, z, margin) || DragonBuilder.NearHideout(x, z, margin)
            || VolcanoLifeBuilder.Occupies(x, z, margin);

        /// <summary>Center of the lava pond surface, set when the pond is built.</summary>
        static Vector3 PondSurface;

        static void BuildScenery(Transform parent, Material natureMat, Material lavaMat, Material[] crystalGlows, Material pondGlow)
        {
            var rocks = new MeshKit();
            var plants = new MeshKit();
            var flowers = new MeshKit { Wind = p => new Vector2(0.5f, (p.x + p.z) * 2f) };
            var rnd = new System.Random(31);

            // Dark rocks, larger further away.
            for (int i = 0; i < 80; i++)
            {
                float r = R(rnd, 3f, 42f), a = R(rnd, -Mathf.PI, Mathf.PI);
                float x = Mathf.Sin(a) * r, z = Mathf.Cos(a) * r;
                if (Blocked(x, z, 0.8f) || (Mathf.Abs(x) < 9f && z > 30f))
                    continue;
                float size = R(rnd, 0.2f, 0.55f) * (1f + r / 18f);
                Vector3 c = OnGround(x, z);
                Color col = Color.Lerp(new Color(0.27f, 0.16f, 0.22f), new Color(0.42f, 0.26f, 0.3f), (float)rnd.NextDouble());
                rocks.AddEllipsoid(c, new Vector3(size, size * R(rnd, 0.5f, 0.85f), size * R(rnd, 0.7f, 1f)),
                    Quaternion.Euler(0f, R(rnd, 0f, 360f), 0f), 1, p => col * (0.85f + 0.3f * Mathf.Clamp01((p.y - c.y) / size)), 0.2f, rnd.Next());
            }

            // Small pebbles of dark glass and pale pumice near the viewer.
            for (int i = 0; i < 260; i++)
            {
                float r = R(rnd, 1.4f, 12f), a = R(rnd, -Mathf.PI, Mathf.PI);
                float x = Mathf.Sin(a) * r, z = Mathf.Cos(a) * r;
                if (VolcanoGround.NearLavaPond(x, z, 0.2f) || DragonBuilder.NearHideout(x, z, 0f) || VolcanoLifeBuilder.NearFeature(x, z, 0f))
                    continue;
                float s = R(rnd, 0.025f, 0.075f);
                Color col = rnd.NextDouble() < 0.6 ? new Color(0.2f, 0.13f, 0.2f) : new Color(0.86f, 0.72f, 0.66f);
                rocks.AddEllipsoid(OnGround(x, z), new Vector3(s, s * 0.6f, s), Quaternion.Euler(0f, R(rnd, 0f, 360f), 0f), 0, _ => col, 0.25f, rnd.Next());
            }

            // Pastel crystal clusters with a soft halo.
            var glowRoot = new GameObject("Crystal Glows");
            glowRoot.transform.SetParent(parent, false);
            int clusters = 0;
            for (int attempt = 0; attempt < 60 && clusters < 12; attempt++)
            {
                float r = R(rnd, 4f, 17f), a = R(rnd, -1.7f, 1.7f);
                float x = Mathf.Sin(a) * r, z = Mathf.Cos(a) * r;
                if (Blocked(x, z, 1.2f))
                    continue;
                float size = R(rnd, 0.5f, 1.1f);
                Vector3 b = OnGround(x, z);
                int color = clusters % CrystalColors.Length;
                AddCrystalCluster(rocks, b, size, color, rnd);
                rocks.AddEllipsoid(b, new Vector3(size * 0.45f, size * 0.18f, size * 0.4f), Quaternion.identity, 1,
                    _ => new Color(0.3f, 0.18f, 0.24f), 0.2f, rnd.Next());
                MagicLandSetup.CreateMeshObject("Crystal Glow " + clusters, PrimitiveType.Sphere, glowRoot.transform,
                    b + Vector3.up * size * 0.55f, size * 1.7f, crystalGlows[color]);
                clusters++;
            }

            // Fire flowers: little flame-shaped petals glowing on dark stems.
            for (int i = 0; i < 46; i++)
            {
                float r = R(rnd, 1.8f, 13f), a = R(rnd, -2.2f, 2.2f);
                float x = Mathf.Sin(a) * r, z = Mathf.Cos(a) * r;
                if (VolcanoGround.NearLavaPond(x, z, 0.4f) || DragonBuilder.NearHideout(x, z, 0.1f) || VolcanoLifeBuilder.NearFeature(x, z, 0.1f))
                    continue;
                Vector3 b = OnGround(x, z);
                float h = R(rnd, 0.14f, 0.3f);
                Vector3 head = b + new Vector3(R(rnd, -0.03f, 0.03f), h, R(rnd, -0.03f, 0.03f));
                Color stem = new Color(0.2f, 0.42f, 0.4f);
                plants.AddTube(b, head, 0.012f, 0.008f, 4, _ => stem, false, false);
                for (int s = -1; s <= 1; s += 2)
                    plants.AddEllipsoid(b + new Vector3(s * 0.04f, h * 0.3f, 0f), new Vector3(0.045f, 0.008f, 0.018f),
                        Quaternion.Euler(0f, R(rnd, 0f, 180f), s * -25f), 0, _ => stem * 1.15f);
                float spin = R(rnd, 0f, 360f);
                for (int k = 0; k < 5; k++)
                {
                    Vector3 dir = Quaternion.Euler(0f, spin + k * 72f, 0f) * Quaternion.Euler(28f, 0f, 0f) * Vector3.up;
                    flowers.AddTube(head, head + dir * 0.085f, 0.026f, 0f, 4, p =>
                        Color.Lerp(LavaEdge, LavaCore, Mathf.Clamp01((p.y - head.y) / 0.07f)), true, false);
                }
                flowers.AddEllipsoid(head + Vector3.up * 0.015f, Vector3.one * 0.02f, Quaternion.identity, 0, _ => new Color(1f, 0.95f, 0.7f));
            }

            BuildLavaPond(parent, rocks, lavaMat, pondGlow);

            MeshObject(parent, rocks, "Volcano Rocks", natureMat);
            MeshObject(parent, plants, "Fire Flower Stems", natureMat);
            MeshObject(parent, flowers, "Fire Flowers", lavaMat);
        }

        static void BuildLavaPond(Transform parent, MeshKit rocks, Material lavaMat, Material glowMat)
        {
            Vector2 c = VolcanoGround.LavaPond;
            float radius = VolcanoGround.LavaPondRadius;
            const int segments = 28;
            float Edge(float a) => radius * (1f + 0.12f * Mathf.Sin(a * 3f) + 0.06f * Mathf.Sin(a * 5f + 1f));

            float y = float.MinValue;
            for (int s = 0; s < segments; s++)
            {
                float a = s * Mathf.PI * 2f / segments;
                y = Mathf.Max(y, VolcanoGround.HeightAt(c.x + Mathf.Sin(a) * Edge(a), c.y + Mathf.Cos(a) * Edge(a)));
            }
            y += 0.03f;

            var kit = new MeshKit { Wind = p => new Vector2(1f, ((p - new Vector3(c.x, 0f, c.y)).magnitude) * 2.5f) };
            Vector3 center = new Vector3(c.x, y, c.y);
            PondSurface = center;
            Vector3 Ring(float a, float f) => center + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * Edge(a) * f;
            Color mid = Color.Lerp(LavaEdge, LavaCore, 0.55f);
            for (int s = 0; s < segments; s++)
            {
                float a0 = s * Mathf.PI * 2f / segments, a1 = (s + 1) * Mathf.PI * 2f / segments;
                Vector3 m0 = Ring(a0, 0.55f), m1 = Ring(a1, 0.55f), o0 = Ring(a0, 1f), o1 = Ring(a1, 1f);
                kit.AddRaw(center, m0, m1, Vector3.up, LavaCore, mid, mid);
                kit.AddRaw(m0, o0, m1, Vector3.up, mid, LavaEdge, mid);
                kit.AddRaw(m1, o0, o1, Vector3.up, mid, LavaEdge, LavaEdge);
            }
            MeshObject(parent, kit, "Lava Pond", lavaMat);

            // Dark rocks around the rim and two little crusts floating on it.
            var rnd = new System.Random(41);
            for (int s = 0; s < 16; s++)
            {
                float a = s * Mathf.PI * 2f / 16f + R(rnd, -0.1f, 0.1f);
                Vector3 p = Ring(a, 1.05f);
                p.y = VolcanoGround.HeightAt(p.x, p.z);
                float size = R(rnd, 0.22f, 0.38f);
                rocks.AddEllipsoid(p, new Vector3(size, size * 0.6f, size), Quaternion.Euler(0f, R(rnd, 0f, 360f), 0f), 1,
                    _ => new Color(0.24f, 0.14f, 0.2f), 0.2f, rnd.Next());
            }
            rocks.AddEllipsoid(center + new Vector3(0.4f, 0f, 0.2f), new Vector3(0.28f, 0.05f, 0.2f), Quaternion.Euler(0f, 30f, 0f), 1,
                _ => new Color(0.3f, 0.17f, 0.2f), 0.15f, 3);
            rocks.AddEllipsoid(center + new Vector3(-0.5f, 0f, -0.3f), new Vector3(0.18f, 0.04f, 0.14f), Quaternion.Euler(0f, -20f, 0f), 1,
                _ => new Color(0.3f, 0.17f, 0.2f), 0.15f, 4);

            MagicLandSetup.CreateMeshObject("Lava Pond Glow", PrimitiveType.Sphere, parent, center + Vector3.up * 0.4f, 3.4f, glowMat);
        }

        static void CreateEmbers(Transform parent, Material mat)
        {
            var go = new GameObject("Embers");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, -0.8f, 9f);
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.06f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.55f, 0.2f), new Color(1f, 0.85f, 0.45f));
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 160;

            var emission = ps.emission;
            emission.rateOverTime = 16f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(20f, 1f, 18f);

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.03f, 0.03f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.2f;
            noise.frequency = 0.4f;
            noise.scrollSpeed = 0.15f;

            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.6f, 0.5f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0.8f, 0.7f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }
}
