using JianpuEditor.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JianpuEditor.Services
{
    public static class ScoreCloneService
    {
        public static JianpuScore Clone(JianpuScore score)
        {
            if (score == null)
            {
                return new JianpuScore();
            }

            var json = JsonConvert.SerializeObject(score);
            var settings = new JsonSerializerSettings
            {
                ObjectCreationHandling = ObjectCreationHandling.Replace
            };
            var clone = JsonConvert.DeserializeObject<JianpuScore>(json, settings) ?? new JianpuScore();
            ChordMarkerService.NormalizeScore(clone);
            return clone;
        }
    }
}