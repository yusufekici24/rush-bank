using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RushBank.Gameplay
{
    public class InsuranceReferralSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private QueueManager queueManager;
        [SerializeField] private Transform insuranceSpecialistDesk;
        [SerializeField] private GameObject umbrellaHouseCarIconPrefab;
        [SerializeField] private Button referToSpecialistButton;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip paperSlipSound;
        [SerializeField] private StationeryDeliverySystem stationeryDeliverySystem;

        [Header("Boost Targets")]
        [SerializeField] private WireTransferMiniGame wireTransferMiniGame;
        [SerializeField] private MobileActivationMiniGame mobileActivationMiniGame;
        [SerializeField] private CardBlockMiniGame cardBlockMiniGame;
        [SerializeField] private UtilityBillSystem utilityBillSystem;
        [SerializeField] private FastTrackActionSystem fastTrackActionSystem;
        [SerializeField] private DocumentProcessWorkflow documentProcessWorkflow;
        [SerializeField] private GoldExchangeWorkflow goldExchangeWorkflow;
        [SerializeField] private AccountOpeningSystem accountOpeningSystem;

        [Header("Teamwork Boost")]
        [SerializeField, Min(0.1f)] private float teamworkBoostSeconds = 12f;
        [SerializeField, Range(0.05f, 1f)] private float teamworkActionTimeMultiplier = 0.6f;
        [SerializeField] private ParticleSystem teamworkHandEffectPrefab;
        [SerializeField] private Transform playerHandEffectAnchor;

        [Header("Movement")]
        [SerializeField, Min(0.1f)] private float redirectedCustomerMoveSpeed = 2.5f;

        public UnityEvent<QueueCustomer> OnInsuranceCustomerReady = new UnityEvent<QueueCustomer>();
        public UnityEvent<QueueCustomer> OnInsuranceCustomerRedirected = new UnityEvent<QueueCustomer>();
        public UnityEvent OnTeamworkBoostStarted = new UnityEvent();
        public UnityEvent OnTeamworkBoostEnded = new UnityEvent();

        private QueueCustomer activeInsuranceCustomer;
        private GameObject fallbackIconInstance;
        private Coroutine teamworkBoostRoutine;
        private ParticleSystem activeTeamworkEffect;
        private bool queueListenerRegistered;
        private bool boostAppliedToWire;
        private bool boostAppliedToMobileActivation;
        private bool boostAppliedToCard;
        private bool boostAppliedToUtility;
        private bool boostAppliedToFastTrack;
        private bool boostAppliedToDocument;
        private bool boostAppliedToGold;
        private bool boostAppliedToAccountOpening;

        public bool HasActiveInsuranceCustomer => activeInsuranceCustomer != null;
        public bool IsTeamworkBoostActive => teamworkBoostRoutine != null;

        private void Awake()
        {
            ResolveMissingReferences();
            SetReferButtonVisible(false);
        }

        private void OnEnable()
        {
            RegisterQueueListener();
            if (referToSpecialistButton != null)
            {
                referToSpecialistButton.onClick.AddListener(ReferActiveCustomer);
            }
        }

        private void Start()
        {
            ResolveMissingReferences();
            RegisterQueueListener();
        }

        private void OnDisable()
        {
            if (queueManager != null && queueListenerRegistered)
            {
                queueManager.OnCustomerCalled.RemoveListener(HandleCustomerCalled);
                queueListenerRegistered = false;
            }

            if (referToSpecialistButton != null)
            {
                referToSpecialistButton.onClick.RemoveListener(ReferActiveCustomer);
            }

            EndTeamworkBoost();
        }

        public void Interact()
        {
            ReferActiveCustomer();
        }

        public void ReferActiveCustomer()
        {
            if (activeInsuranceCustomer == null)
            {
                return;
            }

            if (stationeryDeliverySystem != null && !stationeryDeliverySystem.CanAcceptRedirect(insuranceSpecialistDesk))
            {
                return;
            }

            if (audioSource != null && paperSlipSound != null)
            {
                audioSource.PlayOneShot(paperSlipSound);
            }

            var redirectedCustomer = queueManager != null
                ? queueManager.ReleaseActiveCustomerForRedirect()
                : activeInsuranceCustomer;

            if (redirectedCustomer == null)
            {
                redirectedCustomer = activeInsuranceCustomer;
            }

            ClearFallbackIcon();
            SetReferButtonVisible(false);
            var speedMultiplier = stationeryDeliverySystem != null
                ? stationeryDeliverySystem.ConsumeRedirectSpeedMultiplier(insuranceSpecialistDesk)
                : 1f;
            RedirectCustomerToInsuranceDesk(redirectedCustomer, speedMultiplier);
            ApplyTeamworkBoost();
            OnInsuranceCustomerRedirected.Invoke(redirectedCustomer);
            activeInsuranceCustomer = null;
        }

        public void CancelReferral()
        {
            activeInsuranceCustomer = null;
            SetReferButtonVisible(false);
            ClearFallbackIcon();
        }

        public void ApplyTeamworkBoost()
        {
            if (teamworkBoostRoutine != null)
            {
                EndTeamworkBoost();
            }

            teamworkBoostRoutine = StartCoroutine(TeamworkBoostRoutine());
        }

        private void HandleCustomerCalled(GameObject customerObject)
        {
            ClearFallbackIcon();
            SetReferButtonVisible(false);
            activeInsuranceCustomer = null;

            if (customerObject == null || !customerObject.TryGetComponent<QueueCustomer>(out var queueCustomer))
            {
                return;
            }

            if (queueCustomer.RequestKind != CustomerRequestKind.InsuranceReferral)
            {
                return;
            }

            activeInsuranceCustomer = queueCustomer;
            if (umbrellaHouseCarIconPrefab != null)
            {
                queueCustomer.ShowRequestIcon(umbrellaHouseCarIconPrefab);
            }
            else
            {
                fallbackIconInstance = CreateFallbackInsuranceIcon(queueCustomer.transform);
            }

            SetReferButtonVisible(true);
            OnInsuranceCustomerReady.Invoke(queueCustomer);
        }

        private IEnumerator TeamworkBoostRoutine()
        {
            ApplyTeamworkMultipliers();
            SpawnTeamworkEffect();
            OnTeamworkBoostStarted.Invoke();
            yield return new WaitForSeconds(teamworkBoostSeconds);
            teamworkBoostRoutine = null;
            EndTeamworkBoost();
        }

        private void ApplyTeamworkMultipliers()
        {
            ResolveMissingReferences();

            if (wireTransferMiniGame != null)
            {
                wireTransferMiniGame.ActionTimeMultiplier *= teamworkActionTimeMultiplier;
                boostAppliedToWire = true;
            }

            if (mobileActivationMiniGame != null)
            {
                mobileActivationMiniGame.ActionTimeMultiplier *= teamworkActionTimeMultiplier;
                boostAppliedToMobileActivation = true;
            }

            if (cardBlockMiniGame != null)
            {
                cardBlockMiniGame.ActionTimeMultiplier *= teamworkActionTimeMultiplier;
                boostAppliedToCard = true;
            }

            if (utilityBillSystem != null)
            {
                utilityBillSystem.ActionTimeMultiplier *= teamworkActionTimeMultiplier;
                boostAppliedToUtility = true;
            }

            if (fastTrackActionSystem != null)
            {
                fastTrackActionSystem.ActionTimeMultiplier *= teamworkActionTimeMultiplier;
                boostAppliedToFastTrack = true;
            }

            if (documentProcessWorkflow != null)
            {
                documentProcessWorkflow.ActionTimeMultiplier *= teamworkActionTimeMultiplier;
                boostAppliedToDocument = true;
            }

            if (goldExchangeWorkflow != null)
            {
                goldExchangeWorkflow.ActionTimeMultiplier *= teamworkActionTimeMultiplier;
                boostAppliedToGold = true;
            }

            if (accountOpeningSystem != null)
            {
                accountOpeningSystem.ActionTimeMultiplier *= teamworkActionTimeMultiplier;
                boostAppliedToAccountOpening = true;
            }
        }

        private void EndTeamworkBoost()
        {
            if (wireTransferMiniGame != null && boostAppliedToWire)
            {
                wireTransferMiniGame.ActionTimeMultiplier /= teamworkActionTimeMultiplier;
            }

            if (mobileActivationMiniGame != null && boostAppliedToMobileActivation)
            {
                mobileActivationMiniGame.ActionTimeMultiplier /= teamworkActionTimeMultiplier;
            }

            if (cardBlockMiniGame != null && boostAppliedToCard)
            {
                cardBlockMiniGame.ActionTimeMultiplier /= teamworkActionTimeMultiplier;
            }

            if (utilityBillSystem != null && boostAppliedToUtility)
            {
                utilityBillSystem.ActionTimeMultiplier /= teamworkActionTimeMultiplier;
            }

            if (fastTrackActionSystem != null && boostAppliedToFastTrack)
            {
                fastTrackActionSystem.ActionTimeMultiplier /= teamworkActionTimeMultiplier;
            }

            if (documentProcessWorkflow != null && boostAppliedToDocument)
            {
                documentProcessWorkflow.ActionTimeMultiplier /= teamworkActionTimeMultiplier;
            }

            if (goldExchangeWorkflow != null && boostAppliedToGold)
            {
                goldExchangeWorkflow.ActionTimeMultiplier /= teamworkActionTimeMultiplier;
            }

            if (accountOpeningSystem != null && boostAppliedToAccountOpening)
            {
                accountOpeningSystem.ActionTimeMultiplier /= teamworkActionTimeMultiplier;
            }

            var wasActive = teamworkBoostRoutine != null
                || boostAppliedToWire
                || boostAppliedToMobileActivation
                || boostAppliedToCard
                || boostAppliedToUtility
                || boostAppliedToFastTrack
                || boostAppliedToDocument
                || boostAppliedToGold
                || boostAppliedToAccountOpening;

            boostAppliedToWire = false;
            boostAppliedToMobileActivation = false;
            boostAppliedToCard = false;
            boostAppliedToUtility = false;
            boostAppliedToFastTrack = false;
            boostAppliedToDocument = false;
            boostAppliedToGold = false;
            boostAppliedToAccountOpening = false;

            if (teamworkBoostRoutine != null)
            {
                StopCoroutine(teamworkBoostRoutine);
                teamworkBoostRoutine = null;
            }

            DestroyTeamworkEffect();
            if (wasActive)
            {
                OnTeamworkBoostEnded.Invoke();
            }
        }

        private void RedirectCustomerToInsuranceDesk(QueueCustomer customer, float speedMultiplier)
        {
            if (customer == null || insuranceSpecialistDesk == null)
            {
                return;
            }

            var agent = customer.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                StartCoroutine(MoveAgentToDeskRoutine(agent, insuranceSpecialistDesk.position, speedMultiplier));
                return;
            }

            StartCoroutine(MoveCustomerToDeskRoutine(customer.transform, insuranceSpecialistDesk.position, speedMultiplier));
        }

        private IEnumerator MoveAgentToDeskRoutine(NavMeshAgent agent, Vector3 destination, float speedMultiplier)
        {
            if (agent == null)
            {
                yield break;
            }

            var previousSpeed = agent.speed;
            agent.speed = previousSpeed * Mathf.Max(0.1f, speedMultiplier);
            agent.SetDestination(destination);
            while (agent != null && agent.enabled && agent.isOnNavMesh && agent.pathPending)
            {
                yield return null;
            }

            while (agent != null
                && agent.enabled
                && agent.isOnNavMesh
                && agent.remainingDistance > Mathf.Max(agent.stoppingDistance, 0.1f))
            {
                yield return null;
            }

            if (agent != null)
            {
                agent.speed = previousSpeed;
            }
        }

        private IEnumerator MoveCustomerToDeskRoutine(Transform customer, Vector3 destination, float speedMultiplier)
        {
            while (customer != null && (customer.position - destination).sqrMagnitude > 0.04f)
            {
                var current = customer.position;
                customer.position = Vector3.MoveTowards(
                    current,
                    destination,
                    redirectedCustomerMoveSpeed * Mathf.Max(0.1f, speedMultiplier) * Time.deltaTime);

                var direction = destination - current;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                {
                    customer.rotation = Quaternion.Slerp(
                        customer.rotation,
                        Quaternion.LookRotation(direction.normalized, Vector3.up),
                        8f * Time.deltaTime);
                }

                yield return null;
            }
        }

        private GameObject CreateFallbackInsuranceIcon(Transform customer)
        {
            var icon = new GameObject("Insurance Referral Umbrella Icon");
            icon.transform.SetParent(customer, false);
            icon.transform.localPosition = Vector3.up * 2.1f;
            icon.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
            var label = icon.AddComponent<TextMesh>();
            label.text = "UMBRELLA+HOME";
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontStyle = FontStyle.Bold;
            label.characterSize = 0.14f;
            label.color = new Color(0.32f, 0.82f, 1f);
            return icon;
        }

        private void SpawnTeamworkEffect()
        {
            DestroyTeamworkEffect();
            if (teamworkHandEffectPrefab == null)
            {
                return;
            }

            var parent = playerHandEffectAnchor != null ? playerHandEffectAnchor : transform;
            activeTeamworkEffect = Instantiate(teamworkHandEffectPrefab, parent);
            activeTeamworkEffect.transform.localPosition = Vector3.zero;
            activeTeamworkEffect.Play();
        }

        private void DestroyTeamworkEffect()
        {
            if (activeTeamworkEffect != null)
            {
                Destroy(activeTeamworkEffect.gameObject);
                activeTeamworkEffect = null;
            }
        }

        private void SetReferButtonVisible(bool visible)
        {
            if (referToSpecialistButton != null)
            {
                referToSpecialistButton.gameObject.SetActive(visible);
            }
        }

        private void ClearFallbackIcon()
        {
            if (fallbackIconInstance != null)
            {
                Destroy(fallbackIconInstance);
                fallbackIconInstance = null;
            }
        }

        private void ResolveMissingReferences()
        {
            if (queueManager == null)
            {
                queueManager = QueueManager.Instance != null ? QueueManager.Instance : FindFirstObjectByType<QueueManager>();
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (stationeryDeliverySystem == null)
            {
                stationeryDeliverySystem = StationeryDeliverySystem.Instance != null
                    ? StationeryDeliverySystem.Instance
                    : FindFirstObjectByType<StationeryDeliverySystem>();
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
        }

        private void RegisterQueueListener()
        {
            ResolveMissingReferences();
            if (queueManager == null || queueListenerRegistered)
            {
                return;
            }

            queueManager.OnCustomerCalled.AddListener(HandleCustomerCalled);
            queueListenerRegistered = true;
        }
    }
}
