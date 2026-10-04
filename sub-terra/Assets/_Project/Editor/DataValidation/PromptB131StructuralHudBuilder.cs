using System.Collections.Generic;
using System.IO;
using System.Linq;
using SubTerra.App.UI.HUD;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// Prompt-B 131: HUD 하단 '전력·연결·활성' 줄을 구조 상태 행으로 교체한다.
    /// 대상은 BasicHUD.prefab, HUDCanvas.prefab과 이번에 생성하는 구조 상태 아이콘 PNG뿐이다.
    /// </summary>
    public static class PromptB131StructuralHudBuilder
    {
        public const string ArtFolder = "Assets/_Project/Art/UI/Gameplay/HUD/";
        public const string BasicHudPath = "Assets/_Project/Prefabs/UI/BasicHUD.prefab";
        public const string HudCanvasPath = "Assets/_Project/Prefabs/UI/HUDCanvas.prefab";
        public const string RowName = "StructuralStatusRow";
        public const string LegacyPowerTextName = "PowerConnectionText";

        public static readonly string[] IconFiles =
        {
            "structural-safe", "structural-caution", "structural-critical", "structural-imminent"
        };
        public const string GlowFile = "structural-glow";

        // 행 배치(HUD 좌상단 기준 px). 하단 구분선(y=196)과 프레임 하단 테두리 사이에 둔다.
        public const float RowX = 18f, RowY = 199f, RowWidth = 394f, RowHeight = 32f;
        public const float IconX = 4f, IconY = 2f, IconSize = 28f;
        public const float LabelX = 42f, LabelWidth = 340f;

        private static readonly Vector2[] Rock =
        {
            new Vector2(9, 27), new Vector2(19, 10), new Vector2(41, 7), new Vector2(55, 18),
            new Vector2(57, 40), new Vector2(47, 56), new Vector2(22, 57), new Vector2(8, 45)
        };

        [MenuItem("SubTerra/UI/Build Prompt-B 131 Structural Status HUD")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                throw new System.InvalidOperationException("Stop Play Mode first.");
            }

            GenerateArt();
            BuildBasicHud();
            RewireHudCanvas();
            AssetDatabase.SaveAssets();
        }

        // ---------- 프리팹 ----------

        private static void BuildBasicHud()
        {
            var icons = IconFiles.Select(f => AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + f + ".png")).ToArray();
            var glowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + GlowFile + ".png");
            var root = PrefabUtility.LoadPrefabContents(BasicHudPath);
            try
            {
                var basic = root.GetComponent<BasicHudView>();
                // 기존 전력·연결·활성 아이콘(7번째 HUD 아이콘)을 제거한다.
                var legacyIcon = root.transform.Find("HudIcon6");
                if (legacyIcon != null)
                {
                    Object.DestroyImmediate(legacyIcon.gameObject);
                }

                var oldRow = root.transform.Find(RowName);
                if (oldRow != null)
                {
                    Object.DestroyImmediate(oldRow.gameObject);
                }

                var row = NewRect(root.transform, RowName);
                Place(row, RowX, RowY, RowWidth, RowHeight);

                var glow = NewRect(row, "Glow");
                glow.anchorMin = Vector2.zero;
                glow.anchorMax = Vector2.one;
                glow.offsetMin = glow.offsetMax = Vector2.zero;
                var glowImage = glow.gameObject.AddComponent<Image>();
                glowImage.sprite = glowSprite;
                glowImage.color = new Color(1f, 0.2f, 0.16f, 0f);
                glowImage.raycastTarget = false;

                var icon = NewRect(row, "Icon");
                Place(icon, IconX, IconY, IconSize, IconSize);
                var iconImage = icon.gameObject.AddComponent<Image>();
                iconImage.sprite = icons[0];
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;

                var label = NewRect(row, "Label");
                Place(label, LabelX, 0f, LabelWidth, RowHeight);
                var text = label.gameObject.AddComponent<TextMeshProUGUI>();
                text.font = basic.DepthText.font;
                text.fontSharedMaterial = basic.DepthText.fontSharedMaterial;
                text.fontSize = 21;
                text.enableAutoSizing = false;
                text.alignment = TextAlignmentOptions.MidlineLeft;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.overflowMode = TextOverflowModes.Overflow;
                text.color = Color.white;
                text.raycastTarget = false;
                text.text = "구조 안전";

                var pulse = row.gameObject.AddComponent<StructuralStatusPulse>();
                pulse.enabled = false;
                var view = row.gameObject.AddComponent<StructuralHudView>();
                var so = new SerializedObject(view);
                so.FindProperty("structuralRiskText").objectReferenceValue = text;
                so.FindProperty("statusIcon").objectReferenceValue = iconImage;
                so.FindProperty("glowImage").objectReferenceValue = glowImage;
                so.FindProperty("pulse").objectReferenceValue = pulse;
                var array = so.FindProperty("stateIcons");
                array.arraySize = icons.Length;
                for (var i = 0; i < icons.Length; i++)
                {
                    array.GetArrayElementAtIndex(i).objectReferenceValue = icons[i];
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, BasicHudPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void RewireHudCanvas()
        {
            var root = PrefabUtility.LoadPrefabContents(HudCanvasPath);
            try
            {
                var binder = root.GetComponent<HudBinder>();
                var views = root.GetComponentsInChildren<StructuralHudView>(true);
                var rowView = views.Single(v => v.GetComponentInParent<BasicHudView>() != null);
                foreach (var legacy in views.Where(v => v != rowView))
                {
                    // 우측 상단에 떠 있던 StructuralHUD 인스턴스를 통째로 제거한다.
                    var instance = PrefabUtility.GetOutermostPrefabInstanceRoot(legacy.gameObject);
                    Object.DestroyImmediate(instance != null ? instance : legacy.gameObject);
                }

                var legacyPower = root.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(t => t.name == LegacyPowerTextName);
                if (legacyPower != null)
                {
                    Object.DestroyImmediate(legacyPower.gameObject);
                }

                var so = new SerializedObject(binder);
                so.FindProperty("structuralHud").objectReferenceValue = rowView;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, HudCanvasPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static RectTransform NewRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void Place(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
        }

        // ---------- 아이콘 아트(절차 생성) ----------

        private static void GenerateArt()
        {
            var accents = new[]
            {
                new Color(0.30f, 0.76f, 0.80f),   // 안전: 은은한 청록
                new Color(1f, 0.78f, 0.25f),      // 주의
                new Color(1f, 0.30f, 0.24f),      // 위험
                new Color(1f, 0.12f, 0.10f)       // 붕괴 임박
            };
            for (var i = 0; i < IconFiles.Length; i++)
            {
                WritePng(IconFiles[i], RenderIcon(i, accents[i]));
            }

            WritePng(GlowFile, RenderGlow());
            AssetDatabase.Refresh();
            foreach (var file in IconFiles.Concat(new[] { GlowFile }))
            {
                var path = ArtFolder + file + ".png";
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        private static void WritePng(string file, Texture2D texture)
        {
            var absolute = Path.Combine(Directory.GetCurrentDirectory(), ArtFolder + file + ".png");
            File.WriteAllBytes(absolute, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        private static Texture2D RenderGlow()
        {
            const int w = 64, h = 16;
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    var u = (x + 0.5f) / w;
                    var v = (y + 0.5f) / h;
                    var ax = Mathf.SmoothStep(0f, 1f, Mathf.Min(u, 1f - u) / 0.35f);
                    var ay = Mathf.SmoothStep(0f, 1f, Mathf.Min(v, 1f - v) / 0.5f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, ax * ay));
                }
            }

            texture.Apply();
            return texture;
        }

        private static Texture2D RenderIcon(int kind, Color accent)
        {
            const int n = 64, ss = 4;
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, false);
            for (var py = 0; py < n; py++)
            {
                for (var px = 0; px < n; px++)
                {
                    float r = 0, g = 0, b = 0, a = 0;
                    for (var sy = 0; sy < ss; sy++)
                    {
                        for (var sx = 0; sx < ss; sx++)
                        {
                            var c = Shade(new Vector2(px + (sx + 0.5f) / ss, py + (sy + 0.5f) / ss), kind, accent);
                            r += c.r * c.a; g += c.g * c.a; b += c.b * c.a; a += c.a;
                        }
                    }

                    var total = ss * ss;
                    var alpha = a / total;
                    var color = a > 0f ? new Color(r / a, g / a, b / a, alpha) : Color.clear;
                    texture.SetPixel(px, n - 1 - py, color);
                }
            }

            texture.Apply();
            return texture;
        }

        private static Color Shade(Vector2 p, int kind, Color accent)
        {
            if (!InPolygon(p, Rock))
            {
                return Color.clear;
            }

            if (kind == 3 && DistToPolyline(p, ImminentGap) < 2.7f)
            {
                return Color.clear;
            }

            var detail = DetailLines(kind).Any(line => DistToPolyline(p, line) < 2.1f);
            if (detail)
            {
                return Color.Lerp(accent, Color.white, 0.25f);
            }

            if (MinEdgeDistance(p, Rock) < 3.4f)
            {
                return accent;
            }

            // 어두운 남색 암석면. 위쪽을 살짝 밝게 해 HUD 금속 아이콘과 톤을 맞춘다.
            return Color.Lerp(new Color(0.15f, 0.22f, 0.30f), new Color(0.07f, 0.11f, 0.16f), p.y / 64f);
        }

        private static readonly Vector2[] ImminentGap =
        {
            new Vector2(36, 6), new Vector2(30, 22), new Vector2(38, 34), new Vector2(30, 47), new Vector2(35, 58)
        };

        private static IEnumerable<Vector2[]> DetailLines(int kind)
        {
            var mainCrack = new[]
            {
                new Vector2(34, 14), new Vector2(29, 26), new Vector2(37, 35), new Vector2(31, 48)
            };
            switch (kind)
            {
                case 0:
                    yield return new[] { new Vector2(21, 33), new Vector2(29, 42), new Vector2(45, 24) };
                    break;
                case 1:
                    yield return mainCrack;
                    break;
                case 2:
                    yield return mainCrack;
                    yield return new[] { new Vector2(29, 26), new Vector2(18, 30) };
                    yield return new[] { new Vector2(37, 35), new Vector2(50, 38) };
                    yield return new[] { new Vector2(31, 48), new Vector2(37, 55) };
                    break;
            }
        }

        private static bool InPolygon(Vector2 p, Vector2[] poly)
        {
            var inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if ((poly[i].y > p.y) != (poly[j].y > p.y)
                    && p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private static float MinEdgeDistance(Vector2 p, Vector2[] poly)
        {
            var min = float.MaxValue;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                min = Mathf.Min(min, DistToSegment(p, poly[j], poly[i]));
            }

            return min;
        }

        private static float DistToPolyline(Vector2 p, Vector2[] line)
        {
            var min = float.MaxValue;
            for (var i = 0; i < line.Length - 1; i++)
            {
                min = Mathf.Min(min, DistToSegment(p, line[i], line[i + 1]));
            }

            return min;
        }

        private static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }
    }
}
