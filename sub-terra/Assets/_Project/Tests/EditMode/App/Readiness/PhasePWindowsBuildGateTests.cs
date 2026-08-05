using NUnit.Framework;
using SubTerra.App.RuntimeInfo;
using SubTerra.App.Editor.DataValidation;
using UnityEditor;

namespace SubTerra.App.Tests.Readiness
{
    public sealed class PhasePWindowsBuildGateTests
    {
        [Test]
        public void P_S01_ProfilesHaveDistinctDefinesAndExpectedDebugOptions()
        {
            var development = PhasePWindowsBuildGate.GetProfile(PhasePWindowsBuildGate.Profile.Development);
            var qa = PhasePWindowsBuildGate.GetProfile(PhasePWindowsBuildGate.Profile.Qa);
            var release = PhasePWindowsBuildGate.GetProfile(PhasePWindowsBuildGate.Profile.Release);

            Assert.That(development.Define, Is.EqualTo("SUBTERRA_BUILD_DEVELOPMENT"));
            Assert.That(qa.Define, Is.EqualTo("SUBTERRA_BUILD_QA"));
            Assert.That(release.Define, Is.EqualTo("SUBTERRA_BUILD_RELEASE"));
            Assert.That((development.Options & BuildOptions.Development) != 0, Is.True);
            Assert.That((qa.Options & BuildOptions.Development) != 0, Is.True);
            Assert.That((release.Options & BuildOptions.Development) == 0, Is.True);
            Assert.That((release.Options & BuildOptions.AllowDebugging) == 0, Is.True);
        }

        [Test]
        public void P_S02_WindowsSceneListExcludesSampleSceneAndPassesReleaseGate()
        {
            Assert.That(PhasePWindowsBuildGate.WindowsScenes, Does.Not.Contain("Assets/Scenes/SampleScene.unity"));
            Assert.That(PhasePWindowsBuildGate.WindowsScenes.Count, Is.EqualTo(4));
            Assert.That(
                PhasePWindowsBuildGate.Validate(PhasePWindowsBuildGate.Profile.Release),
                Is.Empty);
        }

        [Test]
        public void P_S04_BuildVersionLabelIncludesChannelAndSaveVersion()
        {
            var label = BuildVersionInfo.Format("1.0.0");
            Assert.That(label, Does.Contain("Game 1.0.0"));
            Assert.That(label, Does.Contain("Build Editor"));
            Assert.That(label, Does.Contain("Save v2"));
        }
    }
}
