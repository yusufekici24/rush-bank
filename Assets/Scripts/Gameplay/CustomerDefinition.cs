using System.Collections.Generic;
using UnityEngine;

namespace RushBank.Gameplay
{
    [CreateAssetMenu(fileName = "Customer", menuName = "RushBank/Customer")]
    public class CustomerDefinition : ScriptableObject
    {
        [SerializeField] private string customerId = "customer_id";
        [SerializeField] private string displayName = "Customer";
        [SerializeField, TextArea] private string description = "Customer description.";
        [SerializeField, Min(0f)] private float patienceSeconds = 45f;
        [SerializeField] private List<CustomerRequestDefinition> possibleRequests = new List<CustomerRequestDefinition>();

        public string CustomerId => customerId;
        public string DisplayName => displayName;
        public string Description => description;
        public float PatienceSeconds => patienceSeconds;
        public IReadOnlyList<CustomerRequestDefinition> PossibleRequests => possibleRequests;

        public CustomerRequestDefinition PickRequest()
        {
            if (possibleRequests.Count == 0)
            {
                return null;
            }

            return possibleRequests[Random.Range(0, possibleRequests.Count)];
        }
    }
}
