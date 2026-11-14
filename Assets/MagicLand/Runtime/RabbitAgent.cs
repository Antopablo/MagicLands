using System.Collections.Generic;
using UnityEngine;

namespace MagicLand
{
    /// <summary>
    /// Rabbit that now and then hops across the clearing in front of the viewer: it comes out of the
    /// forest on one side, stops to nibble, sit up or look around, then hops away to the other side.
    /// Two rabbits with the same seed and a small delay make a mother followed by her little one.
    /// </summary>
    public class RabbitAgent : MonoBehaviour
    {
        [SerializeField] Transform body;
        [SerializeField] Transform head;
        [SerializeField] Transform earLeft;
        [SerializeField] Transform earRight;
        [SerializeField] Transform tail;
        [SerializeField] Transform[] frontLegs = new Transform[2];
        [SerializeField] Transform[] hindLegs = new Transform[2];
        [Tooltip("Seconds before the first crossing.")]
        [SerializeField] float startDelay = 8f;
        [Tooltip("Seconds hidden in the forest between two crossings.")]
        [SerializeField] Vector2 waitRange = new Vector2(20f, 45f);
        [SerializeField] Vector2 laneZ = new Vector2(3.8f, 6.2f);
        [SerializeField] float exitX = 14f;
        [Tooltip("Shifts the whole route along Z (used by the little one).")]
        [SerializeField] float lateralOffset;
        [SerializeField] int seed = 41;

        enum State { Hidden, Travel, Nibble, SitUp, Look }

        State m_State;
        float m_StateTime;
        float m_StateDuration;
        readonly List<Vector3> m_Route = new List<Vector3>();
        int m_RouteIndex;
        readonly Queue<State> m_Pauses = new Queue<State>();
        float m_Yaw;
        float m_HopT = -1f;
        float m_HopDuration;
        Vector3 m_HopFrom, m_HopTo;
        float m_Rest;
        float m_Pitch;
        float m_HeadPitch;
        float m_HeadYaw;
        float m_Time;
        float m_EarTwitch, m_NextTwitch;
        float m_TailWiggle, m_NextWiggle;
        Quaternion m_EarLeftRest, m_EarRightRest, m_HeadRest, m_TailRest;
        Quaternion[] m_FrontRest, m_HindRest;
        Renderer[] m_Renderers;
        System.Random m_Random;
        // Separate stream for cosmetic twitches so two rabbits with the same seed keep the same route.
        System.Random m_FxRandom;
        bool m_Initialized;

        public bool Visible => m_State != State.Hidden;

        /// <summary>Seconds before this rabbit shows up again (0 while it is visible).</summary>
        public float QuietTime => !m_Initialized ? startDelay
            : m_State == State.Hidden ? Mathf.Max(0f, m_StateDuration - m_StateTime) : 0f;

        /// <summary>While held, a hidden rabbit stays in the forest.</summary>
        public bool Held { get; set; }

        public void Configure(Transform bodyPivot, Transform headPivot, Transform earL, Transform earR, Transform tailPivot,
            Transform[] front, Transform[] hind, float delay, float offset, int randomSeed)
        {
            body = bodyPivot;
            head = headPivot;
            earLeft = earL;
            earRight = earR;
            tail = tailPivot;
            frontLegs = front;
            hindLegs = hind;
            startDelay = delay;
            lateralOffset = offset;
            seed = randomSeed;
        }

        float Range(float min, float max) => min + (float)m_Random.NextDouble() * (max - min);

        void Init()
        {
            m_Random = new System.Random(seed);
            m_FxRandom = new System.Random(seed * 31 + (int)(transform.localScale.x * 1000f));
            m_EarLeftRest = earLeft.localRotation;
            m_EarRightRest = earRight.localRotation;
            m_HeadRest = head.localRotation;
            m_TailRest = tail.localRotation;
            m_FrontRest = new Quaternion[frontLegs.Length];
            for (int i = 0; i < frontLegs.Length; i++)
                m_FrontRest[i] = frontLegs[i].localRotation;
            m_HindRest = new Quaternion[hindLegs.Length];
            for (int i = 0; i < hindLegs.Length; i++)
                m_HindRest[i] = hindLegs[i].localRotation;
            m_Renderers = GetComponentsInChildren<Renderer>(true);
            Hide(startDelay);
            m_Initialized = true;
        }

        void Update() => Step(Time.deltaTime);

        void Hide(float duration)
        {
            m_State = State.Hidden;
            m_StateTime = 0f;
            m_StateDuration = duration;
            foreach (var r in m_Renderers)
                r.enabled = false;
        }

        void PlanTrip()
        {
            float side = m_Random.NextDouble() < 0.5 ? -1f : 1f;
            m_Route.Clear();
            m_Route.Add(Lane(side * exitX));
            m_Route.Add(Lane(side * Range(2.5f, 4f)));
            if (m_Random.NextDouble() < 0.6)
                m_Route.Add(Lane(side * Range(-1.2f, 1.2f)));
            m_Route.Add(Lane(-side * Range(2.5f, 4f)));
            m_Route.Add(Lane(-side * exitX));
            m_RouteIndex = 1;

            Vector3 start = m_Route[0];
            transform.localPosition = new Vector3(start.x, ForestGround.HeightAt(start.x, start.z), start.z);
            m_Yaw = Mathf.Atan2(m_Route[1].x - start.x, m_Route[1].z - start.z) * Mathf.Rad2Deg;
            m_HopT = -1f;
            m_Rest = 0f;
            m_State = State.Travel;
            m_StateTime = 0f;
            foreach (var r in m_Renderers)
                r.enabled = true;
        }

        Vector3 Lane(float x) => new Vector3(x, 0f, Range(laneZ.x, laneZ.y) + lateralOffset);

        public void Step(float dt)
        {
            if (!m_Initialized)
                Init();
            m_Time += dt;
            m_StateTime += dt;

            float targetPitch = 0f, targetHeadPitch = 0f, targetHeadYaw = 0f;
            float hopP = 0f;
            float frontTuck = 0f;

            switch (m_State)
            {
                case State.Hidden:
                    if (Held)
                        m_StateTime -= dt;
                    else if (m_StateTime >= m_StateDuration)
                        PlanTrip();
                    return;

                case State.Travel:
                    hopP = StepTravel(dt);
                    targetHeadPitch = 5f;
                    break;

                case State.Nibble:
                    targetPitch = 10f;
                    targetHeadPitch = 42f + Mathf.Sin(m_Time * 14f) * 3f;
                    break;

                case State.SitUp:
                    targetPitch = -55f;
                    targetHeadPitch = 42f;
                    targetHeadYaw = Mathf.Sin(m_Time * 0.8f) * 30f;
                    frontTuck = 1f;
                    break;

                case State.Look:
                {
                    Vector3 p = transform.localPosition;
                    float toViewer = Mathf.Atan2(-p.x, -p.z) * Mathf.Rad2Deg;
                    targetHeadYaw = Mathf.Clamp(Mathf.DeltaAngle(m_Yaw, toViewer), -60f, 60f);
                    targetHeadPitch = -6f + Mathf.Sin(m_Time * 18f) * 1.5f;
                    break;
                }
            }

            if (m_State != State.Travel && m_StateTime >= m_StateDuration)
                NextPause();

            float smooth = 1f - Mathf.Exp(-5f * dt);
            m_Pitch = Mathf.Lerp(m_Pitch, targetPitch, smooth);
            m_HeadPitch = Mathf.Lerp(m_HeadPitch, targetHeadPitch, smooth);
            m_HeadYaw = Mathf.Lerp(m_HeadYaw, targetHeadYaw, smooth);

            float arc = Mathf.Sin(hopP * Mathf.PI);
            float hopPitch = -15f * Mathf.Sin(hopP * Mathf.PI * 2f);
            transform.localRotation = Quaternion.Euler(0f, m_Yaw, 0f);
            body.localRotation = Quaternion.Euler(m_Pitch + hopPitch, 0f, 0f);
            head.localRotation = m_HeadRest * Quaternion.Euler(0f, m_HeadYaw, 0f) * Quaternion.Euler(m_HeadPitch - m_Pitch * 0.3f, 0f, 0f);

            float push = Mathf.Sin(Mathf.PI * Mathf.Clamp01(hopP * 1.4f));
            for (int i = 0; i < hindLegs.Length; i++)
                hindLegs[i].localRotation = m_HindRest[i] * Quaternion.Euler(55f * push, 0f, 0f);
            for (int i = 0; i < frontLegs.Length; i++)
                frontLegs[i].localRotation = m_FrontRest[i] * Quaternion.Euler(-45f * arc + 30f * frontTuck * (1f - arc), 0f, 0f);

            if (m_Time > m_NextTwitch)
            {
                m_EarTwitch = 1f;
                m_NextTwitch = m_Time + 1f + (float)m_FxRandom.NextDouble() * 3f;
            }
            m_EarTwitch = Mathf.Max(0f, m_EarTwitch - dt * 3.5f);
            float twitch = Mathf.Sin(m_EarTwitch * Mathf.PI * 3f) * m_EarTwitch * 15f;
            float sweep = -35f * arc;
            earLeft.localRotation = m_EarLeftRest * Quaternion.Euler(sweep + twitch, 0f, 0f);
            earRight.localRotation = m_EarRightRest * Quaternion.Euler(sweep - twitch * 0.5f, twitch * 0.8f, 0f);

            if (m_Time > m_NextWiggle)
            {
                m_TailWiggle = 1f;
                m_NextWiggle = m_Time + 2f + (float)m_FxRandom.NextDouble() * 4f;
            }
            m_TailWiggle = Mathf.Max(0f, m_TailWiggle - dt * 2.5f);
            tail.localRotation = m_TailRest * Quaternion.Euler(0f, Mathf.Sin(m_TailWiggle * Mathf.PI * 4f) * 25f * m_TailWiggle, 0f);
        }

        /// <summary>Hops towards the next route point. Returns the hop progress (0 when resting).</summary>
        float StepTravel(float dt)
        {
            Vector3 pos = transform.localPosition;
            Vector3 target = m_Route[m_RouteIndex];

            if (m_HopT < 0f)
            {
                Vector3 to = target - pos;
                to.y = 0f;
                float wanted = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
                m_Yaw = Mathf.MoveTowardsAngle(m_Yaw, wanted, 300f * dt);
                m_Rest -= dt;
                if (m_Rest <= 0f && Mathf.Abs(Mathf.DeltaAngle(m_Yaw, wanted)) < 25f)
                {
                    // Long bounds when far from the viewer, small hops close to them.
                    bool far = Mathf.Abs(pos.x) > 6f;
                    float length = Mathf.Min(far ? 0.6f : 0.42f, to.magnitude);
                    m_HopFrom = pos;
                    m_HopTo = pos + Quaternion.Euler(0f, m_Yaw, 0f) * Vector3.forward * length;
                    m_HopDuration = far ? 0.4f : 0.36f;
                    m_HopT = 0f;
                }
                return 0f;
            }

            m_HopT += dt / m_HopDuration;
            float p = Mathf.Min(m_HopT, 1f);
            Vector3 flat = Vector3.Lerp(m_HopFrom, m_HopTo, p);
            flat.y = ForestGround.HeightAt(flat.x, flat.z) + Mathf.Sin(p * Mathf.PI) * 0.13f * transform.localScale.y;
            transform.localPosition = flat;
            if (m_HopT < 1f)
                return p;

            m_HopT = -1f;
            m_Rest = Range(0.05f, 0.2f);
            Vector3 left = target - flat;
            left.y = 0f;
            if (left.magnitude < 0.12f)
            {
                m_RouteIndex++;
                if (m_RouteIndex >= m_Route.Count)
                {
                    Hide(Range(waitRange.x, waitRange.y));
                    return 0f;
                }
                if (m_RouteIndex < m_Route.Count - 1)
                {
                    m_Pauses.Clear();
                    m_Pauses.Enqueue(PickPause());
                    if (m_Random.NextDouble() < 0.5)
                        m_Pauses.Enqueue(PickPause());
                    NextPause();
                }
            }
            return 0f;
        }

        State PickPause()
        {
            double r = m_Random.NextDouble();
            return r < 0.45 ? State.Nibble : r < 0.75 ? State.SitUp : State.Look;
        }

        void NextPause()
        {
            m_StateTime = 0f;
            if (m_Pauses.Count == 0)
            {
                m_State = State.Travel;
                return;
            }
            m_State = m_Pauses.Dequeue();
            m_StateDuration = m_State == State.Nibble ? Range(3f, 6f) : m_State == State.SitUp ? Range(2.5f, 4f) : Range(1.5f, 3f);
        }
    }
}
