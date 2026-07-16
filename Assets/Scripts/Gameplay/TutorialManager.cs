using System.Collections;
using RushBank.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace RushBank.Gameplay
{
    public enum TutorialState
    {
        MoveToCounter,
        SimpleTransaction,
        TwoStepTransaction,
        Completed
    }

    public class TutorialManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private MobilePlayerController mobilePlayerController;
        [SerializeField] private ChubbyTopDownInputController topDownController;
        [SerializeField] private TimeManager timeManager;
        [SerializeField] private QueueManager queueManager;
        [SerializeField] private FastTrackActionSystem fastTrackActionSystem;
        [SerializeField] private UtilityBillSystem utilityBillSystem;

        [Header("Tutorial Gates")]
        [SerializeField] private Collider counterTrigger;
        [SerializeField] private Transform counterPointerTarget;
        [SerializeField, Min(0.2f)] private float counterReachDistance = 1.25f;
        [SerializeField] private Transform passbookDeskTarget;
        [SerializeField] private Transform stampAreaTarget;
        [SerializeField] private Transform billDeskTarget;
        [SerializeField] private Transform barcodeScannerTarget;
        [SerializeField] private Transform scanButtonTarget;

        [Header("Customers")]
        [SerializeField] private Transform tutorialCustomerSpawnPoint;
        [SerializeField] private GameObject passbookCustomerPrefab;
        [SerializeField] private GameObject electricityBillCustomerPrefab;

        [Header("Guidance Visuals")]
        [SerializeField] private GameObject floorPointerPrefab;
        [SerializeField] private RectTransform uiFingerPointer;
        [SerializeField] private TMP_Text instructionText;
        [SerializeField] private GameObject managerDialogRoot;
        [SerializeField] private TMP_Text managerDialogText;

        [Header("Completion")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip successSound;
        [SerializeField] private string levelSelectionSceneName = "MainMenu";
        [SerializeField, Min(0f)] private float completionDelaySeconds = 2.2f;
        [SerializeField] private bool loadLevelSelectionOnComplete = true;

        [Header("Systems To Pause")]
        [SerializeField] private Behaviour[] systemsToDisableDuringTutorial;

        public UnityEvent<TutorialState> OnTutorialStateChanged = new UnityEvent<TutorialState>();
        public UnityEvent OnTutorialCompleted = new UnityEvent();

        private TutorialState state;
        private GameObject floorPointerInstance;
        private QueueCustomer activeTutorialCustomer;
        private Coroutine fingerPulseRoutine;
        private Coroutine completionRoutine;
        private float previousQueuePatienceMultiplier = 1f;
        private float previousCustomerPatienceMultiplier = 1f;
        private bool started;

        public TutorialState State => state;

        private void Awake()
        {
            ResolveMissingReferences();
            HideGuidance();
        }

        private void OnEnable()
        {
            if (fastTrackActionSystem != null)
            {
                fastTrackActionSystem.OnFastTrackStarted.AddListener(HandleFastTrackStarted);
                fastTrackActionSystem.OnFastTrackCompleted.AddListener(HandleFastTrackCompleted);
            }

            if (utilityBillSystem != null)
            {
                utilityBillSystem.OnBillScanned.AddListener(HandleBillScanned);
                utilityBillSystem.OnBillCompleted.AddListener(HandleBillCompleted);
            }
        }

        private void OnDisable()
        {
            if (fastTrackActionSystem != null)
            {
                fastTrackActionSystem.OnFastTrackStarted.RemoveListener(HandleFastTrackStarted);
                fastTrackActionSystem.OnFastTrackCompleted.RemoveListener(HandleFastTrackCompleted);
            }

            if (utilityBillSystem != null)
            {
                utilityBillSystem.OnBillScanned.RemoveListener(HandleBillScanned);
                utilityBillSystem.OnBillCompleted.RemoveListener(HandleBillCompleted);
            }

            StopFingerPulse();
            DestroyFloorPointer();
            RestorePatienceDrain();
        }

        private void Start()
        {
            BeginTutorial();
        }

        private void Update()
        {
            if (state != TutorialState.MoveToCounter || player == null)
            {
                return;
            }

            if (counterTrigger != null && counterTrigger.bounds.Contains(player.position))
            {
                NotifyCounterReached();
                return;
            }

            if (counterPointerTarget != null && Vector3.Distance(player.position, counterPointerTarget.position) <= counterReachDistance)
            {
                NotifyCounterReached();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (state != TutorialState.MoveToCounter || counterTrigger == null || other != counterTrigger)
            {
                return;
            }

            NotifyCounterReached();
        }

        public void BeginTutorial()
        {
            if (started)
            {
                return;
            }

            started = true;
            ResolveMissingReferences();
            DisableStressSystems();
            FreezeTutorialPressure();
            SetState(TutorialState.MoveToCounter);
        }

        public void NotifyCounterReached()
        {
            if (state != TutorialState.MoveToCounter)
            {
                return;
            }

            DestroyFloorPointer();
            StartSimpleTransaction();
        }

        public void NotifyPassbookStampedAndReturned()
        {
            if (state != TutorialState.SimpleTransaction)
            {
                return;
            }

            CompleteCurrentTutorialCustomer();
            StartTwoStepTransaction();
        }

        public void NotifyElectricityBillDelivered()
        {
            if (state == TutorialState.TwoStepTransaction)
            {
                CompleteTutorial();
            }
        }

        public void CompleteTutorial()
        {
            if (state == TutorialState.Completed)
            {
                return;
            }

            SetState(TutorialState.Completed);
            CompleteCurrentTutorialCustomer();
            HideGuidance();
            RestorePatienceDrain();
            SaveTutorialCompletion();
            PlaySuccessFeedback();

            if (completionRoutine != null)
            {
                StopCoroutine(completionRoutine);
            }

            completionRoutine = StartCoroutine(CompleteRoutine());
        }

        private void SetState(TutorialState nextState)
        {
            state = nextState;
            OnTutorialStateChanged.Invoke(state);

            switch (state)
            {
                case TutorialState.MoveToCounter:
                    ShowFloorPointer(counterPointerTarget);
                    SetInstruction("Gisene yuruyerek ilk mesaini baslat.");
                    break;
                case TutorialState.SimpleTransaction:
                    SetInstruction("Ilk musterinin hesap cuzdanini al, muhurle ve geri ver.");
                    break;
                case TutorialState.TwoStepTransaction:
                    SetInstruction("Elektrik faturasini al, barkod okuyucuda tarat ve teslim et.");
                    break;
                case TutorialState.Completed:
                    SetInstruction(string.Empty);
                    break;
            }
        }

        private void StartSimpleTransaction()
        {
            SetState(TutorialState.SimpleTransaction);
            activeTutorialCustomer = SpawnTutorialCustomer(CustomerRequestKind.PassbookPrinting, passbookCustomerPrefab);
            fastTrackActionSystem?.SetActiveTask(FastTrackTaskType.PassbookPrinting);
            ShowFingerAt(passbookDeskTarget);
        }

        private void StartTwoStepTransaction()
        {
            SetState(TutorialState.TwoStepTransaction);
            activeTutorialCustomer = SpawnTutorialCustomer(CustomerRequestKind.BillPayment, electricityBillCustomerPrefab);
            utilityBillSystem?.StartBillPayment(activeTutorialCustomer, BillType.Electricity);
            ShowFingerAt(billDeskTarget);
        }

        private QueueCustomer SpawnTutorialCustomer(CustomerRequestKind requestKind, GameObject prefab)
        {
            GameObject customerObject;
            var spawnPosition = tutorialCustomerSpawnPoint != null ? tutorialCustomerSpawnPoint.position : transform.position + Vector3.forward * 2f;
            if (prefab != null)
            {
                customerObject = Instantiate(prefab, spawnPosition, Quaternion.identity);
            }
            else
            {
                customerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                customerObject.name = $"Tutorial Customer - {requestKind}";
                customerObject.transform.position = spawnPosition;
                customerObject.AddComponent<Rigidbody>().isKinematic = true;
                TintTutorialCustomer(customerObject, requestKind);
            }

            var queueCustomer = customerObject.GetComponent<QueueCustomer>();
            if (queueCustomer == null)
            {
                queueCustomer = customerObject.AddComponent<QueueCustomer>();
            }

            queueCustomer.Initialize(CustomerAgeGroup.Middle, CustomerGender.Male, requestKind, null);
            queueCustomer.StartPatience(9999f);
            queueCustomer.SetPatienceDrainMultiplier(0f);

            if (queueManager != null)
            {
                queueManager.RemoveCustomer(customerObject);
            }

            return queueCustomer;
        }

        private void CompleteCurrentTutorialCustomer()
        {
            if (activeTutorialCustomer == null)
            {
                return;
            }

            Destroy(activeTutorialCustomer.gameObject);
            activeTutorialCustomer = null;
        }

        private void HandleFastTrackStarted(FastTrackTaskType taskType)
        {
            if (state == TutorialState.SimpleTransaction && taskType == FastTrackTaskType.PassbookPrinting)
            {
                ShowFingerAt(stampAreaTarget);
            }
        }

        private void HandleFastTrackCompleted(FastTrackTaskType taskType)
        {
            if (state == TutorialState.SimpleTransaction && taskType == FastTrackTaskType.PassbookPrinting)
            {
                NotifyPassbookStampedAndReturned();
            }
        }

        private void HandleBillScanned(BillType billType)
        {
            if (state == TutorialState.TwoStepTransaction && billType == BillType.Electricity)
            {
                ShowFingerAt(scanButtonTarget != null ? scanButtonTarget : barcodeScannerTarget);
            }
        }

        private void HandleBillCompleted(BillType billType)
        {
            if (state == TutorialState.TwoStepTransaction && billType == BillType.Electricity)
            {
                NotifyElectricityBillDelivered();
            }
        }

        private void DisableStressSystems()
        {
            if (systemsToDisableDuringTutorial == null || systemsToDisableDuringTutorial.Length == 0)
            {
                DisableFoundSystem<QuestSpawner>();
                DisableFoundSystem<QuestPoolDirector>();
                DisableFoundSystem<ThiefEventSystem>();
                DisableFoundSystem<HeistRaidSystem>();
                DisableFoundSystem<PhoneInterruptionSystem>();
                DisableFoundSystem<StaffInterruptionSystem>();
                DisableFoundSystem<TeaLadyBoostSystem>();
                DisableFoundSystem<AssistantManager>();
                return;
            }

            for (var i = 0; i < systemsToDisableDuringTutorial.Length; i++)
            {
                if (systemsToDisableDuringTutorial[i] != null)
                {
                    systemsToDisableDuringTutorial[i].enabled = false;
                }
            }
        }

        private void DisableFoundSystem<T>() where T : Behaviour
        {
            var system = FindFirstObjectByType<T>();
            if (system != null)
            {
                system.enabled = false;
            }
        }

        private void FreezeTutorialPressure()
        {
            if (timeManager != null)
            {
                timeManager.PauseTimer();
                timeManager.FreezeTime(false);
            }

            previousQueuePatienceMultiplier = QueueCustomer.GlobalPatienceDrainMultiplier;
            previousCustomerPatienceMultiplier = CustomerPatience.GlobalPatienceDrainMultiplier;
            QueueCustomer.GlobalPatienceDrainMultiplier = 0f;
            CustomerPatience.GlobalPatienceDrainMultiplier = 0f;
        }

        private void RestorePatienceDrain()
        {
            QueueCustomer.GlobalPatienceDrainMultiplier = previousQueuePatienceMultiplier;
            CustomerPatience.GlobalPatienceDrainMultiplier = previousCustomerPatienceMultiplier;
        }

        private void SaveTutorialCompletion()
        {
            var settingsManager = GameSettingsManager.EnsureInstance();
            settingsManager.UnlockTasraBranch();
            settingsManager.SetBranchSettings(BranchSettings.CreateDefault(BranchType.Tasra));
        }

        private IEnumerator CompleteRoutine()
        {
            yield return new WaitForSecondsRealtime(completionDelaySeconds);

            OnTutorialCompleted.Invoke();
            if (loadLevelSelectionOnComplete)
            {
                LoadLevelSelection();
            }

            completionRoutine = null;
        }

        private void LoadLevelSelection()
        {
            if (!string.IsNullOrWhiteSpace(levelSelectionSceneName))
            {
                for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                {
                    var scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                    var sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                    if (string.Equals(sceneName, levelSelectionSceneName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        SceneManager.LoadScene(i);
                        return;
                    }
                }
            }

            SceneManager.LoadScene((int)SceneId.MainMenu);
        }

        private void PlaySuccessFeedback()
        {
            if (audioSource != null && successSound != null)
            {
                audioSource.PlayOneShot(successSound);
            }

            if (managerDialogRoot != null)
            {
                managerDialogRoot.SetActive(true);
            }

            if (managerDialogText != null)
            {
                managerDialogText.text = "Harika is cikardin! Artik gercek Tasra Subesi'ne hazirsin.";
            }
        }

        private void ShowFloorPointer(Transform target)
        {
            DestroyFloorPointer();
            if (target == null)
            {
                return;
            }

            floorPointerInstance = floorPointerPrefab != null
                ? Instantiate(floorPointerPrefab, target.position, Quaternion.identity)
                : CreateFallbackFloorPointer(target.position);
        }

        private GameObject CreateFallbackFloorPointer(Vector3 position)
        {
            var pointer = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pointer.name = "Tutorial Counter Glowing Ring";
            pointer.transform.position = position + Vector3.up * 0.025f;
            pointer.transform.localScale = new Vector3(1.35f, 0.02f, 1.35f);

            var renderer = pointer.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = new Color(1f, 0.82f, 0.24f, 0.65f);
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(1f, 0.72f, 0.18f) * 1.4f);
                renderer.sharedMaterial = material;
            }

            if (pointer.TryGetComponent<Collider>(out var collider))
            {
                collider.enabled = false;
            }

            return pointer;
        }

        private void DestroyFloorPointer()
        {
            if (floorPointerInstance != null)
            {
                Destroy(floorPointerInstance);
                floorPointerInstance = null;
            }
        }

        private void ShowFingerAt(Transform target)
        {
            if (uiFingerPointer == null)
            {
                ShowFloorPointer(target);
                return;
            }

            uiFingerPointer.gameObject.SetActive(true);
            if (target != null && Camera.main != null)
            {
                uiFingerPointer.position = Camera.main.WorldToScreenPoint(target.position + Vector3.up * 0.55f);
            }

            StartFingerPulse();
        }

        private void StartFingerPulse()
        {
            StopFingerPulse();
            if (uiFingerPointer != null)
            {
                fingerPulseRoutine = StartCoroutine(FingerPulseRoutine());
            }
        }

        private void StopFingerPulse()
        {
            if (fingerPulseRoutine != null)
            {
                StopCoroutine(fingerPulseRoutine);
                fingerPulseRoutine = null;
            }

            if (uiFingerPointer != null)
            {
                uiFingerPointer.localScale = Vector3.one;
            }
        }

        private IEnumerator FingerPulseRoutine()
        {
            while (uiFingerPointer != null && uiFingerPointer.gameObject.activeSelf)
            {
                var wave = Mathf.Sin(Time.unscaledTime * 6f) * 0.12f;
                uiFingerPointer.localScale = Vector3.one * (1f + wave);
                yield return null;
            }
        }

        private void SetInstruction(string text)
        {
            if (instructionText != null)
            {
                instructionText.text = text;
                instructionText.gameObject.SetActive(!string.IsNullOrWhiteSpace(text));
            }
        }

        private void HideGuidance()
        {
            SetInstruction(string.Empty);
            StopFingerPulse();
            DestroyFloorPointer();

            if (uiFingerPointer != null)
            {
                uiFingerPointer.gameObject.SetActive(false);
            }

            if (managerDialogRoot != null)
            {
                managerDialogRoot.SetActive(false);
            }
        }

        private void ResolveMissingReferences()
        {
            if (player == null)
            {
                if (mobilePlayerController == null)
                {
                    mobilePlayerController = FindFirstObjectByType<MobilePlayerController>();
                }

                if (mobilePlayerController != null)
                {
                    player = mobilePlayerController.transform;
                }
            }

            if (topDownController == null && player != null)
            {
                topDownController = player.GetComponent<ChubbyTopDownInputController>();
            }

            if (timeManager == null)
            {
                timeManager = TimeManager.Instance != null ? TimeManager.Instance : FindFirstObjectByType<TimeManager>();
            }

            if (queueManager == null)
            {
                queueManager = QueueManager.Instance != null ? QueueManager.Instance : FindFirstObjectByType<QueueManager>();
            }

            if (fastTrackActionSystem == null)
            {
                fastTrackActionSystem = FindFirstObjectByType<FastTrackActionSystem>();
            }

            if (utilityBillSystem == null)
            {
                utilityBillSystem = FindFirstObjectByType<UtilityBillSystem>();
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        private static void TintTutorialCustomer(GameObject customerObject, CustomerRequestKind requestKind)
        {
            if (customerObject == null || !customerObject.TryGetComponent<Renderer>(out var renderer))
            {
                return;
            }

            var material = new Material(Shader.Find("Standard"));
            material.color = requestKind == CustomerRequestKind.BillPayment
                ? new Color(1f, 0.84f, 0.18f)
                : new Color(0.42f, 0.72f, 1f);
            renderer.sharedMaterial = material;
        }
    }
}
