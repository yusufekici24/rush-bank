using UnityEngine;
using UnityEngine.Events;

namespace RushBank.Gameplay
{
    public enum CustomerMoodState
    {
        Calm,
        Grumpy,
        Raging
    }

    public class CustomerPatience : MonoBehaviour
    {
        [SerializeField, Range(0f, 100f)] private float patience = 100f;
        [SerializeField, Min(0f)] private float patienceDrainPerSecond = 4f;
        [SerializeField] private Animator animator;
        [SerializeField] private GameObject grumpyOverlay;
        [SerializeField] private GameObject ragingOverlay;

        [Header("Age Psychology")]
        [SerializeField] private bool readAgeFromQueueCustomer = true;
        [SerializeField] private CustomerAgeGroup ageGroup = CustomerAgeGroup.Middle;
        [SerializeField] private float youthDrainMultiplier = 0.7f;
        [SerializeField] private float middleDrainMultiplier = 1f;
        [SerializeField] private float elderlyDrainMultiplier = 1.5f;
        [SerializeField, Min(0f)] private float vipDrainMultiplier = 2f;

        [Header("Animation Triggers")]
        [SerializeField] private string grumpyTrigger = "Grumpy";
        [SerializeField] private string ragingTrigger = "Raging";
        [SerializeField] private string calmTrigger = "Calm";

        public UnityEvent<CustomerMoodState> OnMoodChanged = new UnityEvent<CustomerMoodState>();
        public UnityEvent<float> OnPatienceChanged = new UnityEvent<float>();

        private int grumpyHash;
        private int ragingHash;
        private int calmHash;
        private CustomerMoodState moodState = CustomerMoodState.Calm;
        private float drainMultiplier = 1f;
        private QueueCustomer queueCustomer;
        private static float globalPatienceDrainMultiplier = 1f;

        public float Patience => patience;
        public float Patience01 => Mathf.Clamp01(patience / 100f);
        public CustomerMoodState MoodState => moodState;
        public float DrainMultiplier => drainMultiplier;
        public float AgeDrainMultiplier => GetAgeDrainMultiplier(ResolvedAgeGroup);
        public float RequestDrainMultiplier => queueCustomer != null && queueCustomer.IsVipRequest ? vipDrainMultiplier : 1f;
        public float EffectiveDrainMultiplier => drainMultiplier * AgeDrainMultiplier * RequestDrainMultiplier * GlobalPatienceDrainMultiplier;
        public static float GlobalPatienceDrainMultiplier
        {
            get => globalPatienceDrainMultiplier;
            set => globalPatienceDrainMultiplier = Mathf.Max(0f, value);
        }
        public CustomerAgeGroup ResolvedAgeGroup => readAgeFromQueueCustomer && queueCustomer != null
            ? queueCustomer.AgeGroup
            : ageGroup;

        private void Awake()
        {
            queueCustomer = GetComponent<QueueCustomer>();

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            grumpyHash = Animator.StringToHash(grumpyTrigger);
            ragingHash = Animator.StringToHash(ragingTrigger);
            calmHash = Animator.StringToHash(calmTrigger);
            ApplyMoodVisuals();
        }

        private void Update()
        {
            if (patience <= 0f)
            {
                return;
            }

            SetPatience(patience - patienceDrainPerSecond * EffectiveDrainMultiplier * Time.deltaTime);
        }

        public void RestorePatience(float amount)
        {
            SetPatience(patience + amount);
            if (patience >= 50f)
            {
                SetMood(CustomerMoodState.Calm);
            }
        }

        public void SetDrainMultiplier(float multiplier)
        {
            drainMultiplier = Mathf.Max(0f, multiplier);
        }

        public void ResetDrainMultiplier()
        {
            drainMultiplier = 1f;
        }

        public void SetAgeGroup(CustomerAgeGroup customerAgeGroup)
        {
            ageGroup = customerAgeGroup;
            readAgeFromQueueCustomer = false;
        }

        public void SetPatience(float value)
        {
            var previousPatience = patience;
            patience = Mathf.Clamp(value, 0f, 100f);

            if (!Mathf.Approximately(previousPatience, patience))
            {
                OnPatienceChanged.Invoke(patience);
            }

            if (patience < 20f)
            {
                SetMood(CustomerMoodState.Raging);
            }
            else if (patience < 50f)
            {
                SetMood(CustomerMoodState.Grumpy);
            }
            else
            {
                SetMood(CustomerMoodState.Calm);
            }
        }

        private void SetMood(CustomerMoodState newMood)
        {
            if (moodState == newMood)
            {
                return;
            }

            moodState = newMood;
            ApplyMoodVisuals();
            PlayMoodAnimation();
            OnMoodChanged.Invoke(moodState);
        }

        private void ApplyMoodVisuals()
        {
            if (grumpyOverlay != null)
            {
                grumpyOverlay.SetActive(moodState == CustomerMoodState.Grumpy);
            }

            if (ragingOverlay != null)
            {
                ragingOverlay.SetActive(moodState == CustomerMoodState.Raging);
            }
        }

        private void PlayMoodAnimation()
        {
            if (animator == null)
            {
                return;
            }

            var trigger = moodState switch
            {
                CustomerMoodState.Grumpy => grumpyHash,
                CustomerMoodState.Raging => ragingHash,
                _ => calmHash
            };

            if (trigger != 0)
            {
                animator.SetTrigger(trigger);
            }
        }

        private float GetAgeDrainMultiplier(CustomerAgeGroup customerAgeGroup)
        {
            return customerAgeGroup switch
            {
                CustomerAgeGroup.Youth => youthDrainMultiplier,
                CustomerAgeGroup.Elderly => elderlyDrainMultiplier,
                _ => middleDrainMultiplier
            };
        }
    }
}
