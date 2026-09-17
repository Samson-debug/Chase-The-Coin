using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChaseTheCoin.Player
{
    /// <summary>
    /// Reads local input from the New Input System and feeds it to Photon Fusion.
    /// Attach this to the Player Prefab.
    /// </summary>
    public class PlayerInputPoller : NetworkBehaviour, INetworkRunnerCallbacks
    {
        [Header("Input Settings (New Input System)")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference jumpAction;
        
        private float _localMoveInput;
        private bool _localJumpInput;

        public override void Spawned()
        {
            //only local player
            if (HasInputAuthority)
            {
                Runner.AddCallbacks(this);
                
                moveAction?.action.Enable();
                jumpAction?.action.Enable();
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (HasInputAuthority)
            {
                Runner.RemoveCallbacks(this);
                
                moveAction?.action.Disable();
                jumpAction?.action.Disable();
            }
        }

        private void Update()
        {
            if (!HasInputAuthority) return;

            if (moveAction != null)
            {
                _localMoveInput = moveAction.action.ReadValue<float>();
            }

            if (jumpAction != null)
            {
                //IsPressed returns true as long as the button is held down.
                _localJumpInput = jumpAction.action.IsPressed();
            }
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            var inputData = new NetworkInputData();
            
            inputData.MovementInput = _localMoveInput;
            inputData.Buttons.Set(PlayerController2D.InputButtons.Jump, _localJumpInput);
            
            input.Set(inputData);
        }

        #region Unused INetworkRunnerCallbacks

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }

        #endregion
    }
}
