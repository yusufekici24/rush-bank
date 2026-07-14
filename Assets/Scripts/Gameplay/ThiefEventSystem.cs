using System.Collections;
using RushBank.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RushBank.Gameplay
{
    public class ThiefEventSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private QueueManager queueManager;
        [SerializeField] private Button callPoliceButton;
        [SerializeField] private GameObject thiefPrefab;
        [SerializeField] private GameObject policeOfficerPrefab;
        [SerializeField] private Transform thiefSpawnPoint;
        [SerializeField] private Transform policeSpawnPoint;
        [SerializeField] private Transform policeExitPoint;
        [SerializeField] private Animator policeAnimator;

        [Header("Tuning")]
        [SerializeField, Min(0f)] private float criticalTimeThreshold = 20f;
        [SerializeField, Range(0f, 1f)] private float timeCriticalSpawnChance = 0.1f;
        [SerializeField, Min(0.1f)] private float policeMoveSpeed = 3.4f;
        [SerializeField] private bool spawnWhenTimeCritical = true;

        [Header("Animation")]
        [SerializeField] private string policeWalkTrigger = "Walk";
        [SerializeField] private string policeArrestTrigger = "Arrest";
        [SerializeField] private string policeIdleTrigger = "Idle";

        public UnityEvent<GameObject> OnThiefSpawned = new UnityEvent<GameObject>();
        public UnityEvent OnPoliceCalled = new UnityEvent();
        public UnityEvent OnThiefRemoved = new UnityEvent();

        private GameObject activeThief;
        private Transform activePolice;
        private bool eventRunning;
        private WaitForSeconds arrestWait;
        private int policeWalkHash;
        private int policeArrestHash;
        private int policeIdleHash;
        private bool rolledForCriticalTime;

        public bool IsEventActive => activeThief != null || eventRunning;

        private void Awake()
        {
            policeWalkHash = Animator.StringToHash(policeWalkTrigger);
            policeArrestHash = Animator.StringToHash(policeArrestTrigger);
            policeIdleHash = Animator.StringToHash(policeIdleTrigger);
            arrestWait = new WaitForSeconds(0.35f);
            ApplySelectedBranchSettings();
            SetCallPoliceButton(false);
        }

        private void OnEnable()
        {
            if (callPoliceButton != null)
            {
                callPoliceButton.onClick.AddListener(CallPolice);
            }
        }

        private void OnDisable()
        {
            if (callPoliceButton != null)
            {
                callPoliceButton.onClick.RemoveListener(CallPolice);
            }
        }

        private void Update()
        {
            if (!spawnWhenTimeCritical || activeThief != null || eventRunning || TimeManager.Instance == null)
            {
                return;
            }

            if (TimeManager.Instance.RemainingSeconds > criticalTimeThreshold)
            {
                rolledForCriticalTime = false;
                return;
            }

            if (rolledForCriticalTime)
            {
                return;
            }

            rolledForCriticalTime = true;
            if (Random.value <= timeCriticalSpawnChance)
            {
                SpawnThief();
            }
        }

        public void SpawnThief()
        {
            if (queueManager == null || activeThief != null)
            {
                return;
            }

            activeThief = CreateThiefObject();
            queueManager.AddCustomerToQueue(activeThief);
            SetCallPoliceButton(true);
            OnThiefSpawned.Invoke(activeThief);
        }

        public void CallPolice()
        {
            if (activeThief == null || eventRunning)
            {
                return;
            }

            StartCoroutine(PoliceRoutine());
        }

        private IEnumerator PoliceRoutine()
        {
            eventRunning = true;
            SetCallPoliceButton(false);

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.FreezeTime(true);
            }

            OnPoliceCalled.Invoke();

            activePolice = CreatePoliceOfficer();
            PlayPoliceTrigger(policeWalkHash);
            yield return MoveTransform(activePolice, activeThief.transform.position);

            PlayPoliceTrigger(policeArrestHash);
            yield return arrestWait;

            var thiefTransform = activeThief != null ? activeThief.transform : null;
            var exitPosition = GetPoliceExitPosition();

            while (activePolice != null && (activePolice.position - exitPosition).sqrMagnitude > 0.1f)
            {
                MoveOneStep(activePolice, exitPosition, policeMoveSpeed);
                if (thiefTransform != null)
                {
                    thiefTransform.position = activePolice.position + activePolice.right * 0.75f;
                }

                yield return null;
            }

            if (activeThief != null)
            {
                queueManager.RemoveCustomer(activeThief);
                activeThief = null;
            }

            if (activePolice != null)
            {
                Destroy(activePolice.gameObject);
                activePolice = null;
            }

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.FreezeTime(false);
            }

            PlayPoliceTrigger(policeIdleHash);
            OnThiefRemoved.Invoke();
            eventRunning = false;
        }

        private GameObject CreateThiefObject()
        {
            GameObject thief;
            if (thiefPrefab != null)
            {
                thief = Instantiate(thiefPrefab, GetThiefSpawnPosition(), Quaternion.identity);
            }
            else
            {
                thief = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                thief.name = "Cute Thief Customer";
                thief.transform.position = GetThiefSpawnPosition();
                AddMaskVisual(thief.transform);
            }

            if (thief.GetComponent<ThiefMarker>() == null)
            {
                thief.AddComponent<ThiefMarker>();
            }

            return thief;
        }

        public void ApplyBranchSettings(BranchSettings settings)
        {
            timeCriticalSpawnChance = settings.WithClampedValues().thiefAttackChance;
        }

        public void ApplySelectedBranchSettings()
        {
            if (GameSettingsManager.Instance == null)
            {
                return;
            }

            ApplyBranchSettings(GameSettingsManager.Instance.CurrentBranchSettings);
        }

        private Transform CreatePoliceOfficer()
        {
            GameObject police;
            if (policeOfficerPrefab != null)
            {
                police = Instantiate(policeOfficerPrefab, GetPoliceSpawnPosition(), Quaternion.identity);
            }
            else
            {
                police = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                police.name = "Police Officer Prototype";
                police.transform.position = GetPoliceSpawnPosition();
                police.transform.localScale = new Vector3(0.9f, 1.2f, 0.9f);

                var renderer = police.GetComponent<Renderer>();
                if (renderer != null)
                {
                    var material = new Material(Shader.Find("Standard"));
                    material.color = new Color(0.1f, 0.22f, 0.75f);
                    renderer.sharedMaterial = material;
                }
            }

            if (policeAnimator == null)
            {
                policeAnimator = police.GetComponentInChildren<Animator>();
            }

            return police.transform;
        }

        private void AddMaskVisual(Transform thief)
        {
            var mask = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mask.name = "Cute Mask";
            mask.transform.SetParent(thief, false);
            mask.transform.localPosition = new Vector3(0f, 0.45f, 0.47f);
            mask.transform.localScale = new Vector3(0.5f, 0.18f, 0.08f);

            var renderer = mask.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = Color.black;
                renderer.sharedMaterial = material;
            }
        }

        private IEnumerator MoveTransform(Transform target, Vector3 destination)
        {
            while (target != null && (target.position - destination).sqrMagnitude > 0.08f)
            {
                MoveOneStep(target, destination, policeMoveSpeed);
                yield return null;
            }
        }

        private static void MoveOneStep(Transform target, Vector3 destination, float speed)
        {
            var current = target.position;
            target.position = Vector3.MoveTowards(current, destination, speed * Time.deltaTime);

            var direction = destination - current;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                target.rotation = Quaternion.Slerp(
                    target.rotation,
                    Quaternion.LookRotation(direction, Vector3.up),
                    8f * Time.deltaTime);
            }
        }

        private Vector3 GetThiefSpawnPosition()
        {
            return thiefSpawnPoint != null ? thiefSpawnPoint.position : transform.position;
        }

        private Vector3 GetPoliceSpawnPosition()
        {
            return policeSpawnPoint != null ? policeSpawnPoint.position : transform.position + Vector3.left * 4f;
        }

        private Vector3 GetPoliceExitPosition()
        {
            return policeExitPoint != null ? policeExitPoint.position : GetPoliceSpawnPosition();
        }

        private void SetCallPoliceButton(bool enabled)
        {
            if (callPoliceButton != null)
            {
                callPoliceButton.gameObject.SetActive(enabled);
                callPoliceButton.interactable = enabled;
            }
        }

        private void PlayPoliceTrigger(int triggerHash)
        {
            if (policeAnimator != null && triggerHash != 0)
            {
                policeAnimator.SetTrigger(triggerHash);
            }
        }
    }
}
