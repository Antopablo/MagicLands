using System;
using UnityEngine;

namespace MagicLand
{
    /// <summary>A cheerful snow friend who visits several hiding places in turn, each with its own way of saying hello.</summary>
    public class SnowFriendAgent : MonoBehaviour
    {
        public enum Entrance { StepAside, PopUp, PeekOverBush }

        [Serializable]
        public struct Hideout
        {
            public Entrance entrance;
            /// <summary>Ground point where he waits out of sight.</summary>
            public Vector3 hidden;
            /// <summary>Ground point where he stands to say hello.</summary>
            public Vector3 shown;
        }

        [SerializeField] Transform head;
        [SerializeField] Transform eyes;
        [SerializeField] Transform leftArm;
        [SerializeField] Transform rightArm;
        [SerializeField] Hideout[] hideouts;
        [SerializeField] float startDelay = 35f;

        const float SunkDepth = 2.1f;
        const float BushDepth = 0.35f;

        bool m_Ready;
        bool m_Visiting;
        int m_Hideout = -1;
        float m_Clock;
        float m_Time;
        float m_Wait;
        Quaternion m_LeftRest;
        Quaternion m_RightRest;
        Renderer[] m_Renderers;

        public int CurrentHideout => m_Visiting ? m_Hideout : -1;
        public int HideoutCount => hideouts.Length;

        /// <summary>Seconds before the next visit (0 while visiting).</summary>
        public float QuietTime => !m_Ready ? startDelay : m_Visiting ? 0f : Mathf.Max(0f, m_Wait - m_Time);

        /// <summary>While held, he keeps waiting in his hiding place.</summary>
        public bool Held { get; set; }

        public void Configure(Transform headPart, Transform eyesPart, Transform left, Transform right, Hideout[] places, float delay)
        {
            head = headPart;
            eyes = eyesPart;
            leftArm = left;
            rightArm = right;
            hideouts = places;
            startDelay = delay;
        }

        void Init()
        {
            if (m_Ready)
                return;
            m_Ready = true;
            m_LeftRest = leftArm.localRotation;
            m_RightRest = rightArm.localRotation;
            m_Renderers = GetComponentsInChildren<Renderer>(true);
            m_Wait = startDelay;
            SetVisible(false);
            transform.localPosition = OnGround(hideouts[0].hidden);
        }

        void Start() => Init();

        void Update() => Step(Time.deltaTime);

        void SetVisible(bool visible)
        {
            foreach (Renderer r in m_Renderers)
                r.enabled = visible;
        }

        static float Duration(Entrance entrance) =>
            entrance == Entrance.StepAside ? 10.4f : entrance == Entrance.PopUp ? 8.6f : 15f;

        public void Step(float dt)
        {
            Init();
            m_Clock += dt;
            m_Time += dt;
            if (!m_Visiting)
            {
                if (Held)
                    m_Time -= dt;
                if (m_Time < m_Wait)
                    return;
                m_Hideout = (m_Hideout + 1) % hideouts.Length;
                m_Visiting = true;
                m_Time = 0f;
                SetVisible(true);
            }

            Hideout place = hideouts[m_Hideout];
            if (m_Time >= Duration(place.entrance))
            {
                m_Visiting = false;
                m_Time = 0f;
                m_Wait = 24f + Mathf.PingPong(m_Clock * 0.37f, 16f);
                SetVisible(false);
                return;
            }
            Animate(place, m_Time);
        }

        struct Pose
        {
            public float travel, lift, turn, headYaw, headTilt, rightRaise, leftRaise, squash;
        }

        static float Smooth(float t) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

        static float Window(float t, float from, float to) => Mathf.Clamp01((t - from) / (to - from));

        static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            t = Mathf.Clamp01(t) - 1f;
            return 1f + c3 * t * t * t + c1 * t * t;
        }

        /// <summary>Little bouncing hops; returns the height and fills a landing squash.</summary>
        static float Hops(float t, int count, float height, ref float squash)
        {
            if (t <= 0f || t >= 1f)
                return 0f;
            float f = Mathf.Repeat(t * count, 1f);
            squash = Mathf.Max(squash, 0.07f * Mathf.Clamp01(1f - Mathf.Min(f, 1f - f) * 8f));
            return Mathf.Sin(f * Mathf.PI) * height;
        }

        /// <summary>Raised arm angle while waving, fading in and out over the waving window.</summary>
        static float Wave(float t, float from, float to, float speed)
        {
            if (t <= from || t >= to)
                return 0f;
            float envelope = Smooth((t - from) / 0.45f) * Smooth((to - t) / 0.45f);
            return envelope * (62f + Mathf.Sin((t - from) * speed) * 26f);
        }

        void Animate(Hideout place, float t)
        {
            var pose = new Pose();
            switch (place.entrance)
            {
                case Entrance.StepAside:
                {
                    float outward = Window(t, 0f, 2.4f), back = Window(t, 7.6f, 10f);
                    pose.travel = Smooth(outward) * (1f - Smooth(back));
                    pose.lift = Hops(outward, 3, 0.1f, ref pose.squash) + Hops(back, 3, 0.1f, ref pose.squash);
                    pose.turn = 32f * (Mathf.Sin(outward * Mathf.PI) - Mathf.Sin(back * Mathf.PI));
                    pose.rightRaise = Wave(t, 2.4f, 7.6f, 7f);
                    pose.headTilt = Mathf.Sin(t * 1.4f) * 9f * Smooth(t - 2.4f) * (1f - Smooth(t - 7.6f));
                    break;
                }
                case Entrance.PopUp:
                {
                    pose.travel = 1f;
                    float rise = Window(t, 0f, 1.1f), sink = Window(t, 6.4f, 7.6f);
                    pose.lift = -SunkDepth * (1f - EaseOutBack(rise)) - SunkDepth * sink * sink;
                    pose.lift += Hops(Window(t, 1.3f, 5.9f), 4, 0.12f, ref pose.squash);
                    if (t > 5.9f && t < 6.5f)
                        pose.squash = Mathf.Max(pose.squash, 0.1f * Mathf.Sin(Window(t, 5.9f, 6.5f) * Mathf.PI));
                    pose.rightRaise = Wave(t, 0.6f, 6.1f, 8f);
                    pose.leftRaise = Wave(t, 0.6f, 6.1f, 8f);
                    pose.headTilt = Mathf.Sin(t * 2.1f) * 7f;
                    break;
                }
                default:
                {
                    float appear = Window(t, 0f, 1.6f), outward = Window(t, 4.4f, 6.2f), back = Window(t, 10.8f, 12.8f),
                        hide = Window(t, 12.8f, 14.2f);
                    pose.travel = Smooth(outward) * (1f - Smooth(back));
                    pose.lift = Mathf.Lerp(-SunkDepth, -BushDepth, Smooth(appear)) + BushDepth * pose.travel
                        - (SunkDepth - BushDepth) * Smooth(hide);
                    pose.lift += Hops(outward, 3, 0.14f, ref pose.squash) + Hops(back, 3, 0.14f, ref pose.squash);
                    pose.turn = 35f * (Mathf.Sin(outward * Mathf.PI) - Mathf.Sin(back * Mathf.PI));
                    pose.headYaw = Mathf.Sin(Window(t, 1.6f, 4.4f) * Mathf.PI * 2f) * 35f;
                    pose.rightRaise = Wave(t, 6.2f, 8.6f, 7f);
                    pose.leftRaise = Wave(t, 8.4f, 10.8f, 7f);
                    pose.headTilt = Mathf.Sin(t * 1.5f) * 8f * Smooth(t - 6.2f) * (1f - Smooth(t - 10.8f));
                    break;
                }
            }
            Apply(place, pose);
        }

        void Apply(Hideout place, Pose pose)
        {
            Vector3 p = OnGround(Vector3.Lerp(place.hidden, place.shown, pose.travel));
            p.y += pose.lift;
            transform.localPosition = p;

            Quaternion facing = Quaternion.LookRotation(new Vector3(-p.x, 0f, -p.z));
            float direction = Mathf.Sign(Vector3.Dot(place.shown - place.hidden, facing * Vector3.right));
            transform.localRotation = facing * Quaternion.Euler(0f, pose.turn * direction, 0f);
            transform.localScale = new Vector3(1f + pose.squash * 0.5f, 1f - pose.squash, 1f + pose.squash * 0.5f);

            float breathing = Mathf.Sin(m_Clock * 1.7f) * 3f;
            head.localRotation = Quaternion.Euler(breathing, pose.headYaw, pose.headTilt);
            bool blinking = Mathf.Repeat(m_Clock, 3.7f) < 0.13f;
            eyes.localScale = new Vector3(1f, blinking ? 0.15f : 1f, 1f);
            rightArm.localRotation = m_RightRest * Quaternion.Euler(0f, 0f, pose.rightRaise);
            leftArm.localRotation = m_LeftRest * Quaternion.Euler(0f, 0f, -pose.leftRaise);
        }

        static Vector3 OnGround(Vector3 p)
        {
            p.y = ForestGround.HeightAt(p.x, p.z);
            return p;
        }
    }
}
