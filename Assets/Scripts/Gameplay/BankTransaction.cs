using System.Collections.Generic;
using UnityEngine;

namespace RushBank.Gameplay
{
    public enum BankTransactionFlow
    {
        FastTrack,
        MiniGame,
        CashVault,
        CurrencyExchange,
        GoldExchange,
        CreditApproval,
        VipSafeEscort
    }

    public enum BankTransactionDifficulty
    {
        VeryEasy,
        Easy,
        Medium,
        Hard,
        VeryHard
    }

    [CreateAssetMenu(fileName = "BankTransaction", menuName = "RushBank/Bank Transaction")]
    public class BankTransaction : ScriptableObject
    {
        [Header("Core")]
        [SerializeField] private string transactionName = "Bank Transaction";
        [SerializeField, Min(0f)] private float baseTimeReward = 4f;
        [SerializeField] private Sprite requestIcon;
        [SerializeField] private GameObject itemPrefabNeeded;
        [SerializeField] private string stationRoute = "Counter";
        [SerializeField] private BankTransactionDifficulty difficulty = BankTransactionDifficulty.VeryEasy;
        [SerializeField, TextArea] private string gameplayRole;
        [SerializeField, TextArea] private string flowSummary;
        [SerializeField] private string[] workflowSteps;
        [SerializeField] private string feedbackCue;

        [Header("Prototype Workflow")]
        [SerializeField] private BankTransactionFlow flow = BankTransactionFlow.FastTrack;
        [SerializeField, Min(0f)] private float processingSeconds = 0.8f;
        [SerializeField, Min(0f)] private float customerSignatureSeconds;
        [SerializeField, Min(0f)] private float managerApprovalSeconds;
        [SerializeField, Min(0f)] private float expertiseEvaluationSeconds;
        [SerializeField] private bool requiresDocumentDesk;
        [SerializeField] private bool requiresManagerApproval;
        [SerializeField] private bool requiresExpertiseDesk;

        public string TransactionName => transactionName;
        public float BaseTimeReward => baseTimeReward;
        public Sprite RequestIcon => requestIcon;
        public GameObject ItemPrefabNeeded => itemPrefabNeeded;
        public string StationRoute => stationRoute;
        public BankTransactionDifficulty Difficulty => difficulty;
        public string GameplayRole => gameplayRole;
        public string FlowSummary => flowSummary;
        public IReadOnlyList<string> WorkflowSteps => workflowSteps ?? System.Array.Empty<string>();
        public string FeedbackCue => feedbackCue;
        public BankTransactionFlow Flow => flow;
        public float ProcessingSeconds => processingSeconds;
        public float CustomerSignatureSeconds => customerSignatureSeconds;
        public float ManagerApprovalSeconds => managerApprovalSeconds;
        public float ExpertiseEvaluationSeconds => expertiseEvaluationSeconds;
        public bool RequiresDocumentDesk => requiresDocumentDesk;
        public bool RequiresManagerApproval => requiresManagerApproval;
        public bool RequiresExpertiseDesk => requiresExpertiseDesk;
    }
}
