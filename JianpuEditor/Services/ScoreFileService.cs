using System;
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
                var settings = new JsonSerializerSettings
                {
                    // Measures 默认列表若已存在条目，Auto 会追加而非替换，导致读入后多出一节空小节。
                    ObjectCreationHandling = ObjectCreationHandling.Replace
                };
                var score = JsonConvert.DeserializeObject<JianpuScore>(json, settings) ?? CreateEmptyScore();
                ImportLegacyChordMarkers(score, token["Measures"] as JArray);
                ChordMarkerService.NormalizeScore(score);
                return score;
            }

            var legacyScore = MigrateLegacyScore(token);
            ChordMarkerService.NormalizeScore(legacyScore);
            return legacyScore;
        }

        private static void ImportLegacyChordMarkers(JianpuScore score, JArray measuresToken)
        {
            if (score?.Measures == null || measuresToken == null)
            {
                return;
            }

            var count = Math.Min(measuresToken.Count, score.Measures.Count);
            for (var i = 0; i < count; i++)
            {
                var legacyText = measuresToken[i].Value<string>("SecondaryText");
                if (!string.IsNullOrWhiteSpace(legacyText))
                {
                    ChordMarkerService.ImportLegacyChordText(score.Measures[i], legacyText);
                }
            }
        }

        private static JianpuScore MigrateLegacyScore(JToken token)
        {
            var score = new JianpuScore
            {
                Title = token.Value<string>("Title") ?? "未命名乐曲",
                KeySignature = token.Value<string>("KeySignature") ?? "1=C",
                Tempo = token.Value<string>("Tempo") ?? "中速",
                Bpm = token.Value<int?>("Bpm") ?? 120,
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