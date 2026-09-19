using System.Collections.Generic;
using UnityEngine;

namespace SlimeCoop.Prototype
{
    /// <summary>Fixed participant-count puzzle; no role reduction after death or disconnect.</summary>
    public sealed class PrototypeStageObjective : MonoBehaviour
    {
        private PrototypeGame _game;
        private readonly List<Transform> _pads = new List<Transform>();
        private readonly HashSet<int> _records = new HashSet<int>();
        private readonly List<PrototypeInteractable> _devices = new List<PrototypeInteractable>();
        private MaterialPropertyBlock _warningColor;
        public Renderer AttackArea { get; set; }
        private float _charge, _phaseStarted;
        private int _round;
        public int RequiredRoles { get; private set; }
        public int ActiveRoles { get; private set; }
        public bool KeyInserted { get; private set; }
        public bool RegionalReady { get; private set; }
        public bool BoxReady { get; private set; }
        public bool Completed { get; private set; }
        public float Charge => _charge / PrototypeTuning.Current.puzzleHoldSeconds;
        public bool IsBoss => _game != null && _game.StageNumber == 6;
        public float BossPhase => (Time.time - _phaseStarted) % 10f;
        public bool BossDanger => IsBoss && !Completed && BossPhase >= 8f;
        public bool BossWarning => IsBoss && !Completed && BossPhase >= 5f && BossPhase < 8f;
        public int BossRounds => _round;
        public bool RequiresKey => _game != null && _game.StageNumber >= 2;
        public bool RequiresBox => _game != null && _game.StageNumber >= 3 && _game.StageNumber != 6;
        public PrototypeCarryable RequiredBox { get; set; }
        public Vector3 BoxSocket { get; set; }
        public IReadOnlyList<Transform> Pads => _pads;
        public string RegionalLabel => _game.ChapterNumber switch
        {
            1 => "광산 전력 복구", 2 => "냉각 밸브 두 곳 가동", 3 => "오염원 두 곳 정화",
            4 => "수문과 부유물 계류", 5 => "광장 기록 두 개 발견", 6 => "빙판 지지대 고정", _ => "고향 기억 장치 연결"
        };
        public string Description => Completed ? "출구 개방 · 한 명이 먼저 들어가면 5초 집계"
            : $"{RegionalLabel}: {(RegionalReady ? "완료" : _records.Count + "/2")}\n" +
              (RequiresKey ? $"열쇠 {(KeyInserted ? "연결 완료" : "미연결")}  " : "") +
              (RequiresBox ? $"상자 {(BoxReady ? "설치 완료" : "소켓으로 운반")}\n" : "\n") +
              $"동시 장치 {ActiveRoles}/{RequiredRoles} · 충전 {Charge:P0}" + (IsBoss ? $" · 보스 장치 {_round}/3" : "");

        public void Configure(PrototypeGame game)
        {
            _game = game; RequiredRoles = Application.isPlaying ? PrototypeSession.StartingCount : 4;
            _phaseStarted = Time.time; game.RegisterObjective(this);
        }
        public void AddPad(Transform pad) { _pads.Add(pad); }
        public void RegisterDevice(PrototypeInteractable device) { _devices.Add(device); }
        public Vector3 GuidanceTarget(Vector3 from)
        {
            if (Completed) return _game.Exit.EntryPoint.position;
            PrototypeInteractable nearest = null; var distance = float.PositiveInfinity;
            foreach (var device in _devices)
            {
                if (device.IsUsed || device.Kind == PrototypeInteractionKind.KeySocket) continue;
                var next = Vector3.SqrMagnitude(device.transform.position - from);
                if (next < distance) { nearest = device; distance = next; }
            }
            return nearest != null ? nearest.transform.position : _pads.Count > 0 ? _pads[0].position : _game.Exit.EntryPoint.position;
        }
        public bool InsertKey(PrototypeParticipant actor)
        {
            if (actor == null || !actor.CanAct || !actor.Inventory.Has(PrototypeItemKind.Key)) return false;
            // Key is a reusable tool, not a consumable. Never delete it upon use.
            KeyInserted = true; return true;
        }
        public bool ActivateRegion(int id)
        {
            var added = _records.Add(id);
            RegionalReady = _records.Count >= 2;
            return added;
        }
        private void Update()
        {
            if (AttackArea != null)
            {
                AttackArea.enabled = BossWarning || BossDanger;
                _warningColor ??= new MaterialPropertyBlock();
                var color = BossDanger ? new Color(1,.08f,.02f) : new Color(1,.65f,.05f);
                _warningColor.SetColor("_BaseColor", color); _warningColor.SetColor("_EmissionColor", color);
                AttackArea.SetPropertyBlock(_warningColor);
            }
            if (_game == null || Completed) return;
            BoxReady = RequiredBox != null && !RequiredBox.IsCarried && Vector3.Distance(RequiredBox.transform.position, BoxSocket) < 2f;
            var assigned = new HashSet<int>();
            foreach (var pad in _pads)
            {
                foreach (var p in _game.Participants)
                {
                    var delta = p.transform.position - pad.position;
                    if (!p.CanAct || assigned.Contains(p.ParticipantId) || Mathf.Abs(delta.y) > 2f || new Vector2(delta.x, delta.z).magnitude > 1.3f) continue;
                    assigned.Add(p.ParticipantId); break;
                }
            }
            ActiveRoles = assigned.Count;
            var ready = RegionalReady && (!RequiresKey || KeyInserted) && (!RequiresBox || BoxReady);
            if (BossDanger)
            {
                _charge = 0;
                foreach (var p in _game.Participants)
                    if (p.CanAct && p.transform.position.z > 44 && p.transform.position.z < 54 && Mathf.Abs(p.transform.position.x) < 7f) p.Damage(18f * Time.deltaTime);
            }
            else if (ready && ActiveRoles >= RequiredRoles) _charge += Time.deltaTime;
            else _charge = Mathf.Max(0, _charge - Time.deltaTime * 2);
            if (_charge < PrototypeTuning.Current.puzzleHoldSeconds) return;
            _charge = 0; _round++;
            if (IsBoss && _round < 3) { _phaseStarted = Time.time - 5f; _game.SetStatus("장치 명중! 노란 공격 예고 3초. 가장자리로 대피하세요."); PrototypeCues.Ping(new Vector3(0,1,49)); return; }
            Completed = true; _game.Exit.SetGateOpen(true);
            _game.SetStatus("협동 장치 완료. 출구가 열렸습니다. 함께 도착하면 팀 점수가 늘어납니다.");
        }
    }
}
