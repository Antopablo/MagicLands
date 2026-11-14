using UnityEngine;

namespace MagicLand
{
    /// <summary>Slowly drifts clouds across the sky; they shrink away at the ends of the path and come back on the other side.</summary>
    public class CloudDrift : MonoBehaviour
    {
        [SerializeField] Transform[] clouds = new Transform[0];
        [SerializeField] float[] speeds = new float[0];
        [SerializeField] float halfWidth = 50f;
        [Tooltip("Distance from each end over which a cloud grows or shrinks.")]
        [SerializeField] float fadeDistance = 12f;

        Vector3[] m_Scales;

        public void Configure(Transform[] items, float[] itemSpeeds, float width)
        {
            clouds = items;
            speeds = itemSpeeds;
            halfWidth = width;
        }

        void Update() => Step(Time.deltaTime);

        public void Step(float dt)
        {
            if (m_Scales == null || m_Scales.Length != clouds.Length)
            {
                m_Scales = new Vector3[clouds.Length];
                for (int i = 0; i < clouds.Length; i++)
                    m_Scales[i] = clouds[i].localScale;
            }

            for (int i = 0; i < clouds.Length; i++)
            {
                var p = clouds[i].localPosition;
                p.x += speeds[i] * dt;
                if (p.x > halfWidth)
                    p.x -= halfWidth * 2f;
                clouds[i].localPosition = p;
                float edge = Mathf.Clamp01((halfWidth - Mathf.Abs(p.x)) / fadeDistance);
                clouds[i].localScale = m_Scales[i] * Mathf.SmoothStep(0f, 1f, edge);
            }
        }
    }
}
