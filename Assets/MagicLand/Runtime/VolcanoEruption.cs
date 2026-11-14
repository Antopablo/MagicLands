using UnityEngine;

namespace MagicLand
{
    /// <summary>
    /// Now and then the volcano wakes up gently: it puffs more smoke and glows brighter, then throws up a fountain of
    /// glowing drops and colorful sparkles, like a little firework, before calming down again. No loud sounds, no shaking.
    /// </summary>
    public class VolcanoEruption : MonoBehaviour
    {
        [SerializeField] ParticleSystem smoke;
        [SerializeField] ParticleSystem fountain;
        [SerializeField] ParticleSystem sparkles;
        [SerializeField] Transform craterGlow;
        [SerializeField] Renderer[] lava = new Renderer[0];
        [SerializeField] AudioSource rumble;
        [SerializeField] AudioSource whoosh;
        [SerializeField] AudioSource chime;
        [SerializeField] DragonAgent babyDragon;
        [SerializeField] float startDelay = 75f;
        [SerializeField] Vector2 interval = new Vector2(150f, 210f);
        [SerializeField] int seed = 9;

        public const float WakeDuration = 12f;
        public const float BurstDuration = 11f;
        public const float CalmDuration = 14f;
        public const float Duration = WakeDuration + BurstDuration + CalmDuration;
        static readonly float[] SparkleTimes = { 0.4f, 2.6f, 4.9f, 7.3f, 9.6f };
        static readonly float[] ChimePitches = { 1f, 1.12f, 0.94f, 1.19f, 1.06f };

        static readonly int s_Intensity = Shader.PropertyToID("_Intensity");

        bool m_Ready;
        System.Random m_Random;
        MaterialPropertyBlock m_Block;
        float m_Wait, m_Time;
        bool m_Active;
        float m_SmokeRate, m_RumbleVolume;
        Vector3 m_GlowScale;

        public bool IsActive => m_Active;
        public float Progress => m_Active ? m_Time : -1f;
        public int Eruptions { get; private set; }
        public int SparkleBursts { get; private set; }
        /// <summary>Strength of the event, 0 at rest and 1 at the top of the fountain.</summary>
        public float Strength { get; private set; }

        public void Configure(ParticleSystem smokeSystem, ParticleSystem fountainSystem, ParticleSystem sparkleSystem,
            Transform glow, Renderer[] lavaRenderers, AudioSource rumbleSource, AudioSource whooshSource, AudioSource chimeSource,
            DragonAgent dragon, float delay)
        {
            smoke = smokeSystem;
            fountain = fountainSystem;
            sparkles = sparkleSystem;
            craterGlow = glow;
            lava = lavaRenderers;
            rumble = rumbleSource;
            whoosh = whooshSource;
            chime = chimeSource;
            babyDragon = dragon;
            startDelay = delay;
        }

        float Range(float min, float max) => min + (float)m_Random.NextDouble() * (max - min);

        void Init()
        {
            if (m_Ready)
                return;
            m_Ready = true;
            m_Random = new System.Random(seed);
            m_Block = new MaterialPropertyBlock();
            m_Wait = startDelay;
            m_SmokeRate = smoke.emission.rateOverTime.constant;
            m_GlowScale = craterGlow.localScale;
            if (rumble != null)
                m_RumbleVolume = rumble.volume;
            var emission = fountain.emission;
            emission.rateOverTime = 0f;
            Apply(0f, 0f, 0f);
        }

        void Start() => Init();

        void Update() => Step(Time.deltaTime);

        public void Step(float dt)
        {
            Init();
            m_Time += dt;
            if (!m_Active)
            {
                if (m_Time < m_Wait)
                    return;
                // Let the baby dragon finish its visit first, then keep it home during the show.
                if (babyDragon != null && babyDragon.CurrentHideout >= 0)
                {
                    m_Wait = m_Time + 3f;
                    return;
                }
                if (babyDragon != null)
                    babyDragon.PostponeVisit(Duration + 4f);
                m_Active = true;
                m_Time = 0f;
                Eruptions++;
                Play(rumble, 1f);
            }

            float previous = m_Time - dt;
            float t = m_Time;
            if (t >= Duration)
            {
                m_Active = false;
                m_Time = 0f;
                m_Wait = Range(interval.x, interval.y);
                Apply(0f, 0f, 0f);
                if (rumble != null && Application.isPlaying)
                    rumble.Stop();
                return;
            }

            float wake = Mathf.SmoothStep(0f, 1f, t / WakeDuration);
            float burstTime = t - WakeDuration;
            float burst = burstTime < 0f ? 0f
                : Mathf.SmoothStep(0f, 1f, burstTime / 1.2f) * (1f - Mathf.SmoothStep(0f, 1f, (burstTime - BurstDuration + 2f) / 2f));
            float calm = 1f - Mathf.SmoothStep(0f, 1f, (t - WakeDuration - BurstDuration) / CalmDuration);
            float smokeLevel = t < WakeDuration + BurstDuration ? wake : calm;
            Strength = Mathf.Max(burst, smokeLevel * 0.45f);

            if (previous < WakeDuration && t >= WakeDuration)
                Play(whoosh, 1f);
            foreach (float at in SparkleTimes)
                if (previous < WakeDuration + at && t >= WakeDuration + at)
                {
                    SparkleBursts++;
                    sparkles.Emit(70);
                    Play(chime, ChimePitches[SparkleBursts % ChimePitches.Length]);
                }

            // The volcano breathes a little while it wakes up.
            float breath = 0.5f + 0.5f * Mathf.Sin(t * 2.4f);
            Apply(smokeLevel, burst, breath * smokeLevel * (1f - burst));
            if (rumble != null)
                rumble.volume = m_RumbleVolume * Mathf.Max(smokeLevel, burst) * (t < WakeDuration + BurstDuration ? 1f : calm);
        }

        void Apply(float smokeLevel, float burst, float breath)
        {
            var smokeEmission = smoke.emission;
            smokeEmission.rateOverTime = m_SmokeRate * (1f + smokeLevel * 1.8f + burst);
            var fountainEmission = fountain.emission;
            fountainEmission.rateOverTime = burst * 34f;
            craterGlow.localScale = m_GlowScale * (1f + smokeLevel * 0.18f + burst * 0.4f + breath * 0.08f);
            foreach (var r in lava)
            {
                r.GetPropertyBlock(m_Block);
                m_Block.SetFloat(s_Intensity, 1f + smokeLevel * 0.25f + burst * 0.45f + breath * 0.1f);
                r.SetPropertyBlock(m_Block);
            }
        }

        static void Play(AudioSource source, float pitch)
        {
            if (source == null || source.clip == null || !Application.isPlaying)
                return;
            source.pitch = pitch;
            source.Play();
        }
    }
}
