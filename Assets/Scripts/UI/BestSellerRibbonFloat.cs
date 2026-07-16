using UnityEngine;

namespace RushBank.UI
{
    public class BestSellerRibbonFloat : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float bobAmplitude = 8f;
        [SerializeField, Min(0.01f)] private float bobSpeed = 2.2f;
        [SerializeField, Min(0f)] private float tiltAmplitude = 4f;

        private RectTransform rectTransform;
        private Vector2 startPosition;
        private Quaternion startRotation;
        private float timeOffset;

        private void Awake()
        {
            rectTransform = transform as RectTransform;
            if (rectTransform == null)
            {
                enabled = false;
                return;
            }

            startPosition = rectTransform.anchoredPosition;
            startRotation = rectTransform.localRotation;
            timeOffset = Random.value * Mathf.PI * 2f;
        }

        private void OnEnable()
        {
            if (rectTransform == null)
            {
                return;
            }

            startPosition = rectTransform.anchoredPosition;
            startRotation = rectTransform.localRotation;
        }

        private void Update()
        {
            var wave = Mathf.Sin((Time.unscaledTime + timeOffset) * bobSpeed);
            rectTransform.anchoredPosition = startPosition + Vector2.up * (wave * bobAmplitude);
            rectTransform.localRotation = startRotation * Quaternion.Euler(0f, 0f, wave * tiltAmplitude);
        }
    }
}
