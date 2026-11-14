using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MagicLand.EditorTools
{
    /// <summary>
    /// Accumulates flat-shaded, vertex-colored triangles with per-vertex wind data (UV1),
    /// used to generate low-poly nature meshes.
    /// </summary>
    public class MeshKit
    {
        public delegate Color ColorFn(Vector3 p);
        public delegate Vector2 WindFn(Vector3 p);

        readonly List<Vector3> m_Positions = new List<Vector3>();
        readonly List<Vector3> m_Normals = new List<Vector3>();
        readonly List<Color> m_Colors = new List<Color>();
        readonly List<Vector2> m_Wind = new List<Vector2>();
        readonly List<int> m_Indices = new List<int>();

        /// <summary>Wind data for the vertices added next. Null means no wind.</summary>
        public WindFn Wind;

        public int VertexCount => m_Positions.Count;

        Vector2 WindAt(Vector3 p) => Wind != null ? Wind(p) : Vector2.zero;

        /// <summary>Adds a triangle oriented so that its normal points away from the inside point.</summary>
        public void AddFace(Vector3 a, Vector3 b, Vector3 c, Vector3 inside, Color color)
        {
            Vector3 n = Vector3.Cross(b - a, c - a);
            if (n.sqrMagnitude < 1e-14f)
                return;
            n.Normalize();
            if (Vector3.Dot(n, (a + b + c) / 3f - inside) < 0f)
            {
                (b, c) = (c, b);
                n = -n;
            }
            AddRaw(a, b, c, n, color, color, color);
        }

        public void AddRaw(Vector3 a, Vector3 b, Vector3 c, Vector3 n, Color ca, Color cb, Color cc)
        {
            int i = m_Positions.Count;
            m_Positions.Add(a);
            m_Positions.Add(b);
            m_Positions.Add(c);
            m_Normals.Add(n);
            m_Normals.Add(n);
            m_Normals.Add(n);
            m_Colors.Add(ca.linear);
            m_Colors.Add(cb.linear);
            m_Colors.Add(cc.linear);
            m_Wind.Add(WindAt(a));
            m_Wind.Add(WindAt(b));
            m_Wind.Add(WindAt(c));
            m_Indices.Add(i);
            m_Indices.Add(i + 1);
            m_Indices.Add(i + 2);
        }

        public void AddEllipsoid(Vector3 center, Vector3 radii, Quaternion rotation, int subdiv, ColorFn color, float jitter = 0f, int seed = 0)
        {
            Vector3 Map(Vector3 u)
            {
                float j = jitter > 0f ? 1f + jitter * (Hash01(u, seed) * 2f - 1f) : 1f;
                return center + rotation * Vector3.Scale(u * j, radii);
            }

            foreach (var tri in Icosphere(subdiv))
            {
                Vector3 a = Map(tri[0]), b = Map(tri[1]), c = Map(tri[2]);
                AddFace(a, b, c, center, color((a + b + c) / 3f));
            }
        }

        /// <summary>Tapered cylinder (or cone when topRadius is 0) between two points.</summary>
        public void AddTube(Vector3 bottom, Vector3 top, float bottomRadius, float topRadius, int sides, ColorFn color,
            bool capBottom, bool capTop, float angleOffset = 0f)
        {
            Vector3 dir = (top - bottom).normalized;
            Vector3 u = Vector3.Cross(dir, Mathf.Abs(dir.y) < 0.95f ? Vector3.up : Vector3.right).normalized;
            Vector3 w = Vector3.Cross(dir, u);
            Vector3 mid = (bottom + top) * 0.5f;

            void Side(Vector3 p, Vector3 q, Vector3 r)
            {
                Vector3 centroid = (p + q + r) / 3f;
                Vector3 inside = bottom + dir * Vector3.Dot(centroid - bottom, dir);
                AddFace(p, q, r, inside, color(centroid));
            }

            for (int i = 0; i < sides; i++)
            {
                float a0 = angleOffset + i * Mathf.PI * 2f / sides;
                float a1 = angleOffset + (i + 1) * Mathf.PI * 2f / sides;
                Vector3 d0 = u * Mathf.Cos(a0) + w * Mathf.Sin(a0);
                Vector3 d1 = u * Mathf.Cos(a1) + w * Mathf.Sin(a1);
                Vector3 b0 = bottom + d0 * bottomRadius, b1 = bottom + d1 * bottomRadius;
                Vector3 t0 = top + d0 * topRadius, t1 = top + d1 * topRadius;
                Side(b0, t0, t1);
                Side(b0, t1, b1);
                if (capTop && topRadius > 0f)
                    AddFace(top, t0, t1, mid, color(top));
                if (capBottom && bottomRadius > 0f)
                    AddFace(bottom, b1, b0, mid, color(bottom));
            }
        }

        /// <summary>Single double-sided grass blade (to be used with a Cull Off material).</summary>
        public void AddBlade(Vector3 root, Vector3 side, Vector3 tip, Color baseColor, Color tipColor)
        {
            Vector3 a = root - side * 0.5f, b = root + side * 0.5f;
            Vector3 n = Vector3.Cross(b - a, tip - a).normalized;
            if (n.y < 0f)
                n = -n;
            n = (n + Vector3.up * 2f).normalized;
            AddRaw(a, tip, b, n, baseColor, tipColor, baseColor);
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            if (m_Positions.Count > 65000)
                mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(m_Positions);
            mesh.SetNormals(m_Normals);
            mesh.SetColors(m_Colors);
            mesh.SetUVs(1, m_Wind);
            mesh.SetTriangles(m_Indices, 0);
            mesh.RecalculateBounds();
            var bounds = mesh.bounds;
            bounds.Expand(1.5f);
            mesh.bounds = bounds;
            return mesh;
        }

        public static float Hash01(Vector3 p, int seed)
        {
            unchecked
            {
                int x = Mathf.RoundToInt(p.x * 1000f);
                int y = Mathf.RoundToInt(p.y * 1000f);
                int z = Mathf.RoundToInt(p.z * 1000f);
                int h = x * 73856093 ^ y * 19349663 ^ z * 83492791 ^ seed * 1013904223;
                h ^= h >> 13;
                h *= 1274126177;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }

        static readonly Dictionary<int, List<Vector3[]>> s_Icospheres = new Dictionary<int, List<Vector3[]>>();

        static List<Vector3[]> Icosphere(int subdiv)
        {
            if (s_Icospheres.TryGetValue(subdiv, out var cached))
                return cached;

            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            Vector3[] v =
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1),
            };
            int[] f =
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
            };
            var tris = new List<Vector3[]>();
            for (int i = 0; i < f.Length; i += 3)
                tris.Add(new[] { v[f[i]].normalized, v[f[i + 1]].normalized, v[f[i + 2]].normalized });

            for (int s = 0; s < subdiv; s++)
            {
                var next = new List<Vector3[]>();
                foreach (var tri in tris)
                {
                    Vector3 ab = ((tri[0] + tri[1]) * 0.5f).normalized;
                    Vector3 bc = ((tri[1] + tri[2]) * 0.5f).normalized;
                    Vector3 ca = ((tri[2] + tri[0]) * 0.5f).normalized;
                    next.Add(new[] { tri[0], ab, ca });
                    next.Add(new[] { ab, tri[1], bc });
                    next.Add(new[] { ca, bc, tri[2] });
                    next.Add(new[] { ab, bc, ca });
                }
                tris = next;
            }

            s_Icospheres[subdiv] = tris;
            return tris;
        }
    }
}
