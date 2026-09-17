using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ZOG.Player
{
    /// <summary>
    /// Server-authoritative top-down movement. The owning client samples input and sends it up;
    /// the server is the only peer that actually moves the character.
    ///
    /// Authority lives on the server on purpose. A client that cannot move itself also cannot
    /// teleport, and - more importantly for this game - a server that owns every position is the
    /// only thing that can withhold positions from clients who should not see them. See CLAUDE.md.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : NetworkBehaviour
    {
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float gravity = -20f;

        private CharacterController _controller;

        // Owner-side: last input we sent, so we only spend bandwidth when it actually changes.
        private Vector2 _lastSentInput;

        // Server-side: the input we are currently applying for this player.
        private Vector2 _serverInput;
        private float _verticalVelocity;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                TopDownCamera.SetTarget(transform);
            }
        }

        private void Update()
        {
            if (IsOwner)
            {
                SampleInput();
            }

            if (IsServer)
            {
                ApplyMovement();
            }
        }

        private void SampleInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            var input = new Vector2(
                (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
                (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f));

            input = Vector2.ClampMagnitude(input, 1f);

            // Only tell the server when the input actually changed. Sending every frame would work
            // but wastes bandwidth we will want back once there are many players on one server.
            if ((input - _lastSentInput).sqrMagnitude > 0.0001f)
            {
                _lastSentInput = input;
                SubmitInputRpc(input);
            }
        }

        [Rpc(SendTo.Server)]
        private void SubmitInputRpc(Vector2 input)
        {
            // Clamp server-side too: never trust a client to send a sane magnitude.
            _serverInput = Vector2.ClampMagnitude(input, 1f);
        }

        private void ApplyMovement()
        {
            var horizontal = new Vector3(_serverInput.x, 0f, _serverInput.y) * moveSpeed;

            if (_controller.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -1f;
            }
            else
            {
                _verticalVelocity += gravity * Time.deltaTime;
            }

            var motion = horizontal;
            motion.y = _verticalVelocity;
            _controller.Move(motion * Time.deltaTime);

            // Face the direction of travel - top-down reads badly without it.
            if (horizontal.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(horizontal.normalized, Vector3.up),
                    12f * Time.deltaTime);
            }
        }
    }
}
