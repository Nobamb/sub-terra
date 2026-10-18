using System.Collections;
using System.IO;
using NUnit.Framework;
using SubTerra.App.Tutorial;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.Tutorial;
using UnityEngine;
using UnityEngine.TestTools;

namespace SubTerra.App.Tests.PlayMode
{
    public sealed class DesktopVisualPolishCaptureTests
    {
        private UiTestEnvironment environment;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (environment != null) environment.Dispose();
            yield return null;
        }

        [UnityTest, Category("Visual")]
        public IEnumerator HudBuildingAndReward_AtTwoResolutions()
        {
            environment = new UiTestEnvironment();
            yield return environment.LoadIntegration(false);
            var menu = Object.FindAnyObjectByType<GameplaySideMenuController>();
            var chrome = Object.FindAnyObjectByType<HudPanelChromeController>();
            var objective = Object.FindAnyObjectByType<DemoObjectiveView>();
            Assert.That(menu.IsOpen, Is.False);
            Assert.That(chrome.IsBuildingMenuOpen, Is.False);
            var output = Path.GetFullPath("Builds/VisualPolish-20261014/screenshots");
            foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(2560, 1440) })
            {
                yield return environment.Resolution.Set(size.x, size.y);
                var title = objective.GetComponentsInChildren<TMPro.TMP_Text>(true);
                foreach (var text in title)
                    if (text.name == "ObjectiveTitle")
                    {
                        text.ForceMeshUpdate();
                        Assert.That(text.textInfo.characterInfo, Has.Some.Matches<TMPro.TMP_CharacterInfo>(c => c.isVisible),
                            "현재 목표 제목이 압축된 미션 카드에서 보여야 한다.");
                    }
                yield return UiTestWait.Capture(Path.Combine(output, "hud-" + size.x + ".png"));
                chrome.ToggleBuildingMenu();
                menu.OpenIfClosed();
                yield return UiTestWait.Until(() => !menu.IsTransitioning, "menu opened for layout capture", stage: "animation");
                yield return UiTestWait.Capture(Path.Combine(output, "building-menu-" + size.x + ".png"));
                chrome.CloseBuildingMenu();
                menu.CloseIfOpen();
                yield return UiTestWait.Until(() => !menu.IsTransitioning, "menu closed for reward capture", stage: "animation");
                objective.SetClaimText("퀘스트 클리어", "블록 제거", "", "닫으면 보상이 지급됩니다.");
                objective.SetClaimRewards(new QuestReward(copper: 3, iron: 0, lithium: 0, gold: 50));
                objective.SetClaimVisible(true);
                yield return new WaitForSecondsRealtime(1.2f);
                yield return UiTestWait.Capture(Path.Combine(output, "reward-" + size.x + ".png"));
                objective.SetClaimVisible(false);
                yield return new WaitForSecondsRealtime(0.8f);
            }
        }
    }
}
