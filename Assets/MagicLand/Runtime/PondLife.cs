using UnityEngine;

namespace MagicLand
{
    /// <summary>Gently animates the ducks and schedules occasional, quiet frog calls.</summary>
    public class PondLife : MonoBehaviour
    {
        [SerializeField] Transform[] ducks = new Transform[0];
        [SerializeField] Transform[] frogs = new Transform[0];
        [SerializeField] AudioClip frogCall;

        Vector3[] m_DuckHomes;
        AudioSource[] m_FrogVoices;
        float m_NextCall;
        int m_NextFrog;
        float m_Time;
        System.Random m_Random;

        public void Configure(Transform[] duckTransforms, Transform[] frogTransforms, AudioClip call)
        {
            ducks = duckTransforms;
            frogs = frogTransforms;
            frogCall = call;
        }

        void Start()
        {
            m_Random = new System.Random(63);
            m_DuckHomes = new Vector3[ducks.Length];
            for (int i = 0; i < ducks.Length; i++)
                m_DuckHomes[i] = ducks[i].localPosition;

            m_FrogVoices = new AudioSource[frogs.Length];
            for (int i = 0; i < frogs.Length; i++)
            {
                var source = frogs[i].gameObject.AddComponent<AudioSource>();
                source.clip = frogCall;
                source.playOnAwake = false;
                source.spatialBlend = 0.9f;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 3f;
                source.maxDistance = 18f;
                source.volume = 0.08f;
                source.dopplerLevel = 0f;
                m_FrogVoices[i] = source;
            }
            m_NextCall = 5f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            m_Time += dt;

            for (int i = 0; i < ducks.Length; i++)
            {
                Transform duck = ducks[i];
                Vector3 home = m_DuckHomes[i];
                float phase = m_Time * 0.16f + i * 2.4f;
                duck.localPosition = home + new Vector3(Mathf.Sin(phase) * 0.65f, Mathf.Sin(m_Time * 1.7f + i) * 0.025f,
                    Mathf.Cos(phase) * 0.28f);
                float heading = Mathf.Atan2(Mathf.Cos(phase) * 0.65f, -Mathf.Sin(phase) * 0.28f) * Mathf.Rad2Deg;
                duck.localRotation = Quaternion.Euler(0f, heading, 0f);
            }

            if (frogCall != null && Time.timeSinceLevelLoad >= m_NextCall && m_FrogVoices.Length > 0)
            {
                m_FrogVoices[m_NextFrog].Play();
                m_NextFrog = (m_NextFrog + 1) % m_FrogVoices.Length;
                m_NextCall = Time.timeSinceLevelLoad + 9f + (float)m_Random.NextDouble() * 8f;
            }
        }
    }
}
