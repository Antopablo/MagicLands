using UnityEngine;

namespace MagicLand
{
    /// <summary>
    /// Drives the wind gusts (tree sway and wind sound volume) and publishes light, wind and up
    /// directions to the shaders, expressed relative to this content root so they follow recentering.
    /// </summary>
    [ExecuteAlways]
    public class NatureEnvironment : MonoBehaviour
    {
        [SerializeField] Vector3 lightDirection = new Vector3(0.35f, 0.75f, -0.45f);
        [SerializeField] Vector3 windDirection = new Vector3(1f, 0f, 0.3f);
        [SerializeField] AudioSource windAudio;
        [SerializeField] float minStrength = 0.5f;
        [SerializeField] float maxStrength = 1.2f;
        [SerializeField] float gustSpeed = 0.06f;
        [SerializeField] float minVolume = 0.18f;
        [SerializeField] float maxVolume = 0.38f;

        static readonly int s_StrengthId = Shader.PropertyToID("_MagicWindStrength");
        static readonly int s_LightId = Shader.PropertyToID("_MagicLightDir");
        static readonly int s_WindId = Shader.PropertyToID("_MagicWindDir");
        static readonly int s_UpId = Shader.PropertyToID("_MagicUp");

        public float Strength { get; private set; }

        public void Configure(AudioSource audioSource) => windAudio = audioSource;

        void OnEnable() => Apply(0f);

        void Update() => Apply(Application.isPlaying ? Time.time : Time.realtimeSinceStartup);

        public void Apply(float time)
        {
            float noise = Mathf.PerlinNoise(time * gustSpeed, 0.37f);
            float gust = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((noise - 0.2f) * 1.7f));
            Strength = Mathf.Lerp(minStrength, maxStrength, gust);

            Shader.SetGlobalFloat(s_StrengthId, Strength);
            Shader.SetGlobalVector(s_LightId, transform.TransformDirection(lightDirection.normalized));
            Shader.SetGlobalVector(s_WindId, transform.TransformDirection(windDirection.normalized));
            Shader.SetGlobalVector(s_UpId, transform.up);

            if (windAudio != null && Application.isPlaying)
                windAudio.volume = Mathf.Lerp(minVolume, maxVolume, gust);
        }
    }
}
