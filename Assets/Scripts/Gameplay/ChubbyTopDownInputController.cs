using RushBank.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RushBank.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    public class ChubbyTopDownInputController : MonoBehaviour
    {
        [Header("Input System")]
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private InputActionReference moveActionReference;
        [SerializeField] private string moveActionName = "Move";

        [Header("Mobile")]
        [SerializeField] private ScreenJoystick screenJoystick;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float movementSpeed = 3.25f;
        [SerializeField, Min(0f)] private float rotationSpeed = 9f;
        [SerializeField, Min(0f)] private float accelerationRate = 5f;
        [SerializeField, Min(0f)] private float decelerationRate = 2.4f;
        [SerializeField, Range(0f, 0.4f)] private float inputDeadZone = 0.08f;

        private Rigidbody body;
        private InputAction moveAction;
        private Vector2 cachedInput;
        private Vector3 planarVelocity;
        private float movementSpeedMultiplier = 1f;

        public float MovementSpeed
        {
            get => movementSpeed;
            set => movementSpeed = Mathf.Max(0f, value);
        }

        public float MovementSpeedMultiplier
        {
            get => movementSpeedMultiplier;
            set => movementSpeedMultiplier = Mathf.Max(0f, value);
        }

        public float RotationSpeed
        {
            get => rotationSpeed;
            set => rotationSpeed = Mathf.Max(0f, value);
        }

        public float DecelerationRate
        {
            get => decelerationRate;
            set => decelerationRate = Mathf.Max(0f, value);
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            if (playerInput == null)
            {
                playerInput = GetComponent<PlayerInput>();
            }

            ResolveMoveAction();
        }

        private void OnEnable()
        {
            if (moveAction != null && !moveAction.enabled)
            {
                moveAction.Enable();
            }
        }

        private void OnDisable()
        {
            if (moveActionReference != null && moveActionReference.action != null)
            {
                moveActionReference.action.Disable();
            }
        }

        private void Update()
        {
            cachedInput = ReadMovementInput();
        }

        private void FixedUpdate()
        {
            var desiredDirection = new Vector3(cachedInput.x, 0f, cachedInput.y);
            if (desiredDirection.sqrMagnitude > 1f)
            {
                desiredDirection.Normalize();
            }

            var targetVelocity = desiredDirection * movementSpeed * movementSpeedMultiplier;
            var rate = desiredDirection.sqrMagnitude > 0f ? accelerationRate : decelerationRate;

            // Low acceleration and lower deceleration create the chubby, slightly clumsy slide.
            planarVelocity = Vector3.MoveTowards(
                planarVelocity,
                targetVelocity,
                rate * Time.fixedDeltaTime);

            var currentVelocity = body.linearVelocity;
            body.linearVelocity = new Vector3(planarVelocity.x, currentVelocity.y, planarVelocity.z);

            RotateTowardsVelocity();
        }

        private void ResolveMoveAction()
        {
            if (moveActionReference != null)
            {
                moveAction = moveActionReference.action;
                return;
            }

            if (playerInput != null && playerInput.actions != null)
            {
                moveAction = playerInput.actions.FindAction(moveActionName, false);
            }
        }

        private Vector2 ReadMovementInput()
        {
            var joystickInput = screenJoystick != null ? screenJoystick.Value : Vector2.zero;
            if (joystickInput.sqrMagnitude > inputDeadZone * inputDeadZone)
            {
                return Vector2.ClampMagnitude(joystickInput, 1f);
            }

            if (moveAction != null)
            {
                var actionInput = moveAction.ReadValue<Vector2>();
                if (actionInput.sqrMagnitude > inputDeadZone * inputDeadZone)
                {
                    return Vector2.ClampMagnitude(actionInput, 1f);
                }
            }

            return ReadEditorKeyboardInput();
        }

        private Vector2 ReadEditorKeyboardInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return Vector2.zero;
            }

            var x = 0f;
            var y = 0f;

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                x -= 1f;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                x += 1f;
            }

            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                y -= 1f;
            }

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                y += 1f;
            }

            var keyboardInput = new Vector2(x, y);
            return keyboardInput.sqrMagnitude > 1f ? keyboardInput.normalized : keyboardInput;
        }

        private void RotateTowardsVelocity()
        {
            var flatVelocity = new Vector3(planarVelocity.x, 0f, planarVelocity.z);
            if (flatVelocity.sqrMagnitude < 0.02f)
            {
                return;
            }

            var targetRotation = Quaternion.LookRotation(flatVelocity, Vector3.up);
            var smoothedRotation = Quaternion.Slerp(
                body.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime);

            body.MoveRotation(smoothedRotation);
        }
    }
}
