using UnityEngine;

namespace MagicLand
{
    /// <summary>Plays soft bird calls from random spots in the trees around the viewer.</summary>
    public class BirdSongs : MonoBehaviour
    {
        [SerializeField] AudioClip[] clips;
        [SerializeField] Vector2 interval = new Vector2(7f, 16f);
        [SerializeField] float volume = 0.2f;
        [SerializeField] int voices = 3;

        AudioSource[] m_Sources;
        float m_Next;
        int m_Index;

        public void Configure(AudioClip[] birdClips) => clips = birdClips;

        void Start()
        {
            m_Sources = new AudioSource[voices];
            for (int i = 0; i < voices; i++)
            {
                var go = new GameObject("Bird Voice " + i);
                go.transform.SetParent(transform, false);
                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 1f;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 2f;
                source.maxDistance = 40f;
                source.dopplerLevel = 0f;
                go.AddComponent<AudioLowPassFilter>().cutoffFrequency = 4500f;
                m_Sources[i] = source;
            }
            m_Next = Time.time + Random.Range(4f, 8f);
        }

        void Update()
        {
            if (clips == null || clips.Length == 0 || Time.time < m_Next)
                return;

            m_Next = Time.time + Random.Range(interval.x, interval.y);
            var source = m_Sources[m_Index];
            m_Index = (m_Index + 1) % m_Sources.Length;

            float angle = Random.Range(-160f, 160f) * Mathf.Deg2Rad;
            float distance = Random.Range(12f, 25f);
            source.transform.localPosition = new Vector3(Mathf.Sin(angle) * distance, Random.Range(2f, 6f), Mathf.Cos(angle) * distance);
            source.pitch = Random.Range(0.92f, 1.05f);
            source.volume = volume * Random.Range(0.6f, 1f);
            source.clip = clips[Random.Range(0, clips.Length)];
            source.Play();
        }
    }
}
