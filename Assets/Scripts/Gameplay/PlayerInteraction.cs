using UnityEngine;

namespace RushBank.Gameplay
{
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Anchors")]
        [SerializeField] private Transform holdPoint;
        [SerializeField] private Transform throwDirectionSource;

        [Header("Throw")]
        [SerializeField, Min(0f)] private float throwForce = 7f;
        [SerializeField, Min(0f)] private float upwardForce = 2f;
        [SerializeField, Min(0f)] private float playerCollisionIgnoreSeconds = 0.35f;

        private Rigidbody candidateBody;
        private Rigidbody heldBody;
        private Collider[] playerColliders;
        private Collider[] heldColliders;
        private float collisionRestoreTimer;

        public bool IsHolding => heldBody != null;
        public Rigidbody HeldBody => heldBody;
        public GameObject HeldObject => heldBody != null ? heldBody.gameObject : null;

        private void Awake()
        {
            playerColliders = GetComponentsInChildren<Collider>();

            if (throwDirectionSource == null)
            {
                throwDirectionSource = transform;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Grab();
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                ThrowHeldObject();
            }

            TickCollisionRestore();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (heldBody != null || !other.CompareTag("Interactable"))
            {
                return;
            }

            var body = other.attachedRigidbody;
            if (body != null)
            {
                candidateBody = body;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (candidateBody == null || other.attachedRigidbody != candidateBody)
            {
                return;
            }

            candidateBody = null;
        }

        public void Grab()
        {
            if (heldBody != null || candidateBody == null || holdPoint == null)
            {
                return;
            }

            heldBody = candidateBody;
            candidateBody = null;
            heldColliders = heldBody.GetComponentsInChildren<Collider>();

            IgnorePlayerCollision(true);
            heldBody.isKinematic = true;
            heldBody.detectCollisions = true;
            heldBody.linearVelocity = Vector3.zero;
            heldBody.angularVelocity = Vector3.zero;

            heldBody.transform.SetParent(holdPoint, false);
            heldBody.transform.localPosition = Vector3.zero;
            heldBody.transform.localRotation = Quaternion.identity;
        }

        public void ThrowHeldObject()
        {
            if (heldBody == null)
            {
                return;
            }

            var body = heldBody;
            heldBody = null;

            body.transform.SetParent(null, true);
            body.isKinematic = false;
            body.detectCollisions = true;

            var forward = throwDirectionSource != null ? throwDirectionSource.forward : transform.forward;
            var force = (forward.normalized * throwForce) + (Vector3.up * upwardForce);
            body.AddForce(force, ForceMode.Impulse);

            collisionRestoreTimer = playerCollisionIgnoreSeconds;
        }

        public bool TryDestroyHeldObject()
        {
            if (heldBody == null)
            {
                return false;
            }

            var objectToDestroy = heldBody.gameObject;
            heldBody.transform.SetParent(null, true);
            heldBody = null;
            IgnorePlayerCollision(false);
            heldColliders = null;
            Destroy(objectToDestroy);
            return true;
        }

        public void DropHeldObject()
        {
            if (heldBody == null)
            {
                return;
            }

            heldBody.transform.SetParent(null, true);
            heldBody.isKinematic = false;
            heldBody.detectCollisions = true;
            heldBody = null;
            collisionRestoreTimer = playerCollisionIgnoreSeconds;
        }

        private void TickCollisionRestore()
        {
            if (collisionRestoreTimer <= 0f)
            {
                return;
            }

            collisionRestoreTimer -= Time.deltaTime;
            if (collisionRestoreTimer <= 0f)
            {
                IgnorePlayerCollision(false);
                heldColliders = null;
            }
        }

        private void IgnorePlayerCollision(bool ignore)
        {
            if (playerColliders == null || heldColliders == null)
            {
                return;
            }

            for (var i = 0; i < playerColliders.Length; i++)
            {
                var playerCollider = playerColliders[i];
                if (playerCollider == null || playerCollider.isTrigger)
                {
                    continue;
                }

                for (var j = 0; j < heldColliders.Length; j++)
                {
                    var objectCollider = heldColliders[j];
                    if (objectCollider == null || objectCollider.isTrigger)
                    {
                        continue;
                    }

                    Physics.IgnoreCollision(playerCollider, objectCollider, ignore);
                }
            }
        }
    }
}
