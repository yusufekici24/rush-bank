using RushBank.Core;
using UnityEngine;
using UnityEngine.Events;

namespace RushBank.Gameplay
{
    public enum ActionType
    {
        Withdraw,
        Deposit,
        CurrencyExchange
    }

    public class BankingActionSystem : MonoBehaviour
    {
        private enum ActionStep
        {
            None,
            NeedCounterPickup,
            NeedVaultPickup,
            NeedVaultDeposit,
            NeedCounterDelivery
        }

        [Header("Quest Source")]
        [SerializeField] private QueueManager queueManager;
        [SerializeField] private CashDeliverySystem cashDeliverySystem;
        [SerializeField] private ActionType activeActionType = ActionType.Withdraw;
        [SerializeField] private bool autoDetectCustomerRequest = true;

        [Header("Stations")]
        [SerializeField] private string vaultTag = "CashRegister";
        [SerializeField] private string counterTag = "Counter";

        [Header("Carry")]
        [SerializeField] private Transform holdPoint;
        [SerializeField] private GameObject moneyPrefab;
        [SerializeField] private GameObject folderPrefab;
        [SerializeField] private string carryableTag = "Interactable";

        [Header("Rewards")]
        [SerializeField, Min(0f)] private float bonusTime = 7f;
        [SerializeField, Min(0)] private int points = 100;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private string countingMoneyTrigger = "CountingMoney";
        [SerializeField] private string handingOverTrigger = "HandingOver";
        [SerializeField] private string grabTrigger = "Grab";
        [SerializeField] private string depositTrigger = "Deposit";

        public UnityEvent<ActionType> OnActionStarted = new UnityEvent<ActionType>();
        public UnityEvent<ActionType> OnActionCompleted = new UnityEvent<ActionType>();
        public UnityEvent<ActionType> OnActionFailed = new UnityEvent<ActionType>();

        private static readonly Vector3 HeldItemLocalPosition = Vector3.zero;
        private static readonly Quaternion HeldItemLocalRotation = Quaternion.identity;

        private ActionStep currentStep;
        private GameObject nearbyStation;
        private GameObject heldItem;
        private QueueCustomer detectedCustomer;
        private int countingMoneyHash;
        private int handingOverHash;
        private int grabHash;
        private int depositHash;

        public ActionType ActiveActionType => activeActionType;
        public float BonusTime => bonusTime;
        public bool IsHoldingItem => heldItem != null;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            countingMoneyHash = Animator.StringToHash(countingMoneyTrigger);
            handingOverHash = Animator.StringToHash(handingOverTrigger);
            grabHash = Animator.StringToHash(grabTrigger);
            depositHash = Animator.StringToHash(depositTrigger);
        }

        private void Start()
        {
            if (cashDeliverySystem == null)
            {
                cashDeliverySystem = FindFirstObjectByType<CashDeliverySystem>();
            }

            RefreshActiveActionFromCustomer();
        }

        private void Update()
        {
            if (!autoDetectCustomerRequest || queueManager == null)
            {
                return;
            }

            if (queueManager.ActiveCustomer != detectedCustomer)
            {
                RefreshActiveActionFromCustomer();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsBankingStation(other))
            {
                nearbyStation = other.gameObject;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (nearbyStation == other.gameObject)
            {
                nearbyStation = null;
            }
        }

        public void RefreshActiveActionFromCustomer()
        {
            if (!autoDetectCustomerRequest || queueManager == null)
            {
                detectedCustomer = null;
                StartAction(activeActionType);
                return;
            }

            if (queueManager.ActiveCustomer == null)
            {
                detectedCustomer = null;
                currentStep = ActionStep.None;
                return;
            }

            detectedCustomer = queueManager.ActiveCustomer;
            StartAction(MapRequestToAction(queueManager.ActiveCustomer.RequestKind));
        }

        public void StartAction(ActionType actionType)
        {
            activeActionType = actionType;
            currentStep = actionType switch
            {
                ActionType.Withdraw => ActionStep.NeedVaultPickup,
                ActionType.Deposit => ActionStep.NeedCounterPickup,
                ActionType.CurrencyExchange => ActionStep.NeedCounterPickup,
                _ => ActionStep.None
            };

            OnActionStarted.Invoke(activeActionType);
        }

        public void Grab()
        {
            if (holdPoint == null || heldItem != null)
            {
                FailAction();
                return;
            }

            if (currentStep == ActionStep.NeedVaultPickup && IsAtVault())
            {
                if (cashDeliverySystem != null && !cashDeliverySystem.CanWithdrawCash)
                {
                    cashDeliverySystem.NotifyCashEmpty();
                    FailAction();
                    return;
                }

                PlayTrigger(countingMoneyHash);
                heldItem = CreateHeldItem(moneyPrefab, "withdraw_money");
                currentStep = ActionStep.NeedCounterDelivery;
                PlayTrigger(grabHash);
                return;
            }

            if (currentStep == ActionStep.NeedCounterPickup && IsAtCounter())
            {
                PlayTrigger(handingOverHash);
                heldItem = CreateHeldItem(GetCounterPickupPrefab(), GetCounterPickupId());
                currentStep = ActionStep.NeedVaultDeposit;
                PlayTrigger(grabHash);
                return;
            }

            FailAction();
        }

        public void Deliver()
        {
            if (currentStep != ActionStep.NeedCounterDelivery || !IsAtCounter() || heldItem == null)
            {
                FailAction();
                return;
            }

            if (activeActionType == ActionType.Withdraw
                && cashDeliverySystem != null
                && !cashDeliverySystem.TryConsumeCashForWithdrawal())
            {
                FailAction();
                return;
            }

            PlayTrigger(handingOverHash);
            DestroyHeldItem();
            CompleteAction();
        }

        public void Deposit()
        {
            if (currentStep != ActionStep.NeedVaultDeposit || !IsAtVault() || heldItem == null)
            {
                FailAction();
                return;
            }

            PlayTrigger(depositHash);
            DestroyHeldItem();
            if (activeActionType == ActionType.Deposit)
            {
                cashDeliverySystem?.AddCashFromDeposit();
            }

            CompleteAction();
        }

        public void Action()
        {
            if (currentStep == ActionStep.NeedVaultPickup || currentStep == ActionStep.NeedCounterPickup)
            {
                Grab();
                return;
            }

            if (currentStep == ActionStep.NeedCounterDelivery)
            {
                Deliver();
                return;
            }

            if (currentStep == ActionStep.NeedVaultDeposit)
            {
                Deposit();
            }
        }

        public void CancelAction()
        {
            DestroyHeldItem();
            currentStep = ActionStep.None;
            detectedCustomer = null;
            OnActionFailed.Invoke(activeActionType);
        }

        private void CompleteAction()
        {
            currentStep = ActionStep.None;

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.AddTime(bonusTime);
            }

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(points);
            }

            queueManager?.CompleteActiveCustomer();
            OnActionCompleted.Invoke(activeActionType);
        }

        private void FailAction()
        {
            OnActionFailed.Invoke(activeActionType);
        }

        private GameObject CreateHeldItem(GameObject prefab, string itemId)
        {
            GameObject item;
            if (prefab != null)
            {
                item = Instantiate(prefab, holdPoint);
            }
            else
            {
                item = GameObject.CreatePrimitive(PrimitiveType.Cube);
                item.transform.SetParent(holdPoint, false);
                item.transform.localScale = new Vector3(0.45f, 0.18f, 0.3f);
            }

            item.name = itemId;
            TrySetTag(item, carryableTag);
            item.transform.localPosition = HeldItemLocalPosition;
            item.transform.localRotation = HeldItemLocalRotation;

            if (!item.TryGetComponent<DeliverableItem>(out var deliverable))
            {
                deliverable = item.AddComponent<DeliverableItem>();
            }

            deliverable.Configure(itemId, Color.white);
            SetPhysicsForHeldItem(item);
            return item;
        }

        private GameObject GetCounterPickupPrefab()
        {
            return activeActionType == ActionType.CurrencyExchange ? folderPrefab : moneyPrefab;
        }

        private string GetCounterPickupId()
        {
            return activeActionType == ActionType.CurrencyExchange ? "currency_exchange_folder" : "deposit_money";
        }

        private void DestroyHeldItem()
        {
            if (heldItem == null)
            {
                return;
            }

            var itemToDestroy = heldItem;
            heldItem = null;
            Destroy(itemToDestroy);
        }

        private bool IsAtVault()
        {
            return nearbyStation != null && nearbyStation.tag == vaultTag;
        }

        private bool IsAtCounter()
        {
            return nearbyStation != null && nearbyStation.tag == counterTag;
        }

        private bool IsBankingStation(Collider other)
        {
            var objectTag = other.gameObject.tag;
            return objectTag == vaultTag || objectTag == counterTag;
        }

        private ActionType MapRequestToAction(CustomerRequestKind requestKind)
        {
            return requestKind switch
            {
                CustomerRequestKind.Withdraw => ActionType.Withdraw,
                CustomerRequestKind.CashWithdrawDeposit => ActionType.Withdraw,
                CustomerRequestKind.Deposit => ActionType.Deposit,
                CustomerRequestKind.CurrencyExchange => ActionType.CurrencyExchange,
                _ => ActionType.CurrencyExchange
            };
        }

        private void PlayTrigger(int triggerHash)
        {
            if (animator != null && triggerHash != 0)
            {
                animator.SetTrigger(triggerHash);
            }
        }

        private static void SetPhysicsForHeldItem(GameObject item)
        {
            if (item.TryGetComponent<Rigidbody>(out var body))
            {
                body.isKinematic = true;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            var colliders = item.GetComponentsInChildren<Collider>();
            for (var i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }
        }

        private static void TrySetTag(GameObject target, string tagName)
        {
            if (string.IsNullOrEmpty(tagName))
            {
                return;
            }

            try
            {
                target.tag = tagName;
            }
            catch (UnityException)
            {
                // The prototype setup creates required tags, but manual scenes may not have them yet.
            }
        }

    }
}
