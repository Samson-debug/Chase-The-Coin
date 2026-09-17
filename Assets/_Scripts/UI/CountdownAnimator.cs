using DG.Tweening;
using TMPro;
using UnityEngine;
using System;
using System.Collections;

namespace ChaseTheCoin.UI
{
    public class CountdownAnimator : MonoBehaviour
    {
        [Header("Reference")] [SerializeField] private TextMeshProUGUI countdownText;

        [Header("Timing")] [SerializeField] private float numberDuration = 0.9f;
        [SerializeField] private float goDuration = 1.0f;

        [Header("Scale")] [SerializeField] private float startScale = 0.2f;
        [SerializeField] private float punchScale = 1.2f;
        [SerializeField] private float endScale = 0.8f;

        public event Action OnCountdownFinished;

        private readonly string[] sequence = { "3", "2", "1", "GO!" };

        private void Awake()
        {
            countdownText.gameObject.SetActive(false);
        }

        [ContextMenu("Play")]
        public void Play()
        {
            StopAllCoroutines();
            DOTween.Kill(countdownText);

            StartCoroutine(PlayRoutine());
        }

        private IEnumerator PlayRoutine()
        {
            countdownText.gameObject.SetActive(true);

            // 3 2 1
            for (int i = 0; i < 3; i++)
            {
                yield return AnimateNumber(sequence[i], Color.white, numberDuration);
            }

            // GO
            yield return AnimateNumber(sequence[3], Color.green, goDuration);

            countdownText.gameObject.SetActive(false);
            OnCountdownFinished?.Invoke();
        }

        private IEnumerator AnimateNumber(string value, Color color, float duration)
        {
            countdownText.text = value;
            countdownText.color = color;

            countdownText.transform.localScale = Vector3.one * startScale;
            countdownText.alpha = 1f;

            Sequence s = DOTween.Sequence();

            s.Append(countdownText.transform.DOScale(punchScale, duration * 0.35f)
                .SetEase(Ease.OutBack));

            s.Append(countdownText.transform.DOScale(endScale, duration * 0.25f)
                .SetEase(Ease.InOutQuad));

            s.Join(countdownText.DOFade(0f, duration * 0.35f)
                .SetDelay(duration * 0.65f));

            yield return s.WaitForCompletion();
        }
    }
}