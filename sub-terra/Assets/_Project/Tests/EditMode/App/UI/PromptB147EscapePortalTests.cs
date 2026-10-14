using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Integration;
using SubTerra.App.Run;
using SubTerra.App.State;
using SubTerra.App.UI.EmergencyEscape;
using SubTerra.App.UI;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Player;
using SubTerra.Gameplay.Power;
using SubTerra.Shared;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Tests.UI
{
    /// <summary>prompt-B 147: 긴급 탈출 포탈 터미널 창 연출·목록과 상호작용 우선순위.</summary>
    public sealed class PromptB147EscapePortalTests
    {
        private const string PanelPrefabPath = "Assets/_Project/Prefabs/UI/EmergencyEscapePanel.prefab";
        private readonly List<GameObject> spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (var i = spawned.Count - 1; i >= 0; i--)
            {
                if (spawned[i] != null)
                {
                    Object.DestroyImmediate(spawned[i]);
                }
            }

            spawned.Clear();
        }

        // ---------------------------------------------------------------- 열기 타임라인

        [Test]
        public void OpenTimeline_PhasesRunInOrder_WormholeThenPopThenConnect()
        {
            Assert.That(EscapePortalOpenTimeline.SpinUpEnd, Is.LessThanOrEqualTo(EscapePortalOpenTimeline.PopStart));
            Assert.That(EscapePortalOpenTimeline.PopEnd, Is.LessThanOrEqualTo(EscapePortalOpenTimeline.ConnectStart));
            Assert.That(EscapePortalOpenTimeline.ConnectStart, Is.LessThan(EscapePortalOpenTimeline.ConnectEnd));

            var start = EscapePortalOpenTimeline.EvaluateOpen(0.05f);
            Assert.That(start.WormholeAlpha, Is.GreaterThan(0f), "웜홀이 먼저 나타난다.");
            Assert.That(start.WindowAlpha, Is.EqualTo(0f), "이 시점에 창은 아직 없다.");
            Assert.That(start.Interactable, Is.False);

            var mid = EscapePortalOpenTimeline.EvaluateOpen(EscapePortalOpenTimeline.PopStart + 0.05f);
            Assert.That(mid.WindowScale, Is.InRange(EscapePortalOpenTimeline.PopStartScale, 1f));
            Assert.That(mid.WindowTilt, Is.GreaterThan(0f).And.LessThan(EscapePortalOpenTimeline.PopStartTilt),
                "창은 기울어진 채 앞으로 튀어나온다.");
            Assert.That(mid.Line1, Is.EqualTo(0f), "접속 줄은 창이 열린 뒤에 시작한다.");

            var popped = EscapePortalOpenTimeline.EvaluateOpen(EscapePortalOpenTimeline.PopEnd);
            Assert.That(popped.WindowScale, Is.EqualTo(1f).Within(0.001f));
            Assert.That(popped.WindowTilt, Is.EqualTo(0f).Within(0.001f));
            Assert.That(popped.Interactable, Is.True);

            var connecting = EscapePortalOpenTimeline.EvaluateOpen(
                EscapePortalOpenTimeline.ConnectStart + EscapePortalOpenTimeline.ConnectLineSeconds * 0.5f);
            Assert.That(connecting.Line1, Is.InRange(0.01f, 0.99f));
            Assert.That(connecting.Line2, Is.EqualTo(0f));
        }

        [Test]
        public void OpenTimeline_TotalTimeStaysUnderLimit_AndConnectIsShort()
        {
            Assert.That(EscapePortalOpenTimeline.OpenDuration, Is.LessThanOrEqualTo(1.2f));
            Assert.That(
                EscapePortalOpenTimeline.ConnectEnd - EscapePortalOpenTimeline.ConnectStart,
                Is.LessThanOrEqualTo(0.3f + 0.0001f));
            Assert.That(EscapePortalOpenTimeline.ConnectLineCount, Is.InRange(1, 2));
            Assert.That(EscapePortalOpenTimeline.CloseDuration, Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(EscapePortalOpenTimeline.EvaluateOpen(EscapePortalOpenTimeline.OpenDuration).Finished, Is.True);
            Assert.That(EscapePortalOpenTimeline.EvaluateOpen(EscapePortalOpenTimeline.OpenDuration - 0.01f).Finished, Is.False);
        }

        [Test]
        public void OpenTimeline_WormholeSpinAccelerates()
        {
            var early = EscapePortalOpenTimeline.SpinSpeed(0.05f);
            var late = EscapePortalOpenTimeline.SpinSpeed(0.45f);
            Assert.That(late, Is.GreaterThan(early));
            Assert.That(EscapePortalOpenTimeline.SpinSpeed(EscapePortalOpenTimeline.SpinUpEnd),
                Is.EqualTo(EscapePortalOpenTimeline.SpinSpeedMax).Within(0.01f));

            // 같은 간격에서 각도 증가량이 점점 커진다(가속).
            var first = EscapePortalOpenTimeline.SpinAngle(0.25f) - EscapePortalOpenTimeline.SpinAngle(0f);
            var second = EscapePortalOpenTimeline.SpinAngle(0.5f) - EscapePortalOpenTimeline.SpinAngle(0.25f);
            Assert.That(second, Is.GreaterThan(first));
        }

        [Test]
        public void OpenTimeline_SkipJumpsToEndState()
        {
            var end = EscapePortalOpenTimeline.OpenEndPose;
            Assert.That(end.Finished, Is.True);
            Assert.That(end.Interactable, Is.True);
            Assert.That(end.WindowScale, Is.EqualTo(1f).Within(0.001f));
            Assert.That(end.WindowTilt, Is.EqualTo(0f).Within(0.001f));
            Assert.That(end.WindowAlpha, Is.EqualTo(1f));
            Assert.That(end.Line1, Is.EqualTo(1f));
            Assert.That(end.Line2, Is.EqualTo(1f));
        }

        [Test]
        public void CloseTimeline_PullsWindowIntoWormholeWithinShortTime()
        {
            var begin = EscapePortalOpenTimeline.EvaluateClose(0f, 100f);
            Assert.That(begin.WindowScale, Is.EqualTo(1f).Within(0.001f));
            Assert.That(begin.Finished, Is.False);

            var end = EscapePortalOpenTimeline.EvaluateClose(EscapePortalOpenTimeline.CloseDuration, 100f);
            Assert.That(end.Finished, Is.True);
            Assert.That(end.WindowScale, Is.EqualTo(EscapePortalOpenTimeline.PopStartScale).Within(0.001f));
            Assert.That(end.WindowAlpha, Is.EqualTo(0f).Within(0.001f));
            Assert.That(end.WormholeAlpha, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void View_OpensToEndStateInEditMode_AndSkipDoesNothingOutsideOpening()
        {
            var host = Spawn("B147_ViewOnly");
            var view = host.AddComponent<EmergencyEscapePanelView>();
            SetField(view, "panelRoot", host);

            view.SetVisible(true);
            Assert.That(view.State, Is.EqualTo(EmergencyEscapePanelView.PlayState.Open));
            Assert.That(view.IsOpen, Is.True);
            view.SkipOpen();
            Assert.That(view.State, Is.EqualTo(EmergencyEscapePanelView.PlayState.Open));

            view.BeginClose(null);
            Assert.That(view.IsOpen, Is.False);
        }

        // ---------------------------------------------------------------- 표시 우선순위·웜홀

        [Test]
        public void PanelPrefab_DrawsAboveOtherPopups_AndWormholeCanRender()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath);
            // HUDCanvas 아래에 놓인 것과 같은 조건에서 창을 열면 자체 Canvas가 최상단으로 정렬된다.
            var hud = Spawn("B147_HudCanvas");
            hud.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, hud.transform);
            var view = instance.GetComponent<EmergencyEscapePanelView>();
            view.SetVisible(true);
            var root = view.PanelRoot.GetComponent<Canvas>();
            Assert.That(root, Is.Not.Null, "창은 자체 Canvas로 다른 HUD 위에 그려진다.");
            Assert.That(root.overrideSorting, Is.True);
            Assert.That(root.sortingOrder, Is.EqualTo(EmergencyEscapePanelView.SortingOrder));
            Assert.That(root.sortingOrder, Is.GreaterThan(32_100 + 16), "팝업 창(32,100~)보다 앞.");
            Assert.That(root.sortingOrder, Is.GreaterThan(32_000), "광산 초기화 시계보다 앞.");
            Assert.That(root.sortingOrder, Is.LessThan(PopupWindowSorting.SettingsSortOrder), "설정 창만 더 위.");
            Assert.That(root.GetComponent<UnityEngine.UI.GraphicRaycaster>(), Is.Not.Null);

            Assert.That(prefab.GetComponentInChildren<MinimapGlyphGraphic>(true), Is.Null,
                "미니맵 범례의 분홍 3갈래 기호(선풍기 느낌)는 더 이상 웜홀에 쓰지 않는다.");
            var particles = prefab.GetComponentInChildren<EscapeWormholeParticleGraphic>(true);
            Assert.That(particles, Is.Not.Null, "청록 입자가 솟아오르는 그래픽이 있다.");
            Assert.That(particles.GetComponent<CanvasRenderer>(), Is.Not.Null,
                "CanvasRenderer가 없으면 웜홀이 그려지지 않는다.");
        }

        // ---------------------------------------------------------------- 블랙홀 웜홀

        [Test]
        public void WormholeSpin_IsSlowEnoughNotToLookLikeAFan()
        {
            Assert.That(EscapePortalOpenTimeline.SpinSpeedMax, Is.LessThanOrEqualTo(300f),
                "초당 3.5바퀴를 넘으면 선풍기처럼 보인다.");
        }

        [Test]
        public void WormholeArt_IsCyanBlackHole_EmptyCenterAndNoPink()
        {
            for (var layer = 0; layer < EscapeWormholeArt.LayerCount; layer++)
            {
                Assert.That(EscapeWormholeArt.SwirlPixel(layer, 0f, 0f).a, Is.EqualTo(0f).Within(0.01f),
                    "소용돌이 한가운데는 비어 있고 그 자리를 검은 지평선이 채운다.");
                Assert.That(EscapeWormholeArt.SwirlPixel(layer, 1.1f, 0f).a, Is.EqualTo(0f));

                var strongest = 0f;
                for (var step = 0; step < 360; step += 3)
                {
                    for (var r = 0.45f; r < 0.98f; r += 0.05f)
                    {
                        var angle = step * Mathf.Deg2Rad;
                        var pixel = EscapeWormholeArt.SwirlPixel(layer, Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);
                        strongest = Mathf.Max(strongest, pixel.a);
                        if (pixel.a > 0.05f)
                        {
                            Assert.That(pixel.r, Is.LessThan(pixel.b), "분홍(붉은 기운)이 아닌 청록 계열이다.");
                            Assert.That(pixel.g, Is.GreaterThanOrEqualTo(pixel.r));
                        }
                    }
                }

                Assert.That(strongest, Is.GreaterThan(0.3f), "소용돌이가 실제로 보인다.");
            }
        }

        [Test]
        public void WormholeArt_SwirlIsSmoothGasNotSeparateBlades()
        {
            // 같은 반지름의 한 바퀴에서 투명도가 완전히 0으로 끊기는 빈틈(날개 사이 공백)이 없어야 한다.
            for (var layer = 0; layer < EscapeWormholeArt.LayerCount; layer++)
            {
                var r = layer == 0 ? 0.6f : 0.75f;
                for (var step = 0; step < 360; step += 2)
                {
                    var angle = step * Mathf.Deg2Rad;
                    var pixel = EscapeWormholeArt.SwirlPixel(layer, Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);
                    Assert.That(pixel.a, Is.GreaterThan(0.08f), "layer " + layer + " 각도 " + step);
                }
            }
        }

        [Test]
        public void WormholeCore_IsBlackDiscWithBrightCyanRing()
        {
            Assert.That(EscapeWormholeArt.Core(), Is.Not.Null);
            var center = EscapeWormholeArt.CorePixel(0f, 0f);
            Assert.That(center.a, Is.GreaterThan(0.95f));
            Assert.That(center.r + center.g + center.b, Is.LessThan(0.1f), "중심은 검다.");

            var ring = EscapeWormholeArt.CorePixel(EscapeWormholeArt.CoreRingRadius, 0f);
            Assert.That(ring.g, Is.GreaterThan(0.6f), "광자 고리는 밝은 청록이다.");
            Assert.That(ring.r, Is.LessThan(ring.g));
            Assert.That(EscapeWormholeArt.CorePixel(1.05f, 0f).a, Is.EqualTo(0f), "바깥은 투명하다.");
        }

        [Test]
        public void ParticleField_SpawnsAroundHoleAndRisesUpwardAndSpreads()
        {
            var field = new EscapeWormholeParticleField { RingRadius = 76f };
            field.Prime(1f);
            Assert.That(field.Count, Is.GreaterThan(20), "열리는 순간 이미 입자가 피어오른다.");
            Assert.That(field.Count, Is.LessThanOrEqualTo(EscapeWormholeParticleField.Capacity));

            // 새로 태어난 입자를 따로 추적한다.
            var single = new EscapeWormholeParticleField { RingRadius = 76f };
            single.Tick(0.05f, true);
            Assert.That(single.Count, Is.GreaterThan(0));
            var born = single[0];
            var startRadius = born.Position.magnitude;
            Assert.That(startRadius, Is.InRange(76f * 0.9f, 76f * 1.3f), "구멍 가장자리 고리에서 태어난다.");
            Assert.That(born.Velocity.y, Is.GreaterThan(100f), "위로 솟는다.");

            var startY = born.Position.y;
            single.Tick(0.5f, false);
            Assert.That(single.Count, Is.GreaterThan(0));
            Assert.That(single[0].Position.y, Is.GreaterThan(startY + 40f), "시간이 지나면 위로 올라간다.");
        }

        [Test]
        public void ParticleField_FadesOutAndDisappears_WhenNotEmitting()
        {
            var field = new EscapeWormholeParticleField();
            field.Prime(1f);
            for (var i = 0; i < 120 && field.Count > 0; i++)
            {
                field.Tick(1f / 30f, false);
            }

            Assert.That(field.Count, Is.EqualTo(0), "방출을 멈추면 남은 입자가 모두 사라진다.");
            Assert.That(EscapeWormholeParticleField.AlphaAt(0f), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(EscapeWormholeParticleField.AlphaAt(0.3f), Is.GreaterThan(0.9f));
            Assert.That(EscapeWormholeParticleField.AlphaAt(1f), Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void ParticleField_IsDeterministicForTheSameInput()
        {
            var a = new EscapeWormholeParticleField();
            var b = new EscapeWormholeParticleField();
            a.Prime(1.2f);
            b.Prime(1.2f);
            Assert.That(a.Count, Is.EqualTo(b.Count));
            for (var i = 0; i < a.Count; i++)
            {
                Assert.That(a[i].Position, Is.EqualTo(b[i].Position));
            }
        }

        [Test]
        public void PanelPrefab_WormholeLayers_AreWiredAndSwirlLayersTurnAtDifferentSpeeds()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            spawned.Add(instance);
            var view = instance.GetComponent<EmergencyEscapePanelView>();
            view.SetVisible(true);

            var outer = instance.transform.Find("PanelRoot/Wormhole/SwirlOuter");
            var inner = instance.transform.Find("PanelRoot/Wormhole/SwirlInner");
            var core = instance.transform.Find("PanelRoot/Wormhole/Core");
            var particles = instance.transform.Find("PanelRoot/Wormhole/Particles");
            Assert.That(outer, Is.Not.Null);
            Assert.That(inner, Is.Not.Null);
            Assert.That(core, Is.Not.Null);
            Assert.That(particles, Is.Not.Null);
            Assert.That(particles.GetSiblingIndex(), Is.LessThan(core.GetSiblingIndex()),
                "아래쪽에서 올라오는 입자는 검은 지평선 뒤를 지난다.");
            Assert.That(outer.GetComponent<UnityEngine.UI.Image>().sprite, Is.Not.Null, "소용돌이 그림이 입혀진다.");
            Assert.That(core.GetComponent<UnityEngine.UI.Image>().sprite, Is.Not.Null);

            view.ApplyOpenPreview(0.4f);
            var outerAngle = Mathf.DeltaAngle(0f, outer.localEulerAngles.z);
            var innerAngle = Mathf.DeltaAngle(0f, inner.localEulerAngles.z);
            Assert.That(Mathf.Abs(innerAngle), Is.GreaterThan(Mathf.Abs(outerAngle)), "안쪽 원반이 더 빨리 돈다.");
            Assert.That(instance.transform.Find("PanelRoot/Wormhole").localEulerAngles.z, Is.EqualTo(0f).Within(0.01f),
                "웜홀 루트는 돌지 않아 입자가 항상 화면 위로 솟는다.");
        }

        // ---------------------------------------------------------------- 버튼 높이·호버

        [Test]
        public void PanelPrefab_HasOnlyTallConfirmButton_CloseIsCornerXOnly()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath);
            var window = prefab.transform.Find("PanelRoot/Window");
            var confirm = (RectTransform)window.Find("ConfirmButton");
            var result = (RectTransform)window.Find("ResultText");
            var list = (RectTransform)window.Find("ListPanel");
            Assert.That(window.Find("CloseButton"), Is.Null, "닫기는 우상단 ×가 맡아 닫기 버튼은 없다.");
            Assert.That(window.Find("CornerCloseButton"), Is.Not.Null);
            Assert.That(confirm.rect.height, Is.GreaterThanOrEqualTo(56f), "이동 버튼 세로가 넉넉하다.");

            var resultBottom = result.anchoredPosition.y - result.rect.height;
            var buttonTop = confirm.anchoredPosition.y;
            Assert.That(buttonTop, Is.LessThanOrEqualTo(resultBottom + 0.5f), "결과 줄과 버튼이 겹치지 않는다.");
            var buttonBottom = confirm.anchoredPosition.y - confirm.rect.height;
            var listBottom = list.anchoredPosition.y - list.rect.height;
            Assert.That(buttonBottom, Is.GreaterThanOrEqualTo(listBottom - 0.5f), "버튼이 목록 아래로 삐져나오지 않는다.");
        }

        [Test]
        public void ConfirmButton_HoverLightStaysInsideTheButton_AndNoButtonHasOuterGlow()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath);
            var window = prefab.transform.Find("PanelRoot/Window");
            var confirm = (RectTransform)window.Find("ConfirmButton");
            var skin = confirm.GetComponent<SubTerra.App.UI.MainMenu.MenuSpriteButtonSkin>();
            Assert.That(skin, Is.Not.Null, "이동 버튼은 호버 오버레이 페이드를 쓴다.");

            var overlay = (RectTransform)confirm.Find("HoverOverlay");
            Assert.That(overlay, Is.Not.Null);
            var image = overlay.GetComponent<UnityEngine.UI.Image>();
            Assert.That(image.sprite, Is.Not.Null, "호버 그림이 연결되어 있다.");
            Assert.That(image.raycastTarget, Is.False);
            Assert.That(image.color.a, Is.EqualTo(0f), "평소에는 보이지 않는다.");
            Assert.That(overlay.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(overlay.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(overlay.offsetMin, Is.EqualTo(Vector2.zero), "빛은 버튼 영역 안에만 있다.");
            Assert.That(overlay.offsetMax, Is.EqualTo(Vector2.zero));
            Assert.That(overlay.GetSiblingIndex(), Is.LessThan(confirm.Find("Label").GetSiblingIndex()), "글자는 빛 위에 그려진다.");

            // 버튼 바깥으로 번지는 형제 발광 오브젝트는 어디에도 없다.
            for (var i = 0; i < window.childCount; i++)
            {
                Assert.That(window.GetChild(i).name, Does.Not.EndWith("Glow"), window.GetChild(i).name);
            }

            foreach (var name in new[] { "ConfirmButton", "CornerCloseButton" })
            {
                foreach (var graphic in window.Find(name).GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                {
                    var rect = graphic.rectTransform;
                    var button = (RectTransform)window.Find(name);
                    Assert.That(rect.rect.width, Is.LessThanOrEqualTo(button.rect.width + 0.01f), name + "/" + graphic.name);
                    Assert.That(rect.rect.height, Is.LessThanOrEqualTo(button.rect.height + 0.01f), name + "/" + graphic.name);
                }
            }
        }

        [Test]
        public void CornerCloseButton_UsesSameHoverAsCoreCctvPopup()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath);
            var x = prefab.transform.Find("PanelRoot/Window/CornerCloseButton").GetComponent<UnityEngine.UI.Button>();
            var overlay = x.transform.Find("HoverOverlay");
            Assert.That(overlay, Is.Not.Null, "코어 팝업 × 버튼과 같은 청록 오버레이가 있다.");
            Assert.That(x.targetGraphic.gameObject, Is.SameAs(overlay.gameObject));
            Assert.That(x.transition, Is.EqualTo(UnityEngine.UI.Selectable.Transition.ColorTint));
            Assert.That(x.colors.normalColor.a, Is.EqualTo(0f));
            Assert.That(x.colors.highlightedColor.a, Is.EqualTo(0.18f).Within(0.001f), "코어 팝업과 같은 호버 세기.");
            Assert.That(x.colors.fadeDuration, Is.EqualTo(0.15f).Within(0.001f));
            Assert.That(overlay.GetSiblingIndex(), Is.EqualTo(0), "글자 뒤에 깔린다.");
        }

        [Test]
        public void ConfirmHoverOverlay_IsHiddenWhenTravelIsNotPossible()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            spawned.Add(instance);
            var view = instance.GetComponent<EmergencyEscapePanelView>();
            var options = new List<EmergencyEscapeDestinationOption>
            {
                new EmergencyEscapeDestinationOption(EmergencyEscapeDestination.Elevator, string.Empty, "엘리베이터")
            };
            var overlay = instance.transform.Find("PanelRoot/Window/ConfirmButton/HoverOverlay").gameObject;

            view.SetDestinations(EscapeDestinationListModel.Build(options, EmergencyEscapePaymentFailure.None));
            Assert.That(overlay.activeSelf, Is.True);
            view.SetDestinations(EscapeDestinationListModel.Build(options, EmergencyEscapePaymentFailure.InsufficientGold));
            Assert.That(view.ConfirmInteractable, Is.False);
            Assert.That(overlay.activeSelf, Is.False, "이동할 수 없으면 호버해도 밝아지지 않는다.");
        }

        [Test]
        public void DestinationRow_ShowsHoverTint_ButSelectedRowStaysBrighter()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            spawned.Add(instance);
            var view = instance.GetComponent<EmergencyEscapePanelView>();
            var options = new List<EmergencyEscapeDestinationOption>
            {
                new EmergencyEscapeDestinationOption(EmergencyEscapeDestination.Elevator, string.Empty, "엘리베이터"),
                new EmergencyEscapeDestinationOption(EmergencyEscapeDestination.OutpostCore, "c1", "코어 1")
            };
            var model = EscapeDestinationListModel.Build(options, EmergencyEscapePaymentFailure.None);
            model.Select(0);
            view.SetDestinations(model);

            var idle = view.RowAt(1);
            var background = idle.transform.GetComponent<UnityEngine.UI.Image>();
            var idleColor = background.color;
            idle.OnPointerEnter(null);
            Assert.That(background.color.g, Is.GreaterThan(idleColor.g), "호버하면 청록빛이 도는 밝은 면이 된다.");
            var selectedColor = view.RowAt(0).transform.GetComponent<UnityEngine.UI.Image>().color;
            Assert.That(selectedColor.g, Is.GreaterThan(background.color.g), "선택된 줄이 호버 줄보다 밝다.");
            idle.OnPointerExit(null);
            Assert.That(background.color, Is.EqualTo(idleColor));
        }

        [Test]
        public void PanelPrefab_UsesCoreStyleLayout_ListLeftCctvRightWithCornerClose()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath);
            var window = prefab.transform.Find("PanelRoot/Window");
            Assert.That(window, Is.Not.Null);
            var list = window.Find("ListPanel");
            var cctv = window.Find("CctvArea");
            Assert.That(list, Is.Not.Null);
            Assert.That(cctv, Is.Not.Null);
            Assert.That(((RectTransform)list).anchoredPosition.x, Is.LessThan(((RectTransform)cctv).anchoredPosition.x));
            Assert.That(window.Find("CornerCloseButton"), Is.Not.Null);
            Assert.That(prefab.GetComponent<EmergencyEscapePanelView>().HasRequiredReferences(), Is.True);
        }

        // ---------------------------------------------------------------- 목적지 목록

        [Test]
        public void DestinationList_DisablesAllRowsWithReason_WhenPaymentFails()
        {
            var options = new[]
            {
                new EmergencyEscapeDestinationOption(EmergencyEscapeDestination.Elevator, string.Empty, "엘리베이터"),
                new EmergencyEscapeDestinationOption(EmergencyEscapeDestination.OutpostCore, "core-1", "전진기지 코어 1")
            };

            var ok = EscapeDestinationListModel.Build(options, EmergencyEscapePaymentFailure.None);
            Assert.That(ok.Count, Is.EqualTo(2));
            Assert.That(ok.CanTravel, Is.True);
            Assert.That(ok.Rows[1].Reason, Is.Empty);

            var gold = EscapeDestinationListModel.Build(options, EmergencyEscapePaymentFailure.InsufficientGold);
            Assert.That(gold.Rows[0].Enabled, Is.False);
            Assert.That(gold.Rows[0].Reason, Is.EqualTo("골드 부족"));
            Assert.That(gold.CanTravel, Is.False);

            var energy = EscapeDestinationListModel.Build(options, EmergencyEscapePaymentFailure.InsufficientEnergy);
            Assert.That(energy.Rows[1].Reason, Is.EqualTo("전력 부족"));
        }

        [Test]
        public void DestinationList_StepClampsAtEnds_AndSelectionStaysInRange()
        {
            var options = new[]
            {
                new EmergencyEscapeDestinationOption(EmergencyEscapeDestination.Elevator, string.Empty, "엘리베이터"),
                new EmergencyEscapeDestinationOption(EmergencyEscapeDestination.OutpostCore, "core-1", "코어 1")
            };
            var model = EscapeDestinationListModel.Build(options, EmergencyEscapePaymentFailure.None);
            Assert.That(model.Step(-1), Is.False);
            Assert.That(model.SelectedIndex, Is.EqualTo(0));
            Assert.That(model.Step(1), Is.True);
            Assert.That(model.SelectedIndex, Is.EqualTo(1));
            Assert.That(model.Step(1), Is.False);
            model.Select(99);
            Assert.That(model.SelectedIndex, Is.EqualTo(1));

            var empty = EscapeDestinationListModel.Build(null, EmergencyEscapePaymentFailure.None);
            Assert.That(empty.Step(1), Is.False);
            Assert.That(empty.CanTravel, Is.False);
        }

        [Test]
        public void DestinationOptions_IncludeCoreRightAfterInstall_AndAfterSaveRestore()
        {
            var state = GameState.CreateNew();
            state.SetGold(300);
            var player = Spawn("B147_Player");
            var elevator = Spawn("B147_Elevator");
            var host = Spawn("B147_Bridge");
            var bridge = host.AddComponent<EmergencyEscapePortalRuntimeBridge>();
            bridge.Bind(state, player.transform, elevator.transform);

            var before = bridge.GetDestinationOptions().Count;

            // 코어 설치 직후: 새로 연 창이 곧바로 목록에 코어를 보여 준다.
            var core = SpawnCore("B147_Core1", "building.outpost_core.basic-b147-1");
            var afterInstall = bridge.GetDestinationOptions();
            Assert.That(afterInstall.Count, Is.EqualTo(before + 1));
            Assert.That(Contains(afterInstall, "building.outpost_core.basic-b147-1"), Is.True);

            // 세이브 로드 직후: 월드가 다시 만들어져도 같은 ID의 코어가 목록에 있다.
            Object.DestroyImmediate(core);
            Assert.That(Contains(bridge.GetDestinationOptions(), "building.outpost_core.basic-b147-1"), Is.False);
            SpawnCore("B147_Core1Restored", "building.outpost_core.basic-b147-1");
            SpawnCore("B147_Core2Restored", "building.outpost_core.basic-b147-2");
            var afterLoad = bridge.GetDestinationOptions();
            Assert.That(afterLoad[0].Kind, Is.EqualTo(EmergencyEscapeDestination.Elevator));
            Assert.That(Contains(afterLoad, "building.outpost_core.basic-b147-1"), Is.True);
            Assert.That(Contains(afterLoad, "building.outpost_core.basic-b147-2"), Is.True);
        }

        [Test]
        public void Panel_RebuildsListEveryTimeItOpens_WithCurrentDestinations()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath);
            Assert.That(prefab, Is.Not.Null);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            spawned.Add(instance);
            var view = instance.GetComponent<EmergencyEscapePanelView>();
            var binder = instance.GetComponent<EmergencyEscapePanelBinder>();
            Assert.That(view.HasRequiredReferences(), Is.True);

            var elevatorOnly = new List<EmergencyEscapeDestinationOption>
            {
                new EmergencyEscapeDestinationOption(EmergencyEscapeDestination.Elevator, string.Empty, "엘리베이터")
            };
            binder.Open(elevatorOnly, new EmergencyEscapeCost(100, 15));
            Assert.That(view.RowCount, Is.EqualTo(1));
            Assert.That(view.CostLine, Is.EqualTo("비용: 100G + 전력 15"));
            Assert.That(view.ConfirmInteractable, Is.True);
            binder.Close();

            var withCores = new List<EmergencyEscapeDestinationOption>(elevatorOnly)
            {
                new EmergencyEscapeDestinationOption(EmergencyEscapeDestination.OutpostCore, "c1", "전진기지 코어 1"),
                new EmergencyEscapeDestinationOption(EmergencyEscapeDestination.OutpostCore, "c2", "전진기지 코어 2")
            };
            binder.Open(withCores, new EmergencyEscapeCost(100, 15), EmergencyEscapePaymentFailure.InsufficientEnergy);
            Assert.That(view.RowCount, Is.EqualTo(3));
            Assert.That(view.RowAt(2).LabelText, Is.EqualTo("전진기지 코어 2"));
            Assert.That(view.RowAt(2).ReasonText, Is.EqualTo("전력 부족"));
            Assert.That(view.ConfirmInteractable, Is.False, "이동할 수 없으면 이동 버튼을 막는다.");

            binder.StepSelection(1);
            Assert.That(view.SelectedDestinationIndex, Is.EqualTo(1));
            binder.StepSelection(1);
            Assert.That(view.SelectedDestinationIndex, Is.EqualTo(2));
        }

        // ---------------------------------------------------------------- 완전 포함 판정

        [Test]
        public void Containment_FullyInside_StraddlingEdge_FullyOutside()
        {
            var zone = new Bounds(new Vector3(0f, 0f, 0f), new Vector3(2f, 2f, 1f));

            Assert.That(EscapePortalContainment.IsFullyInside(
                new Bounds(new Vector3(0.2f, -0.2f, 0f), new Vector3(0.6f, 1f, 1f)), zone), Is.True);
            Assert.That(EscapePortalContainment.IsFullyInside(
                new Bounds(Vector3.zero, new Vector3(2f, 2f, 1f)), zone), Is.True, "경계에 딱 맞으면 안이다.");

            Assert.That(EscapePortalContainment.IsFullyInside(
                new Bounds(new Vector3(0.8f, 0f, 0f), new Vector3(0.6f, 1f, 1f)), zone), Is.False, "오른쪽 변에 걸침");
            Assert.That(EscapePortalContainment.IsFullyInside(
                new Bounds(new Vector3(0f, -0.7f, 0f), new Vector3(0.6f, 1f, 1f)), zone), Is.False, "아래쪽 변에 걸침");

            Assert.That(EscapePortalContainment.IsFullyInside(
                new Bounds(new Vector3(5f, 5f, 0f), new Vector3(0.6f, 1f, 1f)), zone), Is.False);
        }

        // ---------------------------------------------------------------- 우선순위

        [Test]
        public void Priority_PortalWinsOnlyWhenPlayerIsFullyInside()
        {
            var network = Spawn("B147_Network").AddComponent<PowerNetworkSystem>();
            var portalObject = Spawn("B147_Portal");
            portalObject.transform.position = new Vector3(10f, 0f, 0f);
            var zone = portalObject.AddComponent<BoxCollider2D>();
            zone.isTrigger = true;
            zone.size = new Vector2(2f, 2f);
            var power = portalObject.AddComponent<PowerNode>();
            power.Configure(network, false, 0, 30, PowerPriority.Critical);
            var portal = portalObject.AddComponent<EmergencyEscapePortal>();
            network.Rebuild();

            var other = Spawn("B147_OtherFacility");
            other.transform.position = new Vector3(11.4f, 0f, 0f);

            var playerObject = Spawn("B147_Rider");
            playerObject.AddComponent<Rigidbody2D>().gravityScale = 0f;
            var body = playerObject.AddComponent<BoxCollider2D>();
            body.size = new Vector2(0.6f, 1f);
            var movement = playerObject.AddComponent<PlayerMovement>();
            var portObject = Spawn("B147_Port");
            var port = portObject.AddComponent<FakePort>();
            SetField(portal, "escapePort", port);
            SetField(portal, "rider", movement);

            // 완전히 안: 포탈이 대상이고 우선권을 가진다.
            playerObject.transform.position = new Vector3(10f, 0f, 0f);
            Physics2D.SyncTransforms();
            Assert.That(portal.IsPlayerFullyInside(movement), Is.True);
            Assert.That(portal.TryClaimInteractionPriority(), Is.True);
            Assert.That(portal.RequestEscape(), Is.True);
            Assert.That(port.OpenCount, Is.EqualTo(1));

            // 한 변이 걸침(다른 시설 쪽으로 나옴): 포탈은 대상에서 빠지고 다른 시설이 우선한다.
            playerObject.transform.position = new Vector3(10.8f, 0f, 0f);
            Physics2D.SyncTransforms();
            Assert.That(portal.IsPlayerFullyInside(movement), Is.False);
            Assert.That(portal.TryClaimInteractionPriority(), Is.False);
            AdvanceFrame(portal);
            Assert.That(portal.RequestEscape(), Is.False);
            Assert.That(port.OpenCount, Is.EqualTo(1), "걸친 상태에서는 패널이 열리지 않는다.");

            // 근처에 다른 시설이 없어도 같은 규칙: 완전히 밖.
            other.SetActive(false);
            playerObject.transform.position = new Vector3(13f, 0f, 0f);
            Physics2D.SyncTransforms();
            Assert.That(portal.TryClaimInteractionPriority(), Is.False);
        }

        [Test]
        public void NameTagGuide_FollowsSameRule_PortalOnlyWhenFullyInside()
        {
            var portalObject = Spawn("B147_TagPortal");
            portalObject.transform.position = new Vector3(0f, 0f, 0f);
            var zone = portalObject.AddComponent<BoxCollider2D>();
            zone.isTrigger = true;
            zone.size = new Vector2(2f, 2f);
            portalObject.AddComponent<PowerNode>();
            var portal = portalObject.AddComponent<EmergencyEscapePortal>();
            var instance = portalObject.AddComponent<BuildingInstance>();
            instance.Initialize("portal-b147", DataIds.Buildings.EmergencyEscapePortal);

            var playerObject = Spawn("B147_TagRider");
            playerObject.AddComponent<Rigidbody2D>().gravityScale = 0f;
            playerObject.AddComponent<BoxCollider2D>().size = new Vector2(0.6f, 1f);
            var movement = playerObject.AddComponent<PlayerMovement>();

            playerObject.transform.position = Vector3.zero;
            Physics2D.SyncTransforms();
            Assert.That(EmergencyEscapePortal.IsPortalTarget(instance, movement), Is.True);

            playerObject.transform.position = new Vector3(0.9f, 0f, 0f);
            Physics2D.SyncTransforms();
            Assert.That(EmergencyEscapePortal.IsPortalTarget(instance, movement), Is.False);
            Assert.That(portal, Is.Not.Null);
        }

        // ---------------------------------------------------------------- 도우미

        private GameObject Spawn(string name)
        {
            var created = new GameObject(name);
            spawned.Add(created);
            return created;
        }

        private GameObject SpawnCore(string name, string instanceId)
        {
            var core = Spawn(name);
            core.AddComponent<BuildingInstance>().Initialize(instanceId, DataIds.Buildings.OutpostCoreBasic);
            return core;
        }

        private static bool Contains(IReadOnlyList<EmergencyEscapeDestinationOption> options, string instanceId)
        {
            for (var i = 0; i < options.Count; i++)
            {
                if (options[i].InstanceId == instanceId)
                {
                    return true;
                }
            }

            return false;
        }

        // RequestEscape는 같은 프레임 중복 호출을 막으므로 테스트에서는 마지막 요청 프레임을 되돌린다.
        private static void AdvanceFrame(EmergencyEscapePortal portal)
        {
            SetField(portal, "lastRequestFrame", -1);
        }

        private static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private sealed class FakePort : MonoBehaviour, IEmergencyEscapePortalPort
        {
            public int OpenCount { get; private set; }

            public bool TryOpenEscapePanel(out string reason)
            {
                OpenCount++;
                reason = string.Empty;
                return true;
            }

            public IReadOnlyList<EmergencyEscapeDestinationOption> GetDestinationOptions()
            {
                return new[]
                {
                    new EmergencyEscapeDestinationOption(EmergencyEscapeDestination.Elevator, string.Empty, "엘리베이터")
                };
            }

            public bool TryEscapeTo(EmergencyEscapeDestination kind, string outpostInstanceId, out string reason)
            {
                reason = string.Empty;
                return true;
            }
        }
    }
}
