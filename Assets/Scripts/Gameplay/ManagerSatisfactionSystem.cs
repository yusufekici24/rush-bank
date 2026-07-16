using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RushBank.Gameplay
{
    public class ManagerSatisfactionSystem : MonoBehaviour
    {
        [Header("Satisfaction")]
        [SerializeField, Range(0f, 100f)] private float currentSatisfaction;
        [SerializeField] private Slider satisfactionBarUI;
        [SerializeField] private RectTransform satisfactionIcon;
        [SerializeField, Min(0.1f)] private float fullSatisfaction = 100f;

        [Header("Event Rewards")]
        [SerializeField, Min(0f)] private float itRepairSatisfaction = 25f;
        [SerializeField, Min(0f)] private float stationeryDeliverySatisfaction = 15f;
        [SerializeField, Min(0f)] private float scammerCaughtSatisfaction = 20f;
        [SerializeField, Min(0f)] private float redAlertRedirectSatisfaction = 15f;
        [SerializeField, Min(0f)] private float perfectTransactionSatisfaction = 5f;
        [SerializeField, Min(0f)] private float counterMeltdownPenalty = 30f;
        [SerializeField, Min(0f)] private float scammerApprovedPenalty = 25f;

        [Header("Boost Targets")]
        [SerializeField] private QueueManager queueManager;
        [SerializeField] private MobilePlayerController mobilePlayerController;
        [SerializeField] private ChubbyTopDownInputController topDownController;
        [SerializeField] private FastTrackActionSystem fastTrackActionSystem;
        [SerializeField] private UtilityBillSystem utilityBillSystem;
        [SerializeField] private MobileActivationMiniGame mobileActivationMiniGame;
        [SerializeField] private WireTransferMiniGame wireTransferMiniGame;
        [SerializeField] private CardBlockMiniGame cardBlockMiniGame;
        [SerializeField] private DocumentProcessWorkflow documentProcessWorkflow;
        [SerializeField] private GoldExchangeWorkflow goldExchangeWorkflow;
        [SerializeField] private AccountOpeningSystem accountOpeningSystem;
        [SerializeField] private CreditApplicationSystem creditApplicationSystem;
        [SerializeField] private StationeryDeliverySystem stationeryDeliverySystem;

        [Header("Event Sources")]
        [SerializeField] private ManagerITSupportEvent managerITSupportEvent;
        [SerializeField] private StationeryDeliverySystem stationeryEventSource;
        [SerializeField] private CounterIncidentManager counterIncidentManager;
        [SerializeField] private ScammerDetectionSystem scammerDetectionSystem;
        [SerializeField] private RedAlertRedirectionSystem redAlertRedirectionSystem;
        [SerializeField] private WireTransferMiniGame perfectTransferSource;

        [Header("Staff Feast")]
        [SerializeField, Min(0.1f)] private float feastDurationSeconds = 20f;
        [SerializeField, Min(1f)] private float playerSpeedMultiplier = 1.3f;
        [SerializeField, Range(0.05f, 1f)] private float transactionTimeMultiplier = 0.5f;
        [SerializeField, Min(1f)] private float redirectSpeedMultiplier = 2f;
        [SerializeField] private Transform playerCounter;
        [SerializeField] private Transform[] feastDeskAnchors;
        [SerializeField] private Transform[] staffEmojiAnchors;
        [SerializeField] private GameObject pizzaBoxPrefab;
        [SerializeField] private GameObject pizzaSliceEmojiPrefab;
        [SerializeField] private ParticleSystem confettiPrefab;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip satisfactionUpSound;
        [SerializeField] private AudioClip satisfactionDownSound;
        [SerializeField] private AudioClip staffFeastSound;

        public UnityEvent<float> OnSatisfactionChanged = new UnityEvent<float>();
        public UnityEvent OnStaffFeastStarted = new UnityEvent();
        public UnityEvent OnStaffFeastEnded = new UnityEvent();

        private readonly List<GameObject> spawnedFeastObjects = new List<GameObject>();
        private Coroutine iconFeedbackRoutine;
        private Coroutine feastRoutine;
        private bool speedAppliedToMobile;
        private bool speedAppliedToTopDown;
        private bool feastAppliedToFastTrack;
        private bool feastAppliedToUtility;
        private bool feastAppliedToMobileActivation;
        private bool feastAppliedToWireTransfer;
        private bool feastAppliedToCardBlock;
        private bool feastAppliedToDocument;
        private bool feastAppliedToGold;
        private bool feastAppliedToAccountOpening;
        private bool feastAppliedToCreditApplication;
        private float previousStationeryRedirectMultiplier = 1f;
        private float appliedPermanentFeastDurationBonus;

        public float CurrentSatisfaction => currentSatisfaction;
        public float Satisfaction01 => Mathf.Clamp01(currentSatisfaction / Mathf.Max(1f, fullSatisfaction));
        public bool IsStaffFeastActive => feastRoutine != null;

        private void Awake()
        {
            ResolveMissingReferences();
            ApplyPermanentFeastDurationBonus();
            UpdateSatisfactionUI();
        }

        private void OnEnable()
        {
            ResolveMissingReferences();
            RegisterEventHooks();
        }

        private void OnDisable()
        {
            UnregisterEventHooks();
        }

        public void AddSatisfaction(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            currentSatisfaction = Mathf.Clamp(currentSatisfaction + amount, 0f, fullSatisfaction);
            UpdateSatisfactionUI();
            PlaySound(satisfactionUpSound);
            PlayIconPunch();
            OnSatisfactionChanged.Invoke(currentSatisfaction);

            if (currentSatisfaction >= fullSatisfaction)
            {
                currentSatisfaction = 0f;
                UpdateSatisfactionUI();
                TriggerStaffFeast();
            }
        }

        public void DeductSatisfaction(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            currentSatisfaction = Mathf.Clamp(currentSatisfaction - amount, 0f, fullSatisfaction);
            UpdateSatisfactionUI();
            PlaySound(satisfactionDownSound);
            PlayIconShake();
            OnSatisfactionChanged.Invoke(currentSatisfaction);
        }

        public void TriggerStaffFeast()
        {
            if (feastRoutine != null)
            {
                StopCoroutine(feastRoutine);
                RestoreStaffFeastBoost();
            }

            feastRoutine = StartCoroutine(StaffFeastRoutine());
        }

        public void ApplyPermanentFeastDurationBonus()
        {
            var totalBonus = PlayerPrefs.GetFloat(QuestAndAchievementManager.PermanentFeastDurationBonusKey, 0f);
            var delta = totalBonus - appliedPermanentFeastDurationBonus;
            if (delta <= 0f)
            {
                return;
            }

            feastDurationSeconds += delta;
            appliedPermanentFeastDurationBonus = totalBonus;
        }

        private IEnumerator StaffFeastRoutine()
        {
            PlaySound(staffFeastSound);
            SpawnConfetti();
            SpawnFeastObjects();
            ApplyStaffFeastBoost();
            OnStaffFeastStarted.Invoke();

            yield return new WaitForSeconds(feastDurationSeconds);

            RestoreStaffFeastBoost();
            DestroyFeastObjects();
            OnStaffFeastEnded.Invoke();
            feastRoutine = null;
        }

        private void ApplyStaffFeastBoost()
        {
            if (queueManager != null)
            {
                queueManager.ApplyQueueReliefBoost(0f, 0f, feastDurationSeconds);
            }

            if (mobilePlayerController != null)
            {
                mobilePlayerController.MovementSpeedMultiplier *= playerSpeedMultiplier;
                speedAppliedToMobile = true;
            }

            if (topDownController != null)
            {
                topDownController.MovementSpeedMultiplier *= playerSpeedMultiplier;
                speedAppliedToTopDown = true;
            }

            ApplyTransactionMultiplier();

            if (stationeryDeliverySystem != null)
            {
                previousStationeryRedirectMultiplier = stationeryDeliverySystem.GlobalRedirectSpeedMultiplier;
                stationeryDeliverySystem.GlobalRedirectSpeedMultiplier = previousStationeryRedirectMultiplier * redirectSpeedMultiplier;
            }
        }

        private void RestoreStaffFeastBoost()
        {
            queueManager?.ResetQueueReliefBoost();

            if (mobilePlayerController != null && speedAppliedToMobile)
            {
                mobilePlayerController.MovementSpeedMultiplier /= playerSpeedMultiplier;
            }

            if (topDownController != null && speedAppliedToTopDown)
            {
                topDownController.MovementSpeedMultiplier /= playerSpeedMultiplier;
            }

            RestoreTransactionMultiplier();

            if (stationeryDeliverySystem != null)
            {
                stationeryDeliverySystem.GlobalRedirectSpeedMultiplier = previousStationeryRedirectMultiplier;
            }

            speedAppliedToMobile = false;
            speedAppliedToTopDown = false;
        }

        private void ApplyTransactionMultiplier()
        {
            if (fastTrackActionSystem != null)
            {
                fastTrackActionSystem.ActionTimeMultiplier *= transactionTimeMultiplier;
                feastAppliedToFastTrack = true;
            }

            if (utilityBillSystem != null)
            {
                utilityBillSystem.ActionTimeMultiplier *= transactionTimeMultiplier;
                feastAppliedToUtility = true;
            }

            if (mobileActivationMiniGame != null)
            {
                mobileActivationMiniGame.ActionTimeMultiplier *= transactionTimeMultiplier;
                feastAppliedToMobileActivation = true;
            }

            if (wireTransferMiniGame != null)
            {
                wireTransferMiniGame.ActionTimeMultiplier *= transactionTimeMultiplier;
                feastAppliedToWireTransfer = true;
            }

            if (cardBlockMiniGame != null)
            {
                cardBlockMiniGame.ActionTimeMultiplier *= transactionTimeMultiplier;
                feastAppliedToCardBlock = true;
            }

            if (documentProcessWorkflow != null)
            {
                documentProcessWorkflow.ActionTimeMultiplier *= transactionTimeMultiplier;
                feastAppliedToDocument = true;
            }

            if (goldExchangeWorkflow != null)
            {
                goldExchangeWorkflow.ActionTimeMultiplier *= transactionTimeMultiplier;
                feastAppliedToGold = true;
            }

            if (accountOpeningSystem != null)
            {
                accountOpeningSystem.ActionTimeMultiplier *= transactionTimeMultiplier;
                feastAppliedToAccountOpening = true;
            }

            if (creditApplicationSystem != null)
            {
                creditApplicationSystem.ActionTimeMultiplier *= transactionTimeMultiplier;
                feastAppliedToCreditApplication = true;
            }
        }

        private void RestoreTransactionMultiplier()
        {
            if (fastTrackActionSystem != null && feastAppliedToFastTrack)
            {
                fastTrackActionSystem.ActionTimeMultiplier /= transactionTimeMultiplier;
            }

            if (utilityBillSystem != null && feastAppliedToUtility)
            {
                utilityBillSystem.ActionTimeMultiplier /= transactionTimeMultiplier;
            }

            if (mobileActivationMiniGame != null && feastAppliedToMobileActivation)
            {
                mobileActivationMiniGame.ActionTimeMultiplier /= transactionTimeMultiplier;
            }

            if (wireTransferMiniGame != null && feastAppliedToWireTransfer)
            {
                wireTransferMiniGame.ActionTimeMultiplier /= transactionTimeMultiplier;
            }

            if (cardBlockMiniGame != null && feastAppliedToCardBlock)
            {
                cardBlockMiniGame.ActionTimeMultiplier /= transactionTimeMultiplier;
            }

            if (documentProcessWorkflow != null && feastAppliedToDocument)
            {
                documentProcessWorkflow.ActionTimeMultiplier /= transactionTimeMultiplier;
            }

            if (goldExchangeWorkflow != null && feastAppliedToGold)
            {
                goldExchangeWorkflow.ActionTimeMultiplier /= transactionTimeMultiplier;
            }

            if (accountOpeningSystem != null && feastAppliedToAccountOpening)
            {
                accountOpeningSystem.ActionTimeMultiplier /= transactionTimeMultiplier;
            }

            if (creditApplicationSystem != null && feastAppliedToCreditApplication)
            {
                creditApplicationSystem.ActionTimeMultiplier /= transactionTimeMultiplier;
            }

            feastAppliedToFastTrack = false;
            feastAppliedToUtility = false;
            feastAppliedToMobileActivation = false;
            feastAppliedToWireTransfer = false;
            feastAppliedToCardBlock = false;
            feastAppliedToDocument = false;
            feastAppliedToGold = false;
            feastAppliedToAccountOpening = false;
            feastAppliedToCreditApplication = false;
        }

        private void SpawnConfetti()
        {
            if (confettiPrefab == null)
            {
                return;
            }

            var anchor = playerCounter != null ? playerCounter : transform;
            var confetti = Instantiate(confettiPrefab, anchor.position + Vector3.up * 2f, Quaternion.identity);
            confetti.Play();
            Destroy(confetti.gameObject, 3f);
        }

        private void SpawnFeastObjects()
        {
            DestroyFeastObjects();
            SpawnPizzaBox(playerCounter);

            if (feastDeskAnchors != null)
            {
                for (var i = 0; i < feastDeskAnchors.Length; i++)
                {
                    SpawnPizzaBox(feastDeskAnchors[i]);
                }
            }

            if (staffEmojiAnchors != null)
            {
                for (var i = 0; i < staffEmojiAnchors.Length; i++)
                {
                    SpawnPizzaEmoji(staffEmojiAnchors[i]);
                }
            }
        }

        private void SpawnPizzaBox(Transform anchor)
        {
            if (anchor == null)
            {
                return;
            }

            GameObject box;
            if (pizzaBoxPrefab != null)
            {
                box = Instantiate(pizzaBoxPrefab, anchor);
            }
            else
            {
                box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.name = "Staff Feast Pizza Box";
                box.transform.SetParent(anchor, false);
                box.transform.localScale = new Vector3(0.72f, 0.08f, 0.46f);
                if (box.TryGetComponent<Renderer>(out var rendererComponent))
                {
                    var material = new Material(Shader.Find("Standard"));
                    material.color = new Color(1f, 0.78f, 0.42f);
                    rendererComponent.sharedMaterial = material;
                }
            }

            box.transform.localPosition = Vector3.up * 0.8f;
            box.transform.localRotation = Quaternion.identity;
            spawnedFeastObjects.Add(box);
        }

        private void SpawnPizzaEmoji(Transform anchor)
        {
            if (anchor == null)
            {
                return;
            }

            GameObject emoji;
            if (pizzaSliceEmojiPrefab != null)
            {
                emoji = Instantiate(pizzaSliceEmojiPrefab, anchor);
            }
            else
            {
                emoji = new GameObject("Staff Feast Pizza Slice Icon");
                emoji.transform.SetParent(anchor, false);
                var label = emoji.AddComponent<TextMesh>();
                label.text = "PIZZA";
                label.anchor = TextAnchor.MiddleCenter;
                label.alignment = TextAlignment.Center;
                label.fontStyle = FontStyle.Bold;
                label.characterSize = 0.18f;
                label.color = new Color(1f, 0.72f, 0.18f);
            }

            emoji.transform.localPosition = Vector3.up * 2f;
            emoji.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
            spawnedFeastObjects.Add(emoji);
        }

        private void DestroyFeastObjects()
        {
            for (var i = spawnedFeastObjects.Count - 1; i >= 0; i--)
            {
                if (spawnedFeastObjects[i] != null)
                {
                    Destroy(spawnedFeastObjects[i]);
                }
            }

            spawnedFeastObjects.Clear();
        }

        private void RegisterEventHooks()
        {
            if (managerITSupportEvent != null)
            {
                managerITSupportEvent.OnSupportEventCompleted.AddListener(HandleITRepairCompleted);
            }

            if (stationeryEventSource != null)
            {
                stationeryEventSource.OnSupplyDelivered.AddListener(HandleStationeryDelivered);
            }

            if (counterIncidentManager != null)
            {
                counterIncidentManager.OnCounterMeltdownStarted.AddListener(HandleCounterMeltdown);
            }

            if (scammerDetectionSystem != null)
            {
                scammerDetectionSystem.OnScammerSecurityCalled.AddListener(HandleScammerCaught);
                scammerDetectionSystem.OnScammerApprovedByMistake.AddListener(HandleScammerApprovedByMistake);
            }

            if (redAlertRedirectionSystem != null)
            {
                redAlertRedirectionSystem.OnBarutCustomerRedirected.AddListener(HandleRedAlertRedirected);
            }

            if (perfectTransferSource != null)
            {
                perfectTransferSource.OnPerfectTransferBoostStarted.AddListener(HandlePerfectTransaction);
            }
        }

        private void UnregisterEventHooks()
        {
            if (managerITSupportEvent != null)
            {
                managerITSupportEvent.OnSupportEventCompleted.RemoveListener(HandleITRepairCompleted);
            }

            if (stationeryEventSource != null)
            {
                stationeryEventSource.OnSupplyDelivered.RemoveListener(HandleStationeryDelivered);
            }

            if (counterIncidentManager != null)
            {
                counterIncidentManager.OnCounterMeltdownStarted.RemoveListener(HandleCounterMeltdown);
            }

            if (scammerDetectionSystem != null)
            {
                scammerDetectionSystem.OnScammerSecurityCalled.RemoveListener(HandleScammerCaught);
                scammerDetectionSystem.OnScammerApprovedByMistake.RemoveListener(HandleScammerApprovedByMistake);
            }

            if (redAlertRedirectionSystem != null)
            {
                redAlertRedirectionSystem.OnBarutCustomerRedirected.RemoveListener(HandleRedAlertRedirected);
            }

            if (perfectTransferSource != null)
            {
                perfectTransferSource.OnPerfectTransferBoostStarted.RemoveListener(HandlePerfectTransaction);
            }
        }

        private void HandleITRepairCompleted()
        {
            AddSatisfaction(itRepairSatisfaction);
        }

        private void HandleStationeryDelivered(StationeryDeskType deskType)
        {
            AddSatisfaction(stationeryDeliverySatisfaction);
        }

        private void HandleScammerCaught(GameObject scammer)
        {
            AddSatisfaction(scammerCaughtSatisfaction);
        }

        private void HandleScammerApprovedByMistake(GameObject scammer)
        {
            DeductSatisfaction(scammerApprovedPenalty);
        }

        private void HandleRedAlertRedirected(QueueCustomer customer)
        {
            AddSatisfaction(redAlertRedirectSatisfaction);
        }

        private void HandleCounterMeltdown(GameObject customer)
        {
            DeductSatisfaction(counterMeltdownPenalty);
        }

        private void HandlePerfectTransaction()
        {
            AddSatisfaction(perfectTransactionSatisfaction);
        }

        private void UpdateSatisfactionUI()
        {
            if (satisfactionBarUI != null)
            {
                satisfactionBarUI.value = Satisfaction01;
            }
        }

        private void PlayIconPunch()
        {
            if (satisfactionIcon == null)
            {
                return;
            }

            if (iconFeedbackRoutine != null)
            {
                StopCoroutine(iconFeedbackRoutine);
            }

            iconFeedbackRoutine = StartCoroutine(IconPunchRoutine());
        }

        private void PlayIconShake()
        {
            if (satisfactionIcon == null)
            {
                return;
            }

            if (iconFeedbackRoutine != null)
            {
                StopCoroutine(iconFeedbackRoutine);
            }

            iconFeedbackRoutine = StartCoroutine(IconShakeRoutine());
        }

        private IEnumerator IconPunchRoutine()
        {
            var baseScale = satisfactionIcon.localScale;
            var elapsed = 0f;
            while (elapsed < 0.24f)
            {
                elapsed += Time.deltaTime;
                var pulse = 1f + Mathf.Sin(Mathf.Clamp01(elapsed / 0.24f) * Mathf.PI) * 0.18f;
                satisfactionIcon.localScale = baseScale * pulse;
                yield return null;
            }

            satisfactionIcon.localScale = baseScale;
            iconFeedbackRoutine = null;
        }

        private IEnumerator IconShakeRoutine()
        {
            var basePosition = satisfactionIcon.anchoredPosition;
            var elapsed = 0f;
            while (elapsed < 0.28f)
            {
                elapsed += Time.deltaTime;
                satisfactionIcon.anchoredPosition = basePosition + Vector2.right * (Mathf.Sin(elapsed * 70f) * 8f);
                yield return null;
            }

            satisfactionIcon.anchoredPosition = basePosition;
            iconFeedbackRoutine = null;
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private void ResolveMissingReferences()
        {
            if (queueManager == null)
            {
                queueManager = QueueManager.Instance != null ? QueueManager.Instance : FindFirstObjectByType<QueueManager>();
            }

            if (mobilePlayerController == null)
            {
                mobilePlayerController = FindFirstObjectByType<MobilePlayerController>();
            }

            if (topDownController == null)
            {
                topDownController = FindFirstObjectByType<ChubbyTopDownInputController>();
            }

            if (fastTrackActionSystem == null)
            {
                fastTrackActionSystem = FindFirstObjectByType<FastTrackActionSystem>();
            }

            if (utilityBillSystem == null)
            {
                utilityBillSystem = FindFirstObjectByType<UtilityBillSystem>();
            }

            if (mobileActivationMiniGame == null)
            {
                mobileActivationMiniGame = FindFirstObjectByType<MobileActivationMiniGame>();
            }

            if (wireTransferMiniGame == null)
            {
                wireTransferMiniGame = FindFirstObjectByType<WireTransferMiniGame>();
            }

            if (cardBlockMiniGame == null)
            {
                cardBlockMiniGame = FindFirstObjectByType<CardBlockMiniGame>();
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

            if (stationeryDeliverySystem == null)
            {
                stationeryDeliverySystem = StationeryDeliverySystem.Instance != null
                    ? StationeryDeliverySystem.Instance
                    : FindFirstObjectByType<StationeryDeliverySystem>();
            }

            if (creditApplicationSystem == null)
            {
                creditApplicationSystem = FindFirstObjectByType<CreditApplicationSystem>();
            }

            if (stationeryEventSource == null)
            {
                stationeryEventSource = stationeryDeliverySystem;
            }

            if (managerITSupportEvent == null)
            {
                managerITSupportEvent = FindFirstObjectByType<ManagerITSupportEvent>();
            }

            if (counterIncidentManager == null)
            {
                counterIncidentManager = CounterIncidentManager.Instance != null
                    ? CounterIncidentManager.Instance
                    : FindFirstObjectByType<CounterIncidentManager>();
            }

            if (scammerDetectionSystem == null)
            {
                scammerDetectionSystem = FindFirstObjectByType<ScammerDetectionSystem>();
            }

            if (redAlertRedirectionSystem == null)
            {
                redAlertRedirectionSystem = FindFirstObjectByType<RedAlertRedirectionSystem>();
            }

            if (perfectTransferSource == null)
            {
                perfectTransferSource = wireTransferMiniGame;
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }
    }
}
