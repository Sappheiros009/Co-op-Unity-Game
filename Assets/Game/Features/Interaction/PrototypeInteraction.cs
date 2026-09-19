using UnityEngine;
using UnityEngine.InputSystem;

namespace SlimeCoop.Prototype
{
    [System.Serializable]
    public struct PrototypeInteractionInput
    {
        public bool hasControl, interactPressed, interactHeld, dropPressed, usePressed;
        public int selectedSlot;
    }
    public sealed class PrototypeInteraction : MonoBehaviour
    {
        private PrototypeGame _game;
        private PrototypeCapsulePlayer _player;
        private PrototypeParticipant _rescueTarget;
        private PrototypeCarryable _carried;
        private float _rescueProgress;
        private int _selected;
        public string Prompt { get; private set; } = "";
        public float RescueProgress { get; private set; }
        public int SelectedSlot => _selected;
        private string PromptKey(string action) => _player.Control == PrototypePlayerControl.Server
            ? "{key:" + action + "}" : PrototypeInput.Label(action);
        public void Configure(PrototypeGame game, PrototypeCapsulePlayer player) { _game = game; _player = player; }
        public static PrototypeInteractionInput ReadInput()
        {
            var keys = Keyboard.current;
            return new PrototypeInteractionInput
            {
                hasControl = Cursor.lockState == CursorLockMode.Locked && !PrototypeUi.IsModalOpen,
                selectedSlot = keys == null ? -1 : keys.digit1Key.wasPressedThisFrame ? 0 : keys.digit2Key.wasPressedThisFrame ? 1 : keys.digit3Key.wasPressedThisFrame ? 2 : -1,
                interactPressed = PrototypeInput.Pressed("Interact"), interactHeld = PrototypeInput.Held("Interact"),
                dropPressed = PrototypeInput.Pressed("Drop"), usePressed = PrototypeInput.Pressed("Use")
            };
        }
        private void Update()
        {
            if (_player != null && _player.ReadsLocalInput) StepInteraction(ReadInput(), Time.deltaTime);
        }
        public void StepInteraction(PrototypeInteractionInput input, float deltaTime)
        {
            if (_player == null || _player.Control == PrototypePlayerControl.Replica || !_player.Participant.CanAct
                || (_player.ReadsLocalInput && PrototypeUi.IsModalOpen) || !input.hasControl)
            { ResetRescue(); Prompt = ""; if (_player != null && !_player.Participant.CanAct) Drop(); return; }
            if (!float.IsFinite(deltaTime) || deltaTime < 0) return;
            if (input.selectedSlot >= 0 && input.selectedSlot < _player.Participant.Inventory.Capacity) _selected = input.selectedSlot;
            if (input.dropPressed)
            {
                if (_carried != null) Drop();
                else if (_selected < _player.Participant.Inventory.Items.Count)
                {
                    var kind = _player.Participant.Inventory.Items[_selected];
                    if (_player.Participant.Inventory.Consume(kind)) _game.Map.DropItem(kind, transform.position + transform.forward + Vector3.up * .7f);
                }
            }
            var camera = _player.ViewCamera.transform;
            var range = PrototypeTuning.Current.interactionRange;
            Physics.Raycast(camera.position, camera.forward, out var hit, range, ~0, QueryTriggerInteraction.Ignore);
            PrototypeParticipant ally = null;
            foreach (var p in _game.Participants)
            {
                if (p == _player.Participant || !p.IsConnected || p.HasEnteredExit || Vector3.Distance(transform.position, p.transform.position) > range) continue;
                var direction = p.transform.position + Vector3.up * 0.7f - camera.position;
                if (Vector3.Dot(direction.normalized, camera.forward) < 0.35f) continue;
                if (Physics.Raycast(camera.position, direction.normalized, out var blocker, direction.magnitude, ~0, QueryTriggerInteraction.Ignore)
                    && blocker.collider.GetComponentInParent<PrototypeParticipant>() != p) continue;
                if (!p.IsAlive || transform.position.y - p.transform.position.y > 1.1f) { ally = p; break; }
            }
            // Ally assistance always takes priority over overlapping boxes.
            if (ally != null)
            {
                Prompt = PromptKey("Interact") + (ally.IsAlive ? ": 가까운 동료 끌어올리기" : " 유지: 쓰러진 동료 구조");
                if (!ally.IsAlive) Rescue(ally, input.interactHeld, deltaTime);
                else
                {
                    ResetRescue();
                    if (input.interactPressed)
                    {
                        var destination = transform.position - transform.forward * 1.1f;
                        if (_player.CanOccupy(destination))
                        {
                            var controlledPlayer = ally.GetComponent<PrototypeCapsulePlayer>();
                            var bot = ally.GetComponent<PrototypeTeammate>();
                            if (controlledPlayer != null) controlledPlayer.Teleport(destination);
                            else if (bot != null) bot.Teleport(destination);
                            else return;
                            _game.SetStatus("가까운 동료를 끌어올렸습니다.");
                        }
                        else _game.SetStatus("동료가 올라설 공간이 부족합니다. 안전한 위치로 이동하세요.");
                    }
                }
            }
            else
            {
                ResetRescue();
                var item = hit.collider == null ? null : hit.collider.GetComponent<PrototypeInteractable>();
                var box = hit.collider == null ? null : hit.collider.GetComponent<PrototypeCarryable>();
                Prompt = item != null ? PromptKey("Interact") + ": " + item.Label : box != null ? box.IsLarge ? PromptKey("Interact") + " 유지: 큰 상자 밀기" : PromptKey("Interact") + ": 상자 들기 / " + PromptKey("Drop") + ": 내려놓기" : "";
                if (item != null && input.interactPressed)
                {
                    if (item.TryUse(_player.Participant)) PrototypeCues.Ping(item.transform.position);
                    else _game.SetStatus("사용 조건 또는 빈 소지품 슬롯을 확인하세요.");
                }
                if (box != null)
                {
                    if (box.IsLarge && input.interactHeld) box.Push(camera.forward, _player.Participant.ParticipantId);
                    else if (input.interactPressed && _carried == null && box.PickUp(camera)) _carried = box;
                }
            }
            if (input.usePressed) UseSelected(hit.collider);
        }
        private void Rescue(PrototypeParticipant ally, bool held, float deltaTime)
        {
            var actor = _player.Participant;
            var medkit = actor.Inventory.Has(PrototypeItemKind.Medkit);
            if (!medkit && actor.Specialty != PrototypeSpecialty.Healer) { Prompt = "구조에는 회복팩 또는 힐러 특기가 필요합니다."; ResetRescue(); return; }
            if (!held) { ResetRescue(); return; }
            if (_rescueTarget != ally) { _rescueTarget = ally; _rescueProgress = 0; }
            var duration = medkit ? PrototypeTuning.Current.reviveSeconds : PrototypeTuning.Current.healerReviveSeconds;
            _rescueProgress += deltaTime; RescueProgress = _rescueProgress / duration;
            if (_rescueProgress < duration) return;
            if (ally.Revive())
            {
                if (medkit) actor.Inventory.Consume(PrototypeItemKind.Medkit);
                _game.SetStatus("동료 부활. 잠시 피해로부터 보호됩니다.");
            }
            ResetRescue();
        }
        private void ResetRescue() { _rescueTarget = null; _rescueProgress = 0; RescueProgress = 0; }
        public void Drop() { if (_carried == null) return; _carried.Drop(); _carried = null; }
        private void UseSelected(Collider target)
        {
            var inventory = _player.Participant.Inventory;
            if (_selected >= inventory.Items.Count) return;
            var kind = inventory.Items[_selected];
            if (kind == PrototypeItemKind.Medkit)
            {
                var recipient = target == null ? null : target.GetComponentInParent<PrototypeParticipant>();
                if (recipient == null) recipient = _player.Participant;
                if (!recipient.CanAct || recipient.Health >= 100) return;
                if (inventory.Consume(kind)) recipient.Heal(45);
            }
            else if (kind == PrototypeItemKind.Lure)
            {
                if (inventory.Consume(kind)) PrototypeLure.Create(_player.ViewCamera.transform.position + _player.ViewCamera.transform.forward * 2);
            }
            else if (kind == PrototypeItemKind.Rope)
            {
                if (target != null && target.GetComponent<PrototypeClimbSurface>() != null)
                {
                    var surface = target.GetComponent<PrototypeClimbSurface>(); surface.RopePlaced = true;
                    _game.SetStatus("표시된 벽에 로프를 설치했습니다. 이 벽은 스태미나 소모 없이 오를 수 있습니다. (시험 기능)");
                    PrototypeCues.Ping(target.transform.position);
                }
            }
            else if (target != null) target.GetComponent<PrototypeInteractable>()?.TryUse(_player.Participant);
        }
    }
}
