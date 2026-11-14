using UnityEngine;

namespace MagicLand
{
    /// <summary>Ground height and landmarks of the volcano world, in content space (viewer at the origin, looking +Z).</summary>
    public static class VolcanoGround
    {
        public const float BaseHeight = -1.1f;

        /// <summary>Foot of the volcano, far in front of the viewer.</summary>
        public static readonly Vector2 VolcanoCenter = new Vector2(0f, 58f);
        public const float VolcanoBaseRadius = 24f;
        public const float VolcanoHeight = 21f;
        public const float CraterRadius = 4f;

        /// <summary>Small lava pond on the right of the viewer.</summary>
        public static readonly Vector2 LavaPond = new Vector2(6.4f, 12.5f);
        public const float LavaPondRadius = 1.7f;

        public static float HeightAt(float x, float z)
        {
            float r = Mathf.Sqrt(x * x + z * z);
            float ripple = (Mathf.PerlinNoise(x * 0.18f + 4f, z * 0.18f + 9f) - 0.5f) * 0.18f;
            float hills = Mathf.PerlinNoise(x * 0.05f + 11f, z * 0.05f + 2f) * 3.5f;
            float rise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(16f, 48f, r));
            // Lower hills straight ahead so the foot of the volcano stays in view.
            float valley = 1f - 0.65f * Mathf.Exp(-x * x / 196f) * Mathf.Clamp01(z / 20f);
            return BaseHeight + ripple + (hills + 1f) * rise * valley;
        }

        /// <summary>Open area in front of the viewer, kept free of rocks and crystals.</summary>
        public static bool InClearing(float x, float z) =>
            z > 1.2f && z < 16f && Mathf.Abs(x) < 1.8f + z * 0.35f;

        public static bool NearLavaPond(float x, float z, float margin) =>
            (new Vector2(x, z) - LavaPond).sqrMagnitude < (LavaPondRadius + margin) * (LavaPondRadius + margin);
    }
}
