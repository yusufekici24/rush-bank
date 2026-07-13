using System;
using UnityEngine;
using UnityEngine.Events;

namespace RushBank.Gameplay
{
    public class ScenarioRunner : MonoBehaviour
    {
        [SerializeField] private ScenarioDefinition startingScenario;

        public event Action<ScenarioStepDefinition> StepStarted;
        public event Action<ScenarioDefinition> ScenarioCompleted;

        public ScenarioDefinition ActiveScenario { get; private set; }
        public int CurrentStepIndex { get; private set; } = -1;
        public bool IsRunning => ActiveScenario != null && CurrentStepIndex >= 0;
        public ScenarioStepDefinition CurrentStep => IsRunning ? ActiveScenario.Steps[CurrentStepIndex] : null;

        [Header("Unity Events")]
        public StringEvent OnStepInstructionChanged = new StringEvent();
        public UnityEvent OnScenarioCompleted = new UnityEvent();

        private void Start()
        {
            if (startingScenario != null)
            {
                Begin(startingScenario);
            }
        }

        public void Begin(ScenarioDefinition scenario)
        {
            if (scenario == null || scenario.Steps.Count == 0)
            {
                Debug.LogWarning("ScenarioRunner could not start an empty scenario.");
                return;
            }

            ActiveScenario = scenario;
            CurrentStepIndex = 0;
            RaiseStepStarted();
        }

        public bool TryCompleteObjective(ScenarioObjectiveType objectiveType, string targetId)
        {
            if (!IsRunning)
            {
                return false;
            }

            var step = CurrentStep;
            if (step.ObjectiveType != objectiveType || step.TargetId != targetId)
            {
                return false;
            }

            Advance();
            return true;
        }

        public void Advance()
        {
            if (!IsRunning)
            {
                return;
            }

            CurrentStepIndex++;
            if (CurrentStepIndex >= ActiveScenario.Steps.Count)
            {
                CompleteScenario();
                return;
            }

            RaiseStepStarted();
        }

        private void RaiseStepStarted()
        {
            var step = CurrentStep;
            StepStarted?.Invoke(step);
            OnStepInstructionChanged?.Invoke(step.Instruction);
        }

        private void CompleteScenario()
        {
            var completedScenario = ActiveScenario;
            ActiveScenario = null;
            CurrentStepIndex = -1;

            ScenarioCompleted?.Invoke(completedScenario);
            OnScenarioCompleted?.Invoke();
        }
    }
}
