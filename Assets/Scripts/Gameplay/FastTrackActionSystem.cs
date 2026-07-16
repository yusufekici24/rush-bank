using System.Collections;
using System.Collections.Generic;
using RushBank.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RushBank.Gameplay
{
    public enum FastTrackTaskType
    {
        PassbookPrinting,
        CardActivation
    }

    [System.Serializable]
    public class FastTrackTaskDefinition
    {
        public FastTrackTaskType taskType = FastTrackTaskType.PassbookPrinting;
        public string displayName = "Passbook Printing";
        public GameObject outputItemPrefab;
        public float processingSeconds = 0.8f;
        public float bonusTime = 4f;
        public int score = 50;
    }

    public class FastTrackActionSystem : MonoBehaviour
    {
        [Header("Tasks")]
        [SerializeField] private List<FastTrackTaskDefinition> tasks = new List<FastTrackTaskDefinition>();
        [SerializeField] private FastTrackTaskType activeTaskType = FastTrackTaskType.PassbookPrinting;

        [Header("Stations")]
        [SerializeField] private string passbookPrinterTag = "PassbookPrinter";
        [SerializeField] private string counterTag = "Counter";

        [Header("Carry")]
        [SerializeField] private Transform holdPoint;

        [Header("Feedback")]
        [SerializeField] private Slider processingProgressBar;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip printingSound;

        public UnityEvent<FastTrackTaskType> OnFastTrackStarted = new UnityEvent<FastTrackTaskType>();
        public UnityEvent<FastTrackTaskType> OnFastTrackCompleted = new UnityEvent<FastTrackTaskType>();
        public UnityEvent<FastTrackTaskType> OnFastTrackFailed = new UnityEvent<FastTrackTaskType>();

        private GameObject nearbyStation;
        private GameObject heldItem;
        private Coroutine processingRoutine;
        private float actionTimeMultiplier = 1f;

        public bool IsProcessing => processingRoutine != null;
        public bool IsHoldingItem => heldItem != null;
        public float ActionTimeMultiplier
        {
            get => actionTimeMultiplier;
            set => actionTimeMultiplier = Mathf.Max(0.05f, value);
        }

        private void Awake()
        {
            if (tasks.Count == 0)
            {
                tasks.Add(new FastTrackTaskDefinition());
            }

            SetProgress(0f, false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsFastTrackStation(other))
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

        public void SetActiveTask(FastTrackTaskType taskType)
        {
            activeTaskType = taskType;
        }

        public void Interact()
        {
            if (IsProcessing)
            {
                return;
            }

            if (heldItem != null)
            {
                TryDeliverToCustomer();
                return;
            }

            TryStartProcessing();
        }

        public void TryStartProcessing()
        {
            if (!IsAtStation(passbookPrinterTag) || holdPoint == null)
            {
                Fail();
                return;
            }

            var task = GetActiveTask();
            if (task == null)
            {
                Fail();
                return;
            }

            processingRoutine = StartCoroutine(ProcessRoutine(task));
        }

        public void TryDeliverToCustomer()
        {
            if (!IsAtStation(counterTag) || heldItem == null)
            {
                Fail();
                return;
            }

            var task = GetActiveTask();
            DestroyHeldItem();

            if (task != null)
            {
                if (TimeManager.Instance != null)
                {
                    TimeManager.Instance.AddTime(task.bonusTime);
                }

                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.AddScore(task.score);
                }

                OnFastTrackCompleted.Invoke(task.taskType);
            }
        }

        public void CancelWorkflow()
        {
            if (processingRoutine != null)
            {
                StopCoroutine(processingRoutine);
                processingRoutine = null;
            }

            SetProgress(0f, false);
            DestroyHeldItem();
            OnFastTrackFailed.Invoke(activeTaskType);
        }

        private IEnumerator ProcessRoutine(FastTrackTaskDefinition task)
        {
            OnFastTrackStarted.Invoke(task.taskType);
            SetProgress(0f, true);

            if (audioSource != null && printingSound != null)
            {
                audioSource.PlayOneShot(printingSound);
            }

            var progress = 0f;
            if (AccountOpeningSystem.TryConsumeQuickBoostCharge())
            {
                progress = 1f;
                SetProgress(1f, true);
            }

            while (progress < 1f)
            {
                var duration = Mathf.Max(0.05f, task.processingSeconds * actionTimeMultiplier);
                progress += Time.deltaTime / duration;
                SetProgress(progress, true);
                yield return null;
            }

            SetProgress(0f, false);
            heldItem = CreateOutputItem(task);
            processingRoutine = null;
        }

        private GameObject CreateOutputItem(FastTrackTaskDefinition task)
        {
            GameObject item;
            if (task.outputItemPrefab != null)
            {
                item = Instantiate(task.outputItemPrefab, holdPoint);
            }
            else
            {
                item = GameObject.CreatePrimitive(PrimitiveType.Cube);
                item.transform.SetParent(holdPoint, false);
                item.transform.localScale = new Vector3(0.45f, 0.06f, 0.28f);

                var renderer = item.GetComponent<Renderer>();
                if (renderer != null)
                {
                    var material = new Material(Shader.Find("Standard"));
                    material.color = new Color(0.76f, 0.92f, 1f);
                    renderer.sharedMaterial = material;
                }
            }

            item.name = task.taskType == FastTrackTaskType.PassbookPrinting ? "Passbook" : "Activated Card";
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;

            var colliders = item.GetComponentsInChildren<Collider>();
            for (var i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }

            if (item.TryGetComponent<Rigidbody>(out var body))
            {
                body.isKinematic = true;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            return item;
        }

        private void DestroyHeldItem()
        {
            if (heldItem == null)
            {
                return;
            }

            var item = heldItem;
            heldItem = null;
            Destroy(item);
        }

        private FastTrackTaskDefinition GetActiveTask()
        {
            for (var i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                if (task != null && task.taskType == activeTaskType)
                {
                    return task;
                }
            }

            return null;
        }

        private bool IsFastTrackStation(Collider other)
        {
            var objectTag = other.gameObject.tag;
            return objectTag == passbookPrinterTag || objectTag == counterTag;
        }

        private bool IsAtStation(string stationTag)
        {
            return nearbyStation != null && nearbyStation.tag == stationTag;
        }

        private void SetProgress(float value, bool visible)
        {
            if (processingProgressBar == null)
            {
                return;
            }

            processingProgressBar.value = Mathf.Clamp01(value);
            processingProgressBar.gameObject.SetActive(visible);
        }

        private void Fail()
        {
            OnFastTrackFailed.Invoke(activeTaskType);
        }
    }
}
