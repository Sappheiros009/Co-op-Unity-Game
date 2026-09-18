using UnityEngine;
using UnityEngine.InputSystem;

namespace SlimeCoop.Prototype
{
    /// <summary>
    /// First-person capsule controller for the local prototype.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PrototypeCapsulePlayer : MonoBehaviour
    {
        [SerializeField] private float walkSpeed = 4.5f;
        [SerializeField] private float sprintSpeed = 7f;
        [SerializeField] private float jumpHeight = 1.4f;
        [SerializeField] private float gravity = -22f;
        [SerializeField] private float mouseSensitivity = 0.08f;

        private CharacterController _controller;
        private PrototypeParticipant _participant;
        private PrototypeGame _game;
        private Camera _camera;
        private Vector3 _spawnPoint;
        private Vector3 _velocity;
        private float _pitch;
        private bool _exitLocked;
        private float _health;

        public PrototypeParticipant Participant => _participant;
        public float Health => _health;
        public bool IsExitLocked => _exitLocked;

        public void Configure(PrototypeGame game, Vector3 spawnPoint, Color bodyColor)
        {
            _game = game;
            _spawnPoint = spawnPoint;
            _controller = GetComponent<CharacterController>();
            _controller.height = 2f;
            _controller.radius = 0.48f;
            _controller.center = new Vector3(0f, 1f, 0f);
            _controller.stepOffset = 0.35f;
            _controller.slopeLimit = 50f;

            _participant = GetComponent<PrototypeParticipant>();
            if (_participant == null)
            {
                _participant = gameObject.AddComponent<PrototypeParticipant>();
            }

            _participant.Configure(0, "You");
            _health = 100f;
            _exitLocked = false;

            var body = transform.Find("Player Capsule Body");
            if (body == null)
            {
                var bodyObject = PrototypeVisuals.CreateCapsule(
                    "Player Capsule Body",
                    transform,
                    transform.position + Vector3.up,
                    bodyColor,
                    false);
                bodyObject.transform.localPosition = Vector3.up;
            }

            EnsureCamera();
            ResetForRun();
        }

        private void Update()
        {
            if (_game == null || _controller == null)
            {
                return;
            }

            HandleCursorAndLook();

            if (!_exitLocked)
            {
                HandleMovement();

                if (_game.Exit != null && Vector3.Distance(transform.position, _game.Exit.EntryPoint.position) < 1.7f)
                {
                    _game.Exit.RegisterArrival(_participant);
                }
            }
        }

        public void ReceiveDamage(float amount)
        {
            if (_exitLocked || !_participant.IsAlive)
            {
                return;
            }

            _health = Mathf.Max(0f, _health - amount);
            if (_health <= 0f)
            {
                _participant.MarkDown();
                _game.HandlePlayerDown();
            }
        }

        public void LockAtExit()
        {
            _exitLocked = true;
            _velocity = Vector3.zero;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void ResetForRun()
        {
            _controller.enabled = false;
            transform.position = _spawnPoint;
            transform.rotation = Quaternion.identity;
            _controller.enabled = true;
            _velocity = Vector3.zero;
            _pitch = 0f;
            _health = 100f;
            _exitLocked = false;
            _participant.ResetParticipant();
        }

        private void HandleMovement()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            var input = Vector2.zero;
            if (keyboard.wKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed) input.x -= 1f;
            input = Vector2.ClampMagnitude(input, 1f);

            var direction = transform.forward * input.y + transform.right * input.x;
            var speed = keyboard.leftShiftKey.isPressed ? sprintSpeed : walkSpeed;
            var movement = direction * speed;

            if (_controller.isGrounded && _velocity.y < 0f)
            {
                _velocity.y = -2f;
            }

            if (keyboard.spaceKey.wasPressedThisFrame && _controller.isGrounded)
            {
                _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            _velocity.y += gravity * Time.deltaTime;
            movement.y = _velocity.y;
            _controller.Move(movement * Time.deltaTime);
        }

        private void HandleCursorAndLook()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (mouse != null && mouse.leftButton.wasPressedThisFrame && !_exitLocked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (mouse == null || Cursor.lockState != CursorLockMode.Locked || _exitLocked)
            {
                return;
            }

            var delta = mouse.delta.ReadValue();
            transform.Rotate(Vector3.up, delta.x * mouseSensitivity);
            _pitch = Mathf.Clamp(_pitch - delta.y * mouseSensitivity, -82f, 82f);
            if (_camera != null)
            {
                _camera.transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }
        }

        private void EnsureCamera()
        {
            var existing = transform.Find("First Person Camera");
            if (existing != null)
            {
                _camera = existing.GetComponent<Camera>();
                return;
            }

            var cameraObject = new GameObject("First Person Camera");
            cameraObject.transform.SetParent(transform);
            cameraObject.transform.localPosition = new Vector3(0f, 1.55f, 0f);
            cameraObject.transform.localRotation = Quaternion.identity;
            cameraObject.tag = "MainCamera";
            _camera = cameraObject.AddComponent<Camera>();
            _camera.fieldOfView = 72f;
            _camera.nearClipPlane = 0.05f;
            _camera.farClipPlane = 150f;
        }
    }
}
