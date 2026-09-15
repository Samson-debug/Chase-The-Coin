using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using ChaseTheCoin.Manager;

namespace ChaseTheCoin.UI
{
    /// <summary>
    /// Manages Main Menu UI interactions and room creation/joining flows.
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private LoadingPanel loadingPanel;

        [Header("UI Controls")]
        [SerializeField] private TMP_InputField roomCodeInputField;
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button joinRoomButton;
        [SerializeField] private Button autoJoinButton;

        [Header("Parameters")]
        [SerializeField] private int minRoomCodeLength = 4;

        private NetworkRunnerController _networkRunnerController;
        
        private void Start()
        {
            _networkRunnerController = GlobalManagers.Instance.GetManager<NetworkRunnerController>();
            
            RegisterListeners();
            ValidateInitialState();
        }

        private void OnDestroy()
        {
            if (roomCodeInputField) roomCodeInputField.onValueChanged.RemoveListener(OnRoomCodeValueChanged);
            if (createRoomButton) createRoomButton.onClick.RemoveListener(OnCreateRoomClicked);
            if (joinRoomButton) joinRoomButton.onClick.RemoveListener(OnJoinRoomClicked);
            if (autoJoinButton) autoJoinButton.onClick.RemoveListener(OnAutoJoinClicked);
        }

        private void RegisterListeners()
        { 
            if (roomCodeInputField) roomCodeInputField.onValueChanged.AddListener(OnRoomCodeValueChanged);
            if (createRoomButton) createRoomButton.onClick.AddListener(OnCreateRoomClicked);
            if (joinRoomButton) joinRoomButton.onClick.AddListener(OnJoinRoomClicked);
            if (autoJoinButton) autoJoinButton.onClick.AddListener(OnAutoJoinClicked);
        }

        private void ValidateInitialState()
        {
            string currentCode = GetRoomCode();
            UpdateButtonsInteractable(currentCode);
        }

        private void OnRoomCodeValueChanged(string roomCode)
        {
            UpdateButtonsInteractable(roomCode);
        }

        private void UpdateButtonsInteractable(string roomCode)
        {
            bool isValid = IsRoomCodeValid(roomCode);

            if (createRoomButton != null) createRoomButton.interactable = isValid;
            if (joinRoomButton != null) joinRoomButton.interactable = isValid;
        }
        
        private void OnCreateRoomClicked()
        {
            Debug.Log($"[MainMenuManager] Creating Room Code: {GetRoomCode()}");
            string roomCode = GetRoomCode();
            if (!IsRoomCodeValid(roomCode))
            {
                Debug.LogWarning("[MainMenuManager] Room Code is invalid or too short.");
                return;
            }
            
            StartMatchmaking(GameMode.Host, roomCode);
        }
        
        private void OnJoinRoomClicked()
        {
            string roomCode = GetRoomCode();
            if (!IsRoomCodeValid(roomCode))
            {
                Debug.LogWarning("[MainMenuManager] Room Code is invalid or too short.");
                return;
            }

            StartMatchmaking(GameMode.Client, roomCode);
        }

        private void OnAutoJoinClicked()
        {
            string roomCode = string.Empty; // Empty string triggers Fusion's random matchmaking
            
            StartMatchmaking(GameMode.AutoHostOrClient, roomCode);
        }

        private void StartMatchmaking(GameMode gameMode, string roomCode)
        {
            Debug.Log($"[MainMenuManager] Initiating matchmaking in {gameMode} mode with Room Code: {roomCode}");

            if (loadingPanel) loadingPanel.Show(roomCode);

            _networkRunnerController.StartGameAsync(gameMode, roomCode);
        }

        private bool IsRoomCodeValid(string roomCode)
        {
            string code = roomCode?.Trim() ?? string.Empty;
            return code.Length >= minRoomCodeLength;
        }

        private string GetRoomCode()
        {
            return roomCodeInputField != null ? roomCodeInputField.text.Trim().ToUpper() : string.Empty;
        }
    }
}