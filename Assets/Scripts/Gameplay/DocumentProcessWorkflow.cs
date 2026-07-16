using System.Collections;
using RushBank.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RushBank.Gameplay
{
    public enum DocumentProcessState
    {
        RequestForm,
        WaitCustomerSignature,
        SendToManagerApproval,
        DeliverToCustomer
    }

    public class DocumentProcessWorkflow : MonoBehaviour
    {
        [Header("Stations")]
        [SerializeField] private string documentDeskTag = "DocumentDesk";
        [SerializeField] private string counterTag = "Counter";
        [SerializeField] private string managerDeskTag = "ManagerDesk";

        [Header("Carry")]
        [SerializeField] private Transform holdPoint;
        [SerializeField] private GameObject blankApplicationFormPrefab;
        [SerializeField] private GameObject signedDocumentPrefab;
        [SerializeField] private GameObject approvedDocumentPrefab;

        [Header("Progress UI")]
        [SerializeField] private Slider signatureProgressBar;
        [SerializeField] private GameObject penScribbleEffect;
        [SerializeField] private Slider managerApprovalProgressBar;
        [SerializeField] private GameObject managerStampEffect;

        [Header("Rewards")]
        [SerializeField, Min(0f)] private float signatureDurationSeconds = 1.5f;
        [SerializeField, Min(0f)] private float managerApprovalSeconds = 2f;
        [SerializeField, Min(0f)] private float completionBonusTime = 12f;
        [SerializeField, Min(0)] private int completionPoints = 250;

        public UnityEvent<DocumentProcessState> OnStateChanged = new UnityEvent<DocumentProcessState>();
        public UnityEvent OnWorkflowCompleted = new UnityEvent();
        public UnityEvent OnWorkflowFailed = new UnityEvent();

        private DocumentProcessState currentState = DocumentProcessState.RequestForm;
        private GameObject nearbyStation;
        private GameObject heldDocument;
        private Coroutine activeRoutine;
        private bool workflowActive;
        private float actionTimeMultiplier = 1f;

        public DocumentProcessState CurrentState => currentState;
        public bool IsWorkflowActive => workflowActive;
        public float ActionTimeMultiplier
        {
            get => actionTimeMultiplier;
            set => actionTimeMultiplier = Mathf.Max(0.05f, value);
        }

        private void Awake()
        {
            SetProgress(signatureProgressBar, 0f, false);
            SetProgress(managerApprovalProgressBar, 0f, false);

            if (penScribbleEffect != null)
            {
                penScribbleEffect.SetActive(false);
            }

            if (managerStampEffect != null)
            {
                managerStampEffect.SetActive(false);
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

        public void StartWorkflow()
        {
            workflowActive = true;
            SetState(DocumentProcessState.RequestForm);
        }

        public void Interact()
        {
            if (!workflowActive)
            {
                StartWorkflow();
            }

            switch (currentState)
            {
                case DocumentProcessState.RequestForm:
                    TryGrabBlankForm();
                    break;
                case DocumentProcessState.WaitCustomerSignature:
                    TryStartCustomerSignature();
                    break;
                case DocumentProcessState.SendToManagerApproval:
                    TryStartManagerApprovalOrGrabApprovedDocument();
                    break;
                case DocumentProcessState.DeliverToCustomer:
                    TryDeliverApprovedDocument();
                    break;
            }
        }

        public void CancelWorkflow()
        {
            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
                activeRoutine = null;
            }

            DestroyHeldDocument();
            SetProgress(signatureProgressBar, 0f, false);
            SetProgress(managerApprovalProgressBar, 0f, false);

            if (penScribbleEffect != null)
            {
                penScribbleEffect.SetActive(false);
            }

            if (managerStampEffect != null)
            {
                managerStampEffect.SetActive(false);
            }

            workflowActive = false;
            SetState(DocumentProcessState.RequestForm);
            OnWorkflowFailed.Invoke();
        }

        private void TryGrabBlankForm()
        {
            if (!IsAtStation(documentDeskTag) || holdPoint == null || heldDocument != null)
            {
                Fail();
                return;
            }

            heldDocument = CreateDocument(blankApplicationFormPrefab, "Blank Application Form", Color.white);
            SetState(DocumentProcessState.WaitCustomerSignature);
        }

        private void TryStartCustomerSignature()
        {
            if (!IsAtStation(counterTag) || heldDocument == null || activeRoutine != null)
            {
                Fail();
                return;
            }

            activeRoutine = StartCoroutine(CustomerSignatureRoutine());
        }

        private void TryStartManagerApprovalOrGrabApprovedDocument()
        {
            if (!IsAtStation(managerDeskTag))
            {
                Fail();
                return;
            }

            if (heldDocument != null && activeRoutine == null)
            {
                activeRoutine = StartCoroutine(ManagerApprovalRoutine());
                return;
            }

            if (heldDocument == null)
            {
                heldDocument = CreateDocument(approvedDocumentPrefab, "Approved Document", Color.green);
                SetState(DocumentProcessState.DeliverToCustomer);
            }
        }

        private void TryDeliverApprovedDocument()
        {
            if (!IsAtStation(counterTag) || heldDocument == null)
            {
                Fail();
                return;
            }

            DestroyHeldDocument();

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.AddTime(completionBonusTime);
            }

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(completionPoints);
            }

            workflowActive = false;
            OnWorkflowCompleted.Invoke();
            SetState(DocumentProcessState.RequestForm);
        }

        private IEnumerator CustomerSignatureRoutine()
        {
            SetProgress(signatureProgressBar, 0f, true);
            if (penScribbleEffect != null)
            {
                penScribbleEffect.SetActive(true);
            }

            var progress = 0f;
            while (progress < 1f)
            {
                var duration = Mathf.Max(0.05f, signatureDurationSeconds * actionTimeMultiplier);
                progress += Time.deltaTime / duration;
                SetProgress(signatureProgressBar, progress, true);
                yield return null;
            }

            if (penScribbleEffect != null)
            {
                penScribbleEffect.SetActive(false);
            }

            SetProgress(signatureProgressBar, 0f, false);
            DestroyHeldDocument();
            heldDocument = CreateDocument(signedDocumentPrefab, "Signed Document", new Color(1f, 0.88f, 0.25f));
            SetState(DocumentProcessState.SendToManagerApproval);
            activeRoutine = null;
        }

        private IEnumerator ManagerApprovalRoutine()
        {
            SetProgress(managerApprovalProgressBar, 0f, true);
            if (managerStampEffect != null)
            {
                managerStampEffect.SetActive(true);
            }

            var placedDocument = heldDocument;
            heldDocument = null;
            placedDocument.transform.SetParent(nearbyStation.transform, true);
            placedDocument.transform.position = nearbyStation.transform.position + Vector3.up * 0.85f;

            var progress = 0f;
            while (progress < 1f)
            {
                var duration = Mathf.Max(0.05f, managerApprovalSeconds * actionTimeMultiplier);
                progress += Time.deltaTime / duration;
                SetProgress(managerApprovalProgressBar, progress, true);
                yield return null;
            }

            if (managerStampEffect != null)
            {
                managerStampEffect.SetActive(false);
            }

            SetProgress(managerApprovalProgressBar, 0f, false);
            Destroy(placedDocument);
            heldDocument = CreateDocument(approvedDocumentPrefab, "Approved Document", Color.green);
            SetState(DocumentProcessState.DeliverToCustomer);
            activeRoutine = null;
        }

        private GameObject CreateDocument(GameObject prefab, string documentName, Color fallbackColor)
        {
            GameObject document;
            if (prefab != null)
            {
                document = Instantiate(prefab, holdPoint);
            }
            else
            {
                document = GameObject.CreatePrimitive(PrimitiveType.Cube);
                document.transform.SetParent(holdPoint, false);
                document.transform.localScale = new Vector3(0.5f, 0.04f, 0.35f);

                var renderer = document.GetComponent<Renderer>();
                if (renderer != null)
                {
                    var material = new Material(Shader.Find("Standard"));
                    material.color = fallbackColor;
                    renderer.sharedMaterial = material;
                }
            }

            document.name = documentName;
            document.transform.localPosition = Vector3.zero;
            document.transform.localRotation = Quaternion.identity;

            if (document.TryGetComponent<Rigidbody>(out var body))
            {
                body.isKinematic = true;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            var colliders = document.GetComponentsInChildren<Collider>();
            for (var i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }

            return document;
        }

        private void DestroyHeldDocument()
        {
            if (heldDocument == null)
            {
                return;
            }

            var document = heldDocument;
            heldDocument = null;
            Destroy(document);
        }

        private bool IsWorkflowStation(Collider other)
        {
            var objectTag = other.gameObject.tag;
            return objectTag == documentDeskTag || objectTag == counterTag || objectTag == managerDeskTag;
        }

        private bool IsAtStation(string stationTag)
        {
            return nearbyStation != null && nearbyStation.tag == stationTag;
        }

        private void SetState(DocumentProcessState state)
        {
            currentState = state;
            OnStateChanged.Invoke(currentState);
        }

        private void Fail()
        {
            OnWorkflowFailed.Invoke();
        }

        private static void SetProgress(Slider slider, float value, bool visible)
        {
            if (slider == null)
            {
                return;
            }

            slider.value = Mathf.Clamp01(value);
            slider.gameObject.SetActive(visible);
        }
    }
}
