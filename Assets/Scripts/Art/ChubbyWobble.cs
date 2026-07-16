using UnityEngine;

namespace RushBank.Art
{
    public class ChubbyWobble : MonoBehaviour
    {
        [SerializeField] private float breatheAmplitude = 0.022f;
        [SerializeField] private float breatheSpeed = 2.6f;
        [SerializeField] private float waddleDegrees = 5f;
        [SerializeField] private float waddleSpeed = 11f;

        private Transform trackedRoot;
        private Vector3 baseScale;
        private Vector3 lastRootPosition;
        private float phase;
        private float moveBlend;

        private void Start()
        {
            trackedRoot = transform.parent != null ? transform.parent : transform;
            baseScale = transform.localScale;
            lastRootPosition = trackedRoot.position;
            phase = Random.Range(0f, 20f);
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            var rootPosition = trackedRoot.position;
            var speed = (rootPosition - lastRootPosition).magnitude / deltaTime;
            lastRootPosition = rootPosition;

            var targetBlend = Mathf.Clamp01(speed / 1.5f);
            moveBlend = Mathf.MoveTowards(moveBlend, targetBlend, deltaTime * 4f);

            var breathe = 1f + Mathf.Sin(Time.time * breatheSpeed + phase) * breatheAmplitude;
            transform.localScale = new Vector3(
                baseScale.x * (2f - breathe),
                baseScale.y * breathe,
                baseScale.z);

            var waddle = Mathf.Sin(Time.time * waddleSpeed + phase) * waddleDegrees * moveBlend;
            transform.localRotation = Quaternion.Euler(0f, 0f, waddle);
        }
    }
}
