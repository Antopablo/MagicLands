using UnityEngine;

namespace MagicLand.EditorTools
{
    /// <summary>Creates a small family of low-poly unicorns with simple walking and flying rigs.</summary>
    public static class UnicornBuilder
    {
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

        public static void Create(Transform parent, Material material, float rainbowReadyAt)
        {
            Color[] coats = { new Color(0.96f, 0.89f, 0.87f), new Color(0.82f, 0.88f, 0.98f), new Color(0.98f, 0.93f, 0.76f) };
            Color[] manes = { new Color(0.82f, 0.48f, 0.78f), new Color(0.48f, 0.78f, 0.86f), new Color(0.9f, 0.61f, 0.48f) };
            float[] sizes = { 1f, 0.78f, 0.58f };
            float[] offsets = { 0f, 0.35f, -0.25f };
            var family = new GameObject("Unicorn Family");
            family.transform.SetParent(parent, false);

            for (int i = 0; i < sizes.Length; i++)
                CreateOne(family.transform, "Unicorn " + (i + 1), sizes[i], offsets[i], i + 1, coats[i], manes[i],
                    material, rainbowReadyAt, i * 2.4f);
        }

        static void CreateOne(Transform parent, string name, float scale, float offset, int seed,
            Color coat, Color mane, Material material, float rainbowReadyAt, float familyDelay)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var start = new Vector3(13.5f, 0f, 13.5f + offset);
            start.y = ForestGround.HeightAt(start.x, start.z);
            root.transform.localPosition = start;
            root.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            root.transform.localScale = Vector3.one * scale;

            Mesh bodyMesh = MakeMesh(name + " Body", kit =>
            {
                kit.AddEllipsoid(Vector3.zero, new Vector3(0.25f, 0.24f, 0.43f), Quaternion.identity, 2,
                    p => p.y < -0.05f ? Color.Lerp(coat, new Color(0.98f, 0.92f, 0.86f), 0.65f) : coat, 0.08f, seed);
                kit.AddEllipsoid(new Vector3(0f, 0.04f, 0.3f), new Vector3(0.19f, 0.2f, 0.2f), Quaternion.identity, 1, _ => coat);
            });
            Part("Body", root.transform, new Vector3(0f, 0.84f, 0f), bodyMesh, material);

            Mesh neckMesh = MakeMesh(name + " Neck", kit =>
            {
                kit.AddEllipsoid(new Vector3(0f, 0.02f, 0f), new Vector3(0.145f, 0.2f, 0.17f), Quaternion.Euler(-22f, 0f, 0f), 1, _ => coat);
                kit.AddEllipsoid(new Vector3(0f, 0.09f, 0.05f), new Vector3(0.16f, 0.17f, 0.18f), Quaternion.identity, 1, _ => coat);
            });
            Part("Neck", root.transform, new Vector3(0f, 1.02f, 0.24f), neckMesh, material);

            Mesh headMesh = MakeMesh(name + " Head", kit =>
            {
                kit.AddEllipsoid(Vector3.zero, new Vector3(0.17f, 0.16f, 0.19f), Quaternion.identity, 2, _ => coat);
                kit.AddEllipsoid(new Vector3(0f, -0.065f, 0.14f), new Vector3(0.12f, 0.08f, 0.12f), Quaternion.identity, 1,
                    p => Color.Lerp(coat, Color.white, 0.55f));
                for (int side = -1; side <= 1; side += 2)
                {
                    kit.AddEllipsoid(new Vector3(side * 0.105f, 0.13f, 0.02f), new Vector3(0.045f, 0.075f, 0.035f),
                        Quaternion.Euler(-12f, 0f, side * 12f), 1, _ => coat);
                    kit.AddEllipsoid(new Vector3(side * 0.112f, 0.0f, 0.14f), new Vector3(0.017f, 0.021f, 0.012f),
                        Quaternion.identity, 1, _ => new Color(0.16f, 0.12f, 0.2f));
                    kit.AddEllipsoid(new Vector3(side * 0.115f, 0.008f, 0.151f), new Vector3(0.005f, 0.006f, 0.004f),
                        Quaternion.identity, 0, _ => Color.white);
                }
                kit.AddTube(new Vector3(0f, 0.14f, 0.02f), new Vector3(0f, 0.37f, 0.0f), 0.045f, 0f, 7,
                    _ => new Color(0.96f, 0.77f, 0.32f), false, false);
            });
            Part("Head", root.transform, new Vector3(0f, 1.31f, 0.39f), headMesh, material);

            Mesh maneMesh = MakeMesh(name + " Mane", kit =>
            {
                for (int i = 0; i < 5; i++)
                {
                    float z = 0.12f - i * 0.16f;
                    kit.AddEllipsoid(new Vector3(0f, 0.02f - i * 0.015f, z),
                        new Vector3(0.11f, 0.15f, 0.15f), Quaternion.Euler(-12f, 0f, 0f), 1,
                        p => Color.Lerp(mane, Color.white, Mathf.Clamp01((p.y + 0.1f) * 0.45f)), 0.06f, seed + i);
                }
            });
            Part("Flowing Mane", root.transform, new Vector3(0f, 1.16f, 0.11f), maneMesh, material);

            Mesh tailMesh = MakeMesh(name + " Tail", kit =>
            {
                kit.AddTube(Vector3.zero, new Vector3(0f, -0.18f, -0.25f), 0.055f, 0.012f, 7, _ => mane, false, false);
                kit.AddEllipsoid(new Vector3(0f, -0.22f, -0.27f), new Vector3(0.075f, 0.12f, 0.09f), Quaternion.Euler(-18f, 0f, 0f), 1, _ => mane);
            });
            Part("Tail", root.transform, new Vector3(0f, 0.94f, -0.41f), tailMesh, material);

            Mesh legMesh = MakeMesh(name + " Leg", kit =>
            {
                kit.AddTube(Vector3.zero, new Vector3(0f, -0.64f, 0.015f), 0.053f, 0.036f, 6, _ => coat, false, false);
                kit.AddEllipsoid(new Vector3(0f, -0.64f, 0.035f), new Vector3(0.055f, 0.05f, 0.075f), Quaternion.identity, 1,
                    _ => new Color(0.46f, 0.35f, 0.43f));
            });
            var legs = new Transform[4];
            Vector3[] hips =
            {
                new Vector3(-0.15f, 0.69f, 0.26f), new Vector3(0.15f, 0.69f, 0.26f),
                new Vector3(-0.15f, 0.69f, -0.28f), new Vector3(0.15f, 0.69f, -0.28f),
            };
            for (int i = 0; i < legs.Length; i++)
                legs[i] = Part("Leg " + i, root.transform, hips[i], legMesh, material);

            Mesh wingMesh = MakeMesh(name + " Wing", kit =>
            {
                kit.AddEllipsoid(new Vector3(0.18f, 0.02f, 0f), new Vector3(0.23f, 0.045f, 0.12f),
                    Quaternion.Euler(0f, 0f, -12f), 1, p => Color.Lerp(mane, Color.white, Mathf.Clamp01(p.x + 0.4f)));
                for (int feather = 0; feather < 3; feather++)
                    kit.AddEllipsoid(new Vector3(0.29f + feather * 0.06f, 0.02f, -0.03f - feather * 0.025f),
                        new Vector3(0.12f, 0.025f, 0.055f), Quaternion.Euler(0f, 0f, -18f), 1, _ => Color.Lerp(mane, Color.white, 0.35f));
            });
            Transform leftWing = Part("Wing L", root.transform, new Vector3(-0.18f, 0.97f, -0.03f), wingMesh, material);
            leftWing.localScale = new Vector3(-1f, 1f, 1f);
            leftWing.localRotation = Quaternion.Euler(0f, -75f, 30f);
            Transform rightWing = Part("Wing R", root.transform, new Vector3(0.18f, 0.97f, -0.03f), wingMesh, material);
            rightWing.localRotation = Quaternion.Euler(0f, 75f, -30f);

            var agent = root.AddComponent<UnicornAgent>();
            agent.Configure(root.transform.Find("Body"), legs, leftWing, rightWing, scale, offset, seed,
                rainbowReadyAt, familyDelay);
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
                renderer.enabled = false;
        }
    }
}
