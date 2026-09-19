using UnityEngine;
using UnityEngine.InputSystem;

namespace SlimeCoop.Prototype
{
    public enum PrototypePlayerControl { Local, Server, Replica }

    [RequireComponent(typeof(CharacterController))]
    public sealed class PrototypeCapsulePlayer : MonoBehaviour
    {
        private CharacterController _controller;
        private PrototypeGame _game;
        private Vector3 _spawn, _lastSafe, _horizontal, _wallNormal;
        private Vector3 _wallJumpImpulse;
        private float _reattachAfter;
        private float _vertical, _pitch, _jumpAt = -99, _groundAt = -99, _exhaustedAt = -1;
        private bool _sprintToggle, _crouchToggle;
        private Vector3 _mantleStart, _mantleTarget;
        private float _mantleProgress = -1;
        public PrototypeParticipant Participant { get; private set; }
        public Camera ViewCamera { get; private set; }
        public PrototypeInteraction Interaction { get; private set; }
        public float Health => Participant == null ? 0 : Participant.Health;
        public float Stamina { get; private set; } = 100;
        public bool IsExitLocked => Participant != null && Participant.HasEnteredExit;
        public bool IsClimbing { get; private set; }
        public bool IsMantling => _mantleProgress >= 0;
        public bool NecklaceActive { get; private set; }
        public PrototypePlayerControl Control { get; private set; }
        public bool ReadsLocalInput => Control == PrototypePlayerControl.Local;

        public void Configure(PrototypeGame game, Vector3 spawn, Color color, int participantId = 0,
            string displayName = "나", PrototypePlayerControl control = PrototypePlayerControl.Local)
        {
            _game = game; _spawn = spawn; _lastSafe = spawn; Control = control;
            _controller = GetComponent<CharacterController>();
            _controller.height = 1.8f; _controller.radius = 0.4f; _controller.center = Vector3.up * 0.9f;
            _controller.stepOffset = 0.3f; _controller.slopeLimit = 50; _controller.minMoveDistance = 0;
            Participant = GetComponent<PrototypeParticipant>() ?? gameObject.AddComponent<PrototypeParticipant>();
            Participant.Configure(participantId, displayName);
            var oldBody = transform.Find("Player Capsule Body");
            if (oldBody != null) oldBody.gameObject.SetActive(false);
            var visual = new GameObject("CharacterParts"); visual.transform.SetParent(transform, false);
            visual.AddComponent<PrototypeSlimeBody>().Configure(Participant, color, ReadsLocalInput);
            var cameraObject = new GameObject("First Person Camera"); cameraObject.transform.SetParent(transform, false);
            cameraObject.tag = ReadsLocalInput ? "MainCamera" : "Untagged";
            ViewCamera = cameraObject.AddComponent<Camera>();
            ViewCamera.enabled = ReadsLocalInput;
            ViewCamera.nearClipPlane = 0.03f; ViewCamera.farClipPlane = 140; ViewCamera.fieldOfView = 72;
            cameraObject.transform.localPosition = Vector3.up * 1.55f;
            if (ReadsLocalInput) cameraObject.AddComponent<AudioListener>();
            Interaction = gameObject.AddComponent<PrototypeInteraction>(); Interaction.Configure(game, this);
            ResetForRun();
        }
        private void Update()
        {
            if (_game == null || Participant == null || !Application.isPlaying || !ReadsLocalInput) return;
            var keys = Keyboard.current; var mouse = Mouse.current;
            if (keys != null && keys.escapeKey.wasPressedThisFrame) SetCursor(false);
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && !PrototypeUi.IsModalOpen && Participant.CanAct)
                SetCursor(true);
            if (PrototypeUi.IsModalOpen) SetCursor(false);
            Look();
            if (!PrototypeUi.IsModalOpen && PrototypeInput.Pressed("Ping")) { _game.SetStatus(Participant.IsAlive ? "핑: 이쪽을 확인해 주세요!" : "구조 요청! 쓰러진 동료를 도와주세요."); PrototypeCues.Ping(transform.position); }
            if (!Participant.CanAct || _game.IsTransitioning) return;
            var input = new PrototypeMotorInput();
            if (!PrototypeUi.IsModalOpen && Cursor.lockState == CursorLockMode.Locked)
            {
                if (PrototypeInput.Pressed("Assist")) _game.CommandTeam(true);
                if (PrototypeInput.Pressed("Follow")) _game.CommandTeam(false);
                if (PrototypeInput.Pressed("Necklace")) ToggleNecklace();
                if (PrototypeInput.Pressed("Crouch")) _crouchToggle = !_crouchToggle;
                if (PrototypeInput.Pressed("Sprint")) _sprintToggle = !_sprintToggle;
                input.move = new Vector2((PrototypeInput.Held("Right") ? 1 : 0) - (PrototypeInput.Held("Left") ? 1 : 0),
                    (PrototypeInput.Held("Forward") ? 1 : 0) - (PrototypeInput.Held("Backward") ? 1 : 0));
                input.jump = PrototypeInput.Pressed("Jump");
                input.crouch = PrototypeSettings.Current.crouchToggle ? _crouchToggle : PrototypeInput.Held("Crouch");
                input.sprint = PrototypeSettings.Current.sprintToggle ? _sprintToggle : PrototypeInput.Held("Sprint");
                input.climb = mouse != null && mouse.leftButton.isPressed;
            }
            // Menus and the free cursor suppress input, never gravity or ongoing simulation.
            StepMotor(input, Time.deltaTime);
            CheckExitAndFall();
        }
        /// <summary>Called once per server simulation tick, not once per received packet.</summary>
        public bool StepServerInput(PrototypePlayerInput input, float serverDeltaTime)
        {
            if (Control != PrototypePlayerControl.Server || _game == null || Participant == null || !input.IsValid
                || !float.IsFinite(serverDeltaTime) || serverDeltaTime <= 0 || serverDeltaTime > .1f) return false;
            if (input.hasControl && Participant.CanAct)
            {
                transform.rotation = Quaternion.Euler(0, input.yaw, 0);
                _pitch = input.pitch; ViewCamera.transform.localRotation = Quaternion.Euler(_pitch, 0, 0);
                if (input.necklacePressed) ToggleNecklace();
            }
            if (input.hasControl && input.pingPressed && Participant.IsConnected)
                _game.SetStatus(Participant.DisplayName + (Participant.IsAlive ? ": 이쪽을 확인해 주세요!" : ": 구조 요청!"));
            var interaction = input.interaction;
            interaction.hasControl = input.hasControl && interaction.hasControl && !_game.IsTransitioning;
            Interaction.StepInteraction(interaction, serverDeltaTime);
            if (Participant.CanAct && !_game.IsTransitioning)
            {
                StepMotor(input.hasControl ? input.motor : default, serverDeltaTime);
                CheckExitAndFall();
            }
            return true;
        }
        private void CheckExitAndFall()
        {
            if (_game.Exit != null) _game.Exit.RegisterArrival(Participant);
            if (transform.position.y < -8)
            {
                Participant.Damage(25);
                Teleport(_lastSafe);
                _game.SetStatus("낙하 피해를 받고 마지막 안전 발판으로 돌아왔습니다.");
            }
        }
        private void Look()
        {
            if (ViewCamera == null) return;
            ViewCamera.fieldOfView = PrototypeSettings.Current.fieldOfView;
            var mouse = Mouse.current;
            if (mouse == null || Cursor.lockState != CursorLockMode.Locked || IsExitLocked) return;
            var delta = mouse.delta.ReadValue() * PrototypeSettings.Current.sensitivity;
            transform.Rotate(0, delta.x, 0);
            _pitch = Mathf.Clamp(_pitch - delta.y, -82, 82);
            ViewCamera.transform.localRotation = Quaternion.Euler(_pitch, 0, 0);
        }
        public void StepMotor(PrototypeMotorInput controls, float deltaTime)
        {
            if (Control == PrototypePlayerControl.Replica || Participant == null || !Participant.CanAct || _controller == null
                || !float.IsFinite(deltaTime) || deltaTime <= 0 || !float.IsFinite(controls.move.x) || !float.IsFinite(controls.move.y)) return;
            var tuning = PrototypeTuning.Current;
            var input = Vector2.ClampMagnitude(controls.move, 1);
            if (controls.jump) _jumpAt = Time.time;
            var crouch = controls.crouch;
            if (!crouch && Participant.IsCrouching)
                crouch = Physics.SphereCast(transform.position + Vector3.up * 0.6f, 0.38f, Vector3.up, out _, 0.9f, ~0, QueryTriggerInteraction.Ignore);
            Participant.IsCrouching = crouch;
            _controller.height = crouch ? 1f : 1.8f; _controller.center = Vector3.up * _controller.height / 2;
            ViewCamera.transform.localPosition = Vector3.Lerp(ViewCamera.transform.localPosition, Vector3.up * (crouch ? 0.8f : 1.55f), deltaTime * 15);
            if (StepMantle(deltaTime) || TryClimb(input, controls.climb, controls.jump, tuning, deltaTime)) return;
            var sprint = PrototypeMovementRules.CanSprint(input, controls.sprint, crouch, Stamina);
            Stamina = Mathf.Clamp(Stamina + (sprint ? -tuning.sprintDrain : tuning.staminaRecovery) * deltaTime, 0, tuning.staminaMaximum);
            var desired = (transform.forward * input.y + transform.right * input.x) * (crouch ? tuning.walkSpeed * 0.55f : sprint ? tuning.sprintSpeed : tuning.walkSpeed);
            var ice = Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, out var below, 0.6f) && below.collider.GetComponent<PrototypeIceSurface>() != null;
            var supported = false;
            foreach (var ally in _game.Participants)
                if (ally != Participant && ally.CanAct && ally.IsCrouching && Vector3.Distance(transform.position, ally.transform.position) < 2) supported = true;
            _horizontal = ice && !supported ? Vector3.Lerp(_horizontal, desired, deltaTime * 1.4f) : desired;
            if (_controller.isGrounded)
            {
                _groundAt = Time.time; if (_vertical < 0) _vertical = -2;
                if (!ice && Physics.Raycast(transform.position + Vector3.up * 0.15f, Vector3.down, out var floor, 0.5f, ~0, QueryTriggerInteraction.Ignore)
                    && floor.collider.GetComponent<PrototypeCyclePlatform>() == null) _lastSafe = transform.position;
            }
            if (Time.time - _jumpAt <= 0.2f && Time.time - _groundAt <= 0.15f)
            {
                var height = tuning.jumpHeight * (supported ? 2.5f : 1);
                _vertical = Mathf.Sqrt(2 * tuning.gravity * height); _jumpAt = -99; _groundAt = -99;
            }
            _vertical -= tuning.gravity * deltaTime;
            _controller.Move((_horizontal + _wallJumpImpulse + Vector3.up * _vertical) * deltaTime);
            _wallJumpImpulse = Vector3.MoveTowards(_wallJumpImpulse, Vector3.zero, 8f * deltaTime);
        }
        private bool TryClimb(Vector2 input, bool held, bool jump, PrototypeTuning tuning, float deltaTime)
        {
            if (Time.time < _reattachAfter) return false;
            if (!held) { IsClimbing = false; _exhaustedAt = -1; return false; }
            var direction = IsClimbing ? -_wallNormal : transform.forward;
            var chest = transform.position + Vector3.up;
            if (!Physics.SphereCast(chest, 0.2f, direction, out var hit, 1.1f, ~0, QueryTriggerInteraction.Ignore) ||
                hit.collider.GetComponent<PrototypeClimbSurface>() == null)
            {
                if (IsClimbing && input.y > 0)
                {
                    var probe = chest - _wallNormal * 0.8f + Vector3.up * 1.2f;
                    if (Physics.Raycast(probe, Vector3.down, out var ledge, 1.5f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        var destination = ledge.point + Vector3.up * .08f;
                        if (CanOccupy(destination))
                        { _mantleStart = transform.position; _mantleTarget = destination; _mantleProgress = 0; return StepMantle(deltaTime); }
                    }
                }
                IsClimbing = false; return false;
            }
            if (Stamina <= 0 && !hit.collider.GetComponent<PrototypeClimbSurface>().RopePlaced)
            {
                if (_exhaustedAt < 0) { _exhaustedAt = Time.time; _game.SetStatus("힘이 다했습니다! 동료의 끌어올리기 또는 로프를 이용하세요."); }
                if (Time.time - _exhaustedAt >= tuning.exhaustionGrace) { IsClimbing = false; _reattachAfter = Time.time + .7f; return false; }
                return true; // Brief grip grace, not free movement after stamina reaches zero.
            }
            else _exhaustedAt = -1;
            _wallNormal = IsClimbing ? Vector3.Slerp(_wallNormal, hit.normal, Mathf.Min(1, deltaTime * 12)) : hit.normal;
            IsClimbing = true; _vertical = 0;
            if (!hit.collider.GetComponent<PrototypeClimbSurface>().RopePlaced)
                Stamina = Mathf.Max(0, Stamina - tuning.climbDrain * deltaTime);
            if (jump)
            {
                IsClimbing = false; _vertical = Mathf.Sqrt(2 * tuning.gravity * tuning.jumpHeight);
                var away = ViewCamera.transform.forward; away.y = 0;
                _wallJumpImpulse = away.normalized * 5;
                _reattachAfter = Time.time + .3f;
                _controller.Move((_wallJumpImpulse + Vector3.up * _vertical) * deltaTime);
                _jumpAt = -99; return true;
            }
            var tangent = PrototypeMovementRules.ClimbRight(_wallNormal);
            _controller.Move((Vector3.up * input.y + tangent * input.x) * tuning.climbSpeed * deltaTime);
            return true;
        }
        private bool StepMantle(float deltaTime)
        {
            if (!IsMantling) return false;
            _mantleProgress = Mathf.Min(1, _mantleProgress + deltaTime / .4f);
            var raised = new Vector3(_mantleStart.x, _mantleTarget.y, _mantleStart.z);
            var target = _mantleProgress < .5f ? Vector3.Lerp(_mantleStart, raised, _mantleProgress * 2)
                : Vector3.Lerp(raised, _mantleTarget, (_mantleProgress - .5f) * 2);
            _controller.Move(target - transform.position);
            if (_mantleProgress >= 1) { _mantleProgress = -1; IsClimbing = false; _vertical = 0; _reattachAfter = Time.time + .3f; }
            return true;
        }
        public bool CanOccupy(Vector3 feet)
        {
            foreach (var hit in Physics.OverlapCapsule(feet + Vector3.up * .4f, feet + Vector3.up * 1.4f, .36f, ~0, QueryTriggerInteraction.Ignore))
                if (hit.GetComponentInParent<PrototypeParticipant>() == null) return false;
            return true;
        }
        public void ReceiveDamage(float amount)
        {
            var wasAlive = Participant.IsAlive; Participant.Damage(amount);
            if (wasAlive && !Participant.IsAlive) { Interaction.Drop(); _game.HandlePlayerDown(); }
        }
        public void ToggleNecklace() { if (Participant != null && Participant.CanAct) NecklaceActive = !NecklaceActive; }
        public void LockAtExit() { _vertical = 0; _horizontal = Vector3.zero; Interaction.Drop(); if (ReadsLocalInput) SetCursor(false); }
        public void Teleport(Vector3 position)
        {
            _controller.enabled = false; transform.position = position; _controller.enabled = true; _vertical = 0;
            _horizontal = Vector3.zero; _wallJumpImpulse = Vector3.zero; IsClimbing = false; _mantleProgress = -1;
            _groundAt = _jumpAt = -99; _exhaustedAt = -1; _reattachAfter = 0;
        }
        public void ResetForRun()
        {
            Teleport(_spawn); _pitch = 0; Stamina = PrototypeTuning.Current.staminaMaximum;
            Participant.ResetParticipant(); transform.rotation = Quaternion.identity;
        }
        public static void SetCursor(bool locked) { Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !locked; }
    }
}
