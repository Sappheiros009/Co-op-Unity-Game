namespace SlimeCoop.Prototype
{
    /// <summary>Projects validated server data into this client's local save. Does not authenticate Steam/cloud records.</summary>
    public sealed class PrototypeNetworkProgress
    {
        private PrototypeChapterCompletion _completed;
        public int AppliedCompletions { get; private set; }
        public string CompletedRunId => _completed?.runId ?? "";

        // Called only after the server transport and replica have accepted the snapshot and fixed actor binding.
        public bool Apply(PrototypeNetworkWorldState state)
        {
            if (state == null || !state.IsValid() || state.yourActor < 0 || !state.actors[state.yourActor].connected) return false;
            if (state.chapterCompleted)
            {
                if (_completed?.runId == state.runId)
                { if (!_completed.SameResult(state.completion)) return false; }
                else
                {
                    if (!PrototypeSave.ApplyCompletion(state.completion)) return false;
                    _completed = state.completion.Copy(); AppliedCompletions++;
                }
            }
            // Discoveries are permanent even when a later stage wipes, just as in local play.
            PrototypeSave.Remember(state.journal);
            return true;
        }
    }
}
