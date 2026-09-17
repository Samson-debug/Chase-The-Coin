using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;
using ChaseTheCoin.Manager;

namespace ChaseTheCoin.Player
{
    /// <summary>
    /// Reads local input from the New Input System and feeds it to Photon Fusion.
    /// Attach this to the Player Prefab.
    /// </summary>
    public class PlayerInputPoller : NetworkBehaviour, INetworkRunnerCallbacks
    {
        [Header("Input Settings (New Input System)")]
        [Tooltip("Action for moving. Expected to be a Value type (Vector2). Useful for Mobile On-Screen Stick.")]
        [SerializeField] private InputActionReference moveAction;
        
        [Tooltip("Action for jumping. Expected to be a Button type. Useful for Mobile On-Screen Button.")]
        [SerializeField] private InputActionReference jumpAction;
        
        private float _localMoveInput;
        private bool _localJumpInput;

        public override void Spawned()
        {
            // Only the local player (Input Authority) should poll their local input
            if (HasInputAuthority)
            {
                Runner.AddCallbacks(this);
                
                if (moveAction != null) moveAction.action.Enable();
                if (jumpAction != null) jumpAction.action.Enable();
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (HasInputAuthority)
            {
                Runner.RemoveCallbacks(this);
                
                if (moveAction != null) moveAction.action.Disable();
                if (jumpAction != null) jumpAction.action.Disable();
            }
        }

        private void Update()
        {
            if (!HasInputAuthority) return;

            // Read Movement (Using Axis Control Type - 1D float)
            if (moveAction != null)
            {
                _localMoveInput = moveAction.action.ReadValue<float>();
            }

            // Read Jump Button
            if (jumpAction != null)
            {
                // IsPressed returns true as long as the button is held down.
                // Fusion's NetworkButtons handles tracking the "WasPressed" state cleanly in FixedUpdateNetwork.
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
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
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
