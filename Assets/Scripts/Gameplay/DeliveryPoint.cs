using RushBank.Core;
using UnityEngine;

namespace RushBank.Gameplay
{
    [RequireComponent(typeof(Collider))]
    public class DeliveryPoint : MonoBehaviour
    {
        [Header("Quest Match")]
        [SerializeField] private ScenarioRunner scenarioRunner;
        [SerializeField] private string requiredItemId = "item_id";
        [SerializeField] private bool useCurrentScenarioTarget = true;
        [SerializeField] private bool matchColor;
        [SerializeField] private Color requiredColor = Color.white;
        [SerializeField, Range(0f, 0.5f)] private float colorTolerance = 0.08f;

        [Header("Reward")]
        [SerializeField, Min(0f)] private float deliveryRewardTime = 5f;

        [Header("Feedback")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip successClip;
        [SerializeField] private Transform bounceTarget;
        [SerializeField] private Color feedbackColor = new Color(0.25f, 1f, 0.45f);
        [SerializeField, Min(0.1f)] private float feedbackDuration = 0.65f;
        [SerializeField, Min(0.01f)] private float bounceScale = 1.08f;

        private TextMesh feedbackText;
        private Vector3 feedbackStartPosition;
        private Vector3 bounceBaseScale;
        private float feedbackTimer;
        private PlayerInteraction currentPlayer;

        private void Awake()
        {
            var trigger = GetComponent<Collider>();
            trigger.isTrigger = true;

            if (bounceTarget == null)
            {
                bounceTarget = transform;
            }

            bounceBaseScale = bounceTarget.localScale;
            EnsureFeedbackText();
        }

        private void Update()
        {
            TickFeedback();
        }

        private void OnTriggerEnter(Collider other)
        {
            var interaction = other.GetComponentInParent<PlayerInteraction>();
            if (interaction == null)
            {
                return;
            }

            currentPlayer = interaction;
            TryDeliver(interaction);
        }

        private void OnTriggerStay(Collider other)
        {
            if (currentPlayer != null)
            {
                TryDeliver(currentPlayer);
                return;
            }

            var interaction = other.GetComponentInParent<PlayerInteraction>();
            if (interaction != null)
            {
                currentPlayer = interaction;
                TryDeliver(interaction);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var interaction = other.GetComponentInParent<PlayerInteraction>();
            if (interaction == currentPlayer)
            {
                currentPlayer = null;
            }
        }

        public bool TryDeliver(PlayerInteraction interaction)
        {
            if (interaction == null || !interaction.IsHolding || !IsCorrectItem(interaction.HeldObject))
            {
                return false;
            }

            if (!interaction.TryDestroyHeldObject())
            {
                return false;
            }

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.AddTime(deliveryRewardTime);
            }

            PlaySuccessFeedback();
            return true;
        }

        private bool IsCorrectItem(GameObject heldObject)
        {
            if (heldObject == null)
            {
                return false;
            }

            var deliverable = heldObject.GetComponentInParent<DeliverableItem>();
            if (deliverable == null)
            {
                return false;
            }

            var expectedItemId = GetExpectedItemId();
            var idMatches = string.IsNullOrEmpty(expectedItemId) || deliverable.ItemId == expectedItemId;
            var colorMatches = !matchColor || ColorsAreClose(deliverable.ItemColor, requiredColor);

            return idMatches && colorMatches;
        }

        private string GetExpectedItemId()
        {
            if (useCurrentScenarioTarget && scenarioRunner != null && scenarioRunner.CurrentStep != null)
            {
                return scenarioRunner.CurrentStep.TargetId;
            }

            return requiredItemId;
        }

        private bool ColorsAreClose(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) <= colorTolerance
                && Mathf.Abs(a.g - b.g) <= colorTolerance
                && Mathf.Abs(a.b - b.b) <= colorTolerance;
        }

        private void PlaySuccessFeedback()
        {
            feedbackTimer = feedbackDuration;

            if (feedbackText != null)
            {
                feedbackText.text = $"+{Mathf.RoundToInt(deliveryRewardTime)}s";
                feedbackText.color = feedbackColor;
                feedbackText.gameObject.SetActive(true);
                feedbackText.transform.localPosition = feedbackStartPosition;
            }

            if (audioSource != null && successClip != null)
            {
                audioSource.PlayOneShot(successClip);
            }
        }

        private void TickFeedback()
        {
            if (feedbackTimer <= 0f)
            {
                return;
            }

            feedbackTimer -= Time.deltaTime;
            var progress = 1f - Mathf.Clamp01(feedbackTimer / feedbackDuration);
            var bounce = Mathf.Sin(progress * Mathf.PI);

            if (bounceTarget != null)
            {
                bounceTarget.localScale = Vector3.Lerp(bounceBaseScale, bounceBaseScale * bounceScale, bounce);
            }

            if (feedbackText != null)
            {
                feedbackText.transform.localPosition = feedbackStartPosition + (Vector3.up * progress * 0.75f);

                var camera = Camera.main;
                if (camera != null)
                {
                    feedbackText.transform.rotation = Quaternion.LookRotation(feedbackText.transform.position - camera.transform.position);
                }

                var color = feedbackColor;
                color.a = 1f - progress;
                feedbackText.color = color;
            }

            if (feedbackTimer <= 0f)
            {
                if (bounceTarget != null)
                {
                    bounceTarget.localScale = bounceBaseScale;
                }

                if (feedbackText != null)
                {
                    feedbackText.gameObject.SetActive(false);
                }
            }
        }

        private void EnsureFeedbackText()
        {
            var go = new GameObject("Delivery Feedback Text");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.up * 1.2f;

            feedbackText = go.AddComponent<TextMesh>();
            feedbackText.anchor = TextAnchor.MiddleCenter;
            feedbackText.alignment = TextAlignment.Center;
            feedbackText.characterSize = 0.28f;
            feedbackText.text = $"+{Mathf.RoundToInt(deliveryRewardTime)}s";
            feedbackText.color = feedbackColor;
            feedbackText.gameObject.SetActive(false);
            feedbackStartPosition = feedbackText.transform.localPosition;
        }
    }
}
