using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RushBank.Gameplay
{
    public class AssistantManager : MonoBehaviour
    {
        [Header("Queue Tracking")]
        [SerializeField] private QueueManager queueManager;
        [SerializeField, Min(0f)] private float fillPerSecondAtFullGrumpiness = 0.28f;
        [SerializeField, Min(0f)] private float minimumFillPerSecondWhenGrumpy = 0.03f;
        [SerializeField, Range(0f, 1f)] private float grumpyWeight = 0.6f;
        [SerializeField, Range(0f, 1f)] private float ragingWeight = 1f;

        [Header("Assistant")]
        [SerializeField] private LazyAssistantAI lazyAssistant;

        [Header("UI")]
        [SerializeField] private Slider summonBar;
        [SerializeField] private Image summonBarFill;
        [SerializeField] private Button summonHelperButton;

        public UnityEvent OnSummonReady = new UnityEvent();
        public UnityEvent OnAssistantSummoned = new UnityEvent();

        private float summonProgress;
        private bool accumulationEnabled = true;
        private bool readyEventRaised;

        public float SummonProgress01 => summonProgress;
        public float CurrentGrumpiness01 { get; private set; }
        public bool CanSummon => accumulationEnabled && summonProgress >= 1f;

        private void Awake()
        {
            if (queueManager == null)
            {
                queueManager = QueueManager.Instance;
            }

            if (lazyAssistant == null)
            {
                lazyAssistant = LazyAssistantAI.Instance;
            }
        }

        private void OnEnable()
        {
            if (summonHelperButton != null)
            {
                summonHelperButton.onClick.AddListener(SummonHelper);
            }

            SubscribeAssistantLeave();
            RefreshUI();
        }

        private void OnDisable()
        {
            if (summonHelperButton != null)
            {
                summonHelperButton.onClick.RemoveListener(SummonHelper);
            }

            UnsubscribeAssistantLeave();
        }

        private void Update()
        {
            if (queueManager == null)
            {
                queueManager = QueueManager.Instance;
            }

            if (lazyAssistant == null)
            {
                lazyAssistant = LazyAssistantAI.Instance;
                SubscribeAssistantLeave();
            }

            CurrentGrumpiness01 = CalculateQueueGrumpiness();

            if (accumulationEnabled && summonProgress < 1f && CurrentGrumpiness01 > 0f)
            {
                var fillSpeed = Mathf.Max(
                    minimumFillPerSecondWhenGrumpy,
                    CurrentGrumpiness01 * fillPerSecondAtFullGrumpiness);
                summonProgress = Mathf.Min(1f, summonProgress + fillSpeed * Time.deltaTime);
            }

            if (!readyEventRaised && summonProgress >= 1f)
            {
                readyEventRaised = true;
                OnSummonReady.Invoke();
            }

            RefreshUI();
        }

        public void SummonHelper()
        {
            if (!CanSummon)
            {
                return;
            }

            if (lazyAssistant == null)
            {
                lazyAssistant = LazyAssistantAI.Instance;
            }

            if (lazyAssistant == null)
            {
                return;
            }

            summonProgress = 0f;
            readyEventRaised = false;
            accumulationEnabled = false;
            RefreshUI();

            lazyAssistant.ActivateAssistant();
            OnAssistantSummoned.Invoke();
        }

        public void ResumeAccumulation()
        {
            accumulationEnabled = true;
            readyEventRaised = summonProgress >= 1f;
            RefreshUI();
        }

        public void ResetSummonBar()
        {
            summonProgress = 0f;
            readyEventRaised = false;
            RefreshUI();
        }

        private float CalculateQueueGrumpiness()
        {
            var manager = queueManager != null ? queueManager : QueueManager.Instance;
            if (manager == null || manager.CustomerQueue.Count == 0)
            {
                return 0f;
            }

            var totalWeight = 0f;
            var trackedCustomers = 0;

            for (var i = 0; i < manager.CustomerQueue.Count; i++)
            {
                var customer = manager.CustomerQueue[i];
                if (customer == null || !customer.TryGetComponent<CustomerPatience>(out var patience))
                {
                    continue;
                }

                trackedCustomers++;
                totalWeight += patience.MoodState switch
                {
                    CustomerMoodState.Raging => ragingWeight,
                    CustomerMoodState.Grumpy => grumpyWeight,
                    _ => 0f
                };
            }

            return trackedCustomers == 0 ? 0f : Mathf.Clamp01(totalWeight / trackedCustomers);
        }

        private void SubscribeAssistantLeave()
        {
            if (lazyAssistant != null)
            {
                lazyAssistant.OnAssistantLeave.RemoveListener(ResumeAccumulation);
                lazyAssistant.OnAssistantLeave.AddListener(ResumeAccumulation);
            }
        }

        private void UnsubscribeAssistantLeave()
        {
            if (lazyAssistant != null)
            {
                lazyAssistant.OnAssistantLeave.RemoveListener(ResumeAccumulation);
            }
        }

        private void RefreshUI()
        {
            if (summonBar != null)
            {
                summonBar.normalizedValue = summonProgress;
            }

            if (summonBarFill != null)
            {
                summonBarFill.fillAmount = summonProgress;
            }

            if (summonHelperButton != null)
            {
                summonHelperButton.interactable = CanSummon;
            }
        }
    }
}
