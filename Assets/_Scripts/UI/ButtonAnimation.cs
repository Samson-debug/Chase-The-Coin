using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace ChaseTheCoin.UI
{
    public class ButtonAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler,
        IPointerUpHandler
    {
        [Header("Settings")] [SerializeField] private float hoverScale = 1.1f;
        [SerializeField] private float clickScale = 0.9f;
        [SerializeField] private float animationDuration = 0.2f;

        private Vector3 originalScale;

        private void Awake()
        {
            originalScale = transform.localScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            transform.DOScale(originalScale * hoverScale, animationDuration).SetUpdate(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.DOScale(originalScale, animationDuration).SetUpdate(true);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            transform.DOScale(originalScale * clickScale, animationDuration).SetUpdate(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            transform.DOScale(originalScale * hoverScale, animationDuration).SetUpdate(true);
        }

        private void OnDisable()
        {
            // Reset scale in case the button gets disabled while animating
            transform.DOKill();
            transform.localScale = originalScale;
        }
    }
}