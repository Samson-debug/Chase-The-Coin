using Fusion;
using UnityEngine;

namespace ChaseTheCoin.Manager
{
    public class CoinManager : NetworkBehaviour, IManager
    {
        [Header("Coin Setup")]
        [SerializeField] private NetworkPrefabRef coinPrefab;
        [SerializeField] private Transform[] spawnPoints;

        private int _lastSpawnIndex = -1;
        private ChaseTheCoin.Interactables.Coin _activeCoin;
        
        [Networked] private TickTimer _spawnTimer { get; set; }
        
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

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority && _spawnTimer.Expired(Runner))
            {
                _spawnTimer = TickTimer.None;
                SpawnCoin();
            }
        }

        public void QueueSpawnCoin(float delaySeconds)
        {
            if (!HasStateAuthority) return;
            
            _spawnTimer = TickTimer.CreateFromSeconds(Runner, delaySeconds);
            
            // Temporarily move the coin out of bounds while it's "despawned"
            if (_activeCoin != null)
            {
                var networkTransform = _activeCoin.GetComponent<NetworkTransform>();
                if (networkTransform != null)
                {
                    networkTransform.Teleport(new Vector3(0, -1000f, 0));
                }
                else
                {
                    _activeCoin.transform.position = new Vector3(0, -1000f, 0);
                }
            }
        }

        /// <summary>
        /// Spawns a coin at a random valid location, prioritizing locations where no players are standing.
        /// </summary>
        public void SpawnCoin()
        {
            if (!HasStateAuthority) return;

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning("[CoinManager] No spawn points assigned!");
                return;
            }

            int playerLayerMask = 1 << LayerMask.NameToLayer("Player");
            System.Collections.Generic.List<int> validIndices = new System.Collections.Generic.List<int>();

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                // Never spawn at the exact same location consecutively
                if (spawnPoints.Length > 1 && i == _lastSpawnIndex) continue;

                // Check if the spawn point is clear of players (within a 1.5 unit radius)
                Collider2D playerAtSpawn = Physics2D.OverlapCircle(spawnPoints[i].position, 1.5f, playerLayerMask);
                if (playerAtSpawn == null)
                {
                    validIndices.Add(i);
                }
            }

            int randomIndex;
            if (validIndices.Count > 0)
            {
                // Pick a random clear spawn point
                randomIndex = validIndices[Random.Range(0, validIndices.Count)];
            }
            else
            {
                // Fallback: all points are occupied, pick a random one that isn't the last one
                randomIndex = Random.Range(0, spawnPoints.Length);
                if (spawnPoints.Length > 1 && randomIndex == _lastSpawnIndex)
                {
                    randomIndex = (randomIndex + 1) % spawnPoints.Length;
                }
            }
            
            _lastSpawnIndex = randomIndex;
            Transform spawnPoint = spawnPoints[randomIndex];

            if (_activeCoin == null)
            {
                var spawnedObj = Runner.Spawn(coinPrefab, spawnPoint.position, spawnPoint.rotation);
                _activeCoin = spawnedObj.GetComponent<ChaseTheCoin.Interactables.Coin>();
            }
            else
            {
                // Teleport the existing coin to the new location
                var networkTransform = _activeCoin.GetComponent<NetworkTransform>();
                if (networkTransform != null)
                {
                    networkTransform.Teleport(spawnPoint.position, spawnPoint.rotation);
                }
                else
                {
                    _activeCoin.transform.position = spawnPoint.position;
                    _activeCoin.transform.rotation = spawnPoint.rotation;
                }
                
                _activeCoin.ResetCoin();
            }
        }
    }
}
