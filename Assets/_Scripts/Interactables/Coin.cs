using Fusion;
using UnityEngine;
using ChaseTheCoin.Player;
using ChaseTheCoin.Manager;

namespace ChaseTheCoin.Interactables
{
    public class Coin : NetworkBehaviour
    {
        [Header("Collision Setup")]
        [SerializeField] private float pickupRadius = 0.5f;
        [SerializeField] private LayerMask playerLayer;

        [Networked] private NetworkBool _isCollected { get; set; }

        public override void FixedUpdateNetwork()
        {
            // Only the server handles the collection logic to prevent discrepancies
            if (!HasStateAuthority || _isCollected) return;

            // Check for players in range
            Collider2D col = Physics2D.OverlapCircle(transform.position, pickupRadius, playerLayer);
            
            if (col != null)
            {
                var playerController = col.GetComponent<PlayerController2D>();
                if (playerController != null)
                {
                    _isCollected = true;

                    // Add score
                    PlayerRef playerRef = playerController.Object.InputAuthority;
                    var scoreManager = GlobalManagers.Instance.GetManager<ScoreManager>();
                    if (scoreManager != null)
                    {
                        scoreManager.AddScore(playerRef, 1);
                    }
                    // Queue coin reposition after 1 second
                    var coinManager = GlobalManagers.Instance.GetManager<CoinManager>();
                    if (coinManager != null)
                    {
                        coinManager.QueueSpawnCoin(1f);
                    }
                }
            }
        }

        public void ResetCoin()
        {
            _isCollected = false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, pickupRadius);
        }
    }
}
