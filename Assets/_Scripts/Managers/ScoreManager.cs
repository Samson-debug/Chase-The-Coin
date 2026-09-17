using Fusion;
using UnityEngine;

namespace ChaseTheCoin.Manager
{
    /// <summary>
    /// Centralized Score System. 
    /// Tracks scores for all players in the match using a NetworkDictionary.
    /// </summary>
    public class ScoreManager : NetworkBehaviour, IManager
    {
        // Tracks the score for each player. The capacity must be set to the max players.
        [Networked, Capacity(2)]
        public NetworkDictionary<PlayerRef, int> PlayerScores => default;

        public override void Spawned()
        {
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
        
        [ContextMenu("Add Local Score")]
        public void AddLocalScore()
        {
            if (Runner == null) return;
            
            AddScore(Runner.LocalPlayer, 1);

            Debug.Log($"Local player({Runner.LocalPlayer} score incremented by 1)");
        }
        
        [ContextMenu("Add Opponent Score")]
        public void AddOpponentScore()
        {
            if (Runner == null) return;

            foreach (var player in Runner.ActivePlayers)
            {
                if(Runner.LocalPlayer !=  player) AddScore(player, 1);
            }

            Debug.Log($"Oppnent({Runner.LocalPlayer} score incremented by 1)");
        }

        /// <summary>
        /// Adds score to a specific player. Can be called by clients or the server.
        /// </summary>
        public void AddScore(PlayerRef player, int amount)
        {
            // Only the State Authority (Server/Host) can modify networked collections
            if (!HasStateAuthority)
            {
                Rpc_AddScore(player, amount);
                return;
            }

            if (PlayerScores.TryGet(player, out int currentScore))
            {
                PlayerScores.Set(player, currentScore + amount);
            }
            else
            {
                PlayerScores.Set(player, amount);
            }

            Debug.Log($"[ScoreManager] Player {player.PlayerId} score is now {PlayerScores.Get(player)}");
        }

        /// <summary>
        /// Gets the current score for a specific player.
        /// </summary>
        public int GetScore(PlayerRef player)
        {
            if (PlayerScores.TryGet(player, out int score))
            {
                return score;
            }
            return 0;
        }

        /// <summary>
        /// RPC to forward the score addition request to the State Authority.
        /// </summary>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void Rpc_AddScore(PlayerRef player, int amount)
        {
            AddScore(player, amount);
        }
    }
}
