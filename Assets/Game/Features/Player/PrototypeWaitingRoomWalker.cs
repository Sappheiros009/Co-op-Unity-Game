using UnityEngine;
using UnityEngine.InputSystem;

namespace SlimeCoop.Prototype
{
    /// <summary>Safe first-person waiting-room locomotion; no chapter health, scoring, or puzzle simulation.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PrototypeWaitingRoomWalker : MonoBehaviour
    {
        private CharacterController _controller;
        private float _vertical, _pitch;
        private bool _crouchToggle, _sprintToggle;
        private Vector3 _spawn;
        public PrototypePlayerControl Control { get; private set; }
        public float Pitch => _pitch;
        public Camera ViewCamera { get; private set; }
        public PrototypeParticipant Participant { get; private set; }
        public static readonly Vector3 Spawn = new Vector3(0, .12f, -5.5f);
        public void Configure(int id = 0, string displayName = "나", PrototypePlayerControl control = PrototypePlayerControl.Local, Vector3? spawn = null)
        {
            Control = control; _spawn = spawn ?? Spawn;
            _controller = GetComponent<CharacterController>(); _controller.height = 1.8f;
            _controller.center = Vector3.up * .9f; _controller.radius = .4f;
            _controller.stepOffset = .3f; _controller.minMoveDistance = 0;
            Participant = gameObject.AddComponent<PrototypeParticipant>(); Participant.Configure(id, displayName);
            Participant.SetConnected(true);
            var parts = new GameObject("CharacterParts"); parts.transform.SetParent(transform, false);
            parts.AddComponent<PrototypeSlimeBody>().Configure(Participant, control == PrototypePlayerControl.Local ? new Color(.35f,.9f,.75f) : Color.HSVToRGB(.43f+id*.12f,.5f,1), control == PrototypePlayerControl.Local);
            var cameraObject = new GameObject("Waiting First Person Camera"); cameraObject.transform.SetParent(transform, false);
            cameraObject.tag = control == PrototypePlayerControl.Local ? "MainCamera" : "Untagged";
            ViewCamera = cameraObject.AddComponent<Camera>(); ViewCamera.nearClipPlane = .03f; ViewCamera.farClipPlane = 80;
            ViewCamera.transform.localPosition = Vector3.up * 1.55f; ViewCamera.fieldOfView = 72;
            ViewCamera.enabled = control == PrototypePlayerControl.Local;
            if (control == PrototypePlayerControl.Local) { ViewCamera.fieldOfView = PrototypeSettings.Current.fieldOfView; cameraObject.AddComponent<AudioListener>(); }
            Teleport(_spawn); if (control == PrototypePlayerControl.Replica) _controller.enabled = false;
        }
        private void Update()
        {
            if (_controller == null || ViewCamera == null || Control != PrototypePlayerControl.Local) return;
            var mouse = Mouse.current;
            if (PrototypeUi.IsModalOpen || (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
                PrototypeCapsulePlayer.SetCursor(false);
            else if (mouse != null && mouse.leftButton.wasPressedThisFrame) PrototypeCapsulePlayer.SetCursor(true);
            var controls = new PrototypeMotorInput();
            if (!PrototypeUi.IsModalOpen && Cursor.lockState == CursorLockMode.Locked)
            {
                if (mouse != null)
                {
                    var delta = mouse.delta.ReadValue() * PrototypeSettings.Current.sensitivity;
                    transform.Rotate(0, delta.x, 0); _pitch = Mathf.Clamp(_pitch - delta.y, -82, 82);
                    ViewCamera.transform.localRotation = Quaternion.Euler(_pitch, 0, 0);
                }
                if (PrototypeInput.Pressed("Crouch")) _crouchToggle = !_crouchToggle;
                if (PrototypeInput.Pressed("Sprint")) _sprintToggle = !_sprintToggle;
                controls.move = new Vector2((PrototypeInput.Held("Right") ? 1 : 0) - (PrototypeInput.Held("Left") ? 1 : 0),
                    (PrototypeInput.Held("Forward") ? 1 : 0) - (PrototypeInput.Held("Backward") ? 1 : 0));
                controls.jump = PrototypeInput.Pressed("Jump");
                controls.sprint = PrototypeSettings.Current.sprintToggle ? _sprintToggle : PrototypeInput.Held("Sprint");
                controls.crouch = PrototypeSettings.Current.crouchToggle ? _crouchToggle : PrototypeInput.Held("Crouch");
            }
            ViewCamera.fieldOfView = PrototypeSettings.Current.fieldOfView;
            StepMotor(controls, Time.deltaTime);
        }
        public void StepMotor(PrototypeMotorInput input, float deltaTime)
        {
            if (_controller == null || Control == PrototypePlayerControl.Replica || !float.IsFinite(deltaTime) || deltaTime <= 0 || deltaTime > .1f
                || !float.IsFinite(input.move.x) || !float.IsFinite(input.move.y)) return;
            var move = Vector2.ClampMagnitude(input.move, 1);
            var height = input.crouch ? 1.05f : 1.8f;
            _controller.height = height; _controller.center = Vector3.up * height * .5f;
            Participant.IsCrouching = input.crouch;
            ViewCamera.transform.localPosition = Vector3.Lerp(ViewCamera.transform.localPosition, Vector3.up * (input.crouch ? .85f : 1.55f), deltaTime * 15);
            if (_controller.isGrounded)
            {
                _vertical = -2;
                if (input.jump) _vertical = Mathf.Sqrt(2 * 22 * 1.2f);
            }
            _vertical -= 22 * deltaTime;
            var speed = input.crouch ? 2.2f : input.sprint && move.y > 0 ? 6 : 4;
            _controller.Move(((transform.forward * move.y + transform.right * move.x) * speed + Vector3.up * _vertical) * deltaTime);
            if (transform.position.y < -3) Teleport(_spawn);
        }
        public bool StepServerInput(PrototypePlayerInput input, float seconds)
        {
            if (Control != PrototypePlayerControl.Server || !input.IsValid || !float.IsFinite(seconds) || seconds <= 0 || seconds > .1f) return false;
            SetLook(input.yaw,input.pitch);
            StepMotor(input.hasControl ? input.motor : new PrototypeMotorInput(),seconds); return true;
        }
        public void SetLook(float yaw, float pitch)
        {
            if (!float.IsFinite(yaw) || !float.IsFinite(pitch)) return;
            transform.rotation = Quaternion.Euler(0,Mathf.Repeat(yaw,360),0); _pitch = Mathf.Clamp(pitch,-82,82);
            ViewCamera.transform.localRotation = Quaternion.Euler(_pitch,0,0);
        }
        public void EnableReplicaView()
        {
            if (Control != PrototypePlayerControl.Replica) return;
            transform.Find("CharacterParts/Head").gameObject.SetActive(false);
            ViewCamera.enabled = true; ViewCamera.tag = "MainCamera";
            if (ViewCamera.GetComponent<AudioListener>() == null) ViewCamera.gameObject.AddComponent<AudioListener>();
        }
        public void ApplyReplicaPose(Vector3 position, float yaw, float pitch, bool crouching, float cameraHeight)
        {
            if (Control != PrototypePlayerControl.Replica) return;
            transform.position = position; SetLook(yaw,pitch); Participant.IsCrouching = crouching;
            ViewCamera.transform.localPosition = Vector3.up * cameraHeight;
        }
        public void Teleport(Vector3 position)
        {
            _controller.enabled = false; transform.position = position; _controller.enabled = Control != PrototypePlayerControl.Replica; _vertical = 0;
        }
        public void AimAt(Vector3 point)
        {
            var direction = point - ViewCamera.transform.position;
            var rotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Euler(0, rotation.eulerAngles.y, 0);
            _pitch = Mathf.Clamp(Mathf.DeltaAngle(0, rotation.eulerAngles.x), -82, 82);
            ViewCamera.transform.localRotation = Quaternion.Euler(_pitch, 0, 0);
        }
    }
}
