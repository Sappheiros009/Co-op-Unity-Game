using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SlimeCoop.Prototype
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        private PrototypeGame _game;
        private TextMeshProUGUI _objective, _team, _vitals, _prompt, _status, _inventory, _danger, _controls, _crosshair;
        private float _refreshAt;
        public void Configure(PrototypeGame game)
        {
            _game = game;
            var canvas = PrototypeUi.Canvas(transform, "GameplayHUD");
            var top = PrototypeUi.Panel(canvas, "ObjectivePanel", .018f,.025f,.38f,.22f);
            PrototypeUi.Text(top, "Stage", game.ChapterDisplayName + $"  /  {(game.StageNumber == 6 ? "BOSS" : game.StageNumber + " / 5")}", .04f,.03f,.92f,.24f, 22, PrototypeUi.Accent);
            _objective = PrototypeUi.Text(top, "Objective", "", .04f,.28f,.92f,.68f, 18);
            var team = PrototypeUi.Panel(canvas, "TeamPanel", .74f,.025f,.242f,.28f);
            _team = PrototypeUi.Text(team, "Team", "", .05f,.04f,.9f,.9f, 18);
            _danger = PrototypeUi.Text(canvas, "Danger", "", .34f,.29f,.4f,.085f, 26, Color.yellow);
            _danger.alignment = TextAlignmentOptions.Center;
            var bottom = PrototypeUi.Panel(canvas, "PlayerPanel", .018f,.77f,.38f,.2f);
            _vitals = PrototypeUi.Text(bottom, "Vitals", "", .04f,.06f,.92f,.23f, 21, PrototypeUi.Accent);
            _inventory = PrototypeUi.Text(bottom, "Inventory", "", .04f,.33f,.92f,.62f, 17);
            _crosshair = PrototypeUi.Text(canvas, "Crosshair", "+", .485f,.465f,.03f,.055f, 28); _crosshair.alignment = TextAlignmentOptions.Center;
            _prompt = PrototypeUi.Text(canvas, "Prompt", "", .32f,.61f,.52f,.11f, 21); _prompt.alignment = TextAlignmentOptions.Center;
            _status = PrototypeUi.Text(canvas, "Status", "", .42f,.8f,.55f,.1f, 18);
            _controls = PrototypeUi.Text(canvas, "Controls", "", .42f,.9f,.56f,.09f,15);
        }
        private void Update()
        {
            if (_game == null || _game.Player == null) return;
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame && !PrototypeUi.IsModalOpen) PrototypeSettingsPanel.Open(transform);
            if (PrototypeInput.Pressed("Journal") && !PrototypeUi.IsModalOpen) PrototypeRecordsPanel.OpenJournal(transform);
            if (keyboard != null && keyboard.f2Key.wasPressedThisFrame && !PrototypeUi.IsModalOpen) OpenDiagnostics();
            if (Time.unscaledTime < _refreshAt) return; _refreshAt = Time.unscaledTime + .1f;
            var player = _game.Player; var exit = _game.Exit;
            _objective.text = _game.Objective.Description;
            var builder = new StringBuilder("로컬 시험 동료 / 방장 " + (PrototypeSession.RoomOwner + 1) + "\n");
            foreach (var p in _game.Participants)
                builder.AppendLine($"{p.DisplayName}: {(!p.IsConnected ? "이탈" : p.HasEnteredExit ? "탈출·안전" : !p.IsAlive ? "구조 대기" : $"HP {p.Health:0}")}");
            builder.Append($"출구 {exit.ArrivedCount}/{exit.StageStartParticipantCount}  ");
            if (exit.HasStartedSettlement) builder.Append(exit.IsSettled ? "집계 완료" : $"{exit.SecondsRemaining:0.0}초");
            _team.text = builder.ToString();
            _vitals.text = $"체력 {player.Health:0}  /  스태미나 {player.Stamina:0}";
            builder.Clear();
            for (var i = 0; i < player.Participant.Inventory.Capacity; i++)
                builder.AppendLine($"{(player.Interaction.SelectedSlot == i ? "▶" : " ")} {i + 1}. {(i < player.Participant.Inventory.Items.Count ? PrototypeInventory.Label(player.Participant.Inventory.Items[i]) : "빈 슬롯")}");
            _inventory.text = builder.ToString();
            _prompt.text = player.Interaction.Prompt;
            _crosshair.text = string.IsNullOrEmpty(_prompt.text) ? "+" : "◇";
            _crosshair.color = string.IsNullOrEmpty(_prompt.text) ? Color.white : PrototypeUi.Accent;
            if (Cursor.lockState != CursorLockMode.Locked && !PrototypeUi.IsModalOpen && player.Participant.CanAct)
                _prompt.text = "화면을 클릭하면 시점·이동 조작을 시작합니다.";
            if (player.Interaction.RescueProgress > 0) _prompt.text += $"  {player.Interaction.RescueProgress:P0}";
            _status.text = _game.StatusMessage;
            var target = _game.Objective.GuidanceTarget(player.transform.position);
            _danger.text = !player.Participant.IsAlive ? "쓰러짐 · " + PrototypeInput.Label("Ping") + "로 구조 요청" : _game.Objective.BossDanger ? "위험! 붉은 바닥을 벗어나세요" :
                _game.Objective.BossWarning ? $"공격 예고! {Mathf.CeilToInt(8 - _game.Objective.BossPhase)}초 · 가장자리로" :
                player.IsClimbing && player.Stamina < 15 ? "힘이 다해 갑니다!" : player.NecklaceActive ? "목걸이 · " + PrototypeStoryMemory.Compass(player.transform.forward,target-player.transform.position) + " / 노출 위험 ↑" : "";
            _controls.text = $"{PrototypeInput.Label("Forward")}{PrototypeInput.Label("Left")}{PrototypeInput.Label("Backward")}{PrototypeInput.Label("Right")} 이동 · {PrototypeInput.Label("Sprint")} 달리기 · {PrototypeInput.Label("Jump")} 점프 · {PrototypeInput.Label("Crouch")} 앉기\n" +
                $"{PrototypeInput.Label("Interact")} 상호작용 · {PrototypeInput.Label("Use")} 사용 · {PrototypeInput.Label("Drop")} 내려놓기 · {PrototypeInput.Label("Assist")} 배치 · {PrototypeInput.Label("Follow")} 따라오기\n" +
                $"{PrototypeInput.Label("Journal")} 기록 · {PrototypeInput.Label("Necklace")} 목걸이 · Esc 설정 · F2 시험 도구";
        }
        private void OpenDiagnostics()
        {
            var panel = PrototypeUi.Modal(transform, "로컬 시뮬레이션 도구 · 실제 온라인/제재 아님");
            PrototypeUi.Text(panel,"Notice","이탈해도 역할 수·출구 명단은 줄지 않습니다. 전멸해야 런이 종료됩니다.\nF2 도구는 네트워크 검증을 대체하지 않습니다.",.04f,.13f,.92f,.1f,19);
            for (var i = 0; i < _game.Participants.Count; i++)
            {
                var actor = _game.Participants[i]; var id = actor.ParticipantId; var y = .27f + i * .12f;
                PrototypeUi.Text(panel,"Actor"+id,actor.DisplayName,.04f,y,.18f,.08f,20);
                PrototypeUi.Button(panel,"Disconnect"+id,"이탈",.23f,y,.16f,.08f,()=> { PrototypeSession.Disconnect(id); actor.SetConnected(false); });
                PrototypeUi.Button(panel,"Reconnect"+id,"재접속",.41f,y,.16f,.08f,()=> { if (PrototypeSession.Reconnect(id)) actor.SetConnected(true); });
                if (id > 0)
                {
                    PrototypeUi.Button(panel,"Hold"+id,"제자리 대기",.59f,y,.17f,.08f,()=>actor.GetComponent<PrototypeTeammate>().SetOrder(PrototypeBotOrder.Hold));
                    PrototypeUi.Button(panel,"Crouch"+id,"앉아 도움",.78f,y,.17f,.08f,()=>actor.GetComponent<PrototypeTeammate>().SetOrder(PrototypeBotOrder.Crouch));
                }
            }
            PrototypeUi.Text(panel,"Evidence","확인된 부정행위 → 세션 추방 → 계정 제재는 운영자 검토.\n이 시험판은 PC를 검사하거나 프로그램을 삭제하지 않습니다.",.04f,.76f,.92f,.1f,18);
            PrototypeUi.Button(panel,"Close","닫기",.3f,.89f,.4f,.08f,PrototypeUi.CloseModal);
        }
    }
}
