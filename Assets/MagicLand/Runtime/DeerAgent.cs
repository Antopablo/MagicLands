using UnityEngine;

namespace MagicLand
{
    /// <summary>
    /// Calm deer behaviour: slowly walks between random spots of the clearing, grazes and looks around.
    /// Legs, neck, head and ears are animated procedurally from their rest pose.
    /// </summary>
    public class DeerAgent : MonoBehaviour
    {
        [SerializeField] Transform body;
        [SerializeField] Transform neck;
        [SerializeField] Transform head;
        [SerializeField] Transform earLeft;
        [SerializeField] Transform earRight;
        [Tooltip("Front left, front right, back left, back right.")]
        [SerializeField] Transform[] legs = new Transform[4];
        [SerializeField] Vector2 areaX = new Vector2(-6f, 6f);
        [SerializeField] Vector2 areaZ = new Vector2(6f, 13f);
        [SerializeField] float walkSpeed = 0.45f;
        [SerializeField] float turnSpeed = 35f;
        [SerializeField] float stride = 1.1f;
        [SerializeField] int seed = 1;
        [Tooltip("Optional: stays close to this deer (a fawn following its mother).")]
        [SerializeField] DeerAgent follow;

        enum State { Walk, Graze, Look }

        // Phase offsets per leg (front left, front right, back left, back right) for a lateral-sequence walk.
        static readonly float[] s_LegPhase = { 0.25f, 0.75f, 0f, 0.5f };

        State m_State;
        float m_StateTime;
        float m_StateDuration;
        Vector3 m_Target;
        float m_Speed;
        float m_GaitPhase;
        float m_NeckAngle;
        float m_HeadAngle;
        float m_Yaw;
        float m_EarTwitch;
        float m_NextTwitch;
        float m_Time;
        Quaternion[] m_LegRest;
        Quaternion m_NeckRest, m_HeadRest, m_EarLeftRest, m_EarRightRest;
        Vector3 m_BodyRest;
        System.Random m_Random;
        bool m_Initialized;

        public void Configure(Transform bodyPivot, Transform neckPivot, Transform headPivot, Transform earL, Transform earR,
            Transform[] legPivots, Vector2 x, Vector2 z, int randomSeed, DeerAgent followTarget)
        {
            body = bodyPivot;
            neck = neckPivot;
            head = headPivot;
            earLeft = earL;
            earRight = earR;
            legs = legPivots;
            areaX = x;
            areaZ = z;
            seed = randomSeed;
            follow = followTarget;
        }

        void Init()
        {
            m_Random = new System.Random(seed);
            m_LegRest = new Quaternion[legs.Length];
            for (int i = 0; i < legs.Length; i++)
                m_LegRest[i] = legs[i].localRotation;
            m_NeckRest = neck.localRotation;
            m_HeadRest = head.localRotation;
            m_EarLeftRest = earLeft.localRotation;
            m_EarRightRest = earRight.localRotation;
            m_BodyRest = body.localPosition;
            EnterState(m_Random.NextDouble() < 0.5 ? State.Graze : State.Walk);
            m_Initialized = true;
        }

        float Range(float min, float max) => min + (float)m_Random.NextDouble() * (max - min);

        void Update() => Step(Time.deltaTime);

        public void Step(float dt)
        {
            if (!m_Initialized)
                Init();

            m_Time += dt;
            m_StateTime += dt;

            float targetSpeed = 0f;
            float targetNeck = 0f;
            float targetHead = 0f;
            float targetYaw = 0f;
            Vector3 pos = transform.localPosition;

            switch (m_State)
            {
                case State.Walk:
                {
                    Vector3 to = m_Target - pos;
                    to.y = 0f;
                    if (to.magnitude < 0.4f || m_StateTime > 40f)
                    {
                        NextState();
                        break;
                    }
                    var wanted = Quaternion.LookRotation(to.normalized, Vector3.up);
                    transform.localRotation = Quaternion.RotateTowards(transform.localRotation, wanted, turnSpeed * dt);
                    float facing = Vector3.Dot(transform.localRotation * Vector3.forward, to.normalized);
                    targetSpeed = walkSpeed * Mathf.Lerp(0.35f, 1f, Mathf.Clamp01(facing));
                    targetNeck = 5f;
                    break;
                }
                case State.Graze:
                    targetNeck = 112f;
                    targetHead = -25f + Mathf.Sin(m_Time * 5f) * 4f;
                    break;
                case State.Look:
                {
                    // Gently turns the head towards the viewer (content origin).
                    Vector3 toViewer = transform.InverseTransformPoint(transform.parent != null ? transform.parent.position : Vector3.zero);
                    targetYaw = Mathf.Clamp(Mathf.Atan2(toViewer.x, toViewer.z) * Mathf.Rad2Deg, -55f, 55f);
                    targetNeck = -10f;
                    targetHead = -10f;
                    break;
                }
            }

            if (m_State != State.Walk && m_StateTime > m_StateDuration)
                NextState();

            m_Speed = Mathf.MoveTowards(m_Speed, targetSpeed, dt * 0.6f);
            pos += transform.localRotation * Vector3.forward * (m_Speed * dt);
            pos.y = ForestGround.HeightAt(pos.x, pos.z);
            transform.localPosition = pos;

            float scale = Mathf.Max(0.01f, transform.localScale.x);
            m_GaitPhase += m_Speed * dt / (stride * scale) * Mathf.PI * 2f;
            float gait = Mathf.Clamp01(m_Speed / (walkSpeed * 0.4f));

            for (int i = 0; i < legs.Length && i < s_LegPhase.Length; i++)
            {
                float angle = Mathf.Sin(m_GaitPhase + s_LegPhase[i] * Mathf.PI * 2f) * 26f * gait;
                legs[i].localRotation = m_LegRest[i] * Quaternion.Euler(angle, 0f, 0f);
            }
            body.localPosition = m_BodyRest + Vector3.up * (Mathf.Sin(m_GaitPhase * 2f) * 0.012f * gait);

            float smooth = 1f - Mathf.Exp(-2.2f * dt);
            m_NeckAngle = Mathf.Lerp(m_NeckAngle, targetNeck, smooth);
            m_HeadAngle = Mathf.Lerp(m_HeadAngle, targetHead, smooth);
            m_Yaw = Mathf.Lerp(m_Yaw, targetYaw, smooth);
            neck.localRotation = Quaternion.Euler(0f, m_Yaw * 0.5f, 0f) * m_NeckRest * Quaternion.Euler(m_NeckAngle, 0f, 0f);
            head.localRotation = m_HeadRest * Quaternion.Euler(m_HeadAngle, m_Yaw * 0.5f, 0f);

            if (m_Time > m_NextTwitch)
            {
                m_EarTwitch = 1f;
                m_NextTwitch = m_Time + Range(1.5f, 6f);
            }
            m_EarTwitch = Mathf.Max(0f, m_EarTwitch - dt * 3f);
            float twitch = Mathf.Sin(m_EarTwitch * Mathf.PI * 3f) * m_EarTwitch * 20f;
            earLeft.localRotation = m_EarLeftRest * Quaternion.Euler(twitch, 0f, 0f);
            earRight.localRotation = m_EarRightRest * Quaternion.Euler(-twitch * 0.5f, 0f, 0f);
        }

        void NextState()
        {
            double r = m_Random.NextDouble();
            if (m_State == State.Walk)
                EnterState(r < 0.6 ? State.Graze : State.Look);
            else
                EnterState(r < 0.55 ? State.Walk : (m_State == State.Graze ? State.Look : State.Graze));
        }

        void EnterState(State state)
        {
            m_State = state;
            m_StateTime = 0f;
            switch (state)
            {
                case State.Walk:
                    m_Target = PickTarget();
                    break;
                case State.Graze:
                    m_StateDuration = Range(6f, 14f);
                    break;
                case State.Look:
                    m_StateDuration = Range(3f, 6f);
                    break;
            }
        }

        Vector3 PickTarget()
        {
            Vector3 pos = transform.localPosition;
            for (int attempt = 0; attempt < 10; attempt++)
            {
                Vector3 candidate;
                if (follow != null)
                {
                    Vector3 lead = follow.transform.localPosition;
                    candidate = new Vector3(lead.x + Range(-1.6f, 1.6f), 0f, lead.z + Range(-1.6f, 1.6f));
                }
                else
                {
                    candidate = new Vector3(Range(areaX.x, areaX.y), 0f, Range(areaZ.x, areaZ.y));
                }
                candidate.x = Mathf.Clamp(candidate.x, areaX.x, areaX.y);
                candidate.z = Mathf.Clamp(candidate.z, areaZ.x, areaZ.y);
                if ((candidate - new Vector3(pos.x, 0f, pos.z)).magnitude > 1.5f || attempt == 9)
                    return candidate;
            }
            return pos;
        }
    }
}
