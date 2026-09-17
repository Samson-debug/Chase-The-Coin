using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using ChaseTheCoin.Manager;
using UnityEngine.SceneManagement;

namespace ChaseTheCoin.UI
{
    public class GameOverScreen : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI localScoreText;
        [SerializeField] private TextMeshProUGUI opponentScoreText;
        [SerializeField] private Button homeButton;

        private TimerManager _timerManager;
        private ScoreManager _scoreManager;
        private NetworkRunnerController _networkRunnerController;

        private void Start()
        {
            if (homeButton != null)
            {
                homeButton.onClick.AddListener(OnHomeButtonClicked);
            }
            
            gameOverPanel?.SetActive(false);

            StartCoroutine(GetDependenciesRoutine());
        }

        private IEnumerator GetDependenciesRoutine()
        {
            // Wait until managers are registered in GlobalManagers
            while (_timerManager == null || _scoreManager == null || _networkRunnerController == null)
            {
                if (_timerManager == null) _timerManager = GlobalManagers.Instance.GetManager<TimerManager>();
                if (_scoreManager == null) _scoreManager = GlobalManagers.Instance.GetManager<ScoreManager>();
                if (_networkRunnerController == null) _networkRunnerController = GlobalManagers.Instance.GetManager<NetworkRunnerController>();
                yield return null;
            }
            
            _timerManager.OnStateChanged += HandleStateChanged;
            
            HandleStateChanged(_timerManager.State);
        }

        private void OnDestroy()
        {
            homeButton?.onClick.RemoveListener(OnHomeButtonClicked);

            if (_timerManager != null)
            {
                _timerManager.OnStateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(MatchState newState)
        {
            if (newState == MatchState.Finished)
            {
                ShowGameOverScreen();
            }
        }

        private void ShowGameOverScreen()
        {
            gameOverPanel?.SetActive(true);

            int localScore = 0;
            int opponentScore = 0;

            if (_networkRunnerController != null && _networkRunnerController.IsActive)
            {
                var runner = FindFirstObjectByType<NetworkRunner>();
                
                if (runner != null)
                {
                    PlayerRef localPlayer = runner.LocalPlayer;
                    foreach (var player in runner.ActivePlayers)
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
                }
            }

            if (localScoreText != null) localScoreText.text = localScore.ToString();
            if (opponentScoreText != null) opponentScoreText.text = opponentScore.ToString();

            if (titleText != null)
            {
                if (localScore > opponentScore)
                {
                    titleText.text = "You won!";
                }
                else if (localScore < opponentScore)
                {
                    titleText.text = "Opponent Won!";
                }
                else
                {
                    titleText.text = "It's a Draw!";
                }
            }
        }

        private void OnHomeButtonClicked()
        {
            _networkRunnerController?.CancelMatchmaking();

            // Then, immediately load the Main Menu scene.
            const int Main_Menu_Scene_Index = 0;
            SceneManager.LoadScene(Main_Menu_Scene_Index);
        }
    }
}
