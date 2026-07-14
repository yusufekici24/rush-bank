using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace RushBank.Gameplay
{
    public class SecuritySystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private QueueManager queueManager;
        [SerializeField] private Transform securityGuard;
        [SerializeField] private Transform guardHomePoint;
        [SerializeField] private Transform escortExitPoint;
        [SerializeField] private Animator guardAnimator;

        [Header("Tuning")]
        [SerializeField, Min(0.1f)] private float guardMoveSpeed = 3f;
        [SerializeField, Min(0f)] private float cooldownSeconds = 8f;
        [SerializeField, Range(0f, 100f)] private float calmRestorePatience = 30f;
        [SerializeField, Range(0f, 100f)] private float escortBelowPatience = 15f;

        [Header("Animation Triggers")]
        [SerializeField] private string walkTrigger = "Walk";
        [SerializeField] private string calmDownTrigger = "CalmDown";
        [SerializeField] private string escortTrigger = "Escort";
        [SerializeField] private string idleTrigger = "Idle";

        public UnityEvent OnSecurityCalled = new UnityEvent();
        public UnityEvent<GameObject> OnCustomerCalmed = new UnityEvent<GameObject>();
        public UnityEvent<GameObject> OnCustomerEscorted = new UnityEvent<GameObject>();

        private bool isBusy;
        private float cooldownTimer;
        private WaitForSeconds calmWait;
        private int walkHash;
        private int calmDownHash;
        private int escortHash;
        private int idleHash;

        public bool IsOnCooldown => cooldownTimer > 0f;
        public bool IsBusy => isBusy;

        private void Awake()
        {
            if (guardAnimator == null && securityGuard != null)
            {
                guardAnimator = securityGuard.GetComponentInChildren<Animator>();
            }

            walkHash = Animator.StringToHash(walkTrigger);
            calmDownHash = Animator.StringToHash(calmDownTrigger);
            escortHash = Animator.StringToHash(escortTrigger);
            idleHash = Animator.StringToHash(idleTrigger);
            calmWait = new WaitForSeconds(0.35f);
        }

        private void Update()
        {
            if (cooldownTimer > 0f)
            {
                cooldownTimer = Mathf.Max(0f, cooldownTimer - Time.deltaTime);
            }
        }

        public void CallSecurity()
        {
            if (isBusy || IsOnCooldown || queueManager == null || securityGuard == null)
            {
                return;
            }

            var target = queueManager.FindLowestPatienceCustomer();
            if (target == null)
            {
                return;
            }

            StartCoroutine(SecurityRoutine(target));
        }

        private IEnumerator SecurityRoutine(CustomerPatience target)
        {
            isBusy = true;
            cooldownTimer = cooldownSeconds;
            OnSecurityCalled.Invoke();

            PlayGuardTrigger(walkHash);
            yield return MoveGuardTo(target.transform.position);

            if (target == null)
            {
                yield return ReturnGuardHome();
                isBusy = false;
                yield break;
            }

            if (target.Patience <= escortBelowPatience || target.MoodState == CustomerMoodState.Raging)
            {
                PlayGuardTrigger(escortHash);
                OnCustomerEscorted.Invoke(target.gameObject);
                yield return EscortCustomerOut(target);
            }
            else
            {
                PlayGuardTrigger(calmDownHash);
                target.RestorePatience(calmRestorePatience);
                OnCustomerCalmed.Invoke(target.gameObject);
                yield return calmWait;
            }

            yield return ReturnGuardHome();
            isBusy = false;
        }

        private IEnumerator EscortCustomerOut(CustomerPatience target)
        {
            if (target == null)
            {
                yield break;
            }

            var exitPosition = escortExitPoint != null ? escortExitPoint.position : target.transform.position + Vector3.back * 4f;
            var customerTransform = target.transform;

            while (customerTransform != null && (customerTransform.position - exitPosition).sqrMagnitude > 0.08f)
            {
                var nextPosition = Vector3.MoveTowards(
                    customerTransform.position,
                    exitPosition,
                    guardMoveSpeed * Time.deltaTime);
                customerTransform.position = nextPosition;
                yield return null;
            }

            queueManager.RemoveCustomer(target.gameObject);
        }

        private IEnumerator ReturnGuardHome()
        {
            if (guardHomePoint == null)
            {
                PlayGuardTrigger(idleHash);
                yield break;
            }

            PlayGuardTrigger(walkHash);
            yield return MoveGuardTo(guardHomePoint.position);
            PlayGuardTrigger(idleHash);
        }

        private IEnumerator MoveGuardTo(Vector3 targetPosition)
        {
            while ((securityGuard.position - targetPosition).sqrMagnitude > 0.05f)
            {
                var currentPosition = securityGuard.position;
                var nextPosition = Vector3.MoveTowards(currentPosition, targetPosition, guardMoveSpeed * Time.deltaTime);
                securityGuard.position = nextPosition;

                var direction = targetPosition - currentPosition;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                {
                    securityGuard.rotation = Quaternion.Slerp(
                        securityGuard.rotation,
                        Quaternion.LookRotation(direction, Vector3.up),
                        8f * Time.deltaTime);
                }

                yield return null;
            }
        }

        private void PlayGuardTrigger(int triggerHash)
        {
            if (guardAnimator != null && triggerHash != 0)
            {
                guardAnimator.SetTrigger(triggerHash);
            }
        }
    }
}
