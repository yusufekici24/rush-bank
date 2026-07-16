using System.Collections.Generic;
using RushBank.Core;
using UnityEngine;
using UnityEngine.Events;

namespace RushBank.Gameplay
{
    [System.Serializable]
    public struct QuestData
    {
        public string questName;
        [Min(0f)] public float spawnProbabilityWeight;
        public GameObject customerRequestPrefab;
        [Min(0f)] public float rewardTime;

        [Header("Progression")]
        public CustomerRequestKind requestKind;
        [Min(1)] public int unlockLevel;
        public bool isEasyQuickWin;
    }

    public class QuestSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private QueueManager queueManager;
        [SerializeField] private ThiefEventSystem thiefEventSystem;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private GameObject customerPrefab;

        [Header("Level Progression")]
        [SerializeField, Min(1)] private int currentLevel = 1;
        [SerializeField] private List<QuestData> allQuestData = new List<QuestData>
        {
            new QuestData { questName = "Hesap Cüzdanı", spawnProbabilityWeight = 60f, rewardTime = 4f, requestKind = CustomerRequestKind.PassbookPrinting, unlockLevel = 1, isEasyQuickWin = true },
            new QuestData { questName = "Para Çekme/Yatırma", spawnProbabilityWeight = 40f, rewardTime = 7f, requestKind = CustomerRequestKind.CashWithdrawDeposit, unlockLevel = 1, isEasyQuickWin = false },
            new QuestData { questName = "Fatura Ödeme", spawnProbabilityWeight = 20f, rewardTime = 4f, requestKind = CustomerRequestKind.BillPayment, unlockLevel = 2, isEasyQuickWin = true },
            new QuestData { questName = "Kart Şifre Blokesi", spawnProbabilityWeight = 18f, rewardTime = 5f, requestKind = CustomerRequestKind.CardBlockRemoval, unlockLevel = 3, isEasyQuickWin = true },
            new QuestData { questName = "Döviz Bozdurma", spawnProbabilityWeight = 16f, rewardTime = 7f, requestKind = CustomerRequestKind.CurrencyExchange, unlockLevel = 3, isEasyQuickWin = false },
            new QuestData { questName = "Kredi Onayı", spawnProbabilityWeight = 12f, rewardTime = 12f, requestKind = CustomerRequestKind.CreditApproval, unlockLevel = 4, isEasyQuickWin = false },
            new QuestData { questName = "Altın Bozdurma", spawnProbabilityWeight = 12f, rewardTime = 10f, requestKind = CustomerRequestKind.GoldExchange, unlockLevel = 5, isEasyQuickWin = false },
            new QuestData { questName = "VIP Kiralık Kasa", spawnProbabilityWeight = 7f, rewardTime = 15f, requestKind = CustomerRequestKind.VipSafeRental, unlockLevel = 5, isEasyQuickWin = false },
            new QuestData { questName = "Hırsız", spawnProbabilityWeight = 5f, rewardTime = 0f, requestKind = CustomerRequestKind.Thief, unlockLevel = 5, isEasyQuickWin = false }
        };
        [SerializeField] private List<QuestData> activeUnlockedQuests = new List<QuestData>();

        [Header("Spawn Timing")]
        [SerializeField] private Vector2 spawnCooldown = new Vector2(5f, 8f);
        [SerializeField, Min(1)] private int maxQueueCapacity = 5;

        [Header("Dynamic Pacing")]
        [SerializeField, Min(0f)] private float criticalTimeThresholdSeconds = 20f;
        [SerializeField, Range(0.1f, 1f)] private float criticalCooldownMultiplier = 0.7f;
        [SerializeField, Min(1f)] private float quickWinWeightMultiplier = 2.5f;

        public UnityEvent<QuestData> OnQuestSelected = new UnityEvent<QuestData>();
        public UnityEvent<GameObject> OnCustomerSpawned = new UnityEvent<GameObject>();
        public UnityEvent OnQueueCapacityReached = new UnityEvent();
        public UnityEvent OnThiefEventBlockedSpawn = new UnityEvent();

        private float spawnTimer;
        private bool queueCapacityPauseRaised;
        private bool thiefEventPauseRaised;

        public int CurrentLevel => currentLevel;
        public IReadOnlyList<QuestData> ActiveUnlockedQuests => activeUnlockedQuests;

        private void Awake()
        {
            if (queueManager == null)
            {
                queueManager = QueueManager.Instance;
            }

            RefreshUnlockedQuests();
            ApplySelectedBranchSettings();
        }

        private void OnEnable()
        {
            ResetSpawnTimer();
        }

        private void Update()
        {
            if (queueManager == null)
            {
                queueManager = QueueManager.Instance;
            }

            if (queueManager == null)
            {
                return;
            }

            // Spawn decisions intentionally happen only when the timer expires:
            // queue capacity and thief events pause spawning without consuming a new cooldown.
            spawnTimer -= Time.deltaTime;
            if (spawnTimer > 0f)
            {
                return;
            }

            if (queueManager.QueueCount >= maxQueueCapacity)
            {
                if (!queueCapacityPauseRaised)
                {
                    queueCapacityPauseRaised = true;
                    OnQueueCapacityReached.Invoke();
                }

                return;
            }

            queueCapacityPauseRaised = false;

            if (thiefEventSystem != null && thiefEventSystem.IsEventActive)
            {
                if (!thiefEventPauseRaised)
                {
                    thiefEventPauseRaised = true;
                    OnThiefEventBlockedSpawn.Invoke();
                }

                return;
            }

            thiefEventPauseRaised = false;

            SpawnQuestCustomer();
            ResetSpawnTimer();
        }

        public void SetLevel(int level)
        {
            currentLevel = Mathf.Max(1, level);
            RefreshUnlockedQuests();
        }

        public void AdvanceLevel()
        {
            SetLevel(currentLevel + 1);
        }

        public void ApplyBranchSettings(BranchSettings settings)
        {
            var clampedSettings = settings.WithClampedValues();
            var interval = clampedSettings.customerSpawnInterval;
            spawnCooldown = new Vector2(interval * 0.85f, interval * 1.15f);

            currentLevel = clampedSettings.branchType switch
            {
                BranchType.Tasra => 1,
                BranchType.Sehir => 3,
                BranchType.Metropol => 5,
                _ => currentLevel
            };

            RefreshUnlockedQuests();
            ResetSpawnTimer();
        }

        public void ApplySelectedBranchSettings()
        {
            if (GameSettingsManager.Instance == null)
            {
                return;
            }

            ApplyBranchSettings(GameSettingsManager.Instance.CurrentBranchSettings);
        }

        public void RefreshUnlockedQuests()
        {
            // Rebuild the active pool whenever the level/day changes.
            activeUnlockedQuests.Clear();
            for (var i = 0; i < allQuestData.Count; i++)
            {
                var quest = allQuestData[i];
                if (quest.unlockLevel <= currentLevel && quest.spawnProbabilityWeight > 0f)
                {
                    activeUnlockedQuests.Add(quest);
                }
            }
        }

        public void SpawnQuestCustomer()
        {
            if (activeUnlockedQuests.Count == 0)
            {
                RefreshUnlockedQuests();
            }

            if (activeUnlockedQuests.Count == 0)
            {
                return;
            }

            var quest = PickWeightedQuest();
            OnQuestSelected.Invoke(quest);

            if (quest.requestKind == CustomerRequestKind.Thief && thiefEventSystem != null)
            {
                thiefEventSystem.SpawnThief();
                return;
            }

            var age = (CustomerAgeGroup)Random.Range(0, 3);
            var gender = (CustomerGender)Random.Range(0, 2);
            var customer = CreateCustomerObject(age, gender);
            var queueCustomer = EnsureQueueCustomer(customer);
            queueCustomer.Initialize(age, gender, quest.requestKind, null);
            queueCustomer.ShowRequestIcon(quest.customerRequestPrefab);

            queueManager.AddCustomerToQueue(customer, true);
            OnCustomerSpawned.Invoke(customer);
        }

        private QuestData PickWeightedQuest()
        {
            // Easy/quick-win quests receive a temporary weight boost during critical time.
            var totalWeight = 0f;
            for (var i = 0; i < activeUnlockedQuests.Count; i++)
            {
                totalWeight += GetEffectiveWeight(activeUnlockedQuests[i]);
            }

            if (totalWeight <= 0f)
            {
                return activeUnlockedQuests.Count > 0 ? activeUnlockedQuests[0] : default;
            }

            var roll = Random.Range(0f, totalWeight);
            for (var i = 0; i < activeUnlockedQuests.Count; i++)
            {
                roll -= GetEffectiveWeight(activeUnlockedQuests[i]);
                if (roll <= 0f)
                {
                    return activeUnlockedQuests[i];
                }
            }

            return activeUnlockedQuests[activeUnlockedQuests.Count - 1];
        }

        private float GetEffectiveWeight(QuestData quest)
        {
            var weight = Mathf.Max(0f, quest.spawnProbabilityWeight);
            if (IsCriticalTime() && quest.isEasyQuickWin)
            {
                weight *= quickWinWeightMultiplier;
            }

            return weight;
        }

        private GameObject CreateCustomerObject(CustomerAgeGroup age, CustomerGender gender)
        {
            var customer = customerPrefab != null
                ? Instantiate(customerPrefab, GetSpawnPosition(), GetSpawnRotation())
                : CreateFallbackCustomer();

            ApplyRandomAppearance(customer, age, gender);
            return customer;
        }

        private GameObject CreateFallbackCustomer()
        {
            var customer = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            customer.name = "Quest Customer";
            customer.transform.SetPositionAndRotation(GetSpawnPosition(), GetSpawnRotation());

            var body = customer.AddComponent<Rigidbody>();
            body.isKinematic = true;

            return customer;
        }

        private static QueueCustomer EnsureQueueCustomer(GameObject customer)
        {
            var queueCustomer = customer.GetComponent<QueueCustomer>();
            if (queueCustomer == null)
            {
                queueCustomer = customer.AddComponent<QueueCustomer>();
            }

            return queueCustomer;
        }

        private void ApplyRandomAppearance(GameObject customer, CustomerAgeGroup age, CustomerGender gender)
        {
            // Prefabs can include child meshes named with age/gender tokens,
            // e.g. YoungMale, FemaleMiddle, Elderly. Only matching visuals stay active.
            var renderers = customer.GetComponentsInChildren<Renderer>(true);
            var foundTaggedVisual = false;

            for (var i = 0; i < renderers.Length; i++)
            {
                var visualObject = renderers[i].gameObject;
                if (visualObject == customer)
                {
                    continue;
                }

                var lowerName = visualObject.name.ToLowerInvariant();
                var hasAgeToken = HasAgeToken(lowerName);
                var hasGenderToken = HasGenderToken(lowerName);
                if (!hasAgeToken && !hasGenderToken)
                {
                    continue;
                }

                foundTaggedVisual = true;
                visualObject.SetActive(MatchesAge(lowerName, age) && MatchesGender(lowerName, gender));
            }

            if (!foundTaggedVisual)
            {
                TintFallbackCustomer(customer, age, gender);
            }
        }

        private static bool HasAgeToken(string lowerName)
        {
            return lowerName.Contains("young")
                || lowerName.Contains("youth")
                || lowerName.Contains("middle")
                || lowerName.Contains("elder")
                || lowerName.Contains("elderly");
        }

        private static bool HasGenderToken(string lowerName)
        {
            return lowerName.Contains("male") || lowerName.Contains("female");
        }

        private static bool MatchesAge(string lowerName, CustomerAgeGroup age)
        {
            if (!HasAgeToken(lowerName))
            {
                return true;
            }

            return age switch
            {
                CustomerAgeGroup.Youth => lowerName.Contains("young") || lowerName.Contains("youth"),
                CustomerAgeGroup.Middle => lowerName.Contains("middle"),
                CustomerAgeGroup.Elderly => lowerName.Contains("elder") || lowerName.Contains("elderly"),
                _ => true
            };
        }

        private static bool MatchesGender(string lowerName, CustomerGender gender)
        {
            if (!HasGenderToken(lowerName))
            {
                return true;
            }

            return gender == CustomerGender.Female
                ? lowerName.Contains("female")
                : lowerName.Contains("male") && !lowerName.Contains("female");
        }

        private static void TintFallbackCustomer(GameObject customer, CustomerAgeGroup age, CustomerGender gender)
        {
            var renderer = customer.GetComponentInChildren<Renderer>();
            if (renderer == null)
            {
                return;
            }

            var hue = ((int)age * 0.19f + (int)gender * 0.31f + Random.Range(0f, 0.12f)) % 1f;
            var material = new Material(Shader.Find("Standard"));
            material.color = Color.HSVToRGB(hue, 0.45f, 0.88f);
            renderer.sharedMaterial = material;
        }

        private bool IsCriticalTime()
        {
            return TimeManager.Instance != null
                && TimeManager.Instance.RemainingSeconds < criticalTimeThresholdSeconds;
        }

        private void ResetSpawnTimer()
        {
            var minCooldown = Mathf.Min(spawnCooldown.x, spawnCooldown.y);
            var maxCooldown = Mathf.Max(spawnCooldown.x, spawnCooldown.y);
            var cooldown = Random.Range(minCooldown, maxCooldown);

            if (IsCriticalTime())
            {
                cooldown *= criticalCooldownMultiplier;
            }

            spawnTimer = Mathf.Max(0.5f, cooldown);
        }

        private Vector3 GetSpawnPosition()
        {
            return spawnPoint != null ? spawnPoint.position : transform.position;
        }

        private Quaternion GetSpawnRotation()
        {
            return spawnPoint != null ? spawnPoint.rotation : transform.rotation;
        }
    }
}
