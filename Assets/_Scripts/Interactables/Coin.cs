using System;
using Fusion;
using UnityEngine;
using ChaseTheCoin.Player;
using ChaseTheCoin.Manager;

namespace ChaseTheCoin.Interactables
{
    public class Coin : NetworkBehaviour
    {
        public event Action OnCollected;
        
        [Header("Collision Setup")]
        [SerializeField] private float pickupRadius = 0.5f;
        [SerializeField] private LayerMask playerLayer;

        [Networked] private NetworkBool _isCollected { get; set; }

        private ScoreManager _scoreManager;

        public override void Spawned()
        {
            _scoreManager = GlobalManagers.Instance.GetManager<ScoreManager>();
            
            if(!_scoreManager) GlobalManagers.Instance.OnManagerRegistered += HandleManagerRegistered;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            GlobalManagers.Instance.OnManagerRegistered -= HandleManagerRegistered;
        }

        private void HandleManagerRegistered(IManager newManager)
        {
            if (newManager is ScoreManager scoreManager)
            {
                _scoreManager = scoreManager;
                GlobalManagers.Instance.OnManagerRegistered -= HandleManagerRegistered;
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || _isCollected) return;

            //check for players in range
            Collider2D col = Physics2D.OverlapCircle(transform.position, pickupRadius, playerLayer);

            if (col == null) return;
            if (col.TryGetComponent(out PlayerController2D playerController))
            {
                _isCollected = true;

                // Add score
                PlayerRef playerRef = playerController.Object.InputAuthority;
                if (_scoreManager) _scoreManager.AddScore(playerRef, 1);

                OnCollected?.Invoke();
            }
        }

        public void ResetCoin()
        {
            _isCollected = false;
        }


        #region Debug
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, pickupRadius);
        }
        
        #endregion
    }
}
