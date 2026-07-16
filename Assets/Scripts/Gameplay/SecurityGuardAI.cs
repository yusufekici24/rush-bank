using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace RushBank.Gameplay
{
    public class SecurityGuardAI : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private Transform idlePost;
        [SerializeField] private Transform mainExitDoor;
        [SerializeField, Min(0.1f)] private float sprintSpeed = 4.6f;
        [SerializeField, Min(0.1f)] private float fallbackMoveSpeed = 4.6f;
        [SerializeField] private Vector3 escortedCustomerLocalOffset = new Vector3(0.85f, 0f, 0.2f);

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private string sprintTrigger = "Sprint";
        [SerializeField] private string grabTrigger = "GrabCustomer";
        [SerializeField] private string escortTrigger = "Escort";
        [SerializeField] private string idleTrigger = "Idle";

        public UnityEvent<GameObject> OnEscortStarted = new UnityEvent<GameObject>();
        public UnityEvent<GameObject> OnCustomerRemoved = new UnityEvent<GameObject>();
        public UnityEvent OnReturnedToIdle = new UnityEvent();

        private int sprintHash;
        private int grabHash;
        private int escortHash;
        private int idleHash;
        private float previousAgentSpeed;
        private bool isBusy;

        public bool IsBusy => isBusy;

        private void Awake()
        {
            if (agent == null)
            {
                agent = GetComponent<NavMeshAgent>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            sprintHash = Animator.StringToHash(sprintTrigger);
            grabHash = Animator.StringToHash(grabTrigger);
            escortHash = Animator.StringToHash(escortTrigger);
            idleHash = Animator.StringToHash(idleTrigger);

            if (agent != null)
            {
                previousAgentSpeed = agent.speed;
            }
        }

        public IEnumerator EscortAngryCustomerRoutine(
            GameObject angryCustomer,
            Transform counterPointOverride,
            Transform exitPointOverride,
            Transform idlePostOverride)
        {
            if (angryCustomer == null || isBusy)
            {
                yield break;
            }

            isBusy = true;
            OnEscortStarted.Invoke(angryCustomer);

            var counterPosition = counterPointOverride != null
                ? counterPointOverride.position
                : angryCustomer.transform.position;
            var exitPosition = exitPointOverride != null
                ? exitPointOverride.position
                : mainExitDoor != null
                    ? mainExitDoor.position
                    : angryCustomer.transform.position + Vector3.back * 5f;
            var homePosition = idlePostOverride != null
                ? idlePostOverride.position
                : idlePost != null
                    ? idlePost.position
                    : transform.position;

            SetAgentSpeed(sprintSpeed);
            PlayTrigger(sprintHash);
            yield return MoveTo(counterPosition);

            if (angryCustomer == null)
            {
                yield return ReturnHome(homePosition);
                yield break;
            }

            PlayTrigger(grabHash);
            AttachCustomer(angryCustomer);
            PlayTrigger(escortHash);
            yield return MoveTo(exitPosition);

            if (angryCustomer != null)
            {
                OnCustomerRemoved.Invoke(angryCustomer);
                Destroy(angryCustomer);
            }

            yield return ReturnHome(homePosition);
        }

        private IEnumerator ReturnHome(Vector3 homePosition)
        {
            PlayTrigger(sprintHash);
            yield return MoveTo(homePosition);
            RestoreAgentSpeed();
            PlayTrigger(idleHash);
            isBusy = false;
            OnReturnedToIdle.Invoke();
        }

        private void AttachCustomer(GameObject angryCustomer)
        {
            var customerTransform = angryCustomer.transform;
            customerTransform.SetParent(transform, false);
            customerTransform.localPosition = escortedCustomerLocalOffset;
            customerTransform.localRotation = Quaternion.identity;

            if (angryCustomer.TryGetComponent<Rigidbody>(out var body))
            {
                body.isKinematic = true;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            var navAgent = angryCustomer.GetComponent<NavMeshAgent>();
            if (navAgent != null)
            {
                navAgent.enabled = false;
            }
        }

        private IEnumerator MoveTo(Vector3 destination)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.SetDestination(destination);
                while (agent.pathPending)
                {
                    yield return null;
                }

                while (agent.enabled && agent.isOnNavMesh && agent.remainingDistance > Mathf.Max(agent.stoppingDistance, 0.12f))
                {
                    yield return null;
                }

                yield break;
            }

            while ((transform.position - destination).sqrMagnitude > 0.04f)
            {
                var current = transform.position;
                transform.position = Vector3.MoveTowards(current, destination, fallbackMoveSpeed * Time.deltaTime);

                var direction = destination - current;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        Quaternion.LookRotation(direction.normalized, Vector3.up),
                        10f * Time.deltaTime);
                }

                yield return null;
            }
        }

        private void SetAgentSpeed(float speed)
        {
            if (agent == null)
            {
                return;
            }

            previousAgentSpeed = agent.speed;
            agent.speed = speed;
        }

        private void RestoreAgentSpeed()
        {
            if (agent != null)
            {
                agent.speed = previousAgentSpeed;
            }
        }

        private void PlayTrigger(int triggerHash)
        {
            if (animator != null && triggerHash != 0)
            {
                animator.SetTrigger(triggerHash);
            }
        }
    }
}
