using RushBank.Gameplay;
using UnityEngine;
using UnityEngine.UIElements;

namespace RushBank.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class GameHudUIController : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private StyleSheet themeStyleSheet;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private TellerServiceController tellerService;

        private Label scoreLabel;
        private Label comboLabel;
        private Label multiplierLabel;
        private Label customerNameLabel;
        private Label requestNameLabel;
        private Label requestLineLabel;
        private VisualElement timerFill;
        private Button completeRequestButton;
        private Button callNextButton;

        private void Awake()
        {
            if (document == null)
            {
                document = GetComponent<UIDocument>();
            }
        }

        private void OnEnable()
        {
            var root = document.rootVisualElement;
            ApplyTheme(root);
            scoreLabel = root.Q<Label>("score-label");
            comboLabel = root.Q<Label>("combo-label");
            multiplierLabel = root.Q<Label>("multiplier-label");
            customerNameLabel = root.Q<Label>("customer-name-label");
            requestNameLabel = root.Q<Label>("request-name-label");
            requestLineLabel = root.Q<Label>("request-line-label");
            timerFill = root.Q<VisualElement>("timer-fill");
            completeRequestButton = root.Q<Button>("complete-request-button");
            callNextButton = root.Q<Button>("call-next-button");

            BindEvents();
            ResetServiceInfo();
            UpdateScore(0);
            UpdateCombo(0);
            UpdateMultiplier(1f);
        }

        private void OnDisable()
        {
            UnbindEvents();
        }

        private void BindEvents()
        {
            if (scoreManager != null)
            {
                scoreManager.OnScoreChanged.AddListener(UpdateScore);
                scoreManager.OnComboChanged.AddListener(UpdateCombo);
                scoreManager.OnMultiplierChanged.AddListener(UpdateMultiplier);
            }

            if (tellerService != null)
            {
                tellerService.OnServiceStarted.AddListener(UpdateServiceInfo);
                tellerService.OnServiceCompleted.AddListener(ResetServiceInfo);
                tellerService.OnServiceTimerChanged.AddListener(UpdateTimer);
            }

            if (completeRequestButton != null)
            {
                completeRequestButton.clicked += CompleteRequest;
            }

            if (callNextButton != null)
            {
                callNextButton.clicked += CallNextCustomer;
            }
        }

        private void ApplyTheme(VisualElement root)
        {
            if (themeStyleSheet != null)
            {
                root.styleSheets.Add(themeStyleSheet);
            }
        }

        private void UnbindEvents()
        {
            if (scoreManager != null)
            {
                scoreManager.OnScoreChanged.RemoveListener(UpdateScore);
                scoreManager.OnComboChanged.RemoveListener(UpdateCombo);
                scoreManager.OnMultiplierChanged.RemoveListener(UpdateMultiplier);
            }

            if (tellerService != null)
            {
                tellerService.OnServiceStarted.RemoveListener(UpdateServiceInfo);
                tellerService.OnServiceCompleted.RemoveListener(ResetServiceInfo);
                tellerService.OnServiceTimerChanged.RemoveListener(UpdateTimer);
            }

            if (completeRequestButton != null)
            {
                completeRequestButton.clicked -= CompleteRequest;
            }

            if (callNextButton != null)
            {
                callNextButton.clicked -= CallNextCustomer;
            }
        }

        private void UpdateScore(int score)
        {
            if (scoreLabel != null)
            {
                scoreLabel.text = score.ToString();
            }
        }

        private void UpdateCombo(int combo)
        {
            if (comboLabel != null)
            {
                comboLabel.text = combo.ToString();
            }
        }

        private void UpdateMultiplier(float multiplier)
        {
            if (multiplierLabel != null)
            {
                multiplierLabel.text = $"x{multiplier:0.00}";
            }
        }

        private void UpdateServiceInfo(BankCustomer customer)
        {
            var request = customer != null ? customer.ActiveRequest : null;

            if (customerNameLabel != null)
            {
                customerNameLabel.text = customer != null && customer.Definition != null
                    ? customer.Definition.DisplayName
                    : "Müşteri";
            }

            if (requestNameLabel != null)
            {
                requestNameLabel.text = request != null ? request.DisplayName : "İşlem bekleniyor";
            }

            if (requestLineLabel != null)
            {
                requestLineLabel.text = request != null ? request.CustomerLine : string.Empty;
            }

            SetTimerPercent(0f);
        }

        private void ResetServiceInfo()
        {
            if (customerNameLabel != null)
            {
                customerNameLabel.text = "Sıradaki müşteri bekleniyor";
            }

            if (requestNameLabel != null)
            {
                requestNameLabel.text = "Gişeye müşteri geldiğinde işlem burada görünür.";
            }

            if (requestLineLabel != null)
            {
                requestLineLabel.text = string.Empty;
            }

            SetTimerPercent(0f);
        }

        private void UpdateTimer(float elapsedSeconds)
        {
            if (tellerService == null || tellerService.ActiveRequest == null)
            {
                SetTimerPercent(0f);
                return;
            }

            var targetSeconds = tellerService.ActiveRequest.TargetProcessingSeconds;
            var percent = targetSeconds <= 0f ? 1f : Mathf.Clamp01(elapsedSeconds / targetSeconds);
            SetTimerPercent(percent);
        }

        private void SetTimerPercent(float percent)
        {
            if (timerFill != null)
            {
                timerFill.style.width = Length.Percent(percent * 100f);
            }
        }

        private void CompleteRequest()
        {
            tellerService?.CompleteActiveRequest();
        }

        private void CallNextCustomer()
        {
            tellerService?.CallNextCustomer();
        }
    }
}
