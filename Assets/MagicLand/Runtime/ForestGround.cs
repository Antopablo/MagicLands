using UnityEngine;

namespace MagicLand
{
    /// <summary>Ground height in content space, shared by the scene generator and the animals.</summary>
    public static class ForestGround
    {
        public const float BaseHeight = -1.1f;

        public static float HeightAt(float x, float z)
        {
            float r = Mathf.Sqrt(x * x + z * z);
            float ripple = (Mathf.PerlinNoise(x * 0.15f + 10f, z * 0.15f + 10f) - 0.5f) * 0.25f;
            float hills = Mathf.PerlinNoise(x * 0.04f + 3f, z * 0.04f + 7f) * 4f;
            float rise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(14f, 45f, r));
            return BaseHeight + ripple + (hills + 1.5f) * rise;
        }

        /// <summary>Center of the waterfall pool, on the left side of the clearing.</summary>
        public static readonly Vector2 WaterfallPool = new Vector2(-9.2f, 14f);

        public static bool NearWaterfall(float x, float z, float radius) =>
            (new Vector2(x, z) - WaterfallPool).sqrMagnitude < radius * radius;

        /// <summary>Center of the small pond on the left edge of the clearing.</summary>
        public static readonly Vector2 PondCenter = new Vector2(-6.4f, 7.6f);

        public static bool NearPond(float x, float z, float radius) =>
            (new Vector2(x, z) - PondCenter).sqrMagnitude < radius * radius;

        public static bool BlocksPondView(float x, float z, float halfWidth)
        {
            var p = new Vector2(x, z);
            float t = Vector2.Dot(p, PondCenter) / PondCenter.sqrMagnitude;
            return t > 0f && t < 1f && (p - PondCenter * t).sqrMagnitude < halfWidth * halfWidth;
        }

        /// <summary>Tree on the right of the clearing whose low branches host the little birds.</summary>
        public static readonly Vector2 BirdTree = new Vector2(6.2f, 7.4f);

        public static bool NearBirdTree(float x, float z, float radius) =>
            (new Vector2(x, z) - BirdTree).sqrMagnitude < radius * radius;

        /// <summary>Small area right in front of the viewer where the mouse wanders (grass kept short there).</summary>
        public static readonly Vector2 MouseAreaX = new Vector2(-1.6f, 1.6f);
        public static readonly Vector2 MouseAreaZ = new Vector2(1.6f, 3.4f);

        public static bool InMouseArea(float x, float z, float margin) =>
            x > MouseAreaX.x - margin && x < MouseAreaX.y + margin && z > MouseAreaZ.x - margin && z < MouseAreaZ.y + margin;

        /// <summary>True near the line of sight from the viewer to the waterfall.</summary>
        public static bool BlocksWaterfallView(float x, float z, float halfWidth)
        {
            var p = new Vector2(x, z);
            float t = Mathf.Clamp01(Vector2.Dot(p, WaterfallPool) / WaterfallPool.sqrMagnitude);
            return (p - WaterfallPool * t).sqrMagnitude < halfWidth * halfWidth;
        }

        /// <summary>Open area in front of the viewer, kept free of trees.</summary>
        public static bool InClearing(float x, float z) =>
            z > 1.5f && z < 19f && Mathf.Abs(x) < 2.5f + z * 0.45f;
    }
}
