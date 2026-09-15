using Fusion;
using UnityEngine;

namespace ChaseTheCoin.Manager
{
    /// <summary>
    /// Placed in the Gameplay scene. Spawns player prefabs when the scene loads.
    /// </summary>
    public class PlayerSpawner : NetworkBehaviour, IPlayerJoined, IPlayerLeft, ISceneLoadDone
    {
        [Header("Prefabs")]
        [SerializeField] private NetworkPrefabRef playerPrefab;

        [Header("Spawn Points")]
        [Tooltip("Assign 2 spawn points for the 2 players")]
        [SerializeField] private Transform[] spawnPoints;

        // Keep track of spawned players to clean them up if they leave
        private readonly System.Collections.Generic.Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new();


        public void SceneLoadDone(in SceneLoadDoneArgs sceneInfo)
        {
            if (!Runner.IsServer) return;

            foreach (var player in Runner.ActivePlayers)
            {
                SpawnPlayer(player);
            }
        }

        public void PlayerJoined(PlayerRef player)
        {
            if (!Runner.IsServer) return;
            
            // If a player joins late (if allowed), spawn them too
            SpawnPlayer(player);
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
            // Avoid spawning twice for the same player
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
