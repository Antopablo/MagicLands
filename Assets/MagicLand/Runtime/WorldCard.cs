using UnityEngine;

namespace MagicLand
{
    /// <summary>One world in the selection screen: a floating picture that can be touched, or looked at and pinched.</summary>
    public class WorldCard : MonoBehaviour
    {
        [SerializeField] string sceneName;
        [Tooltip("A locked card shows a world that is not available yet.")]
        [SerializeField] bool locked;
        [SerializeField] Renderer picture;
        [SerializeField] Vector2 size = new Vector2(0.26f, 0.195f);

        static readonly int s_HighlightId = Shader.PropertyToID("_Highlight");
        static readonly int s_DimId = Shader.PropertyToID("_Dim");

        MaterialPropertyBlock m_Block;
        Vector3 m_BaseScale;
        Vector3 m_BasePosition;
        float m_Highlight;
        float m_Wiggle;
        float m_Chosen;
        float m_Faded;

        public string SceneName => sceneName;
        public bool Locked => locked;

        public void Configure(string scene, bool isLocked, Renderer pictureRenderer, Vector2 cardSize)
        {
            sceneName = scene;
            locked = isLocked;
            picture = pictureRenderer;
            size = cardSize;
        }

        void Awake()
        {
            m_Block = new MaterialPropertyBlock();
            m_BaseScale = picture.transform.localScale;
            m_BasePosition = transform.localPosition;
        }

        /// <summary>Position of a point in card space: x right, y up (in meters), z distance in front of the card.</summary>
        Vector3 Local(Vector3 world)
        {
            // The card root is never scaled, so local units are meters; the picture faces -Z.
            Vector3 p = transform.InverseTransformPoint(world);
            return new Vector3(p.x, p.y, -p.z);
        }

        bool Inside(Vector3 local, float margin) =>
            Mathf.Abs(local.x) < size.x * 0.5f + margin && Mathf.Abs(local.y) < size.y * 0.5f + margin;

        /// <summary>True when the fingertip touches the card (or has gone slightly through it).</summary>
        public bool IsTouchedBy(Vector3 fingertip)
        {
            Vector3 p = Local(fingertip);
            return Inside(p, 0.015f) && p.z < 0.015f && p.z > -0.08f;
        }

        /// <summary>How close a fingertip is to touching (0 far away, 1 touching).</summary>
        public float Proximity(Vector3 fingertip)
        {
            Vector3 p = Local(fingertip);
            if (!Inside(p, 0.04f) || p.z < -0.08f)
                return 0f;
            return Mathf.Clamp01(1f - (p.z - 0.015f) / 0.15f);
        }

        /// <summary>True when the ray crosses the card from its front side.</summary>
        public bool IsHitBy(Ray ray)
        {
            var plane = new Plane(-transform.forward, transform.position);
            if (!plane.Raycast(ray, out float distance) || Vector3.Dot(ray.direction, transform.forward) <= 0f)
                return false;
            return Inside(Local(ray.GetPoint(distance)), 0.02f);
        }

        public void SetHighlight(float target, float dt) =>
            m_Highlight = Mathf.MoveTowards(m_Highlight, target, dt * 4f);

        /// <summary>Little "no" shake for a world that is not ready yet.</summary>
        public void Wiggle() => m_Wiggle = 1f;

        public void MarkChosen() => m_Chosen = Mathf.Max(m_Chosen, 0.001f);

        public void FadeAway() => m_Faded = Mathf.Max(m_Faded, 0.001f);

        public void Animate(float time, float dt)
        {
            m_Wiggle = Mathf.Max(0f, m_Wiggle - dt * 1.5f);
            if (m_Chosen > 0f)
                m_Chosen = Mathf.Min(1f, m_Chosen + dt * 2f);
            if (m_Faded > 0f)
                m_Faded = Mathf.Min(1f, m_Faded + dt * 2f);

            float bob = Mathf.Sin(time * 0.8f + m_BasePosition.x * 10f) * 0.006f;
            float shake = Mathf.Sin(m_Wiggle * 30f) * m_Wiggle * 0.012f;
            transform.localPosition = m_BasePosition + transform.localRotation * new Vector3(shake, bob, 0f)
                - transform.localRotation * Vector3.forward * (0.03f * Mathf.SmoothStep(0f, 1f, m_Chosen));
            float grow = 1f + 0.05f * m_Highlight + 0.08f * Mathf.SmoothStep(0f, 1f, m_Chosen);
            picture.transform.localScale = m_BaseScale * grow * (1f - 0.3f * m_Faded);

            picture.GetPropertyBlock(m_Block);
            m_Block.SetFloat(s_HighlightId, Mathf.Max(m_Highlight, m_Chosen));
            m_Block.SetFloat(s_DimId, Mathf.Max(locked ? 0.7f : 0f, m_Faded));
            picture.SetPropertyBlock(m_Block);
        }
    }
}
