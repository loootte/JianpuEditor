using JianpuEditor.Models;

namespace JianpuEditor.Core.Abstractions
{
    public interface IScoreFileService
    {
        JianpuScore Load(string path);

        void Save(JianpuScore score, string path);
    }
}
