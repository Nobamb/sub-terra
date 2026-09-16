using System.IO;
using System.Linq;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.UI;
using SubTerra.App.UI.MainMenu;
using SubTerra.App.UI.SurfaceBase;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SubTerra.App.Tests.UI.MainMenu
{
    /// <summary>L-S01~S05 정적/구조 검증.</summary>
    public sealed class MainMenuStaticStructureTests
    {
        public void BuildScenes()
        {
            PhaseLMenuSceneBuilder.Build();
            PhaseOUiPolishBuilder.Build();
        }

        [TestCase(1920, 1080)]
        [TestCase(2560, 1440)]
        [TestCase(1366, 768)]
        [TestCase(2560, 1080)]
        public void Prompt103_MenuFitsScreen_AndDecorationsDoNotBlockButtons(int width, int height)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PromptB103MainMenuBuilder.PrefabPath);
            var instance = Object.Instantiate(prefab);
            try
            {
                var scale = Mathf.Sqrt(width / 1920f * height / 1080f);
                var root = instance.GetComponent<RectTransform>();
                root.sizeDelta = new Vector2(width / scale, height / scale);
                instance.GetComponent<MainMenuView>().RefreshLayout();
                var content = (RectTransform)root.Find("MenuContent");
                foreach (Transform child in content)
                {
                    var rect = (RectTransform)child;
                    var extent = rect.sizeDelta * 0.5f;
                    var position = rect.anchoredPosition;
                    Assert.That((Mathf.Abs(position.x) + extent.x) * content.localScale.x,
                        Is.LessThanOrEqualTo(root.rect.width * 0.5f), child.name);
                    Assert.That((Mathf.Abs(position.y) + extent.y) * content.localScale.y,
                        Is.LessThanOrEqualTo(root.rect.height * 0.5f), child.name);
                }
                Assert.That(instance.GetComponentsInChildren<SaveSlotCardView>(true).Length, Is.EqualTo(3));
                Assert.That(root.Find("Background").GetComponent<UnityEngine.UI.RawImage>().raycastTarget, Is.False);
                Assert.That(root.Find("Background").GetComponent<UnityEngine.UI.AspectRatioFitter>().aspectMode,
                    Is.EqualTo(UnityEngine.UI.AspectRatioFitter.AspectMode.EnvelopeParent));
                foreach (var graphic in content.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                    if (graphic.GetComponent<UnityEngine.UI.Button>() == null)
                        Assert.That(graphic.raycastTarget, Is.False, graphic.name);
            }
            finally { Object.DestroyImmediate(instance); }
        }

        [Test]
        public void Prompt103_1_VisualsAndEffectsVerified()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PromptB103MainMenuBuilder.PrefabPath);
            Assert.That(prefab, Is.Not.Null);
            var content = prefab.transform.Find("MenuContent");
            Assert.That(content, Is.Not.Null);

            // 1. "지금은 40미터다...", "봉인 너머에서..." 텍스트 삭제 검증
            Assert.That(content.Find("DepthCopy"), Is.Null);
            Assert.That(content.Find("SignalCopy"), Is.Null);

            // 2. 타이틀: 로고 이미지, 글로우 아우라, 파티클 연출 검증
            var title = content.Find("Title");
            Assert.That(title, Is.Not.Null);
            Assert.That(title.Find("TitleLogo"), Is.Not.Null);
            Assert.That(title.Find("TitleGlow"), Is.Not.Null);
            Assert.That(title.GetComponent<MenuTitleParticles>(), Is.Not.Null);
            Assert.That(title.Find("TitleGlow").GetComponent<MenuTitleAura>(), Is.Not.Null);

            // 3. 청록 실선 디바이더 검증
            var divider = content.Find("TitleDivider");
            Assert.That(divider, Is.Not.Null);
            Assert.That(divider.GetComponent<UnityEngine.UI.RawImage>().raycastTarget, Is.False);

            // 4. 슬롯 구성 (반투명, 모서리 컷, 이너 글로우, 코너 브라켓, 삼각형 화살표) 검증
            for (var i = 1; i <= 3; i++)
            {
                var slot = content.Find("Slot" + i);
                Assert.That(slot, Is.Not.Null);
                var btnImg = slot.GetComponent<UnityEngine.UI.Image>();
                Assert.That(btnImg.color.a, Is.InRange(0.45f, 0.55f));
                Assert.That(btnImg.sprite, Is.Not.Null);

                // 텍스트 & 썸네일 불투명도 검증
                var label = slot.Find("Label").GetComponent<TMP_Text>();
                Assert.That(label.color.a, Is.EqualTo(1f));
                var thumbnail = slot.Find("Thumbnail").GetComponent<UnityEngine.UI.RawImage>();
                Assert.That(thumbnail.color.a, Is.EqualTo(1f));

                var selection = slot.Find("Selection");
                Assert.That(selection, Is.Not.Null);
                Assert.That(selection.Find("InnerGlow"), Is.Not.Null);
                Assert.That(selection.Find("CornerBrackets"), Is.Not.Null);
                Assert.That(selection.Find("TriangleSelector"), Is.Not.Null);
            }

            // Visual Snapshot Capture for verification
            var instance = Object.Instantiate(prefab);
            var camGo = new GameObject("TestCamera", typeof(Camera));
            var camera = camGo.GetComponent<Camera>();
            var canvas = instance.GetComponent<Canvas>();
            if (canvas == null) canvas = instance.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.06f, 0.09f);

            var rt = new RenderTexture(1920, 1080, 24);
            camera.targetTexture = rt;
            var view = instance.GetComponent<MainMenuView>();
            if (view != null)
            {
                view.RefreshLayout();
                view.SetSelectedSlot(1, true, "탐사 기록 01 — 이어하기 가능");
            }
            Canvas.ForceUpdateCanvases();
            camera.Render();

            var prev = RenderTexture.active;
            try
            {
                RenderTexture.active = rt;
                var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                tex.Apply();
                var outPath = Path.Combine(Application.dataPath, "../../work_process/MVP2/103-main-menu/main-menu-103-1-final.png");
                File.WriteAllBytes(outPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            finally
            {
                RenderTexture.active = prev;
                camera.targetTexture = null;
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(camGo);
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void Prompt103_2_VisualsAndNavigationVerified()
        {
            PromptB103MainMenuBuilder.Build();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PromptB103MainMenuBuilder.PrefabPath);
            Assert.That(prefab, Is.Not.Null);
            var content = prefab.transform.Find("MenuContent");
            Assert.That(content, Is.Not.Null);

            // 1. 슬롯 코너 브라켓 두께 3px 및 Navigation.Mode.None 검증
            for (var i = 1; i <= 3; i++)
            {
                var slot = content.Find("Slot" + i);
                Assert.That(slot, Is.Not.Null);
                var btn = slot.GetComponent<UnityEngine.UI.Button>();
                Assert.That(btn.navigation.mode, Is.EqualTo(UnityEngine.UI.Navigation.Mode.None));

                var selection = slot.Find("Selection");
                Assert.That(selection, Is.Not.Null);
                var brackets = selection.Find("CornerBrackets");
                Assert.That(brackets, Is.Not.Null);

                var tlH = brackets.Find("TL_H") as RectTransform;
                var tlV = brackets.Find("TL_V") as RectTransform;
                var trH = brackets.Find("TR_H") as RectTransform;
                var trV = brackets.Find("TR_V") as RectTransform;
                var blH = brackets.Find("BL_H") as RectTransform;
                var blV = brackets.Find("BL_V") as RectTransform;
                var brH = brackets.Find("BR_H") as RectTransform;
                var brV = brackets.Find("BR_V") as RectTransform;

                Assert.That(tlH.sizeDelta.y, Is.EqualTo(3f));
                Assert.That(tlV.sizeDelta.x, Is.EqualTo(3f));
                Assert.That(trH.sizeDelta.y, Is.EqualTo(3f));
                Assert.That(trV.sizeDelta.x, Is.EqualTo(3f));
                Assert.That(blH.sizeDelta.y, Is.EqualTo(3f));
                Assert.That(blV.sizeDelta.x, Is.EqualTo(3f));
                Assert.That(brH.sizeDelta.y, Is.EqualTo(3f));
                Assert.That(brV.sizeDelta.x, Is.EqualTo(3f));
            }

            // 2. Sub-Terra 타이틀: 중앙 청록색 그림자 레이어 & TitleGlow Shadow 컴포넌트 검증
            var title = content.Find("Title");
            Assert.That(title, Is.Not.Null);
            var titleShadow = title.Find("TitleShadow") as RectTransform;
            Assert.That(titleShadow, Is.Not.Null);
            Assert.That(titleShadow.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(titleShadow.sizeDelta.x, Is.GreaterThan(592f));
            Assert.That(titleShadow.sizeDelta.y, Is.GreaterThan(120f));
            Assert.That(titleShadow.GetComponent<MenuTitleAura>(), Is.Not.Null);

            var titleGlow = title.Find("TitleGlow") as RectTransform;
            Assert.That(titleGlow, Is.Not.Null);
            var shadowComponent = titleGlow.GetComponent<UnityEngine.UI.Shadow>();
            Assert.That(shadowComponent, Is.Not.Null);
            Assert.That(shadowComponent.effectDistance, Is.EqualTo(Vector2.zero));

            var titleLogo = title.Find("TitleLogo");
            Assert.That(titleLogo, Is.Not.Null);
            Assert.That(titleShadow.GetSiblingIndex(), Is.EqualTo(0));
            Assert.That(titleGlow.GetSiblingIndex(), Is.EqualTo(1));
            Assert.That(titleLogo.GetSiblingIndex(), Is.EqualTo(2));

            // 3. 위/아래 방향키를 통한 세이브 파일 선택 (NavigateSlot) 로직 검증
            var instance = Object.Instantiate(prefab);
            try
            {
                var view = instance.GetComponent<MainMenuView>();
                Assert.That(view, Is.Not.Null);

                int lastSelectedSlot = 0;
                view.SlotSelected += s => lastSelectedSlot = s;

                // 초기 상태: 슬롯 1
                view.SetSelectedSlot(1, true, "메시지");
                Assert.That(view.CurrentSelectedSlotId, Is.EqualTo(1));

                // 1번에서 위로 이동 시 최소 1번 유지
                view.NavigateSlot(-1);
                Assert.That(lastSelectedSlot, Is.EqualTo(0));

                // 1번에서 아래로 이동 -> 2번 슬롯 이벤트 발생
                view.NavigateSlot(1);
                Assert.That(lastSelectedSlot, Is.EqualTo(2));
                view.SetSelectedSlot(2, true, "메시지 2");
                Assert.That(view.CurrentSelectedSlotId, Is.EqualTo(2));

                // 2번에서 아래로 이동 -> 3번 슬롯 이벤트 발생
                view.NavigateSlot(1);
                Assert.That(lastSelectedSlot, Is.EqualTo(3));
                view.SetSelectedSlot(3, true, "메시지 3");
                Assert.That(view.CurrentSelectedSlotId, Is.EqualTo(3));

                // 3번에서 아래로 이동 시 최대 3번 유지
                lastSelectedSlot = 0;
                view.NavigateSlot(1);
                Assert.That(lastSelectedSlot, Is.EqualTo(0));

                // 3번에서 위로 이동 -> 2번 슬롯 이벤트 발생
                view.NavigateSlot(-1);
                Assert.That(lastSelectedSlot, Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }

            // 4. 최종 스냅샷 렌더링 캡처
            var renderInstance = Object.Instantiate(prefab);
            var camGo = new GameObject("TestCamera", typeof(Camera));
            var camera = camGo.GetComponent<Camera>();
            var canvas = renderInstance.GetComponent<Canvas>();
            if (canvas == null) canvas = renderInstance.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.06f, 0.09f);

            var rt = new RenderTexture(1920, 1080, 24);
            camera.targetTexture = rt;
            var renderView = renderInstance.GetComponent<MainMenuView>();
            if (renderView != null)
            {
                renderView.RefreshLayout();
                renderView.SetSelectedSlot(1, true, "탐사 기록 01 — 이어하기 가능");
            }
            Canvas.ForceUpdateCanvases();
            camera.Render();

            var prev = RenderTexture.active;
            try
            {
                RenderTexture.active = rt;
                var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                tex.Apply();
                var outPath = Path.Combine(Application.dataPath, "../../work_process/MVP2/103-main-menu/main-menu-103-2-final.png");
                File.WriteAllBytes(outPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            finally
            {
                RenderTexture.active = prev;
                camera.targetTexture = null;
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(camGo);
                Object.DestroyImmediate(renderInstance);
            }
        }

        [Test]
        public void L_S01_MainMenu_ExposesNewContinueSlotsSettingsQuitVersion()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PhaseLMenuSceneBuilder.MainMenuPrefabPath);
            Assert.That(prefab, Is.Not.Null);
            var view = prefab.GetComponent<MainMenuView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view.HasRequiredReferences(), Is.True);
            Assert.That(prefab.GetComponent<MainMenuBinder>(), Is.Not.Null);
            foreach (var text in prefab.GetComponentsInChildren<TMP_Text>(true))
            {
                Assert.That(text.font, Is.Not.Null, text.name);
                Assert.That(text.font.material, Is.Not.Null, text.name + " material");
                Assert.That(text.font.atlasTextures, Is.Not.Null, text.name + " atlas");
                Assert.That(text.font.atlasTextures.Length, Is.GreaterThan(0), text.name + " atlas count");
                Assert.That(text.font.atlasTextures[0], Is.Not.Null, text.name + " atlas[0]");
            }

            var scene = EditorSceneManager.OpenScene(
                PhaseLMenuSceneBuilder.MainMenuScenePath,
                OpenSceneMode.Additive);
            try
            {
                Assert.That(FindInScene<MainMenuBinder>(scene), Is.Not.Null);
                Assert.That(FindInScene<SafeAreaFitter>(scene), Is.Not.Null);
                Assert.That(FindInScene<EventSystem>(scene), Is.Not.Null);
                var eventSystems = 0;
                foreach (var root in scene.GetRootGameObjects())
                {
                    eventSystems += root.GetComponentsInChildren<EventSystem>(true).Length;
                }

                Assert.That(eventSystems, Is.EqualTo(1), "L-S05: active EventSystem must be unique");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void L_S02_SurfaceBase_ExposesSellCraftUpgradeGoalsExplore()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PhaseLMenuSceneBuilder.SurfaceBasePrefabPath);
            Assert.That(prefab, Is.Not.Null);
            var view = prefab.GetComponent<SurfaceBaseView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view.HasRequiredReferences(), Is.True);
            var binder = prefab.GetComponent<SurfaceBaseBinder>();
            Assert.That(binder, Is.Not.Null);

            var scene = EditorSceneManager.OpenScene(
                PhaseLMenuSceneBuilder.SurfaceBaseScenePath,
                OpenSceneMode.Additive);
            try
            {
                Assert.That(FindInScene<SurfaceBaseBinder>(scene), Is.Not.Null);
                Assert.That(FindInScene<SafeAreaFitter>(scene), Is.Not.Null);
                Assert.That(FindInScene<EventSystem>(scene), Is.Not.Null);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            Assert.That(
                EditorBuildSettings.scenes.Any(
                    s => s.enabled && s.path == PhaseLMenuSceneBuilder.SurfaceBaseScenePath),
                Is.True);
            Assert.That(SceneNames.SurfaceBase, Is.EqualTo("SurfaceBase"));
        }

        [Test]
        public void L_S03_SurfaceMenu_DoesNotDuplicateEconomyProgressionLogic()
        {
            var surfaceDir = Path.Combine(
                Application.dataPath, "_Project", "Scripts", "App", "UI", "SurfaceBase");
            var menuDir = Path.Combine(
                Application.dataPath, "_Project", "Scripts", "App", "UI", "MainMenu");
            foreach (var dir in new[] { surfaceDir, menuDir })
            {
                Assert.That(Directory.Exists(dir), Is.True, dir);
                foreach (var file in Directory.GetFiles(dir, "*.cs"))
                {
                    var text = File.ReadAllText(file);
                    Assert.That(text, Does.Not.Contain("TrySellMineral"), file);
                    Assert.That(text, Does.Not.Contain("TryReduceMany"), file);
                    Assert.That(text, Does.Not.Contain("TryPurchase"), file);
                    Assert.That(text, Does.Not.Contain("AddGold("), file);
                }
            }

            // 탐사 가드는 런타임 단일 진입점만.
            var surfacePresenter = File.ReadAllText(
                Path.Combine(surfaceDir, "SurfaceBasePresenter.cs"));
            var surfaceBinder = File.ReadAllText(
                Path.Combine(surfaceDir, "SurfaceBaseBinder.cs"));
            Assert.That(surfacePresenter, Does.Not.Contain("ExplorationStartGuard"));
            Assert.That(surfaceBinder, Does.Contain("TryStartExploration"));
            Assert.That(surfaceBinder, Does.Not.Contain("new ExplorationStartGuard"));

            // 기존 서비스 경로 파일은 유지
            Assert.That(
                File.Exists(Path.Combine(
                    Application.dataPath, "_Project", "Scripts", "App", "Economy", "EconomyService.cs")),
                Is.True);
            Assert.That(
                File.Exists(Path.Combine(
                    Application.dataPath, "_Project", "Scripts", "App", "Progression", "ProgressionService.cs")),
                Is.True);
        }

        [Test]
        public void L_S04_OverwriteAndDeletePaths_RequireConfirmedSlot()
        {
            var gatePath = Path.Combine(
                Application.dataPath,
                "_Project",
                "Scripts",
                "App",
                "UI",
                "MainMenu",
                "NewGameOverwriteGate.cs");
            var runtimePath = Path.Combine(
                Application.dataPath,
                "_Project",
                "Scripts",
                "App",
                "Save",
                "SaveRuntimeController.cs");
            var gate = File.ReadAllText(gatePath);
            var runtime = File.ReadAllText(runtimePath);
            Assert.That(gate, Does.Contain("AwaitingOverwriteConfirm"));
            Assert.That(gate, Does.Contain("CancelOverwrite"));
            Assert.That(runtime, Does.Contain("confirmOverwrite"));
            Assert.That(runtime, Does.Contain("RequiresOverwriteConfirm"));
            Assert.That(runtime, Does.Contain("SceneNames.SurfaceBase"));
        }

        [Test]
        public void Prompt16_MainMenuAndOverwriteDialog_AreCenteredAndEnlarged()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PhaseLMenuSceneBuilder.MainMenuPrefabPath);
            var content = prefab.transform.Find("MenuContent") as RectTransform;
            Assert.That(content, Is.Not.Null);
            Assert.That(content.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(content.sizeDelta.x, Is.GreaterThanOrEqualTo(900f));
            Assert.That(content.sizeDelta.y, Is.GreaterThanOrEqualTo(780f));

            var overwrite = prefab.transform.Find("OverwriteConfirm") as RectTransform;
            Assert.That(overwrite, Is.Not.Null);
            Assert.That(overwrite.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(overwrite.sizeDelta.x, Is.EqualTo(624f).Within(0.1f));
            Assert.That(overwrite.sizeDelta.y, Is.EqualTo(286f).Within(0.1f));

            var message = overwrite.Find("OverwriteMessage").GetComponent<TMP_Text>();
            var yes = overwrite.Find("OverwriteYes").GetComponent<RectTransform>();
            var no = overwrite.Find("OverwriteNo").GetComponent<RectTransform>();
            Assert.That(message.fontSize, Is.EqualTo(23.4f).Within(0.1f));
            Assert.That(message.rectTransform.sizeDelta, Is.EqualTo(new Vector2(546f, 78f)));
            Assert.That(message.rectTransform.anchoredPosition.y, Is.EqualTo(28.6f).Within(0.1f));
            Assert.That(yes.sizeDelta, Is.EqualTo(new Vector2(182f, 57.2f)));
            Assert.That(no.sizeDelta, Is.EqualTo(new Vector2(182f, 57.2f)));
            Assert.That(yes.anchoredPosition.y, Is.EqualTo(-28.6f).Within(0.1f));
            Assert.That(no.anchoredPosition.y, Is.EqualTo(-28.6f).Within(0.1f));
        }

        [Test]
        public void Prompt16_SurfaceBaseInformation_IsOneCenteredGroup()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PhaseLMenuSceneBuilder.SurfaceBasePrefabPath);
            var content = prefab.transform.Find("SurfaceBaseContent") as RectTransform;
            Assert.That(content, Is.Not.Null);
            Assert.That(content.anchorMin, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(content.anchorMax, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(content.anchoredPosition, Is.EqualTo(Vector2.zero));

            var centeredPaths = new[]
            {
                "EnergyText",
                "GoalsText",
                "DeepZoneText",
                "RecentRunText",
                "EconomyPanel/SellModalCard/EcoStatus",
                "ProgressionPanel/UpgradeList",
                "ProgressionPanel/ProgDeep"
            };
            // prompt-B 31-1: 새로고침 제거, 설정·종료 추가.
            Assert.That(content.Find("RefreshButton"), Is.Null);
            Assert.That(content.Find("SettingsButton"), Is.Not.Null);
            Assert.That(content.Find("QuitButton"), Is.Not.Null);
            var explore = content.Find("ExploreButton") as RectTransform;
            Assert.That(explore, Is.Not.Null);
            Assert.That(explore.anchoredPosition.x, Is.EqualTo(-160f).Within(0.1f));
            foreach (var path in centeredPaths)
            {
                var rect = content.Find(path) as RectTransform;
                Assert.That(rect, Is.Not.Null, path);
                Assert.That(rect.anchoredPosition.x, Is.EqualTo(0f).Within(0.1f), path);
            }
        }

        [Test]
        public void Prompt16_IntegrationTerrain_IsReadableBoundedAndExplained()
        {
            var scene = EditorSceneManager.OpenScene(
                PhaseOUiPolishBuilder.IntegrationScenePath,
                OpenSceneMode.Additive);
            try
            {
                var tilemap = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Tilemap>(true))
                    .FirstOrDefault(item => item.name == "ForegroundTilemap");
                Assert.That(tilemap, Is.Not.Null);
                // Phase B owns left/right edges as BoundaryRock; Phase O must not repaint them as Rock.
                var boundary = AssetDatabase.LoadAssetAtPath<TileBase>(
                    "Assets/_Project/Tilemaps/DemoWorld/BoundaryRock.asset");
                Assert.That(boundary, Is.Not.Null);
                Assert.That(
                    tilemap.GetTile(new Vector3Int(-40, -2, 0)),
                    Is.SameAs(boundary));
                Assert.That(
                    tilemap.GetTile(new Vector3Int(40, -2, 0)),
                    Is.SameAs(boundary));
                Assert.That(
                    tilemap.GetTile(new Vector3Int(-40, 5, 0)),
                    Is.SameAs(boundary));
                Assert.That(
                    tilemap.GetTile(new Vector3Int(40, 5, 0)),
                    Is.SameAs(boundary));
                Assert.That(
                    tilemap.GetSprite(new Vector3Int(0, -2, 0)).bounds.size.x,
                    Is.EqualTo(1f).Within(0.01f));

                var terrainCollider = tilemap.GetComponent<TilemapCollider2D>();
                Assert.That(terrainCollider, Is.Not.Null);
                Assert.That(terrainCollider.enabled, Is.True);
                Assert.That(
                    terrainCollider.compositeOperation,
                    Is.EqualTo(Collider2D.CompositeOperation.None));
                Assert.That(tilemap.GetTile(new Vector3Int(0, -41, 0)), Is.Not.Null);

                var legend = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<TMP_Text>(true))
                    .FirstOrDefault(item => item.name == "LegendText");
                Assert.That(legend, Is.Not.Null);
                var legendPanel = legend.transform.parent.GetComponent<RectTransform>();
                Assert.That(legendPanel.anchorMin, Is.EqualTo(new Vector2(0.5f, 0f)));
                Assert.That(legendPanel.anchorMax, Is.EqualTo(new Vector2(0.5f, 0f)));
                Assert.That(legendPanel.anchoredPosition.x, Is.EqualTo(0f));
                Assert.That(legend.text, Does.Contain("[##] 암반"));
                Assert.That(legend.text, Does.Contain("(Cu) 구리"));
                Assert.That(legend.text, Does.Contain("~~~ 가스"));
                Assert.That(legend.text, Does.Contain("봉인 신호"));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }
    }
}
