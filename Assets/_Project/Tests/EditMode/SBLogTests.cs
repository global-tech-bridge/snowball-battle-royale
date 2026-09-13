using NUnit.Framework;
using SnowballBattleRoyale.Domain;
using SnowballBattleRoyale.Tests.EditMode.TestDoubles;
using UnityEngine;

namespace SnowballBattleRoyale.Tests.EditMode
{
    public sealed class SBLogTests
    {
        private LogSinkScope _logScope;
        private RecordingLogSink _sink;

        [SetUp]
        public void SetUp()
        {
            _logScope = new LogSinkScope();
            _sink = _logScope.Sink;
        }

        [TearDown]
        public void TearDown()
        {
            _logScope.Dispose();
        }

        [Test]
        public void Info_WritesInfoLevelToSink()
        {
            SBLog.Info("hello");

            Assert.That(_sink.Entries, Has.Count.EqualTo(1));
            Assert.That(_sink.Entries[0].Level, Is.EqualTo(SBLogLevel.Info));
        }

        [Test]
        public void Warn_WritesWarningLevelToSink()
        {
            SBLog.Warn("careful");

            Assert.That(_sink.Entries, Has.Count.EqualTo(1));
            Assert.That(_sink.Entries[0].Level, Is.EqualTo(SBLogLevel.Warning));
        }

        [Test]
        public void Error_WritesErrorLevelToSink()
        {
            SBLog.Error("broken");

            Assert.That(_sink.Entries, Has.Count.EqualTo(1));
            Assert.That(_sink.Entries[0].Level, Is.EqualTo(SBLogLevel.Error));
        }

        [Test]
        public void Message_IsPrefixedWithProjectTag()
        {
            SBLog.Info("hello");

            Assert.That(_sink.Entries[0].Message, Does.StartWith(SBLog.Prefix));
        }

        [Test]
        public void Message_IncludesCategoryWhenProvided()
        {
            SBLog.Info("moved", category: "Player");

            Assert.That(_sink.Entries[0].Message, Does.Contain("[Player]"));
            Assert.That(_sink.Entries[0].Message, Does.Contain("moved"));
        }

        [Test]
        public void Message_OmitsCategoryBracketsWhenCategoryIsEmpty()
        {
            SBLog.Info("plain", category: "");

            Assert.That(_sink.Entries[0].Message, Is.EqualTo($"{SBLog.Prefix} plain"));
        }

        [Test]
        public void Context_IsForwardedToSink()
        {
            var context = new GameObject("ctx");
            try
            {
                SBLog.Warn("with context", context);

                Assert.That(_sink.Entries[0].Context, Is.SameAs(context));
            }
            finally
            {
                Object.DestroyImmediate(context);
            }
        }

        [Test]
        public void Sink_FallsBackToUnityConsoleWhenSetToNull()
        {
            SBLog.Sink = null;

            Assert.That(SBLog.Sink, Is.InstanceOf<UnityConsoleLogSink>());
        }
    }
}
