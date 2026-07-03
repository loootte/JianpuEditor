namespace JianpuEditor.Core.Messaging.Messages
{
    public sealed class ScoreEditedMessage
    {
        public ScoreEditedMessage(string message, bool stopPlayback = true, bool markDirty = true)
        {
            Message = message ?? string.Empty;
            StopPlayback = stopPlayback;
            MarkDirty = markDirty;
        }

        public string Message { get; }

        public bool StopPlayback { get; }

        public bool MarkDirty { get; }
    }
}
