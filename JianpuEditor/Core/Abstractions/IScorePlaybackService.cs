using System;
using JianpuEditor.Models;

namespace JianpuEditor.Core.Abstractions
{
    public interface IScorePlaybackService : IDisposable
    {
        bool IsPlaying { get; }

        double PositionQuarter { get; }

        double TotalQuarterLength { get; }

        event Action<double> PositionChanged;

        event Action PlaybackFinished;

        event Action<Exception> PlaybackError;

        void Prepare(JianpuScore score, double startQuarter = 0);

        void Play(JianpuScore score, int bpm, double startQuarter = 0);

        void StopPlayback();

        void Seek(double quarterBeat);
    }
}
