using UnityEngine;
using UnityEngine.InputSystem;

namespace MagicLand
{
    /// <summary>
    /// Holding a wide open hand in front of the face, palm toward the eyes, goes back to the world selection.
    /// A soft light grows in the palm while the gesture is held, so it never happens by surprise.
    /// Escape does the same in the editor.
    /// </summary>
    public class ReturnToMenu : MonoBehaviour
    {
        [SerializeField] string menuScene = "WorldSelect";
        [SerializeField] float holdDuration = 1.5f;
        [SerializeField] Transform glow;

        float m_Hold;
        float m_Grace;
        Vector3 m_GlowScale;

        public float Progress => Mathf.Clamp01(m_Hold / holdDuration);

        public void Configure(string scene, Transform palmGlow)
        {
            menuScene = scene;
            glow = palmGlow;
        }

        void Awake()
        {
            m_GlowScale = glow.localScale;
            glow.gameObject.SetActive(false);
        }

        void Update()
        {
            var hands = HandPinchTracker.Instance;
            Vector3 palm = default;
            bool open = hands != null && hands.TryGetOpenPalm(out palm);
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame && ScreenFade.Instance != null)
                ScreenFade.Instance.FadeOutAndLoad(menuScene);
            Step(open, palm, Time.deltaTime);
        }

        public void Step(bool openPalm, Vector3 palm, float dt)
        {
            var fade = ScreenFade.Instance;
            if (fade != null && fade.IsLeaving)
                return;

            // Tracking flickers for a frame now and then: keep the progress for a short moment.
            if (openPalm)
            {
                m_Grace = 0.25f;
                m_Hold += dt;
                glow.position = palm;
            }
            else
            {
                m_Grace -= dt;
                if (m_Grace <= 0f)
                    m_Hold = Mathf.Max(0f, m_Hold - dt * 2f);
            }

            float k = Progress;
            glow.gameObject.SetActive(k > 0f);
            glow.localScale = m_GlowScale * (0.3f + 0.7f * Mathf.SmoothStep(0f, 1f, k)) * (1f + 0.08f * Mathf.Sin(Time.time * 12f) * k);

            if (k >= 1f && fade != null)
                fade.FadeOutAndLoad(menuScene);
        }
    }
}
