using System.Collections;
using RushBank.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RushBank.Gameplay
{
    public enum CashDeliveryState
    {
        Ready,
        CashEmpty,
        VehicleArriving,
        AwaitingBagPickup,
        CarryingCashBag,
        RestockingVault
    }

    public class CashDeliverySystem : MonoBehaviour
    {
        private const string CashDeliveryBagId = "cash_delivery_bag";

        [Header("Cash Stock")]
        [SerializeField, Min(1)] private int maxVaultCash = 5;
        [SerializeField, Min(0)] private int currentVaultCash = 5;

        [Header("References")]
        [SerializeField] private BankingActionSystem bankingActionSystem;
        [SerializeField] private PlayerInteraction playerInteraction;
        [SerializeField] private ChubbyTopDownInputController topDownController;
        [SerializeField] private MobilePlayerController mobilePlayerController;
        [SerializeField] private HeistRaidSystem heistRaidSystem;
        [SerializeField] private Transform player;
        [SerializeField] private Collider vaultRestockZone;
        [SerializeField] private GameObject noCashWarningIcon;
        [SerializeField] private Button requestCashButton;
        [SerializeField] private GameObject requestCashButtonRoot;

        [Header("Delivery Vehicle")]
        [SerializeField] private GameObject armoredVehiclePrefab;
        [SerializeField] private GameObject cashBagPrefab;
        [SerializeField] private Transform vehicleSpawnPoint;
        [SerializeField] private Transform vehicleParkingPoint;
        [SerializeField] private Transform vehicleExitPoint;
        [SerializeField, Min(0.1f)] private float vehicleMoveSpeed = 2.4f;
        [SerializeField, Range(0.1f, 1f)] private float heavyBagSpeedMultiplier = 0.8f;

        [Header("Rewards")]
        [SerializeField, Min(1f)] private float emergencyBonusMultiplier = 1.5f;
        [SerializeField, Min(0f)] private float fallbackBonusTime = 8f;
        [SerializeField] private ParticleSystem vaultCashExplosionEffect;

        [Header("Transaction Pause")]
        [SerializeField] private Behaviour[] pausableCounterSystems;

        public IntEvent OnCashStockChanged = new IntEvent();
        public BoolEvent OnRequestButtonVisibilityChanged = new BoolEvent();
        public UnityEvent<CashDeliveryState> OnStateChanged = new UnityEvent<CashDeliveryState>();
        public UnityEvent OnCashDeliveryStarted = new UnityEvent();
        public UnityEvent OnCashDeliveryCompleted = new UnityEvent();

        private CashDeliveryState state = CashDeliveryState.Ready;
        private GameObject activeVehicle;
        private GameObject activeCashBag;
        private bool[] pausablePreviousStates = System.Array.Empty<bool>();
        private bool counterSystemsPaused;
        private Coroutine deliveryRoutine;
        private bool heavyBagSlowApplied;

        public CashDeliveryState State => state;
        public int CurrentVaultCash => currentVaultCash;
        public int MaxVaultCash => maxVaultCash;
        public float CashDeliveryBonusTime => GetEmergencyBonusTime();
        public bool IsCashEmpty => currentVaultCash <= 0;
        public bool CanWithdrawCash => currentVaultCash > 0;
        public bool IsDeliveryActive => state is CashDeliveryState.VehicleArriving
            or CashDeliveryState.AwaitingBagPickup
            or CashDeliveryState.CarryingCashBag
            or CashDeliveryState.RestockingVault;

        private void Awake()
        {
            currentVaultCash = Mathf.Clamp(currentVaultCash, 0, maxVaultCash);

            if (bankingActionSystem == null)
            {
                bankingActionSystem = FindFirstObjectByType<BankingActionSystem>();
            }

            if (playerInteraction == null)
            {
                playerInteraction = FindFirstObjectByType<PlayerInteraction>();
            }

            if (player == null && playerInteraction != null)
            {
                player = playerInteraction.transform;
            }

            if (topDownController == null && player != null)
            {
                topDownController = player.GetComponent<ChubbyTopDownInputController>();
            }

            if (mobilePlayerController == null && player != null)
            {
                mobilePlayerController = player.GetComponent<MobilePlayerController>();
            }

            if (heistRaidSystem == null)
            {
                heistRaidSystem = FindFirstObjectByType<HeistRaidSystem>();
            }

            if (requestCashButtonRoot == null && requestCashButton != null)
            {
                requestCashButtonRoot = requestCashButton.gameObject;
            }

            if (requestCashButton != null)
            {
                requestCashButton.onClick.AddListener(RequestCashDelivery);
            }

            RefreshStateFromStock();
            UpdateRequestButton();
        }

        private void OnDestroy()
        {
            if (requestCashButton != null)
            {
                requestCashButton.onClick.RemoveListener(RequestCashDelivery);
            }

            ApplyHeavyBagSlow(false);
        }

        private void Update()
        {
            if (state == CashDeliveryState.AwaitingBagPickup && IsPlayerHoldingCashDeliveryBag())
            {
                SetState(CashDeliveryState.CarryingCashBag);
                ApplyHeavyBagSlow(true);
            }

            if (state == CashDeliveryState.CarryingCashBag && !IsPlayerHoldingCashDeliveryBag())
            {
                SetState(CashDeliveryState.AwaitingBagPickup);
                ApplyHeavyBagSlow(false);
            }

            if (!IsHeistRaidBlockingNormalRestock()
                && (state == CashDeliveryState.AwaitingBagPickup || state == CashDeliveryState.CarryingCashBag)
                && IsCashBagDeliveredToVault())
            {
                CompleteRestock();
            }
        }

        public bool TryConsumeCashForWithdrawal()
        {
            if (currentVaultCash <= 0)
            {
                NotifyCashEmpty();
                return false;
            }

            currentVaultCash--;
            OnCashStockChanged.Invoke(currentVaultCash);

            if (currentVaultCash <= 0)
            {
                NotifyCashEmpty();
            }
            else
            {
                UpdateRequestButton();
            }

            return true;
        }

        public void AddCashFromDeposit(int amount = 1)
        {
            if (amount <= 0 || currentVaultCash >= maxVaultCash)
            {
                return;
            }

            currentVaultCash = Mathf.Min(maxVaultCash, currentVaultCash + amount);
            OnCashStockChanged.Invoke(currentVaultCash);

            if (!IsDeliveryActive && currentVaultCash > 0)
            {
                SetState(CashDeliveryState.Ready);
            }

            UpdateRequestButton();
            UpdateNoCashWarning();
        }

        public void NotifyCashEmpty()
        {
            if (!IsDeliveryActive)
            {
                SetState(CashDeliveryState.CashEmpty);
            }

            UpdateRequestButton();
        }

        public void RefillCashStock()
        {
            currentVaultCash = maxVaultCash;
            OnCashStockChanged.Invoke(currentVaultCash);
            SetState(CashDeliveryState.Ready);
            UpdateRequestButton();
            UpdateNoCashWarning();
        }

        public void RequestCashDelivery()
        {
            if (state != CashDeliveryState.CashEmpty || deliveryRoutine != null)
            {
                return;
            }

            deliveryRoutine = StartCoroutine(RunCashDelivery());
        }

        public bool CompleteRestockFromHeist(float rewardMultiplier)
        {
            if (state != CashDeliveryState.AwaitingBagPickup && state != CashDeliveryState.CarryingCashBag)
            {
                return false;
            }

            SetState(CashDeliveryState.RestockingVault);
            ApplyHeavyBagSlow(false);

            if (playerInteraction != null && playerInteraction.HeldObject != null)
            {
                playerInteraction.TryDestroyHeldObject();
            }
            else if (activeCashBag != null)
            {
                Destroy(activeCashBag);
            }

            activeCashBag = null;
            RefillCashStock();

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.AddTime(GetEmergencyBonusTime() * Mathf.Max(0f, rewardMultiplier));
            }

            if (vaultCashExplosionEffect != null)
            {
                vaultCashExplosionEffect.Play();
            }

            SetCounterSystemsPaused(false);
            OnCashDeliveryCompleted.Invoke();

            if (deliveryRoutine != null)
            {
                StopCoroutine(deliveryRoutine);
            }

            deliveryRoutine = StartCoroutine(SendVehicleAway());
            return true;
        }

        private IEnumerator RunCashDelivery()
        {
            OnCashDeliveryStarted.Invoke();
            SetCounterSystemsPaused(true);

            SetState(CashDeliveryState.VehicleArriving);
            UpdateRequestButton();
            activeVehicle = SpawnVehicle();
            yield return MoveObjectTo(activeVehicle.transform, GetVehicleParkingPosition());

            activeCashBag = SpawnCashBag();
            SetState(CashDeliveryState.AwaitingBagPickup);
            deliveryRoutine = null;
        }

        private GameObject SpawnVehicle()
        {
            var spawnPosition = vehicleSpawnPoint != null ? vehicleSpawnPoint.position : transform.position;
            if (armoredVehiclePrefab != null)
            {
                return Instantiate(armoredVehiclePrefab, spawnPosition, Quaternion.identity);
            }

            var vehicle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vehicle.name = "White Armored Van Prototype";
            vehicle.transform.position = spawnPosition;
            vehicle.transform.localScale = new Vector3(1.6f, 1.05f, 1.05f);

            var renderer = vehicle.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.93f, 0.96f, 0.98f);
                renderer.sharedMaterial = material;
            }

            CreateVehicleWheel(vehicle.transform, new Vector3(-0.48f, -0.62f, -0.42f));
            CreateVehicleWheel(vehicle.transform, new Vector3(0.48f, -0.62f, -0.42f));
            CreateVehicleWheel(vehicle.transform, new Vector3(-0.48f, -0.62f, 0.42f));
            CreateVehicleWheel(vehicle.transform, new Vector3(0.48f, -0.62f, 0.42f));
            return vehicle;
        }

        private static void CreateVehicleWheel(Transform parent, Vector3 localPosition)
        {
            var wheel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            wheel.name = "Soft Wheel";
            wheel.transform.SetParent(parent, false);
            wheel.transform.localPosition = localPosition;
            wheel.transform.localScale = new Vector3(0.28f, 0.28f, 0.28f);

            var renderer = wheel.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.12f, 0.13f, 0.15f);
                renderer.sharedMaterial = material;
            }
        }

        private GameObject SpawnCashBag()
        {
            var spawnPosition = GetVehicleParkingPosition() + Vector3.up * 0.55f;
            var bag = cashBagPrefab != null
                ? Instantiate(cashBagPrefab, spawnPosition, Quaternion.identity)
                : CreateFallbackCashBag(spawnPosition);

            bag.name = "Super Cash Bag";
            TrySetTag(bag, "Interactable");

            var item = bag.GetComponent<DeliverableItem>();
            if (item == null)
            {
                item = bag.AddComponent<DeliverableItem>();
            }

            item.Configure(CashDeliveryBagId, new Color(0.18f, 0.48f, 0.22f));
            return bag;
        }

        private static GameObject CreateFallbackCashBag(Vector3 position)
        {
            var bag = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bag.transform.position = position;
            bag.transform.localScale = new Vector3(0.62f, 0.5f, 0.62f);

            var renderer = bag.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.18f, 0.48f, 0.22f);
                renderer.sharedMaterial = material;
            }

            var body = bag.AddComponent<Rigidbody>();
            body.mass = 2.4f;
            body.linearDamping = 2.5f;
            body.angularDamping = 4f;
            return bag;
        }

        private bool IsPlayerHoldingCashDeliveryBag()
        {
            if (playerInteraction == null || playerInteraction.HeldObject == null)
            {
                return false;
            }

            return IsCashDeliveryBag(playerInteraction.HeldObject);
        }

        private bool IsCashBagDeliveredToVault()
        {
            if (vaultRestockZone == null || player == null || playerInteraction == null)
            {
                return false;
            }

            if (!vaultRestockZone.bounds.Contains(player.position))
            {
                return false;
            }

            var heldObject = playerInteraction.HeldObject;
            return heldObject != null && IsCashDeliveryBag(heldObject);
        }

        private bool IsHeistRaidBlockingNormalRestock()
        {
            return heistRaidSystem != null && heistRaidSystem.IsRaidActive;
        }

        private static bool IsCashDeliveryBag(GameObject target)
        {
            return target != null
                && target.TryGetComponent<DeliverableItem>(out var item)
                && item.ItemId == CashDeliveryBagId;
        }

        private void CompleteRestock()
        {
            SetState(CashDeliveryState.RestockingVault);
            ApplyHeavyBagSlow(false);

            if (playerInteraction != null && playerInteraction.HeldObject != null)
            {
                playerInteraction.TryDestroyHeldObject();
            }
            else if (activeCashBag != null)
            {
                Destroy(activeCashBag);
            }

            activeCashBag = null;
            RefillCashStock();

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.AddTime(GetEmergencyBonusTime());
            }

            if (vaultCashExplosionEffect != null)
            {
                vaultCashExplosionEffect.Play();
            }

            SetCounterSystemsPaused(false);
            OnCashDeliveryCompleted.Invoke();

            if (deliveryRoutine != null)
            {
                StopCoroutine(deliveryRoutine);
            }

            deliveryRoutine = StartCoroutine(SendVehicleAway());
        }

        private float GetEmergencyBonusTime()
        {
            var baseBonus = bankingActionSystem != null ? bankingActionSystem.BonusTime : fallbackBonusTime;
            return Mathf.Max(0f, baseBonus * emergencyBonusMultiplier);
        }

        private IEnumerator SendVehicleAway()
        {
            if (activeVehicle != null)
            {
                var exitPosition = vehicleExitPoint != null ? vehicleExitPoint.position : activeVehicle.transform.position;
                yield return MoveObjectTo(activeVehicle.transform, exitPosition);
                Destroy(activeVehicle);
                activeVehicle = null;
            }

            deliveryRoutine = null;
        }

        private IEnumerator MoveObjectTo(Transform target, Vector3 destination)
        {
            if (target == null)
            {
                yield break;
            }

            while ((target.position - destination).sqrMagnitude > 0.01f)
            {
                var currentPosition = target.position;
                target.position = Vector3.MoveTowards(currentPosition, destination, vehicleMoveSpeed * Time.deltaTime);

                var direction = destination - currentPosition;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                {
                    target.rotation = Quaternion.Slerp(
                        target.rotation,
                        Quaternion.LookRotation(direction, Vector3.up),
                        8f * Time.deltaTime);
                }

                yield return null;
            }
        }

        private void SetCounterSystemsPaused(bool paused)
        {
            if (paused == counterSystemsPaused)
            {
                return;
            }

            if (pausableCounterSystems == null || pausableCounterSystems.Length == 0)
            {
                counterSystemsPaused = paused;
                return;
            }

            if (paused)
            {
                counterSystemsPaused = true;
                pausablePreviousStates = new bool[pausableCounterSystems.Length];
                for (var i = 0; i < pausableCounterSystems.Length; i++)
                {
                    var system = pausableCounterSystems[i];
                    if (system == null || system == this)
                    {
                        continue;
                    }

                    pausablePreviousStates[i] = system.enabled;
                    system.enabled = false;
                }

                return;
            }

            for (var i = 0; i < pausableCounterSystems.Length; i++)
            {
                var system = pausableCounterSystems[i];
                if (system == null || system == this)
                {
                    continue;
                }

                var wasEnabled = i < pausablePreviousStates.Length && pausablePreviousStates[i];
                system.enabled = wasEnabled;
            }

            pausablePreviousStates = System.Array.Empty<bool>();
            counterSystemsPaused = false;
        }

        private void RefreshStateFromStock()
        {
            SetState(currentVaultCash <= 0 ? CashDeliveryState.CashEmpty : CashDeliveryState.Ready);
            OnCashStockChanged.Invoke(currentVaultCash);
            UpdateNoCashWarning();
        }

        private void UpdateRequestButton()
        {
            var shouldShow = state == CashDeliveryState.CashEmpty;
            if (requestCashButtonRoot != null)
            {
                requestCashButtonRoot.SetActive(shouldShow);
            }

            if (requestCashButton != null)
            {
                requestCashButton.interactable = shouldShow;
            }

            OnRequestButtonVisibilityChanged.Invoke(shouldShow);
            UpdateNoCashWarning();
        }

        private void UpdateNoCashWarning()
        {
            if (noCashWarningIcon != null)
            {
                noCashWarningIcon.SetActive(currentVaultCash <= 0);
            }
        }

        private void ApplyHeavyBagSlow(bool apply)
        {
            if (heavyBagSlowApplied == apply)
            {
                return;
            }

            heavyBagSlowApplied = apply;
            var multiplier = apply ? heavyBagSpeedMultiplier : 1f;

            if (topDownController != null)
            {
                topDownController.MovementSpeedMultiplier = multiplier;
            }

            if (mobilePlayerController != null)
            {
                mobilePlayerController.MovementSpeedMultiplier = multiplier;
            }
        }

        private Vector3 GetVehicleParkingPosition()
        {
            if (vehicleParkingPoint != null)
            {
                return vehicleParkingPoint.position;
            }

            return vehicleSpawnPoint != null ? vehicleSpawnPoint.position + Vector3.forward * 1.5f : transform.position;
        }

        private void SetState(CashDeliveryState nextState)
        {
            if (state == nextState)
            {
                return;
            }

            state = nextState;
            OnStateChanged.Invoke(state);
        }

        private static void TrySetTag(GameObject target, string tagName)
        {
            if (target == null || string.IsNullOrEmpty(tagName))
            {
                return;
            }

            try
            {
                target.tag = tagName;
            }
            catch (UnityException)
            {
                // Prototype setup creates the Interactable tag. Manual scenes can still run safely.
            }
        }
    }
}
