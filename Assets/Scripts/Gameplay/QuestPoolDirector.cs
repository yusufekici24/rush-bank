using System.Collections.Generic;
using RushBank.Core;
using UnityEngine;
using UnityEngine.Events;

namespace RushBank.Gameplay
{
    public class QuestPoolDirector : MonoBehaviour
    {
        [System.Serializable]
        public class QuestPoolEntry
        {
            public CustomerRequestKind requestKind;
            [Min(0f)] public float dayOneWeight;
            [Min(0f)] public float dayFiveWeight;
            public bool quickWin;
            [Min(1)] public int unlockDay = 1;
        }

        [Header("References")]
        [SerializeField] private QueueManager queueManager;
        [SerializeField] private ThiefEventSystem thiefEventSystem;

        [Header("Day and Pool")]
        [SerializeField, Min(1)] private int currentDay = 1;
        [SerializeField, Min(1)] private int chaosDay = 5;
        [SerializeField] private List<QuestPoolEntry> questPool = new List<QuestPoolEntry>
        {
            new QuestPoolEntry { requestKind = CustomerRequestKind.PassbookPrinting, dayOneWeight = 60f, dayFiveWeight = 14f, quickWin = true, unlockDay = 1 },
            new QuestPoolEntry { requestKind = CustomerRequestKind.CashWithdrawDeposit, dayOneWeight = 40f, dayFiveWeight = 16f, quickWin = false, unlockDay = 1 },
            new QuestPoolEntry { requestKind = CustomerRequestKind.BillPayment, dayOneWeight = 0f, dayFiveWeight = 14f, quickWin = true, unlockDay = 2 },
            new QuestPoolEntry { requestKind = CustomerRequestKind.CardBlockRemoval, dayOneWeight = 0f, dayFiveWeight = 10f, quickWin = true, unlockDay = 3 },
            new QuestPoolEntry { requestKind = CustomerRequestKind.CurrencyExchange, dayOneWeight = 0f, dayFiveWeight = 12f, quickWin = false, unlockDay = 3 },
            new QuestPoolEntry { requestKind = CustomerRequestKind.GoldExchange, dayOneWeight = 0f, dayFiveWeight = 12f, quickWin = false, unlockDay = 5 },
            new QuestPoolEntry { requestKind = CustomerRequestKind.CreditApproval, dayOneWeight = 0f, dayFiveWeight = 10f, quickWin = false, unlockDay = 4 },
            new QuestPoolEntry { requestKind = CustomerRequestKind.VipSafeRental, dayOneWeight = 0f, dayFiveWeight = 7f, quickWin = false, unlockDay = 5 },
            new QuestPoolEntry { requestKind = CustomerRequestKind.Thief, dayOneWeight = 0f, dayFiveWeight = 5f, quickWin = false, unlockDay = 5 }
        };

        [Header("Dynamic Pacing")]
        [SerializeField, Min(1)] private int maxQueueSize = 5;
        [SerializeField, Min(0.1f)] private float baseArrivalIntervalSeconds = 6f;
        [SerializeField, Min(0f)] private float arrivalIntervalVarianceSeconds = 1.25f;
        [SerializeField, Min(0f)] private float criticalTimeThresholdSeconds = 15f;
        [SerializeField, Min(1f)] private float criticalQuickWinWeightMultiplier = 3f;

        public UnityEvent<CustomerRequestKind> OnQuestSpawned = new UnityEvent<CustomerRequestKind>();
        public UnityEvent OnQueueFullSpawnPaused = new UnityEvent();
        public UnityEvent OnThiefEventSpawnPaused = new UnityEvent();

        private float nextSpawnTimer;

        public int CurrentDay => currentDay;
        public int MaxQueueSize => maxQueueSize;

        private void Awake()
        {
            if (queueManager == null)
            {
                queueManager = QueueManager.Instance;
            }
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

            nextSpawnTimer -= Time.deltaTime;
            if (nextSpawnTimer > 0f)
            {
                return;
            }

            if (queueManager.QueueCount >= maxQueueSize)
            {
                OnQueueFullSpawnPaused.Invoke();
                return;
            }

            if (IsThiefEventActive())
            {
                OnThiefEventSpawnPaused.Invoke();
                return;
            }

            SpawnNextQuestCustomer();
            ResetSpawnTimer();
        }

        public void SetCurrentDay(int day)
        {
            currentDay = Mathf.Max(1, day);
        }

        public void SpawnNextQuestCustomer()
        {
            var requestKind = PickRequestKind();
            if (requestKind == CustomerRequestKind.Thief && thiefEventSystem != null)
            {
                thiefEventSystem.SpawnThief();
                OnQuestSpawned.Invoke(requestKind);
                return;
            }

            var ageGroup = (CustomerAgeGroup)Random.Range(0, 3);
            var gender = (CustomerGender)Random.Range(0, 2);
            queueManager.SpawnCustomer(requestKind, ageGroup, gender);
            OnQuestSpawned.Invoke(requestKind);
        }

        private CustomerRequestKind PickRequestKind()
        {
            var totalWeight = 0f;
            for (var i = 0; i < questPool.Count; i++)
            {
                totalWeight += GetEffectiveWeight(questPool[i]);
            }

            if (totalWeight <= 0f)
            {
                return CustomerRequestKind.PassbookPrinting;
            }

            var roll = Random.Range(0f, totalWeight);
            for (var i = 0; i < questPool.Count; i++)
            {
                roll -= GetEffectiveWeight(questPool[i]);
                if (roll <= 0f)
                {
                    return questPool[i].requestKind;
                }
            }

            return questPool[questPool.Count - 1].requestKind;
        }

        private float GetEffectiveWeight(QuestPoolEntry entry)
        {
            if (entry == null || currentDay < entry.unlockDay)
            {
                return 0f;
            }

            var chaosProgress = chaosDay <= 1 ? 1f : Mathf.InverseLerp(1f, chaosDay, currentDay);
            var weight = Mathf.Lerp(entry.dayOneWeight, entry.dayFiveWeight, chaosProgress);

            if (IsCriticalTime() && entry.quickWin)
            {
                weight *= criticalQuickWinWeightMultiplier;
            }

            return Mathf.Max(0f, weight);
        }

        private bool IsCriticalTime()
        {
            return TimeManager.Instance != null
                && TimeManager.Instance.RemainingSeconds <= criticalTimeThresholdSeconds;
        }

        private bool IsThiefEventActive()
        {
            return thiefEventSystem != null && thiefEventSystem.IsEventActive;
        }

        private void ResetSpawnTimer()
        {
            var variance = arrivalIntervalVarianceSeconds <= 0f
                ? 0f
                : Random.Range(-arrivalIntervalVarianceSeconds, arrivalIntervalVarianceSeconds);

            nextSpawnTimer = Mathf.Max(0.5f, baseArrivalIntervalSeconds + variance);
        }
    }
}
