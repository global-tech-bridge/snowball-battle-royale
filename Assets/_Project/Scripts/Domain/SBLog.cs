using System.Diagnostics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SnowballBattleRoyale.Domain
{
    /// <summary>Severity of an <see cref="SBLog"/> entry.</summary>
    public enum SBLogLevel
    {
        Info,
        Warning,
        Error,
    }

    /// <summary>Destination for log entries. Swapped out in tests to capture output.</summary>
    public interface ISBLogSink
    {
        void Write(SBLogLevel level, string message, Object context);
    }

    /// <summary>Default sink: forwards to the Unity console.</summary>
    public sealed class UnityConsoleLogSink : ISBLogSink
    {
        public void Write(SBLogLevel level, string message, Object context)
        {
            switch (level)
            {
                case SBLogLevel.Warning:
                    UnityEngine.Debug.LogWarning(message, context);
                    break;
                case SBLogLevel.Error:
                    UnityEngine.Debug.LogError(message, context);
                    break;
                default:
                    UnityEngine.Debug.Log(message, context);
                    break;
            }
        }
    }

    /// <summary>
    /// Project-wide logger.
    ///
    /// Every entry point carries <see cref="ConditionalAttribute"/>, so calls (and the evaluation of
    /// their arguments) are removed by the compiler in release player builds. Define <c>SB_LOG</c> in
    /// Player Settings &gt; Scripting Define Symbols to keep logging in a release build.
    /// </summary>
    public static class SBLog
    {
        /// <summary>Tag prefixed to every message, so project logs are greppable in the console.</summary>
        public const string Prefix = "[SB]";

        private static ISBLogSink _sink = new UnityConsoleLogSink();

        /// <summary>Active sink. Assigning <c>null</c> restores the Unity console sink.</summary>
        public static ISBLogSink Sink
        {
            get => _sink;
            set => _sink = value ?? new UnityConsoleLogSink();
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        [Conditional("SB_LOG")]
        public static void Info(string message, Object context = null, string category = null)
        {
            _sink.Write(SBLogLevel.Info, Format(message, category), context);
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        [Conditional("SB_LOG")]
        public static void Warn(string message, Object context = null, string category = null)
        {
            _sink.Write(SBLogLevel.Warning, Format(message, category), context);
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        [Conditional("SB_LOG")]
        public static void Error(string message, Object context = null, string category = null)
        {
            _sink.Write(SBLogLevel.Error, Format(message, category), context);
        }

        private static string Format(string message, string category)
        {
            return string.IsNullOrWhiteSpace(category)
                ? $"{Prefix} {message}"
                : $"{Prefix} [{category}] {message}";
        }
    }
}
