using System;
using UnityEngine;

namespace MagicLand
{
    /// <summary>A playful baby dragon who visits several hiding places in turn and says hello with a puff of sparkles.</summary>
    public class DragonAgent : MonoBehaviour
    {
        public enum Entrance { StepAside, PopUp, FlyOver }

        [Serializable]
        public struct Hideout
        {
            public Entrance entrance;
            /// <summary>Ground point where it waits out of sight.</summary>
            public Vector3 hidden;
            /// <summary>Ground point where it says hello.</summary>
            public Vector3 shown;
        }

        [SerializeField] Transform head;
        [SerializeField] Transform eyes;
        [SerializeField] Transform leftWing;
        [SerializeField] Transform rightWing;
        [SerializeField] Transform tail;
        [SerializeField] ParticleSystem puff;
        [SerializeField] AudioSource voice;
        [SerializeField] Hideout[] hideouts;
        [SerializeField] float startDelay = 20f;

        public const float SunkDepth = 1.9f;
        /// <summary>Height reached when flying over its hiding rock.</summary>
        public const float FlyPeak = 2.6f;
        public const float HoverHeight = 0.75f;

        bool m_Ready;
        bool m_Visiting;
        int m_Hideout = -1;
        float m_Clock;
        float m_Time;
        float m_Wait;
        Quaternion m_TailRest;
        Renderer[] m_Renderers;

        public int CurrentHideout => m_Visiting ? m_Hideout : -1;
        public int HideoutCount => hideouts.Length;
        public int PuffCount { get; private set; }

        /// <summary>Seconds before the next visit (0 while visiting).</summary>
        public float QuietTime => !m_Ready ? startDelay : m_Visiting ? 0f : Mathf.Max(0f, m_Wait - m_Time);

        public Hideout GetHideout(int index) => hideouts[index];

        /// <summary>Keeps the dragon hidden for at least the given time (ignored while it is visiting).</summary>
        public void PostponeVisit(float seconds)
        {
            Init();
            if (!m_Visiting)
                m_Wait = Mathf.Max(m_Wait, m_Time + seconds);
        }

        public void Configure(Transform headPart, Transform eyesPart, Transform left, Transform right, Transform tailPart,
            ParticleSystem sparkles, AudioSource chirp, Hideout[] places, float delay)
        {
            head = headPart;
            eyes = eyesPart;
            leftWing = left;
            rightWing = right;
            tail = tailPart;
            puff = sparkles;
            voice = chirp;
            hideouts = places;
            startDelay = delay;
        }

        void Init()
        {
            if (m_Ready)
                return;
            m_Ready = true;
            m_TailRest = tail.localRotation;
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
                if (!(r is ParticleSystemRenderer))
                    r.enabled = visible;
        }

        public static float Duration(Entrance entrance) =>
            entrance == Entrance.StepAside ? 11.2f : entrance == Entrance.PopUp ? 8.6f : 14.6f;

        static float PuffTime(Entrance entrance) =>
            entrance == Entrance.StepAside ? 5.2f : entrance == Entrance.PopUp ? 3.6f : 7.2f;

        public void Step(float dt)
        {
            Init();
            m_Clock += dt;
            float previous = m_Time;
            m_Time += dt;
            if (!m_Visiting)
            {
                if (m_Time < m_Wait)
                    return;
                m_Hideout = (m_Hideout + 1) % hideouts.Length;
                m_Visiting = true;
                m_Time = 0f;
                previous = -1f;
                SetVisible(true);
            }

            Hideout place = hideouts[m_Hideout];
            if (m_Time >= Duration(place.entrance))
            {
                m_Visiting = false;
                m_Time = 0f;
                m_Wait = 22f + Mathf.PingPong(m_Clock * 0.41f, 16f);
                SetVisible(false);
                return;
            }

            float puffAt = PuffTime(place.entrance);
            if (previous < puffAt && m_Time >= puffAt)
                Puff();
            Animate(place, m_Time);
        }

        void Puff()
        {
            PuffCount++;
            if (puff != null)
                puff.Emit(28);
            if (voice != null && voice.clip != null && Application.isPlaying)
                voice.Play();
        }

        struct Pose
        {
            public float travel, lift, turn, pitch, roll, headYaw, headTilt, headPitch, wingOpen, flap, tailWag, squash;
        }

        static float Smooth(float t) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

        static float Window(float t, float from, float to) => Mathf.Clamp01((t - from) / (to - from));

        static float Envelope(float t, float from, float to, float fade) =>
            t <= from || t >= to ? 0f : Smooth((t - from) / fade) * Smooth((to - t) / fade);

        static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            t = Mathf.Clamp01(t) - 1f;
            return 1f + c3 * t * t * t + c1 * t * t;
        }

        /// <summary>Little bouncing steps; returns the height and fills a landing squash.</summary>
        static float Hops(float t, int count, float height, ref float squash)
        {
            if (t <= 0f || t >= 1f)
                return 0f;
            float f = Mathf.Repeat(t * count, 1f);
            squash = Mathf.Max(squash, 0.06f * Mathf.Clamp01(1f - Mathf.Min(f, 1f - f) * 8f));
            return Mathf.Sin(f * Mathf.PI) * height;
        }

        /// <summary>Quick happy wing beats within a window.</summary>
        static float Flutter(float t, float from, float to, float speed) =>
            Envelope(t, from, to, 0.3f) * Mathf.Sin((t - from) * speed) * 30f;

        /// <summary>Head thrown back, then forward, around the sparkle puff.</summary>
        static float PuffNod(float t, float at) =>
            -14f * Envelope(t, at - 0.7f, at + 0.05f, 0.3f) + 12f * Envelope(t, at, at + 0.7f, 0.2f);

        void Animate(Hideout place, float t)
        {
            var pose = new Pose();
            switch (place.entrance)
            {
                case Entrance.StepAside:
                {
                    float outward = Window(t, 0f, 2.6f), back = Window(t, 8.4f, 10.9f);
                    pose.travel = Smooth(outward) * (1f - Smooth(back));
                    pose.lift = Hops(outward, 5, 0.05f, ref pose.squash) + Hops(back, 5, 0.05f, ref pose.squash);
                    pose.lift += Hops(Window(t, 6.2f, 7.1f), 2, 0.13f, ref pose.squash);
                    pose.roll = 7f * (Mathf.Sin(outward * Mathf.PI * 5f) * Mathf.Sin(outward * Mathf.PI)
                                      + Mathf.Sin(back * Mathf.PI * 5f) * Mathf.Sin(back * Mathf.PI));
                    pose.turn = 38f * (Mathf.Sin(outward * Mathf.PI) - Mathf.Sin(back * Mathf.PI));
                    float hello = Envelope(t, 2.4f, 8.6f, 0.5f);
                    pose.wingOpen = hello;
                    pose.flap = Flutter(t, 2.9f, 4.4f, 12f) + Flutter(t, 6.1f, 7.9f, 12f);
                    pose.headTilt = Mathf.Sin(t * 1.4f) * 11f * hello;
                    pose.headPitch = PuffNod(t, PuffTime(place.entrance));
                    pose.tailWag = Mathf.Sin(t * 5f) * 22f * hello;
                    break;
                }
                case Entrance.PopUp:
                {
                    pose.travel = 1f;
                    float rise = Window(t, 0f, 1.1f), sink = Window(t, 6.6f, 7.8f);
                    pose.lift = -SunkDepth * (1f - EaseOutBack(rise)) - SunkDepth * sink * sink;
                    pose.lift += Hops(Window(t, 4.4f, 6.3f), 3, 0.1f, ref pose.squash);
                    if (t > 6.2f && t < 6.8f)
                        pose.squash = Mathf.Max(pose.squash, 0.09f * Mathf.Sin(Window(t, 6.2f, 6.8f) * Mathf.PI));
                    pose.wingOpen = Envelope(t, 0.4f, 6.5f, 0.5f);
                    pose.flap = Flutter(t, 0.9f, 2.6f, 13f) + Flutter(t, 4.4f, 6.2f, 11f);
                    pose.headTilt = Mathf.Sin(t * 2f) * 8f;
                    pose.headPitch = PuffNod(t, PuffTime(place.entrance));
                    pose.tailWag = Mathf.Sin(t * 6f) * 18f;
                    break;
                }
                default:
                {
                    // Takes off behind its rock, glides out over it, hovers to say hello, then flies back down.
                    float rise = Window(t, 0.2f, 2f), outward = Window(t, 2f, 4.6f), back = Window(t, 10.2f, 12.6f),
                        land = Window(t, 12.6f, 14.3f);
                    pose.travel = Smooth(outward) * (1f - Smooth(back));
                    float hover = HoverHeight + Mathf.Sin(t * 2.2f) * 0.1f;
                    float glide = Smooth(Window(outward, 0.4f, 1f)) * (1f - Smooth(Window(back, 0f, 0.6f)));
                    pose.lift = Mathf.Lerp(FlyPeak * Smooth(rise) * (1f - Smooth(land)), hover, glide);
                    pose.turn = 40f * (Mathf.Sin(outward * Mathf.PI) - Mathf.Sin(back * Mathf.PI));
                    pose.pitch = 10f * (Mathf.Sin(outward * Mathf.PI) + Mathf.Sin(back * Mathf.PI));
                    pose.roll = Mathf.Sin(t * 1.3f) * 5f * glide;
                    pose.wingOpen = Envelope(t, 0f, 14.5f, 0.3f);
                    float airborne = Envelope(t, 0.05f, 14.4f, 0.25f);
                    pose.flap = airborne * (8f + Mathf.Sin(t * 12f) * 38f);
                    pose.headTilt = Mathf.Sin(t * 1.5f) * 9f * glide;
                    pose.headPitch = PuffNod(t, PuffTime(place.entrance));
                    pose.tailWag = Mathf.Sin(t * 4f) * 15f;
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
            transform.localRotation = facing * Quaternion.Euler(pose.pitch, pose.turn * direction, pose.roll);
            transform.localScale = new Vector3(1f + pose.squash * 0.5f, 1f - pose.squash, 1f + pose.squash * 0.5f);

            float breathing = Mathf.Sin(m_Clock * 1.9f) * 3f;
            head.localRotation = Quaternion.Euler(breathing + pose.headPitch, pose.headYaw, pose.headTilt);
            bool blinking = Mathf.Repeat(m_Clock, 3.3f) < 0.13f;
            eyes.localScale = new Vector3(1f, blinking ? 0.15f : 1f, 1f);

            // Wings rest folded back along the body and spread out to the sides to flap.
            float yaw = Mathf.Lerp(58f, 18f, pose.wingOpen), droop = Mathf.Lerp(-22f, 8f, pose.wingOpen);
            float idle = Mathf.Sin(m_Clock * 2.3f) * 4f;
            rightWing.localRotation = Quaternion.Euler(0f, yaw, droop + idle + pose.flap);
            leftWing.localRotation = Quaternion.Euler(0f, -yaw, -(droop + idle + pose.flap));
            tail.localRotation = m_TailRest * Quaternion.Euler(0f, pose.tailWag + Mathf.Sin(m_Clock * 1.1f) * 6f, 0f);
        }

        static Vector3 OnGround(Vector3 p)
        {
            p.y = VolcanoGround.HeightAt(p.x, p.z);
            return p;
        }
    }
}
