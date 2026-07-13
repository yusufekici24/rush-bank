using UnityEngine;

namespace RushBank.Gameplay
{
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private ScenarioRunner scenarioRunner;
        [SerializeField] private float interactionDistance = 3f;
        [SerializeField] private LayerMask interactableLayers = ~0;

        private void Awake()
        {
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (WasInteractionPressed())
            {
                TryInteract();
            }
        }

        public bool TryInteract()
        {
            if (playerCamera == null)
            {
                return false;
            }

            var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (!Physics.Raycast(ray, out var hit, interactionDistance, interactableLayers))
            {
                return false;
            }

            var interactable = hit.collider.GetComponentInParent<InteractableBase>();
            if (interactable == null)
            {
                return false;
            }

            interactable.Interact(new InteractionContext(gameObject, scenarioRunner));
            return true;
        }

        private static bool WasInteractionPressed()
        {
            if (Input.GetMouseButtonDown(0))
            {
                return true;
            }

            return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
        }
    }
}
