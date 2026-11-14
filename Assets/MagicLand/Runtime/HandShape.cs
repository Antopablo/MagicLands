using UnityEngine;

namespace MagicLand
{
    /// <summary>Pure hand pose maths, independent from the tracking API so it can be tested without a headset.</summary>
    public static class HandShape
    {
        /// <summary>How straight a finger is: 1 when fully extended, about 0.5 when curled.</summary>
        public static float Straightness(Vector3 proximal, Vector3 intermediate, Vector3 distal, Vector3 tip)
        {
            float path = Vector3.Distance(proximal, intermediate) + Vector3.Distance(intermediate, distal) + Vector3.Distance(distal, tip);
            return path > 1e-5f ? Vector3.Distance(proximal, tip) / path : 0f;
        }

        /// <summary>Direction the palm faces, from three points of the hand (works for any hand size).</summary>
        public static Vector3 PalmNormal(Vector3 wrist, Vector3 indexProximal, Vector3 littleProximal, bool leftHand)
        {
            Vector3 n = Vector3.Cross(indexProximal - wrist, littleProximal - wrist).normalized;
            return leftHand ? n : -n;
        }

        /// <summary>
        /// True for a wide open hand (the four long fingers extended), held in front of the head
        /// with the palm turned toward the face.
        /// </summary>
        public static bool IsOpenPalmFacingHead(Vector3 palmCenter, Vector3 palmNormal, float minStraightness,
            Vector3 headPosition, Vector3 headForward)
        {
            if (minStraightness < 0.86f)
                return false;
            Vector3 toHead = headPosition - palmCenter;
            float distance = toHead.magnitude;
            if (distance > 0.55f || distance < 0.08f)
                return false;
            if (Vector3.Dot(headForward, -toHead) < 0.1f)
                return false;
            return Vector3.Dot(palmNormal, toHead / distance) > 0.6f;
        }
    }
}
