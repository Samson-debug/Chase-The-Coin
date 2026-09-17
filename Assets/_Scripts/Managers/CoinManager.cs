using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace ChaseTheCoin.Manager
{
    public class CoinManager : NetworkBehaviour, IManager
    {
        [Header("Coin Setup")] [SerializeField]
        private NetworkPrefabRef coinPrefab;

        [SerializeField] private float spawnDelay = 1f;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private LayerMask playerLayerMask;

        private int _lastSpawnIndex = -1;
        private Interactables.Coin _activeCoin;
        private NetworkTransform _activeCoinTransform;

        [Networked] private TickTimer _spawnTimer { get; set; }

        public override void Spawned()
        {
            Initialize();

            //host only
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
            if (GlobalManagers.Instance) GlobalManagers.Instance.UnregisterManager(this);
        }

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority && _spawnTimer.Expired(Runner))
            {
                _spawnTimer = TickTimer.None;
                SpawnCoin();
            }
        }

        private void SpawnCoin()
        {
            if (!HasStateAuthority) return;

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning("[CoinManager] No spawn points assigned!");
                return;
            }

            var validIndices = new List<int>();

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                //don't spawn at last spawn location
                if (spawnPoints.Length > 1 && i == _lastSpawnIndex) continue;

                // Check if the spawn point is clear of players
                Collider2D playerAtSpawn = Physics2D.OverlapCircle(spawnPoints[i].position, 0.5f, playerLayerMask);
                if (playerAtSpawn == null) validIndices.Add(i);
            }

            int randomIndex;
            if (validIndices.Count > 0)
            {
                randomIndex = validIndices[Random.Range(0, validIndices.Count)];
            }
            else
            {
                // Fallback: all points are occupied
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
                _activeCoin = spawnedObj.GetComponent<Interactables.Coin>();

                _activeCoin.OnCollected += () => QueueSpawnCoin(spawnDelay);

                _activeCoinTransform = _activeCoin.GetComponent<NetworkTransform>();
                if (!_activeCoinTransform) Debug.LogWarning("[RegisterManager] Coin has no Network Transform!");
            }
            else
            {
                //coin teleport
                if (_activeCoinTransform) _activeCoinTransform.Teleport(spawnPoint.position, spawnPoint.rotation);

                _activeCoin.ResetCoin();
            }
        }

        private void QueueSpawnCoin(float delaySeconds)
        {
            if (!HasStateAuthority) return;

            _spawnTimer = TickTimer.CreateFromSeconds(Runner, delaySeconds);


            if (_activeCoin == null) return;

            // Temporarily move the coin
            if (_activeCoinTransform) _activeCoinTransform.Teleport(new Vector3(0, -1000f, 0));
        }
    }
}
