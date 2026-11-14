using UnityEngine;

namespace MagicLand
{
    /// <summary>A small V-shaped flock of birds crossing the sky now and then.</summary>
    public class BirdFlock : MonoBehaviour
    {
        [SerializeField] BirdRig[] birds = new BirdRig[0];
        [Tooltip("Seconds before the first crossing.")]
        [SerializeField] float delay = 20f;
        [SerializeField] Vector2 interval = new Vector2(35f, 70f);
        [SerializeField] float speed = 4.5f;
        [SerializeField] float halfWidth = 50f;
        [SerializeField] int seed = 31;

        System.Random m_Random;
        float m_Time;
        float m_Next;
        bool m_Flying;
        Vector3 m_Start, m_End;
        float m_Progress;
        float[] m_Phase;
        bool m_Initialized;

        public bool IsFlying => m_Flying;

        public void Configure(BirdRig[] rigs, float startDelay)
        {
            birds = rigs;
            delay = startDelay;
        }

        float Range(float min, float max) => min + (float)m_Random.NextDouble() * (max - min);

        void Init()
        {
            if (m_Initialized)
                return;
            m_Random = new System.Random(seed);
            m_Next = delay;
            m_Phase = new float[birds.Length];
            for (int i = 0; i < birds.Length; i++)
            {
                m_Phase[i] = Range(0f, 6.28f);
                birds[i].SetVisible(false);
            }
            m_Initialized = true;
        }

        void Update() => Step(Time.deltaTime);

        /// <summary>Starts a crossing right away (used for previews).</summary>
        public void Launch()
        {
            Init();
            float side = m_Random.NextDouble() < 0.5 ? -1f : 1f;
            float height = Range(9f, 15f);
            m_Start = new Vector3(-side * halfWidth, height, Range(22f, 32f));
            m_End = new Vector3(side * halfWidth, height + Range(-2f, 3f), Range(22f, 32f));
            m_Progress = 0f;
            m_Flying = true;
            foreach (var b in birds)
                b.SetVisible(true);
        }

        public void Step(float dt)
        {
            Init();
            m_Time += dt;
            if (!m_Flying)
            {
                if (m_Time >= m_Next)
                    Launch();
                else
                    return;
            }

            Vector3 path = m_End - m_Start;
            float length = path.magnitude;
            m_Progress += speed * dt / length;
            if (m_Progress >= 1f + 0.2f)
            {
                m_Flying = false;
                m_Next = m_Time + Range(interval.x, interval.y);
                foreach (var b in birds)
                    b.SetVisible(false);
                return;
            }

            Vector3 dir = path / length;
            Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;
            Vector3 lead = m_Start + path * m_Progress;
            for (int i = 0; i < birds.Length; i++)
            {
                // V formation: the leader in front, the others alternating left and right behind.
                int rank = (i + 1) / 2;
                float sign = i % 2 == 0 ? 1f : -1f;
                float t = m_Time + m_Phase[i];
                Vector3 offset = -dir * rank * 1.6f + side * sign * rank * 1.3f
                                 + new Vector3(0f, Mathf.Sin(t * 0.8f) * 0.25f, Mathf.Sin(t * 0.6f) * 0.2f);
                var rig = birds[i];
                rig.root.localPosition = lead + offset;
                rig.root.localRotation = Quaternion.LookRotation(dir, Vector3.up);

                // Flap, then glide for a moment.
                float glide = Mathf.PerlinNoise(t * 0.4f, i * 3.1f) > 0.55f ? 0.15f : 1f;
                float flap = 10f + Mathf.Sin(t * 5.5f * Mathf.PI * 2f * 0.5f) * 50f * glide;
                rig.Pose(1f, flap, 0f, 0f, 0f);
            }
        }
    }
}
