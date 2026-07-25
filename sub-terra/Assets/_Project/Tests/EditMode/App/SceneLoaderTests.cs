using NUnit.Framework;
using SubTerra.App.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace SubTerra.App.Tests
{
    /// <summary>
    /// UnitySceneLoader의 실패 폐쇄 경로.
    /// 실제 Build Settings에 없는 이름은 false를 반환해야 한다.
    /// </summary>
    public sealed class SceneLoaderTests
    {
        [Test]
        public void Load_EmptyName_ReturnsFalse()
        {
            var loader = new UnitySceneLoader();
            LogAssert.Expect(LogType.Error, "[SubTerra] Scene load failed: empty scene name.");
            Assert.That(loader.Load(null), Is.False);
            LogAssert.Expect(LogType.Error, "[SubTerra] Scene load failed: empty scene name.");
            Assert.That(loader.Load(""), Is.False);
            LogAssert.Expect(LogType.Error, "[SubTerra] Scene load failed: empty scene name.");
            Assert.That(loader.Load("   "), Is.False);
        }

        [Test]
        public void Load_UnknownSceneName_ReturnsFalse()
        {
            var loader = new UnitySceneLoader();
            // Build Settings에 없는 이름은 예외 없이도 실패로 처리한다.
            LogAssert.Expect(
                LogType.Error,
                "[SubTerra] Scene load failed: scene is not in build settings or name is invalid.");
            Assert.That(loader.Load("DefinitelyNotARegisteredScene_SubTerra_PhaseA"), Is.False);
        }
    }
}
