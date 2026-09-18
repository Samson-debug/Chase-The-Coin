using UnityEngine;
using TMPro;
using DG.Tweening;

namespace ChaseTheCoin.UI
{
    public class ScorePopup : MonoBehaviour
    {
        [SerializeField] private TMP_Text scoreText;

        [Header("Animation Settings")] [SerializeField]
        private float moveY = 1.5f;

        [SerializeField] private float animationDuration = 1f;
        [SerializeField] private float fadeDuration = 0.8f;
        private float randomizeX = 0.2f;

        // Simple static pool to reuse instances
        private static System.Collections.Generic.Queue<ScorePopup> popupPool = new ();

        private void Awake()
        {
            if (scoreText == null)
            {
                scoreText = GetComponentInChildren<TMP_Text>();
            }
        }

        /// <summary>
        /// Spawns a score popup using an object pool to reuse instances.
        /// </summary>
        public static void Create(ScorePopup prefab, Vector3 position, int scoreAmount)
        {
            ScorePopup popup;
            if (popupPool.Count > 0)
            {
                popup = popupPool.Dequeue();
                popup.transform.position = position;
                popup.gameObject.SetActive(true);
            }
            else
            {
                popup = Instantiate(prefab, position, Quaternion.identity);
            }

            popup.Setup(scoreAmount);
        }

        /// <summary>
        /// Call this method to initialize and start the popup animation.
        /// </summary>
        /// <param name="scoreAmount">The score value to display.</param>
        private void Setup(int scoreAmount)
        {
            // Kill any active tweens on this object in case it was reused before finishing
            transform.DOKill();
            scoreText.DOKill();

            scoreText.text = "+" + scoreAmount.ToString();

            // Reset properties in case this object is being reused from an object pool
            scoreText.alpha = 1f;

            AnimatePopup();
        }

        private void AnimatePopup()
        {
            // Optional: Randomize starting position slightly for a better visual effect 
            // if multiple popups occur at the same time.
            float randomXOffset = UnityEngine.Random.Range(-randomizeX, randomizeX);
            transform.position += new Vector3(randomXOffset, 0, 0);

            // Move up using DOTween
            transform.DOMoveY(transform.position.y + moveY, animationDuration).SetEase(Ease.OutCirc);

            // Fade out text alpha
            scoreText.DOFade(0f, fadeDuration)
                // Wait to start fading until near the end of the movement
                .SetDelay(animationDuration - fadeDuration)
                .OnComplete(() =>
                {
                    // Deactivate and return to pool instead of destroying
                    gameObject.SetActive(false);
                    popupPool.Enqueue(this);
                });
        }
    }
}