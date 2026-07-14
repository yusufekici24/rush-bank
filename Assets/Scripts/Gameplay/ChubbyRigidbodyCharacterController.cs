using RushBank.UI;
using UnityEngine;

namespace RushBank.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    public class ChubbyRigidbodyCharacterController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private ScreenJoystick joystick;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private bool useCameraRelativeMovement = true;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float speed = 3.2f;
        [SerializeField, Min(0f)] private float turnSpeed = 8f;
        [SerializeField, Min(0f)] private float acceleration = 4.5f;
        [SerializeField, Min(0f)] private float stopDeceleration = 2.2f;
        [SerializeField, Range(0f, 0.4f)] private float inputDeadZone = 0.08f;

        private Rigidbody body;
        private Vector2 movementInput;
        private Vector3 horizontalVelocity;

        public float Speed
        {
            get => speed;
            set => speed = Mathf.Max(0f, value);
        }

        public float TurnSpeed
        {
            get => turnSpeed;
            set => turnSpeed = Mathf.Max(0f, value);
        }

        public float Acceleration
        {
            get => acceleration;
            set => acceleration = Mathf.Max(0f, value);
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        private void Update()
        {
            movementInput = ReadMovementInput();
        }

        private void FixedUpdate()
        {
            var desiredDirection = BuildWorldDirection(movementInput);
            var targetVelocity = desiredDirection * speed;
            var selectedAcceleration = desiredDirection.sqrMagnitude > 0f ? acceleration : stopDeceleration;

            horizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity,
                targetVelocity,
                selectedAcceleration * Time.fixedDeltaTime);

            var currentVelocity = body.linearVelocity;
            body.linearVelocity = new Vector3(horizontalVelocity.x, currentVelocity.y, horizontalVelocity.z);

            RotateTowardsMovement();
        }

        private Vector2 ReadMovementInput()
        {
            var joystickInput = joystick != null ? joystick.Value : Vector2.zero;
            if (joystickInput.sqrMagnitude > inputDeadZone * inputDeadZone)
            {
                return Vector2.ClampMagnitude(joystickInput, 1f);
            }

            var keyboardInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (keyboardInput.sqrMagnitude <= inputDeadZone * inputDeadZone)
            {
                return Vector2.zero;
            }

            return Vector2.ClampMagnitude(keyboardInput, 1f);
        }

        private Vector3 BuildWorldDirection(Vector2 input)
        {
            if (input.sqrMagnitude <= inputDeadZone * inputDeadZone)
            {
                return Vector3.zero;
            }

            if (!useCameraRelativeMovement || cameraTransform == null)
            {
                return new Vector3(input.x, 0f, input.y).normalized;
            }

            var forward = cameraTransform.forward;
            var right = cameraTransform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            return ((right * input.x) + (forward * input.y)).normalized;
        }

        private void RotateTowardsMovement()
        {
            var flatVelocity = new Vector3(horizontalVelocity.x, 0f, horizontalVelocity.z);
            if (flatVelocity.sqrMagnitude < 0.02f)
            {
                return;
            }

            var targetRotation = Quaternion.LookRotation(flatVelocity, Vector3.up);
            body.MoveRotation(Quaternion.Slerp(body.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));
        }
    }
}
