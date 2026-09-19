using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SlimeCoop.Prototype
{
    [Serializable] public sealed class PrototypeTeamRecord
    {
        public string runId, scoreRuleVersion, gameBuildVersion = "prototype-v2";
        public int chapter, score, participants;
        public float seconds;
    }
    [Serializable] public sealed class PrototypeProgress
    {
        public int schemaVersion = 1, unlockedChapter = 1, completedMask, fragmentMask;
        public string endingChoice = "";
        public List<string> achievements = new List<string>();
        public List<string> memories = new List<string>();
        public List<PrototypeTeamRecord> records = new List<PrototypeTeamRecord>();
    }
    public static class PrototypeSave
    {
        private static PrototypeProgress _progress;
        private static string _rootOverride;
        public static string RootOverride
        {
            get => _rootOverride;
            set
            {
                if (_rootOverride == value) return;
                _rootOverride = value; Reload(); PrototypeSettings.Reload();
            }
        }
        public static string Root => RootOverride ?? Path.Combine(Application.persistentDataPath, "PrototypeV2");
        public static string Error { get; private set; } = "";
        public static bool ReadProtected { get; private set; }
        public static bool HasPendingProgress { get; private set; }
        public static PrototypeProgress Progress => _progress ??= Read();
        public static void Reload() { _progress = null; Error = ""; ReadProtected = false; HasPendingProgress = false; }

        private static PrototypeProgress Read()
        {
            var path = Path.Combine(Root, "progress.json");
            try
            {
                var raw = File.ReadAllText(path);
                if (!raw.TrimStart().StartsWith("{") || !raw.Contains("schemaVersion")) throw new InvalidDataException("저장 형식 오류");
                var data = JsonUtility.FromJson<PrototypeProgress>(raw);
                if (data == null || data.schemaVersion < 0 || data.schemaVersion > 1) throw new InvalidDataException("지원하지 않는 저장 버전");
                data.schemaVersion = 1;
                data.achievements ??= new List<string>(); data.records ??= new List<PrototypeTeamRecord>(); data.memories ??= new List<string>();
                if (data.unlockedChapter < 1 || data.unlockedChapter > 7 || data.completedMask < 0 || data.completedMask > 127 ||
                    data.fragmentMask < 0 || data.fragmentMask > 31) throw new InvalidDataException("진행 범위 오류");
                foreach (var record in data.records)
                    if (record == null || string.IsNullOrEmpty(record.runId) || string.IsNullOrEmpty(record.scoreRuleVersion) ||
                        record.chapter < 1 || record.chapter > 7 || record.participants != 4 || record.score < 0 ||
                        !float.IsFinite(record.seconds) || record.seconds < 0) throw new InvalidDataException("팀 기록 형식 오류");
                return data;
            }
            catch (FileNotFoundException) { return new PrototypeProgress(); }
            catch (DirectoryNotFoundException) { return new PrototypeProgress(); }
            catch (Exception e) when (e is IOException || e is InvalidDataException || e is ArgumentException || e is UnauthorizedAccessException)
            { ReadProtected = true; Error = "저장을 읽지 못했습니다. 원본 보존: " + e.Message; return new PrototypeProgress(); }
        }
        public static bool WriteProgress()
        {
            // Load first: Read may lock a corrupt/future file. Never evaluate the lock before lazy loading.
            var data = Progress; HasPendingProgress = true;
            if (ReadProtected) return false;
            var success = TryWriteJson("progress.json", JsonUtility.ToJson(data, true), out var error);
            Error = error; HasPendingProgress = !success; return success;
        }
        internal static bool TryWriteJson(string fileName, string json, out string error)
        {
            error = "";
            if (fileName != "progress.json" && fileName != "settings.json")
            { error = "지원하지 않는 저장 파일입니다."; return false; }
            try
            {
                Directory.CreateDirectory(Root);
                var path = Path.Combine(Root, fileName);
                var temp = path + ".tmp";
                File.WriteAllText(temp, json);
                if (File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path);
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            { error = "저장 실패: " + e.Message; return false; }
        }
        public static void CompleteChapter()
        {
            if (PrototypeSession.Practice || PrototypeSession.Stage != 6 || PrototypeSession.Chapter < 1) return;
            ApplyCompletion(PrototypeChapterCompletion.FromSession());
        }
        /// <summary>Idempotent local projection. True means accepted; Error/HasPendingProgress report disk failures separately.</summary>
        public static bool ApplyCompletion(PrototypeChapterCompletion completion)
        {
            if (completion == null || !completion.IsValid()) return false;
            var data = Progress;
            var chapter = completion.chapter;
            var before = JsonUtility.ToJson(data);
            var existing = data.records.Find(r => r.runId == completion.runId);
            if (existing != null && (existing.chapter != chapter || existing.score != completion.score
                || existing.seconds != completion.seconds || existing.participants != completion.participants
                || existing.scoreRuleVersion != completion.scoreRuleVersion || existing.gameBuildVersion != completion.gameBuildVersion)) return false;
            data.completedMask |= 1 << (chapter - 1);
            data.unlockedChapter = Mathf.Max(data.unlockedChapter, Mathf.Min(7, chapter + 1));
            // Placement is a trial only; the five final story locations are not decided.
            if (chapter <= 5) data.fragmentMask |= 1 << (chapter - 1);
            if (!data.achievements.Contains("first-chapter")) data.achievements.Add("first-chapter");
            if (data.completedMask == 127 && !data.achievements.Contains("seven-prototype-chapters")) data.achievements.Add("seven-prototype-chapters");
            if (completion.participants == 4 && existing == null)
            {
                data.records.Add(new PrototypeTeamRecord { runId = completion.runId, chapter = chapter,
                    score = completion.score, seconds = completion.seconds, participants = 4,
                    scoreRuleVersion = completion.scoreRuleVersion, gameBuildVersion = completion.gameBuildVersion });
                data.records.Sort((a, b) => a.score == b.score ? a.seconds.CompareTo(b.seconds) : b.score.CompareTo(a.score));
                if (data.records.Count > 50) data.records.RemoveRange(50, data.records.Count - 50);
            }
            if (before != JsonUtility.ToJson(data)) WriteProgress();
            return true;
        }
        public static void Remember(string[] memories)
        {
            if (memories == null || memories.Length == 0) return;
            var changed = false;
            foreach (var memory in memories)
                if (!string.IsNullOrWhiteSpace(memory) && memory.Length <= 1024 && !Progress.memories.Contains(memory))
                { Progress.memories.Add(memory); changed = true; }
            if (changed) WriteProgress();
        }
        public static List<PrototypeTeamRecord> Ranking(int chapter, string rulesVersion)
        {
            var records = Progress.records.FindAll(r => r != null && r.chapter == chapter && r.participants == 4 &&
                r.scoreRuleVersion == rulesVersion && r.score >= 0 && float.IsFinite(r.seconds) && r.seconds >= 0);
            records.Sort((a,b) => a.score == b.score ? a.seconds.CompareTo(b.seconds) : b.score.CompareTo(a.score));
            return records;
        }
    }
}
