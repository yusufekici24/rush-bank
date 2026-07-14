using RushBank.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RushBank.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    public class MobilePlayerController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private ScreenJoystick virtualJoystick;
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference grabAction;
        [SerializeField] private InputActionReference depositAction;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float movementSpeed = 3.2f;
        [SerializeField, Min(0f)] private float acceleration = 8f;
        [SerializeField, Min(0f)] private float rotationSpeed = 10f;
        [SerializeField, Range(0f, 0.4f)] private float inputDeadZone = 0.08f;

        [Header("Interaction")]
        [SerializeField] private Transform holdPoint;
        [SerializeField] private string cashRegisterTag = "CashRegister";
        [SerializeField] private string counterTag = "Counter";
        [SerializeField] private string snackDrawerTag = "SnackDrawer";
        [SerializeField] private string carryableTag = "Interactable";
        [SerializeField] private GameObject snackItemPrefab;
        [SerializeField] private string snackItemId = "assistant_snack";
        [SerializeField] private Color fallbackSnackColor = new Color(0.76f, 0.42f, 0.16f);
        [SerializeField, Min(0)] private int snackDrawerStock = 3;

        private Rigidbody body;
        private Vector2 movementInput;
        private Vector3 currentPlanarVelocity;
        private GameObject nearbyStation;
        private GameObject heldItem;
        private float movementSpeedMultiplier = 1f;

        public bool IsHoldingItem => heldItem != null;
        public GameObject HeldItem => heldItem;
        public float MovementSpeedMultiplier
        {
            get => movementSpeedMultiplier;
            set => movementSpeedMultiplier = Mathf.Max(0f, value);
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        private void OnEnable()
        {
            EnableAction(moveAction);
            EnableAction(grabAction);
            EnableAction(depositAction);

            if (grabAction != null && grabAction.action != null)
            {
                grabAction.action.performed += OnGrabPerformed;
            }

            if (depositAction != null && depositAction.action != null)
            {
                depositAction.action.performed += OnDepositPerformed;
            }
        }

        private void OnDisable()
        {
            if (grabAction != null && grabAction.action != null)
            {
                grabAction.action.performed -= OnGrabPerformed;
            }

            if (depositAction != null && depositAction.action != null)
            {
                depositAction.action.performed -= OnDepositPerformed;
            }
        }

        private void Update()
        {
            movementInput = ReadMovementInput();
        }

        private void FixedUpdate()
        {
            Move();
            RotateTowardsMovement();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsInteractionStation(other))
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

        public void Action()
        {
            if (TryFeedAssistant())
            {
                return;
            }

            if (IsHoldingItem)
            {
                Deposit();
            }
            else
            {
                Grab();
            }
        }

        public void Grab()
        {
            if (heldItem != null || nearbyStation == null || holdPoint == null)
            {
                return;
            }

            if (nearbyStation.CompareTag(snackDrawerTag))
            {
                if (snackDrawerStock <= 0)
                {
                    return;
                }

                snackDrawerStock--;
                heldItem = CreateSnackItem();
                PrepareHeldItem(heldItem);
                heldItem.transform.SetParent(holdPoint, false);
                heldItem.transform.localPosition = Vector3.zero;
                heldItem.transform.localRotation = Quaternion.identity;
                return;
            }

            var item = FindCarryableItem(nearbyStation.transform);
            if (item == null)
            {
                return;
            }

            heldItem = item;
            PrepareHeldItem(heldItem);
            heldItem.transform.SetParent(holdPoint, false);
            heldItem.transform.localPosition = Vector3.zero;
            heldItem.transform.localRotation = Quaternion.identity;
        }

        public void Deposit()
        {
            if (heldItem == null || nearbyStation == null)
            {
                return;
            }

            heldItem.transform.SetParent(nearbyStation.transform, true);
            heldItem.transform.position = nearbyStation.transform.position + Vector3.up * 0.75f;
            heldItem.transform.rotation = nearbyStation.transform.rotation;
            RestoreDepositedItem(heldItem);
            heldItem = null;
        }

        private bool TryFeedAssistant()
        {
            if (heldItem == null || nearbyStation == null || !IsSnackItem(heldItem))
            {
                return false;
            }

            var assistant = nearbyStation.GetComponentInParent<LazyAssistantAI>();
            if (assistant == null)
            {
                assistant = nearbyStation.GetComponent<LazyAssistantAI>();
            }

            if (assistant == null)
            {
                return false;
            }

            if (!assistant.FeedSnack(heldItem))
            {
                return true;
            }

            heldItem = null;
            return true;
        }

        private Vector2 ReadMovementInput()
        {
            var joystickInput = virtualJoystick != null ? virtualJoystick.Value : Vector2.zero;
            if (joystickInput.sqrMagnitude > inputDeadZone * inputDeadZone)
            {
                return Vector2.ClampMagnitude(joystickInput, 1f);
            }

            if (moveAction != null && moveAction.action != null)
            {
                var actionInput = moveAction.action.ReadValue<Vector2>();
                if (actionInput.sqrMagnitude > inputDeadZone * inputDeadZone)
                {
                    return Vector2.ClampMagnitude(actionInput, 1f);
                }
            }

            return Vector2.zero;
        }

        private void Move()
        {
            var desiredDirection = new Vector3(movementInput.x, 0f, movementInput.y);
            if (desiredDirection.sqrMagnitude > 1f)
            {
                desiredDirection.Normalize();
            }

            var targetVelocity = desiredDirection * movementSpeed * movementSpeedMultiplier;
            currentPlanarVelocity = Vector3.MoveTowards(
                currentPlanarVelocity,
                targetVelocity,
                acceleration * Time.fixedDeltaTime);

            var velocity = body.linearVelocity;
            body.linearVelocity = new Vector3(currentPlanarVelocity.x, velocity.y, currentPlanarVelocity.z);
        }

        private void RotateTowardsMovement()
        {
            var flatVelocity = new Vector3(currentPlanarVelocity.x, 0f, currentPlanarVelocity.z);
            if (flatVelocity.sqrMagnitude < 0.01f)
            {
                return;
            }

            var targetRotation = Quaternion.LookRotation(flatVelocity.normalized, Vector3.up);
            body.MoveRotation(Quaternion.Slerp(body.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
        }

        private bool IsInteractionStation(Collider other)
        {
            var objectTag = other.gameObject.tag;
            return objectTag == cashRegisterTag
                || objectTag == counterTag
                || objectTag == snackDrawerTag
                || other.GetComponentInParent<LazyAssistantAI>() != null;
        }

        private GameObject FindCarryableItem(Transform station)
        {
            for (var i = 0; i < station.childCount; i++)
            {
                var child = station.GetChild(i);
                if (child.CompareTag(carryableTag))
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        private static void PrepareHeldItem(GameObject item)
        {
            if (item.TryGetComponent<Rigidbody>(out var itemBody))
            {
                itemBody.isKinematic = true;
                itemBody.linearVelocity = Vector3.zero;
                itemBody.angularVelocity = Vector3.zero;
            }

            SetCollidersEnabled(item, false);
        }

        private GameObject CreateSnackItem()
        {
            GameObject item;
            if (snackItemPrefab != null)
            {
                item = Instantiate(snackItemPrefab, holdPoint);
            }
            else
            {
                item = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                item.name = "Tontis Snack Item";
                item.transform.localScale = new Vector3(0.28f, 0.16f, 0.28f);

                if (item.TryGetComponent<Renderer>(out var rendererComponent))
                {
                    var material = new Material(Shader.Find("Standard"));
                    material.color = fallbackSnackColor;
                    rendererComponent.sharedMaterial = material;
                }
            }

            item.tag = carryableTag;
            if (!item.TryGetComponent<DeliverableItem>(out var deliverable))
            {
                deliverable = item.AddComponent<DeliverableItem>();
            }

            deliverable.Configure(snackItemId, fallbackSnackColor);
            return item;
        }

        private bool IsSnackItem(GameObject item)
        {
            return item != null
                && item.TryGetComponent<DeliverableItem>(out var deliverable)
                && deliverable.ItemId == snackItemId;
        }

        private static void RestoreDepositedItem(GameObject item)
        {
            if (item.TryGetComponent<Rigidbody>(out var itemBody))
            {
                itemBody.isKinematic = false;
            }

            SetCollidersEnabled(item, true);
        }

        private static void SetCollidersEnabled(GameObject item, bool enabled)
        {
            var colliders = item.GetComponentsInChildren<Collider>();
            for (var i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = enabled;
            }
        }

        private static void EnableAction(InputActionReference actionReference)
        {
            if (actionReference != null && actionReference.action != null && !actionReference.action.enabled)
            {
                actionReference.action.Enable();
            }
        }

        private void OnGrabPerformed(InputAction.CallbackContext context)
        {
            Grab();
        }

        private void OnDepositPerformed(InputAction.CallbackContext context)
        {
            Deposit();
        }
    }
}
