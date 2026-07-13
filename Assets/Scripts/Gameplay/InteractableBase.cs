using UnityEngine;

namespace RushBank.Gameplay
{
    public abstract class InteractableBase : MonoBehaviour
    {
        [SerializeField] private string interactionId = "interaction_id";
        [SerializeField] private string displayName = "Interactable";

        public string InteractionId => interactionId;
        public string DisplayName => displayName;

        public virtual void Interact(InteractionContext context)
        {
            context.ScenarioRunner?.TryCompleteObjective(ScenarioObjectiveType.InteractWith, interactionId);
        }
    }
}
