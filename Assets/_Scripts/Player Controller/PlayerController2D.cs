using Fusion;
using UnityEngine;
using ChaseTheCoin.Manager;
using Fusion.Addons.Physics;

namespace ChaseTheCoin.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController2D : NetworkBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpForce = 12f;
        
        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.2f;
        [SerializeField] private LayerMask groundLayer;

        private Rigidbody2D _rb;
        private Vector3 _spawnPosition;
        
        private TimerManager _timerManager;
        
        [Networked]
        private NetworkButtons _previousButtons { get; set; }
        
        [Networked]
        private NetworkBool _isRespawning { get; set; }

        public enum InputButtons
        {
            Jump
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        public override void Spawned()
        {
            _spawnPosition = transform.position;

            if (GlobalManagers.Instance != null)
            {
                _timerManager = GlobalManagers.Instance.GetManager<TimerManager>();
                if (_timerManager == null)
                {
                    GlobalManagers.Instance.OnManagerRegistered += HandleManagerRegistered;
                }
            }
        }

        private void HandleManagerRegistered(IManager newManager)
        {
            if (newManager is TimerManager timerManager)
            {
                _timerManager = timerManager;
                GlobalManagers.Instance.OnManagerRegistered -= HandleManagerRegistered;
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (GlobalManagers.Instance != null)
            {
                GlobalManagers.Instance.OnManagerRegistered -= HandleManagerRegistered;
            }
        }

        public override void FixedUpdateNetwork()
        {
            /*if (_isRespawning)
            {
                Respawn();
                if (HasStateAuthority)
                {
                    _isRespawning = false;
                }
            }
            */

            if (GetInput(out NetworkInputData data))
            {
                bool canMove = !(_timerManager != null && _timerManager.State != MatchState.Playing);

                if (!canMove)
                {
                    _rb.linearVelocity = new Vector2(0, _rb.linearVelocity.y);
                    
                    return;
                }

                // Movement
                _rb.linearVelocity = new Vector2(data.MovementInput * moveSpeed, _rb.linearVelocity.y);

                // Jumping
                bool isGrounded = false;
                if (groundCheck != null)
                {
                    isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
                }
                else
                {
                    // Fallback
                    isGrounded = Mathf.Abs(_rb.linearVelocity.y) < 0.01f;
                }

                // WasPressed handles detecting the exact tick the button was pressed.
                var pressedButtons = data.Buttons.GetPressed(_previousButtons);
                
                if (pressedButtons.IsSet(InputButtons.Jump) && isGrounded)
                {
                    _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
                }

                // Store current buttons for the next tick to compare what "WasPressed"
                _previousButtons = data.Buttons;
            }
        }

        private void Respawn()
        {
            var nt = GetComponent<NetworkRigidbody>();
            if (nt != null)
            {
                nt.Teleport(_spawnPosition);
            }
            
            _rb.linearVelocity = Vector2.zero;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!HasStateAuthority) return;

            if (other.gameObject.layer == LayerMask.NameToLayer("World Edge"))
            {
                //_isRespawning = true;
                Respawn();
            }
        }

        #region Debug
        
        private void OnDrawGizmosSelected()
        {
            if (groundCheck != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            }
        }
        #endregion
    }
}
