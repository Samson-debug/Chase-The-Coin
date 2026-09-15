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
        public static ScoreManager Instance { get; private set; }

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

        /// <summary>
        /// Context Menu function to quickly test the score system in the editor.
        /// It will add a score to the first active player it finds.
        /// </summary>
        [ContextMenu("Test Add Score")]
        public void TestAddScore()
        {
            if (Runner != null)
            {
                foreach (var player in Runner.ActivePlayers)
                {
                    AddScore(player, 1);
                    break;
                }
            }
            else
            {
                Debug.LogWarning("[ScoreManager] Runner is null. Ensure you are in play mode and connected.");
            }
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
        /// RPC to forward the score addition request to the State Authority.
        /// </summary>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void Rpc_AddScore(PlayerRef player, int amount)
        {
            AddScore(player, amount);
        }
    }
}
