using UnityEngine;
using UnityEngine.Events;

namespace MagicLand
{
    /// <summary>
    /// Hands-free button: activates when the user looks at it for a while (dwell),
    /// or immediately when the user pinches while looking at it.
    /// </summary>
    public class GazeButton : MonoBehaviour
    {
        [SerializeField] float dwellTime = 2.5f;
        [SerializeField] float hitRadius = 0.12f;
        [SerializeField] float cooldown = 3f;
        [SerializeField] Color idleColor = new Color(0.6f, 0.8f, 1f);
        [SerializeField] Color activeColor = new Color(1f, 0.85f, 0.4f);
        [SerializeField] Renderer visual;

        public UnityEvent onActivated = new UnityEvent();

        static readonly int s_ColorId = Shader.PropertyToID("_Color");
        static readonly int s_IntensityId = Shader.PropertyToID("_Intensity");

        float m_Progress;
        float m_CooldownLeft;
        float m_Flash;
        Vector3 m_BaseScale;
        MaterialPropertyBlock m_Block;

        public void SetVisual(Renderer r) => visual = r;

        void Awake()
        {
            m_Block = new MaterialPropertyBlock();
            m_BaseScale = visual != null ? visual.transform.localScale : Vector3.one;
        }

        void Update()
        {
            var cam = Camera.main;
            if (cam == null)
                return;

            m_CooldownLeft -= Time.deltaTime;
            bool gazed = m_CooldownLeft <= 0f && IsGazed(cam.transform);

            if (gazed)
            {
                m_Progress += Time.deltaTime / dwellTime;
                var hands = HandPinchTracker.Instance;
                if (m_Progress >= 1f || (hands != null && hands.PinchStartedThisFrame))
                    Activate();
            }
            else
            {
                m_Progress = Mathf.Max(0f, m_Progress - Time.deltaTime / (dwellTime * 0.5f));
            }

            m_Flash = Mathf.Max(0f, m_Flash - Time.deltaTime);
            UpdateVisual();
        }

        bool IsGazed(Transform head)
        {
            Vector3 toButton = transform.position - head.position;
            float along = Vector3.Dot(toButton, head.forward);
            if (along <= 0f)
                return false;
            float radius = hitRadius * transform.lossyScale.x;
            return (toButton - head.forward * along).sqrMagnitude < radius * radius;
        }

        void Activate()
        {
            m_Progress = 0f;
            m_CooldownLeft = cooldown;
            m_Flash = 1f;
            onActivated.Invoke();
        }

        void UpdateVisual()
        {
            if (visual == null)
                return;

            float breathe = 0.5f + 0.5f * Mathf.Sin(Time.time * 1.5f);
            float eased = Mathf.SmoothStep(0f, 1f, m_Progress);
            visual.transform.localScale = m_BaseScale * (1f + 0.35f * eased + 0.05f * breathe + 0.5f * m_Flash);

            visual.GetPropertyBlock(m_Block);
            m_Block.SetColor(s_ColorId, Color.Lerp(idleColor, activeColor, Mathf.Max(eased, m_Flash)));
            m_Block.SetFloat(s_IntensityId, 0.8f + 0.4f * breathe + 1.5f * eased + 2f * m_Flash);
            visual.SetPropertyBlock(m_Block);
        }
    }
}
