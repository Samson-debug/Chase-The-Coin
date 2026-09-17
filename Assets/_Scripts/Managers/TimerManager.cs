using Fusion;
using UnityEngine;
using System;

namespace ChaseTheCoin.Manager
{
    public enum MatchState
    {
        WaitingForPlayers,
        Countdown,
        Playing,
        Finished
    }

    public class TimerManager : NetworkBehaviour, IManager
    {
        [Networked] public MatchState State { get; set; }
        [Networked] public TickTimer StateTimer { get; set; }

        public event Action<MatchState> OnStateChanged;

        [Header("Settings")]
        [SerializeField] private float countdownDuration = 5f; // 3, 2, 1, Go! (approx 3.7s from animator)
        [SerializeField] private float matchDuration = 60f;

        // Tracks previous state for detecting changes on clients
        private MatchState _previousState;

        public override void Spawned()
        {
            _previousState = State;
            Initialize();
        }

        public void Initialize()
        {
            bool success = GlobalManagers.Instance.RegisterManager(this);
            if (!success)
            {
                Runner.Despawn(Object);
            }
        }

        public override void FixedUpdateNetwork()
        {
            // State change detection for events
            if (_previousState != State)
            {
                OnStateChanged?.Invoke(State);
                _previousState = State;
            }

            if (!HasStateAuthority) return;

            switch (State)
            {
                case MatchState.WaitingForPlayers:
                    // Check if both players are present
                    // Using 2 as the expected player count
                    int playerCount = 0;
                    foreach(var _ in Runner.ActivePlayers) playerCount++;
                    
                    if (playerCount == 2)
                    {
                        StartCountdown();
                    }
                    break;

                case MatchState.Countdown:
                    if (StateTimer.Expired(Runner))
                    {
                        StartMatch();
                    }
                    break;

                case MatchState.Playing:
                    if (StateTimer.Expired(Runner))
                    {
                        EndMatch();
                    }
                    break;

                case MatchState.Finished:
                    // Handle post-match logic here
                    break;
            }
        }

        private void StartCountdown()
        {
            Debug.Log("[TimerManager] CountDown Started!");
            
            State = MatchState.Countdown;
            StateTimer = TickTimer.CreateFromSeconds(Runner, countdownDuration);
        }

        private void StartMatch()
        {
            Debug.Log("[TimerManager] Match Started!");
            
            State = MatchState.Playing;
            StateTimer = TickTimer.CreateFromSeconds(Runner, matchDuration);
        }

        private void EndMatch()
        {
            State = MatchState.Finished;
            StateTimer = TickTimer.None;
        }

        public float GetRemainingTime()
        {
            if (StateTimer.IsRunning)
            {
                float? remainingTime = StateTimer.RemainingTime(Runner);
                return remainingTime.HasValue ? remainingTime.Value : 0f;
            }
            return 0f;
        }

        [ContextMenu("Test Start Countdown")]
        public void TestStartCountdown()
        {
            if (HasStateAuthority)
            {
                StartCountdown();
            }
            else
            {
                Debug.LogWarning("[TimerManager] Only the State Authority (Host/Server) can manually start the countdown.");
            }
        }
    }
}
