using System.Collections;
using RushBank.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RushBank.Gameplay
{
    public enum GoldExchangeState
    {
        ReceiveGold,
        EvaluateGold,
        DeliverValueReceipt
    }

    public class GoldExchangeWorkflow : MonoBehaviour
    {
        [Header("Stations")]
        [SerializeField] private string counterTag = "Counter";
        [SerializeField] private string expertiseStationTag = "ExpertiseStation";

        [Header("Carry")]
        [SerializeField] private Transform holdPoint;
        [SerializeField] private GameObject goldBagPrefab;
        [SerializeField] private GameObject goldBarPrefab;
        [SerializeField] private GameObject valueReceiptPrefab;

        [Header("Evaluation")]
        [SerializeField, Min(0.1f)] private float evaluationSeconds = 2f;
        [SerializeField] private Slider evaluationProgressBar;
        [SerializeField] private GameObject evaluationScaleVisual;
        [SerializeField] private ParticleSystem goldSparkleEffect;

        [Header("Rewards")]
        [SerializeField, Min(0f)] private float bonusTime = 10f;
        [SerializeField, Min(0)] private int points = 180;

        public UnityEvent<GoldExchangeState> OnStateChanged = new UnityEvent<GoldExchangeState>();
        public UnityEvent OnGoldExchangeCompleted = new UnityEvent();
        public UnityEvent OnGoldExchangeFailed = new UnityEvent();

        private GoldExchangeState currentState = GoldExchangeState.ReceiveGold;
        private GameObject nearbyStation;
        private GameObject heldItem;
        private Coroutine evaluationRoutine;
        private bool workflowActive;
        private float actionTimeMultiplier = 1f;

        public GoldExchangeState CurrentState => currentState;
        public bool IsWorkflowActive => workflowActive;
        public GameObject HeldItem => heldItem;
        public float ActionTimeMultiplier
        {
            get => actionTimeMultiplier;
            set => actionTimeMultiplier = Mathf.Max(0.05f, value);
        }

        private void Awake()
        {
            SetProgress(evaluationProgressBar, 0f, false);
            if (evaluationScaleVisual != null)
            {
                evaluationScaleVisual.SetActive(false);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsWorkflowStation(other))
            {
                nearbyStation = other.gameObject;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (nearbyStation == other.gameObject)
            {
                nearbyStation = null;
            }
        }

        public void StartWorkflow()
        {
            workflowActive = true;
            SetState(GoldExchangeState.ReceiveGold);
        }

        public void Interact()
        {
            if (!workflowActive)
            {
                StartWorkflow();
            }

            switch (currentState)
            {
                case GoldExchangeState.ReceiveGold:
                    TryReceiveGold();
                    break;
                case GoldExchangeState.EvaluateGold:
                    TryEvaluateOrGrabReceipt();
                    break;
                case GoldExchangeState.DeliverValueReceipt:
                    TryDeliverReceipt();
                    break;
            }
        }

        private void TryReceiveGold()
        {
            if (!IsAtStation(counterTag) || holdPoint == null || heldItem != null)
            {
                Fail();
                return;
            }

            heldItem = CreateHeldItem(PickGoldPrefab(), "Gold Exchange Item", new Color(1f, 0.72f, 0.12f));
            SetState(GoldExchangeState.EvaluateGold);
        }

        private void TryEvaluateOrGrabReceipt()
        {
            if (!IsAtStation(expertiseStationTag) || evaluationRoutine != null)
            {
                Fail();
                return;
            }

            if (heldItem != null)
            {
                evaluationRoutine = StartCoroutine(EvaluationRoutine());
                return;
            }

            heldItem = CreateHeldItem(valueReceiptPrefab, "Value Receipt", new Color(0.76f, 1f, 0.78f));
            SetState(GoldExchangeState.DeliverValueReceipt);
        }

        private void TryDeliverReceipt()
        {
            if (!IsAtStation(counterTag) || heldItem == null)
            {
                Fail();
                return;
            }

            DestroyHeldItem();

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.AddTime(bonusTime);
            }

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(points);
            }

            if (goldSparkleEffect != null)
            {
                goldSparkleEffect.Play();
            }

            workflowActive = false;
            OnGoldExchangeCompleted.Invoke();
            SetState(GoldExchangeState.ReceiveGold);
        }

        private IEnumerator EvaluationRoutine()
        {
            var placedGold = heldItem;
            heldItem = null;
            placedGold.transform.SetParent(nearbyStation.transform, true);
            placedGold.transform.position = nearbyStation.transform.position + Vector3.up * 0.9f;

            SetProgress(evaluationProgressBar, 0f, true);
            if (evaluationScaleVisual != null)
            {
                evaluationScaleVisual.SetActive(true);
            }

            var baseScale = placedGold.transform.localScale;
            var progress = 0f;
            while (progress < 1f)
            {
                var duration = Mathf.Max(0.05f, evaluationSeconds * actionTimeMultiplier);
                progress += Time.deltaTime / duration;
                SetProgress(evaluationProgressBar, progress, true);

                var pulse = 1f + Mathf.Sin(progress * Mathf.PI * 4f) * 0.06f;
                placedGold.transform.localScale = baseScale * pulse;
                yield return null;
            }

            if (evaluationScaleVisual != null)
            {
                evaluationScaleVisual.SetActive(false);
            }

            SetProgress(evaluationProgressBar, 0f, false);
            Destroy(placedGold);
            heldItem = CreateHeldItem(valueReceiptPrefab, "Value Receipt", new Color(0.76f, 1f, 0.78f));
            SetState(GoldExchangeState.DeliverValueReceipt);
            evaluationRoutine = null;
        }

        private GameObject PickGoldPrefab()
        {
            if (goldBagPrefab != null && goldBarPrefab != null)
            {
                return Random.value > 0.5f ? goldBagPrefab : goldBarPrefab;
            }

            return goldBagPrefab != null ? goldBagPrefab : goldBarPrefab;
        }

        private GameObject CreateHeldItem(GameObject prefab, string itemName, Color fallbackColor)
        {
            GameObject item;
            if (prefab != null)
            {
                item = Instantiate(prefab, holdPoint);
            }
            else
            {
                item = GameObject.CreatePrimitive(PrimitiveType.Cube);
                item.transform.SetParent(holdPoint, false);
                item.transform.localScale = new Vector3(0.42f, 0.18f, 0.28f);

                var renderer = item.GetComponent<Renderer>();
                if (renderer != null)
                {
                    var material = new Material(Shader.Find("Standard"));
                    material.color = fallbackColor;
                    renderer.sharedMaterial = material;
                }
            }

            item.name = itemName;
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;

            if (item.TryGetComponent<Rigidbody>(out var body))
            {
                body.isKinematic = true;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            var colliders = item.GetComponentsInChildren<Collider>();
            for (var i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }

            return item;
        }

        private void DestroyHeldItem()
        {
            if (heldItem == null)
            {
                return;
            }

            var item = heldItem;
            heldItem = null;
            Destroy(item);
        }

        private bool IsWorkflowStation(Collider other)
        {
            var objectTag = other.gameObject.tag;
            return objectTag == counterTag || objectTag == expertiseStationTag;
        }

        private bool IsAtStation(string stationTag)
        {
            return nearbyStation != null && nearbyStation.tag == stationTag;
        }

        private void SetState(GoldExchangeState state)
        {
            currentState = state;
            OnStateChanged.Invoke(currentState);
        }

        private void Fail()
        {
            OnGoldExchangeFailed.Invoke();
        }

        private static void SetProgress(Slider slider, float value, bool visible)
        {
            if (slider == null)
            {
                return;
            }

            slider.value = Mathf.Clamp01(value);
            slider.gameObject.SetActive(visible);
        }
    }
}
