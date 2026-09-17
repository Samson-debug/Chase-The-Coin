using Fusion;
using TMPro;
using UnityEngine;
using ChaseTheCoin.Manager;
using System.Collections;

namespace ChaseTheCoin.UI
{
    public class UIManager : NetworkBehaviour, IManager
    {
        [Header("Scores")]
        [SerializeField] private TextMeshProUGUI localScoreText;
        [SerializeField] private TextMeshProUGUI opponentScoreText;
        
        [Header("Timer")]
        [SerializeField] private TextMeshProUGUI timerText;

        [Header("Countdown")]
        [SerializeField] private CountdownAnimator countdownAnimator;

        private ScoreManager _scoreManager;
        private TimerManager _timerManager;

        public override void Spawned()
        {
            Initialize();
            StartCoroutine(GetDependenciesRoutine());
        }

        private void OnDestroy()
        {
            if (_timerManager) _timerManager.OnStateChanged -= HandleStateChanged;
            
            if (GlobalManagers.Instance != null)
            {
                GlobalManagers.Instance.UnregisterManager(this);
            }
        }

        public void Initialize()
        {
            GlobalManagers.Instance.RegisterManager(this);
        }

        private IEnumerator GetDependenciesRoutine()
        {
            //Wait until managers are registered
            while (_scoreManager == null || _timerManager == null)
            {
                if (_scoreManager == null)
                    _scoreManager = GlobalManagers.Instance.GetManager<ScoreManager>();

                if (_timerManager == null)
                    _timerManager = GlobalManagers.Instance.GetManager<TimerManager>();

                yield return null;
            }

            // Dependencies acquired, safely subscribe
            _timerManager.OnStateChanged += HandleStateChanged;
            HandleStateChanged(_timerManager.State);
        }

        private void HandleStateChanged(MatchState newState)
        {
            if (newState == MatchState.Countdown && countdownAnimator != null)
            {
                countdownAnimator.Play();
            }
            else if (newState == MatchState.Finished)
            {
                if (timerText != null) timerText.text = "00:00";
            }
        }

        private void Update()
        {
            if (Runner == null || !Runner.IsRunning) return;

            UpdateScores();
            UpdateTimer();
        }

        private void UpdateScores()
        {
            if (_scoreManager == null) return;

            int localScore = 0;
            int opponentScore = 0;

            PlayerRef localPlayer = Runner.LocalPlayer;

            foreach (var player in Runner.ActivePlayers)
            {
                int score = _scoreManager.GetScore(player);
                if (player == localPlayer)
                {
                    localScore = score;
                }
                else
                {
                    opponentScore = score;
                }
            }

            if (localScoreText != null) localScoreText.text = localScore.ToString();
            if (opponentScoreText != null) opponentScoreText.text = opponentScore.ToString();
        }

        private void UpdateTimer()
        {
            if (_timerManager == null) return;

            if (_timerManager.State == MatchState.Playing)
            {
                float time = _timerManager.GetRemainingTime();
                int minutes = Mathf.FloorToInt(time / 60F);
                int seconds = Mathf.FloorToInt(time - minutes * 60);
                
                if (timerText != null)
                {
                    timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
                }
            }
            else if (_timerManager.State == MatchState.WaitingForPlayers)
            {
                if (timerText != null) timerText.text = "WAITING";
            }
        }
    }
}
