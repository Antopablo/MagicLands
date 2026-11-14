using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MagicLand.EditorTools
{
    /// <summary>Builds the waterfall on the side of the clearing: rocky cliff, falling water, pool, mist and sound.</summary>
    public static class WaterfallBuilder
    {
        const float Height = 4f;
        const float PoolRadius = 2.25f;

        static readonly Color Stone = new Color(0.52f, 0.52f, 0.57f);
        static readonly Color StoneDark = new Color(0.38f, 0.38f, 0.44f);
        static readonly Color Moss = new Color(0.32f, 0.5f, 0.26f);

        public struct Materials
        {
            public Material rock;
            public Material fall;
            public Material veil;
            public Material pool;
            public Material mist;
        }

        public static Transform Create(Transform parent, Materials mats, AudioClip sound)
        {
            var rnd = new System.Random(21);
            float R(float min, float max) => min + (float)rnd.NextDouble() * (max - min);

            Vector2 pool2 = ForestGround.WaterfallPool;
            float groundY = ForestGround.HeightAt(pool2.x, pool2.y);
            var center = new Vector3(pool2.x, groundY, pool2.y);
            // Facing the viewer (content origin), on the horizontal plane.
            Vector3 front = new Vector3(-pool2.x, 0f, -pool2.y).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, front).normalized;

            var root = new GameObject("Waterfall");
            root.transform.SetParent(parent, false);

            // ---------- Cliff ----------
            var rocks = new MeshKit();
            MeshKit.ColorFn RockColor(Vector3 c, float radiusY)
            {
                Color baseColor = Color.Lerp(Stone, StoneDark, R(0f, 1f));
                return p => p.y > c.y + radiusY * 0.55f ? Moss : Color.Lerp(baseColor * 0.85f, baseColor, Mathf.Clamp01((p.y - c.y) / radiusY * 0.5f + 0.5f));
            }
            void Rock(Vector3 c, Vector3 radii)
            {
                rocks.AddEllipsoid(c, radii, Quaternion.LookRotation(front) * Quaternion.Euler(0f, R(-20f, 20f), 0f), 1, RockColor(c, radii.y), 0.25f, rnd.Next());
            }

            Vector3 back = center - front * 3.2f;
            for (int k = -2; k <= 2; k++)
                Rock(back + side * (k * 1.5f) + Vector3.up * R(1.0f, 1.3f), new Vector3(R(1.1f, 1.4f), R(1.4f, 1.7f), 1.3f));
            for (int k = -1; k <= 1; k++)
                Rock(back - front * 0.2f + side * (k * 1.4f) + Vector3.up * 3f, new Vector3(R(1.0f, 1.2f), R(1.2f, 1.4f), 1.2f));
            for (int k = -1; k <= 1; k += 2)
            {
                Rock(back + side * (k * 3.6f) + Vector3.up * 0.6f, new Vector3(1.1f, 1.1f, 1.2f));
                Rock(back - front * 0.3f + side * (k * 2.6f) + Vector3.up * 4.1f, new Vector3(0.8f, 0.7f, 0.9f));
            }
            // Lip rock the water spills over.
            Rock(center - front * 2.25f + Vector3.up * (Height - 0.35f), new Vector3(0.95f, 0.45f, 0.8f));

            // Rocks around the pool.
            for (int i = 0; i < 16; i++)
            {
                float a = i / 16f * Mathf.PI * 2f + R(-0.1f, 0.1f);
                Vector3 dir = side * Mathf.Cos(a) + front * Mathf.Sin(a);
                Vector3 p = center + dir * (PoolRadius + R(-0.05f, 0.2f));
                p.y = ForestGround.HeightAt(p.x, p.z);
                float s = R(0.25f, 0.45f);
                Rock(p, new Vector3(s, s * 0.6f, s * R(0.8f, 1.2f)));
            }

            // Bushes on top of the cliff.
            for (int k = -1; k <= 1; k += 2)
            {
                Vector3 c = back + side * (k * 1.8f) + Vector3.up * 4.4f;
                Color leaf = new Color(0.27f, 0.5f, 0.24f);
                rocks.Wind = p => new Vector2(Mathf.Clamp01((p.y - c.y + 0.6f) / 1.2f) * 0.05f, k);
                rocks.AddEllipsoid(c, new Vector3(0.9f, 0.7f, 0.9f), Quaternion.identity, 1,
                    p => Color.Lerp(leaf * 0.7f, leaf * 1.1f, Mathf.Clamp01((p.y - c.y) / 0.7f * 0.5f + 0.5f)), 0.2f, rnd.Next());
            }
            rocks.Wind = null;
            AddRenderer(root.transform, "Cliff", MagicLandSetup.SaveMesh(rocks.ToMesh("Waterfall Cliff"), "Waterfall Cliff"), mats.rock);

            // ---------- Falling water ----------
            Vector3 lip = center - front * 1.6f + Vector3.up * Height;
            float poolY = PoolSurface(center);
            AddRenderer(root.transform, "Falling Water",
                MagicLandSetup.SaveMesh(FallMesh("Waterfall Water", lip, poolY, front, side, 1.3f, 1.8f, 0f), "Waterfall Water"), mats.fall);
            AddRenderer(root.transform, "Falling Water Veil",
                MagicLandSetup.SaveMesh(FallMesh("Waterfall Veil", lip + front * 0.08f, poolY, front, side, 0.9f, 1.4f, 0.12f), "Waterfall Veil"), mats.veil);

            // ---------- Pool ----------
            var poolGo = AddRenderer(root.transform, "Pool", MagicLandSetup.SaveMesh(PoolMesh(PoolRadius + 0.15f, -front * 1.3f), "Waterfall Pool"), mats.pool);
            poolGo.transform.localPosition = new Vector3(center.x, poolY, center.z);

            // ---------- Mist ----------
            Vector3 landing = center - front * 1.0f;
            landing.y = poolY;
            CreateMist(root.transform, landing, Quaternion.LookRotation(front), mats.mist);

            // ---------- Sound ----------
            var soundGo = new GameObject("Waterfall Sound");
            soundGo.transform.SetParent(root.transform, false);
            soundGo.transform.localPosition = landing + Vector3.up * 1.2f;
            var source = soundGo.AddComponent<AudioSource>();
            source.clip = sound;
            source.loop = true;
            source.playOnAwake = true;
            source.volume = 0.4f;
            source.spatialBlend = 0.75f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 5f;
            source.maxDistance = 45f;
            source.dopplerLevel = 0f;

            return root.transform;
        }

        static float PoolSurface(Vector3 center)
        {
            float max = float.MinValue;
            for (int i = 0; i <= 24; i++)
            {
                for (int ring = 0; ring <= 3; ring++)
                {
                    float a = i / 24f * Mathf.PI * 2f;
                    float r = PoolRadius * ring / 3f;
                    max = Mathf.Max(max, ForestGround.HeightAt(center.x + Mathf.Cos(a) * r, center.z + Mathf.Sin(a) * r));
                }
            }
            return max + 0.03f;
        }

        /// <summary>
        /// Curved ribbon falling from the lip to the pool. UV.x across, UV.y from top (0) to bottom (1).
        /// </summary>
        static Mesh FallMesh(string name, Vector3 lip, float bottomY, Vector3 front, Vector3 side, float topWidth, float bottomWidth, float extraOut)
        {
            const int rows = 18, cols = 6;
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            float drop = lip.y - bottomY;

            for (int r = 0; r <= rows; r++)
            {
                float t = r / (float)rows;
                // Water leaves the lip forward, then falls almost vertically.
                float outward = (0.6f + extraOut) * Mathf.Sqrt(t);
                Vector3 c = lip + front * outward + Vector3.down * (drop * t);
                float width = Mathf.Lerp(topWidth, bottomWidth, t * t);
                for (int k = 0; k <= cols; k++)
                {
                    float u = k / (float)cols;
                    // Slight bulge toward the viewer in the middle.
                    float bulge = Mathf.Sin(u * Mathf.PI) * 0.12f;
                    vertices.Add(c + side * ((u - 0.5f) * width) + front * bulge);
                    uvs.Add(new Vector2(u, t));
                }
            }
            for (int r = 0; r < rows; r++)
            {
                for (int k = 0; k < cols; k++)
                {
                    int i0 = r * (cols + 1) + k, i1 = i0 + 1, i2 = i0 + cols + 1, i3 = i2 + 1;
                    tris.AddRange(new[] { i0, i2, i1, i1, i2, i3 });
                }
            }
            return BuildMesh(name, vertices, uvs, tris);
        }

        /// <summary>Flat disk whose polar UVs radiate from <paramref name="origin"/> (local), so ripples spread from where the water lands.
        /// UV.x = angle (0..1), UV.y = 0 at origin, 1 at the edge.</summary>
        static Mesh PoolMesh(float radius, Vector3 origin)
        {
            const int rings = 6, segments = 40;
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            for (int r = 0; r <= rings; r++)
            {
                float v = r / (float)rings;
                for (int s = 0; s <= segments; s++)
                {
                    float u = s / (float)segments;
                    float a = u * Mathf.PI * 2f;
                    vertices.Add(Vector3.Lerp(origin, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius, v));
                    uvs.Add(new Vector2(u, v));
                }
            }
            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int i0 = r * (segments + 1) + s, i1 = i0 + 1, i2 = i0 + segments + 1, i3 = i2 + 1;
                    tris.AddRange(new[] { i0, i2, i1, i1, i2, i3 });
                }
            }
            return BuildMesh("Waterfall Pool", vertices, uvs, tris);
        }

        static Mesh BuildMesh(string name, List<Vector3> vertices, List<Vector2> uvs, List<int> tris)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static GameObject AddRenderer(Transform parent, string name, Mesh mesh, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go;
        }

        static void CreateMist(Transform parent, Vector3 position, Quaternion rotation, Material mat)
        {
            var go = new GameObject("Mist");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation;
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new Color(0.9f, 0.97f, 1f, 0.18f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.gravityModifier = -0.02f;
            main.maxParticles = 80;

            var emission = ps.emission;
            emission.rateOverTime = 18f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1.6f, 0.2f, 0.6f);
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.4f));

            var color = ps.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = mat;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }
}
