using UnityEngine;

namespace MagicLand
{
    /// <summary>
    /// Little mouse scurrying in front of the viewer: short runs, sniffing pauses,
    /// and now and then it turns to the viewer and sits up on its hind legs.
    /// </summary>
    public class MouseAgent : MonoBehaviour
    {
        [SerializeField] Transform body;
        [SerializeField] Transform head;
        [SerializeField] Transform earLeft;
        [SerializeField] Transform earRight;
        [SerializeField] Transform[] tail = new Transform[0];
        [Tooltip("Front left, front right, back left, back right.")]
        [SerializeField] Transform[] feet = new Transform[4];
        [SerializeField] Vector2 areaX = new Vector2(-1.6f, 1.6f);
        [SerializeField] Vector2 areaZ = new Vector2(1.6f, 3.4f);
        [SerializeField] float runSpeed = 0.32f;
        [SerializeField] float turnSpeed = 220f;
        [SerializeField] float stride = 0.07f;
        [SerializeField] int seed = 5;
        [Tooltip("A visitor stays hidden until it is called to meet the other mouse.")]
        [SerializeField] bool visitor;

        enum State { Run, Sniff, SitUp, Meet, Kiss }

        static readonly float[] s_FootPhase = { 0f, 0.5f, 0.5f, 0f };

        State m_State;
        float m_StateTime;
        float m_StateDuration;
        Vector3 m_Target;
        float m_Speed;
        float m_GaitPhase;
        float m_Rear;
        float m_HeadPitch;
        float m_HeadYaw;
        float m_Time;
        float m_EarTwitch;
        float m_Lean;
        float m_NextTwitch;
        Quaternion m_BodyRest, m_HeadRest, m_EarLeftRest, m_EarRightRest;
        Quaternion[] m_TailRest;
        Vector3[] m_FootRest;
        System.Random m_Random;
        bool m_Initialized;
        bool m_Hidden;
        bool m_Leaving;
        Vector3 m_Partner;
        Vector3 m_KissFrom;
        Renderer[] m_Renderers;

        /// <summary>True once the mouse stands at its meeting spot, facing its partner.</summary>
        public bool Arrived { get; private set; }
        public bool Hidden => m_Hidden;

        public void Configure(Transform bodyPivot, Transform headPivot, Transform earL, Transform earR,
            Transform[] tailSegments, Transform[] footPivots, Vector2 x, Vector2 z, int randomSeed)
        {
            body = bodyPivot;
            head = headPivot;
            earLeft = earL;
            earRight = earR;
            tail = tailSegments;
            feet = footPivots;
            areaX = x;
            areaZ = z;
            seed = randomSeed;
        }

        public void MakeVisitor() => visitor = true;

        void Init()
        {
            m_Random = new System.Random(seed);
            m_BodyRest = body.localRotation;
            m_HeadRest = head.localRotation;
            m_EarLeftRest = earLeft.localRotation;
            m_EarRightRest = earRight.localRotation;
            m_TailRest = new Quaternion[tail.Length];
            for (int i = 0; i < tail.Length; i++)
                m_TailRest[i] = tail[i].localRotation;
            m_FootRest = new Vector3[feet.Length];
            for (int i = 0; i < feet.Length; i++)
                m_FootRest[i] = feet[i].localPosition;
            m_Renderers = GetComponentsInChildren<Renderer>(true);
            EnterState(State.Sniff);
            m_Initialized = true;
            if (visitor)
                SetHidden(true);
        }

        void SetHidden(bool hidden)
        {
            m_Hidden = hidden;
            foreach (Renderer r in m_Renderers)
                r.enabled = !hidden;
        }

        /// <summary>Walks to a spot, then turns to face the partner and waits there.</summary>
        public void Meet(Vector3 spot, Vector3 partner)
        {
            if (!m_Initialized)
                Init();
            m_Leaving = false;
            m_Partner = partner;
            m_Target = spot;
            Arrived = false;
            m_State = State.Meet;
            m_StateTime = 0f;
        }

        /// <summary>Shows a hidden visitor at the given place, already turned toward where it is heading.</summary>
        public void Appear(Vector3 at, Vector3 heading)
        {
            if (!m_Initialized)
                Init();
            at.y = ForestGround.HeightAt(at.x, at.z);
            transform.localPosition = at;
            Vector3 dir = heading - at;
            dir.y = 0f;
            transform.localRotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            m_Speed = 0f;
            SetHidden(false);
        }

        /// <summary>Leans in to touch noses, ending exactly at the given point.</summary>
        public void Kiss(Vector3 point)
        {
            m_KissFrom = transform.localPosition;
            m_Target = point;
            m_Speed = 0f;
            m_State = State.Kiss;
            m_StateTime = 0f;
        }

        /// <summary>Goes back to wandering around (resident) or runs away and hides (visitor).</summary>
        public void Release(Vector3 exit)
        {
            Arrived = false;
            if (!visitor)
            {
                EnterState(State.Sniff);
                return;
            }
            m_Leaving = true;
            m_Target = exit;
            m_State = State.Meet;
            m_StateTime = 0f;
        }

        /// <summary>Turns toward a direction and runs to the target; returns the wanted speed.</summary>
        float RunTo(Vector3 pos, Vector3 target, float speed, float dt)
        {
            Vector3 to = target - pos;
            to.y = 0f;
            var wanted = Quaternion.LookRotation(to.normalized, Vector3.up);
            transform.localRotation = Quaternion.RotateTowards(transform.localRotation, wanted, turnSpeed * dt);
            float facing = Vector3.Dot(transform.localRotation * Vector3.forward, to.normalized);
            return speed * Mathf.Clamp01(facing) * Mathf.Clamp01(to.magnitude / 0.25f + 0.3f);
        }

        bool FacePartner(Vector3 pos, float dt)
        {
            Vector3 to = m_Partner - pos;
            to.y = 0f;
            if (to.sqrMagnitude < 0.0001f)
                return true;
            var wanted = Quaternion.LookRotation(to.normalized, Vector3.up);
            transform.localRotation = Quaternion.RotateTowards(transform.localRotation, wanted, turnSpeed * 0.7f * dt);
            return Quaternion.Angle(transform.localRotation, wanted) < 6f;
        }

        float Range(float min, float max) => min + (float)m_Random.NextDouble() * (max - min);

        void Update() => Step(Time.deltaTime);

        public void Step(float dt)
        {
            if (!m_Initialized)
                Init();

            if (m_Hidden)
                return;
            m_Time += dt;
            m_StateTime += dt;
            Vector3 pos = transform.localPosition;
            float targetLean = 0f;

            float targetSpeed = 0f;
            float targetRear = 0f;
            float targetPitch = 0f;
            float targetYaw = 0f;

            switch (m_State)
            {
                case State.Run:
                {
                    Vector3 to = m_Target - pos;
                    to.y = 0f;
                    if (to.magnitude < 0.08f || m_StateTime > 12f)
                    {
                        NextState();
                        break;
                    }
                    targetSpeed = RunTo(pos, m_Target, runSpeed, dt);
                    targetPitch = 8f;
                    break;
                }
                case State.Meet:
                {
                    Vector3 to = m_Target - pos;
                    to.y = 0f;
                    if (to.magnitude > 0.06f)
                    {
                        Arrived = false;
                        targetSpeed = RunTo(pos, m_Target, runSpeed * 1.2f, dt);
                        targetPitch = 8f;
                    }
                    else if (m_Leaving)
                    {
                        m_Leaving = false;
                        m_Speed = 0f;
                        SetHidden(true);
                        EnterState(State.Sniff);
                        return;
                    }
                    else
                    {
                        Arrived = FacePartner(pos, dt) && m_Speed < 0.02f;
                        targetPitch = 10f + Mathf.Sin(m_Time * 15f) * 3f;
                    }
                    break;
                }
                case State.Kiss:
                {
                    // Small step forward until the noses touch, then a little nuzzle.
                    m_Speed = 0f;
                    Vector3 p = Vector3.Lerp(m_KissFrom, m_Target, Mathf.SmoothStep(0f, 1f, m_StateTime / 0.7f));
                    pos.x = p.x;
                    pos.z = p.z;
                    FacePartner(pos, dt);
                    targetLean = 1f;
                    targetPitch = -10f + Mathf.Sin(Mathf.Max(0f, m_StateTime - 0.7f) * 9f) * 5f;
                    break;
                }
                case State.Sniff:
                    // Quick little nose bobs while looking around.
                    targetPitch = 14f + Mathf.Sin(m_Time * 17f) * 4f;
                    targetYaw = Mathf.Sin(m_Time * 0.9f + seed) * 35f;
                    break;
                case State.SitUp:
                {
                    // Turns towards the viewer (content origin), then sits up and looks at them.
                    Vector3 toViewer = -pos;
                    toViewer.y = 0f;
                    var wanted = Quaternion.LookRotation(toViewer.normalized, Vector3.up);
                    transform.localRotation = Quaternion.RotateTowards(transform.localRotation, wanted, turnSpeed * 0.6f * dt);
                    bool facingViewer = Quaternion.Angle(transform.localRotation, wanted) < 10f;
                    targetRear = facingViewer ? 1f : 0f;
                    float lookUp = Mathf.Atan2(-pos.y, toViewer.magnitude) * Mathf.Rad2Deg;
                    targetPitch = Mathf.Lerp(0f, 55f - lookUp, m_Rear) + Mathf.Sin(m_Time * 15f) * 2f * m_Rear;
                    targetYaw = Mathf.Sin(m_Time * 0.7f) * 12f * m_Rear;
                    if (!facingViewer)
                        m_StateTime = Mathf.Min(m_StateTime, 0.5f);
                    break;
                }
            }

            if ((m_State == State.Sniff || m_State == State.SitUp) && m_StateTime > m_StateDuration)
                NextState();

            m_Speed = Mathf.MoveTowards(m_Speed, targetSpeed, dt * 1.6f);
            pos += transform.localRotation * Vector3.forward * (m_Speed * dt);
            pos.y = ForestGround.HeightAt(pos.x, pos.z);
            transform.localPosition = pos;

            float scale = Mathf.Max(0.01f, transform.localScale.x);
            m_GaitPhase += m_Speed * dt / (stride * scale) * Mathf.PI * 2f;
            float gait = Mathf.Clamp01(m_Speed / (runSpeed * 0.3f));

            float smooth = 1f - Mathf.Exp(-6f * dt);
            m_Rear = Mathf.Lerp(m_Rear, targetRear, 1f - Mathf.Exp(-3f * dt));
            m_HeadPitch = Mathf.Lerp(m_HeadPitch, targetPitch, smooth);
            m_HeadYaw = Mathf.Lerp(m_HeadYaw, targetYaw, smooth);
            m_Lean = Mathf.Lerp(m_Lean, targetLean, smooth);

            float bob = Mathf.Abs(Mathf.Sin(m_GaitPhase)) * 4f * gait;
            body.localRotation = m_BodyRest * Quaternion.Euler(-62f * m_Rear - bob + 8f * m_Lean, 0f, 0f);
            head.localRotation = m_HeadRest * Quaternion.Euler(0f, m_HeadYaw, 0f) * Quaternion.Euler(m_HeadPitch, 0f, 0f);

            for (int i = 0; i < feet.Length && i < s_FootPhase.Length; i++)
            {
                float p = m_GaitPhase + s_FootPhase[i] * Mathf.PI * 2f;
                feet[i].localPosition = m_FootRest[i] + new Vector3(0f, Mathf.Max(0f, Mathf.Cos(p)) * 0.012f, Mathf.Sin(p) * 0.018f) * gait;
            }

            for (int i = 0; i < tail.Length; i++)
            {
                float wave = Mathf.Sin(m_Time * (2.2f + m_Speed * 12f) - i * 0.9f) * (10f + 8f * gait);
                tail[i].localRotation = m_TailRest[i] * Quaternion.Euler(0f, wave, 0f);
            }

            if (m_Time > m_NextTwitch)
            {
                m_EarTwitch = 1f;
                m_NextTwitch = m_Time + Range(1f, 4f);
            }
            m_EarTwitch = Mathf.Max(0f, m_EarTwitch - dt * 4f);
            float twitch = Mathf.Sin(m_EarTwitch * Mathf.PI * 3f) * m_EarTwitch * 18f;
            earLeft.localRotation = m_EarLeftRest * Quaternion.Euler(twitch - 25f * m_Lean, 0f, 0f);
            earRight.localRotation = m_EarRightRest * Quaternion.Euler(-twitch * 0.6f - 25f * m_Lean, 0f, 0f);
        }

        void NextState()
        {
            double r = m_Random.NextDouble();
            if (m_State == State.Run)
                EnterState(r < 0.7 ? State.Sniff : State.SitUp);
            else
                EnterState(r < 0.75 ? State.Run : (m_State == State.Sniff ? State.SitUp : State.Sniff));
        }

        void EnterState(State state)
        {
            m_State = state;
            m_StateTime = 0f;
            switch (state)
            {
                case State.Run:
                    m_Target = PickTarget();
                    break;
                case State.Sniff:
                    m_StateDuration = Range(1.2f, 3.5f);
                    break;
                case State.SitUp:
                    m_StateDuration = Range(2.5f, 4.5f);
                    break;
            }
        }

        Vector3 PickTarget()
        {
            Vector3 pos = transform.localPosition;
            for (int attempt = 0; attempt < 10; attempt++)
            {
                var candidate = new Vector3(Range(areaX.x, areaX.y), 0f, Range(areaZ.x, areaZ.y));
                float d = (candidate - new Vector3(pos.x, 0f, pos.z)).magnitude;
                if ((d > 0.4f && d < 1.6f) || attempt == 9)
                    return candidate;
            }
            return pos;
        }
    }
}
