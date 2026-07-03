using JianpuEditor.Core.Abstractions;
using JianpuEditor.Models;

namespace JianpuEditor.Services
{
    public sealed class ScoreFileServiceAdapter : IScoreFileService
    {
        public JianpuScore Load(string path)
        {
            return ScoreFileService.Load(path);
        }

        public void Save(JianpuScore score, string path)
        {
            ScoreFileService.Save(score, path);
        }
    }
}
