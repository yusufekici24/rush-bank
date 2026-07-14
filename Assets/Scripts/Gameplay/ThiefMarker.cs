using UnityEngine;

namespace RushBank.Gameplay
{
    public class ThiefMarker : MonoBehaviour
    {
        [SerializeField] private GameObject maskVisual;

        public void SetMaskVisual(GameObject visual)
        {
            maskVisual = visual;
        }

        private void OnEnable()
        {
            if (maskVisual != null)
            {
                maskVisual.SetActive(true);
            }
        }
    }
}
