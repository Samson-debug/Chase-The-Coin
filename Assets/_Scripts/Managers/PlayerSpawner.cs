using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace ChaseTheCoin.Manager
{
    /// <summary>
    /// Placed in the Gameplay scene. Spawns player prefabs when the scene loads.
    /// </summary>
    public class PlayerSpawner : NetworkBehaviour, IPlayerLeft
    {
        [Header("Prefabs")]
        [SerializeField] private NetworkPrefabRef playerPrefab;

        [Header("Spawn Points")]
        [SerializeField] private Transform[] spawnPoints;

        private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new();

        public override void Spawned()
        {
            if (!Runner.IsServer) return;

            foreach (var player in Runner.ActivePlayers)
            {
                SpawnPlayer(player);
            };
        }

        public void PlayerLeft(PlayerRef player)
        {
            if (!Runner.IsServer) return;

            // Despawn the player's character when they leave
            if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
            {
                Runner.Despawn(networkObject);
                _spawnedCharacters.Remove(player);
            }
        }

        private void SpawnPlayer(PlayerRef player)
        {
            //avoid spawning multiple times
            if (_spawnedCharacters.ContainsKey(player)) return;

            // Determine spawn position based on player index
            // (e.g. Player 1 gets spawnPoints[0], Player 2 gets spawnPoints[1])
            int index = player.PlayerId % spawnPoints.Length;
            Vector3 spawnPos = spawnPoints.Length > 0 ? spawnPoints[index].position : Vector3.zero;

            // Spawn the character and pass the PlayerRef to give them Input Authority
            var playerObject = Runner.Spawn(playerPrefab, spawnPos, Quaternion.identity, player);
            
            _spawnedCharacters.Add(player, playerObject);
        }
    }
}
