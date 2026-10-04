using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.State;
using SubTerra.App.UI.HUD;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace SubTerra.App.Tests.PlayMode
{
    /// <summary>
    /// prompt-B 131: 실제 Integration Scene의 좌측 상단 HUD 하단 행이 GameState 구조 상태에 따라
    /// 아이콘·문구·색·발광을 바꾸는지, 중복 표시가 없는지, 해상도별 배치가 유지되는지 확인한다.
    /// </summary>
    public sealed class PromptB131StructuralHudPlayModeTests
    {
        private static readonly StructuralRiskLevel[] Levels =
        {
            StructuralRiskLevel.Safe, StructuralRiskLevel.Caution,
            StructuralRiskLevel.Critical, StructuralRiskLevel.Imminent
        };

        private UiTestEnvironment environment;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (environment != null) environment.Dispose();
            yield return null;
        }

        [UnityTest]
        public IEnumerator StructuralRow_FollowsGameState_InIntegrationScene()
        {
            yield return Verify(false);
        }

        [UnityTest, Category("Visual")]
        public IEnumerator StructuralRow_StatesAndLayout_AtThreeResolutions()
        {
            yield return Verify(true);
        }

        private IEnumerator Verify(bool visual)
        {
            environment = new UiTestEnvironment();
            yield return environment.LoadIntegration();
            var state = GameBootstrapper.Instance.State;

            var views = Object.FindObjectsByType<StructuralHudView>(FindObjectsInactive.Include);
            Assert.That(views.Length, Is.EqualTo(1), "구조 상태 표시는 HUD 하단 행 하나뿐이어야 한다.");
            var view = views[0];
            var hud = view.GetComponentInParent<BasicHudView>();
            Assert.That(hud, Is.Not.Null, "구조 상태 행은 좌측 상단 HUD 안에 있어야 한다.");
            var names = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
                .Select(t => t.name).ToArray();
            Assert.That(names, Does.Not.Contain("StructuralHUD"));
            Assert.That(names, Does.Not.Contain("StructuralRiskText"));
            Assert.That(names, Does.Not.Contain("PowerConnectionText"));
            Assert.That(hud.transform.Find("HudIcon6"), Is.Null);

            var evidence = Path.GetFullPath("Temp/visual/prompt-b131");
            if (visual) Directory.CreateDirectory(evidence);

            foreach (var level in Levels)
            {
                state.SetStructuralRisk(level);
                yield return null;
                yield return null;
                AssertStateShown(view, level);
                AssertLayout(view, hud);
                if (visual) yield return UiTestWait.Capture(Path.Combine(evidence, $"hud-{level}-1920x1080.png"));
            }

            if (visual)
            {
                foreach (var resolution in new[] { new Vector2Int(2560, 1440), new Vector2Int(1366, 768) })
                {
                    yield return environment.Resolution.Set(resolution.x, resolution.y);
                    foreach (var level in new[] { StructuralRiskLevel.Safe, StructuralRiskLevel.Imminent })
                    {
                        state.SetStructuralRisk(level);
                        yield return null;
                        yield return null;
                        AssertStateShown(view, level);
                        AssertLayout(view, hud);
                        yield return UiTestWait.Capture(Path.Combine(evidence, $"hud-{level}-{resolution.x}x{resolution.y}.png"));
                    }
                }

                yield return environment.Resolution.Set(1920, 1080);
            }

            // 위험 상태에서 HUD를 껐다 켜도 표시가 복구되고 같은 상태 변경에 한 번만 반응한다.
            state.SetStructuralRisk(StructuralRiskLevel.Critical);
            yield return null;
            hud.gameObject.SetActive(false);
            yield return null;
            hud.gameObject.SetActive(true);
            yield return null;
            AssertStateShown(view, StructuralRiskLevel.Critical);

            state.SetStructuralRisk(StructuralRiskLevel.Safe);
            yield return null;
            AssertStateShown(view, StructuralRiskLevel.Safe);
        }

        private static void AssertStateShown(StructuralHudView view, StructuralRiskLevel level)
        {
            var kind = StructuralStatusPresentation.ToKind(level);
            var style = StructuralStatusPresentation.Resolve(kind);
            Assert.That(view.CurrentKind, Is.EqualTo(kind));
            Assert.That(view.StructuralRiskText.text, Is.EqualTo(HudFormatter.FormatStructuralRisk(level)));
            Assert.That(view.StructuralRiskText.color, Is.EqualTo(style.TextColor));
            Assert.That(view.StructuralRiskText.isTextTruncated, Is.False, "구조 상태 문구가 잘렸습니다.");
            Assert.That(view.IsPulsing, Is.EqualTo(style.Pulses));
            if (!style.Pulses)
            {
                Assert.That(view.GlowImage.color.a, Is.Zero, "안전/주의 상태에는 발광이 없어야 한다.");
            }
            else
            {
                Assert.That(view.GlowImage.color.a, Is.LessThanOrEqualTo(style.GlowColor.a + 0.0001f));
            }
        }

        private static void AssertLayout(StructuralHudView view, BasicHudView hud)
        {
            var row = ScreenRect((RectTransform)view.transform);
            var frame = ScreenRect((RectTransform)hud.transform);
            Assert.That(frame.Contains(new Vector2(row.xMin, row.yMin)) && frame.Contains(new Vector2(row.xMax, row.yMax)),
                Is.True, "구조 상태 행이 HUD 프레임 밖으로 나갔습니다.");

            // 발광 이미지는 행 안에 머물러 체력·전력 게이지로 번지지 않는다.
            var glow = ScreenRect((RectTransform)view.GlowImage.transform);
            Assert.That(glow.xMin, Is.GreaterThanOrEqualTo(row.xMin - 0.5f));
            Assert.That(glow.xMax, Is.LessThanOrEqualTo(row.xMax + 0.5f));
            Assert.That(glow.yMin, Is.GreaterThanOrEqualTo(row.yMin - 0.5f));
            Assert.That(glow.yMax, Is.LessThanOrEqualTo(row.yMax + 0.5f));
            foreach (var gaugeName in new[] { "HealthGauge", "EnergyGauge" })
            {
                var gauge = hud.transform.Find(gaugeName);
                Assert.That(gauge, Is.Not.Null, gaugeName);
                Assert.That(glow.Overlaps(ScreenRect((RectTransform)gauge)), Is.False, gaugeName + "와 겹칩니다.");
            }

            var quest = GameObject.Find("QuestSummaryButton");
            if (quest != null && quest.activeInHierarchy)
            {
                var questRect = ScreenRect((RectTransform)quest.transform);
                Assert.That(frame.Overlaps(questRect), Is.False, "HUD가 퀘스트 카드와 겹칩니다.");
                Assert.That(frame.yMin - questRect.yMax, Is.GreaterThanOrEqualTo(0f), "퀘스트 카드 간격이 음수입니다.");
            }
        }

        private static Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var canvas = rect.GetComponentInParent<Canvas>().rootCanvas;
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            var max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
