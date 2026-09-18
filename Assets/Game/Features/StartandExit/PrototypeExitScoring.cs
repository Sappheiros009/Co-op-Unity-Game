using System.Collections.Generic;
using UnityEngine;

namespace SlimeCoop.Prototype
{
    /// <summary>
    /// Local prototype implementation of the confirmed exit rule.
    /// The score values are clearly prototype trial values; the formula remains a pending design decision.
    /// </summary>
    public sealed class PrototypeExitScoring : MonoBehaviour
    {
        public const float ExitWindowSeconds = 5f;

        private readonly HashSet<int> _arrivedParticipantIds = new HashSet<int>();
        private int _stageStartParticipantCount;
        private float _runStartedAt;
        private float _firstArrivalAt = -1f;
        private float _settlementAt = -1f;
        private string _settlementReason = "Waiting for the first valid arrival.";
        private int _teamScore;

        public Transform EntryPoint { get; private set; }
        public bool HasStartedSettlement => _firstArrivalAt >= 0f;
        public bool IsSettled { get; private set; }
        public int ArrivedCount => _arrivedParticipantIds.Count;
        public int StageStartParticipantCount => _stageStartParticipantCount;
        public int TeamScore => _teamScore;
        public float SecondsRemaining => !HasStartedSettlement || IsSettled
            ? 0f
            : Mathf.Max(0f, (_firstArrivalAt + ExitWindowSeconds) - Time.time);
        public string SettlementReason => _settlementReason;
        public float SettlementElapsed => _settlementAt < 0f ? 0f : _settlementAt - _runStartedAt;

        public void Configure(Transform entryPoint, int stageStartParticipantCount, float runStartedAt)
        {
            EntryPoint = entryPoint;
            _stageStartParticipantCount = stageStartParticipantCount;
            _runStartedAt = runStartedAt;
            ResetRun();
        }

        private void Update()
        {
            if (!IsSettled && HasStartedSettlement && Time.time >= _firstArrivalAt + ExitWindowSeconds)
            {
                Settle("5-second arrival window expired");
            }
        }

        public void RegisterArrival(PrototypeParticipant participant)
        {
            if (participant == null || IsSettled || !participant.IsAlive || participant.HasEnteredExit)
            {
                return;
            }

            if (_firstArrivalAt < 0f)
            {
                _firstArrivalAt = Time.time;
            }

            _arrivedParticipantIds.Add(participant.ParticipantId);
            participant.MarkEnteredExit();

            var player = participant.GetComponent<PrototypeCapsulePlayer>();
            if (player != null)
            {
                player.LockAtExit();
            }

            if (_arrivedParticipantIds.Count >= _stageStartParticipantCount)
            {
                Settle("stage-start roster arrived early");
            }
        }

        public void ResetRun()
        {
            _arrivedParticipantIds.Clear();
            _firstArrivalAt = -1f;
            _settlementAt = -1f;
            _settlementReason = "Waiting for the first valid arrival.";
            _teamScore = 0;
            IsSettled = false;
        }

        private void Settle(string reason)
        {
            if (IsSettled)
            {
                return;
            }

            IsSettled = true;
            _settlementAt = Time.time;
            _settlementReason = reason;

            var speedScore = Mathf.RoundToInt(Mathf.Max(0f, 1000f - SettlementElapsed * 10f));
            var arrivalBonus = Mathf.Max(0, ArrivedCount - 1) * 250;
            _teamScore = speedScore + arrivalBonus;
        }
    }
}
