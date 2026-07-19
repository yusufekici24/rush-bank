using UnityEngine;

namespace RushBank.Core
{
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class PortraitCameraFitter : MonoBehaviour
    {
        // Dikey (9:16 ve daha uzun) ekranlarda şube genişliğinin tamamı kadrajda kalsın diye
        // orthographic size'ı aspect'e göre büyütür; geniş editör görünümünde min değere iner.
        [SerializeField, Min(0.1f)] private float targetHalfWidth = 6.2f;
        [SerializeField, Min(0.1f)] private float minOrthographicSize = 8.65f;

        private Camera targetCamera;

        public float TargetHalfWidth
        {
            get => targetHalfWidth;
            set
            {
                targetHalfWidth = Mathf.Max(0.1f, value);
                Apply();
            }
        }

        private void OnEnable()
        {
            targetCamera = GetComponent<Camera>();
            Apply();
        }

        private void Update()
        {
            // Aspect hem cihaz rotasyonunda hem editör Game view değişiminde güncellenebilir.
            Apply();
        }

        private void Apply()
        {
            if (targetCamera == null || !targetCamera.orthographic)
            {
                return;
            }

            var aspect = targetCamera.aspect;
            if (aspect <= 0.0001f)
            {
                return;
            }

            var requiredSize = targetHalfWidth / aspect;
            targetCamera.orthographicSize = Mathf.Max(minOrthographicSize, requiredSize);
        }
    }
}
