using UnityEngine;

namespace MagicLand
{
    /// <summary>A few birds gliding in slow wide circles in the sky, with a flap now and then.</summary>
    public class SoaringBirds : MonoBehaviour
    {
        [SerializeField] BirdRig[] birds = new BirdRig[0];
        [SerializeField] Vector3[] centers = new Vector3[0];
        [SerializeField] float[] radii = new float[0];
        [Tooltip("Angular speed in radians per second; negative turns the other way.")]
        [SerializeField] float[] speeds = new float[0];

        float m_Time;

        public void Configure(BirdRig[] rigs, Vector3[] circleCenters, float[] circleRadii, float[] angularSpeeds)
        {
            birds = rigs;
            centers = circleCenters;
            radii = circleRadii;
            speeds = angularSpeeds;
        }

        void Start() => Step(0f);

        void Update() => Step(Time.deltaTime);

        public void Step(float dt)
        {
            m_Time += dt;
            for (int i = 0; i < birds.Length; i++)
            {
                float a = m_Time * speeds[i] + i * 2.1f;
                float r = radii[i];
                // Flattened circle (shallower in depth) with a gentle rise and fall.
                Vector3 p = centers[i] + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(m_Time * 0.23f + i * 1.7f) * 0.9f, Mathf.Sin(a) * r * 0.55f);
                Vector3 velocity = new Vector3(-Mathf.Sin(a) * r, 0f, Mathf.Cos(a) * r * 0.55f) * Mathf.Sign(speeds[i]);
                var rig = birds[i];
                rig.root.localPosition = p;
                rig.root.localRotation = Quaternion.LookRotation(velocity, Vector3.up) * Quaternion.Euler(0f, 0f, 14f * Mathf.Sign(speeds[i]));

                float burst = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Mathf.PerlinNoise(m_Time * 0.2f, i * 5.3f) - 0.55f) * 8f));
                float flap = 6f + Mathf.Sin((m_Time + i) * 13f) * 42f * burst;
                rig.Pose(1f, flap, 0f, 0f, 0f);
            }
        }
    }
}
