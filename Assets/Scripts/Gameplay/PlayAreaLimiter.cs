using UnityEngine;

namespace RushBank.Gameplay
{
    public class PlayAreaLimiter : MonoBehaviour
    {
        // Karakteri şube iç alanında tutar; duvar collider'ları delinse bile
        // oyuncu zemin dışına veya kamera arkasına çıkamaz.
        [SerializeField] private Vector3 areaCenter = new Vector3(0f, 1f, -4.05f);
        [SerializeField] private Vector3 areaSize = new Vector3(10.8f, 4f, 16.8f);

        private Rigidbody body;

        public void SetArea(Vector3 center, Vector3 size)
        {
            areaCenter = center;
            areaSize = size;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            var halfX = areaSize.x * 0.5f;
            var halfZ = areaSize.z * 0.5f;
            var position = body != null ? body.position : transform.position;

            var clamped = position;
            clamped.x = Mathf.Clamp(clamped.x, areaCenter.x - halfX, areaCenter.x + halfX);
            clamped.z = Mathf.Clamp(clamped.z, areaCenter.z - halfZ, areaCenter.z + halfZ);
            if (clamped == position)
            {
                return;
            }

            if (body != null)
            {
                var velocity = body.linearVelocity;
                if (!Mathf.Approximately(clamped.x, position.x))
                {
                    velocity.x = 0f;
                }

                if (!Mathf.Approximately(clamped.z, position.z))
                {
                    velocity.z = 0f;
                }

                body.linearVelocity = velocity;
                body.position = clamped;
            }
            else
            {
                transform.position = clamped;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.4f, 0.6f);
            Gizmos.DrawWireCube(areaCenter, areaSize);
        }
    }
}
