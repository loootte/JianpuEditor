namespace JianpuEditor.Core.Messaging.Messages
{
    public sealed class StatusChangedMessage
    {
        public StatusChangedMessage(string message)
        {
            Message = message ?? string.Empty;
        }

        public string Message { get; }
    }
}
