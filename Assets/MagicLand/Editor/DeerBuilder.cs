using UnityEngine;

namespace MagicLand.EditorTools
{
    /// <summary>
    /// Builds low-poly deer with a pivot hierarchy (body, neck, head, ears, legs) animated by DeerAgent.
    /// </summary>
    public static class DeerBuilder
    {
        static readonly Color Coat = new Color(0.62f, 0.38f, 0.2f);
        static readonly Color CoatDark = new Color(0.46f, 0.27f, 0.14f);
        static readonly Color Cream = new Color(0.93f, 0.85f, 0.72f);
        static readonly Color Hoof = new Color(0.12f, 0.09f, 0.08f);

        const float BodyHeight = 0.8f;
        static readonly Vector3 NeckPivot = new Vector3(0f, 0.06f, 0.36f);
        static readonly Vector3 HeadPivot = new Vector3(0f, 0.6f, 0f);
        const float NeckRest = 28f;
        const float HeadRest = -10f;

        static readonly Vector3[] Hips =
        {
            new Vector3(-0.09f, -0.1f, 0.28f), new Vector3(0.09f, -0.1f, 0.28f),
            new Vector3(-0.09f, -0.08f, -0.3f), new Vector3(0.09f, -0.08f, -0.3f),
        };

        static Mesh s_Body, s_BodySpotted, s_Neck, s_Head, s_Ear, s_FrontLeg, s_BackLeg;

        static Color CoatAt(Vector3 p, float bellyY)
        {
            if (p.y < bellyY)
                return Cream;
            return Color.Lerp(Coat, CoatDark, Mathf.Clamp01((p.y - 0.05f) / 0.15f));
        }

        static void BuildMeshes()
        {
            var body = new MeshKit();
            MeshKit.ColorFn bodyColor = p => CoatAt(p, -0.08f);
            body.AddEllipsoid(Vector3.zero, new Vector3(0.19f, 0.2f, 0.42f), Quaternion.identity, 1, bodyColor);
            body.AddEllipsoid(new Vector3(0f, 0.02f, 0.28f), new Vector3(0.17f, 0.21f, 0.2f), Quaternion.identity, 1, bodyColor);
            body.AddEllipsoid(new Vector3(0f, 0.03f, -0.3f), new Vector3(0.17f, 0.2f, 0.2f), Quaternion.identity, 1, bodyColor);
            body.AddEllipsoid(new Vector3(0f, 0.1f, -0.5f), new Vector3(0.05f, 0.08f, 0.04f), Quaternion.Euler(-30f, 0f, 0f), 0, _ => Cream);
            s_Body = MagicLandSetup.SaveMesh(body.ToMesh("Deer Body"), "Deer Body");

            // Fawn: same body with white spots along the back.
            var spotted = new MeshKit();
            MeshKit.ColorFn spottedColor = p =>
            {
                Color c = CoatAt(p, -0.08f);
                bool back = p.y > 0.07f && Mathf.Abs(p.x) > 0.03f && Mathf.Abs(p.z) < 0.42f;
                return back && MeshKit.Hash01(p, 5) < 0.28f ? Cream : c;
            };
            spotted.AddEllipsoid(Vector3.zero, new Vector3(0.19f, 0.2f, 0.42f), Quaternion.identity, 2, spottedColor);
            spotted.AddEllipsoid(new Vector3(0f, 0.02f, 0.28f), new Vector3(0.17f, 0.21f, 0.2f), Quaternion.identity, 1, bodyColor);
            spotted.AddEllipsoid(new Vector3(0f, 0.03f, -0.3f), new Vector3(0.17f, 0.2f, 0.2f), Quaternion.identity, 1, bodyColor);
            spotted.AddEllipsoid(new Vector3(0f, 0.1f, -0.5f), new Vector3(0.05f, 0.08f, 0.04f), Quaternion.Euler(-30f, 0f, 0f), 0, _ => Cream);
            s_BodySpotted = MagicLandSetup.SaveMesh(spotted.ToMesh("Deer Body Spotted"), "Deer Body Spotted");

            var neck = new MeshKit();
            neck.AddEllipsoid(new Vector3(0f, 0.04f, 0f), new Vector3(0.1f, 0.12f, 0.12f), Quaternion.identity, 1, _ => Coat);
            neck.AddTube(Vector3.zero, HeadPivot, 0.085f, 0.05f, 7,
                p => p.z > 0.03f && p.y < 0.45f ? Color.Lerp(Coat, Cream, 0.55f) : Coat, false, true);
            s_Neck = MagicLandSetup.SaveMesh(neck.ToMesh("Deer Neck"), "Deer Neck");

            var head = new MeshKit();
            head.AddEllipsoid(new Vector3(0f, 0.04f, 0.03f), new Vector3(0.07f, 0.075f, 0.095f), Quaternion.identity, 1, _ => Coat);
            head.AddEllipsoid(new Vector3(0f, 0.0f, 0.13f), new Vector3(0.042f, 0.045f, 0.09f), Quaternion.Euler(12f, 0f, 0f), 1,
                p => p.y < -0.015f ? Cream : Coat);
            head.AddEllipsoid(new Vector3(0f, 0.005f, 0.215f), new Vector3(0.022f, 0.018f, 0.014f), Quaternion.identity, 0, _ => Hoof);
            for (int side = -1; side <= 1; side += 2)
            {
                head.AddEllipsoid(new Vector3(side * 0.058f, 0.06f, 0.075f), new Vector3(0.016f, 0.018f, 0.016f), Quaternion.identity, 0, _ => Hoof);
                head.AddEllipsoid(new Vector3(side * 0.054f, 0.075f, 0.07f), new Vector3(0.012f, 0.004f, 0.012f), Quaternion.identity, 0, _ => Cream);
            }
            s_Head = MagicLandSetup.SaveMesh(head.ToMesh("Deer Head"), "Deer Head");

            var ear = new MeshKit();
            ear.AddEllipsoid(new Vector3(0f, 0.065f, 0f), new Vector3(0.038f, 0.07f, 0.014f), Quaternion.identity, 1,
                p => p.z > 0.004f ? Color.Lerp(Cream, new Color(0.9f, 0.62f, 0.55f), 0.4f) : Coat);
            s_Ear = MagicLandSetup.SaveMesh(ear.ToMesh("Deer Ear"), "Deer Ear");

            s_FrontLeg = MagicLandSetup.SaveMesh(Leg(0.7f, false).ToMesh("Deer Front Leg"), "Deer Front Leg");
            s_BackLeg = MagicLandSetup.SaveMesh(Leg(0.72f, true).ToMesh("Deer Back Leg"), "Deer Back Leg");
        }

        static MeshKit Leg(float length, bool back)
        {
            var kit = new MeshKit();
            if (back)
                kit.AddEllipsoid(new Vector3(0f, -0.08f, 0.02f), new Vector3(0.07f, 0.16f, 0.11f), Quaternion.identity, 1, _ => Coat);
            else
                kit.AddEllipsoid(new Vector3(0f, -0.06f, 0f), new Vector3(0.06f, 0.12f, 0.08f), Quaternion.identity, 1, _ => Coat);

            var knee = new Vector3(0f, -length * 0.48f, back ? -0.03f : 0.01f);
            var ankle = new Vector3(0f, -length + 0.05f, 0f);
            kit.AddTube(new Vector3(0f, -0.05f, 0f), knee, 0.04f, 0.026f, 6, _ => Coat, false, false);
            kit.AddEllipsoid(knee, Vector3.one * 0.027f, Quaternion.identity, 0, _ => Coat);
            kit.AddTube(knee, ankle, 0.024f, 0.018f, 6, _ => Coat, false, false);
            kit.AddTube(ankle, new Vector3(0f, -length, 0.01f), 0.02f, 0.024f, 6, _ => Hoof, false, true);
            return kit;
        }

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

        public static DeerAgent Create(Transform parent, string name, Vector3 position, float yaw, float scale, bool fawn,
            Vector2 areaX, Vector2 areaZ, int seed, DeerAgent follow, Material mat)
        {
            if (s_Body == null)
                BuildMeshes();

            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(position.x, ForestGround.HeightAt(position.x, position.z), position.z);
            root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            root.transform.localScale = Vector3.one * scale;

            var body = Part("Body", root.transform, new Vector3(0f, BodyHeight, 0f), Quaternion.identity, fawn ? s_BodySpotted : s_Body, mat);
            var neck = Part("Neck", body, NeckPivot, Quaternion.Euler(NeckRest, 0f, 0f), s_Neck, mat);
            var head = Part("Head", neck, HeadPivot, Quaternion.Euler(HeadRest, 0f, 0f), s_Head, mat);
            float earScale = fawn ? 1.25f : 1f;
            var earL = Part("Ear L", head, new Vector3(-0.045f, 0.09f, 0.02f), Quaternion.Euler(-20f, 0f, 40f), s_Ear, mat);
            var earR = Part("Ear R", head, new Vector3(0.045f, 0.09f, 0.02f), Quaternion.Euler(-20f, 0f, -40f), s_Ear, mat);
            earL.localScale = earR.localScale = Vector3.one * earScale;

            var legs = new Transform[4];
            string[] legNames = { "Leg FL", "Leg FR", "Leg BL", "Leg BR" };
            for (int i = 0; i < 4; i++)
                legs[i] = Part(legNames[i], body, Hips[i], Quaternion.identity, i < 2 ? s_FrontLeg : s_BackLeg, mat);

            var agent = root.AddComponent<DeerAgent>();
            agent.Configure(body, neck, head, earL, earR, legs, areaX, areaZ, seed, follow);
            return agent;
        }

        /// <summary>Forces mesh regeneration on the next Create call (new setup run).</summary>
        public static void Reset() => s_Body = null;
    }
}
