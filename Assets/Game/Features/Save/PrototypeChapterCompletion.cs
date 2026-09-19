using System;

namespace SlimeCoop.Prototype
{
    /// <summary>Settled chapter data, independent of a client's display/session globals. Local prototype, not a cloud receipt.</summary>
    [Serializable] public sealed class PrototypeChapterCompletion
    {
        public string runId, scoreRuleVersion, gameBuildVersion;
        public int chapter, score, participants;
        public float seconds;

        public bool IsValid() => ValidToken(runId) && ValidToken(scoreRuleVersion) && ValidToken(gameBuildVersion)
            && chapter >= 1 && chapter <= 7 && participants >= 2 && participants <= 4 && score >= 0
            && float.IsFinite(seconds) && seconds >= 0;

        private static bool ValidToken(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length > 64) return false;
            foreach (var c in text) if (char.IsControl(c) || c == '<' || c == '>') return false;
            return true;
        }
        public bool SameResult(PrototypeChapterCompletion other) => other != null && runId == other.runId
            && scoreRuleVersion == other.scoreRuleVersion && gameBuildVersion == other.gameBuildVersion
            && chapter == other.chapter && score == other.score && participants == other.participants && seconds == other.seconds;

        public PrototypeChapterCompletion Copy() => (PrototypeChapterCompletion)MemberwiseClone();

        public static PrototypeChapterCompletion FromSession() => new PrototypeChapterCompletion
        {
            runId = PrototypeSession.RunId, chapter = PrototypeSession.Chapter, score = PrototypeSession.RunScore,
            seconds = PrototypeSession.RunSeconds, participants = PrototypeSession.StartingCount,
            scoreRuleVersion = PrototypeTuning.Current.rulesVersion, gameBuildVersion = "prototype-v2"
        };
    }
}
