using UnityEngine;

namespace MagicLand
{
    /// <summary>
    /// After a delay, sends a shooting star across the sky in front of the viewer every few seconds.
    /// Each child renderer is a reusable streak (head at its origin, tail along local -X).
    /// </summary>
    public class ShootingStars : MonoBehaviour
    {
        [SerializeField] Renderer[] meteors = new Renderer[0];
        [Tooltip("Seconds before the first shooting star.")]
        [SerializeField] float delay = 30f;
        [SerializeField] Vector2 interval = new Vector2(6f, 16f);
        [SerializeField] float distance = 60f;
        [SerializeField] float speed = 20f;
        [SerializeField] Vector2 lifetime = new Vector2(1.1f, 1.7f);
        [SerializeField] float brightness = 2.5f;
        [SerializeField] int seed = 11;

        static readonly int s_Intensity = Shader.PropertyToID("_Intensity");

        class Flight
        {
            public Vector3 position, velocity;
            public float age, life;
        }

        Flight[] m_Flights;
        MaterialPropertyBlock m_Block;
        System.Random m_Random;
        float m_Time;
        float m_Next;

        public void Configure(Renderer[] meteorRenderers, float startDelay)
        {
            meteors = meteorRenderers;
            delay = startDelay;
        }

        void Awake() => Init();

        void Init()
        {
            if (m_Flights != null)
                return;
            m_Random = new System.Random(seed);
            m_Block = new MaterialPropertyBlock();
            m_Flights = new Flight[meteors.Length];
            m_Next = delay;
            foreach (var m in meteors)
                m.enabled = false;
        }

        float Range(float min, float max) => min + (float)m_Random.NextDouble() * (max - min);

        void Update() => Step(Time.deltaTime);

        public void Step(float dt)
        {
            Init();
            m_Time += dt;

            if (m_Time >= m_Next)
            {
                Launch();
                // Now and then a second one follows shortly after, like a small meteor shower.
                m_Next = m_Time + (m_Random.NextDouble() < 0.2 ? Range(1.2f, 2.5f) : Range(interval.x, interval.y));
            }

            for (int i = 0; i < m_Flights.Length; i++)
            {
                var f = m_Flights[i];
                if (f == null)
                    continue;
                f.age += dt;
                if (f.age >= f.life)
                {
                    meteors[i].enabled = false;
                    m_Flights[i] = null;
                    continue;
                }
                f.position += f.velocity * dt;
                float t = f.age / f.life;
                float fade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.2f)) * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - t) / 0.45f));
                Place(meteors[i].transform, f);
                meteors[i].GetPropertyBlock(m_Block);
                m_Block.SetFloat(s_Intensity, brightness * fade);
                meteors[i].SetPropertyBlock(m_Block);
            }
        }

        /// <summary>Starts a new shooting star on a free streak, if any.</summary>
        public void Launch()
        {
            Init();
            int slot = System.Array.FindIndex(m_Flights, f => f == null);
            if (slot < 0)
                return;

            // Start somewhere in the upper part of the view, gliding down and sideways.
            float azimuth = Range(-45f, 45f);
            float elevation = Range(18f, 42f);
            Vector3 dir = Quaternion.Euler(-elevation, azimuth, 0f) * Vector3.forward;
            float side = azimuth > 0f ? -1f : 1f;
            if (m_Random.NextDouble() < 0.3)
                side = -side;
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
            Vector3 up = Vector3.Cross(dir, right);
            float dropAngle = Range(15f, 45f) * Mathf.Deg2Rad;
            Vector3 travel = (right * side * Mathf.Cos(dropAngle) - up * Mathf.Sin(dropAngle)).normalized;

            var f = new Flight
            {
                position = dir * distance,
                velocity = travel * speed * Range(0.85f, 1.15f),
                life = Range(lifetime.x, lifetime.y),
            };
            m_Flights[slot] = f;
            Place(meteors[slot].transform, f);
            meteors[slot].enabled = true;
        }

        static void Place(Transform t, Flight f)
        {
            Vector3 forward = f.position.normalized;
            Vector3 up = Vector3.Cross(forward, f.velocity.normalized);
            t.localPosition = f.position;
            t.localRotation = Quaternion.LookRotation(forward, up);
        }
    }
}
