using UnityEngine;

namespace MagicLand.EditorTools
{
    /// <summary>Builds a cheerful snow friend with a big round head and the hiding places he visits in turn.</summary>
    public static class SnowFriendBuilder
    {
        static readonly Color Snow = new Color(0.94f, 0.97f, 1f);
        static readonly Color Twig = new Color(0.36f, 0.24f, 0.15f);
        static readonly Color Coal = new Color(0.1f, 0.11f, 0.14f);

        const float HeadHeight = 0.95f;
        static readonly Vector3 EyePivot = new Vector3(0f, 0.36f, 0.19f);
        static readonly Vector3 ArmPivot = new Vector3(0.19f, 0.84f, 0f);

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
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
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

        static Vector3 Ground(Vector3 p) => new Vector3(p.x, ForestGround.HeightAt(p.x, p.z), p.z);

        static Vector3 Flat(Vector2 p) => new Vector3(p.x, 0f, p.y);

        /// <summary>Horizontal direction across the line of sight that points back toward the middle of the clearing.</summary>
        static Vector3 TowardClearing(Vector3 spot)
        {
            Vector3 side = Vector3.Cross(spot.normalized, Vector3.up);
            return side.x * spot.x > 0f ? -side : side;
        }

        // The head is one smooth egg shape: a round crown widening into full cheeks near the bottom.
        static readonly Vector3 HeadCenter = new Vector3(0f, 0.28f, 0f);
        const float HeadRadiusY = 0.29f;

        static float CheekBulge(float dy) => Mathf.Exp(-(dy + 0.35f) * (dy + 0.35f) / 0.16f);

        static Vector3 HeadPoint(Vector3 d)
        {
            float bulge = CheekBulge(d.y);
            return HeadCenter + new Vector3(d.x * (0.24f + 0.055f * bulge), d.y * HeadRadiusY,
                d.z * (0.232f + 0.025f * bulge) + 0.025f * bulge);
        }

        /// <summary>Front surface depth of the head at a given point of the face, in head space.</summary>
        static float HeadFront(float x, float y)
        {
            float dy = Mathf.Clamp((y - HeadCenter.y) / HeadRadiusY, -1f, 1f);
            float bulge = CheekBulge(dy);
            float dx = x / (0.24f + 0.055f * bulge);
            float dz = Mathf.Sqrt(Mathf.Max(0f, 1f - dx * dx - dy * dy));
            return HeadCenter.z + dz * (0.232f + 0.025f * bulge) + 0.025f * bulge;
        }

        static void AddHeadShell(MeshKit kit)
        {
            const int rings = 26, segments = 40;
            Vector3 Dir(int ring, int segment)
            {
                float lat = Mathf.PI * ring / rings, lon = Mathf.PI * 2f * segment / segments;
                return new Vector3(Mathf.Sin(lat) * Mathf.Sin(lon), Mathf.Cos(lat), Mathf.Sin(lat) * Mathf.Cos(lon));
            }
            for (int ring = 0; ring < rings; ring++)
                for (int segment = 0; segment < segments; segment++)
                {
                    Vector3 a = HeadPoint(Dir(ring, segment)), b = HeadPoint(Dir(ring, segment + 1));
                    Vector3 c = HeadPoint(Dir(ring + 1, segment)), d = HeadPoint(Dir(ring + 1, segment + 1));
                    if (ring > 0)
                        kit.AddFace(a, b, c, HeadCenter, Snow);
                    if (ring < rings - 1)
                        kit.AddFace(b, d, c, HeadCenter, Snow);
                }
        }

        public static void Create(Transform parent, Material material)
        {
            var hideouts = CreateHideouts(parent, material);

            var root = new GameObject("Snow Friend");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Ground(hideouts[0].hidden);

            var body = MakeMesh("Snow Friend Body", kit =>
            {
                for (int side = -1; side <= 1; side += 2)
                    kit.AddEllipsoid(new Vector3(side * 0.14f, 0.06f, 0.06f), new Vector3(0.12f, 0.08f, 0.16f),
                        Quaternion.Euler(0f, side * 12f, 0f), 2, _ => Snow);
                kit.AddEllipsoid(new Vector3(0f, 0.36f, 0f), new Vector3(0.34f, 0.31f, 0.32f), Quaternion.identity, 3, _ => Snow);
                kit.AddEllipsoid(new Vector3(0f, 0.79f, 0f), new Vector3(0.21f, 0.2f, 0.2f), Quaternion.identity, 3, _ => Snow);
                kit.AddEllipsoid(new Vector3(0f, 0.85f, 0.185f), Vector3.one * 0.03f, Quaternion.identity, 1, _ => Coal);
                kit.AddEllipsoid(new Vector3(0f, 0.74f, 0.19f), Vector3.one * 0.032f, Quaternion.identity, 1, _ => Coal);
                kit.AddEllipsoid(new Vector3(0f, 0.44f, 0.3f), Vector3.one * 0.036f, Quaternion.identity, 1, _ => Coal);
            });
            Part("Snow Body", root.transform, Vector3.zero, body, material);

            var headMesh = MakeMesh("Snow Friend Head", BuildHead);
            Transform head = Part("Head", root.transform, new Vector3(0f, HeadHeight, 0f), headMesh, material);
            var eyesMesh = MakeMesh("Snow Friend Eyes", BuildEyes);
            Transform eyes = Part("Eyes", head, EyePivot, eyesMesh, material);

            Transform rightArm = Part("Right Arm", root.transform, ArmPivot,
                MakeMesh("Snow Friend Right Arm", kit => BuildArm(kit, 1f)), material);
            rightArm.localRotation = Quaternion.Euler(0f, 0f, -18f);
            Transform leftArm = Part("Left Arm", root.transform, Vector3.Scale(ArmPivot, new Vector3(-1f, 1f, 1f)),
                MakeMesh("Snow Friend Left Arm", kit => BuildArm(kit, -1f)), material);
            leftArm.localRotation = Quaternion.Euler(0f, 0f, 18f);

            root.AddComponent<SnowFriendAgent>().Configure(head, eyes, leftArm, rightArm, hideouts, 35f);
        }

        static void BuildHead(MeshKit kit)
        {
            AddHeadShell(kit);

            // Wide open smile: a dark crescent with a row of teeth and a small tongue.
            Color mouth = new Color(0.2f, 0.08f, 0.1f);
            for (int i = -14; i <= 14; i++)
            {
                float u = i / 14f;
                float x = 0.13f * u;
                float ry = 0.04f * (1f - u * u) + 0.011f;
                float y = 0.16f + 0.05f * u * u - ry * 0.5f;
                kit.AddEllipsoid(new Vector3(x, y, HeadFront(x, y) - 0.006f), new Vector3(0.016f, ry, 0.022f),
                    Quaternion.identity, 2, _ => mouth);
            }
            for (int i = -6; i <= 6; i++)
            {
                float u = i / 10f;
                float x = 0.13f * u;
                float ry = 0.04f * (1f - u * u) + 0.011f;
                float y = 0.16f + 0.05f * u * u + ry * 0.5f - 0.016f;
                kit.AddEllipsoid(new Vector3(x, y, HeadFront(x, y) + 0.004f), new Vector3(0.016f, 0.012f, 0.01f),
                    Quaternion.identity, 1, _ => Color.white);
            }
            kit.AddEllipsoid(new Vector3(0f, 0.105f, HeadFront(0f, 0.105f) + 0.004f), new Vector3(0.045f, 0.02f, 0.012f),
                Quaternion.identity, 2, _ => new Color(0.92f, 0.45f, 0.5f));

            for (int side = -1; side <= 1; side += 2)
            {
                kit.AddEllipsoid(new Vector3(side * 0.205f, 0.205f, HeadFront(side * 0.205f, 0.205f) - 0.012f),
                    new Vector3(0.048f, 0.03f, 0.02f), Quaternion.Euler(0f, side * 35f, 0f), 2, _ => new Color(1f, 0.72f, 0.76f));

                // Expressive arched brows made of thin coal strokes.
                Vector3 inner = new Vector3(side * 0.04f, 0.49f, 0f), middle = new Vector3(side * 0.095f, 0.508f, 0f),
                    outer = new Vector3(side * 0.148f, 0.478f, 0f);
                inner.z = HeadFront(inner.x, inner.y) + 0.004f;
                middle.z = HeadFront(middle.x, middle.y) + 0.004f;
                outer.z = HeadFront(outer.x, outer.y) + 0.004f;
                kit.AddTube(inner, middle, 0.009f, 0.012f, 6, _ => Coal, true, false);
                kit.AddTube(middle, outer, 0.012f, 0.008f, 6, _ => Coal, false, true);
            }

            Vector3 noseBase = new Vector3(0f, 0.27f, HeadFront(0f, 0.27f) - 0.02f);
            Vector3 noseTip = noseBase + new Vector3(0f, 0.035f, 0.22f);
            kit.AddTube(noseBase, noseTip, 0.042f, 0f, 10, p => Color.Lerp(new Color(0.93f, 0.38f, 0.08f),
                new Color(1f, 0.6f, 0.15f), Mathf.Clamp01((p.z - noseBase.z) / 0.22f)), true, false);

            Vector3[] hair =
            {
                new Vector3(0f, 0.55f, -0.02f), new Vector3(0.01f, 0.8f, 0.01f), new Vector3(0.008f, 0.72f, 0f), new Vector3(0.065f, 0.79f, 0f),
                new Vector3(-0.07f, 0.53f, -0.03f), new Vector3(-0.16f, 0.74f, -0.04f), new Vector3(-0.13f, 0.67f, -0.035f), new Vector3(-0.2f, 0.69f, -0.035f),
                new Vector3(0.07f, 0.53f, -0.03f), new Vector3(0.15f, 0.72f, -0.02f), new Vector3(0.12f, 0.65f, -0.025f), new Vector3(0.12f, 0.75f, -0.02f),
            };
            for (int i = 0; i < hair.Length; i += 4)
            {
                kit.AddTube(hair[i], hair[i + 1], 0.016f, 0.007f, 6, _ => Twig, false, true);
                kit.AddTube(hair[i + 2], hair[i + 3], 0.009f, 0.004f, 5, _ => Twig, false, true);
            }
        }

        static void BuildEyes(MeshKit kit)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 0.085f, y = 0.375f;
                float surface = HeadFront(x, y) - 0.018f;
                Vector3 white = new Vector3(x, y, surface) - EyePivot;
                kit.AddEllipsoid(white, new Vector3(0.074f, 0.096f, 0.048f), Quaternion.identity, 2, _ => Color.white);
                Vector3 pupil = new Vector3(side * 0.074f, 0.362f, surface + 0.038f) - EyePivot;
                kit.AddEllipsoid(pupil, new Vector3(0.041f, 0.052f, 0.022f), Quaternion.identity, 2, _ => Coal);
                kit.AddEllipsoid(pupil + new Vector3(-0.013f, 0.018f, 0.02f), Vector3.one * 0.012f, Quaternion.identity, 1,
                    _ => Color.white);
            }
        }

        static void BuildArm(MeshKit kit, float side)
        {
            Vector3 S(float x, float y, float z) => new Vector3(x * side, y, z);
            kit.AddTube(S(-0.02f, 0f, 0f), S(0.24f, 0.05f, 0f), 0.022f, 0.016f, 7, _ => Twig, false, false);
            kit.AddTube(S(0.24f, 0.05f, 0f), S(0.4f, 0.15f, 0.02f), 0.016f, 0.011f, 6, _ => Twig, false, true);
            kit.AddTube(S(0.38f, 0.14f, 0.018f), S(0.49f, 0.16f, 0.035f), 0.01f, 0.004f, 5, _ => Twig, false, true);
            kit.AddTube(S(0.38f, 0.14f, 0.018f), S(0.46f, 0.25f, 0.03f), 0.01f, 0.004f, 5, _ => Twig, false, true);
            kit.AddTube(S(0.39f, 0.145f, 0.018f), S(0.42f, 0.27f, 0.0f), 0.009f, 0.004f, 5, _ => Twig, false, true);
            kit.AddTube(S(0.17f, 0.035f, 0f), S(0.22f, -0.06f, 0.02f), 0.009f, 0.004f, 5, _ => Twig, false, true);
        }

        /// <summary>Three hiding places, each with its own entrance: beside the bird tree, behind a boulder, behind a bush.</summary>
        static SnowFriendAgent.Hideout[] CreateHideouts(Transform parent, Material material)
        {
            var kit = new MeshKit();

            Vector3 tree = Flat(ForestGround.BirdTree);
            Vector3 toViewer = -tree.normalized;
            Vector3 towardClearing = TowardClearing(tree);
            var birdTree = new SnowFriendAgent.Hideout
            {
                entrance = SnowFriendAgent.Entrance.StepAside,
                hidden = tree - toViewer * 0.4f,
                shown = tree + towardClearing * 1.25f + toViewer * 0.55f,
            };

            Vector3 rock = new Vector3(-2.4f, 0f, 4.9f);
            Vector3 rockBase = Ground(rock);
            kit.AddEllipsoid(rockBase, new Vector3(0.8f, 0.55f, 0.65f), Quaternion.Euler(0f, 20f, 0f), 2,
                p => p.y > rockBase.y + 0.3f ? new Color(0.36f, 0.52f, 0.29f) : new Color(0.53f, 0.53f, 0.57f), 0.18f, 3);
            kit.AddEllipsoid(rockBase + new Vector3(0.62f, 0f, 0.15f), new Vector3(0.36f, 0.28f, 0.34f), Quaternion.identity, 1,
                _ => new Color(0.5f, 0.5f, 0.54f), 0.2f, 4);
            Vector3 behindRock = rock + rock.normalized * 0.9f;
            var boulder = new SnowFriendAgent.Hideout
            {
                entrance = SnowFriendAgent.Entrance.PopUp,
                hidden = behindRock,
                shown = behindRock,
            };

            Vector3 blossom = new Vector3(-3.6f, 0f, 14.2f);
            Vector3 blossomToViewer = -blossom.normalized;
            Vector3 blossomSide = TowardClearing(blossom);
            ForestBuilder.AddTree(kit, Ground(blossom), 2, 5f, new System.Random(57), true);
            Vector3 bush = blossom + blossomToViewer * 0.95f;
            var rnd = new System.Random(11);
            for (int k = 0; k < 4; k++)
            {
                float s = 0.5f + 0.06f * k;
                Vector3 c = Ground(bush + blossomSide * (k * 0.42f - 0.65f) + blossomToViewer * (k % 2 == 0 ? 0.1f : -0.15f));
                c.y += s * 0.5f;
                Color col = new Color(0.25f + 0.03f * k, 0.47f + 0.03f * k, 0.22f);
                kit.AddEllipsoid(c, new Vector3(s, s * 0.85f, s), Quaternion.Euler(0f, k * 47f, 0f), 1,
                    p => Color.Lerp(col * 0.7f, col * 1.1f, Mathf.Clamp01((p.y - c.y) / s * 0.5f + 0.5f)), 0.2f, rnd.Next());
            }
            var blossomBush = new SnowFriendAgent.Hideout
            {
                entrance = SnowFriendAgent.Entrance.PeekOverBush,
                hidden = blossom + blossomToViewer * 0.45f,
                shown = bush + blossomSide * 1.75f + blossomToViewer * 0.25f,
            };

            var mesh = MagicLandSetup.SaveMesh(kit.ToMesh("Snow Friend Hideouts"), "Snow Friend Hideouts");
            Part("Snow Friend Hideouts", parent, Vector3.zero, mesh, material);
            return new[] { birdTree, boulder, blossomBush };
        }
    }
}
