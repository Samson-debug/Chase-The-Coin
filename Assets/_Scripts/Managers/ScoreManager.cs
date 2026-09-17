using Fusion;
using UnityEngine;

namespace ChaseTheCoin.Manager
{
    /// <summary>
    /// Centralized Score System.
    /// </summary>
    public class ScoreManager : NetworkBehaviour, IManager
    {
        //The capacity must be set to the max players.
        [Networked, Capacity(2)]
        private NetworkDictionary<PlayerRef, int> PlayerScores => default;

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
        
        private void OnDestroy()
        {
            GlobalManagers.Instance?.UnregisterManager(this);
        }

        public void AddScore(PlayerRef player, int amount)
        {
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
        
        //request to the State Authority.
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void Rpc_AddScore(PlayerRef player, int amount)
        {
            AddScore(player, amount);
        }


        public int GetScore(PlayerRef player)
        {
            if (PlayerScores.TryGet(player, out int score))
            {
                return score;
            }
            return 0;
        }
        
        #region Debug

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

        #endregion
    }
}
