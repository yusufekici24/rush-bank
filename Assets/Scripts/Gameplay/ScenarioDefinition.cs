using System;
using System.Collections.Generic;
using UnityEngine;

namespace RushBank.Gameplay
{
    [CreateAssetMenu(fileName = "Scenario", menuName = "RushBank/Scenario")]
    public class ScenarioDefinition : ScriptableObject
    {
        [SerializeField] private string scenarioId = "scenario_id";
        [SerializeField] private string title = "Scenario Title";
        [SerializeField, TextArea] private string description = "Scenario description.";
        [SerializeField] private List<ScenarioStepDefinition> steps = new List<ScenarioStepDefinition>();

        public string ScenarioId => scenarioId;
        public string Title => title;
        public string Description => description;
        public IReadOnlyList<ScenarioStepDefinition> Steps => steps;
    }

    [Serializable]
    public class ScenarioStepDefinition
    {
        [SerializeField] private string stepId = "step_id";
        [SerializeField] private string title = "Step Title";
        [SerializeField, TextArea] private string instruction = "Step instruction.";
        [SerializeField] private ScenarioObjectiveType objectiveType = ScenarioObjectiveType.InteractWith;
        [SerializeField] private string targetId = "target_id";

        public string StepId => stepId;
        public string Title => title;
        public string Instruction => instruction;
        public ScenarioObjectiveType ObjectiveType => objectiveType;
        public string TargetId => targetId;
    }

    public enum ScenarioObjectiveType
    {
        NavigateTo,
        InteractWith,
        WaitForTurn,
        ProcessTransaction,
        CompleteDialogue
    }
}
