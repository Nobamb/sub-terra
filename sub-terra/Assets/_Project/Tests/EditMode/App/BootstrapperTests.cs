using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.State;
using UnityEngine;
using UnityEngine.TestTools;

namespace SubTerra.App.Tests
{
    public sealed class BootstrapperTests
    {
        private sealed class Catalog : IDataCatalogPort
        {
            private readonly bool valid;
            private readonly bool throwOnValidate;

            public Catalog(bool valid, bool throwOnValidate = false)
            {
                this.valid = valid;
                this.throwOnValidate = throwOnValidate;
            }

            public bool Validate(out string reason)
            {
                if (throwOnValidate)
                {
                    throw new System.InvalidOperationException("catalog boom");
                }

                reason = valid ? null : "duplicate id";
                return valid;
            }
        }

        private sealed class Save : ISavePort
        {
            public bool HasSave { get; set; }
            public GameState Loaded { get; set; }
            public bool ThrowOnLoad { get; set; }

            public GameState Load()
            {
                if (ThrowOnLoad)
                {
                    throw new System.InvalidOperationException("save boom");
                }

                return Loaded;
            }
        }

        private sealed class Scenes : ISceneLoader
        {
            public string LoadedName;
            public bool ShouldLoad = true;
            public int LoadCallCount;

            public bool Load(string sceneName)
            {
                LoadCallCount++;
                LoadedName = sceneName;
                return ShouldLoad;
            }
        }

        [SetUp]
        public void SetUp()
        {
            CleanupBootstrappers();
        }

        [TearDown]
        public void TearDown()
        {
            CleanupBootstrappers();
        }

        private static void CleanupBootstrappers()
        {
            GameBootstrapper.ResetInstanceForTests();

            var existing = Object.FindObjectsByType<GameBootstrapper>(FindObjectsInactive.Include);
            for (var i = 0; i < existing.Length; i++)
            {
                if (existing[i] != null)
                {
                    Object.DestroyImmediate(existing[i].gameObject);
                }
            }

            GameBootstrapper.ResetInstanceForTests();
        }

        private static GameBootstrapper CreateBootstrapper()
        {
            return new GameObject("BootstrapperTest").AddComponent<GameBootstrapper>();
        }

        private static void DestroyBootstrapper(GameBootstrapper bootstrapper)
        {
            if (bootstrapper != null)
            {
                Object.DestroyImmediate(bootstrapper.gameObject);
            }
        }

        [Test]
        public void Initialize_CreatesNewStateBeforeMainMenu()
        {
            var scenes = new Scenes();
            var bootstrapper = CreateBootstrapper();
            Assert.That(bootstrapper.Initialize(new Catalog(true), new Save(), scenes), Is.True);
            Assert.That(bootstrapper.State, Is.Not.Null);
            Assert.That(GameState.IsComplete(bootstrapper.State), Is.True);
            Assert.That(scenes.LoadedName, Is.EqualTo(SceneNames.MainMenu));
            Assert.That(bootstrapper.IsInitialized, Is.True);
            Assert.That(bootstrapper.InitializationFailed, Is.False);
        }

        [Test]
        public void Initialize_StopsOnCatalogFailure()
        {
            var scenes = new Scenes();
            var bootstrapper = CreateBootstrapper();
            LogAssert.Expect(LogType.Error, "[SubTerra] Catalog validation failed: duplicate id");
            Assert.That(bootstrapper.Initialize(new Catalog(false), new Save(), scenes), Is.False);
            Assert.That(bootstrapper.InitializationFailed, Is.True);
            Assert.That(scenes.LoadedName, Is.Null);
            Assert.That(scenes.LoadCallCount, Is.Zero);
        }

        [Test]
        public void Initialize_StopsWhenMainMenuLoadFails()
        {
            var scenes = new Scenes { ShouldLoad = false };
            var bootstrapper = CreateBootstrapper();
            LogAssert.Expect(LogType.Error, "[SubTerra] Scene transition failed: MainMenu.");
            Assert.That(bootstrapper.Initialize(new Catalog(true), new Save(), scenes), Is.False);
            Assert.That(bootstrapper.InitializationFailed, Is.True);
            Assert.That(bootstrapper.State, Is.Not.Null);
            Assert.That(bootstrapper.IsInitialized, Is.False);
        }

        [Test]
        public void Initialize_StopsWhenCatalogPortIsNull()
        {
            var bootstrapper = CreateBootstrapper();
            LogAssert.Expect(LogType.Error, "[SubTerra] Bootstrap ports missing.");
            Assert.That(bootstrapper.Initialize(null, new Save(), new Scenes()), Is.False);
            Assert.That(bootstrapper.InitializationFailed, Is.True);
        }

        [Test]
        public void Initialize_StopsWhenSavePortIsNull()
        {
            var bootstrapper = CreateBootstrapper();
            LogAssert.Expect(LogType.Error, "[SubTerra] Bootstrap ports missing.");
            Assert.That(bootstrapper.Initialize(new Catalog(true), null, new Scenes()), Is.False);
            Assert.That(bootstrapper.InitializationFailed, Is.True);
        }

        [Test]
        public void Initialize_StopsWhenScenePortIsNull()
        {
            var bootstrapper = CreateBootstrapper();
            LogAssert.Expect(LogType.Error, "[SubTerra] Bootstrap ports missing.");
            Assert.That(bootstrapper.Initialize(new Catalog(true), new Save(), null), Is.False);
            Assert.That(bootstrapper.InitializationFailed, Is.True);
        }

        [Test]
        public void Initialize_StopsWhenSaveReturnsNullState()
        {
            var scenes = new Scenes();
            var save = new Save { HasSave = true, Loaded = null };
            var bootstrapper = CreateBootstrapper();
            LogAssert.Expect(LogType.Error, "[SubTerra] State initialization failed: incomplete or missing state.");
            Assert.That(bootstrapper.Initialize(new Catalog(true), save, scenes), Is.False);
            Assert.That(bootstrapper.InitializationFailed, Is.True);
            Assert.That(scenes.LoadCallCount, Is.Zero);
        }

        [Test]
        public void Initialize_StopsWhenSaveReturnsIncompleteState()
        {
            // 소유 하위 State가 빠진 불완전 객체(직렬화 손상 등)는 MainMenu 전환을 막아야 한다.
            var incomplete = GameState.CreateNew();
            typeof(GameState).GetProperty(nameof(GameState.Player))!
                .SetValue(incomplete, null);
            Assert.That(GameState.IsComplete(incomplete), Is.False);

            var scenes = new Scenes();
            var save = new Save { HasSave = true, Loaded = incomplete };
            var bootstrapper = CreateBootstrapper();
            LogAssert.Expect(LogType.Error, "[SubTerra] State initialization failed: incomplete or missing state.");
            Assert.That(bootstrapper.Initialize(new Catalog(true), save, scenes), Is.False);
            Assert.That(bootstrapper.InitializationFailed, Is.True);
            Assert.That(scenes.LoadedName, Is.Null);
            Assert.That(scenes.LoadCallCount, Is.Zero);
        }

        [Test]
        public void Initialize_UsesRestoredCompleteState()
        {
            var restored = GameState.FromParts(
                new PlayerState(50, 10, 1f, 0.5f),
                new ProgressState(2),
                new RunState(3, false));
            Assert.That(GameState.IsComplete(restored), Is.True);

            var scenes = new Scenes();
            var save = new Save { HasSave = true, Loaded = restored };
            var bootstrapper = CreateBootstrapper();
            Assert.That(bootstrapper.Initialize(new Catalog(true), save, scenes), Is.True);
            Assert.That(bootstrapper.State, Is.SameAs(restored));
            Assert.That(bootstrapper.State.Player.Gold, Is.EqualTo(10));
            Assert.That(scenes.LoadedName, Is.EqualTo(SceneNames.MainMenu));
        }

        [Test]
        public void Initialize_DoesNotOverwriteStateAfterSuccess()
        {
            var scenes = new Scenes();
            var bootstrapper = CreateBootstrapper();
            Assert.That(bootstrapper.Initialize(new Catalog(true), new Save(), scenes), Is.True);
            var firstState = bootstrapper.State;
            firstState.Player.AddGold(5);

            var scenes2 = new Scenes();
            Assert.That(bootstrapper.Initialize(new Catalog(true), new Save(), scenes2), Is.True);
            Assert.That(bootstrapper.State, Is.SameAs(firstState));
            Assert.That(bootstrapper.State.Player.Gold, Is.EqualTo(5));
            // 재초기화 시 씬 전환을 다시 요청하지 않는다.
            Assert.That(scenes2.LoadCallCount, Is.Zero);
        }

        [Test]
        public void Initialize_StopsWhenCatalogThrows()
        {
            var scenes = new Scenes();
            var bootstrapper = CreateBootstrapper();
            LogAssert.Expect(LogType.Error, "[SubTerra] Initialization exception: InvalidOperationException");
            Assert.That(bootstrapper.Initialize(new Catalog(true, throwOnValidate: true), new Save(), scenes), Is.False);
            Assert.That(bootstrapper.InitializationFailed, Is.True);
            Assert.That(scenes.LoadCallCount, Is.Zero);
        }

        [Test]
        public void Initialize_StopsWhenSaveLoadThrows()
        {
            var scenes = new Scenes();
            var save = new Save { HasSave = true, ThrowOnLoad = true };
            var bootstrapper = CreateBootstrapper();
            LogAssert.Expect(LogType.Error, "[SubTerra] Initialization exception: InvalidOperationException");
            Assert.That(bootstrapper.Initialize(new Catalog(true), save, scenes), Is.False);
            Assert.That(bootstrapper.InitializationFailed, Is.True);
            Assert.That(scenes.LoadCallCount, Is.Zero);
        }

        [Test]
        public void DuplicateRoot_DoesNotReinitializeOrOverwriteExistingState()
        {
            // A-F02: 전역 Instance가 이미 있는 상태에서 새 Bootstrap Root가 생겨도
            // 기존 State를 유지하고, 폐기 대상 Root의 Initialize/씬 로드는 무시한다.
            var scenes1 = new Scenes();
            var first = CreateBootstrapper();
            Assert.That(first.Initialize(new Catalog(true), new Save(), scenes1), Is.True);
            var ownedState = first.State;
            ownedState.Player.AddGold(3);
            Assert.That(GameBootstrapper.Instance == first, Is.True);

            var scenes2 = new Scenes();
            var second = CreateBootstrapper();

            // 이미 전역 Root가 있으면 두 번째 객체의 Initialize/씬 로드는 no-op 이어야 한다.
            Assert.That(second.Initialize(new Catalog(true), new Save(), scenes2), Is.False);
            Assert.That(scenes2.LoadCallCount, Is.Zero);
            Assert.That(GameBootstrapper.Instance == first, Is.True);
            Assert.That(first.State, Is.SameAs(ownedState));
            Assert.That(first.State.Player.Gold, Is.EqualTo(3));
            Assert.That(first.IsInitialized, Is.True);
            Assert.That(first.InitializationFailed, Is.False);
        }
    }
}
