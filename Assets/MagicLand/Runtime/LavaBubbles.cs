using UnityEngine;

namespace MagicLand
{
    /// <summary>Glowing bubbles slowly swelling on the lava pond, then popping with a tiny spark and a soft "bloop".</summary>
    public class LavaBubbles : MonoBehaviour
    {
        [SerializeField] Transform[] bubbles = new Transform[0];
        [SerializeField] ParticleSystem sparks;
        [SerializeField] AudioSource bloop;
        [Tooltip("Pond center (content space) and the radius where bubbles may appear.")]
        [SerializeField] Vector3 center;
        [SerializeField] float radius = 1f;
        [SerializeField] float bubbleSize = 0.22f;
        [SerializeField] int seed = 13;

        class Bubble
        {
            public float time, grow, wait, size;
        }

        Bubble[] m_State;
        System.Random m_Random;

        public int Pops { get; private set; }

        public void Configure(Transform[] bubbleParts, ParticleSystem popSparks, AudioSource popSound, Vector3 pondCenter, float pondRadius)
        {
            bubbles = bubbleParts;
            sparks = popSparks;
            bloop = popSound;
            center = pondCenter;
            radius = pondRadius;
        }

        float Range(float min, float max) => min + (float)m_Random.NextDouble() * (max - min);

        void Init()
        {
            if (m_State != null)
                return;
            m_Random = new System.Random(seed);
            m_State = new Bubble[bubbles.Length];
            for (int i = 0; i < bubbles.Length; i++)
            {
                m_State[i] = new Bubble();
                Restart(i);
                m_State[i].wait = Range(0f, 3f);
            }
        }

        void Restart(int i)
        {
            var b = m_State[i];
            b.time = 0f;
            b.grow = Range(1.2f, 2.4f);
            b.wait = Range(0.6f, 3.5f);
            b.size = bubbleSize * Range(0.6f, 1.3f);
            float a = Range(0f, Mathf.PI * 2f), r = Mathf.Sqrt(Range(0f, 1f)) * radius;
            bubbles[i].localPosition = center + new Vector3(Mathf.Sin(a) * r, 0f, Mathf.Cos(a) * r);
            bubbles[i].localScale = Vector3.zero;
        }

        void Start() => Init();

        void Update() => Step(Time.deltaTime);

        public void Step(float dt)
        {
            Init();
            for (int i = 0; i < bubbles.Length; i++)
            {
                var b = m_State[i];
                b.time += dt;
                float t = b.time - b.wait;
                if (t < 0f)
                {
                    bubbles[i].localScale = Vector3.zero;
                    continue;
                }
                if (t < b.grow)
                {
                    float f = t / b.grow;
                    float wobble = 1f + Mathf.Sin(t * 11f) * 0.06f * f;
                    float s = b.size * Mathf.SmoothStep(0f, 1f, f);
                    bubbles[i].localScale = new Vector3(s * wobble, s * (0.85f + 0.3f * f) / wobble, s * wobble);
                    continue;
                }
                Pop(bubbles[i].localPosition, bubbles[i].position, b.size);
                Restart(i);
            }
        }

        void Pop(Vector3 localPosition, Vector3 position, float size)
        {
            Pops++;
            if (sparks != null)
            {
                var emit = new ParticleSystem.EmitParams { position = localPosition + Vector3.up * size * 0.5f, applyShapeToPosition = true };
                sparks.Emit(emit, 6);
            }
            if (bloop != null && bloop.clip != null && Application.isPlaying)
            {
                bloop.transform.position = position;
                bloop.pitch = Range(0.85f, 1.25f);
                bloop.Play();
            }
        }
    }
}
