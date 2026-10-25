using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SnowballBattleRoyale.Core;
using SnowballBattleRoyale.Domain;
using SnowballBattleRoyale.Tests.EditMode.TestDoubles;

namespace SnowballBattleRoyale.Tests.EditMode
{
    public sealed class BootSequenceTests
    {
        private sealed class FakeSceneLoader : ISceneLoader
        {
            public readonly List<string> LoadedAdditively = new();
            public readonly HashSet<string> AlreadyLoaded = new();
            public readonly HashSet<string> Existing = new() { "01_Persistent", "02_Gameplay" };

            public bool IsSceneLoaded(string sceneName) => AlreadyLoaded.Contains(sceneName);

            public bool SceneExistsInBuild(string sceneName) => Existing.Contains(sceneName);

            public void LoadAdditive(string sceneName)
            {
                LoadedAdditively.Add(sceneName);
                AlreadyLoaded.Add(sceneName);
            }
        }

        private FakeSceneLoader _loader;
        private LogSinkScope _logScope;

        [SetUp]
        public void SetUp()
        {
            _loader = new FakeSceneLoader();

            // BootSequence logs an error for scenes missing from Build Settings. Capturing it keeps
            // Unity's test runner from failing those tests on an "unhandled log message".
            _logScope = new LogSinkScope();
        }

        [TearDown]
        public void TearDown() => _logScope.Dispose();

        [Test]
        public void Run_LoadsEachConfiguredSceneAdditively()
        {
            var sequence = new BootSequence(_loader, new[] { "01_Persistent", "02_Gameplay" });

            BootResult result = sequence.Run();

            Assert.That(result.LoadedScenes, Is.EqualTo(new[] { "01_Persistent", "02_Gameplay" }));
            Assert.That(_loader.LoadedAdditively, Is.EqualTo(new[] { "01_Persistent", "02_Gameplay" }));
        }

        [Test]
        public void Run_SkipsScenesThatAreAlreadyLoaded()
        {
            _loader.AlreadyLoaded.Add("01_Persistent");
            var sequence = new BootSequence(_loader, new[] { "01_Persistent", "02_Gameplay" });

            BootResult result = sequence.Run();

            Assert.That(_loader.LoadedAdditively, Is.EqualTo(new[] { "02_Gameplay" }));
            Assert.That(result.SkippedScenes, Contains.Item("01_Persistent"));
        }

        [Test]
        public void Run_ReportsScenesMissingFromBuildSettingsWithoutLoadingThem()
        {
            var sequence = new BootSequence(_loader, new[] { "01_Persistent", "99_DoesNotExist" });

            BootResult result = sequence.Run();

            Assert.That(result.MissingScenes, Is.EqualTo(new[] { "99_DoesNotExist" }));
            Assert.That(_loader.LoadedAdditively, Does.Not.Contain("99_DoesNotExist"));
        }

        [Test]
        public void Run_ContinuesLoadingRemainingScenesAfterAMissingOne()
        {
            var sequence = new BootSequence(_loader, new[] { "99_DoesNotExist", "01_Persistent" });

            sequence.Run();

            Assert.That(_loader.LoadedAdditively, Is.EqualTo(new[] { "01_Persistent" }));
        }

        [Test]
        public void Run_IsIdempotentWhenCalledTwice()
        {
            var sequence = new BootSequence(_loader, new[] { "01_Persistent" });

            sequence.Run();
            BootResult second = sequence.Run();

            Assert.That(_loader.LoadedAdditively, Has.Count.EqualTo(1));
            Assert.That(second.LoadedScenes, Is.Empty);
        }

        [Test]
        public void Run_IgnoresNullAndWhitespaceSceneNames()
        {
            var sequence = new BootSequence(_loader, new[] { "01_Persistent", null, "   " });

            BootResult result = sequence.Run();

            Assert.That(result.LoadedScenes, Is.EqualTo(new[] { "01_Persistent" }));
            Assert.That(result.MissingScenes, Is.Empty);
        }

        [Test]
        public void Run_DeduplicatesRepeatedSceneNames()
        {
            var sequence = new BootSequence(_loader, new[] { "01_Persistent", "01_Persistent" });

            sequence.Run();

            Assert.That(_loader.LoadedAdditively.Count(s => s == "01_Persistent"), Is.EqualTo(1));
        }

        [Test]
        public void Constructor_ThrowsWhenLoaderIsNull()
        {
            Assert.That(
                () => new BootSequence(null, new[] { "01_Persistent" }),
                Throws.ArgumentNullException);
        }

        [Test]
        public void Run_WithNoScenesConfigured_SucceedsWithEmptyResult()
        {
            var sequence = new BootSequence(_loader, null);

            BootResult result = sequence.Run();

            Assert.That(result.LoadedScenes, Is.Empty);
            Assert.That(result.MissingScenes, Is.Empty);
            Assert.That(result.HasErrors, Is.False);
        }

        [Test]
        public void HasErrors_IsTrueWhenASceneIsMissing()
        {
            var sequence = new BootSequence(_loader, new[] { "99_DoesNotExist" });

            Assert.That(sequence.Run().HasErrors, Is.True);
        }

        [Test]
        public void Run_LogsAnErrorForEachMissingScene()
        {
            var sequence = new BootSequence(_loader, new[] { "99_DoesNotExist" });

            sequence.Run();

            Assert.That(
                _logScope.Sink.Entries,
                Has.Exactly(1).Matches<RecordingLogSink.Entry>(
                    e => e.Level == SBLogLevel.Error && e.Message.Contains("99_DoesNotExist")));
        }
    }
}
