using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace MagicLand.EditorTools
{
    /// <summary>Builds the world selection scene: floating picture cards, one per world.</summary>
    public static class WorldSelectBuilder
    {
        public const string ScenePath = "Assets/Scenes/WorldSelect.unity";
        public const string SceneName = "WorldSelect";
        const string CardFolder = MagicLandSetup.GeneratedFolder + "/Cards";
        const int CardWidth = 512, CardHeight = 384;

        static readonly Vector2 CardSize = new Vector2(0.26f, 0.195f);
        static readonly Color NightHorizon = new Color(0.42f, 0.36f, 0.62f);

        struct World
        {
            public string scene;
            public string picture;
            public bool locked;
        }

        static readonly World[] Worlds =
        {
            new World { scene = MagicLandSetup.SceneName, picture = MagicLandSetup.SceneName },
            new World { scene = VolcanoBuilder.SceneName, picture = VolcanoBuilder.SceneName },
        };

        public static void Build()
        {
            var skyMat = MagicLandSetup.CreateMaterial("MenuSky", "MagicLand/Sky", m =>
            {
                m.SetColor("_TopColor", new Color(0.08f, 0.1f, 0.3f));
                m.SetColor("_MidColor", NightHorizon);
                m.SetColor("_BottomColor", new Color(0.2f, 0.17f, 0.32f));
                m.SetColor("_AuroraColor", new Color(0.3f, 0.45f, 0.6f));
                m.SetFloat("_StarBrightness", 1f);
            });
            var sparkMat = MagicLandSetup.GlowMaterial("Sparkle", Color.white, 1.6f, false);
            var fingerMat = MagicLandSetup.GlowMaterial("Fingertip", new Color(0.7f, 0.9f, 1f), 2f, false);
            var wispMat = MagicLandSetup.GlowMaterial("Wisp", new Color(0.55f, 0.9f, 1f), 1.3f, true);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.black;

            var camGo = MagicLandSetup.CreateRig(NightHorizon, fingerMat);
            var content = new GameObject("Content Root");
            content.AddComponent<ViewRecenter>().SetHead(camGo.transform);

            var sky = MagicLandSetup.CreateMeshObject("Sky", PrimitiveType.Sphere, content.transform, Vector3.zero, 150f, skyMat);
            sky.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            MagicLandSetup.CreateFireflies(content.transform, sparkMat);
            var random = new System.Random(3);
            for (int i = 0; i < 8; i++)
            {
                float angle = (i / 8f - 0.5f) * 2.4f;
                var pos = new Vector3(Mathf.Sin(angle) * 2.2f, (float)random.NextDouble() * 1.4f - 0.5f, Mathf.Cos(angle) * 2.2f + 0.5f);
                var wisp = MagicLandSetup.CreateMeshObject("Wisp " + i, PrimitiveType.Sphere, content.transform, pos, 0.05f + (float)random.NextDouble() * 0.04f, wispMat);
                wisp.AddComponent<FloatingMotion>().Configure(new Vector3(0.3f, 0.2f, 0.3f), 0.1f + (float)random.NextDouble() * 0.08f);
            }

            // Cards within arm's reach, slightly below the eyes, turned toward the viewer.
            var cardsRoot = new GameObject("World Cards");
            cardsRoot.transform.SetParent(content.transform, false);
            var cards = new WorldCard[Worlds.Length];
            float spacing = CardSize.x + 0.07f;
            for (int i = 0; i < Worlds.Length; i++)
            {
                var world = Worlds[i];
                float x = (i - (Worlds.Length - 1) * 0.5f) * spacing;
                var position = new Vector3(x, -0.1f, 0.47f);
                var card = new GameObject("Card " + world.scene);
                card.transform.SetParent(cardsRoot.transform, false);
                card.transform.localPosition = position;
                card.transform.localRotation = Quaternion.LookRotation(position.normalized, Vector3.up);

                var mat = MagicLandSetup.CreateMaterial("Card " + world.picture, "MagicLand/Card", m =>
                {
                    m.SetTexture("_MainTex", LoadPicture(world.picture));
                    m.SetFloat("_Aspect", CardSize.x / CardSize.y);
                    m.SetColor("_BorderColor", world.locked ? new Color(0.75f, 0.7f, 0.85f) : new Color(1f, 0.85f, 0.55f));
                });
                var picture = MagicLandSetup.CreateMeshObject("Picture", PrimitiveType.Quad, card.transform, Vector3.zero, 1f, mat);
                picture.transform.localScale = new Vector3(CardSize.x, CardSize.y, 1f);
                cards[i] = card.AddComponent<WorldCard>();
                cards[i].Configure(world.scene, world.locked, picture.GetComponent<MeshRenderer>(), CardSize);
            }
            new GameObject("World Menu").AddComponent<WorldMenu>().Configure(cards);

            EditorSceneManager.SaveScene(scene, ScenePath);
            MagicLandSetup.RegisterScenes();
            AssetDatabase.SaveAssets();
        }

        static Texture2D LoadPicture(string name) =>
            AssetDatabase.LoadAssetAtPath<Texture2D>($"{CardFolder}/{name}.png");

        /// <summary>Takes a picture of a world from the viewer's place, for its card.</summary>
        public static void CaptureCard(Transform content, string name)
        {
            content.GetComponent<NatureEnvironment>()?.Apply(0f);
            var preview = new PreviewRenderUtility();
            try
            {
                preview.BeginStaticPreview(new Rect(0f, 0f, CardWidth, CardHeight));
                Camera camera = preview.camera;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = MagicLandSetup.DuskHorizon;
                camera.fieldOfView = 50f;
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 200f;
                camera.transform.SetPositionAndRotation(content.position, content.rotation);
                camera.transform.LookAt(content.TransformPoint(new Vector3(0f, -0.3f, 8f)));
                foreach (var filter in content.GetComponentsInChildren<MeshFilter>())
                {
                    var renderer = filter.GetComponent<MeshRenderer>();
                    if (filter.sharedMesh == null || renderer == null || !renderer.enabled || renderer.sharedMaterial == null)
                        continue;
                    for (int s = 0; s < filter.sharedMesh.subMeshCount; s++)
                        preview.DrawMesh(filter.sharedMesh, filter.transform.localToWorldMatrix, renderer.sharedMaterial, s);
                }
                preview.Render(true);
                Texture2D image = preview.EndStaticPreview();
                SavePicture(image, name);
                Object.DestroyImmediate(image);
            }
            finally
            {
                preview.Cleanup();
            }
        }

        static void SavePicture(Texture2D image, string name)
        {
            MagicLandSetup.EnsureFolder(MagicLandSetup.GeneratedFolder, "Cards");
            string path = $"{CardFolder}/{name}.png";
            File.WriteAllBytes(path, image.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 4;
            importer.maxTextureSize = CardWidth;
            importer.SaveAndReimport();
        }
    }
}
