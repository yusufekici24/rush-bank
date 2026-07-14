using RushBank.Core;
using RushBank.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace RushBank.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("Queue")]
        [SerializeField] private QueueManager queueManager;
        [SerializeField] private Button callCustomerButton;
        [SerializeField, Min(0f)] private float callButtonCooldownSeconds = 2f;

        [Header("Request Icons")]
        [SerializeField] private GameObject requestIconPrefab;
        [SerializeField] private Vector3 iconOffset = new Vector3(0f, 2.2f, 0f);
        [SerializeField] private Color depositColor = new Color(0.25f, 0.9f, 0.35f);
        [SerializeField] private Color withdrawColor = new Color(0.95f, 0.2f, 0.2f);
        [SerializeField] private Color exchangeColor = new Color(0.25f, 0.55f, 1f);

        [Header("Timer UI")]
        [SerializeField] private Slider timeRemainingSlider;
        [SerializeField] private Text bonusTimeText;
        [SerializeField, Min(0.1f)] private float bonusTextDuration = 0.65f;
        [SerializeField] private Vector3 bonusTextMove = new Vector3(0f, 42f, 0f);

        private float callCooldownTimer;
        private GameObject activeRequestIcon;
        private Text activeRequestText;
        private Image activeRequestImage;
        private Vector3 bonusTextBasePosition;
        private float bonusTextTimer;

        private void Awake()
        {
            if (bonusTimeText != null)
            {
                bonusTextBasePosition = bonusTimeText.rectTransform.anchoredPosition;
                bonusTimeText.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (callCustomerButton != null)
            {
                callCustomerButton.onClick.AddListener(OnCallCustomerClicked);
            }

            if (queueManager != null)
            {
                queueManager.OnCustomerCalled.AddListener(ShowCustomerRequest);
            }

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnRemainingTimeChanged.AddListener(UpdateTimeRemaining);
                TimeManager.Instance.OnTimeAdded.AddListener(ShowBonusTime);
                UpdateTimeRemaining(TimeManager.Instance.RemainingSeconds);
            }
        }

        private void OnDisable()
        {
            if (callCustomerButton != null)
            {
                callCustomerButton.onClick.RemoveListener(OnCallCustomerClicked);
            }

            if (queueManager != null)
            {
                queueManager.OnCustomerCalled.RemoveListener(ShowCustomerRequest);
            }

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnRemainingTimeChanged.RemoveListener(UpdateTimeRemaining);
                TimeManager.Instance.OnTimeAdded.RemoveListener(ShowBonusTime);
            }
        }

        private void Update()
        {
            TickCallCooldown();
            TickBonusText();
            FaceRequestIconToCamera();
        }

        public void OnCallCustomerClicked()
        {
            if (callCooldownTimer > 0f || queueManager == null)
            {
                return;
            }

            queueManager.CallNextCustomer();
            callCooldownTimer = callButtonCooldownSeconds;
            UpdateCallButtonState();
        }

        public void ShowCustomerRequest(GameObject customerObject)
        {
            if (customerObject == null)
            {
                return;
            }

            ClearRequestIcon();

            var customer = customerObject.GetComponent<QueueCustomer>();
            if (customer == null)
            {
                return;
            }

            activeRequestIcon = CreateRequestIcon(customerObject.transform);
            activeRequestImage = activeRequestIcon.GetComponentInChildren<Image>();
            activeRequestText = activeRequestIcon.GetComponentInChildren<Text>();

            var requestColor = GetRequestColor(customer.RequestKind);
            if (activeRequestImage != null)
            {
                activeRequestImage.color = requestColor;
            }

            if (activeRequestText != null)
            {
                activeRequestText.text = GetRequestShortLabel(customer.RequestKind);
                activeRequestText.color = Color.white;
            }
        }

        public void UpdateTimeRemaining(float remainingSeconds)
        {
            if (timeRemainingSlider == null || TimeManager.Instance == null)
            {
                return;
            }

            var maxTime = Mathf.Max(1f, TimeManager.Instance.TimeLimitSeconds);
            timeRemainingSlider.value = Mathf.Clamp01(remainingSeconds / maxTime);
        }

        public void ShowBonusTime(float seconds)
        {
            if (bonusTimeText == null || seconds <= 0f)
            {
                return;
            }

            bonusTimeText.text = $"+{Mathf.RoundToInt(seconds)}s";
            bonusTimeText.rectTransform.anchoredPosition = bonusTextBasePosition;
            bonusTimeText.color = Color.green;
            bonusTimeText.gameObject.SetActive(true);
            bonusTextTimer = bonusTextDuration;
        }

        private void TickCallCooldown()
        {
            if (callCooldownTimer <= 0f)
            {
                return;
            }

            callCooldownTimer -= Time.deltaTime;
            if (callCooldownTimer <= 0f)
            {
                callCooldownTimer = 0f;
                UpdateCallButtonState();
            }
        }

        private void UpdateCallButtonState()
        {
            if (callCustomerButton != null)
            {
                callCustomerButton.interactable = callCooldownTimer <= 0f;
            }
        }

        private GameObject CreateRequestIcon(Transform customerTransform)
        {
            if (requestIconPrefab != null)
            {
                var icon = Instantiate(requestIconPrefab, customerTransform);
                icon.transform.localPosition = iconOffset;
                return icon;
            }

            var canvasObject = new GameObject("Customer Request Icon");
            canvasObject.transform.SetParent(customerTransform, false);
            canvasObject.transform.localPosition = iconOffset;

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObject.AddComponent<GraphicRaycaster>();

            var rect = canvasObject.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(96f, 96f);
            rect.localScale = Vector3.one * 0.012f;

            var backgroundObject = new GameObject("Icon Background");
            backgroundObject.transform.SetParent(canvasObject.transform, false);
            var image = backgroundObject.AddComponent<Image>();
            var imageRect = backgroundObject.GetComponent<RectTransform>();
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;

            var textObject = new GameObject("Icon Label");
            textObject.transform.SetParent(canvasObject.transform, false);
            var text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 42;
            text.fontStyle = FontStyle.Bold;
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return canvasObject;
        }

        private void ClearRequestIcon()
        {
            if (activeRequestIcon != null)
            {
                Destroy(activeRequestIcon);
                activeRequestIcon = null;
                activeRequestImage = null;
                activeRequestText = null;
            }
        }

        private Color GetRequestColor(CustomerRequestKind requestKind)
        {
            return requestKind switch
            {
                CustomerRequestKind.Deposit => depositColor,
                CustomerRequestKind.BillPayment => depositColor,
                CustomerRequestKind.PassbookPrinting => depositColor,
                CustomerRequestKind.CardBlockRemoval => depositColor,
                CustomerRequestKind.Withdraw => withdrawColor,
                CustomerRequestKind.CashWithdrawDeposit => withdrawColor,
                CustomerRequestKind.Thief => withdrawColor,
                _ => exchangeColor
            };
        }

        private static string GetRequestShortLabel(CustomerRequestKind requestKind)
        {
            return requestKind switch
            {
                CustomerRequestKind.Deposit => "D",
                CustomerRequestKind.Withdraw => "W",
                CustomerRequestKind.PassbookPrinting => "P",
                CustomerRequestKind.BillPayment => "B",
                CustomerRequestKind.CardBlockRemoval => "C",
                CustomerRequestKind.CashWithdrawDeposit => "$",
                CustomerRequestKind.CurrencyExchange => "FX",
                CustomerRequestKind.GoldExchange => "G",
                CustomerRequestKind.CreditApproval => "K",
                CustomerRequestKind.VipSafeRental => "VIP",
                CustomerRequestKind.Thief => "!",
                _ => "X"
            };
        }

        private void TickBonusText()
        {
            if (bonusTimeText == null || bonusTextTimer <= 0f)
            {
                return;
            }

            bonusTextTimer -= Time.deltaTime;
            var progress = 1f - Mathf.Clamp01(bonusTextTimer / bonusTextDuration);
            bonusTimeText.rectTransform.anchoredPosition = Vector3.Lerp(
                bonusTextBasePosition,
                bonusTextBasePosition + bonusTextMove,
                progress);

            var color = bonusTimeText.color;
            color.a = 1f - progress;
            bonusTimeText.color = color;

            if (bonusTextTimer <= 0f)
            {
                bonusTimeText.gameObject.SetActive(false);
            }
        }

        private void FaceRequestIconToCamera()
        {
            if (activeRequestIcon == null || Camera.main == null)
            {
                return;
            }

            var cameraTransform = Camera.main.transform;
            activeRequestIcon.transform.rotation = Quaternion.LookRotation(
                activeRequestIcon.transform.position - cameraTransform.position,
                Vector3.up);
        }
    }
}
