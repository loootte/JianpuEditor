using System.Collections.Generic;
using System.IO;
using JianpuEditor.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JianpuEditor.Services
{
    public static class ScoreFileService
    {
        public static void Save(JianpuScore score, string path)
        {
            var json = JsonConvert.SerializeObject(score, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        public static JianpuScore Load(string path)
        {
            var json = File.ReadAllText(path);
            var token = JToken.Parse(json);

            if (token["Measures"] != null)
            {
                return token.ToObject<JianpuScore>() ?? CreateEmptyScore();
            }

            return MigrateLegacyScore(token);
        }

        private static JianpuScore MigrateLegacyScore(JToken token)
        {
            var score = new JianpuScore
            {
                Title = token.Value<string>("Title") ?? "未命名乐曲",
                KeySignature = token.Value<string>("KeySignature") ?? "1=C",
                Tempo = token.Value<string>("Tempo") ?? "中速",
                Composer = token.Value<string>("Composer") ?? string.Empty,
                Measures = new List<JianpuMeasure>()
            };

            var current = new JianpuMeasure();
            score.Measures.Add(current);

            foreach (var noteToken in token["Notes"] ?? new JArray())
            {
                var typeValue = noteToken.Value<int?>("Type") ?? 0;
                if (typeValue == 2)
                {
                    current = new JianpuMeasure();
                    score.Measures.Add(current);
                    continue;
                }

                current.MelodyNotes.Add(noteToken.ToObject<JianpuNote>());
            }

            if (score.Measures.Count == 0)
            {
                score.Measures.Add(new JianpuMeasure());
            }

            return score;
        }

        private static JianpuScore CreateEmptyScore()
        {
            return new JianpuScore();
        }
    }
}