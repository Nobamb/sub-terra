using System;
using System.Collections.Generic;
using SubTerra.Gameplay.Player;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Editor
{
    /// <summary>#119 엘리베이터 위 홀로그램 안내(패널·문구·청록 사각 파티클)를 StartElevator 프리팹에 구성한다.</summary>
    public static class ElevatorHologramPrefabSetup
    {
        public const string HologramName = "ElevatorHologram";
        public const int ParticleCount = 16;

        private const string SolidArtPath =
            "Assets/_Project/Art/Facilities/MVP/elevator_visual_white.png";
        private const string FontPath = "Assets/_Project/Fonts/NotoSansKR-Regular_SDF.asset";

        private const float PanelWidth = 2.6f;
        private const float PanelHeight = 0.8f;
        private static readonly Vector3 AnchorPosition = new(0f, 1.5f, 0f);

        private static readonly Color FillColor = new(0.02f, 0.10f, 0.12f, 0.74f);
        private static readonly Color GlowColor = new(0.10f, 0.85f, 0.85f, 0.10f);
        private static readonly Color MetalColor = new(0.36f, 0.44f, 0.48f, 0.95f);
        private static readonly Color TealLineColor = new(0.25f, 0.95f, 0.92f, 0.90f);
        private static readonly Color AccentColor = new(0.55f, 1f, 0.97f, 1f);
        private static readonly Color LabelColor = new(0.82f, 1f, 0.98f, 1f);

        [MenuItem("SubTerra/MVP2/Build Elevator Hologram Prompt (B-119)")]
        public static void RefreshExistingPrefab()
        {
            string path = PhaseCElevatorLadderBuilder.ElevatorPrefabPath;
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Configure(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        internal static void Configure(GameObject root)
        {
            Sprite solid = AssetDatabase.LoadAssetAtPath<Sprite>(SolidArtPath);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (solid == null) throw new InvalidOperationException("Missing sprite: " + SolidArtPath);
            if (font == null) throw new InvalidOperationException("Missing font: " + FontPath);

            ElevatorController controller = root.GetComponent<ElevatorController>();
            if (controller == null) throw new InvalidOperationException("ElevatorController missing.");

            Transform anchor = Child(root.transform, HologramName);
            anchor.localPosition = AnchorPosition;
            anchor.localRotation = Quaternion.identity;
            anchor.localScale = Vector3.one;

            Transform panel = Child(anchor, "Panel");
            panel.localPosition = Vector3.zero;
            panel.localScale = Vector3.one;
            BuildPanel(panel, solid);
            TextMeshPro label = BuildLabel(panel, font);

            Transform particleRoot = Child(anchor, "Particles");
            particleRoot.localPosition = Vector3.zero;
            particleRoot.localScale = Vector3.one;
            var particles = new List<SpriteRenderer>();
            for (int i = 0; i < ParticleCount; i++)
            {
                Transform square = Child(particleRoot, "Square" + i.ToString("00"));
                SpriteRenderer renderer = Renderer(square, solid, new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0f), 45);
                square.localScale = new Vector3(0.06f, 0.06f, 1f);
                renderer.enabled = false;
                particles.Add(renderer);
            }

            ElevatorHologramPrompt prompt = anchor.GetComponent<ElevatorHologramPrompt>();
            if (prompt == null) prompt = anchor.gameObject.AddComponent<ElevatorHologramPrompt>();
            var serialized = new SerializedObject(prompt);
            serialized.FindProperty("elevator").objectReferenceValue = controller;
            serialized.FindProperty("panel").objectReferenceValue = panel;
            serialized.FindProperty("label").objectReferenceValue = label;
            AssignArray(serialized.FindProperty("panelSprites"),
                panel.GetComponentsInChildren<SpriteRenderer>(true));
            AssignArray(serialized.FindProperty("particles"), particles.ToArray());
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // 기본 상태는 전부 비활성. 근접 시 컴포넌트가 켠다.
            panel.gameObject.SetActive(false);

            // 개발자용 상태 문구는 기본 off. 오브젝트는 남겨 씬의 기존 프리팹 오버라이드를 유지한다.
            var controllerSerialized = new SerializedObject(controller);
            controllerSerialized.FindProperty("showDebugStatus").boolValue = false;
            SerializedProperty status = controllerSerialized.FindProperty("statusText");
            if (status.objectReferenceValue is TMP_Text statusText)
            {
                statusText.text = string.Empty;
                statusText.gameObject.SetActive(false);
            }
            controllerSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildPanel(Transform panel, Sprite solid)
        {
            float w = PanelWidth;
            float h = PanelHeight;
            Part(panel, "Glow", solid, new Vector2(0f, h / 2f), new Vector2(w + 0.16f, h + 0.16f), GlowColor, 40);
            Part(panel, "Fill", solid, new Vector2(0f, h / 2f), new Vector2(w, h), FillColor, 41);

            // 금속 외곽 프레임
            Part(panel, "MetalTop", solid, new Vector2(0f, h - 0.02f), new Vector2(w, 0.04f), MetalColor, 42);
            Part(panel, "MetalBottom", solid, new Vector2(0f, 0.02f), new Vector2(w, 0.04f), MetalColor, 42);
            Part(panel, "MetalLeft", solid, new Vector2(-w / 2f + 0.02f, h / 2f), new Vector2(0.04f, h), MetalColor, 42);
            Part(panel, "MetalRight", solid, new Vector2(w / 2f - 0.02f, h / 2f), new Vector2(0.04f, h), MetalColor, 42);

            // 청록 안쪽 라인
            Part(panel, "TealTop", solid, new Vector2(0f, h - 0.055f), new Vector2(w - 0.1f, 0.012f), TealLineColor, 43);
            Part(panel, "TealBottom", solid, new Vector2(0f, 0.055f), new Vector2(w - 0.1f, 0.012f), TealLineColor, 43);
            Part(panel, "TealLeft", solid, new Vector2(-w / 2f + 0.055f, h / 2f), new Vector2(0.012f, h - 0.1f), TealLineColor, 43);
            Part(panel, "TealRight", solid, new Vector2(w / 2f - 0.055f, h / 2f), new Vector2(0.012f, h - 0.1f), TealLineColor, 43);

            // 모서리 브래킷
            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sy = -1; sy <= 1; sy += 2)
                {
                    string tag = (sx < 0 ? "L" : "R") + (sy < 0 ? "B" : "T");
                    float edgeY = sy > 0 ? h - 0.0175f : 0.0175f;
                    float armY = sy > 0 ? h - 0.11f : 0.11f;
                    Part(panel, "CornerH_" + tag, solid, new Vector2(sx * (w / 2f - 0.11f), edgeY),
                        new Vector2(0.22f, 0.035f), AccentColor, 44);
                    Part(panel, "CornerV_" + tag, solid, new Vector2(sx * (w / 2f - 0.0175f), armY),
                        new Vector2(0.035f, 0.22f), AccentColor, 44);
                }
            }

            // 엘리베이터에서 켜지는 투사선
            Part(panel, "ProjectorLine", solid, new Vector2(0f, 0f), new Vector2(w * 0.8f, 0.03f), AccentColor, 44);
        }

        private static TextMeshPro BuildLabel(Transform panel, TMP_FontAsset font)
        {
            Transform labelTransform = Child(panel, "Label");
            TextMeshPro label = labelTransform.GetComponent<TextMeshPro>();
            if (label == null) label = labelTransform.gameObject.AddComponent<TextMeshPro>();
            // TextMeshPro 추가 시 Transform이 RectTransform으로 교체되므로 참조를 다시 얻는다.
            labelTransform = label.transform;
            labelTransform.localPosition = new Vector3(0f, PanelHeight / 2f, 0f);
            labelTransform.localScale = Vector3.one;
            label.font = font;
            label.fontSharedMaterial = font.material;
            label.text = ElevatorPromptResolver.ReturnLabel;
            label.fontSize = 2.6f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 1.6f;
            label.fontSizeMax = 2.6f;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.color = LabelColor;
            label.sortingOrder = 46;
            label.rectTransform.sizeDelta = new Vector2(PanelWidth - 0.3f, PanelHeight - 0.2f);
            return label;
        }

        private static void Part(
            Transform parent, string name, Sprite sprite, Vector2 center, Vector2 size, Color color, int order)
        {
            Transform part = Child(parent, name);
            part.localPosition = new Vector3(center.x, center.y, 0f);
            part.localScale = new Vector3(size.x, size.y, 1f);
            Renderer(part, sprite, color, order);
        }

        private static SpriteRenderer Renderer(Transform target, Sprite sprite, Color color, int order)
        {
            SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = target.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            renderer.drawMode = SpriteDrawMode.Simple;
            return renderer;
        }

        private static Transform Child(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) return child;
            var created = new GameObject(name).transform;
            created.SetParent(parent, false);
            return created;
        }

        private static void AssignArray(SerializedProperty array, UnityEngine.Object[] values)
        {
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }
    }
}
