using UnityEngine;
using UnityEngine.SceneManagement;

namespace MagicLand
{
    /// <summary>
    /// Fades the whole view (and the sound) from black when a world starts,
    /// and to black before switching to another world.
    /// </summary>
    public class ScreenFade : MonoBehaviour
    {
        [SerializeField] Renderer cover;
        [SerializeField] float fadeInDuration = 1.5f;
        [SerializeField] float fadeOutDuration = 1f;

        static readonly int s_ColorId = Shader.PropertyToID("_Color");

        public static ScreenFade Instance { get; private set; }

        Material m_Material;
        float m_Alpha = 1f;
        string m_NextScene;
        AsyncOperation m_Loading;

        /// <summary>True once a world change has been requested.</summary>
        public bool IsLeaving => m_NextScene != null;
        public float Alpha => m_Alpha;

        public void Configure(Renderer coverRenderer) => cover = coverRenderer;

        void Awake()
        {
            Instance = this;
            m_Material = cover.material;
            Apply();
        }

        /// <summary>Fades to black, then opens the given scene. Ignored if a change is already running.</summary>
        public void FadeOutAndLoad(string sceneName)
        {
            if (IsLeaving)
                return;
            m_NextScene = sceneName;
        }

        void Update() => Step(Time.unscaledDeltaTime);

        public void Step(float dt)
        {
            if (m_NextScene == null)
                m_Alpha = Mathf.MoveTowards(m_Alpha, 0f, dt / fadeInDuration);
            else
            {
                m_Alpha = Mathf.MoveTowards(m_Alpha, 1f, dt / fadeOutDuration);
                if (m_Alpha >= 1f && m_Loading == null && Application.isPlaying)
                    m_Loading = SceneManager.LoadSceneAsync(m_NextScene);
            }
            Apply();
        }

        void Apply()
        {
            if (m_Material == null)
                return;
            float a = Mathf.SmoothStep(0f, 1f, m_Alpha);
            m_Material.SetColor(s_ColorId, new Color(0f, 0f, 0f, a));
            cover.enabled = a > 0.001f;
            AudioListener.volume = 1f - a;
        }
    }
}
