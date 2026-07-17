using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RushBank.Gameplay
{
    public enum ScammerDiscrepancy
    {
        None,
        PhotoMismatch,
        ExpiredDate,
        FakeStamp
    }

    public class ScammerDetectionSystem : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private QueueManager queueManager;
        [SerializeField] private SecurityGuardAI securityGuardAI;
        [SerializeField] private Transform counterPoint;
        [SerializeField] private Transform scammerEscapeExit;
        [SerializeField] private Transform guardIdlePost;

        [Header("Inspection UI")]
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private GameObject inspectionPanel;
        [SerializeField] private Text avatarDescriptionText;
        [SerializeField] private Text idDocumentText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button approveButton;
        [SerializeField] private Button declineButton;
        [SerializeField] private Button callSecurityButton;

        [Header("Feedback")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip approveFailureSound;
        [SerializeField] private AudioClip declineSuccessSound;
        [SerializeField] private AudioClip securitySuccessSound;
        [SerializeField] private ParticleSystem heroBoostParticlePrefab;
        [SerializeField] private Transform heroBoostParticleAnchor;
        [SerializeField] private Color auditFailureColor = new Color(1f, 0.12f, 0.08f);
        [SerializeField] private Color heroBoostColor = new Color(1f, 0.84f, 0.22f);
        [SerializeField] private Color vigilanceHighlightColor = new Color(0.22f, 0.74f, 1f);

        [Header("Balance")]
        [SerializeField, Range(0f, 1f)] private float discrepancyChance = 0.5f;
        [SerializeField, Min(0)] private int approvePenaltyGold = 150;
        [SerializeField, Min(0)] private int declineRewardGold = 50;
        [SerializeField, Min(0)] private int securityRewardGold = 100;
        [SerializeField, Min(0.1f)] private float counterLockSeconds = 3f;
        [SerializeField, Min(0.1f)] private float heroBoostSeconds = 10f;
        [SerializeField, Min(0.1f)] private float escapeMoveSpeed = 3.6f;

        [Header("Prototype Text")]
        [SerializeField] private string customerName = "Kemal K.";
        [SerializeField] private string actualAvatarDescription = "Actual customer: thick mustache, round glasses, brown hair";
        [SerializeField] private string cleanIdPhotoDescription = "ID photo: clean-shaven, no glasses, black hair";
        [SerializeField] private string matchingIdPhotoDescription = "ID photo: thick mustache, round glasses, brown hair";

        public UnityEvent<GameObject> OnScammerInspectionOpened = new UnityEvent<GameObject>();
        public UnityEvent<GameObject> OnScammerApprovedByMistake = new UnityEvent<GameObject>();
        public UnityEvent<GameObject> OnScammerDeclined = new UnityEvent<GameObject>();
        public UnityEvent<GameObject> OnScammerSecurityCalled = new UnityEvent<GameObject>();
        public UnityEvent OnHeroBoostStarted = new UnityEvent();
        public UnityEvent OnHeroBoostEnded = new UnityEvent();
        public UnityEvent OnVigilanceHighlightStarted = new UnityEvent();
        public UnityEvent OnVigilanceHighlightEnded = new UnityEvent();

        private QueueCustomer activeScammer;
        private ScammerDiscrepancy activeDiscrepancy = ScammerDiscrepancy.None;
        private Coroutine auditLockRoutine;
        private Coroutine heroBoostRoutine;
        private Coroutine vigilanceHighlightRoutine;
        private GameObject activeFloatingText;
        private ParticleSystem activeHeroBoostParticle;
        private bool inspectionOpen;
        private bool counterLocked;
        private bool vigilanceHighlightActive;

        public bool HasActiveScammer => activeScammer != null;
        public bool IsInspectionOpen => inspectionOpen;
        public bool IsCounterLocked => counterLocked;
        public bool IsVigilanceHighlightActive => vigilanceHighlightActive;
        public ScammerDiscrepancy ActiveDiscrepancy => activeDiscrepancy;

        private void Awake()
        {
            ResolveMissingReferences();
            EnsureInspectionUi();
            HideInspectionPanel();
        }

        private void OnEnable()
        {
            ResolveMissingReferences();

            if (queueManager != null)
            {
                queueManager.OnCustomerCalled.AddListener(HandleCustomerCalled);
            }

            if (approveButton != null)
            {
                approveButton.onClick.AddListener(ApproveCurrentCustomer);
            }

            if (declineButton != null)
            {
                declineButton.onClick.AddListener(DeclineCurrentCustomer);
            }

            if (callSecurityButton != null)
            {
                callSecurityButton.onClick.AddListener(CallSecurityForCurrentCustomer);
            }
        }

        private void OnDisable()
        {
            if (queueManager != null)
            {
                queueManager.OnCustomerCalled.RemoveListener(HandleCustomerCalled);
            }

            if (approveButton != null)
            {
                approveButton.onClick.RemoveListener(ApproveCurrentCustomer);
            }

            if (declineButton != null)
            {
                declineButton.onClick.RemoveListener(DeclineCurrentCustomer);
            }

            if (callSecurityButton != null)
            {
                callSecurityButton.onClick.RemoveListener(CallSecurityForCurrentCustomer);
            }

            if (vigilanceHighlightRoutine != null)
            {
                StopCoroutine(vigilanceHighlightRoutine);
                vigilanceHighlightRoutine = null;
            }

            vigilanceHighlightActive = false;
        }

        private void Update()
        {
            if (HasActiveScammer && !inspectionOpen && Input.GetKeyDown(KeyCode.E))
            {
                OpenInspection();
            }
        }

        public void OpenInspection()
        {
            if (!HasActiveScammer || counterLocked)
            {
                return;
            }

            EnsureInspectionUi();
            inspectionOpen = true;

            if (inspectionPanel != null)
            {
                inspectionPanel.SetActive(true);
                inspectionPanel.transform.localScale = Vector3.one;
            }

            RefreshInspectionTexts("Inspect the ID and choose carefully.");
            OnScammerInspectionOpened.Invoke(activeScammer.gameObject);
        }

        public void ApproveCurrentCustomer()
        {
            if (!HasActiveScammer || counterLocked)
            {
                return;
            }

            var scammerObject = activeScammer.gameObject;
            PlayOneShot(approveFailureSound);
            AddGold(-approvePenaltyGold);
            ShowFloatingText($"-{approvePenaltyGold} Gold", auditFailureColor);
            OnScammerApprovedByMistake.Invoke(scammerObject);

            HideInspectionPanel();
            ReleaseActiveScammer();
            StartCoroutine(EscapeRoutine(scammerObject));

            if (auditLockRoutine != null)
            {
                StopCoroutine(auditLockRoutine);
            }

            auditLockRoutine = StartCoroutine(AuditLockRoutine());
        }

        public void DeclineCurrentCustomer()
        {
            if (!HasActiveScammer || counterLocked)
            {
                return;
            }

            var scammerObject = activeScammer.gameObject;
            PlayOneShot(declineSuccessSound);
            AddGold(declineRewardGold);
            ShowFloatingText($"+{declineRewardGold} Gold", Color.green);
            OnScammerDeclined.Invoke(scammerObject);

            HideInspectionPanel();
            ReleaseActiveScammer();
            StartCoroutine(EscapeRoutine(scammerObject));
        }

        public void CallSecurityForCurrentCustomer()
        {
            if (!HasActiveScammer || counterLocked)
            {
                return;
            }

            if (activeDiscrepancy == ScammerDiscrepancy.None)
            {
                RefreshInspectionTexts("No clear discrepancy. Check the document again.");
                PlayOneShot(approveFailureSound);
                return;
            }

            var scammerObject = activeScammer.gameObject;
            PlayOneShot(securitySuccessSound);
            AddGold(securityRewardGold);
            ShowFloatingText($"+{securityRewardGold} Gold\nHero Employee!", heroBoostColor);
            OnScammerSecurityCalled.Invoke(scammerObject);

            HideInspectionPanel();
            ReleaseScammerForSecurity(scammerObject);
            StartCoroutine(SecurityEscortRoutine(scammerObject));
            ApplyHeroBoost();
        }

        public void CancelInspection()
        {
            HideInspectionPanel();
            activeScammer = null;
        }

        public void EnableDiscrepancyAutoHighlight(float seconds)
        {
            seconds = Mathf.Max(0.1f, seconds);
            if (vigilanceHighlightRoutine != null)
            {
                StopCoroutine(vigilanceHighlightRoutine);
            }

            vigilanceHighlightRoutine = StartCoroutine(VigilanceHighlightRoutine(seconds));
        }

        private void HandleCustomerCalled(GameObject customerObject)
        {
            if (customerObject == null || !customerObject.TryGetComponent<QueueCustomer>(out var customer))
            {
                return;
            }

            if (!customer.IsScammer)
            {
                return;
            }

            activeScammer = customer;
            activeDiscrepancy = RollDiscrepancy();
            inspectionOpen = false;
            RefreshInspectionTexts(vigilanceHighlightActive
                ? "High Vigilance active. Suspicious document details will glow."
                : "Press Inspect to review the customer document.");
        }

        private ScammerDiscrepancy RollDiscrepancy()
        {
            if (Random.value > discrepancyChance)
            {
                return ScammerDiscrepancy.None;
            }

            return (ScammerDiscrepancy)Random.Range(
                (int)ScammerDiscrepancy.PhotoMismatch,
                (int)ScammerDiscrepancy.FakeStamp + 1);
        }

        private void RefreshInspectionTexts(string status)
        {
            if (avatarDescriptionText != null)
            {
                avatarDescriptionText.text = actualAvatarDescription;
            }

            if (idDocumentText != null)
            {
                idDocumentText.text = BuildDocumentText();
            }

            if (statusText != null)
            {
                statusText.text = status;
                statusText.color = vigilanceHighlightActive && activeDiscrepancy != ScammerDiscrepancy.None
                    ? vigilanceHighlightColor
                    : Color.white;
            }
        }

        private string BuildDocumentText()
        {
            var photo = activeDiscrepancy == ScammerDiscrepancy.PhotoMismatch
                ? cleanIdPhotoDescription
                : matchingIdPhotoDescription;
            var expiration = activeDiscrepancy == ScammerDiscrepancy.ExpiredDate
                ? "Expiration: 1999 / scribbled"
                : "Expiration: 2032";
            var stamp = activeDiscrepancy == ScammerDiscrepancy.FakeStamp
                ? "Stamp: silly cat-paw mark"
                : "Stamp: official Rush Bank seal";

            if (vigilanceHighlightActive)
            {
                photo = HighlightDiscrepancyLine(photo, ScammerDiscrepancy.PhotoMismatch);
                expiration = HighlightDiscrepancyLine(expiration, ScammerDiscrepancy.ExpiredDate);
                stamp = HighlightDiscrepancyLine(stamp, ScammerDiscrepancy.FakeStamp);
            }

            return $"ID Card\nName: {customerName}\n{photo}\n{expiration}\n{stamp}";
        }

        private string HighlightDiscrepancyLine(string line, ScammerDiscrepancy discrepancy)
        {
            return activeDiscrepancy == discrepancy
                ? $">>> CHECK THIS: {line} <<<"
                : line;
        }

        private void ReleaseActiveScammer()
        {
            if (queueManager != null)
            {
                queueManager.ReleaseActiveCustomerForRedirect();
            }

            activeScammer = null;
        }

        private void ReleaseScammerForSecurity(GameObject scammerObject)
        {
            if (queueManager != null)
            {
                queueManager.ReleaseCustomerForIncident(scammerObject);
            }

            activeScammer = null;
        }

        private IEnumerator SecurityEscortRoutine(GameObject scammerObject)
        {
            if (scammerObject == null)
            {
                yield break;
            }

            if (securityGuardAI != null && !securityGuardAI.IsBusy)
            {
                yield return securityGuardAI.EscortAngryCustomerRoutine(
                    scammerObject,
                    counterPoint,
                    scammerEscapeExit,
                    guardIdlePost);
            }
            else
            {
                yield return EscapeRoutine(scammerObject);
            }
        }

        private IEnumerator EscapeRoutine(GameObject scammerObject)
        {
            if (scammerObject == null)
            {
                yield break;
            }

            var exitPosition = scammerEscapeExit != null
                ? scammerEscapeExit.position
                : scammerObject.transform.position - scammerObject.transform.forward * 5f;

            var navAgent = scammerObject.GetComponent<NavMeshAgent>();
            if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
            {
                navAgent.SetDestination(exitPosition);
                while (navAgent.pathPending)
                {
                    yield return null;
                }

                while (scammerObject != null
                    && navAgent.enabled
                    && navAgent.isOnNavMesh
                    && navAgent.remainingDistance > Mathf.Max(navAgent.stoppingDistance, 0.12f))
                {
                    yield return null;
                }
            }
            else
            {
                while (scammerObject != null && (scammerObject.transform.position - exitPosition).sqrMagnitude > 0.06f)
                {
                    var current = scammerObject.transform.position;
                    scammerObject.transform.position = Vector3.MoveTowards(
                        current,
                        exitPosition,
                        escapeMoveSpeed * Time.deltaTime);

                    var direction = exitPosition - current;
                    direction.y = 0f;
                    if (direction.sqrMagnitude > 0.001f)
                    {
                        scammerObject.transform.rotation = Quaternion.Slerp(
                            scammerObject.transform.rotation,
                            Quaternion.LookRotation(direction.normalized, Vector3.up),
                            10f * Time.deltaTime);
                    }

                    yield return null;
                }
            }

            if (scammerObject != null)
            {
                Destroy(scammerObject);
            }
        }

        private IEnumerator AuditLockRoutine()
        {
            counterLocked = true;
            EnsureInspectionUi();

            if (inspectionPanel != null)
            {
                inspectionPanel.SetActive(true);
            }

            var elapsed = 0f;
            while (elapsed < counterLockSeconds)
            {
                elapsed += Time.deltaTime;
                var flashOn = Mathf.FloorToInt(elapsed * 8f) % 2 == 0;
                if (statusText != null)
                {
                    statusText.text = "FAILED AUDIT";
                    statusText.color = flashOn ? Color.white : auditFailureColor;
                }

                yield return null;
            }

            counterLocked = false;
            auditLockRoutine = null;
            HideInspectionPanel();
        }

        private void ApplyHeroBoost()
        {
            if (heroBoostRoutine != null)
            {
                StopCoroutine(heroBoostRoutine);
                RestoreHeroBoost();
            }

            heroBoostRoutine = StartCoroutine(HeroBoostRoutine());
        }

        private IEnumerator HeroBoostRoutine()
        {
            queueManager?.ApplyQueueReliefBoost(0f, 0f, heroBoostSeconds);
            SpawnHeroBoostParticle();
            OnHeroBoostStarted.Invoke();

            yield return new WaitForSeconds(heroBoostSeconds);

            RestoreHeroBoost();
            OnHeroBoostEnded.Invoke();
            heroBoostRoutine = null;
        }

        private void RestoreHeroBoost()
        {
            queueManager?.ResetQueueReliefBoost();
            DestroyHeroBoostParticle();
        }

        private IEnumerator VigilanceHighlightRoutine(float seconds)
        {
            vigilanceHighlightActive = true;
            OnVigilanceHighlightStarted.Invoke();
            if (HasActiveScammer)
            {
                RefreshInspectionTexts(activeDiscrepancy == ScammerDiscrepancy.None
                    ? "High Vigilance active. No discrepancy detected yet."
                    : "High Vigilance active. Suspicious document detail highlighted.");
            }

            yield return new WaitForSeconds(seconds);

            vigilanceHighlightActive = false;
            if (HasActiveScammer)
            {
                RefreshInspectionTexts("High Vigilance ended. Inspect manually.");
            }

            OnVigilanceHighlightEnded.Invoke();
            vigilanceHighlightRoutine = null;
        }

        private void AddGold(int amount)
        {
            if (amount == 0)
            {
                return;
            }

            var currentGold = PlayerPrefs.GetInt(PreGameShopManager.PlayerGoldKey, 0);
            PlayerPrefs.SetInt(PreGameShopManager.PlayerGoldKey, Mathf.Max(0, currentGold + amount));
            PlayerPrefs.Save();
        }

        private void ShowFloatingText(string message, Color color)
        {
            EnsureCanvas();
            if (targetCanvas == null)
            {
                return;
            }

            if (activeFloatingText != null)
            {
                Destroy(activeFloatingText);
            }

            activeFloatingText = new GameObject("Scammer Detection Floating Text");
            activeFloatingText.transform.SetParent(targetCanvas.transform, false);
            var label = activeFloatingText.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.text = message;
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = 32;
            label.fontStyle = FontStyle.Bold;
            label.color = color;

            var rect = activeFloatingText.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.58f);
            rect.anchorMax = new Vector2(0.5f, 0.58f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(520f, 110f);
            Destroy(activeFloatingText, 1.8f);
        }

        private void SpawnHeroBoostParticle()
        {
            DestroyHeroBoostParticle();
            if (heroBoostParticlePrefab == null)
            {
                return;
            }

            var parent = heroBoostParticleAnchor != null
                ? heroBoostParticleAnchor
                : transform;
            activeHeroBoostParticle = Instantiate(heroBoostParticlePrefab, parent);
            activeHeroBoostParticle.transform.localPosition = Vector3.zero;
            activeHeroBoostParticle.Play();
        }

        private void DestroyHeroBoostParticle()
        {
            if (activeHeroBoostParticle != null)
            {
                Destroy(activeHeroBoostParticle.gameObject);
                activeHeroBoostParticle = null;
            }
        }

        private void HideInspectionPanel()
        {
            inspectionOpen = false;
            if (inspectionPanel != null)
            {
                inspectionPanel.SetActive(false);
            }

            if (statusText != null)
            {
                statusText.color = Color.white;
            }
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private void EnsureInspectionUi()
        {
            EnsureCanvas();
            if (inspectionPanel != null)
            {
                return;
            }

            if (targetCanvas == null)
            {
                return;
            }

            inspectionPanel = new GameObject("Scammer Inspection Panel");
            inspectionPanel.transform.SetParent(targetCanvas.transform, false);

            var panelImage = inspectionPanel.AddComponent<Image>();
            panelImage.color = new Color(0.08f, 0.09f, 0.11f, 0.94f);

            var panelRect = inspectionPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.08f, 0.12f);
            panelRect.anchorMax = new Vector2(0.92f, 0.88f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            avatarDescriptionText = CreatePanelText("Actual Avatar", new Vector2(0.04f, 0.35f), new Vector2(0.48f, 0.92f), 28);
            idDocumentText = CreatePanelText("ID Document", new Vector2(0.52f, 0.35f), new Vector2(0.96f, 0.92f), 28);
            statusText = CreatePanelText("Status", new Vector2(0.05f, 0.22f), new Vector2(0.95f, 0.32f), 24);

            approveButton = CreatePanelButton("Approve", new Vector2(0.08f, 0.06f), new Vector2(0.31f, 0.18f), new Color(0.2f, 0.75f, 0.32f));
            declineButton = CreatePanelButton("Decline", new Vector2(0.38f, 0.06f), new Vector2(0.61f, 0.18f), new Color(0.86f, 0.23f, 0.18f));
            callSecurityButton = CreatePanelButton("Call Security", new Vector2(0.68f, 0.06f), new Vector2(0.92f, 0.18f), new Color(0.95f, 0.68f, 0.18f));
        }

        private Text CreatePanelText(string name, Vector2 anchorMin, Vector2 anchorMax, int fontSize)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(inspectionPanel.transform, false);

            var text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = fontSize;
            text.color = Color.white;

            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return text;
        }

        private Button CreatePanelButton(string label, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var buttonObject = new GameObject(label);
            buttonObject.transform.SetParent(inspectionPanel.transform, false);

            var image = buttonObject.AddComponent<Image>();
            image.color = color;

            var button = buttonObject.AddComponent<Button>();
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var text = CreatePanelText($"{label} Label", Vector2.zero, Vector2.one, 24);
            text.transform.SetParent(buttonObject.transform, false);
            text.text = label;
            text.fontStyle = FontStyle.Bold;
            return button;
        }

        private void EnsureCanvas()
        {
            if (targetCanvas == null)
            {
                targetCanvas = FindFirstObjectByType<Canvas>();
            }

            if (targetCanvas != null)
            {
                return;
            }

            var canvasObject = new GameObject("Scammer Detection Canvas");
            targetCanvas = canvasObject.AddComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
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

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }
    }
}
