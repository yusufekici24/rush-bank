using UnityEngine;

namespace RushBank.Gameplay
{
    [CreateAssetMenu(fileName = "CustomerRequest", menuName = "RushBank/Customer Request")]
    public class CustomerRequestDefinition : ScriptableObject
    {
        [SerializeField] private string requestId = "request_id";
        [SerializeField] private string displayName = "Customer Request";
        [SerializeField, TextArea] private string customerLine = "I need help with a bank transaction.";
        [SerializeField] private CustomerRequestType requestType = CustomerRequestType.Deposit;
        [SerializeField, Min(1f)] private float targetProcessingSeconds = 20f;
        [SerializeField, Min(0)] private int baseScore = 100;
        [SerializeField, Min(0)] private int scorePerRemainingSecond = 5;
        [SerializeField, Range(1, 5)] private int difficulty = 1;

        public string RequestId => requestId;
        public string DisplayName => displayName;
        public string CustomerLine => customerLine;
        public CustomerRequestType RequestType => requestType;
        public float TargetProcessingSeconds => targetProcessingSeconds;
        public int BaseScore => baseScore;
        public int ScorePerRemainingSecond => scorePerRemainingSecond;
        public int Difficulty => difficulty;
    }

    public enum CustomerRequestType
    {
        Deposit,
        Withdraw,
        OpenAccount,
        CardApplication,
        LoanApplication,
        BillPayment,
        UpdateInformation
    }
}
