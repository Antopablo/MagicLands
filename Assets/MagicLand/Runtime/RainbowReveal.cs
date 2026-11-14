using UnityEngine;

namespace MagicLand
{
    /// <summary>Keeps the rainbow hidden at start, then fades it in slowly after a delay.</summary>
    public class RainbowReveal : MonoBehaviour
    {
        [Tooltip("Seconds after the start before the rainbow begins to appear.")]
        [SerializeField] float delay = 150f;
        [Tooltip("Seconds for the rainbow to fully appear.")]
        [SerializeField] float fadeDuration = 30f;

        static readonly int s_FadeId = Shader.PropertyToID("_Fade");

        Renderer m_Renderer;
        MaterialPropertyBlock m_Block;

        public void Configure(float delaySeconds, float fadeSeconds)
        {
            delay = delaySeconds;
            fadeDuration = fadeSeconds;
        }

        /// <summary>Seconds of calm left before the rainbow starts appearing (0 while it fades in).</summary>
        public float QuietTimeAt(float time) =>
            time < delay ? delay - time : time < delay + fadeDuration ? 0f : float.PositiveInfinity;

        void Start()
        {
            m_Renderer = GetComponent<Renderer>();
            m_Block = new MaterialPropertyBlock();
            Apply(0f);
        }

        void Update()
        {
            float k = Mathf.Clamp01((Time.timeSinceLevelLoad - delay) / Mathf.Max(fadeDuration, 0.01f));
            Apply(Mathf.SmoothStep(0f, 1f, k));
        }

        void Apply(float fade)
        {
            m_Renderer.enabled = fade > 0.001f;
            m_Block.SetFloat(s_FadeId, fade);
            m_Renderer.SetPropertyBlock(m_Block);
        }
    }
}
