using System.Collections.Generic;
using SnowballBattleRoyale.Domain;
using UnityEngine;

namespace SnowballBattleRoyale.Tests.EditMode.TestDoubles
{
    /// <summary>
    /// Captures <see cref="SBLog"/> output instead of forwarding it to the Unity console.
    ///
    /// Besides making log output assertable, installing this keeps <c>SBLog.Error</c> calls from
    /// reaching the console — Unity's test runner fails any test that logs an error it was not told
    /// to expect.
    /// </summary>
    public sealed class RecordingLogSink : ISBLogSink
    {
        public readonly List<Entry> Entries = new();

        public readonly struct Entry
        {
            public Entry(SBLogLevel level, string message, Object context)
            {
                Level = level;
                Message = message;
                Context = context;
            }

            public SBLogLevel Level { get; }

            public string Message { get; }

            public Object Context { get; }
        }

        public void Write(SBLogLevel level, string message, Object context)
        {
            Entries.Add(new Entry(level, message, context));
        }
    }

    /// <summary>Installs a <see cref="RecordingLogSink"/> for the lifetime of a test.</summary>
    public sealed class LogSinkScope : System.IDisposable
    {
        private readonly ISBLogSink _previous;

        public LogSinkScope()
        {
            _previous = SBLog.Sink;
            Sink = new RecordingLogSink();
            SBLog.Sink = Sink;
        }

        public RecordingLogSink Sink { get; }

        public void Dispose() => SBLog.Sink = _previous;
    }
}
