using UnityEngine;

namespace MagicLand
{
    /// <summary>Enters from the forest, strolls with the family, visits the rainbow and walks away again.</summary>
    public class UnicornAgent : MonoBehaviour
    {
        [SerializeField] Transform body;
        [SerializeField] Transform[] legs = new Transform[4];
        [SerializeField] Transform wingLeft;
        [SerializeField] Transform wingRight;
        [SerializeField] float scale = 1f;
        [SerializeField] float laneOffset;
        [SerializeField] int seed = 1;
        [SerializeField] float entranceDelay = 132f;
        [SerializeField] float firstFlightTime = 180f;

        enum State { Hidden, Entering, Stroll, FlyOut, AtRainbow, FlyBack, Leaving }

        const float WalkDuration = 26f;
        const float WalkBeforeFlight = 48f;
        static readonly float[] LegPhases = { 0.25f, 0.75f, 0f, 0.5f };

        State m_State;
        float m_Time;
        float m_NextEntrance;
        float m_NextFlight;
        float m_StateTime;
        float m_StateDuration;
        float m_Gait;
        float m_Yaw;
        float m_WalkPhase;
        float m_WalkBlend;
        float m_FlyBlend;
        Vector3 m_Entrance;
        Vector3 m_From, m_ControlA, m_ControlB, m_To;
        Quaternion[] m_LegRest;
        Quaternion m_WingLeftRest;
        Quaternion m_WingRightRest;
        Vector3 m_BodyRest;
        Renderer[] m_Renderers;
        bool m_Initialized;

        public void Configure(Transform bodyPivot, Transform[] legPivots, Transform leftWing, Transform rightWing,
            float characterScale, float offset, int randomSeed, float rainbowReadyAt, float familyDelay)
        {
            body = bodyPivot;
            legs = legPivots;
            wingLeft = leftWing;
            wingRight = rightWing;
            scale = characterScale;
            laneOffset = offset;
            seed = randomSeed;
            entranceDelay = Mathf.Max(8f, rainbowReadyAt - WalkBeforeFlight) + familyDelay;
            firstFlightTime = Mathf.Max(rainbowReadyAt + familyDelay, entranceDelay + WalkBeforeFlight);
        }

        /// <summary>Seconds before the family walks in again (0 while visible).</summary>
        public float QuietTime => !m_Initialized ? entranceDelay
            : m_State == State.Hidden ? Mathf.Max(0f, m_NextEntrance - m_Time) : 0f;

        void Init()
        {
            m_LegRest = new Quaternion[legs.Length];
            for (int i = 0; i < legs.Length; i++)
                m_LegRest[i] = legs[i].localRotation;
            m_WingLeftRest = wingLeft.localRotation;
            m_WingRightRest = wingRight.localRotation;
            m_BodyRest = body.localPosition;
            m_Renderers = GetComponentsInChildren<Renderer>(true);
            m_Entrance = transform.localPosition;
            m_Yaw = transform.localEulerAngles.y;
            m_NextEntrance = entranceDelay;
            m_NextFlight = firstFlightTime;
            SetVisible(false);
            m_Initialized = true;
        }

        void SetVisible(bool visible)
        {
            foreach (Renderer renderer in m_Renderers)
                renderer.enabled = visible;
        }

        Vector3 LandingPoint() => OnGround(new Vector3(3f, 0f, 12f + laneOffset));

        void BeginWalk(bool leaving)
        {
            m_State = leaving ? State.Leaving : State.Entering;
            m_StateTime = 0f;
            m_StateDuration = WalkDuration;
            m_From = transform.localPosition;
            m_ControlA = leaving ? new Vector3(3f, 0f, 10.5f + laneOffset) : new Vector3(9f, 0f, 13.5f + laneOffset);
            m_ControlB = leaving ? new Vector3(9f, 0f, 13.5f + laneOffset) : new Vector3(3f, 0f, 10.5f + laneOffset);
            m_To = leaving ? m_Entrance : LandingPoint();
        }

        void BeginFlight(bool returning)
        {
            m_State = returning ? State.FlyBack : State.FlyOut;
            m_StateTime = 0f;
            m_StateDuration = returning ? 28f : 30f;
            m_From = transform.localPosition;
            m_To = returning
                ? LandingPoint()
                : new Vector3(-3f + seed * 1.6f, 23.5f, 59f);
            m_ControlA = returning
                ? m_From + new Vector3(-5f, 3f, -12f)
                : m_From + transform.localRotation * Vector3.forward * 4f + Vector3.up * 11f;
            m_ControlB = returning
                ? m_To + new Vector3(0f, 3f, 7f)
                : m_To + new Vector3(0f, 3f, -12f);
        }

        void Update() => Step(Time.deltaTime);

        public void Step(float dt)
        {
            if (!m_Initialized)
                Init();
            m_Time += dt;
            m_StateTime += dt;
            Vector3 previous = transform.localPosition;
            bool walking = false;

            switch (m_State)
            {
                case State.Hidden:
                {
                    if (m_Time >= m_NextEntrance)
                    {
                        SetVisible(true);
                        BeginWalk(false);
                    }
                    return;
                }
                case State.Entering:
                case State.Leaving:
                {
                    float t = Mathf.Clamp01(m_StateTime / m_StateDuration);
                    transform.localPosition = OnGround(Bezier(t));
                    walking = true;
                    if (t >= 1f)
                    {
                        if (m_State == State.Entering)
                        {
                            m_State = State.Stroll;
                            m_WalkPhase = 0f;
                        }
                        else
                        {
                            SetVisible(false);
                            m_State = State.Hidden;
                            m_NextEntrance = m_Time + 55f;
                            m_NextFlight = m_NextEntrance + WalkBeforeFlight;
                        }
                        m_StateTime = 0f;
                    }
                    break;
                }
                case State.Stroll:
                {
                    m_WalkPhase += dt * 0.2f;
                    transform.localPosition = OnGround(new Vector3(3f * Mathf.Cos(m_WalkPhase), 0f,
                        12f + laneOffset + 1.5f * Mathf.Sin(m_WalkPhase)));
                    walking = true;
                    if (m_Time >= m_NextFlight)
                        BeginFlight(false);
                    break;
                }
                case State.FlyOut:
                case State.FlyBack:
                {
                    float t = Mathf.Clamp01(m_StateTime / m_StateDuration);
                    transform.localPosition = Bezier(Mathf.SmoothStep(0f, 1f, t));
                    if (t >= 1f)
                    {
                        if (m_State == State.FlyOut)
                        {
                            m_State = State.AtRainbow;
                            m_StateTime = 0f;
                            m_StateDuration = 5f;
                        }
                        else
                        {
                            BeginWalk(true);
                        }
                    }
                    break;
                }
                case State.AtRainbow:
                {
                    transform.localPosition = m_To + Vector3.up * (Mathf.Sin(m_StateTime * 1.2f) * 0.12f);
                    if (m_StateTime >= m_StateDuration)
                        BeginFlight(true);
                    break;
                }
            }

            Vector3 travel = transform.localPosition - previous;
            travel.y = 0f;
            if (travel.sqrMagnitude > 1e-8f)
            {
                m_Yaw = Mathf.MoveTowardsAngle(m_Yaw, Mathf.Atan2(travel.x, travel.z) * Mathf.Rad2Deg, 65f * dt);
                transform.localRotation = Quaternion.Euler(0f, m_Yaw, 0f);
            }
            float distance = travel.magnitude;
            if (walking)
                m_Gait += distance / (0.85f * scale) * Mathf.PI * 2f;
            float walkTarget = walking && dt > 0f ? Mathf.Clamp01(distance / dt / 0.35f) : 0f;
            m_WalkBlend = Mathf.MoveTowards(m_WalkBlend, walkTarget, dt * 3f);
            bool flying = m_State == State.FlyOut || m_State == State.AtRainbow || m_State == State.FlyBack;
            m_FlyBlend = Mathf.MoveTowards(m_FlyBlend, flying ? 1f : 0f, dt * 1.5f);

            for (int i = 0; i < legs.Length; i++)
            {
                float gait = Mathf.Sin(m_Gait + LegPhases[i] * Mathf.PI * 2f) * 23f * m_WalkBlend;
                legs[i].localRotation = m_LegRest[i] * Quaternion.Euler(gait + 18f * m_FlyBlend, 0f, 0f);
            }
            body.localPosition = m_BodyRest + Vector3.up * (Mathf.Sin(m_Gait * 2f) * 0.012f * m_WalkBlend);

            float flap = Mathf.Sin(m_Time * 8f + seed * 0.7f) * 42f;
            wingLeft.localRotation = Quaternion.Slerp(m_WingLeftRest, Quaternion.Euler(0f, 0f, -flap), m_FlyBlend);
            wingRight.localRotation = Quaternion.Slerp(m_WingRightRest, Quaternion.Euler(0f, 0f, flap), m_FlyBlend);
        }

        Vector3 Bezier(float t)
        {
            float u = 1f - t;
            return u * u * u * m_From + 3f * u * u * t * m_ControlA
                + 3f * u * t * t * m_ControlB + t * t * t * m_To;
        }

        static Vector3 OnGround(Vector3 p)
        {
            p.y = ForestGround.HeightAt(p.x, p.z);
            return p;
        }
    }
}
