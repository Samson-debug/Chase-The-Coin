using System;
using ChaseTheCoin.Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChaseTheCoin.UI
{
    /// <summary>
    /// Manages Matchmaking loading panel.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class LoadingPanel : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI roomCodeText;
        [SerializeField] private Button cancelButton;

        private CanvasGroup _canvasGroup;
        private NetworkRunnerController _networkRunnerController;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Start()
        {
            _networkRunnerController = GlobalManagers.Instance.GetManager<NetworkRunnerController>();
            _networkRunnerController.OnPlayerJoinedSuccessfully += Hide;
            _networkRunnerController.OnConnectFailedOccurred += Hide;
            _networkRunnerController.OnShutdownOccurred += Hide;
            
            if (cancelButton) cancelButton.onClick.AddListener(CancelMatchMaking);

            Hide();
        }

        private void OnDestroy()
        {
            if (cancelButton) cancelButton.onClick.RemoveAllListeners();
            if (_networkRunnerController != null)
            {
                _networkRunnerController.OnPlayerJoinedSuccessfully -= Hide;
                _networkRunnerController.OnConnectFailedOccurred -= Hide;
                _networkRunnerController.OnShutdownOccurred -= Hide;
            }
        }

        public void Show(string roomCode)
        {
            if (roomCodeText != null) 
            {
                roomCodeText.text = string.IsNullOrEmpty(roomCode) ? "Searching..." : roomCode;
            }

            ChangeState(true);
        }
        
        public void Hide() 
        {
            if (this == null || gameObject == null) return;
            ChangeState(false);
        }

        private void ChangeState(bool active)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = active ? 1f : 0f;
                _canvasGroup.interactable = active;
                _canvasGroup.blocksRaycasts = active;
            }

            gameObject.SetActive(active);
        }

        private void CancelMatchMaking()
        {
            Debug.Log("[LoadingPanel] Cancel button clicked.");
            
            _networkRunnerController.CancelMatchmaking();
            
            Hide();
        }
    }
}