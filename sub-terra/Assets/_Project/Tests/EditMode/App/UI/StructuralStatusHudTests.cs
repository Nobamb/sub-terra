using System.Linq;
using NUnit.Framework;
using SubTerra.App.State;
using SubTerra.App.UI.Hazards;
using SubTerra.App.UI.HUD;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    /// <summary>Prompt-B 131: HUD 하단 구조 상태 행의 표시 규칙과 프리팹 배치.</summary>
    public sealed class StructuralStatusHudTests
    {
        private const string BasicHudPath = "Assets/_Project/Prefabs/UI/BasicHUD.prefab";
        private const string HudCanvasPath = "Assets/_Project/Prefabs/UI/HUDCanvas.prefab";

        private static readonly StructuralRiskLevel[] AllLevels =
        {
            StructuralRiskLevel.Safe, StructuralRiskLevel.Caution,
            StructuralRiskLevel.Critical, StructuralRiskLevel.Imminent
        };

        [Test]
        public void ToKind_MapsEveryExistingRiskLevelOneToOne()
        {
            Assert.That(StructuralStatusPresentation.ToKind(StructuralRiskLevel.Safe), Is.EqualTo(StructuralStatusKind.Safe));
            Assert.That(StructuralStatusPresentation.ToKind(StructuralRiskLevel.Caution), Is.EqualTo(StructuralStatusKind.Caution));
            Assert.That(StructuralStatusPresentation.ToKind(StructuralRiskLevel.Critical), Is.EqualTo(StructuralStatusKind.Critical));
            Assert.That(StructuralStatusPresentation.ToKind(StructuralRiskLevel.Imminent), Is.EqualTo(StructuralStatusKind.Imminent));
            Assert.That(
                System.Enum.GetValues(typeof(StructuralStatusKind)).Length,
                Is.EqualTo(System.Enum.GetValues(typeof(StructuralRiskLevel)).Length),
                "새 위험 단계를 HUD에서 임의로 추가하지 않는다.");
        }

        [Test]
        public void Resolve_EachStateHasDistinctIconAndColor()
        {
            var styles = AllLevels
                .Select(l => StructuralStatusPresentation.Resolve(StructuralStatusPresentation.ToKind(l)))
                .ToArray();

            Assert.That(styles.Select(s => s.IconIndex).Distinct().Count(), Is.EqualTo(4));
            Assert.That(styles.Select(s => s.TextColor).Distinct().Count(), Is.EqualTo(4));
        }

        [Test]
        public void Resolve_SafeAndCaution_HaveNoGlowAndNoPulse()
        {
            foreach (var kind in new[] { StructuralStatusKind.Safe, StructuralStatusKind.Caution })
            {
                var style = StructuralStatusPresentation.Resolve(kind);
                Assert.That(style.Pulses, Is.False, kind.ToString());
                Assert.That(style.GlowColor.a, Is.Zero, kind.ToString());
            }
        }

        [Test]
        public void Resolve_CriticalAndImminent_PulseWithWeakRedGlow()
        {
            foreach (var kind in new[] { StructuralStatusKind.Critical, StructuralStatusKind.Imminent })
            {
                var style = StructuralStatusPresentation.Resolve(kind);
                Assert.That(style.Pulses, Is.True, kind.ToString());
                Assert.That(style.GlowColor.r, Is.GreaterThan(style.GlowColor.g * 3f), "붉은 계열");
                Assert.That(style.GlowColor.a, Is.InRange(0.05f, 0.35f), "약한 발광");
            }
        }

        [Test]
        public void PulseFactor_IsSlowSmoothAndBounded()
        {
            Assert.That(StructuralStatusPresentation.PulsePeriodSeconds, Is.GreaterThanOrEqualTo(2f));
            Assert.That(StructuralStatusPresentation.PulseFactor(0f), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(
                StructuralStatusPresentation.PulseFactor(StructuralStatusPresentation.PulsePeriodSeconds * 0.5f),
                Is.EqualTo(1f).Within(0.0001f));
            for (var t = 0f; t < 10f; t += 0.05f)
            {
                var value = StructuralStatusPresentation.PulseFactor(t);
                Assert.That(value, Is.InRange(0f, 1f));
            }
        }

        [Test]
        public void HudFormatter_StructuralTexts_KeepExistingLabels()
        {
            Assert.That(HudFormatter.FormatStructuralRisk(StructuralRiskLevel.Safe), Is.EqualTo("구조 안전"));
            Assert.That(HudFormatter.FormatStructuralRisk(StructuralRiskLevel.Caution), Is.EqualTo("구조 주의"));
            Assert.That(HudFormatter.FormatStructuralRisk(StructuralRiskLevel.Critical), Is.EqualTo("구조 위험"));
            Assert.That(HudFormatter.FormatStructuralRisk(StructuralRiskLevel.Imminent), Is.EqualTo("구조 붕괴 임박"));
        }

        [Test]
        public void Presenter_PassesTextAndKind_ForInitialRenderAndChanges()
        {
            var state = GameState.CreateNew();
            state.SetStructuralRisk(StructuralRiskLevel.Critical);
            var view = new RecordingHudView();
            var presenter = new HudPresenter(view);

            presenter.Bind(state);
            Assert.That(view.StructuralKind, Is.EqualTo(StructuralStatusKind.Critical));
            Assert.That(view.Structural, Is.EqualTo("구조 위험"));

            state.SetStructuralRisk(StructuralRiskLevel.Imminent);
            Assert.That(view.StructuralKind, Is.EqualTo(StructuralStatusKind.Imminent));
            state.SetStructuralRisk(StructuralRiskLevel.Safe);
            Assert.That(view.StructuralKind, Is.EqualTo(StructuralStatusKind.Safe));
            Assert.That(view.Structural, Is.EqualTo("구조 안전"));
        }

        [Test]
        public void Presenter_RebindManyTimes_DoesNotDuplicateStructuralUpdates()
        {
            var state = GameState.CreateNew();
            var view = new RecordingHudView();
            var presenter = new HudPresenter(view);
            for (var i = 0; i < 4; i++)
            {
                presenter.Bind(state);
            }

            presenter.Unbind();
            presenter.Bind(state);
            view.ResetCounts();

            state.SetStructuralRisk(StructuralRiskLevel.Caution);
            Assert.That(view.StructuralCount, Is.EqualTo(1));

            presenter.Unbind();
            view.ResetCounts();
            state.SetStructuralRisk(StructuralRiskLevel.Critical);
            Assert.That(view.StructuralCount, Is.Zero);
        }

        [Test]
        public void BasicHud_LegacyPowerIconRemoved_AndRowSitsInsideFrameBelowSeparator()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasicHudPath);
            var instance = Object.Instantiate(prefab);
            try
            {
                var root = (RectTransform)instance.transform;
                Assert.That(root.sizeDelta, Is.EqualTo(new Vector2(430f, 248f)), "HUD 크기는 그대로");
                Assert.That(root.Find("HudIcon6"), Is.Null, "전력·연결·활성 아이콘 제거");

                var row = (RectTransform)root.Find("StructuralStatusRow");
                Assert.That(row, Is.Not.Null);
                var separator = (RectTransform)root.Find("BottomSeparator");
                var rowTop = -row.anchoredPosition.y;
                Assert.That(rowTop, Is.GreaterThan(-separator.anchoredPosition.y), "하단 구분선 아래");
                Assert.That(rowTop + row.sizeDelta.y, Is.LessThanOrEqualTo(248f), "프레임 안쪽");
                Assert.That(row.anchoredPosition.x + row.sizeDelta.x, Is.LessThanOrEqualTo(430f));

                // 아이콘·텍스트 수직 중심이 같고, 텍스트 시작 x는 깊이/골드 텍스트와 맞는다.
                var icon = (RectTransform)row.Find("Icon");
                var label = (RectTransform)row.Find("Label");
                Assert.That(-icon.anchoredPosition.y + icon.sizeDelta.y * 0.5f,
                    Is.EqualTo(-label.anchoredPosition.y + label.sizeDelta.y * 0.5f).Within(0.01f));
                var depth = instance.GetComponent<BasicHudView>().DepthText.rectTransform;
                Assert.That(row.anchoredPosition.x + label.anchoredPosition.x,
                    Is.EqualTo(depth.anchoredPosition.x).Within(0.01f));

                // 별도 배지 테두리/배경 없이 아이콘·라벨·글로우만 존재한다.
                var children = Enumerable.Range(0, row.childCount).Select(i => row.GetChild(i).name).ToArray();
                Assert.That(children, Is.EquivalentTo(new[] { "Glow", "Icon", "Label" }));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void StructuralRow_ShowsMatchingIconTextColorAndGlow_ForEveryState()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasicHudPath);
            var instance = Object.Instantiate(prefab);
            try
            {
                var view = instance.GetComponentInChildren<StructuralHudView>(true);
                Assert.That(view, Is.Not.Null);
                var sprites = new System.Collections.Generic.List<Sprite>();

                foreach (var level in AllLevels)
                {
                    var kind = StructuralStatusPresentation.ToKind(level);
                    var style = StructuralStatusPresentation.Resolve(kind);
                    view.SetStructuralRisk(HudFormatter.FormatStructuralRisk(level), kind);

                    Assert.That(view.StructuralRiskText.text, Is.EqualTo(HudFormatter.FormatStructuralRisk(level)));
                    Assert.That(view.StructuralRiskText.color, Is.EqualTo(style.TextColor));
                    Assert.That(view.StatusIcon.sprite, Is.Not.Null);
                    sprites.Add(view.StatusIcon.sprite);
                    Assert.That(view.GlowImage.color.a, Is.Zero, "펄스 시작 전 발광은 0");
                    Assert.That(view.CurrentKind, Is.EqualTo(kind));
                }

                Assert.That(sprites.Distinct().Count(), Is.EqualTo(4), "상태별 아이콘이 달라야 한다");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void StructuralRow_LongestLabel_FitsWithoutClipping()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasicHudPath);
            var instance = Object.Instantiate(prefab);
            try
            {
                var label = instance.GetComponentInChildren<StructuralHudView>(true).StructuralRiskText;
                foreach (var level in AllLevels)
                {
                    var text = HudFormatter.FormatStructuralRisk(level);
                    var size = label.GetPreferredValues(text);
                    Assert.That(size.x, Is.LessThanOrEqualTo(label.rectTransform.rect.width), text);
                    Assert.That(size.y, Is.LessThanOrEqualTo(label.rectTransform.rect.height), text);
                }

                Assert.That(label.textWrappingMode, Is.EqualTo(TextWrappingModes.NoWrap));
                Assert.That(label.alignment, Is.EqualTo(TextAlignmentOptions.MidlineLeft));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void GlowImage_IsConfinedToRowAndNotRaycastable()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasicHudPath);
            var instance = Object.Instantiate(prefab);
            try
            {
                var view = instance.GetComponentInChildren<StructuralHudView>(true);
                var glow = (RectTransform)view.GlowImage.transform;
                Assert.That(glow.parent, Is.EqualTo(view.transform));
                Assert.That(glow.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(glow.anchorMax, Is.EqualTo(Vector2.one));
                Assert.That(glow.offsetMin, Is.EqualTo(Vector2.zero));
                Assert.That(glow.offsetMax, Is.EqualTo(Vector2.zero));
                Assert.That(view.GlowImage.raycastTarget, Is.False);
                Assert.That(view.GetComponent<StructuralStatusPulse>(), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void HudCanvas_HasSingleStructuralView_BoundToBottomRow_AndNoLegacyTopRightOrPowerText()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudCanvasPath);
            var views = prefab.GetComponentsInChildren<StructuralHudView>(true);
            Assert.That(views.Length, Is.EqualTo(1), "우측 상단 구조 표시와 중복되면 안 된다");
            Assert.That(views[0].GetComponentInParent<BasicHudView>(true), Is.Not.Null);

            var binder = prefab.GetComponent<HudBinder>();
            Assert.That(binder.StructuralHud, Is.EqualTo(views[0]));
            Assert.That(binder.HasRequiredReferences(), Is.True);

            var names = prefab.GetComponentsInChildren<Transform>(true).Select(t => t.name).ToArray();
            Assert.That(names, Does.Not.Contain("StructuralHUD"));
            Assert.That(names, Does.Not.Contain("StructuralRiskText"));
            Assert.That(names, Does.Not.Contain("PowerConnectionText"));

            var hazard = prefab.GetComponent<HazardHudView>();
            Assert.That(hazard.HasRequiredReferences(), Is.True, "가스 경고 참조는 유지");
        }

        [Test]
        public void HazardHudView_PowerAndStructuralStatus_AreAcceptedWithoutDisplayingAnything()
        {
            var go = new GameObject("HazardHudTest");
            try
            {
                var view = go.AddComponent<HazardHudView>();
                Assert.DoesNotThrow(() =>
                {
                    view.SetPowerStatus(new PowerStatusReadModel(true, 5f, 3f, 2, string.Empty));
                    view.SetStructuralStatus(new HazardStatusReadModel(HazardSeverity.Critical, "위험", string.Empty));
                });
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
