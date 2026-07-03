using JianpuEditor.Models;

namespace JianpuEditor.Core.Messaging.Messages
{
    public sealed class ScoreLoadedMessage
    {
        public ScoreLoadedMessage(JianpuScore score, string filePath)
        {
            Score = score;
            FilePath = filePath;
        }

        public JianpuScore Score { get; }

        public string FilePath { get; }
    }
}
