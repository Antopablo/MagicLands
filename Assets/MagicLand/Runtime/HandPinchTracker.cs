using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace MagicLand
{
    /// <summary>
    /// Reads tracked hands and exposes a simple pinch state for each hand.
    /// Displays small glowing lights on the thumb and index fingertips.
    /// </summary>
    public class HandPinchTracker : MonoBehaviour
    {
        [Tooltip("Tracking space (XR Origin). Hand joint poses are relative to it.")]
        [SerializeField] Transform trackingSpace;
        [SerializeField] Material fingertipMaterial;
        [SerializeField] float fingertipSize = 0.018f;
        [SerializeField] float pinchStartDistance = 0.02f;
        [SerializeField] float pinchEndDistance = 0.035f;
        [Tooltip("Below this fingertip speed (m/s) the hand is considered still.")]
        [SerializeField] float stillSpeed = 0.2f;

        public static HandPinchTracker Instance { get; private set; }

        public bool IsAnyHandTracked => m_Hands[0].tracked || m_Hands[1].tracked;
        public bool IsAnyPinching => m_Hands[0].pinching || m_Hands[1].pinching;
        /// <summary>True only on the frame a pinch starts, on either hand.</summary>
        public bool PinchStartedThisFrame { get; private set; }
        /// <summary>Longest current continuous pinch duration, in seconds.</summary>
        public float PinchHoldTime => Mathf.Max(m_Hands[0].pinchTime, m_Hands[1].pinchTime);

        /// <summary>
        /// Index fingertip of the stillest tracked, non-pinching hand, and how long it has been still.
        /// </summary>
        public bool TryGetRestingFingertip(out Vector3 position, out float stillTime)
        {
            position = default;
            stillTime = -1f;
            foreach (var hand in m_Hands)
            {
                if (hand.tracked && !hand.pinching && hand.stillTime > stillTime)
                {
                    stillTime = hand.stillTime;
                    position = hand.indexTip;
                }
            }
            return stillTime >= 0f;
        }

        class HandState
        {
            public bool tracked;
            public bool pinching;
            public float pinchTime;
            public float stillTime;
            public Vector3 indexTip;
            public bool hasPrevious;
            public Transform thumbLight;
            public Transform indexLight;
        }

        readonly HandState[] m_Hands = { new HandState(), new HandState() };
        XRHandSubsystem m_Subsystem;
        static readonly List<XRHandSubsystem> s_Subsystems = new List<XRHandSubsystem>();

        public void Configure(Transform space, Material material)
        {
            trackingSpace = space;
            fingertipMaterial = material;
        }

        void Awake()
        {
            Instance = this;
            EnsureLights();
        }

        // Also called from Update: a script reload during play mode resets the hand states.
        void EnsureLights()
        {
            foreach (var hand in m_Hands)
            {
                if (hand.thumbLight == null)
                    hand.thumbLight = CreateLight("Thumb Light");
                if (hand.indexLight == null)
                    hand.indexLight = CreateLight("Index Light");
            }
        }

        Transform CreateLight(string lightName)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = lightName;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * fingertipSize;
            go.GetComponent<MeshRenderer>().sharedMaterial = fingertipMaterial;
            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.SetActive(false);
            return go.transform;
        }

        void Update()
        {
            PinchStartedThisFrame = false;
            Instance = this;
            EnsureLights();

            if (m_Subsystem == null || !m_Subsystem.running)
            {
                SubsystemManager.GetSubsystems(s_Subsystems);
                m_Subsystem = null;
                foreach (var subsystem in s_Subsystems)
                {
                    if (subsystem.running)
                    {
                        m_Subsystem = subsystem;
                        break;
                    }
                }
            }

            if (m_Subsystem == null)
            {
                SetHand(m_Hands[0], default, false);
                SetHand(m_Hands[1], default, false);
                return;
            }

            SetHand(m_Hands[0], m_Subsystem.leftHand, true);
            SetHand(m_Hands[1], m_Subsystem.rightHand, true);
        }

        void SetHand(HandState state, XRHand hand, bool available)
        {
            bool hasThumb = false, hasIndex = false;
            Pose thumb = default, index = default;
            if (available && hand.isTracked)
            {
                hasThumb = hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out thumb);
                hasIndex = hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out index);
            }

            state.tracked = hasThumb && hasIndex;
            if (!state.tracked)
            {
                state.pinching = false;
                state.pinchTime = 0f;
                state.stillTime = 0f;
                state.hasPrevious = false;
                state.thumbLight.gameObject.SetActive(false);
                state.indexLight.gameObject.SetActive(false);
                return;
            }

            Vector3 thumbPos = ToWorld(thumb.position);
            Vector3 indexPos = ToWorld(index.position);
            float distance = Vector3.Distance(thumbPos, indexPos);

            float speed = state.hasPrevious && Time.deltaTime > 0f
                ? Vector3.Distance(indexPos, state.indexTip) / Time.deltaTime
                : 0f;
            state.stillTime = speed < stillSpeed ? state.stillTime + Time.deltaTime : 0f;
            state.indexTip = indexPos;
            state.hasPrevious = true;

            bool wasPinching = state.pinching;
            state.pinching = wasPinching ? distance < pinchEndDistance : distance < pinchStartDistance;
            state.pinchTime = state.pinching ? state.pinchTime + Time.deltaTime : 0f;
            if (state.pinching && !wasPinching)
                PinchStartedThisFrame = true;

            PlaceLight(state.thumbLight, thumbPos, state.pinching);
            PlaceLight(state.indexLight, indexPos, state.pinching);
        }

        Vector3 ToWorld(Vector3 trackingPosition) =>
            trackingSpace != null ? trackingSpace.TransformPoint(trackingPosition) : trackingPosition;

        void PlaceLight(Transform lightTransform, Vector3 position, bool pinching)
        {
            lightTransform.gameObject.SetActive(true);
            lightTransform.position = position;
            var cam = Camera.main;
            if (cam != null)
                lightTransform.rotation = Quaternion.LookRotation(position - cam.transform.position, cam.transform.up);
            lightTransform.localScale = Vector3.one * fingertipSize * (pinching ? 1.8f : 1f);
        }
    }
}
