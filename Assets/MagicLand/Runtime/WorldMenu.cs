using UnityEngine;
using UnityEngine.InputSystem;

namespace MagicLand
{
    /// <summary>
    /// World selection screen: touch a card with a finger, or look at it and pinch when it is out of reach.
    /// Keys 1, 2... also choose a world, for testing in the editor without a headset.
    /// </summary>
    public class WorldMenu : MonoBehaviour
    {
        [SerializeField] WorldCard[] cards = new WorldCard[0];
        [SerializeField] float launchDelay = 0.7f;

        WorldCard m_Chosen;
        float m_ChosenTime;
        float m_Time;

        public WorldCard Chosen => m_Chosen;

        public void Configure(WorldCard[] worldCards) => cards = worldCards;

        void Update()
        {
            float dt = Time.deltaTime;
            var hands = HandPinchTracker.Instance;
            var cam = Camera.main;
            var keyboard = Keyboard.current;

            WorldCard touched = null, gazed = null;
            var highlight = new float[cards.Length];
            for (int i = 0; i < cards.Length; i++)
            {
                for (int h = 0; h < 2 && hands != null; h++)
                {
                    if (!hands.TryGetIndexTip(h, out Vector3 tip))
                        continue;
                    highlight[i] = Mathf.Max(highlight[i], cards[i].Proximity(tip));
                    if (cards[i].IsTouchedBy(tip))
                        touched = cards[i];
                }
                if (cam != null && cards[i].IsHitBy(new Ray(cam.transform.position, cam.transform.forward)))
                {
                    gazed = cards[i];
                    highlight[i] = Mathf.Max(highlight[i], 0.6f);
                }
                if (keyboard != null && i < 9 && keyboard[Key.Digit1 + i].wasPressedThisFrame)
                    touched = cards[i];
            }

            if (touched != null)
                Choose(touched);
            else if (gazed != null && hands != null && hands.PinchStartedThisFrame)
                Choose(gazed);

            Step(highlight, dt);
        }

        /// <summary>Selects a world; a locked one only shakes its head.</summary>
        public void Choose(WorldCard card)
        {
            if (m_Chosen != null)
                return;
            if (card.Locked)
            {
                card.Wiggle();
                return;
            }
            m_Chosen = card;
            m_ChosenTime = 0f;
            card.MarkChosen();
            foreach (var other in cards)
                if (other != card)
                    other.FadeAway();
        }

        public void Step(float[] highlight, float dt)
        {
            m_Time += dt;
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i].SetHighlight(m_Chosen == null && highlight != null ? highlight[i] : 0f, dt);
                cards[i].Animate(m_Time, dt);
            }
            if (m_Chosen == null)
                return;
            m_ChosenTime += dt;
            if (m_ChosenTime >= launchDelay && ScreenFade.Instance != null)
                ScreenFade.Instance.FadeOutAndLoad(m_Chosen.SceneName);
        }
    }
}
