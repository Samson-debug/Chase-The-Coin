using Fusion;
using UnityEngine;

namespace ChaseTheCoin.Manager
{
    public class CoinManager : NetworkBehaviour, IManager
    {
        [Header("Coin Setup")]
        [SerializeField] private NetworkPrefabRef coinPrefab;
        [SerializeField] private Transform[] spawnPoints;

        public override void Spawned()
        {
            Initialize();

            // Only the server/host should spawn coins
            if (HasStateAuthority)
            {
                SpawnCoin();
            }
        }

        public void Initialize()
        {
            bool success = GlobalManagers.Instance.RegisterManager(this);

            if (!success)
            {
                Runner.Despawn(Object);
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (GlobalManagers.Instance != null)
            {
                GlobalManagers.Instance.UnregisterManager(this);
            }
        }

        /// <summary>
        /// Spawns a coin at a random valid location.
        /// </summary>
        public void SpawnCoin()
        {
            if (!HasStateAuthority) return;

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning("[CoinManager] No spawn points assigned!");
                return;
            }

            int randomIndex = Random.Range(0, spawnPoints.Length);
            Transform spawnPoint = spawnPoints[randomIndex];

            Runner.Spawn(coinPrefab, spawnPoint.position, spawnPoint.rotation);
        }
    }
}
