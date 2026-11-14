using UnityEngine;

namespace MagicLand
{
    /// <summary>
    /// Now and then, when nothing else is happening in the clearing, a second mouse comes out of the grass,
    /// rubs noses with the mouse in front of the viewer under a little floating heart, then runs back.
    /// </summary>
    public class MouseKissDirector : MonoBehaviour
    {
        [SerializeField] MouseAgent resident;
        [SerializeField] MouseAgent visitor;
        [SerializeField] Transform heart;
        [SerializeField] RabbitAgent[] rabbits = new RabbitAgent[0];
        [SerializeField] SnowFriendAgent snowFriend;
        [SerializeField] UnicornAgent[] unicorns = new UnicornAgent[0];
        [SerializeField] RainbowReveal[] rainbows = new RainbowReveal[0];
        [SerializeField] float firstChance = 50f;
        [SerializeField] Vector2 cooldown = new Vector2(80f, 140f);
        [Tooltip("Distance from the middle of the view where the visitor waits, hidden in the tall grass.")]
        [SerializeField] float hidingX = 2.9f;
        [SerializeField] float noseReach = 0.24f;

        // Long enough for the whole meeting, so it never overlaps the unicorns or the rainbow.
        const float SceneLength = 32f;
        const float KissLength = 3.8f;
        const float ApproachTimeout = 22f;

        enum Phase { Waiting, Approach, Kiss, Parting }

        Phase m_Phase;
        float m_Time;
        float m_PhaseTime;
        float m_NextChance;
        Vector3 m_Center;
        Vector3 m_Hiding;
        Vector3 m_ResidentKiss;
        Vector3 m_VisitorKiss;
        Vector3 m_HeartScale;
        Renderer m_HeartRenderer;
        System.Random m_Random;
        bool m_Ready;

        public bool Active => m_Phase != Phase.Waiting;
        public bool HeartVisible => m_HeartRenderer != null && m_HeartRenderer.enabled;
        public int KissCount { get; private set; }

        public void Configure(MouseAgent residentMouse, MouseAgent visitorMouse, Transform heartPart,
            RabbitAgent[] rabbitAgents, SnowFriendAgent snow, UnicornAgent[] unicornAgents, RainbowReveal[] rainbowReveals)
        {
            resident = residentMouse;
            visitor = visitorMouse;
            heart = heartPart;
            rabbits = rabbitAgents;
            snowFriend = snow;
            unicorns = unicornAgents;
            rainbows = rainbowReveals;
        }

        void Init()
        {
            if (m_Ready)
                return;
            m_Ready = true;
            m_Random = new System.Random(19);
            m_NextChance = firstChance;
            m_HeartScale = heart.localScale;
            m_HeartRenderer = heart.GetComponent<Renderer>();
            m_HeartRenderer.enabled = false;
        }

        void Start() => Init();

        void Update() => Step(Time.deltaTime);

        /// <summary>True when nothing else is on stage and nothing is due during the whole meeting.</summary>
        bool SceneIsQuiet()
        {
            foreach (RabbitAgent rabbit in rabbits)
                if (rabbit.QuietTime < 2f)
                    return false;
            if (snowFriend != null && snowFriend.QuietTime < 2f)
                return false;
            foreach (UnicornAgent unicorn in unicorns)
                if (unicorn.QuietTime < SceneLength)
                    return false;
            foreach (RainbowReveal rainbow in rainbows)
                if (rainbow.QuietTimeAt(m_Time) < SceneLength)
                    return false;
            return true;
        }

        void HoldOthers(bool hold)
        {
            foreach (RabbitAgent rabbit in rabbits)
                rabbit.Held = hold;
            if (snowFriend != null)
                snowFriend.Held = hold;
        }

        void Begin()
        {
            Vector3 home = resident.transform.localPosition;
            float side = home.x < 0f ? 1f : -1f;
            var dir = new Vector3(side, 0f, 0f);
            m_ResidentKiss = home + dir * 0.06f;
            m_Center = m_ResidentKiss + dir * noseReach;
            m_VisitorKiss = m_Center + dir * noseReach;
            m_Hiding = new Vector3(side * hidingX, 0f, home.z);

            HoldOthers(true);
            resident.Meet(home, m_Center);
            visitor.Appear(m_Hiding, m_VisitorKiss);
            visitor.Meet(m_VisitorKiss + dir * 0.06f, m_Center);
            SetPhase(Phase.Approach);
        }

        void SetPhase(Phase phase)
        {
            m_Phase = phase;
            m_PhaseTime = 0f;
        }

        public void Step(float dt)
        {
            Init();
            m_Time += dt;
            m_PhaseTime += dt;
            switch (m_Phase)
            {
                case Phase.Waiting:
                    if (m_Time >= m_NextChance)
                    {
                        if (SceneIsQuiet())
                            Begin();
                        else
                            m_NextChance = m_Time + 2f;
                    }
                    break;
                case Phase.Approach:
                    if ((resident.Arrived && visitor.Arrived) || m_PhaseTime > ApproachTimeout)
                    {
                        resident.Kiss(m_ResidentKiss);
                        visitor.Kiss(m_VisitorKiss);
                        SetPhase(Phase.Kiss);
                    }
                    break;
                case Phase.Kiss:
                    if (m_PhaseTime >= KissLength)
                    {
                        resident.Release(m_ResidentKiss);
                        visitor.Release(m_Hiding);
                        KissCount++;
                        SetPhase(Phase.Parting);
                    }
                    break;
                case Phase.Parting:
                    if (visitor.Hidden || m_PhaseTime > ApproachTimeout)
                    {
                        HoldOthers(false);
                        m_NextChance = m_Time + cooldown.x + (float)m_Random.NextDouble() * (cooldown.y - cooldown.x);
                        SetPhase(Phase.Waiting);
                    }
                    break;
            }
            AnimateHeart();
        }

        void AnimateHeart()
        {
            // Pops above the two noses, floats up with a soft heartbeat, then shrinks away.
            float t = m_Phase == Phase.Kiss ? m_PhaseTime - 0.7f
                : m_Phase == Phase.Parting ? KissLength - 0.7f + m_PhaseTime : -1f;
            const float life = KissLength + 0.4f;
            bool visible = t > 0f && t < life;
            m_HeartRenderer.enabled = visible;
            if (!visible)
                return;

            float pop = EaseOutBack(t / 0.5f) * Mathf.SmoothStep(1f, 0f, (t - (life - 0.6f)) / 0.6f);
            float beat = 1f + 0.08f * Mathf.Sin(t * 9f);
            heart.localScale = m_HeartScale * (pop * beat);
            Vector3 p = m_Center;
            p.y = ForestGround.HeightAt(p.x, p.z) + 0.3f + 0.07f * t;
            heart.localPosition = p;
            heart.localRotation = Quaternion.LookRotation(new Vector3(-p.x, 0f, -p.z)) * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2f) * 8f);
        }

        static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            t = Mathf.Clamp01(t) - 1f;
            return 1f + c3 * t * t * t + c1 * t * t;
        }
    }
}
