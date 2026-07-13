using UnityEngine;

namespace RushBank.Gameplay
{
    public class ScenarioObjectiveInteractable : InteractableBase
    {
        [SerializeField] private ScenarioObjectiveType objectiveType = ScenarioObjectiveType.InteractWith;

        public override void Interact(InteractionContext context)
        {
            if (context.ScenarioRunner == null)
            {
                return;
            }

            context.ScenarioRunner.TryCompleteObjective(objectiveType, InteractionId);
        }
    }
}
