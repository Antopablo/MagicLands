using UnityEngine;

namespace MagicLand
{
    /// <summary>Makes glowing halos breathe softly, each one at its own pace.</summary>
    public class GlowPulse : MonoBehaviour
    {
        [SerializeField] Transform[] targets = new Transform[0];
        [SerializeField] float amount = 0.14f;
        [SerializeField] float speed = 0.9f;

        Vector3[] m_Rest;
        float m_Time;

        public void Configure(Transform[] halos, float pulseAmount, float pulseSpeed)
        {
            targets = halos;
            amount = pulseAmount;
            speed = pulseSpeed;
        }

        void Update() => Step(Time.deltaTime);

        public void Step(float dt)
        {
            if (m_Rest == null)
            {
                m_Rest = new Vector3[targets.Length];
                for (int i = 0; i < targets.Length; i++)
                    m_Rest[i] = targets[i].localScale;
            }
            m_Time += dt;
            for (int i = 0; i < targets.Length; i++)
            {
                float s = 1f + amount * Mathf.Sin(m_Time * speed * (0.8f + 0.13f * (i % 4)) + i * 2.3f);
                targets[i].localScale = m_Rest[i] * s;
            }
        }
    }
}
