using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlimeCoop.Prototype
{
    public enum PrototypeSpecialty { Healer, Scout, Carrier }

    /// <summary>Local session simulator, NOT authenticated multiplayer or an anti-cheat service.</summary>
    public static class PrototypeSession
    {
        public static int PartySize = 4;
        public static PrototypeSpecialty SelectedSpecialty = PrototypeSpecialty.Healer;
        public static int Seed = 1839;
        public static bool AllChaptersForTesting = true;
        public static bool Practice;
        public static int Chapter { get; private set; }
        public static int Stage { get; private set; } = 1;
        public static int RunScore { get; private set; }
        public static float RunSeconds { get; private set; }
        public static int StartingCount { get; private set; } = 4;
        public static int RoomOwner { get; private set; }
        public static string RunId { get; private set; } = "none";
        public static string LastResult { get; private set; } = "";
        public static readonly List<string> Journal = new List<string>();
        public static readonly List<string> Events = new List<string>();
        private static readonly HashSet<string> Requests = new HashSet<string>();
        private static readonly HashSet<int> Disconnected = new HashSet<int>();
        private static readonly HashSet<int> Kicked = new HashSet<int>();
        private static readonly HashSet<int> SettledStages = new HashSet<int>();

        public static void BeginChapter(int chapter)
        {
            Chapter = Mathf.Clamp(chapter, 1, 7);
            Stage = 1; RunScore = 0; RunSeconds = 0;
            StartingCount = Mathf.Clamp(PartySize, 2, 4); RoomOwner = 0;
            RunId = Guid.NewGuid().ToString("N");
            Requests.Clear(); Disconnected.Clear(); Kicked.Clear(); SettledStages.Clear();
            Journal.Clear(); LastResult = "";
            Record("런 시작", $"챕터 {Chapter}, {StartingCount}인, 시드 {Seed} (로컬 시험)");
        }

        public static bool IsConnected(int id) => id >= 0 && id < StartingCount && !Disconnected.Contains(id) && !Kicked.Contains(id);
        public static void BeginNetworkChapter(int chapter, string runId, int count, int seed, int ownerActor = 0)
        {
            ValidateNetworkStage(chapter, 1, runId, count);
            if (ownerActor < 0 || ownerActor >= count) throw new ArgumentOutOfRangeException(nameof(ownerActor));
            PartySize = count; Seed = seed; Practice = false;
            BeginChapter(chapter); RunId = runId; RoomOwner = ownerActor;
        }
        // Client-only map construction context. Scores and gameplay remain read-only snapshot fields.
        public static void PrepareReplicaStage(int chapter, int stage, string runId, int count, int seed)
        {
            ValidateNetworkStage(chapter, stage, runId, count);
            Chapter = chapter; Stage = stage; RunId = runId; PartySize = StartingCount = count; Seed = seed;
            Practice = false; Disconnected.Clear(); Kicked.Clear(); Requests.Clear();
        }
        private static void ValidateNetworkStage(int chapter, int stage, string runId, int count)
        {
            if (chapter < 1 || chapter > 7 || stage < 1 || stage > 6 || count < 2 || count > 4
                || string.IsNullOrWhiteSpace(runId) || runId.Length > 64) throw new ArgumentException("Invalid network stage.");
        }
        public static void Disconnect(int id)
        {
            if (!IsConnected(id)) return;
            Disconnected.Add(id);
            if (RoomOwner == id)
            {
                RoomOwner = -1;
                for (var i = 0; i < StartingCount; i++) if (IsConnected(i)) { RoomOwner = i; break; }
            }
            Record("연결 끊김 시험", $"참가자 {id}, 방장 {RoomOwner}; 명단·점수·집계 유지");
        }
        public static bool Reconnect(int id)
        {
            if (id < 0 || id >= StartingCount || Kicked.Contains(id)) return false;
            Disconnected.Remove(id);
            if (RoomOwner < 0) RoomOwner = id;
            Record("재접속 시험", $"참가자 {id}");
            return true;
        }

        public static bool ValidateRequest(string requestId, int boundActorId, PrototypeParticipant actor,
            Vector3 target, float range, out string reason)
        {
            reason = "";
            if (string.IsNullOrWhiteSpace(requestId) || requestId.Length > 96) reason = "요청 ID 오류";
            else if (actor == null || actor.ParticipantId != boundActorId) reason = "소유권 오류";
            else if (!actor.CanAct || !IsConnected(boundActorId)) reason = "행동 불가 상태";
            else if (!Finite(target) || !Finite(actor.transform.position) || float.IsNaN(range) || float.IsInfinity(range) || range < 0 || Vector3.Distance(actor.transform.position, target) > range) reason = "거리·좌표 오류";
            else if (Requests.Contains(requestId)) reason = "중복 요청";
            else if (Requests.Count >= 4096) reason = "요청 한도";
            if (reason.Length > 0) { Record("요청 거부", reason); return false; }
            Requests.Add(requestId);
            return true;
        }
        private static bool Finite(Vector3 p) => !(float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z) || float.IsInfinity(p.x) || float.IsInfinity(p.y) || float.IsInfinity(p.z));

        public static bool ConfirmCheatForSimulation(int id, string reviewedEvidence)
        {
            if (!IsConnected(id) || string.IsNullOrWhiteSpace(reviewedEvidence)) return false;
            Disconnect(id); Kicked.Add(id);
            Record("보안 추방 시험", $"참가자 {id}; 계정 제재는 별도 운영자 검토. 실제 Game Ban 요청 없음");
            return true;
        }
        public static bool CompleteStage(int score, float elapsed)
        {
            if (Chapter == 0 || !SettledStages.Add(Stage)) return false;
            RunScore += Mathf.Max(0, score); RunSeconds += Mathf.Max(0, elapsed);
            LastResult = $"챕터 {Chapter} / 구간 {Stage}: 팀 {RunScore}점 · {RunSeconds:0.0}초 (시험 배점)";
            Record("구간 정산", LastResult);
            return true;
        }
        public static bool AdvanceStage()
        {
            if (Stage >= 6 || !SettledStages.Contains(Stage)) return false;
            Stage++; Requests.Clear(); return true;
        }
        public static void Wipe()
        {
            LastResult = $"챕터 {Chapter}에서 전멸했습니다. 런 점수·임시 진행 초기화. 챕터 처음부터 재도전하세요.";
            RunScore = 0; RunSeconds = 0; Stage = 1; Chapter = 0; Journal.Clear();
            Requests.Clear(); SettledStages.Clear(); Record("전멸", LastResult);
        }
        public static void Record(string type, string detail)
        {
            if (Events.Count >= 100) Events.RemoveAt(0);
            Events.Add(type + " | " + detail);
            Debug.Log("[PrototypeLocal] " + type + " | " + detail);
        }
    }
}
