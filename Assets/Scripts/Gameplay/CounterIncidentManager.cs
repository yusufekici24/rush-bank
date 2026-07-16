using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RushBank.Gameplay
{
    public class CounterIncidentManager : MonoBehaviour
    {
        public static CounterIncidentManager Instance { get; private set; }

        [Header("Core References")]
        [SerializeField] private QueueManager queueManager;
        [SerializeField] private SecurityGuardAI securityGuardAI;
        [SerializeField] private MobilePlayerController mobilePlayerController;
        [SerializeField] private ChubbyTopDownInputController topDownController;
        [SerializeField] private Transform counterPoint;
        [SerializeField] private Transform mainExitDoor;
        [SerializeField] private Transform guardIdlePost;

        [Header("Transaction Systems")]
        [SerializeField] private WireTransferMiniGame wireTransferMiniGame;
        [SerializeField] private MobileActivationMiniGame mobileActivationMiniGame;
        [SerializeField] private CardBlockMiniGame cardBlockMiniGame;
        [SerializeField] private UtilityBillSystem utilityBillSystem;
        [SerializeField] private FastTrackActionSystem fastTrackActionSystem;
        [SerializeField] private BankingActionSystem bankingActionSystem;
        [SerializeField] private DocumentProcessWorkflow documentProcessWorkflow;
        [SerializeField] private GoldExchangeWorkflow goldExchangeWorkflow;
        [SerializeField] private AccountOpeningSystem accountOpeningSystem;
        [SerializeField] private InsuranceReferralSystem insuranceReferralSystem;
        [SerializeField] private RedAlertRedirectionSystem redAlertRedirectionSystem;
        [SerializeField] private ScammerDetectionSystem scammerDetectionSystem;
        [SerializeField] private CharityDonationSystem charityDonationSystem;
        [SerializeField] private CreditApplicationSystem creditApplicationSystem;

        [Header("Penalty")]
        [SerializeField, Min(0)] private int goldPenalty = 100;
        [SerializeField, Min(0.1f)] private float panicDurationSeconds = 10f;
        [SerializeField, Range(0.05f, 1f)] private float panicMoveSpeedMultiplier = 0.7f;
        [SerializeField, Min(1f)] private float panicActionTimeMultiplier = 1.25f;

        [Header("Feedback")]
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip angryShoutSound;
        [SerializeField] private ParticleSystem sweatDropParticlePrefab;
        [SerializeField] private Transform sweatDropAnchor;
        [SerializeField] private GameObject angryExclamationPrefab;
        [SerializeField] private string angryAnimationTrigger = "AngryGesticulation";
        [SerializeField] private Color angryCustomerColor = new Color(1f, 0.18f, 0.12f);

        public UnityEvent<GameObject> OnCounterMeltdownStarted = new UnityEvent<GameObject>();
        public UnityEvent<GameObject> OnCounterMeltdownResolved = new UnityEvent<GameObject>();
        public UnityEvent OnPanicDebuffStarted = new UnityEvent();
        public UnityEvent OnPanicDebuffEnded = new UnityEvent();

        private Coroutine incidentRoutine;
        private Coroutine panicDebuffRoutine;
        private ParticleSystem activeSweatDropEffect;
        private GameObject activeGoldPenaltyText;
        private bool panicMoveAppliedToMobile;
        private bool panicMoveAppliedToTopDown;
        private bool panicAppliedToWireTransfer;
        private bool panicAppliedToMobileActivation;
        private bool panicAppliedToCardBlock;
        private bool panicAppliedToUtility;
        private bool panicAppliedToFastTrack;
        private bool panicAppliedToDocument;
        private bool panicAppliedToGold;
        private bool panicAppliedToAccountOpening;
        private bool panicAppliedToCreditApplication;

        public bool IsIncidentActive => incidentRoutine != null;
        public bool IsPanicDebuffActive => panicDebuffRoutine != null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ResolveMissingReferences();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public bool TriggerCounterMeltdown(GameObject angryCustomer)
        {
            if (angryCustomer == null || IsIncidentActive)
            {
                return false;
            }

            ResolveMissingReferences();
            incidentRoutine = StartCoroutine(CounterMeltdownRoutine(angryCustomer));
            return true;
        }

        public void ApplyPanicDebuff()
        {
            if (panicDebuffRoutine != null)
            {
                StopCoroutine(panicDebuffRoutine);
                RestorePanicDebuff();
            }

            panicDebuffRoutine = StartCoroutine(PanicDebuffRoutine());
        }

        private IEnumerator CounterMeltdownRoutine(GameObject angryCustomer)
        {
            OnCounterMeltdownStarted.Invoke(angryCustomer);
            CancelOngoingTransactions();

            var releasedCustomer = queueManager != null
                ? queueManager.ReleaseCustomerForIncident(angryCustomer)
                : angryCustomer.GetComponent<QueueCustomer>();

            var customerObject = releasedCustomer != null ? releasedCustomer.gameObject : angryCustomer;
            ApplyAngryCustomerFeedback(customerObject);
            DeductGoldPenalty();
            ApplyPanicDebuff();

            if (securityGuardAI != null && !securityGuardAI.IsBusy)
            {
                yield return StartCoroutine(securityGuardAI.EscortAngryCustomerRoutine(
                    customerObject,
                    counterPoint,
                    mainExitDoor,
                    guardIdlePost));
            }
            else if (customerObject != null)
            {
                Destroy(customerObject);
            }

            OnCounterMeltdownResolved.Invoke(customerObject);
            incidentRoutine = null;
        }

        private void CancelOngoingTransactions()
        {
            wireTransferMiniGame?.CancelTransfer();
            mobileActivationMiniGame?.CancelActivation();
            cardBlockMiniGame?.CancelMiniGame();
            utilityBillSystem?.CancelWorkflow();
            fastTrackActionSystem?.CancelWorkflow();
            bankingActionSystem?.CancelAction();
            documentProcessWorkflow?.CancelWorkflow();
            goldExchangeWorkflow?.CancelWorkflow();
            accountOpeningSystem?.CancelStamp();
            insuranceReferralSystem?.CancelReferral();
            redAlertRedirectionSystem?.CancelActiveRedirection();
            scammerDetectionSystem?.CancelInspection();
            charityDonationSystem?.CancelDonation();
            creditApplicationSystem?.CancelCreditApplication();
        }

        private void DeductGoldPenalty()
        {
            if (goldPenalty <= 0)
            {
                return;
            }

            var currentGold = PlayerPrefs.GetInt(PreGameShopManager.PlayerGoldKey, 0);
            PlayerPrefs.SetInt(PreGameShopManager.PlayerGoldKey, Mathf.Max(0, currentGold - goldPenalty));
            PlayerPrefs.Save();
            ShowGoldPenaltyText();
        }

        private IEnumerator PanicDebuffRoutine()
        {
            ApplyPanicMultipliers();
            SpawnSweatDropEffect();
            OnPanicDebuffStarted.Invoke();

            yield return new WaitForSeconds(panicDurationSeconds);

            RestorePanicDebuff();
            OnPanicDebuffEnded.Invoke();
            panicDebuffRoutine = null;
        }

        private void ApplyPanicMultipliers()
        {
            if (mobilePlayerController != null)
            {
                mobilePlayerController.MovementSpeedMultiplier *= panicMoveSpeedMultiplier;
                panicMoveAppliedToMobile = true;
            }

            if (topDownController != null)
            {
                topDownController.MovementSpeedMultiplier *= panicMoveSpeedMultiplier;
                panicMoveAppliedToTopDown = true;
            }

            if (utilityBillSystem != null)
            {
                utilityBillSystem.ActionTimeMultiplier *= panicActionTimeMultiplier;
                panicAppliedToUtility = true;
            }

            if (wireTransferMiniGame != null)
            {
                wireTransferMiniGame.ActionTimeMultiplier *= panicActionTimeMultiplier;
                panicAppliedToWireTransfer = true;
            }

            if (mobileActivationMiniGame != null)
            {
                mobileActivationMiniGame.ActionTimeMultiplier *= panicActionTimeMultiplier;
                panicAppliedToMobileActivation = true;
            }

            if (cardBlockMiniGame != null)
            {
                cardBlockMiniGame.ActionTimeMultiplier *= panicActionTimeMultiplier;
                panicAppliedToCardBlock = true;
            }

            if (fastTrackActionSystem != null)
            {
                fastTrackActionSystem.ActionTimeMultiplier *= panicActionTimeMultiplier;
                panicAppliedToFastTrack = true;
            }

            if (documentProcessWorkflow != null)
            {
                documentProcessWorkflow.ActionTimeMultiplier *= panicActionTimeMultiplier;
                panicAppliedToDocument = true;
            }

            if (goldExchangeWorkflow != null)
            {
                goldExchangeWorkflow.ActionTimeMultiplier *= panicActionTimeMultiplier;
                panicAppliedToGold = true;
            }

            if (accountOpeningSystem != null)
            {
                accountOpeningSystem.ActionTimeMultiplier *= panicActionTimeMultiplier;
                panicAppliedToAccountOpening = true;
            }

            if (creditApplicationSystem != null)
            {
                creditApplicationSystem.ActionTimeMultiplier *= panicActionTimeMultiplier;
                panicAppliedToCreditApplication = true;
            }
        }

        private void RestorePanicDebuff()
        {
            if (mobilePlayerController != null && panicMoveAppliedToMobile)
            {
                mobilePlayerController.MovementSpeedMultiplier /= panicMoveSpeedMultiplier;
            }

            if (topDownController != null && panicMoveAppliedToTopDown)
            {
                topDownController.MovementSpeedMultiplier /= panicMoveSpeedMultiplier;
            }

            if (utilityBillSystem != null && panicAppliedToUtility)
            {
                utilityBillSystem.ActionTimeMultiplier /= panicActionTimeMultiplier;
            }

            if (wireTransferMiniGame != null && panicAppliedToWireTransfer)
            {
                wireTransferMiniGame.ActionTimeMultiplier /= panicActionTimeMultiplier;
            }

            if (mobileActivationMiniGame != null && panicAppliedToMobileActivation)
            {
                mobileActivationMiniGame.ActionTimeMultiplier /= panicActionTimeMultiplier;
            }

            if (cardBlockMiniGame != null && panicAppliedToCardBlock)
            {
                cardBlockMiniGame.ActionTimeMultiplier /= panicActionTimeMultiplier;
            }

            if (fastTrackActionSystem != null && panicAppliedToFastTrack)
            {
                fastTrackActionSystem.ActionTimeMultiplier /= panicActionTimeMultiplier;
            }

            if (documentProcessWorkflow != null && panicAppliedToDocument)
            {
                documentProcessWorkflow.ActionTimeMultiplier /= panicActionTimeMultiplier;
            }

            if (goldExchangeWorkflow != null && panicAppliedToGold)
            {
                goldExchangeWorkflow.ActionTimeMultiplier /= panicActionTimeMultiplier;
            }

            if (accountOpeningSystem != null && panicAppliedToAccountOpening)
            {
                accountOpeningSystem.ActionTimeMultiplier /= panicActionTimeMultiplier;
            }

            if (creditApplicationSystem != null && panicAppliedToCreditApplication)
            {
                creditApplicationSystem.ActionTimeMultiplier /= panicActionTimeMultiplier;
            }

            panicMoveAppliedToMobile = false;
            panicMoveAppliedToTopDown = false;
            panicAppliedToWireTransfer = false;
            panicAppliedToMobileActivation = false;
            panicAppliedToCardBlock = false;
            panicAppliedToUtility = false;
            panicAppliedToFastTrack = false;
            panicAppliedToDocument = false;
            panicAppliedToGold = false;
            panicAppliedToAccountOpening = false;
            panicAppliedToCreditApplication = false;
            DestroySweatDropEffect();
        }

        private void ApplyAngryCustomerFeedback(GameObject customerObject)
        {
            if (customerObject == null)
            {
                return;
            }

            if (audioSource != null && angryShoutSound != null)
            {
                audioSource.PlayOneShot(angryShoutSound);
            }

            var animator = customerObject.GetComponentInChildren<Animator>();
            if (animator != null && !string.IsNullOrWhiteSpace(angryAnimationTrigger))
            {
                animator.SetTrigger(angryAnimationTrigger);
            }

            var renderer = customerObject.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = angryCustomerColor;
            }

            SpawnAngryExclamation(customerObject.transform);
        }

        private void SpawnAngryExclamation(Transform customer)
        {
            if (customer == null)
            {
                return;
            }

            if (angryExclamationPrefab != null)
            {
                var effect = Instantiate(angryExclamationPrefab, customer);
                effect.transform.localPosition = Vector3.up * 2.15f;
                Destroy(effect, 2f);
                return;
            }

            var labelObject = new GameObject("Counter Meltdown Angry Exclamation");
            labelObject.transform.SetParent(customer, false);
            labelObject.transform.localPosition = Vector3.up * 2.15f;
            labelObject.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
            var label = labelObject.AddComponent<TextMesh>();
            label.text = "!";
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontStyle = FontStyle.Bold;
            label.characterSize = 0.62f;
            label.color = Color.red;
            Destroy(labelObject, 2f);
        }

        private void ShowGoldPenaltyText()
        {
            EnsureCanvas();
            if (targetCanvas == null)
            {
                return;
            }

            if (activeGoldPenaltyText != null)
            {
                Destroy(activeGoldPenaltyText);
            }

            activeGoldPenaltyText = new GameObject("Counter Meltdown Gold Penalty Text");
            activeGoldPenaltyText.transform.SetParent(targetCanvas.transform, false);
            var text = activeGoldPenaltyText.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.text = $"-{goldPenalty} Gold";
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 34;
            text.fontStyle = FontStyle.Bold;
            text.color = new Color(1f, 0.18f, 0.12f);

            var rect = activeGoldPenaltyText.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 120f);
            rect.sizeDelta = new Vector2(420f, 80f);
            Destroy(activeGoldPenaltyText, 1.6f);
        }

        private void SpawnSweatDropEffect()
        {
            DestroySweatDropEffect();
            if (sweatDropParticlePrefab == null)
            {
                return;
            }

            var parent = sweatDropAnchor != null
                ? sweatDropAnchor
                : mobilePlayerController != null
                    ? mobilePlayerController.transform
                    : transform;
            activeSweatDropEffect = Instantiate(sweatDropParticlePrefab, parent);
            activeSweatDropEffect.transform.localPosition = Vector3.up * 1.65f;
            activeSweatDropEffect.Play();
        }

        private void DestroySweatDropEffect()
        {
            if (activeSweatDropEffect != null)
            {
                Destroy(activeSweatDropEffect.gameObject);
                activeSweatDropEffect = null;
            }
        }

        private void EnsureCanvas()
        {
            if (targetCanvas == null)
            {
                targetCanvas = FindFirstObjectByType<Canvas>();
            }
        }

        private void ResolveMissingReferences()
        {
            if (queueManager == null)
            {
                queueManager = QueueManager.Instance != null ? QueueManager.Instance : FindFirstObjectByType<QueueManager>();
            }

            if (securityGuardAI == null)
            {
                securityGuardAI = FindFirstObjectByType<SecurityGuardAI>();
            }

            if (mobilePlayerController == null)
            {
                mobilePlayerController = FindFirstObjectByType<MobilePlayerController>();
            }

            if (topDownController == null)
            {
                topDownController = FindFirstObjectByType<ChubbyTopDownInputController>();
            }

            if (wireTransferMiniGame == null)
            {
                wireTransferMiniGame = FindFirstObjectByType<WireTransferMiniGame>();
            }

            if (mobileActivationMiniGame == null)
            {
                mobileActivationMiniGame = FindFirstObjectByType<MobileActivationMiniGame>();
            }

            if (cardBlockMiniGame == null)
            {
                cardBlockMiniGame = FindFirstObjectByType<CardBlockMiniGame>();
            }

            if (utilityBillSystem == null)
            {
                utilityBillSystem = FindFirstObjectByType<UtilityBillSystem>();
            }

            if (fastTrackActionSystem == null)
            {
                fastTrackActionSystem = FindFirstObjectByType<FastTrackActionSystem>();
            }

            if (bankingActionSystem == null)
            {
                bankingActionSystem = FindFirstObjectByType<BankingActionSystem>();
            }

            if (documentProcessWorkflow == null)
            {
                documentProcessWorkflow = FindFirstObjectByType<DocumentProcessWorkflow>();
            }

            if (goldExchangeWorkflow == null)
            {
                goldExchangeWorkflow = FindFirstObjectByType<GoldExchangeWorkflow>();
            }

            if (accountOpeningSystem == null)
            {
                accountOpeningSystem = AccountOpeningSystem.Instance != null
                    ? AccountOpeningSystem.Instance
                    : FindFirstObjectByType<AccountOpeningSystem>();
            }

            if (insuranceReferralSystem == null)
            {
                insuranceReferralSystem = FindFirstObjectByType<InsuranceReferralSystem>();
            }

            if (redAlertRedirectionSystem == null)
            {
                redAlertRedirectionSystem = FindFirstObjectByType<RedAlertRedirectionSystem>();
            }

            if (scammerDetectionSystem == null)
            {
                scammerDetectionSystem = FindFirstObjectByType<ScammerDetectionSystem>();
            }

            if (charityDonationSystem == null)
            {
                charityDonationSystem = FindFirstObjectByType<CharityDonationSystem>();
            }

            if (creditApplicationSystem == null)
            {
                creditApplicationSystem = FindFirstObjectByType<CreditApplicationSystem>();
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }
    }
}
