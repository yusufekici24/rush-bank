using System.Collections.Generic;
using RushBank.Core;
using UnityEngine;
using UnityEngine.Events;

namespace RushBank.Gameplay
{
    public class QueueManager : MonoBehaviour
    {
        public static QueueManager Instance { get; private set; }

        [System.Serializable]
        public class CustomerAppearanceVariant
        {
            public CustomerAgeGroup ageGroup;
            public CustomerGender gender;
            public GameObject prefab;
            public Material[] materials;
        }

        [Header("Queue")]
        [SerializeField] private List<GameObject> customerQueue = new List<GameObject>();
        [SerializeField] private Transform entrancePoint;
        [SerializeField] private Transform waitingArea;
        [SerializeField] private Transform counterPosition;
        [SerializeField] private Vector3 waitingLineDirection = Vector3.back;
        [SerializeField, Min(0.5f)] private float queueSpacing = 1.2f;
        [SerializeField, Min(0.1f)] private float moveSpeed = 2.2f;

        [Header("Spawn")]
        [SerializeField] private GameObject customerPrefab;
        [SerializeField] private GameObject vipCustomerPrefab;
        [SerializeField] private List<CustomerAppearanceVariant> appearanceVariants = new List<CustomerAppearanceVariant>();
        [SerializeField] private Material[] youthMaterials;
        [SerializeField] private Material[] middleMaterials;
        [SerializeField] private Material[] elderlyMaterials;
        [SerializeField] private GameObject[] requestIconPrefabs;

        [Header("Patience")]
        [SerializeField, Min(1f)] private float basePatienceSeconds = 22f;
        [SerializeField, Min(0f)] private float angryLeavePenaltySeconds = 5f;
        [SerializeField] private Transform exitPoint;

        public UnityEvent<GameObject> OnCustomerEntered = new UnityEvent<GameObject>();
        public UnityEvent<GameObject> OnCustomerCalled = new UnityEvent<GameObject>();
        public UnityEvent<GameObject> OnCustomerLeftAngry = new UnityEvent<GameObject>();

        private readonly List<QueueCustomer> movingToWaiting = new List<QueueCustomer>();
        private readonly List<QueueCustomer> angryLeaving = new List<QueueCustomer>();
        private QueueCustomer activeCustomer;

        public IReadOnlyList<GameObject> CustomerQueue => customerQueue;
        public QueueCustomer ActiveCustomer => activeCustomer;
        public int QueueCount => customerQueue.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ApplySelectedBranchSettings();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            UpdateCustomersMovingToWaiting();
            UpdateActiveCustomer();
            UpdateAngryLeavingCustomers();
        }

        public void AddCustomerToQueue(GameObject customerObject)
        {
            AddCustomerToQueue(customerObject, false);
        }

        public void AddCustomerToQueue(GameObject customerObject, bool keepCurrentPosition)
        {
            if (customerObject == null)
            {
                return;
            }

            var queueCustomer = EnsureQueueCustomer(customerObject);
            EnsureCustomerPatience(customerObject);
            if (!keepCurrentPosition)
            {
                customerObject.transform.position = GetEntrancePosition();
            }

            customerQueue.Add(customerObject);
            movingToWaiting.Add(queueCustomer);
            RefreshWaitingTargets();
            OnCustomerEntered.Invoke(customerObject);
        }

        public GameObject SpawnRandomCustomer()
        {
            var ageGroup = (CustomerAgeGroup)Random.Range(0, 3);
            var gender = (CustomerGender)Random.Range(0, 2);
            var requestCount = System.Enum.GetValues(typeof(CustomerRequestKind)).Length;
            var request = (CustomerRequestKind)Random.Range(0, requestCount);
            return SpawnCustomer(request, ageGroup, gender);
        }

        public GameObject SpawnCustomer(CustomerRequestKind requestKind)
        {
            var ageGroup = (CustomerAgeGroup)Random.Range(0, 3);
            var gender = (CustomerGender)Random.Range(0, 2);
            return SpawnCustomer(requestKind, ageGroup, gender);
        }

        public GameObject SpawnCustomer(
            CustomerRequestKind requestKind,
            CustomerAgeGroup ageGroup,
            CustomerGender gender)
        {
            var variant = PickAppearanceVariant(ageGroup, gender);
            var customerObject = CreateCustomerObject(variant, requestKind);
            var queueCustomer = EnsureQueueCustomer(customerObject, requestKind);
            var material = PickMaterial(ageGroup, variant);

            queueCustomer.Initialize(ageGroup, gender, requestKind, material);
            AddCustomerToQueue(customerObject);
            return customerObject;
        }

        public void CallNextCustomer()
        {
            if (activeCustomer != null || customerQueue.Count == 0)
            {
                return;
            }

            var nextCustomerObject = customerQueue[0];
            customerQueue.RemoveAt(0);
            RefreshWaitingTargets();

            activeCustomer = EnsureQueueCustomer(nextCustomerObject);
            activeCustomer.ShowRequestIcon(GetRequestIcon(activeCustomer.RequestKind));
            activeCustomer.StartPatience(GetPatienceSeconds(activeCustomer));

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.StartTimer();
            }

            OnCustomerCalled.Invoke(nextCustomerObject);
        }

        public QueueCustomer TakeNextCustomerForAssistant()
        {
            if (customerQueue.Count == 0)
            {
                return null;
            }

            var nextCustomerObject = customerQueue[0];
            customerQueue.RemoveAt(0);
            RefreshWaitingTargets();

            for (var i = movingToWaiting.Count - 1; i >= 0; i--)
            {
                if (movingToWaiting[i] == null || movingToWaiting[i].gameObject == nextCustomerObject)
                {
                    movingToWaiting.RemoveAt(i);
                }
            }

            var assistantCustomer = EnsureQueueCustomer(nextCustomerObject);
            assistantCustomer.ShowRequestIcon(GetRequestIcon(assistantCustomer.RequestKind));
            assistantCustomer.StartPatience(GetPatienceSeconds(assistantCustomer) * 1.5f);
            OnCustomerCalled.Invoke(nextCustomerObject);
            return assistantCustomer;
        }

        public void CompleteActiveCustomer()
        {
            if (activeCustomer == null)
            {
                return;
            }

            activeCustomer.StopPatience();
            activeCustomer.ClearRequestIcon();
            Destroy(activeCustomer.gameObject);
            activeCustomer = null;
        }

        public CustomerPatience FindLowestPatienceCustomer()
        {
            CustomerPatience lowestPatienceCustomer = null;
            var lowestPatience = float.MaxValue;

            for (var i = 0; i < customerQueue.Count; i++)
            {
                var customer = customerQueue[i];
                if (customer == null || !customer.TryGetComponent<CustomerPatience>(out var patience))
                {
                    continue;
                }

                if (patience.Patience < lowestPatience)
                {
                    lowestPatience = patience.Patience;
                    lowestPatienceCustomer = patience;
                }
            }

            if (activeCustomer != null && activeCustomer.gameObject.TryGetComponent<CustomerPatience>(out var activePatience)
                && activePatience.Patience < lowestPatience)
            {
                lowestPatienceCustomer = activePatience;
            }

            return lowestPatienceCustomer;
        }

        public bool RemoveCustomer(GameObject customerObject)
        {
            if (customerObject == null)
            {
                return false;
            }

            var removed = customerQueue.Remove(customerObject);
            if (activeCustomer != null && activeCustomer.gameObject == customerObject)
            {
                activeCustomer = null;
                removed = true;
            }

            for (var i = movingToWaiting.Count - 1; i >= 0; i--)
            {
                if (movingToWaiting[i] == null || movingToWaiting[i].gameObject == customerObject)
                {
                    movingToWaiting.RemoveAt(i);
                }
            }

            for (var i = angryLeaving.Count - 1; i >= 0; i--)
            {
                if (angryLeaving[i] == null || angryLeaving[i].gameObject == customerObject)
                {
                    angryLeaving.RemoveAt(i);
                }
            }

            if (removed)
            {
                Destroy(customerObject);
                RefreshWaitingTargets();
            }

            return removed;
        }

        public void ApplyQueueReliefBoost(float restorePercent, float drainMultiplier, float durationSeconds)
        {
            restorePercent = Mathf.Clamp01(restorePercent);
            drainMultiplier = Mathf.Max(0f, drainMultiplier);
            durationSeconds = Mathf.Max(0f, durationSeconds);

            ApplyReliefToCustomer(activeCustomer, restorePercent, drainMultiplier);

            for (var i = 0; i < customerQueue.Count; i++)
            {
                if (customerQueue[i] == null)
                {
                    continue;
                }

                ApplyReliefToCustomer(EnsureQueueCustomer(customerQueue[i]), restorePercent, drainMultiplier);
            }

            if (durationSeconds > 0f)
            {
                CancelInvoke(nameof(ResetQueueReliefBoost));
                Invoke(nameof(ResetQueueReliefBoost), durationSeconds);
            }
        }

        public void ResetQueueReliefBoost()
        {
            ResetReliefForCustomer(activeCustomer);

            for (var i = 0; i < customerQueue.Count; i++)
            {
                if (customerQueue[i] == null)
                {
                    continue;
                }

                ResetReliefForCustomer(EnsureQueueCustomer(customerQueue[i]));
            }
        }

        public void ApplyBranchSettings(BranchSettings settings)
        {
            var clampedSettings = settings.WithClampedValues();
            QueueCustomer.GlobalPatienceDrainMultiplier = clampedSettings.globalPatienceMultiplier;
            CustomerPatience.GlobalPatienceDrainMultiplier = clampedSettings.globalPatienceMultiplier;
        }

        public void ApplySelectedBranchSettings()
        {
            var settingsManager = GameSettingsManager.Instance;
            if (settingsManager == null)
            {
                return;
            }

            ApplyBranchSettings(settingsManager.CurrentBranchSettings);
        }

        private void UpdateCustomersMovingToWaiting()
        {
            for (var i = movingToWaiting.Count - 1; i >= 0; i--)
            {
                var customer = movingToWaiting[i];
                if (customer == null)
                {
                    movingToWaiting.RemoveAt(i);
                    continue;
                }

                var targetPosition = GetWaitingPosition(customer.gameObject);
                customer.MoveTowards(targetPosition, moveSpeed);

                if ((customer.transform.position - targetPosition).sqrMagnitude < 0.02f)
                {
                    movingToWaiting.RemoveAt(i);
                }
            }
        }

        private void UpdateActiveCustomer()
        {
            if (activeCustomer == null)
            {
                return;
            }

            activeCustomer.MoveTowards(GetCounterPosition(), moveSpeed);
            if (activeCustomer.IsPatienceExpired)
            {
                SendActiveCustomerAwayAngry();
            }
        }

        private void UpdateAngryLeavingCustomers()
        {
            var targetPosition = GetExitPosition();
            for (var i = angryLeaving.Count - 1; i >= 0; i--)
            {
                var customer = angryLeaving[i];
                if (customer == null)
                {
                    angryLeaving.RemoveAt(i);
                    continue;
                }

                customer.MoveTowards(targetPosition, moveSpeed * 1.25f);
                if ((customer.transform.position - targetPosition).sqrMagnitude < 0.05f)
                {
                    Destroy(customer.gameObject);
                    angryLeaving.RemoveAt(i);
                }
            }
        }

        private void SendActiveCustomerAwayAngry()
        {
            var leavingCustomer = activeCustomer;
            activeCustomer = null;

            leavingCustomer.StopPatience();
            leavingCustomer.ClearRequestIcon();
            angryLeaving.Add(leavingCustomer);

            if (TimeManager.Instance != null && angryLeavePenaltySeconds > 0f)
            {
                TimeManager.Instance.SubtractTime(angryLeavePenaltySeconds);
            }

            OnCustomerLeftAngry.Invoke(leavingCustomer.gameObject);
        }

        private void RefreshWaitingTargets()
        {
            for (var i = 0; i < customerQueue.Count; i++)
            {
                var customer = customerQueue[i];
                if (customer == null)
                {
                    continue;
                }

                var queueCustomer = EnsureQueueCustomer(customer);
                if (!movingToWaiting.Contains(queueCustomer))
                {
                    movingToWaiting.Add(queueCustomer);
                }
            }
        }

        private GameObject CreateCustomerObject(CustomerAppearanceVariant variant, CustomerRequestKind requestKind)
        {
            var prefab = requestKind == CustomerRequestKind.VipSafeRental && vipCustomerPrefab != null
                ? vipCustomerPrefab
                : variant != null && variant.prefab != null
                    ? variant.prefab
                    : customerPrefab;

            if (prefab != null)
            {
                return Instantiate(prefab, GetEntrancePosition(), Quaternion.identity);
            }

            var customerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            customerObject.name = "Queue Customer";
            customerObject.transform.position = GetEntrancePosition();
            customerObject.AddComponent<Rigidbody>().isKinematic = true;
            return customerObject;
        }

        private QueueCustomer EnsureQueueCustomer(GameObject customerObject)
        {
            return EnsureQueueCustomer(customerObject, null);
        }

        private QueueCustomer EnsureQueueCustomer(GameObject customerObject, CustomerRequestKind? requestKind)
        {
            if (customerObject.TryGetComponent<VIPCustomer>(out var vipCustomer))
            {
                return vipCustomer;
            }

            if (requestKind == CustomerRequestKind.VipSafeRental)
            {
                return customerObject.AddComponent<VIPCustomer>();
            }

            var queueCustomer = customerObject.GetComponent<QueueCustomer>();
            if (queueCustomer == null)
            {
                queueCustomer = customerObject.AddComponent<QueueCustomer>();
            }

            return queueCustomer;
        }

        private CustomerPatience EnsureCustomerPatience(GameObject customerObject)
        {
            var patience = customerObject.GetComponent<CustomerPatience>();
            if (patience == null)
            {
                patience = customerObject.AddComponent<CustomerPatience>();
            }

            return patience;
        }

        private CustomerAppearanceVariant PickAppearanceVariant(CustomerAgeGroup ageGroup, CustomerGender gender)
        {
            if (appearanceVariants == null || appearanceVariants.Count == 0)
            {
                return null;
            }

            var firstLooseMatch = -1;
            for (var i = 0; i < appearanceVariants.Count; i++)
            {
                var variant = appearanceVariants[i];
                if (variant == null || variant.ageGroup != ageGroup)
                {
                    continue;
                }

                if (variant.gender == gender)
                {
                    return variant;
                }

                if (firstLooseMatch < 0)
                {
                    firstLooseMatch = i;
                }
            }

            return firstLooseMatch >= 0 ? appearanceVariants[firstLooseMatch] : null;
        }

        private Material PickMaterial(CustomerAgeGroup ageGroup, CustomerAppearanceVariant variant)
        {
            var materials = variant != null && variant.materials != null && variant.materials.Length > 0
                ? variant.materials
                : (ageGroup switch
                {
                    CustomerAgeGroup.Youth => youthMaterials,
                    CustomerAgeGroup.Middle => middleMaterials,
                    CustomerAgeGroup.Elderly => elderlyMaterials,
                    _ => null
                });

            if (materials == null || materials.Length == 0)
            {
                return null;
            }

            return materials[Random.Range(0, materials.Length)];
        }

        private GameObject GetRequestIcon(CustomerRequestKind requestKind)
        {
            var index = (int)requestKind;
            if (requestIconPrefabs == null || index < 0 || index >= requestIconPrefabs.Length)
            {
                return null;
            }

            return requestIconPrefabs[index];
        }

        private float GetPatienceSeconds(QueueCustomer customer)
        {
            if (customer == null)
            {
                return basePatienceSeconds;
            }

            return basePatienceSeconds * Mathf.Max(0.1f, customer.PatienceSecondsMultiplier);
        }

        private static void ApplyReliefToCustomer(QueueCustomer customer, float restorePercent, float drainMultiplier)
        {
            if (customer == null)
            {
                return;
            }

            if (restorePercent >= 0.99f)
            {
                customer.ResetPatienceToFull();
            }
            else if (restorePercent > 0f)
            {
                customer.RestorePatiencePercent(restorePercent);
            }

            customer.SetPatienceDrainMultiplier(drainMultiplier);

            if (customer.TryGetComponent<CustomerPatience>(out var patience))
            {
                patience.RestorePatience(restorePercent * 100f);
                patience.SetDrainMultiplier(drainMultiplier);
            }
        }

        private static void ResetReliefForCustomer(QueueCustomer customer)
        {
            if (customer == null)
            {
                return;
            }

            customer.ResetPatienceDrainMultiplier();

            if (customer.TryGetComponent<CustomerPatience>(out var patience))
            {
                patience.ResetDrainMultiplier();
            }
        }

        private Vector3 GetEntrancePosition()
        {
            return entrancePoint != null ? entrancePoint.position : transform.position;
        }

        private Vector3 GetCounterPosition()
        {
            return counterPosition != null ? counterPosition.position : transform.position + Vector3.forward * 2f;
        }

        private Vector3 GetExitPosition()
        {
            return exitPoint != null ? exitPoint.position : GetEntrancePosition();
        }

        private Vector3 GetWaitingPosition(GameObject customerObject)
        {
            var index = customerQueue.IndexOf(customerObject);
            if (index < 0)
            {
                index = 0;
            }

            var start = waitingArea != null ? waitingArea.position : transform.position;
            var direction = waitingLineDirection.sqrMagnitude > 0f ? waitingLineDirection.normalized : Vector3.back;
            return start + direction * queueSpacing * index;
        }
    }
}
