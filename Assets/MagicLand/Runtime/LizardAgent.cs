using UnityEngine;

namespace MagicLand
{
    /// <summary>
    /// A little lizard of the volcano world. Scurry: now and then it runs across the ground along a route, in short
    /// dashes with cute push-ups in between. Bask: it lies on a warm rock, bobs its head, puffs its throat and looks around.
    /// </summary>
    public class LizardAgent : MonoBehaviour
    {
        public enum Mode { Scurry, Bask }

        [SerializeField] Mode mode;
        [SerializeField] Transform body;
        [SerializeField] Transform head;
        [SerializeField] Transform eyes;
        [SerializeField] Transform throat;
        [Tooltip("Tail segments, each one parented to the previous one.")]
        [SerializeField] Transform[] tail = new Transform[0];
        [Tooltip("Front left, front right, back left, back right.")]
        [SerializeField] Transform[] legs = new Transform[4];
        [Tooltip("Scurry routes on the ground (content space); the lizard walks them alternately in both directions.")]
        [SerializeField] Vector3[] route = new Vector3[0];
        [SerializeField] float startDelay = 10f;
        [SerializeField] Vector2 waitRange = new Vector2(25f, 45f);
        [SerializeField] float runSpeed = 1.5f;
        [SerializeField] float stride = 0.32f;
        [SerializeField] int seed = 3;

        enum State { Hidden, Running, Pausing }

        bool m_Ready;
        System.Random m_Random;
        State m_State;
        float m_Clock, m_Time, m_Duration;
        float m_Distance, m_RouteLength;
        bool m_Reverse;
        float m_Gait;
        float m_DashLeft;
        float m_BaseYaw, m_Yaw, m_TargetYaw;
        float m_NextBob, m_BobStart = -10f, m_NextTurn;
        Quaternion[] m_TailRest;
        Quaternion[] m_LegRest;
        Vector3 m_BodyRest;
        Renderer[] m_Renderers;

        public bool IsVisible => mode == Mode.Bask || m_State != State.Hidden;
        public bool IsCrossing => mode == Mode.Scurry && m_State != State.Hidden;
        public int Crossings { get; private set; }
        public float RouteLength => m_RouteLength;

        public void Configure(Mode lizardMode, Transform bodyPart, Transform headPart, Transform eyesPart, Transform throatPart,
            Transform[] tailParts, Transform[] legParts, Vector3[] path, float delay, float speed, int randomSeed)
        {
            mode = lizardMode;
            body = bodyPart;
            head = headPart;
            eyes = eyesPart;
            throat = throatPart;
            tail = tailParts;
            legs = legParts;
            route = path;
            startDelay = delay;
            runSpeed = speed;
            seed = randomSeed;
        }

        float Range(float min, float max) => min + (float)m_Random.NextDouble() * (max - min);

        void Init()
        {
            if (m_Ready)
                return;
            m_Ready = true;
            m_Random = new System.Random(seed);
            m_Renderers = GetComponentsInChildren<Renderer>(true);
            m_TailRest = new Quaternion[tail.Length];
            for (int i = 0; i < tail.Length; i++)
                m_TailRest[i] = tail[i].localRotation;
            m_LegRest = new Quaternion[legs.Length];
            for (int i = 0; i < legs.Length; i++)
                m_LegRest[i] = legs[i].localRotation;
            m_BodyRest = body.localPosition;
            m_BaseYaw = m_Yaw = m_TargetYaw = transform.localEulerAngles.y;
            m_NextBob = Range(2f, 6f);
            m_NextTurn = Range(8f, 16f);
            for (int i = 1; i < route.Length; i++)
                m_RouteLength += Vector3.Distance(Flat(route[i - 1]), Flat(route[i]));
            if (mode == Mode.Scurry)
            {
                m_State = State.Hidden;
                m_Duration = startDelay;
                SetVisible(false);
            }
        }

        void Start() => Init();

        void Update() => Step(Time.deltaTime);

        void SetVisible(bool visible)
        {
            foreach (var r in m_Renderers)
                r.enabled = visible;
        }

        static Vector3 Flat(Vector3 p) => new Vector3(p.x, 0f, p.z);

        public void Step(float dt)
        {
            Init();
            m_Clock += dt;
            if (mode == Mode.Bask)
                StepBask(dt);
            else
                StepScurry(dt);
        }

        // ---------- Scurry ----------

        void StepScurry(float dt)
        {
            m_Time += dt;
            switch (m_State)
            {
                case State.Hidden:
                    if (m_Time < m_Duration || route.Length < 2)
                        return;
                    m_Distance = 0f;
                    StartDash();
                    SetVisible(true);
                    break;
                case State.Running:
                {
                    float ease = Mathf.Clamp01(m_Time / 0.25f) * Mathf.Clamp01(m_DashLeft / 0.3f + 0.25f);
                    float move = runSpeed * Mathf.Max(0.2f, ease) * dt;
                    move = Mathf.Min(move, m_DashLeft);
                    m_DashLeft -= move;
                    m_Distance += move;
                    m_Gait += move / stride * Mathf.PI * 2f;
                    if (m_Distance >= m_RouteLength)
                    {
                        Crossings++;
                        m_Reverse = !m_Reverse;
                        m_State = State.Hidden;
                        m_Time = 0f;
                        m_Duration = Range(waitRange.x, waitRange.y);
                        SetVisible(false);
                        return;
                    }
                    if (m_DashLeft <= 0f)
                    {
                        m_State = State.Pausing;
                        m_Time = 0f;
                        m_Duration = Range(0.8f, 2.2f);
                    }
                    break;
                }
                case State.Pausing:
                    if (m_Time >= m_Duration)
                        StartDash();
                    break;
            }
            PoseOnRoute();
        }

        void StartDash()
        {
            m_State = State.Running;
            m_Time = 0f;
            m_DashLeft = Range(1.2f, 3.2f);
        }

        Vector3 PointAt(float distance, out Vector3 direction)
        {
            float d = m_Reverse ? m_RouteLength - distance : distance;
            direction = Vector3.forward;
            for (int i = 1; i < route.Length; i++)
            {
                Vector3 a = Flat(route[i - 1]), b = Flat(route[i]);
                float length = Vector3.Distance(a, b);
                if (d <= length || i == route.Length - 1)
                {
                    direction = (b - a).normalized * (m_Reverse ? -1f : 1f);
                    return Vector3.Lerp(a, b, Mathf.Clamp01(d / Mathf.Max(length, 0.0001f)));
                }
                d -= length;
            }
            return Flat(route[route.Length - 1]);
        }

        static float GroundY(Vector3 p) => VolcanoGround.HeightAt(p.x, p.z);

        void PoseOnRoute()
        {
            // Look slightly ahead and behind, so corners are rounded and the body follows the slope.
            Vector3 ahead = PointAt(Mathf.Min(m_Distance + 0.25f, m_RouteLength), out _);
            Vector3 behind = PointAt(Mathf.Max(m_Distance - 0.25f, 0f), out _);
            Vector3 p = PointAt(m_Distance, out Vector3 dir);
            Vector3 forward = ahead - behind;
            if (forward.sqrMagnitude < 0.0001f)
                forward = dir;
            forward.y = GroundY(ahead) - GroundY(behind);
            p.y = GroundY(p);
            transform.localPosition = p;
            transform.localRotation = Quaternion.LookRotation(forward.normalized, Vector3.up);

            bool running = m_State == State.Running;
            float wiggle = running ? Mathf.Sin(m_Gait) : 0f;
            for (int i = 0; i < legs.Length; i++)
            {
                // Diagonal pairs move together, as real lizards do.
                float phase = (i == 0 || i == 3) ? 0f : Mathf.PI;
                float swing = running ? Mathf.Sin(m_Gait + phase) * 38f : 0f;
                float lift = running ? Mathf.Max(0f, Mathf.Cos(m_Gait + phase)) * 18f : 0f;
                float side = (i % 2 == 0) ? -1f : 1f;
                legs[i].localRotation = m_LegRest[i] * Quaternion.Euler(0f, swing, side * lift);
            }

            // Push-ups while pausing: the front of the body goes up and down, the head looks around.
            float pushUp = 0f, look = 0f;
            if (m_State == State.Pausing)
            {
                float t = m_Time;
                pushUp = Mathf.Max(0f, Mathf.Sin(t * 9f)) * Mathf.Clamp01(1.2f - t) * 1f;
                look = Mathf.Sin(t * 2.4f) * 30f * Mathf.Clamp01(t * 3f);
            }
            body.localPosition = m_BodyRest + Vector3.up * pushUp * 0.025f;
            body.localRotation = Quaternion.Euler(-pushUp * 9f, wiggle * 9f, 0f);
            head.localRotation = Quaternion.Euler(-pushUp * 6f - 4f, -wiggle * 12f + look, 0f);
            for (int i = 0; i < tail.Length; i++)
            {
                float sway = running ? Mathf.Sin(m_Gait - (i + 1) * 0.9f) * 16f : Mathf.Sin(m_Clock * 1.7f - i) * 6f;
                tail[i].localRotation = m_TailRest[i] * Quaternion.Euler(0f, sway, 0f);
            }
            Blink();
        }

        // ---------- Bask ----------

        void StepBask(float dt)
        {
            if (m_Clock >= m_NextTurn)
            {
                m_TargetYaw = m_BaseYaw + Range(-35f, 35f);
                m_NextTurn = m_Clock + Range(9f, 18f);
            }
            m_Yaw = Mathf.MoveTowardsAngle(m_Yaw, m_TargetYaw, 14f * dt);
            bool turning = Mathf.Abs(Mathf.DeltaAngle(m_Yaw, m_TargetYaw)) > 0.5f;
            if (turning)
                m_Gait += 14f * dt / stride * 0.03f * Mathf.PI * 2f;
            var euler = transform.localEulerAngles;
            transform.localRotation = Quaternion.Euler(euler.x, m_Yaw, euler.z);

            if (m_Clock >= m_NextBob)
            {
                m_BobStart = m_Clock;
                m_NextBob = m_Clock + Range(5f, 11f);
            }
            // Three proud head bobs and a puffed-out throat.
            float bobTime = m_Clock - m_BobStart;
            float bob = bobTime < 1.8f ? Mathf.Max(0f, Mathf.Sin(bobTime * Mathf.PI * 3f / 1.8f)) * Mathf.Sin(bobTime / 1.8f * Mathf.PI) : 0f;
            float puff = bobTime < 2.6f ? Mathf.Sin(Mathf.Clamp01(bobTime / 2.6f) * Mathf.PI) : 0f;
            float breathing = Mathf.Sin(m_Clock * 1.3f);
            float look = Mathf.Sin(m_Clock * 0.37f) * 22f + Mathf.Sin(m_Clock * 0.91f) * 8f;

            body.localPosition = m_BodyRest + Vector3.up * bob * 0.02f;
            body.localScale = new Vector3(1f + breathing * 0.025f, 1f + breathing * 0.03f, 1f);
            head.localRotation = Quaternion.Euler(-bob * 16f - 6f, look, Mathf.Sin(m_Clock * 0.6f) * 5f);
            if (throat != null)
                throat.localScale = new Vector3(1f, 1f + puff * 0.9f, 1f + puff * 0.4f);
            for (int i = 0; i < tail.Length; i++)
                tail[i].localRotation = m_TailRest[i] * Quaternion.Euler(0f, Mathf.Sin(m_Clock * 0.8f - i * 0.7f) * 7f, 0f);
            for (int i = 0; i < legs.Length; i++)
            {
                float phase = (i == 0 || i == 3) ? 0f : Mathf.PI;
                float swing = turning ? Mathf.Sin(m_Gait + phase) * 14f : 0f;
                legs[i].localRotation = m_LegRest[i] * Quaternion.Euler(0f, swing, 0f);
            }
            Blink();
        }

        void Blink()
        {
            if (eyes != null)
                eyes.localScale = new Vector3(1f, Mathf.Repeat(m_Clock + seed * 0.7f, 3.7f) < 0.14f ? 0.15f : 1f, 1f);
        }
    }
}
