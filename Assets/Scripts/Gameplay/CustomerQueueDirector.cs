using System.Collections.Generic;
using UnityEngine;

namespace RushBank.Gameplay
{
    public class CustomerQueueDirector : MonoBehaviour
    {
        [SerializeField] private BankCustomer customerPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform queueStartPoint;
        [SerializeField] private Transform servicePoint;
        [SerializeField] private Vector3 queueDirection = Vector3.back;
        [SerializeField, Min(0.5f)] private float queueSpacing = 1.3f;
        [SerializeField, Min(0.1f)] private float arrivalIntervalSeconds = 8f;
        [SerializeField] private List<CustomerDefinition> customerSequence = new List<CustomerDefinition>();

        private readonly Queue<BankCustomer> waitingCustomers = new Queue<BankCustomer>();
        private float nextArrivalTimer;
        private int nextCustomerIndex;

        public IntEvent OnQueueCountChanged = new IntEvent();
        public BankCustomerEvent OnCustomerArrived = new BankCustomerEvent();
        public BankCustomerEvent OnCustomerReadyForService = new BankCustomerEvent();

        public int WaitingCount => waitingCustomers.Count;

        private void OnEnable()
        {
            nextArrivalTimer = arrivalIntervalSeconds;
        }

        private void Update()
        {
            if (nextCustomerIndex >= customerSequence.Count)
            {
                return;
            }

            nextArrivalTimer -= Time.deltaTime;
            if (nextArrivalTimer > 0f)
            {
                return;
            }

            SpawnNextCustomer();
            nextArrivalTimer = arrivalIntervalSeconds;
        }

        public void SpawnNextCustomer()
        {
            if (nextCustomerIndex >= customerSequence.Count)
            {
                return;
            }

            var definition = customerSequence[nextCustomerIndex];
            nextCustomerIndex++;

            var customer = CreateCustomer(definition);
            waitingCustomers.Enqueue(customer);
            RefreshQueuePositions();
            OnCustomerArrived?.Invoke(customer);
            OnQueueCountChanged?.Invoke(waitingCustomers.Count);
        }

        public BankCustomer CallNextCustomer()
        {
            if (waitingCustomers.Count == 0)
            {
                return null;
            }

            var customer = waitingCustomers.Dequeue();
            if (servicePoint != null)
            {
                customer.transform.position = servicePoint.position;
                customer.transform.rotation = servicePoint.rotation;
            }

            RefreshQueuePositions();
            OnQueueCountChanged?.Invoke(waitingCustomers.Count);
            OnCustomerReadyForService?.Invoke(customer);
            return customer;
        }

        private BankCustomer CreateCustomer(CustomerDefinition definition)
        {
            BankCustomer customer;
            if (customerPrefab != null)
            {
                customer = Instantiate(customerPrefab, GetSpawnPosition(), GetSpawnRotation());
            }
            else
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = "Customer";
                go.transform.SetPositionAndRotation(GetSpawnPosition(), GetSpawnRotation());
                customer = go.AddComponent<BankCustomer>();

                var renderer = go.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = CreateCustomerMaterial(definition);
                }
            }

            customer.Initialize(definition, definition?.PickRequest());
            return customer;
        }

        private void RefreshQueuePositions()
        {
            var index = 0;
            var normalizedDirection = queueDirection.sqrMagnitude > 0f ? queueDirection.normalized : Vector3.back;
            foreach (var customer in waitingCustomers)
            {
                if (customer == null)
                {
                    continue;
                }

                var queuePosition = GetQueueStartPosition() + (normalizedDirection * queueSpacing * index);
                customer.transform.position = queuePosition;
                customer.transform.rotation = GetSpawnRotation();
                index++;
            }
        }

        private Vector3 GetSpawnPosition()
        {
            return spawnPoint != null ? spawnPoint.position : transform.position;
        }

        private Vector3 GetQueueStartPosition()
        {
            return queueStartPoint != null ? queueStartPoint.position : GetSpawnPosition();
        }

        private Quaternion GetSpawnRotation()
        {
            return spawnPoint != null ? spawnPoint.rotation : transform.rotation;
        }

        private static Material CreateCustomerMaterial(CustomerDefinition definition)
        {
            var shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            var material = new Material(shader);
            var seed = definition != null ? Mathf.Abs(definition.CustomerId.GetHashCode()) : 0;
            var hue = (seed % 100) / 100f;
            material.color = Color.HSVToRGB(hue, 0.45f, 0.86f);
            return material;
        }
    }
}
