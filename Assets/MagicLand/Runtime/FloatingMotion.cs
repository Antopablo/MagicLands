using UnityEngine;

namespace MagicLand
{
    /// <summary>Slow, dreamy floating motion around the initial local position.</summary>
    public class FloatingMotion : MonoBehaviour
    {
        [SerializeField] Vector3 amplitude = new Vector3(0.3f, 0.2f, 0.3f);
        [SerializeField] float speed = 0.2f;
        [SerializeField] float pulseAmount = 0.15f;

        Vector3 m_Origin;
        Vector3 m_BaseScale;
        float m_Seed;

        public void Configure(Vector3 newAmplitude, float newSpeed)
        {
            amplitude = newAmplitude;
            speed = newSpeed;
        }

        void Start()
        {
            m_Origin = transform.localPosition;
            m_BaseScale = transform.localScale;
            m_Seed = Random.value * 100f;
        }

        void Update()
        {
            float t = Time.time * speed + m_Seed;
            transform.localPosition = m_Origin + new Vector3(
                Mathf.Sin(t * 1.0f) * amplitude.x,
                Mathf.Sin(t * 1.3f + 1.7f) * amplitude.y,
                Mathf.Sin(t * 0.7f + 3.1f) * amplitude.z);
            transform.localScale = m_BaseScale * (1f + pulseAmount * Mathf.Sin(t * 2.3f));
        }
    }
}
