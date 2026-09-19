using System.Collections.Generic;
using UnityEngine;

namespace SlimeCoop.Prototype
{
    /// <summary>Local, deterministic exit aggregation. Trial score weights live in PrototypeBalance.</summary>
    public sealed class PrototypeExitScoring : MonoBehaviour
    {
        public const float ExitWindowSeconds = 5f;
        private readonly HashSet<int> _arrivals = new HashSet<int>();
        private readonly Dictionary<int, PrototypeParticipant> _roster = new Dictionary<int, PrototypeParticipant>();
        private float _runStartedAt, _firstArrivalAt = -1, _settlementAt = -1;
        public Transform EntryPoint { get; private set; }
        public bool IsGateOpen { get; private set; } = true;
        public bool HasStartedSettlement => _firstArrivalAt >= 0;
        public bool IsSettled { get; private set; }
        public int ArrivedCount => _arrivals.Count;
        public int StageStartParticipantCount { get; private set; }
        public int TeamScore { get; private set; }
        public float SecondsRemaining => !HasStartedSettlement || IsSettled ? 0 : Mathf.Max(0, _firstArrivalAt + ExitWindowSeconds - Time.time);
        public float SettlementElapsed => _settlementAt < 0 ? 0 : Mathf.Max(0, _settlementAt - _runStartedAt);
        public string SettlementReason { get; private set; } = "유효한 첫 도착 대기";
        public void Configure(Transform entry, int count, float startedAt)
        {
            EntryPoint = entry; StageStartParticipantCount = Mathf.Clamp(count, 2, 4);
            _runStartedAt = startedAt; _roster.Clear(); IsGateOpen = true; ResetRun();
        }
        public void BindRoster(IEnumerable<PrototypeParticipant> participants)
        {
            _roster.Clear();
            foreach (var p in participants) if (p != null) _roster[p.ParticipantId] = p;
        }
        public void SetGateOpen(bool value) { if (!HasStartedSettlement) IsGateOpen = value; }
        private void Update() { Tick(Time.time); }
        public void Tick(float now)
        {
            if (!IsSettled && HasStartedSettlement && now >= _firstArrivalAt + ExitWindowSeconds)
                Settle(_firstArrivalAt + ExitWindowSeconds, "5초 집계 마감");
        }
        public void RegisterArrival(PrototypeParticipant p) { TryRegisterArrival(p, Time.time); }
        public bool TryRegisterArrival(PrototypeParticipant p, float now)
        {
            // Deadline is exclusive in this prototype. A delayed Update cannot extend it.
            Tick(now);
            if (!IsGateOpen || IsSettled || p == null || !p.CanAct || p.ParticipantId < 0 ||
                p.ParticipantId >= StageStartParticipantCount || _arrivals.Contains(p.ParticipantId)) return false;
            if (_roster.Count > 0 && (!_roster.TryGetValue(p.ParticipantId, out var expected) || expected != p)) return false;
            var distance = EntryPoint == null ? float.PositiveInfinity : Vector3.Distance(p.transform.position, EntryPoint.position);
            if (!float.IsFinite(distance) || distance > 2.6f) return false;
            if (!float.IsFinite(now) || now < _runStartedAt) return false;
            if (_firstArrivalAt < 0) _firstArrivalAt = now;
            _arrivals.Add(p.ParticipantId); p.MarkEnteredExit();
            p.GetComponent<PrototypeCapsulePlayer>()?.LockAtExit();
            if (_arrivals.Count == StageStartParticipantCount) Settle(now, "시작 참가자 전원 도착");
            return true;
        }
        public void ResetRun()
        {
            _arrivals.Clear(); _firstArrivalAt = -1; _settlementAt = -1; IsSettled = false;
            TeamScore = 0; SettlementReason = "유효한 첫 도착 대기";
        }
        private void Settle(float at, string reason)
        {
            if (IsSettled) return;
            IsSettled = true; _settlementAt = at; SettlementReason = reason;
            var t = PrototypeTuning.Current;
            TeamScore = Mathf.RoundToInt(Mathf.Max(0, t.speedScoreMaximum - SettlementElapsed * t.speedScoreDecay))
                + Mathf.Max(0, ArrivedCount - 1) * t.arrivalBonusPerExtra;
        }
    }
}
