using UnityEngine;

namespace RushBank.Gameplay
{
    public class BankCustomer : MonoBehaviour
    {
        [SerializeField] private CustomerDefinition definition;
        [SerializeField] private CustomerRequestDefinition activeRequest;

        public CustomerDefinition Definition => definition;
        public CustomerRequestDefinition ActiveRequest => activeRequest;

        public void Initialize(CustomerDefinition customerDefinition, CustomerRequestDefinition request)
        {
            definition = customerDefinition;
            activeRequest = request != null ? request : definition?.PickRequest();
            gameObject.name = definition != null ? $"Customer - {definition.DisplayName}" : "Customer";
        }
    }
}
