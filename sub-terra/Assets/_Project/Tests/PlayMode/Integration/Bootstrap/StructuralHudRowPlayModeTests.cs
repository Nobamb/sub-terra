using System.Collections;
using NUnit.Framework;
using SubTerra.App.State;
using SubTerra.App.UI.HUD;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SubTerra.App.Tests
{
    /// <summary>Prompt-B 131: 구조 상태 행의 재활성 복구와 위험 상태 펄스 동작.</summary>
    public sealed class StructuralHudRowPlayModeTests
    {
        private GameObject root;
        private StructuralHudView view;
        private Image glow;
        private Image icon;
        private Sprite[] sprites;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("StructuralRowTest", typeof(RectTransform));
            var glowGo = new GameObject("Glow", typeof(RectTransform), typeof(Image));
            glowGo.transform.SetParent(root.transform, false);
            glow = glowGo.GetComponent<Image>();
            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(root.transform, false);
            icon = iconGo.GetComponent<Image>();
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(root.transform, false);
            var label = labelGo.AddComponent<TextMeshProUGUI>();

            sprites = new Sprite[4];
            for (var i = 0; i < sprites.Length; i++)
            {
                var texture = new Texture2D(2, 2);
                sprites[i] = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero);
            }

            root.SetActive(false);
            var pulse = root.AddComponent<StructuralStatusPulse>();
            pulse.enabled = false;
            view = root.AddComponent<StructuralHudView>();
            Set("structuralRiskText", label);
            Set("statusIcon", icon);
            Set("glowImage", glow);
            Set("pulse", pulse);
            Set("stateIcons", sprites);
            root.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(root);
        }

        private void Set(string field, object value)
        {
            typeof(StructuralHudView)
                .GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(view, value);
        }

        [UnityTest]
        public IEnumerator SafeState_NeverPulsesOrGlows()
        {
            view.SetStructuralRisk("구조 안전", StructuralStatusKind.Safe);
            yield return null;
            yield return null;

            Assert.That(view.IsPulsing, Is.False);
            Assert.That(glow.color.a, Is.Zero);
            Assert.That(icon.sprite, Is.EqualTo(sprites[0]));
        }

        [UnityTest]
        public IEnumerator CriticalState_PulsesGlowWithinWeakLimit_ThenStopsWhenSafe()
        {
            view.SetStructuralRisk("구조 위험", StructuralStatusKind.Critical);
            var limit = StructuralStatusPresentation.Resolve(StructuralStatusKind.Critical).GlowColor.a;
            Assert.That(view.IsPulsing, Is.True);
            Assert.That(icon.sprite, Is.EqualTo(sprites[2]));

            var max = 0f;
            var min = 1f;
            var start = Time.unscaledTime;
            while (Time.unscaledTime - start < StructuralStatusPresentation.PulsePeriodSeconds + 0.2f)
            {
                max = Mathf.Max(max, glow.color.a);
                min = Mathf.Min(min, glow.color.a);
                yield return null;
            }

            Assert.That(max, Is.LessThanOrEqualTo(limit + 0.0001f), "발광은 최대 알파를 넘지 않는다");
            Assert.That(max, Is.GreaterThan(limit * 0.5f), "펄스가 실제로 동작");
            Assert.That(min, Is.LessThan(limit * 0.5f), "밝기가 오르내린다");

            view.SetStructuralRisk("구조 안전", StructuralStatusKind.Safe);
            yield return null;
            Assert.That(view.IsPulsing, Is.False);
            Assert.That(glow.color.a, Is.Zero);
        }

        [UnityTest]
        public IEnumerator DisableThenEnable_RestoresLastStateAndPulse()
        {
            view.SetStructuralRisk("구조 붕괴 임박", StructuralStatusKind.Imminent);
            root.SetActive(false);
            yield return null;
            root.SetActive(true);
            yield return null;

            Assert.That(view.CurrentKind, Is.EqualTo(StructuralStatusKind.Imminent));
            Assert.That(view.StructuralRiskText.text, Is.EqualTo("구조 붕괴 임박"));
            Assert.That(icon.sprite, Is.EqualTo(sprites[3]));
            Assert.That(view.IsPulsing, Is.True);

            // 비활성 중에 바뀐 상태도 재활성 시 올바르게 보인다.
            root.SetActive(false);
            view.SetStructuralRisk("구조 안전", StructuralStatusKind.Safe);
            root.SetActive(true);
            yield return null;
            Assert.That(view.IsPulsing, Is.False);
            Assert.That(icon.sprite, Is.EqualTo(sprites[0]));
            Assert.That(glow.color.a, Is.Zero);
        }
    }
}
