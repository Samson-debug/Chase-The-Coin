using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChaseTheCoin.Manager
{
    /// <summary>
    /// Manages Photon Fusion NetworkRunner creation, startup, callbacks, and shutdown.
    /// </summary>
    public class NetworkRunnerController : MonoBehaviour, IManager, INetworkRunnerCallbacks
    {
        public event Action OnMatchmakingStarted;
        public event Action OnBothPlayerJoinedSuccessfully;
        public event Action OnShutdownOccurred;
        public event Action OnConnectFailedOccurred;
        
        [Header("Runner Configuration")]
        [SerializeField] private NetworkRunner networkRunnerPrefab;

        [Header("Scene Configuration")]
        [SerializeField] private int gameplaySceneIndex = 1;

        private NetworkRunner _activeRunner;
        
        public bool IsConnecting { get; private set; }      // whether matchmaking is in progress
        
        public bool IsActive => _activeRunner != null && _activeRunner.IsRunning;

        private void Awake()
        {
            if (networkRunnerPrefab == null)
            {
                Debug.LogWarning("[NetworkManager] NetworkRunner prefab is not assigned.");
            }
            
            Initialize();
        }

        public void Initialize()
        {
            bool success = GlobalManagers.Instance.RegisterManager(this, true);

            if (!success)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            Debug.Log("[NetworkRunnerController] OnDestroy");
            
            CleanupActiveRunner();
            GlobalManagers.Instance?.UnregisterManager(this);
        }

        public async Task StartGameAsync(GameMode mode, string roomCode)
        {
            if (IsConnecting)
            {
                Debug.LogWarning("[NetworkRunnerController] Matchmaking already in progress.");
                return;
            }

            IsConnecting = true;
            OnMatchmakingStarted?.Invoke();

            if (_activeRunner != null && _activeRunner.IsRunning)
            {
                Debug.Log("[NetworkRunnerController] Shutting down existing active runner before starting new session.");
                await _activeRunner.Shutdown();
                Destroy(_activeRunner.gameObject);
                _activeRunner = null;
            }
            
            if (networkRunnerPrefab) _activeRunner = Instantiate(networkRunnerPrefab);
            else
            {
                GameObject runnerGO = new GameObject("NetworkRunner");
                _activeRunner = runnerGO.AddComponent<NetworkRunner>();
            }

            _activeRunner.ProvideInput = true;
            _activeRunner.AddCallbacks(this);

            var sceneManager = _activeRunner.GetComponent<INetworkSceneManager>();
            if (sceneManager == null)
            {
                sceneManager = _activeRunner.gameObject.AddComponent<NetworkSceneManagerDefault>();
            }

            var objectProvider = _activeRunner.GetComponent<INetworkObjectProvider>();
            if (objectProvider == null)
            {
                objectProvider = _activeRunner.gameObject.AddComponent<NetworkObjectProviderDefault>();
            }

            try
            {
                var result = await _activeRunner.StartGame(new StartGameArgs
                {
                    GameMode = mode,
                    SessionName = string.IsNullOrEmpty(roomCode) ? null : roomCode,
                    SceneManager = sceneManager,
                    ObjectProvider = objectProvider,
                    PlayerCount = 2
                });

                if (!result.Ok)
                {
                    Debug.LogError($"[NetworkRunnerController] Fusion StartGame failed: {result.ShutdownReason}");
                    CleanupActiveRunner();
                }
                else
                {
                    Debug.Log($"[NetworkRunnerController] Started game in {mode} mode with Session: {_activeRunner.SessionInfo.Name}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkRunnerController] Exception during StartGameAsync: {ex.Message}");
                CleanupActiveRunner();
            }
            finally
            {
                IsConnecting = false;
            }
        }
        
        public void CancelMatchmaking()
        {
            Debug.Log("[NetworkRunnerController] Cancelling matchmaking and shutting down runner.");
            IsConnecting = false;
            CleanupActiveRunner();
        }

        private void CleanupActiveRunner()
        {
            if (_activeRunner != null)
            {
                _activeRunner.RemoveCallbacks(this);
                if (_activeRunner.IsRunning)
                {
                    _activeRunner.Shutdown();
                }
                Destroy(_activeRunner.gameObject);
                _activeRunner = null;
            }
        }
        
        #region INetworkRunnerCallbacks

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            Debug.Log($"[NetworkRunnerController] OnShutdown: {shutdownReason}");
            IsConnecting = false;
            OnShutdownOccurred?.Invoke();
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            Debug.LogError($"[NetworkRunnerController] OnConnectFailed: {reason}");
            IsConnecting = false;
            OnConnectFailedOccurred?.Invoke();
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            Debug.LogWarning($"[NetworkRunnerController] OnDisconnectedFromServer: {reason}");
            IsConnecting = false;
            OnConnectFailedOccurred?.Invoke();
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log($"[NetworkRunnerController] OnPlayerJoined: {player}");
            
            // Check if both players have joined the session
            if (runner.ActivePlayers.Count() == 2)
            {
                OnBothPlayerJoinedSuccessfully?.Invoke();

                if (runner.IsServer)
                {
                    Debug.Log("[NetworkRunnerController] Both players joined. Loading gameplay scene...");
                    runner.LoadScene(SceneRef.FromIndex(gameplaySceneIndex), LoadSceneMode.Single);
                }
            }
        }
        
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }

        #endregion
    }
}