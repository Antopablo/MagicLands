using UnityEngine;

namespace MagicLand.EditorTools
{
    /// <summary>Builds a small storybook mouse (round body, big pink ears) animated by MouseAgent.</summary>
    public static class MouseBuilder
    {
        static readonly Color Fur = new Color(0.66f, 0.58f, 0.54f);
        static readonly Color FurDark = new Color(0.55f, 0.47f, 0.44f);
        static readonly Color Cream = new Color(0.96f, 0.91f, 0.85f);
        static readonly Color Pink = new Color(1f, 0.66f, 0.72f);
        static readonly Color Eye = new Color(0.04f, 0.03f, 0.04f);

        // Pivots in root space (z forward, y up).
        static readonly Vector3 BodyPivot = new Vector3(0f, 0.035f, -0.045f);
        static readonly Vector3 HeadPivot = new Vector3(0f, 0.08f, 0.06f);
        static readonly Vector3 TailBase = new Vector3(0f, 0.045f, -0.1f);
        const int TailSegments = 5;
        const float TailSegmentLength = 0.04f;

        static Color FurAt(Vector3 p, float bellyY, Color fur, Color furDark) =>
            p.y < bellyY ? Cream : Color.Lerp(fur, furDark, Mathf.Clamp01((p.y - bellyY) / 0.08f));

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

        public static MouseAgent Create(Transform parent, Vector3 position, float yaw, float scale,
            Vector2 areaX, Vector2 areaZ, int seed, Material mat) =>
            Create(parent, "Mouse", position, yaw, scale, areaX, areaZ, seed, mat, Fur, FurDark, false);

        /// <summary>A lighter mouse with a pink bow who only comes out to visit the first one.</summary>
        public static MouseAgent CreateVisitor(Transform parent, Vector3 position, float scale,
            Vector2 areaX, Vector2 areaZ, Material mat)
        {
            var agent = Create(parent, "Visiting Mouse", position, 0f, scale, areaX, areaZ, 9, mat,
                new Color(0.82f, 0.74f, 0.68f), new Color(0.72f, 0.63f, 0.58f), true);
            agent.MakeVisitor();
            return agent;
        }

        /// <summary>Small puffy heart (front facing +Z) that floats above the mice when they rub noses.</summary>
        public static Transform CreateHeart(Transform parent, Material mat)
        {
            var kit = new MeshKit();
            Color red = new Color(1f, 0.32f, 0.45f), light = new Color(1f, 0.62f, 0.72f);
            for (int s = -1; s <= 1; s += 2)
                kit.AddEllipsoid(new Vector3(s * 0.036f, 0.004f, 0f), new Vector3(0.048f, 0.072f, 0.035f),
                    Quaternion.Euler(0f, 0f, -s * 40f), 2, p => Color.Lerp(red, light, Mathf.Clamp01(p.y * 6f + p.z * 10f)));
            var mesh = MagicLandSetup.SaveMesh(kit.ToMesh("Mouse Heart"), "Mouse Heart");
            return Part("Mouse Heart", parent, new Vector3(0f, -5f, 0f), Quaternion.identity, mesh, mat);
        }

        static MouseAgent Create(Transform parent, string name, Vector3 position, float yaw, float scale,
            Vector2 areaX, Vector2 areaZ, int seed, Material mat, Color fur, Color furDark, bool bow)
        {
            // Body, relative to the body pivot (at the hind legs, so it can sit up).
            var bodyKit = new MeshKit();
            Vector3 o = -BodyPivot;
            MeshKit.ColorFn bodyColor = p => FurAt(p + BodyPivot, 0.035f, fur, furDark);
            bodyKit.AddEllipsoid(o + new Vector3(0f, 0.06f, 0f), new Vector3(0.055f, 0.05f, 0.08f), Quaternion.identity, 2, bodyColor);
            bodyKit.AddEllipsoid(o + new Vector3(0f, 0.058f, -0.04f), new Vector3(0.062f, 0.056f, 0.06f), Quaternion.identity, 1, bodyColor);
            // Hind haunches.
            for (int s = -1; s <= 1; s += 2)
                bodyKit.AddEllipsoid(o + new Vector3(s * 0.045f, 0.04f, -0.045f), new Vector3(0.022f, 0.032f, 0.036f), Quaternion.identity, 1, _ => fur);
            var bodyMesh = MagicLandSetup.SaveMesh(bodyKit.ToMesh(name + " Body"), name + " Body");

            var headKit = new MeshKit();
            headKit.AddEllipsoid(new Vector3(0f, 0.012f, 0.03f), new Vector3(0.043f, 0.041f, 0.048f), Quaternion.identity, 2,
                p => p.y < -0.012f ? Cream : fur);
            headKit.AddEllipsoid(new Vector3(0f, -0.002f, 0.07f), new Vector3(0.023f, 0.021f, 0.032f), Quaternion.Euler(8f, 0f, 0f), 1,
                p => p.y < -0.008f ? Cream : fur);
            headKit.AddEllipsoid(new Vector3(0f, 0.004f, 0.101f), new Vector3(0.011f, 0.009f, 0.009f), Quaternion.identity, 1, _ => Pink);
            for (int s = -1; s <= 1; s += 2)
            {
                headKit.AddEllipsoid(new Vector3(s * 0.024f, 0.024f, 0.058f), new Vector3(0.0095f, 0.012f, 0.008f), Quaternion.identity, 1, _ => Eye);
                headKit.AddEllipsoid(new Vector3(s * 0.025f, 0.029f, 0.0645f), new Vector3(0.003f, 0.003f, 0.002f), Quaternion.identity, 0, _ => Color.white);
                headKit.AddEllipsoid(new Vector3(s * 0.03f, 0.0f, 0.06f), new Vector3(0.01f, 0.006f, 0.006f), Quaternion.identity, 0,
                    _ => Color.Lerp(Pink, Cream, 0.4f));
            }
            if (bow)
            {
                Color ribbon = new Color(1f, 0.45f, 0.65f);
                // Sits on the forehead in front of the right ear so it shows from the viewer.
                var knot = new Vector3(0.022f, 0.052f, 0.046f);
                var tilt = Quaternion.Euler(-25f, 0f, -15f);
                for (int s = -1; s <= 1; s += 2)
                    headKit.AddEllipsoid(knot + tilt * new Vector3(s * 0.019f, 0f, 0f), new Vector3(0.019f, 0.013f, 0.008f),
                        tilt * Quaternion.Euler(0f, 0f, s * 18f), 1, _ => ribbon);
                headKit.AddEllipsoid(knot + tilt * new Vector3(0f, 0f, 0.003f), Vector3.one * 0.008f, tilt, 1, _ => ribbon * 0.85f);
            }
            var headMesh = MagicLandSetup.SaveMesh(headKit.ToMesh(name + " Head"), name + " Head");

            var earKit = new MeshKit();
            earKit.AddEllipsoid(new Vector3(0f, 0.03f, 0f), new Vector3(0.033f, 0.033f, 0.008f), Quaternion.identity, 1,
                p => p.z > 0.003f ? Pink : fur);
            var earMesh = MagicLandSetup.SaveMesh(earKit.ToMesh(name + " Ear"), name + " Ear");

            var tailKit = new MeshKit();
            tailKit.AddTube(Vector3.zero, new Vector3(0f, 0f, -TailSegmentLength), 0.007f, 0.006f, 5,
                _ => Color.Lerp(Pink, fur, 0.45f), true, true);
            var tailMesh = MagicLandSetup.SaveMesh(tailKit.ToMesh(name + " Tail"), name + " Tail");

            var footKit = new MeshKit();
            footKit.AddEllipsoid(new Vector3(0f, 0.008f, 0.008f), new Vector3(0.012f, 0.009f, 0.02f), Quaternion.identity, 1,
                _ => Color.Lerp(Pink, Cream, 0.3f));
            var footMesh = MagicLandSetup.SaveMesh(footKit.ToMesh(name + " Foot"), name + " Foot");

            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(position.x, ForestGround.HeightAt(position.x, position.z), position.z);
            root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            root.transform.localScale = Vector3.one * scale;

            var body = Part("Body", root.transform, BodyPivot, Quaternion.identity, bodyMesh, mat);
            var head = Part("Head", body, HeadPivot - BodyPivot, Quaternion.identity, headMesh, mat);
            var earL = Part("Ear L", head, new Vector3(-0.03f, 0.04f, 0.02f), Quaternion.Euler(-12f, -15f, 28f), earMesh, mat);
            var earR = Part("Ear R", head, new Vector3(0.03f, 0.04f, 0.02f), Quaternion.Euler(-12f, 15f, -28f), earMesh, mat);

            // Tail rises a little, then curls down to the ground.
            var tail = new Transform[TailSegments];
            Transform tailParent = body;
            Vector3 tailPos = TailBase - BodyPivot;
            for (int i = 0; i < TailSegments; i++)
            {
                float pitch = i == 0 ? 22f : -12f;
                tail[i] = Part("Tail " + i, tailParent, tailPos, Quaternion.Euler(pitch, 0f, 0f), tailMesh, mat);
                tailParent = tail[i];
                tailPos = new Vector3(0f, 0f, -TailSegmentLength);
            }

            var feet = new Transform[4];
            // Front paws follow the body (raised in front of the chest when sitting up); hind feet stay on the ground.
            feet[0] = Part("Foot FL", body, new Vector3(-0.03f, 0f, 0.06f) - BodyPivot, Quaternion.identity, footMesh, mat);
            feet[1] = Part("Foot FR", body, new Vector3(0.03f, 0f, 0.06f) - BodyPivot, Quaternion.identity, footMesh, mat);
            feet[2] = Part("Foot BL", root.transform, new Vector3(-0.045f, 0f, -0.05f), Quaternion.identity, footMesh, mat);
            feet[3] = Part("Foot BR", root.transform, new Vector3(0.045f, 0f, -0.05f), Quaternion.identity, footMesh, mat);

            var agent = root.AddComponent<MouseAgent>();
            agent.Configure(body, head, earL, earR, tail, feet, areaX, areaZ, seed);
            return agent;
        }
    }
}
