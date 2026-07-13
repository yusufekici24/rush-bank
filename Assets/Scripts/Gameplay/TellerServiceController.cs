using UnityEngine;

namespace RushBank.Gameplay
{
    public class TellerServiceController : MonoBehaviour
    {
        [SerializeField] private CustomerQueueDirector queueDirector;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private bool autoCallNextCustomer = true;

        private float serviceTimer;

        public BankCustomer ActiveCustomer { get; private set; }
        public CustomerRequestDefinition ActiveRequest => ActiveCustomer != null ? ActiveCustomer.ActiveRequest : null;
        public bool HasActiveCustomer => ActiveCustomer != null;

        public BankCustomerEvent OnServiceStarted = new BankCustomerEvent();
        public UnityEngine.Events.UnityEvent OnServiceCompleted = new UnityEngine.Events.UnityEvent();
        public FloatEvent OnServiceTimerChanged = new FloatEvent();

        private void Start()
        {
            if (autoCallNextCustomer)
            {
                CallNextCustomer();
            }
        }

        private void Update()
        {
            if (!HasActiveCustomer)
            {
                if (autoCallNextCustomer)
                {
                    CallNextCustomer();
                }

                return;
            }

            serviceTimer += Time.deltaTime;
            OnServiceTimerChanged?.Invoke(serviceTimer);
        }

        public void CallNextCustomer()
        {
            if (queueDirector == null || HasActiveCustomer)
            {
                return;
            }

            ActiveCustomer = queueDirector.CallNextCustomer();
            if (ActiveCustomer == null)
            {
                return;
            }

            serviceTimer = 0f;
            OnServiceStarted?.Invoke(ActiveCustomer);
        }

        public void CompleteActiveRequest()
        {
            if (!HasActiveCustomer)
            {
                return;
            }

            scoreManager?.AwardRequestCompletion(ActiveRequest, serviceTimer);

            var completedCustomer = ActiveCustomer;
            ActiveCustomer = null;
            serviceTimer = 0f;

            if (completedCustomer != null)
            {
                Destroy(completedCustomer.gameObject);
            }

            OnServiceCompleted?.Invoke();

            if (autoCallNextCustomer)
            {
                CallNextCustomer();
            }
        }
    }
}
