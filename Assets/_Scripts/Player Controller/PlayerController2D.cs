using Fusion;
using UnityEngine;
using ChaseTheCoin.Manager;

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
        
        [Networked]
        private NetworkButtons _previousButtons { get; set; }

        public enum InputButtons
        {
            Jump
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            
            // Ensure Physics2D ignores collisions between players
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer != -1)
            {
                gameObject.layer = playerLayer; // Set this game object to Player layer
                Physics2D.IgnoreLayerCollision(playerLayer, playerLayer, true);
            }
        }

        public override void FixedUpdateNetwork()
        {
            // Only apply inputs if we successfully get them from Fusion (valid for both Host/Server and the local client predicting)
            if (GetInput(out NetworkInputData data))
            {
                bool canMove = true;
                if (GlobalManagers.Instance != null)
                {
                    var timer = GlobalManagers.Instance.GetManager<TimerManager>();
                    if (timer != null && timer.State != MatchState.Playing)
                    {
                        canMove = false;
                    }
                }

                if (!canMove)
                {
                    // Keep gravity but block horizontal input
                    _rb.linearVelocity = new Vector2(0, _rb.linearVelocity.y);
                    
                    return;
                }

                // --- MOVEMENT ---
                // We directly set velocity on X axis based on input.
                _rb.linearVelocity = new Vector2(data.MovementInput * moveSpeed, _rb.linearVelocity.y);

                // --- JUMPING ---
                bool isGrounded = false;
                if (groundCheck != null)
                {
                    isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
                }
                else
                {
                    // Fallback just in case ground check isn't setup
                    // Increased tolerance from 0.01f to 0.1f because Unity physics often has micro-fluctuations in Y velocity when resting or sliding on colliders.
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
        
        // Optional: Draw Gizmos for ground check
        private void OnDrawGizmosSelected()
        {
            if (groundCheck != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            }
        }
    }
}
