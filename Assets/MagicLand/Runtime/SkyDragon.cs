using UnityEngine;

namespace MagicLand
{
    /// <summary>
    /// A big friendly dragon flying slow, wide loops in the sky around the volcano:
    /// a few lazy wing beats, then a long glide, banking gently in the turns.
    /// </summary>
    public class SkyDragon : MonoBehaviour
    {
        [SerializeField] Transform head;
        [SerializeField] Transform leftWing;
        [SerializeField] Transform rightWing;
        [SerializeField] Transform[] tail = new Transform[0];
        [SerializeField] Vector3 center = new Vector3(0f, 17f, 52f);
        [SerializeField] Vector2 radii = new Vector2(30f, 22f);
        [Tooltip("Radians per second along the loop.")]
        [SerializeField] float speed = 0.17f;
        [SerializeField] float startAngle = 2.2f;

        float m_Time;
        Quaternion[] m_TailRest;
        Quaternion m_WingRest;

        public void Configure(Transform headPart, Transform left, Transform right, Transform[] tailParts)
        {
            head = headPart;
            leftWing = left;
            rightWing = right;
            tail = tailParts;
        }

        void Init()
        {
            if (m_TailRest != null)
                return;
            m_TailRest = new Quaternion[tail.Length];
            for (int i = 0; i < tail.Length; i++)
                m_TailRest[i] = tail[i].localRotation;
            // Wings are modelled spread to the side; they are laid flat for flying.
            m_WingRest = Quaternion.Euler(90f, 0f, 0f);
        }

        void Start() => Step(0f);

        void Update() => Step(Time.deltaTime);

        Vector3 PathPoint(float a) => center + new Vector3(
            Mathf.Sin(a) * radii.x,
            Mathf.Sin(a * 2f + 0.6f) * 2.2f + Mathf.Sin(a * 3f) * 0.8f,
            -Mathf.Cos(a) * radii.y);

        public Vector3 PathPosition(float time) => PathPoint(startAngle + time * speed);

        public void Step(float dt)
        {
            Init();
            m_Time += dt;
            float a = startAngle + m_Time * speed;
            Vector3 p = PathPoint(a);
            Vector3 ahead = PathPoint(a + 0.02f), further = PathPoint(a + 0.12f);
            Vector3 forward = (ahead - p).normalized;
            // Bank into the turn: compare the current heading with the one a little further along.
            Vector3 nextForward = (PathPoint(a + 0.14f) - further).normalized;
            float turn = Vector3.SignedAngle(Vector3.ProjectOnPlane(forward, Vector3.up), Vector3.ProjectOnPlane(nextForward, Vector3.up), Vector3.up);
            float bank = Mathf.Clamp(-turn * 1.6f, -22f, 22f);

            // Flap cycle: about three beats, then a glide with the wings slightly raised.
            float cycle = Mathf.Repeat(m_Time, 7.5f);
            float beating = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(cycle / 0.5f)) * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((3.6f - cycle) / 0.5f));
            float beat = Mathf.Sin(m_Time * Mathf.PI * 2f / 1.2f);
            float flap = Mathf.Lerp(8f + Mathf.Sin(m_Time * 0.9f) * 3f, beat * 34f, beating);
            // Body rises a little on each downstroke.
            p.y += beating * Mathf.Cos(m_Time * Mathf.PI * 2f / 1.2f) * 0.25f;

            transform.localPosition = p;
            transform.localRotation = Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(0f, 0f, bank);
            rightWing.localRotation = Quaternion.Euler(0f, 0f, flap) * m_WingRest;
            leftWing.localRotation = Quaternion.Euler(0f, 0f, -flap) * m_WingRest;
            head.localRotation = Quaternion.Euler(Mathf.Sin(m_Time * 0.7f) * 6f - beating * beat * 3f, -bank * 0.5f + Mathf.Sin(m_Time * 0.4f) * 12f, 0f);
            for (int i = 0; i < tail.Length; i++)
                tail[i].localRotation = m_TailRest[i]
                                        * Quaternion.Euler(Mathf.Sin(m_Time * 1.6f - i * 0.8f) * 7f, Mathf.Sin(m_Time * 0.9f - i * 0.7f) * 9f + bank * 0.3f, 0f);
        }
    }
}
