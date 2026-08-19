using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.UI.HUD;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Tests.UI
{
    /// <summary>C-S01~S04 정적 완전성: 타입·소스 경로·Update 폴링·구독 대칭.</summary>
    public sealed class HudStaticStructureTests
    {
        [Test]
        public void C_S01_ProcessB_TenHudBindingPoints_ExistOnContracts()
        {
            var viewMethods = typeof(IHudView).GetMethods().Select(m => m.Name).ToArray();
            Assert.That(viewMethods, Does.Contain("SetEnergy"));
            Assert.That(viewMethods, Does.Contain("SetDepth"));
            Assert.That(viewMethods, Does.Contain("SetGold"));
            Assert.That(viewMethods, Does.Contain("SetCargo"));
            Assert.That(viewMethods, Does.Contain("SetUnsettledValue"));
            Assert.That(viewMethods, Does.Contain("SetStructuralRisk"));
            Assert.That(viewMethods, Does.Contain("SetGasRisk"));
            Assert.That(viewMethods, Does.Contain("SetGasWarningVisible"));
            Assert.That(viewMethods, Does.Contain("SetBuildingSelection"));
            Assert.That(viewMethods, Does.Contain("SetInteractionPrompt"));
        }

        [Test]
        public void C_S02_HudSources_DoNotPollTextInUpdate()
        {
            var roots = new[]
            {
                Path.Combine(Application.dataPath, "_Project", "Scripts", "App", "UI", "HUD"),
            };

            foreach (var root in roots)
            {
                Assert.That(Directory.Exists(root), Is.True, "HUD source folder missing: " + root);
                foreach (var file in Directory.GetFiles(root, "*.cs"))
                {
                    if (Path.GetFileName(file) == "HudPanelChromeController.cs")
                    {
                        // 패널 단축키/월드 클릭 입력만 처리하며 HUD Text 바인딩과 무관하다.
                        continue;
                    }

                    var text = File.ReadAllText(file);
                    // Update 루프에서 전체 Text 재설정 패턴이 없어야 한다.
                    Assert.That(text, Does.Not.Contain("void Update("));
                    Assert.That(text, Does.Not.Contain("void Update ()"));
                }
            }
        }

        [Test]
        public void C_S03_ViewTypes_DoNotExposeGameStateMutation()
        {
            var viewTypes = new[]
            {
                typeof(BasicHudView),
                typeof(StructuralHudView),
                typeof(GasWarningPanelView),
                typeof(CompositeHudView),
                typeof(HudBinder)
            };

            var forbidden = new[]
            {
                "AddGold", "SetCurrentEnergy", "SetCargoWeight", "SetUnsettledValue",
                "SetEnergy", "SetDepth", "SetGold", "SetInventory", "SetStructuralRisk",
                "SetGasExposure", "SetBuildingSelection", "SetInteractionPrompt"
            };

            foreach (var type in viewTypes)
            {
                foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    // View의 Set*(string) 표시 API는 허용. GameState 의도 변경 시그니처만 금지.
                    var parameters = method.GetParameters();
                    var takesGameState = parameters.Any(p => p.ParameterType == typeof(SubTerra.App.State.GameState));
                    if (method.Name == "BindTo" && takesGameState)
                    {
                        continue;
                    }

                    if (takesGameState && forbidden.Contains(method.Name))
                    {
                        Assert.Fail(type.Name + "." + method.Name + " must not mutate GameState.");
                    }

                    // 의도 변경 전용 이름(숫자/enum 기반) 금지 — 표시용 string 1인자는 허용.
                    if (method.Name == "AddGold" || method.Name == "SetCurrentEnergy" ||
                        method.Name == "SetCargoWeight" || method.Name == "SetGasExposure")
                    {
                        Assert.Fail(type.Name + " must not expose " + method.Name);
                    }

                    if (parameters.Length == 1 &&
                        parameters[0].ParameterType.Namespace == "SubTerra.App.State" &&
                        parameters[0].ParameterType != typeof(SubTerra.App.State.GameState))
                    {
                        Assert.Fail(type.Name + "." + method.Name + " should not take State enums/models for mutation.");
                    }
                }
            }
        }

        [Test]
        public void C_S04_HudBinder_HasSymmetricEnableDisableLifecycle()
        {
            var binder = typeof(HudBinder);
            Assert.That(binder.GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
            Assert.That(binder.GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);

            var presenter = typeof(HudPresenter);
            Assert.That(presenter.GetMethod("Bind"), Is.Not.Null);
            Assert.That(presenter.GetMethod("Unbind"), Is.Not.Null);
        }

        [Test]
        public void RequiredHudScriptFiles_ExistUnderAppOwnership()
        {
            var hudDir = Path.Combine(Application.dataPath, "_Project", "Scripts", "App", "UI", "HUD");
            var required = new[]
            {
                "HudFormatter.cs",
                "IHudView.cs",
                "HudPresenter.cs",
                "BasicHudView.cs",
                "StructuralHudView.cs",
                "GasWarningPanelView.cs",
                "CompositeHudView.cs",
                "HudBinder.cs"
            };

            foreach (var name in required)
            {
                Assert.That(File.Exists(Path.Combine(hudDir, name)), Is.True, name);
            }
        }

        [Test]
        public void PromptB55_1_HealthRowMatchesOtherHudTextAlignmentAndSpacing()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/BasicHUD.prefab");
            Assert.That(prefab, Is.Not.Null);

            var instance = Object.Instantiate(prefab);
            try
            {
                var view = instance.GetComponent<BasicHudView>();
                Assert.That(view, Is.Not.Null);
                view.AlignHealthRow();
                RectTransform health = view.HealthText.rectTransform;
                RectTransform energy = view.EnergyText.rectTransform;

                Assert.That(health.anchoredPosition.x, Is.EqualTo(energy.anchoredPosition.x).Within(0.001f));
                Assert.That(health.sizeDelta.y, Is.EqualTo(energy.sizeDelta.y).Within(0.001f));
                Assert.That(
                    health.anchoredPosition.y - energy.anchoredPosition.y,
                    Is.EqualTo(energy.rect.height).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }
    }
}
