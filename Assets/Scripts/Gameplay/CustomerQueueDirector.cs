using System.Collections.Generic;
using UnityEngine;

namespace RushBank.Gameplay
{
    public class CustomerQueueDirector : MonoBehaviour
    {
        [SerializeField] private BankCustomer customerPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform servicePoint;
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
                var go = new GameObject("Customer");
                go.transform.SetPositionAndRotation(GetSpawnPosition(), GetSpawnRotation());
                customer = go.AddComponent<BankCustomer>();
            }

            customer.Initialize(definition, definition?.PickRequest());
            return customer;
        }

        private Vector3 GetSpawnPosition()
        {
            return spawnPoint != null ? spawnPoint.position : transform.position;
        }

        private Quaternion GetSpawnRotation()
        {
            return spawnPoint != null ? spawnPoint.rotation : transform.rotation;
        }
    }
}
