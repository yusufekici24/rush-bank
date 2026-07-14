using System.Collections;
using RushBank.Core;
using UnityEngine;
using UnityEngine.Events;

namespace RushBank.Gameplay
{
    public enum BillType
    {
        Electricity,
        Water,
        Telephone
    }

    [System.Serializable]
    public struct BillDetails
    {
        public BillType type;
        public Sprite billIcon;
        public Color feedbackColor;
        public AudioClip scanSound;
    }

    public enum UtilityBillState
    {
        Idle,
        WaitingForCounterPickup,
        CarryingToScanner,
        ProcessingAtScanner,
        ReadyToDeliver
    }

    public class UtilityBillSystem : MonoBehaviour
    {
        private const string BillItemPrefix = "utility_bill_";

        [Header("Bill Types")]
        [SerializeField] private BillDetails[] billDetails =
        {
            new BillDetails { type = BillType.Electricity, feedbackColor = new Color(1f, 0.84f, 0.08f) },
            new BillDetails { type = BillType.Water, feedbackColor = new Color(0.18f, 0.58f, 1f) },
            new BillDetails { type = BillType.Telephone, feedbackColor = new Color(0.78f, 0.28f, 1f) }
        };

        [Header("Stations")]
        [SerializeField] private string counterTag = "Counter";
        [SerializeField] private string barcodeScannerTag = "BarcodeScanner";
        [SerializeField] private Transform holdPoint;
        [SerializeField] private QueueManager queueManager;

        [Header("Rewards")]
        [SerializeField, Min(0f)] private float rewardTimeSeconds = 4f;
        [SerializeField, Min(0)] private int rewardScore = 60;
        [SerializeField, Min(0.05f)] private float scanProcessingSeconds = 0.5f;

        [Header("Feedback")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private Renderer barcodeScannerRenderer;
        [SerializeField] private Light barcodeScannerLight;

        public UnityEvent<BillType> OnBillAssigned = new UnityEvent<BillType>();
        public UnityEvent<BillType> OnBillScanned = new UnityEvent<BillType>();
        public UnityEvent<BillType> OnBillCompleted = new UnityEvent<BillType>();
        public UnityEvent OnBillFailed = new UnityEvent();

        private UtilityBillState state = UtilityBillState.Idle;
        private QueueCustomer activeCustomer;
        private BillDetails activeBill;
        private GameObject activeBubble;
        private GameObject heldBillDocument;
        private GameObject nearbyStation;
        private Coroutine scanRoutine;
        private float actionTimeMultiplier = 1f;

        public UtilityBillState State => state;
        public BillType ActiveBillType => activeBill.type;
        public bool IsWorkflowActive => state != UtilityBillState.Idle;
        public float ActionTimeMultiplier
        {
            get => actionTimeMultiplier;
            set => actionTimeMultiplier = Mathf.Max(0.05f, value);
        }

        private void Awake()
        {
            if (queueManager == null)
            {
                queueManager = QueueManager.Instance != null ? QueueManager.Instance : FindFirstObjectByType<QueueManager>();
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (queueManager != null)
            {
                queueManager.OnCustomerCalled.AddListener(HandleCustomerCalled);
            }
        }

        private void OnDestroy()
        {
            if (queueManager != null)
            {
                queueManager.OnCustomerCalled.RemoveListener(HandleCustomerCalled);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                Interact();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsWorkflowStation(other))
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

        public void StartBillPayment(QueueCustomer customer)
        {
            if (customer == null || customer.RequestKind != CustomerRequestKind.BillPayment)
            {
                return;
            }

            activeCustomer = customer;
            activeBill = GetRandomBillDetails();
            SetState(UtilityBillState.WaitingForCounterPickup);
            ShowBillBubble(customer, activeBill);
            OnBillAssigned.Invoke(activeBill.type);
        }

        public void StartBillPayment(QueueCustomer customer, BillType forcedBillType)
        {
            if (customer == null || customer.RequestKind != CustomerRequestKind.BillPayment)
            {
                return;
            }

            activeCustomer = customer;
            activeBill = GetBillDetails(forcedBillType);
            SetState(UtilityBillState.WaitingForCounterPickup);
            ShowBillBubble(customer, activeBill);
            OnBillAssigned.Invoke(activeBill.type);
        }

        public void Interact()
        {
            EnsureActiveBillFromQueue();

            if (!IsWorkflowActive || scanRoutine != null)
            {
                return;
            }

            switch (state)
            {
                case UtilityBillState.WaitingForCounterPickup:
                    TryGrabBillAtCounter();
                    break;
                case UtilityBillState.CarryingToScanner:
                    TryScanBill();
                    break;
                case UtilityBillState.ReadyToDeliver:
                    TryDeliverProcessedBill();
                    break;
            }
        }

        private void HandleCustomerCalled(GameObject customerObject)
        {
            if (customerObject == null || !customerObject.TryGetComponent<QueueCustomer>(out var customer))
            {
                return;
            }

            if (customer.RequestKind == CustomerRequestKind.BillPayment)
            {
                StartBillPayment(customer);
            }
        }

        private void EnsureActiveBillFromQueue()
        {
            if (IsWorkflowActive || queueManager == null || queueManager.ActiveCustomer == null)
            {
                return;
            }

            if (queueManager.ActiveCustomer.RequestKind == CustomerRequestKind.BillPayment)
            {
                StartBillPayment(queueManager.ActiveCustomer);
            }
        }

        private void TryGrabBillAtCounter()
        {
            if (!IsAtStation(counterTag) || holdPoint == null || heldBillDocument != null)
            {
                Fail();
                return;
            }

            heldBillDocument = CreateBillDocument(activeBill);
            SetState(UtilityBillState.CarryingToScanner);
        }

        private void TryScanBill()
        {
            if (!IsAtStation(barcodeScannerTag) || heldBillDocument == null)
            {
                Fail();
                return;
            }

            scanRoutine = StartCoroutine(ScanRoutine());
        }

        private IEnumerator ScanRoutine()
        {
            SetState(UtilityBillState.ProcessingAtScanner);
            ApplyScannerFeedback(activeBill);

            if (audioSource != null && activeBill.scanSound != null)
            {
                audioSource.PlayOneShot(activeBill.scanSound);
            }

            var progress = 0f;
            while (progress < 1f)
            {
                var duration = Mathf.Max(0.05f, scanProcessingSeconds * actionTimeMultiplier);
                progress += Time.deltaTime / duration;
                yield return null;
            }

            MarkHeldBillProcessed();
            PlayCompletionEffect(activeCustomer != null ? activeCustomer.transform : transform, activeBill);
            OnBillScanned.Invoke(activeBill.type);
            SetState(UtilityBillState.ReadyToDeliver);
            scanRoutine = null;
        }

        private void TryDeliverProcessedBill()
        {
            if (!IsAtStation(counterTag) || heldBillDocument == null)
            {
                Fail();
                return;
            }

            DestroyHeldBill();
            ClearBillBubble();

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.AddTime(rewardTimeSeconds);
            }

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(rewardScore);
            }

            OnBillCompleted.Invoke(activeBill.type);

            if (queueManager != null && queueManager.ActiveCustomer == activeCustomer)
            {
                queueManager.CompleteActiveCustomer();
            }

            activeCustomer = null;
            SetState(UtilityBillState.Idle);
        }

        private GameObject CreateBillDocument(BillDetails bill)
        {
            var document = GameObject.CreatePrimitive(PrimitiveType.Cube);
            document.name = $"{bill.type} Utility Bill";
            document.transform.SetParent(holdPoint, false);
            document.transform.localPosition = Vector3.zero;
            document.transform.localRotation = Quaternion.identity;
            document.transform.localScale = new Vector3(0.52f, 0.04f, 0.34f);

            var renderer = document.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateMaterial(bill.feedbackColor);
            }

            var item = document.AddComponent<DeliverableItem>();
            item.Configure(GetBillItemId(bill.type), bill.feedbackColor);

            if (document.TryGetComponent<Collider>(out var collider))
            {
                collider.enabled = false;
            }

            var icon = new GameObject("Bill Type Label");
            icon.transform.SetParent(document.transform, false);
            icon.transform.localPosition = new Vector3(0f, 0.62f, 0f);
            icon.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var label = icon.AddComponent<TextMesh>();
            label.text = GetBillShortLabel(bill.type);
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.16f;
            label.fontStyle = FontStyle.Bold;
            label.color = Color.white;
            return document;
        }

        private void MarkHeldBillProcessed()
        {
            if (heldBillDocument == null)
            {
                return;
            }

            heldBillDocument.name = $"Processed {activeBill.type} Utility Bill";
            if (heldBillDocument.TryGetComponent<DeliverableItem>(out var item))
            {
                item.Configure(GetProcessedBillItemId(activeBill.type), activeBill.feedbackColor);
            }
        }

        private void ApplyScannerFeedback(BillDetails bill)
        {
            var renderer = barcodeScannerRenderer;
            if (renderer == null && nearbyStation != null)
            {
                renderer = nearbyStation.GetComponentInChildren<Renderer>();
            }

            if (renderer != null)
            {
                var material = renderer.material;
                material.color = bill.feedbackColor;
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", bill.feedbackColor * 1.35f);
            }

            if (barcodeScannerLight != null)
            {
                barcodeScannerLight.color = bill.feedbackColor;
                barcodeScannerLight.enabled = true;
            }

            SpawnScannerBurst(renderer != null ? renderer.transform : (nearbyStation != null ? nearbyStation.transform : transform), bill.feedbackColor);
        }

        private void SpawnScannerBurst(Transform target, Color color)
        {
            var effectObject = new GameObject("Barcode Scanner Bill Burst");
            effectObject.transform.position = target.position + Vector3.up * 0.65f;
            var particles = effectObject.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startColor = color;
            main.startLifetime = 0.42f;
            main.startSpeed = 1.4f;
            main.startSize = 0.08f;
            main.maxParticles = 28;
            var emission = particles.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)18) });
            particles.Play();
            Destroy(effectObject, 1.1f);
        }

        private void PlayCompletionEffect(Transform target, BillDetails bill)
        {
            if (target == null)
            {
                return;
            }

            var effectObject = new GameObject($"{bill.type} Bill Completion Effect");
            effectObject.transform.SetParent(target, false);
            effectObject.transform.localPosition = Vector3.up * 1.55f;
            var particles = effectObject.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startColor = bill.feedbackColor;
            main.startLifetime = 0.7f;
            main.startSpeed = bill.type == BillType.Water ? 0.65f : 1.05f;
            main.startSize = bill.type == BillType.Telephone ? 0.14f : 0.08f;
            main.maxParticles = 24;

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = bill.type == BillType.Telephone ? ParticleSystemShapeType.Circle : ParticleSystemShapeType.Cone;
            shape.radius = bill.type == BillType.Telephone ? 0.34f : 0.12f;

            var emission = particles.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)18) });
            particles.Play();
            Destroy(effectObject, 1.25f);
        }

        private void ShowBillBubble(QueueCustomer customer, BillDetails bill)
        {
            ClearBillBubble();

            if (customer == null)
            {
                return;
            }

            activeBubble = new GameObject($"{bill.type} Bill Bubble");
            activeBubble.transform.SetParent(customer.transform, false);
            activeBubble.transform.localPosition = Vector3.up * 2.05f;
            activeBubble.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);

            if (bill.billIcon != null)
            {
                var spriteRenderer = activeBubble.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = bill.billIcon;
                spriteRenderer.color = bill.feedbackColor;
                spriteRenderer.sortingOrder = 12;
                activeBubble.transform.localScale = Vector3.one * 0.6f;
                return;
            }

            var text = activeBubble.AddComponent<TextMesh>();
            text.text = GetBillBubbleLabel(bill.type);
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = 0.24f;
            text.fontStyle = FontStyle.Bold;
            text.color = bill.feedbackColor;
        }

        private void ClearBillBubble()
        {
            if (activeBubble != null)
            {
                Destroy(activeBubble);
                activeBubble = null;
            }
        }

        private BillDetails GetRandomBillDetails()
        {
            if (billDetails == null || billDetails.Length == 0)
            {
                return new BillDetails { type = BillType.Electricity, feedbackColor = new Color(1f, 0.84f, 0.08f) };
            }

            var details = billDetails[Random.Range(0, billDetails.Length)];
            if (details.feedbackColor == default(Color))
            {
                details.feedbackColor = GetFallbackColor(details.type);
            }

            return details;
        }

        private BillDetails GetBillDetails(BillType type)
        {
            if (billDetails != null)
            {
                for (var i = 0; i < billDetails.Length; i++)
                {
                    if (billDetails[i].type != type)
                    {
                        continue;
                    }

                    var details = billDetails[i];
                    if (details.feedbackColor == default(Color))
                    {
                        details.feedbackColor = GetFallbackColor(details.type);
                    }

                    return details;
                }
            }

            return new BillDetails { type = type, feedbackColor = GetFallbackColor(type) };
        }

        private bool IsWorkflowStation(Collider other)
        {
            var objectTag = other.gameObject.tag;
            return objectTag == counterTag || objectTag == barcodeScannerTag;
        }

        private bool IsAtStation(string stationTag)
        {
            return nearbyStation != null && nearbyStation.tag == stationTag;
        }

        private void DestroyHeldBill()
        {
            if (heldBillDocument == null)
            {
                return;
            }

            var bill = heldBillDocument;
            heldBillDocument = null;
            Destroy(bill);
        }

        private void SetState(UtilityBillState nextState)
        {
            state = nextState;
        }

        private void Fail()
        {
            OnBillFailed.Invoke();
        }

        private static string GetBillItemId(BillType type)
        {
            return BillItemPrefix + type.ToString().ToLowerInvariant();
        }

        private static string GetProcessedBillItemId(BillType type)
        {
            return GetBillItemId(type) + "_processed";
        }

        private static string GetBillShortLabel(BillType type)
        {
            return type switch
            {
                BillType.Water => "SU",
                BillType.Telephone => "TEL",
                _ => "ELEC"
            };
        }

        private static string GetBillBubbleLabel(BillType type)
        {
            return type switch
            {
                BillType.Water => "DROP",
                BillType.Telephone => "CALL",
                _ => "ZAP"
            };
        }

        private static Color GetFallbackColor(BillType type)
        {
            return type switch
            {
                BillType.Water => new Color(0.18f, 0.58f, 1f),
                BillType.Telephone => new Color(0.78f, 0.28f, 1f),
                _ => new Color(1f, 0.84f, 0.08f)
            };
        }

        private static Material CreateMaterial(Color color)
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = color;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 0.65f);
            return material;
        }
    }
}
