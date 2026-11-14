using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.OpenXR;

namespace MagicLand.EditorTools
{
    /// <summary>
    /// One-click project configuration and scene generation for the Quest build.
    /// Safe to run several times: generated assets and scene are overwritten.
    /// </summary>
    public static class MagicLandSetup
    {
        public const string ScenePath = "Assets/Scenes/MagicLand.unity";
        public const string SceneName = "MagicLand";
        internal const string GeneratedFolder = "Assets/MagicLand/Generated";
        const string DefaultMeshFolder = "Meshes";
        const string MeshFolder = GeneratedFolder + "/" + DefaultMeshFolder;
        internal const string AudioFolder = GeneratedFolder + "/Audio";
        const string PackageId = "com.magicland.app";

        [MenuItem("MagicLand/Setup Project and Scene")]
        public static void SetupAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("MagicLand", "Stop Play mode first (press the Play button again), then run Setup.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            ConfigureProject();
            BuildScene();
            VolcanoBuilder.Build();
            WorldSelectBuilder.Build();
            Debug.Log("[MagicLand] Setup done. Scenes: " + WorldSelectBuilder.ScenePath + ", " + ScenePath + ", " + VolcanoBuilder.ScenePath);
        }

        public static void ConfigureProject()
        {
            EnableOpenXRFeatures();

            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageId);
            if ((int)PlayerSettings.Android.minSdkVersion < 32)
                PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)32;

            ConfigureMobileRenderPipeline();
            AssetDatabase.SaveAssets();
        }

        static void EnableOpenXRFeatures()
        {
            FeatureHelpers.RefreshFeatures(BuildTargetGroup.Android);
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (settings == null)
            {
                Debug.LogWarning("[MagicLand] OpenXR settings for Android not found. Enable OpenXR in XR Plug-in Management first.");
                return;
            }

            string[] wanted = { "MetaQuestFeature", "HandTracking", "MetaHandTrackingAim" };
            foreach (var feature in settings.GetFeatures())
            {
                if (wanted.Contains(feature.GetType().Name) && !feature.enabled)
                {
                    feature.enabled = true;
                    EditorUtility.SetDirty(feature);
                    Debug.Log("[MagicLand] Enabled OpenXR feature: " + feature.GetType().Name);
                }
            }
            EditorUtility.SetDirty(settings);
        }

        static void ConfigureMobileRenderPipeline()
        {
            // Quest renders through the "Mobile" quality level: favor sharp edges and stable frame rate.
            var names = QualitySettings.names;
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i] != "Mobile")
                    continue;
                if (QualitySettings.GetRenderPipelineAssetAt(i) is UniversalRenderPipelineAsset urp)
                {
                    urp.supportsHDR = false;
                    urp.msaaSampleCount = 4;
                    urp.renderScale = 1f;
                    EditorUtility.SetDirty(urp);
                }
            }
        }

        // ---------- Materials ----------

        internal static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
                AssetDatabase.CreateFolder(parent, name);
        }

        internal static Material CreateMaterial(string name, string shaderName, System.Action<Material> init)
        {
            EnsureFolder("Assets/MagicLand", "Generated");

            string path = $"{GeneratedFolder}/{name}.mat";
            var shader = Shader.Find(shaderName);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            init(mat);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static string s_MeshFolder = DefaultMeshFolder;

        /// <summary>Sends the meshes saved next to another subfolder of Generated (null restores the default one).</summary>
        internal static void UseMeshFolder(string folderName) =>
            s_MeshFolder = string.IsNullOrEmpty(folderName) ? DefaultMeshFolder : folderName;

        /// <summary>Saves a generated mesh as an asset (overwriting any previous version) and returns it.</summary>
        public static Mesh SaveMesh(Mesh mesh, string name)
        {
            EnsureFolder(GeneratedFolder, s_MeshFolder);
            string path = $"{GeneratedFolder}/{s_MeshFolder}/{name}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        internal static Material GlowMaterial(string name, Color color, float intensity, bool sphere)
        {
            return CreateMaterial(name, "MagicLand/Glow", m =>
            {
                m.SetColor("_Color", color);
                m.SetFloat("_Intensity", intensity);
                m.SetFloat("_SphereMode", sphere ? 1f : 0f);
                if (sphere) m.EnableKeyword("_SPHERE_MODE");
                else m.DisableKeyword("_SPHERE_MODE");
            });
        }

        static Material NatureMaterial(string name, bool twoSided)
        {
            return CreateMaterial(name, "MagicLand/Nature", m =>
            {
                m.SetColor("_FogColor", DuskHorizon);
                m.SetFloat("_FogStart", 12f);
                m.SetFloat("_FogEnd", 58f);
                m.SetFloat("_FogMax", 0.9f);
                m.SetColor("_Ambient", new Color(0.45f, 0.43f, 0.62f));
                m.SetColor("_LightColor", new Color(1f, 0.84f, 0.68f));
                m.SetFloat("_Cull", twoSided ? (float)CullMode.Off : (float)CullMode.Back);
            });
        }

        internal static Material ButterflyMaterial(string name, Texture2D wing, Color color, Color spots)
        {
            return CreateMaterial(name, "MagicLand/Butterfly", m =>
            {
                m.SetTexture("_MainTex", wing);
                m.SetColor("_Color", color);
                m.SetColor("_SpotColor", spots);
                m.SetFloat("_Glow", 1.25f);
            });
        }

        static Material WaterMaterial(string name, Color color, float opacity, float scaleU, float scaleV, float speed,
            float foamStart, float sideFade, float endFade, float centerFoam = 0f)
        {
            return CreateMaterial(name, "MagicLand/Water", m =>
            {
                m.SetColor("_Color", color);
                m.SetFloat("_Opacity", opacity);
                m.SetFloat("_ScaleU", scaleU);
                m.SetFloat("_ScaleV", scaleV);
                m.SetFloat("_Speed", speed);
                m.SetFloat("_FoamStart", foamStart);
                m.SetFloat("_SideFade", sideFade);
                m.SetFloat("_EndFade", endFade);
                m.SetFloat("_CenterFoam", centerFoam);
                m.SetColor("_FogColor", DuskHorizon);
            });
        }

        static Material RainbowMaterial(string name, float opacity, bool reverse)
        {
            return CreateMaterial(name, "MagicLand/Rainbow", m =>
            {
                m.SetFloat("_Opacity", opacity);
                m.SetFloat("_Reverse", reverse ? 1f : 0f);
            });
        }

        // ---------- Rainbow ----------

        const float RainbowDelay = 150f;
        const float RainbowFade = 30f;

        static void CreateRainbow(Transform parent, Material primary, Material secondary)
        {
            // Far in front of the viewer, its center below the horizon so only the arc shows.
            var center = new Vector3(0f, -10f, 62f);
            AddRainbowArc(parent, "Rainbow", center, 34f, 3.4f, primary);
            AddRainbowArc(parent, "Rainbow Secondary", center, 41f, 4.2f, secondary);
        }

        static void AddRainbowArc(Transform parent, string name, Vector3 center, float radius, float width, Material mat)
        {
            const int segments = 72;
            var vertices = new System.Collections.Generic.List<Vector3>();
            var uvs = new System.Collections.Generic.List<Vector2>();
            var tris = new System.Collections.Generic.List<int>();
            for (int i = 0; i <= segments; i++)
            {
                float u = i / (float)segments;
                float a = u * Mathf.PI;
                var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                vertices.Add(dir * (radius - width * 0.5f));
                vertices.Add(dir * (radius + width * 0.5f));
                uvs.Add(new Vector2(u, 0f));
                uvs.Add(new Vector2(u, 1f));
                if (i < segments)
                {
                    int k = i * 2;
                    tris.AddRange(new[] { k, k + 1, k + 2, k + 1, k + 3, k + 2 });
                }
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.AddComponent<MeshFilter>().sharedMesh = SaveMesh(mesh, name);
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            go.AddComponent<RainbowReveal>().Configure(RainbowDelay, RainbowFade);
        }

        // ---------- Shooting stars ----------

        const float ShootingStarDelay = 30f;

        internal static void CreateShootingStars(Transform parent, Material mat)
        {
            var mesh = SaveMesh(MeteorMesh(7f, 0.35f, 1.1f), "Shooting Star");
            var root = new GameObject("Shooting Stars");
            root.transform.SetParent(parent, false);
            var renderers = new Renderer[3];
            for (int i = 0; i < renderers.Length; i++)
            {
                var go = new GameObject("Meteor " + i);
                go.transform.SetParent(root.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = mat;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
                r.enabled = false;
                renderers[i] = r;
            }
            root.AddComponent<ShootingStars>().Configure(renderers, ShootingStarDelay);
        }

        /// <summary>
        /// Streak in the XY plane: glowing head at the origin, tail fading along -X.
        /// UVs suit the Glow shader radial falloff (bright at uv 0.5, dark at the edges). Double-sided.
        /// </summary>
        static Mesh MeteorMesh(float length, float width, float headSize)
        {
            float w = width * 0.5f, h = headSize * 0.5f;
            var v = new[]
            {
                new Vector3(-length, 0f, 0f), new Vector3(0f, w, 0f), new Vector3(0f, 0f, 0f), new Vector3(0f, -w, 0f),
                new Vector3(-h, -h, 0f), new Vector3(-h, h, 0f), new Vector3(h, h, 0f), new Vector3(h, -h, 0f),
            };
            var uv = new[]
            {
                new Vector2(0f, 0.5f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f),
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f),
            };
            var colors = new[]
            {
                new Color(0.7f, 0.8f, 1f, 1f), Color.white, Color.white, Color.white,
                Color.white, Color.white, Color.white, Color.white,
            };
            int[] front = { 0, 1, 2, 0, 2, 3, 4, 5, 6, 4, 6, 7 };
            var tris = new int[front.Length * 2];
            for (int i = 0; i < front.Length; i += 3)
            {
                tris[i] = front[i]; tris[i + 1] = front[i + 1]; tris[i + 2] = front[i + 2];
                tris[front.Length + i] = front[i]; tris[front.Length + i + 1] = front[i + 2]; tris[front.Length + i + 2] = front[i + 1];
            }
            var mesh = new Mesh { name = "Shooting Star", vertices = v, uv = uv, colors = colors, triangles = tris };
            mesh.RecalculateNormals();
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * (length * 2f));
            return mesh;
        }

        // ---------- Scene ----------

        internal static readonly Color DuskHorizon = new Color(0.93f, 0.68f, 0.62f);

        public static void BuildScene()
        {
            EnsureFolder("Assets/MagicLand", "Generated");
            UseMeshFolder(null);
            AssetDatabase.DeleteAsset(MeshFolder);
            EnsureFolder(GeneratedFolder, DefaultMeshFolder);
            EnsureFolder(GeneratedFolder, "Audio");
            DeerBuilder.Reset();

            var skyMat = CreateMaterial("Sky", "MagicLand/Sky", m =>
            {
                m.SetColor("_TopColor", new Color(0.18f, 0.24f, 0.52f));
                m.SetColor("_MidColor", DuskHorizon);
                m.SetColor("_BottomColor", new Color(0.55f, 0.5f, 0.55f));
                m.SetColor("_AuroraColor", new Color(0.35f, 0.22f, 0.3f));
                m.SetFloat("_StarBrightness", 0.5f);
            });
            var sparkMat = GlowMaterial("Sparkle", Color.white, 1.6f, false);
            var fingerMat = GlowMaterial("Fingertip", new Color(0.7f, 0.9f, 1f), 2f, false);
            var moonMat = GlowMaterial("Moon", new Color(1f, 0.95f, 0.85f), 0.9f, true);
            var wispMat = GlowMaterial("Wisp", new Color(0.55f, 0.9f, 1f), 1.3f, true);
            var mushroomGlowMat = GlowMaterial("MushroomGlow", new Color(0.45f, 0.75f, 1f), 1.2f, true);
            var natureMat = NatureMaterial("Nature", false);
            var natureTwoSidedMat = NatureMaterial("NatureTwoSided", true);

            var wingTex = ButterflyBuilder.CreateWingTexture(GeneratedFolder);
            Material[] wingMats =
            {
                ButterflyMaterial("ButterflyBlue", wingTex, new Color(0.25f, 0.6f, 1f), Color.white),
                ButterflyMaterial("ButterflyOrange", wingTex, new Color(1f, 0.55f, 0.15f), new Color(1f, 0.95f, 0.8f)),
                ButterflyMaterial("ButterflyYellow", wingTex, new Color(1f, 0.88f, 0.3f), new Color(0.4f, 0.3f, 0.2f)),
                ButterflyMaterial("ButterflyPink", wingTex, new Color(1f, 0.5f, 0.78f), Color.white),
                ButterflyMaterial("ButterflyLilac", wingTex, new Color(0.7f, 0.55f, 1f), new Color(0.9f, 0.95f, 1f)),
            };

            var waterMats = new WaterfallBuilder.Materials
            {
                rock = natureMat,
                fall = WaterMaterial("WaterFall", new Color(0.6f, 0.85f, 0.97f), 0.8f, 6f, 3f, 1.6f, 0.75f, 0.18f, 0.12f),
                veil = WaterMaterial("WaterVeil", new Color(0.8f, 0.93f, 1f), 0.45f, 8f, 4f, 2.2f, 0.7f, 0.25f, 0.15f),
                pool = WaterMaterial("WaterPool", new Color(0.3f, 0.55f, 0.68f), 0.85f, 24f, 4f, 0.25f, 1f, 0f, 0.001f, 0.35f),
                mist = GlowMaterial("Mist", new Color(0.85f, 0.95f, 1f), 0.8f, false),
            };
            var pondWaterMat = WaterMaterial("PondWater", new Color(0.36f, 0.64f, 0.67f), 0.88f, 10f, 3f, 0.18f, 1f, 0f, 0.001f);
            var meteorMat = GlowMaterial("Shooting Star", new Color(0.85f, 0.92f, 1f), 2.5f, false);
            var rainbowMat = RainbowMaterial("Rainbow", 0.5f, false);
            var rainbowOuterMat = RainbowMaterial("RainbowSecondary", 0.18f, true);
            var cloudMat = CreateMaterial("Cloud", "MagicLand/Nature", m =>
            {
                m.SetColor("_FogColor", DuskHorizon);
                m.SetFloat("_FogStart", 30f);
                m.SetFloat("_FogEnd", 100f);
                m.SetFloat("_FogMax", 0.35f);
                m.SetColor("_Ambient", new Color(0.75f, 0.7f, 0.85f));
                m.SetColor("_LightColor", new Color(0.45f, 0.38f, 0.32f));
                m.SetFloat("_WindAmount", 0f);
                m.SetFloat("_Cull", (float)CullMode.Back);
            });
            var petalMat = CreateMaterial("Petal", "MagicLand/Petal", m =>
            {
                m.SetColor("_Color", Color.white);
                m.SetColor("_FogColor", DuskHorizon);
            });

            var windClip = AudioSynth.Wind(AudioFolder);
            var birdClips = AudioSynth.Birds(AudioFolder);
            var waterfallClip = AudioSynth.Waterfall(AudioFolder);
            var frogCall = AudioSynth.FrogCall(AudioFolder);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.black;

            var camGo = CreateRig(DuskHorizon, fingerMat);

            // Everything visible lives under the content root, recentered on the user's gaze.
            // The viewer sits in the grass at the edge of a clearing, looking along +Z.
            var content = new GameObject("Content Root");
            var recenter = content.AddComponent<ViewRecenter>();
            recenter.SetHead(camGo.transform);
            var nature = content.AddComponent<NatureEnvironment>();

            var sky = CreateMeshObject("Sky", PrimitiveType.Sphere, content.transform, Vector3.zero, 150f, skyMat);
            sky.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            CreateMeshObject("Moon", PrimitiveType.Sphere, content.transform, new Vector3(-12f, 14f, 45f), 3.5f, moonMat);
            CreateClouds(content.transform, cloudMat);

            var forest = ForestBuilder.Build(content.transform, natureMat, natureTwoSidedMat);
            foreach (var cap in forest.blueMushrooms)
                CreateMeshObject("Mushroom Glow", PrimitiveType.Sphere, content.transform, cap + Vector3.up * 0.09f, 0.05f, mushroomGlowMat);

            WaterfallBuilder.Create(content.transform, waterMats, waterfallClip);
            PondBuilder.Create(content.transform, pondWaterMat, natureMat, frogCall);
            CreateRainbow(content.transform, rainbowMat, rainbowOuterMat);
            UnicornBuilder.Create(content.transform, natureMat, RainbowDelay + RainbowFade);
            SnowFriendBuilder.Create(content.transform, natureMat);

            var herd = new GameObject("Deer");
            herd.transform.SetParent(content.transform, false);
            var doeA = DeerBuilder.Create(herd.transform, "Doe A", new Vector3(-2.5f, 0f, 9f), 120f, 1f, false,
                new Vector2(-5f, 0.5f), new Vector2(6f, 13f), 1, null, natureMat);
            DeerBuilder.Create(herd.transform, "Doe B", new Vector3(3f, 0f, 11f), -100f, 1.05f, false,
                new Vector2(-0.5f, 5f), new Vector2(6f, 13f), 2, null, natureMat);
            DeerBuilder.Create(herd.transform, "Fawn", new Vector3(-1.3f, 0f, 8.2f), 200f, 0.62f, true,
                new Vector2(-5f, 5f), new Vector2(6f, 13f), 3, doeA, natureMat);

            var mouse = MouseBuilder.Create(content.transform, new Vector3(-0.6f, 0f, 2.4f), 70f, 1.4f,
                ForestGround.MouseAreaX, ForestGround.MouseAreaZ, 5, natureMat);
            CreateShootingStars(content.transform, meteorMat);

            ButterflyBuilder.Create(content.transform, natureMat, wingMats);

            BirdBuilder.CreateTreeBirds(content.transform, natureMat, birdClips);
            BirdBuilder.CreateFlock(content.transform, natureMat, 20f);
            var rabbits = new GameObject("Rabbits");
            rabbits.transform.SetParent(content.transform, false);
            RabbitBuilder.Create(rabbits.transform, "Rabbit", new Color(0.66f, 0.55f, 0.45f), 1.1f, 8f, 0f, 41, natureMat);
            RabbitBuilder.Create(rabbits.transform, "Bunny", new Color(0.78f, 0.68f, 0.58f), 0.65f, 9f, 0.35f, 41, natureMat);
            CreatePetals(content.transform, petalMat);

            var visitingMouse = MouseBuilder.CreateVisitor(content.transform, new Vector3(2.9f, 0f, 2.4f), 1.35f,
                ForestGround.MouseAreaX, ForestGround.MouseAreaZ, natureMat);
            var heart = MouseBuilder.CreateHeart(content.transform, natureMat);
            heart.localScale = Vector3.one * 0.75f;
            var kisses = new GameObject("Mouse Kiss Director");
            kisses.transform.SetParent(content.transform, false);
            kisses.AddComponent<MouseKissDirector>().Configure(mouse, visitingMouse, heart,
                content.GetComponentsInChildren<RabbitAgent>(true), content.GetComponentInChildren<SnowFriendAgent>(true),
                content.GetComponentsInChildren<UnicornAgent>(true), content.GetComponentsInChildren<RainbowReveal>(true));

            CreateFireflies(content.transform, sparkMat);
            var random = new System.Random(7);
            for (int i = 0; i < 6; i++)
            {
                var pos = new Vector3(-4f + i * 1.6f + (float)random.NextDouble(), (float)random.NextDouble() * 1.2f - 0.4f, 4f + (float)random.NextDouble() * 6f);
                var wisp = CreateMeshObject("Wisp " + i, PrimitiveType.Sphere, content.transform, pos, 0.07f + (float)random.NextDouble() * 0.05f, wispMat);
                wisp.AddComponent<FloatingMotion>().Configure(new Vector3(0.6f, 0.3f, 0.5f), 0.1f + (float)random.NextDouble() * 0.08f);
            }

            // Ambient sounds: looping wind (volume follows the gusts) and distant birds.
            var windGo = new GameObject("Wind Sound");
            windGo.transform.SetParent(content.transform, false);
            var windSource = windGo.AddComponent<AudioSource>();
            windSource.clip = windClip;
            windSource.loop = true;
            windSource.playOnAwake = true;
            windSource.spatialBlend = 0f;
            windSource.volume = 0.45f;
            nature.Configure(windSource);

            var birds = new GameObject("Birds");
            birds.transform.SetParent(content.transform, false);
            birds.AddComponent<BirdSongs>().Configure(birdClips);

            CreateReturnToMenu();

            WorldSelectBuilder.CaptureCard(content.transform, SceneName);
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterScenes();
            AssetDatabase.SaveAssets();
        }

        /// <summary>The world selection opens first, then the worlds.</summary>
        internal static void RegisterScenes()
        {
            string[] ours = { WorldSelectBuilder.ScenePath, ScenePath, VolcanoBuilder.ScenePath };
            var scenes = ours.Where(p => System.IO.File.Exists(p)).Select(p => new EditorBuildSettingsScene(p, true)).ToList();
            scenes.AddRange(EditorBuildSettings.scenes.Where(s => !ours.Contains(s.path))
                .Select(s => new EditorBuildSettingsScene(s.path, false)));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        /// <summary>Open hand in front of the face: back to the world selection.</summary>
        internal static void CreateReturnToMenu()
        {
            var palmGlow = CreateMeshObject("Palm Glow", PrimitiveType.Sphere, null, Vector3.zero, 0.07f,
                GlowMaterial("PalmGlow", new Color(1f, 0.85f, 0.55f), 1.6f, true));
            var back = new GameObject("Return To Menu");
            back.AddComponent<ReturnToMenu>().Configure(WorldSelectBuilder.SceneName, palmGlow.transform);
            palmGlow.transform.SetParent(back.transform, false);
        }

        /// <summary>
        /// XR rig shared by every scene: head-tracked camera with a fade cover, and tracked hands
        /// with glowing fingertips. Returns the camera.
        /// </summary>
        internal static GameObject CreateRig(Color background, Material fingerMat)
        {
            var origin = new GameObject("XR Origin");
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(origin.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 200f;
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = false;
            camData.renderShadows = false;
            camGo.AddComponent<AudioListener>();

            var tpd = camGo.AddComponent<TrackedPoseDriver>();
            tpd.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            tpd.positionInput = new InputActionProperty(new InputAction("Head Position", InputActionType.Value, "<XRHMD>/centerEyePosition", expectedControlType: "Vector3"));
            tpd.rotationInput = new InputActionProperty(new InputAction("Head Rotation", InputActionType.Value, "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion"));
            tpd.trackingStateInput = new InputActionProperty(new InputAction("Head Tracking State", InputActionType.Value, "<XRHMD>/trackingState", expectedControlType: "Integer"));

            var fadeMat = CreateMaterial("Fade", "MagicLand/Fade", m => m.SetColor("_Color", Color.black));
            var cover = CreateMeshObject("Fade Cover", PrimitiveType.Sphere, camGo.transform, Vector3.zero, 0.2f, fadeMat);
            camGo.AddComponent<ScreenFade>().Configure(cover.GetComponent<MeshRenderer>());

            var hands = new GameObject("Hands");
            hands.AddComponent<HandPinchTracker>().Configure(origin.transform, fingerMat);
            return camGo;
        }

        internal static GameObject CreateMeshObject(string name, PrimitiveType type, Transform parent, Vector3 localPos, float scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = Vector3.one * scale;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go;
        }

        static void CreateClouds(Transform parent, Material mat)
        {
            var root = new GameObject("Clouds");
            root.transform.SetParent(parent, false);
            var rnd = new System.Random(19);
            float R(float min, float max) => min + (float)rnd.NextDouble() * (max - min);
            var top = new Color(1f, 0.97f, 0.97f);
            var bottom = new Color(0.95f, 0.72f, 0.8f);
            var lavender = new Color(0.8f, 0.74f, 0.95f);

            const int count = 6;
            var items = new Transform[count];
            var speeds = new float[count];
            for (int i = 0; i < count; i++)
            {
                var kit = new MeshKit();
                int puffs = rnd.Next(4, 7);
                Color under = i % 2 == 0 ? bottom : lavender;
                for (int k = 0; k < puffs; k++)
                {
                    float x = (k - (puffs - 1) * 0.5f) * R(1.6f, 2.2f);
                    float radius = R(1.6f, 2.6f) * (1f - Mathf.Abs(x) * 0.06f);
                    var center = new Vector3(x, R(-0.2f, 0.8f), R(-0.8f, 0.8f));
                    kit.AddEllipsoid(center, new Vector3(radius, radius * 0.7f, radius * 0.85f), Quaternion.Euler(0f, R(0f, 360f), 0f), 1,
                        p => Color.Lerp(under, top, Mathf.Clamp01((p.y + 0.6f) / 2f)), 0.12f, rnd.Next());
                }
                var mesh = SaveMesh(kit.ToMesh("Cloud " + i), "Cloud " + i);
                var go = new GameObject("Cloud " + i);
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = new Vector3(-50f + i * (100f / count) + R(0f, 8f), R(30f, 40f), R(40f, 55f));
                go.transform.localScale = Vector3.one * R(1f, 1.5f);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = mat;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
                items[i] = go.transform;
                speeds[i] = R(0.25f, 0.4f);
            }
            root.AddComponent<CloudDrift>().Configure(items, speeds, 50f);
        }

        static void CreatePetals(Transform parent, Material mat)
        {
            var go = new GameObject("Falling Petals");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 3.5f, 6f);
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(14f, 20f);
            main.startSpeed = 0f;
            main.startSize3D = true;
            main.startSizeX = new ParticleSystem.MinMaxCurve(0.06f, 0.09f);
            main.startSizeY = new ParticleSystem.MinMaxCurve(0.08f, 0.12f);
            main.startSizeZ = 1f;
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.72f, 0.84f), new Color(1f, 0.95f, 0.97f));
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 150;

            var emission = ps.emission;
            emission.rateOverTime = 8f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(14f, 1f, 12f);

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
            velocity.y = new ParticleSystem.MinMaxCurve(-0.3f, -0.2f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.3f;
            noise.frequency = 0.25f;
            noise.scrollSpeed = 0.15f;

            var rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.separateAxes = true;
            rotation.x = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);
            rotation.y = new ParticleSystem.MinMaxCurve(-1f, 1f);
            rotation.z = new ParticleSystem.MinMaxCurve(-2f, 2f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(1f, 0.85f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = gradient;

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var quadMesh = quad.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(quad);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = mat;
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = quadMesh;
            renderer.alignment = ParticleSystemRenderSpace.Local;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        internal static void CreateFireflies(Transform parent, Material mat)
        {
            var go = new GameObject("Fireflies");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, -0.3f, 8f);
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.12f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.09f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.45f), new Color(0.55f, 0.95f, 1f));
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 200;

            var emission = ps.emission;
            emission.rateOverTime = 20f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(16f, 2.5f, 16f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.25f;
            noise.frequency = 0.3f;
            noise.scrollSpeed = 0.1f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = mat;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }
}
