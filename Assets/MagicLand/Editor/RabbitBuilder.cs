using UnityEngine;

namespace MagicLand.EditorTools
{
    /// <summary>Builds a soft storybook rabbit (long ears, cotton tail) animated by RabbitAgent.</summary>
    public static class RabbitBuilder
    {
        static readonly Color Eye = new Color(0.04f, 0.03f, 0.04f);
        static readonly Color Pink = new Color(1f, 0.68f, 0.74f);
        static readonly Color Cream = new Color(0.97f, 0.93f, 0.87f);

        // Pivots in root space (root at the feet, z forward).
        static readonly Vector3 BodyPivot = new Vector3(0f, 0.07f, -0.07f);
        static readonly Vector3 HeadPivot = new Vector3(0f, 0.17f, 0.1f);
        static readonly Vector3 TailPivot = new Vector3(0f, 0.11f, -0.16f);

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

        public static RabbitAgent Create(Transform parent, string name, Color fur, float scale, float delay, float laneOffset, int seed, Material mat)
        {
            Color furDark = fur * 0.85f;
            Color FurAt(Vector3 rootPos) => rootPos.y < 0.085f ? Cream : Color.Lerp(fur, furDark, Mathf.Clamp01((rootPos.y - 0.12f) / 0.08f));

            // Body, relative to the hip pivot so it can sit up.
            var bodyKit = new MeshKit();
            Vector3 o = -BodyPivot;
            MeshKit.ColorFn bodyColor = p => FurAt(p + BodyPivot);
            bodyKit.AddEllipsoid(o + new Vector3(0f, 0.11f, -0.04f), new Vector3(0.075f, 0.07f, 0.1f), Quaternion.Euler(-10f, 0f, 0f), 2, bodyColor);
            bodyKit.AddEllipsoid(o + new Vector3(0f, 0.12f, 0.05f), new Vector3(0.058f, 0.065f, 0.06f), Quaternion.identity, 1, bodyColor);
            for (int s = -1; s <= 1; s += 2)
                bodyKit.AddEllipsoid(o + new Vector3(s * 0.05f, 0.085f, -0.08f), new Vector3(0.035f, 0.055f, 0.065f), Quaternion.identity, 1, bodyColor);
            var bodyMesh = MagicLandSetup.SaveMesh(bodyKit.ToMesh(name + " Body"), name + " Body");

            var headKit = new MeshKit();
            headKit.AddEllipsoid(new Vector3(0f, 0.03f, 0.035f), new Vector3(0.048f, 0.046f, 0.058f), Quaternion.Euler(12f, 0f, 0f), 2,
                p => p.y < 0.005f ? Cream : fur);
            for (int s = -1; s <= 1; s += 2)
            {
                headKit.AddEllipsoid(new Vector3(s * 0.014f, 0.012f, 0.082f), new Vector3(0.019f, 0.017f, 0.018f), Quaternion.identity, 1, _ => Cream);
                headKit.AddEllipsoid(new Vector3(s * 0.032f, 0.042f, 0.06f), new Vector3(0.011f, 0.013f, 0.009f), Quaternion.identity, 1, _ => Eye);
                headKit.AddEllipsoid(new Vector3(s * 0.034f, 0.047f, 0.066f), new Vector3(0.003f, 0.003f, 0.002f), Quaternion.identity, 0, _ => Color.white);
            }
            headKit.AddEllipsoid(new Vector3(0f, 0.024f, 0.097f), new Vector3(0.01f, 0.007f, 0.006f), Quaternion.identity, 1, _ => Pink);
            var headMesh = MagicLandSetup.SaveMesh(headKit.ToMesh(name + " Head"), name + " Head");

            var earKit = new MeshKit();
            earKit.AddEllipsoid(new Vector3(0f, 0.065f, 0f), new Vector3(0.019f, 0.068f, 0.009f), Quaternion.identity, 1,
                p => p.z > 0.004f && p.y > 0.015f ? Pink : fur);
            var earMesh = MagicLandSetup.SaveMesh(earKit.ToMesh(name + " Ear"), name + " Ear");

            var tailKit = new MeshKit();
            tailKit.AddEllipsoid(new Vector3(0f, 0f, -0.012f), new Vector3(0.03f, 0.03f, 0.028f), Quaternion.identity, 1, _ => Color.white, 0.15f, seed);
            var tailMesh = MagicLandSetup.SaveMesh(tailKit.ToMesh(name + " Tail"), name + " Tail");

            var frontKit = new MeshKit();
            frontKit.AddTube(Vector3.zero, new Vector3(0f, -0.045f, 0.008f), 0.014f, 0.012f, 5, _ => fur, false, false);
            frontKit.AddEllipsoid(new Vector3(0f, -0.047f, 0.016f), new Vector3(0.014f, 0.01f, 0.02f), Quaternion.identity, 1, _ => Cream);
            var frontMesh = MagicLandSetup.SaveMesh(frontKit.ToMesh(name + " Front Leg"), name + " Front Leg");

            var hindKit = new MeshKit();
            hindKit.AddEllipsoid(new Vector3(0f, -0.008f, 0.045f), new Vector3(0.019f, 0.013f, 0.06f), Quaternion.identity, 1,
                p => p.y < -0.012f ? Cream : fur);
            var hindMesh = MagicLandSetup.SaveMesh(hindKit.ToMesh(name + " Hind Foot"), name + " Hind Foot");

            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(14f, ForestGround.HeightAt(14f, 5f), 5f);
            root.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            root.transform.localScale = Vector3.one * scale;

            var body = Part("Body", root.transform, BodyPivot, Quaternion.identity, bodyMesh, mat);
            var head = Part("Head", body, HeadPivot - BodyPivot, Quaternion.identity, headMesh, mat);
            var earL = Part("Ear L", head, new Vector3(-0.022f, 0.065f, 0.02f), Quaternion.Euler(-14f, -10f, 9f), earMesh, mat);
            var earR = Part("Ear R", head, new Vector3(0.022f, 0.065f, 0.02f), Quaternion.Euler(-14f, 10f, -9f), earMesh, mat);
            var tail = Part("Tail", body, TailPivot - BodyPivot, Quaternion.identity, tailMesh, mat);

            // Front legs follow the body; hind feet stay on the ground so the rabbit can sit up on them.
            var front = new[]
            {
                Part("Front Leg L", body, new Vector3(-0.03f, 0.05f, 0.075f) - BodyPivot, Quaternion.identity, frontMesh, mat),
                Part("Front Leg R", body, new Vector3(0.03f, 0.05f, 0.075f) - BodyPivot, Quaternion.identity, frontMesh, mat),
            };
            var hind = new[]
            {
                Part("Hind Foot L", root.transform, new Vector3(-0.05f, 0.02f, -0.11f), Quaternion.identity, hindMesh, mat),
                Part("Hind Foot R", root.transform, new Vector3(0.05f, 0.02f, -0.11f), Quaternion.identity, hindMesh, mat),
            };

            var agent = root.AddComponent<RabbitAgent>();
            agent.Configure(body, head, earL, earR, tail, front, hind, delay, laneOffset, seed);
            return agent;
        }
    }
}
