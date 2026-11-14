using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace MagicLand
{
    /// <summary>
    /// Places the content root in front of the user's current gaze, whatever the body posture.
    /// The content is designed facing +Z, so it works when the user is lying down.
    /// </summary>
    public class ViewRecenter : MonoBehaviour
    {
        [SerializeField] Transform head;
        [SerializeField] float settleDelay = 0.6f;
        [SerializeField] float transitionDuration = 1.2f;
        [SerializeField] float pinchHoldDuration = 2f;

        static readonly List<XRInputSubsystem> s_Subsystems = new List<XRInputSubsystem>();

        bool m_HasCentered;
        float m_TrackedSince = -1f;
        float m_LastPinchHold;
        float m_TransitionTime = -1f;
        Vector3 m_FromPos, m_ToPos;
        Quaternion m_FromRot, m_ToRot;

        public void SetHead(Transform headTransform) => head = headTransform;

        void OnEnable()
        {
            SubsystemManager.GetSubsystems(s_Subsystems);
            foreach (var subsystem in s_Subsystems)
            {
                subsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Device);
                subsystem.trackingOriginUpdated += OnTrackingOriginUpdated;
            }
        }

        void OnDisable()
        {
            foreach (var subsystem in s_Subsystems)
                subsystem.trackingOriginUpdated -= OnTrackingOriginUpdated;
        }

        void OnTrackingOriginUpdated(XRInputSubsystem subsystem) => Recenter(false);

        void Update()
        {
            if (!m_HasCentered)
            {
                if (IsHeadTracked())
                {
                    if (m_TrackedSince < 0f)
                        m_TrackedSince = Time.time;
                    if (Time.time - m_TrackedSince >= settleDelay)
                        Recenter(false);
                }
                else if (!XRSettings.isDeviceActive && Time.time > 0.5f)
                {
                    // Editor without headset: keep the default placement.
                    m_HasCentered = true;
                }
            }

            // Holding a pinch recenters on the current gaze, without having to look at a button.
            var hands = HandPinchTracker.Instance;
            if (hands != null)
            {
                float hold = hands.PinchHoldTime;
                if (hold >= pinchHoldDuration && m_LastPinchHold < pinchHoldDuration)
                    Recenter(true);
                m_LastPinchHold = hold;
            }

            if (m_TransitionTime >= 0f)
            {
                m_TransitionTime += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(m_TransitionTime / transitionDuration));
                transform.SetPositionAndRotation(
                    Vector3.Lerp(m_FromPos, m_ToPos, k),
                    Quaternion.Slerp(m_FromRot, m_ToRot, k));
                if (k >= 1f)
                    m_TransitionTime = -1f;
            }
        }

        static bool IsHeadTracked()
        {
            var device = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            return device.isValid
                && device.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked)
                && tracked;
        }

        /// <summary>Moves the content in front of the current gaze. Smooth when requested by the user.</summary>
        public void Recenter(bool smooth)
        {
            if (head == null)
                return;

            m_HasCentered = true;
            m_ToPos = head.position;
            m_ToRot = head.rotation;

            if (smooth)
            {
                m_FromPos = transform.position;
                m_FromRot = transform.rotation;
                m_TransitionTime = 0f;
            }
            else
            {
                m_TransitionTime = -1f;
                transform.SetPositionAndRotation(m_ToPos, m_ToRot);
            }
        }

        public void RecenterSmooth() => Recenter(true);

        /// <summary>
        /// Recenters after a short delay, letting the user look back to a comfortable direction
        /// after activating a button placed off-center.
        /// </summary>
        public void RecenterDelayed(float delay)
        {
            CancelInvoke(nameof(RecenterSmooth));
            Invoke(nameof(RecenterSmooth), delay);
        }

        public bool IsRecenterPending => IsInvoking(nameof(RecenterSmooth));
    }
}
