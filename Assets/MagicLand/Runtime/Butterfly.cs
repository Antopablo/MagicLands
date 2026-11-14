using UnityEngine;

namespace MagicLand
{
    /// <summary>
    /// Wandering butterfly with flapping wings. Can be asked to fly to a world point (a fingertip)
    /// and hover gently there.
    /// </summary>
    public class Butterfly : MonoBehaviour
    {
        [SerializeField] Transform leftWing;
        [SerializeField] Transform rightWing;
        [SerializeField] float roamRadius = 1.2f;
        [SerializeField] float maxSpeed = 0.7f;
        [Tooltip("How fast the wander target moves around the anchor.")]
        [SerializeField] float wanderSpeed = 0.15f;
        [SerializeField] int seed = 1;

        Vector3 m_Anchor;
        Vector3 m_Position;
        Vector3 m_Velocity;
        float m_FlapPhase;
        float m_Time;
        float m_Seed;
        bool m_HasHandTarget;
        Vector3 m_HandTargetWorld;
        bool m_Initialized;

        public bool IsVisitingHand => m_HasHandTarget;

        public void Configure(Transform left, Transform right, float radius, int randomSeed, float wander = 0.15f, float speed = 0.7f)
        {
            wanderSpeed = wander;
            maxSpeed = speed;
            leftWing = left;
            rightWing = right;
            roamRadius = radius;
            seed = randomSeed;
        }

        public void SetHandTarget(Vector3 worldPoint)
        {
            m_HasHandTarget = true;
            m_HandTargetWorld = worldPoint;
        }

        public void ClearHandTarget() => m_HasHandTarget = false;

        void Init()
        {
            m_Anchor = transform.localPosition;
            m_Position = m_Anchor;
            m_Seed = seed * 17.31f;
            m_Time = seed * 3.7f;
            m_Initialized = true;
        }

        void Update() => Step(Time.deltaTime);

        public void Step(float dt)
        {
            if (!m_Initialized)
                Init();
            m_Time += dt;

            Vector3 target;
            float speedLimit = maxSpeed;
            float distanceToTarget;
            if (m_HasHandTarget && transform.parent != null)
            {
                Vector3 hover = new Vector3(
                    Mathf.Sin(m_Time * 1.3f) * 0.015f,
                    0.035f + Mathf.Sin(m_Time * 2.1f) * 0.01f,
                    Mathf.Cos(m_Time * 1.1f) * 0.015f);
                target = transform.parent.InverseTransformPoint(m_HandTargetWorld) + hover;
                speedLimit = maxSpeed * 0.6f;
            }
            else
            {
                // Perlin noise stays close to 0.5, so the offset is amplified then clamped to the roam radius.
                float t = m_Time * wanderSpeed;
                var offset = new Vector3(
                    (Mathf.PerlinNoise(t, m_Seed) * 2f - 1f) * roamRadius * 1.8f,
                    (Mathf.PerlinNoise(m_Seed, t) * 2f - 1f) * roamRadius * 0.6f,
                    (Mathf.PerlinNoise(t + 31.7f, m_Seed + 11.3f) * 2f - 1f) * roamRadius * 1.8f);
                offset.x = Mathf.Clamp(offset.x, -roamRadius, roamRadius);
                offset.y = Mathf.Clamp(offset.y, -roamRadius * 0.4f, roamRadius * 0.4f);
                offset.z = Mathf.Clamp(offset.z, -roamRadius, roamRadius);
                target = m_Anchor + offset;
            }

            Vector3 toTarget = target - m_Position;
            distanceToTarget = toTarget.magnitude;
            Vector3 desired = Vector3.ClampMagnitude(toTarget * 1.2f, speedLimit);
            m_Velocity = Vector3.Lerp(m_Velocity, desired, 1f - Mathf.Exp(-2f * dt));
            m_Position += m_Velocity * dt;

            bool resting = m_HasHandTarget && distanceToTarget < 0.03f;
            float flapRate = resting ? 1.2f : 7f;
            m_FlapPhase += dt * flapRate * Mathf.PI * 2f;
            float glide = resting ? 0.35f : Mathf.Lerp(0.25f, 1f, Mathf.PerlinNoise(m_Time * 0.5f, m_Seed + 5f));
            float bob = resting ? 0f : Mathf.Sin(m_FlapPhase) * 0.012f * glide;
            transform.localPosition = m_Position + Vector3.up * bob;

            Vector3 flat = new Vector3(m_Velocity.x, m_Velocity.y * 0.3f, m_Velocity.z);
            if (flat.sqrMagnitude > 0.0004f)
            {
                var look = Quaternion.LookRotation(flat.normalized, Vector3.up);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, look, 1f - Mathf.Exp(-4f * dt));
            }

            float angle = 15f + Mathf.Sin(m_FlapPhase) * 55f * glide;
            if (rightWing != null)
                rightWing.localRotation = Quaternion.Euler(0f, 0f, angle);
            if (leftWing != null)
                leftWing.localRotation = Quaternion.Euler(0f, 0f, -angle);
        }
    }
}
