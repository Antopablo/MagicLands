using UnityEngine;

namespace MagicLand
{
    /// <summary>
    /// When the user holds a hand still, the closest butterfly comes to hover above the index fingertip.
    /// A pinch or losing the hand lets it fly away.
    /// </summary>
    public class ButterflyGuide : MonoBehaviour
    {
        [SerializeField] float stillTimeNeeded = 1.2f;

        Butterfly[] m_Butterflies;
        Butterfly m_Visitor;

        void Start() => m_Butterflies = GetComponentsInChildren<Butterfly>();

        void Update()
        {
            var hands = HandPinchTracker.Instance;
            float stillTime = 0f;
            Vector3 tip = default;
            bool hasHand = hands != null && hands.TryGetRestingFingertip(out tip, out stillTime);

            if (m_Visitor != null)
            {
                if (hasHand)
                {
                    m_Visitor.SetHandTarget(tip);
                }
                else
                {
                    m_Visitor.ClearHandTarget();
                    m_Visitor = null;
                }
                return;
            }

            if (!hasHand || stillTime < stillTimeNeeded || m_Butterflies == null || m_Butterflies.Length == 0)
                return;

            float best = float.MaxValue;
            foreach (var butterfly in m_Butterflies)
            {
                float d = (butterfly.transform.position - tip).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    m_Visitor = butterfly;
                }
            }
            m_Visitor.SetHandTarget(tip);
        }
    }
}
