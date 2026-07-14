using UnityEngine;
using UnityEngine.EventSystems;

namespace RushBank.UI
{
    public class ScreenJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform handle;
        [SerializeField, Min(12f)] private float radius = 72f;
        [SerializeField, Range(0f, 0.4f)] private float deadZone = 0.08f;

        public Vector2 Value { get; private set; }

        private void Awake()
        {
            if (background == null)
            {
                background = transform as RectTransform;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            UpdateJoystick(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdateJoystick(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Value = Vector2.zero;
            if (handle != null)
            {
                handle.anchoredPosition = Vector2.zero;
            }
        }

        private void UpdateJoystick(PointerEventData eventData)
        {
            if (background == null)
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    background,
                    eventData.position,
                    eventData.pressEventCamera,
                    out var localPoint))
            {
                return;
            }

            var clampedPoint = Vector2.ClampMagnitude(localPoint, radius);
            var normalizedValue = clampedPoint / radius;
            Value = normalizedValue.sqrMagnitude < deadZone * deadZone ? Vector2.zero : normalizedValue;

            if (handle != null)
            {
                handle.anchoredPosition = clampedPoint;
            }
        }
    }
}
