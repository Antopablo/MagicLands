using UnityEngine;

namespace MagicLand
{
    /// <summary>Transforms of a procedurally built bird (see BirdBuilder).</summary>
    [System.Serializable]
    public class BirdRig
    {
        public Transform root;
        public Transform body;
        public Transform head;
        public Transform tail;
        public Transform wingLeft;
        public Transform wingRight;
        public AudioSource voice;

        Quaternion m_WingRestRight, m_WingRestLeft, m_TailRest, m_HeadRest;
        bool m_Cached;

        void Cache()
        {
            if (m_Cached)
                return;
            m_WingRestRight = wingRight.localRotation;
            m_WingRestLeft = wingLeft.localRotation;
            m_TailRest = tail.localRotation;
            m_HeadRest = head.localRotation;
            m_Cached = true;
        }

        /// <summary>Poses the wings: fly 0 = folded, 1 = spread and flapping by 'flapAngle' degrees.</summary>
        public void Pose(float fly, float flapAngle, float headYaw, float headPitch, float tailLift)
        {
            Cache();
            var right = Quaternion.Euler(0f, 5f, flapAngle);
            var left = Quaternion.Euler(0f, -5f, -flapAngle);
            wingRight.localRotation = Quaternion.Slerp(m_WingRestRight, right, fly);
            wingLeft.localRotation = Quaternion.Slerp(m_WingRestLeft, left, fly);
            head.localRotation = m_HeadRest * Quaternion.Euler(0f, headYaw, 0f) * Quaternion.Euler(headPitch, 0f, 0f);
            tail.localRotation = m_TailRest * Quaternion.Euler(-tailLift, 0f, 0f);
        }

        public void SetVisible(bool visible)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                r.enabled = visible;
        }
    }

    /// <summary>
    /// Little birds living in one tree: they look around with quick head turns, flick their tails,
    /// sing now and then, flutter between branches and sometimes hop on the grass to peck.
    /// </summary>
    public class TreeBirds : MonoBehaviour
    {
        [SerializeField] BirdRig[] birds = new BirdRig[0];
        [Tooltip("Perch points in local space.")]
        [SerializeField] Vector3[] perches = new Vector3[0];
        [Tooltip("True for perches on the grass (pecking spots).")]
        [SerializeField] bool[] groundPerches = new bool[0];
        [SerializeField] AudioClip[] songs = new AudioClip[0];
        [SerializeField] float songVolume = 0.35f;
        [SerializeField] int seed = 21;
        [Tooltip("Pecking spots follow the volcano ground instead of the forest ground.")]
        [SerializeField] bool volcanoGround;

        enum State { Idle, Turn, Sing, Fly, Peck }

        class Runtime
        {
            public State state;
            public float time, duration;
            public int perch;
            public float yaw, targetYaw;
            public Vector3 position;
            public Vector3 from, to, control;
            public float headYaw, headPitch, targetHeadYaw, targetHeadPitch, nextHead;
            public float tailLift, nextTail;
            public float fly, flapPhase;
            public float hop;
            public int pecks;
        }

        Runtime[] m_Birds;
        bool[] m_Taken;
        System.Random m_Random;
        float m_Time;
        float m_SongBusyUntil;

        public void Configure(BirdRig[] rigs, Vector3[] perchPoints, bool[] ground, AudioClip[] clips, bool onVolcano = false)
        {
            volcanoGround = onVolcano;
            birds = rigs;
            perches = perchPoints;
            groundPerches = ground;
            songs = clips;
        }

        float Range(float min, float max) => min + (float)m_Random.NextDouble() * (max - min);

        void Init()
        {
            if (m_Birds != null)
                return;
            m_Random = new System.Random(seed);
            m_Taken = new bool[perches.Length];
            m_Birds = new Runtime[birds.Length];
            for (int i = 0; i < birds.Length; i++)
            {
                int perch = FindPerch(i, false);
                m_Taken[perch] = true;
                var b = new Runtime { perch = perch, position = perches[perch] };
                b.yaw = b.targetYaw = FacingYaw(b.position);
                m_Birds[i] = b;
                Enter(b, State.Idle);
                b.time = Range(0f, b.duration);
            }
        }

        void Update() => Step(Time.deltaTime);

        public void Step(float dt)
        {
            Init();
            m_Time += dt;
            for (int i = 0; i < m_Birds.Length; i++)
                StepBird(i, m_Birds[i], birds[i], dt);
        }

        /// <summary>Faces the viewer (content origin) with some variation.</summary>
        float FacingYaw(Vector3 p) => Mathf.Atan2(-p.x, -p.z) * Mathf.Rad2Deg + Range(-55f, 55f);

        int FindPerch(int salt, bool ground)
        {
            int start = m_Random.Next(perches.Length);
            for (int k = 0; k < perches.Length; k++)
            {
                int p = (start + k + salt) % perches.Length;
                if (!m_Taken[p] && groundPerches[p] == ground)
                    return p;
            }
            for (int k = 0; k < perches.Length; k++)
                if (!m_Taken[k])
                    return k;
            return 0;
        }

        void Enter(Runtime b, State state)
        {
            b.state = state;
            b.time = 0f;
            switch (state)
            {
                case State.Idle: b.duration = Range(2.5f, 7f); break;
                case State.Turn: b.duration = 0.25f; break;
                case State.Peck: b.duration = Range(4f, 9f); break;
                case State.Sing:
                {
                    var clip = songs[m_Random.Next(songs.Length)];
                    b.duration = (clip != null ? clip.length : 2f) + 0.4f;
                    m_SongBusyUntil = m_Time + b.duration + Range(4f, 10f);
                    break;
                }
            }
        }

        void StartFlight(Runtime b, bool toGround)
        {
            int target = FindPerch(m_Random.Next(5), toGround);
            if (target == b.perch)
            {
                Enter(b, State.Idle);
                return;
            }
            m_Taken[b.perch] = false;
            m_Taken[target] = true;
            b.perch = target;
            b.from = b.position;
            b.to = perches[target];
            // Arc slightly towards the viewer so the flight is easy to follow.
            Vector3 towardViewer = -new Vector3(b.from.x + b.to.x, 0f, b.from.z + b.to.z).normalized;
            b.control = (b.from + b.to) * 0.5f + Vector3.up * Range(0.5f, 1.1f) + towardViewer * Range(0.6f, 1.4f);
            float distance = Vector3.Distance(b.from, b.to);
            b.state = State.Fly;
            b.time = 0f;
            b.duration = Mathf.Max(0.9f, distance / 1.8f);
        }

        void StepBird(int index, Runtime b, BirdRig rig, float dt)
        {
            b.time += dt;
            bool onGround = groundPerches[b.perch];
            float flyTarget = 0f;
            float flapAngle = 0f;
            float pitchOffset = 0f;
            b.hop = Mathf.Max(0f, b.hop - dt * 5f);

            switch (b.state)
            {
                case State.Idle:
                    if (b.time > b.duration)
                    {
                        double r = m_Random.NextDouble();
                        bool canSing = songs.Length > 0 && m_Time > m_SongBusyUntil && !onGround;
                        if (onGround)
                            Enter(b, r < 0.5 ? State.Peck : State.Idle);
                        else if (r < 0.3)
                            Enter(b, State.Turn);
                        else if (r < 0.55 && canSing)
                        {
                            Enter(b, State.Sing);
                            if (rig.voice != null && songs.Length > 0)
                            {
                                rig.voice.clip = songs[m_Random.Next(songs.Length)];
                                rig.voice.pitch = Range(1.05f, 1.2f);
                                rig.voice.volume = songVolume;
                                rig.voice.Play();
                            }
                        }
                        else if (r < 0.8)
                            StartFlight(b, false);
                        else if (r < 0.92)
                            StartFlight(b, true);
                        else
                            Enter(b, State.Idle);
                        if (onGround && b.state == State.Idle && m_Random.NextDouble() < 0.6)
                            StartFlight(b, false);
                    }
                    break;

                case State.Turn:
                    if (b.time < dt * 1.5f)
                    {
                        b.targetYaw = FacingYaw(b.position);
                        b.hop = 1f;
                    }
                    if (b.time > b.duration)
                        Enter(b, State.Idle);
                    break;

                case State.Sing:
                    // Head raised, throat puffing.
                    b.targetHeadPitch = -28f + Mathf.Sin(m_Time * 22f) * 4f;
                    b.nextHead = m_Time + 0.3f;
                    if (b.time > b.duration)
                        Enter(b, State.Idle);
                    break;

                case State.Peck:
                {
                    float cycle = (b.time * 2.2f) % 1f;
                    float peck = cycle < 0.35f ? Mathf.Sin(cycle / 0.35f * Mathf.PI) : 0f;
                    b.targetHeadPitch = 55f * peck;
                    pitchOffset = 18f * peck;
                    b.nextHead = m_Time + 0.2f;
                    // Small hops around the spot between pecks.
                    if (cycle > 0.95f && m_Random.NextDouble() < 0.08)
                    {
                        Vector3 home = perches[b.perch];
                        Vector3 step = Quaternion.Euler(0f, b.yaw + Range(-50f, 50f), 0f) * Vector3.forward * 0.1f;
                        Vector3 next = b.position + step;
                        if ((next - home).magnitude > 0.45f)
                            next = home + (b.position - home) * 0.5f;
                        next.y = volcanoGround ? VolcanoGround.HeightAt(next.x, next.z) : ForestGround.HeightAt(next.x, next.z);
                        b.targetYaw = Mathf.Atan2(next.x - b.position.x, next.z - b.position.z) * Mathf.Rad2Deg;
                        b.position = next;
                        b.hop = 1f;
                    }
                    if (b.time > b.duration)
                        StartFlight(b, false);
                    break;
                }

                case State.Fly:
                {
                    float t = Mathf.Clamp01(b.time / b.duration);
                    float s = Mathf.SmoothStep(0f, 1f, t);
                    Vector3 p = Bezier(b.from, b.control, b.to, s);
                    Vector3 ahead = Bezier(b.from, b.control, b.to, Mathf.Min(1f, s + 0.05f)) - p;
                    if (ahead.sqrMagnitude > 1e-6f && t < 0.9f)
                        b.targetYaw = Mathf.Atan2(ahead.x, ahead.z) * Mathf.Rad2Deg;
                    b.position = p;
                    flyTarget = 1f;
                    b.flapPhase += dt * 15f * Mathf.PI * 2f;
                    flapAngle = 15f + Mathf.Sin(b.flapPhase) * 60f;
                    b.targetHeadPitch = 0f;
                    b.targetHeadYaw = 0f;
                    b.nextHead = m_Time + 0.5f;
                    if (t >= 1f)
                    {
                        b.position = b.to;
                        b.targetYaw = FacingYaw(b.position);
                        Enter(b, groundPerches[b.perch] ? State.Peck : State.Idle);
                    }
                    break;
                }
            }

            // Quick, jerky head turns like real little birds.
            if (m_Time > b.nextHead)
            {
                b.targetHeadYaw = Range(-75f, 75f);
                b.targetHeadPitch = Range(-20f, 12f);
                b.nextHead = m_Time + Range(0.35f, 1.6f);
            }
            if (m_Time > b.nextTail)
            {
                b.tailLift = 1f;
                b.nextTail = m_Time + Range(1.5f, 5f);
            }
            b.tailLift = Mathf.Max(0f, b.tailLift - dt * 4f);

            float snap = 1f - Mathf.Exp(-25f * dt);
            b.headYaw = Mathf.Lerp(b.headYaw, b.targetHeadYaw, snap);
            b.headPitch = Mathf.Lerp(b.headPitch, b.targetHeadPitch, snap);
            b.yaw = Mathf.LerpAngle(b.yaw, b.targetYaw, 1f - Mathf.Exp(-(b.state == State.Fly ? 8f : 14f) * dt));
            b.fly = Mathf.MoveTowards(b.fly, flyTarget, dt * 6f);

            float hopHeight = Mathf.Sin(b.hop * Mathf.PI) * 0.035f;
            rig.root.localPosition = b.position + Vector3.up * hopHeight;
            rig.root.localRotation = Quaternion.Euler(0f, b.yaw, 0f);
            rig.body.localRotation = Quaternion.Euler(pitchOffset, 0f, 0f);
            float puff = b.state == State.Sing ? 1f + Mathf.Abs(Mathf.Sin(m_Time * 11f)) * 0.06f : 1f + Mathf.Sin(m_Time * 3f + index) * 0.015f;
            rig.body.localScale = new Vector3(puff, puff, 1f);
            float tailWag = Mathf.Sin(b.tailLift * Mathf.PI * 2f) * 25f * b.tailLift;
            rig.Pose(b.fly, flapAngle, b.headYaw * (1f - b.fly), b.headPitch, tailWag);
        }

        static Vector3 Bezier(Vector3 a, Vector3 c, Vector3 b, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * c + t * t * b;
        }
    }
}
