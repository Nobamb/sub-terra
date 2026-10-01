using System.Collections.Generic;
using System.IO;
using SubTerra.App.Core.Data;
using SubTerra.App.UI.Progression;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// prompt-B 118: 업그레이드 창을 드릴 속도 중심 트리 UI로 바꾼다.
    /// - 해금 데이터는 UpgradeData 에셋에만 쓴다(효과·비용·최대 레벨은 건드리지 않는다).
    /// - UI는 업그레이드 창이 있는 두 곳(SurfaceBasePanel.prefab, Mine_Demo_Integration.unity)에서
    ///   명시된 경로의 업그레이드 패널 하나만 수정한다. 기존 탭·목록 오브젝트는 지우지 않고 비활성으로 둔다.
    /// 다른 Prefab/Scene은 열거나 저장하지 않는다.
    /// </summary>
    public static class PromptB118UpgradeTreeBuilder
    {
        public const string SurfacePrefabPath = "Assets/_Project/Prefabs/UI/SurfaceBasePanel.prefab";
        public const string IntegrationScenePath = "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";
        public const string UpgradeDataFolder = "Assets/_Project/Data/Upgrades";
        public const string ArtFolder = "Assets/_Project/Art/UI/Upgrade/";
        public const string TreeRootName = "UpgradeTreeRoot";

        private const string SurfacePanelPath = "SurfaceBaseContent/UpgradeModal/ProgressionPanel";
        private const string HudAtlasPath = "Assets/_Project/Art/UI/Gameplay/HUD/hud-icons.png";

        public readonly struct NodeDef
        {
            public readonly string Id;
            public readonly string IconKey;
            public readonly Vector2 Position;
            public readonly string ParentId;
            public readonly int DrillLevelRequired;

            public NodeDef(string id, string iconKey, float x, float y, string parentId, int drillLevelRequired)
            {
                Id = id;
                IconKey = iconKey;
                Position = new Vector2(x, y);
                ParentId = parentId;
                DrillLevelRequired = drillLevelRequired;
            }
        }

        /// <summary>
        /// 최종 해금 조건표(항목 / 해금 조건 / 연결 대상). 부모의 해금 단계는 항상 자식보다 같거나 빠르다.
        /// 드릴 속도는 중심 노드로 처음부터 공개된다.
        /// </summary>
        public static readonly NodeDef[] Nodes =
        {
            new NodeDef(DataIds.Upgrades.DrillSpeed, "arrow", 0f, 0f, "", 0),
            new NodeDef(DataIds.Upgrades.DrillEfficiency, "plug", 0f, 205f, DataIds.Upgrades.DrillSpeed, 1),
            new NodeDef(DataIds.Upgrades.MaximumEnergy, "bolt", -225f, 0f, DataIds.Upgrades.DrillSpeed, 1),
            new NodeDef(DataIds.Upgrades.MaximumCargo, "box", 225f, 0f, DataIds.Upgrades.DrillSpeed, 1),
            new NodeDef(DataIds.Upgrades.MaximumHealth, "heart", -225f, 205f, DataIds.Upgrades.MaximumEnergy, 2),
            new NodeDef(DataIds.Upgrades.GasResistance, "gas", -225f, -205f, DataIds.Upgrades.MaximumEnergy, 2),
            new NodeDef(DataIds.Upgrades.CargoYield, "copper", 225f, 205f, DataIds.Upgrades.MaximumCargo, 2),
            new NodeDef(DataIds.Upgrades.DroneScan, "drone", 0f, -205f, DataIds.Upgrades.DrillSpeed, 2),
            new NodeDef(DataIds.Upgrades.HealthRegeneration, "reset", -450f, 205f, DataIds.Upgrades.MaximumHealth, 3),
            new NodeDef(DataIds.Upgrades.CargoGold, "coins", 450f, 0f, DataIds.Upgrades.MaximumCargo, 3),
            new NodeDef(DataIds.Upgrades.DroneRescue, "rescue", 225f, -205f, DataIds.Upgrades.DroneScan, 3)
        };

        private static readonly Vector2 NodeSize = new Vector2(128f, 136f);
        private static readonly Vector2 ContentSize = new Vector2(1060f, 560f);

        private static readonly Color PlateColor = new Color(0.035f, 0.085f, 0.105f, 1f);
        // 완전 불투명: 해금 전 이름·아이콘이 비쳐 보이지 않게 한다.
        private static readonly Color BlindColor = new Color(0.02f, 0.05f, 0.065f, 1f);
        private static readonly Color LineBase = new Color(0.1f, 0.26f, 0.3f, 0.95f);

        // ------------------------------------------------------------------ 메뉴

        [MenuItem("SubTerra/Data/Apply Prompt-B 118 Upgrade Unlock Rules")]
        public static void ApplyUnlockRulesFromMenu() => Debug.Log(ApplyUnlockRules());

        [MenuItem("SubTerra/UI/Build Prompt-B 118 Upgrade Tree (Surface Base)")]
        public static void BuildSurfaceBaseFromMenu() => Debug.Log(BuildSurfaceBase());

        [MenuItem("SubTerra/UI/Build Prompt-B 118 Upgrade Tree (Mine Integration)")]
        public static void BuildMineIntegrationFromMenu() => Debug.Log(BuildMineIntegration());

        // ------------------------------------------------------------------ 데이터

        public static string ApplyUnlockRules()
        {
            var byId = new Dictionary<string, UpgradeData>();
            var guids = AssetDatabase.FindAssets("t:UpgradeData", new[] { UpgradeDataFolder });
            for (var i = 0; i < guids.Length; i++)
            {
                var asset = AssetDatabase.LoadAssetAtPath<UpgradeData>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (asset != null && !string.IsNullOrEmpty(asset.Id))
                {
                    byId[asset.Id] = asset;
                }
            }

            var applied = 0;
            for (var i = 0; i < Nodes.Length; i++)
            {
                var def = Nodes[i];
                if (!byId.TryGetValue(def.Id, out var asset))
                {
                    return "FAIL: missing upgrade asset " + def.Id;
                }

                var requirement = def.DrillLevelRequired > 0 ? DataIds.Upgrades.DrillSpeed : string.Empty;
                asset.EditorSetUnlock(requirement, def.DrillLevelRequired, def.ParentId);
                EditorUtility.SetDirty(asset);
                applied++;
            }

            if (byId.Count != Nodes.Length)
            {
                return "FAIL: catalog has " + byId.Count + " upgrades but table has " + Nodes.Length;
            }

            AssetDatabase.SaveAssets();
            return "Prompt-B 118 unlock rules applied: " + applied;
        }

        // ------------------------------------------------------------------ 빌드 진입점

        public static string BuildSurfaceBase()
        {
            EnsureArt();
            var root = PrefabUtility.LoadPrefabContents(SurfacePrefabPath);
            try
            {
                var panel = root.transform.Find(SurfacePanelPath);
                if (panel == null)
                {
                    return "FAIL: " + SurfacePanelPath + " not found";
                }

                // 카드 크기만 키운다. 제목·닫기 버튼은 같은 오브젝트를 그대로 쓴다.
                var card = (RectTransform)panel;
                card.sizeDelta = PromptB1181UpgradeWindowBuilder.WindowSize;
                PromptB1181UpgradeWindowBuilder.ApplyWindowChrome(card, false);
                var title = panel.Find("UpgradeTitle") as RectTransform;
                if (title != null)
                {
                    SetRect(title, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(40f, -22f), new Vector2(520f, 64f));
                    var titleText = title.GetComponent<TMP_Text>();
                    if (titleText != null)
                    {
                        titleText.alignment = TextAlignmentOptions.MidlineLeft;
                    }
                }

                BuildTree(panel, false);
                PrefabUtility.SaveAsPrefabAsset(root, SurfacePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return "Prompt-B 118 upgrade tree built: SurfaceBasePanel";
        }

        public static string BuildMineIntegration()
        {
            EnsureArt();
            var previous = SceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.OpenScene(IntegrationScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                return "FAIL: open integration scene";
            }

            Transform panel = null;
            foreach (var rootObject in scene.GetRootGameObjects())
            {
                foreach (var t in rootObject.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "UpgradePanel" && t.GetComponent<ProgressionPanelView>() != null)
                    {
                        panel = t;
                        break;
                    }
                }

                if (panel != null)
                {
                    break;
                }
            }

            if (panel == null)
            {
                RestoreScene(previous);
                return "FAIL: UpgradePanel not found";
            }

            // prompt-B 118-1: 지하 창도 지상 기지 창과 같은 크기·프레임·닫기·구매 버튼으로 통일한다.
            PromptB1181UpgradeWindowBuilder.ApplyWindowChrome((RectTransform)panel, true);
            BuildTree(panel, true);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            RestoreScene(previous);
            return "Prompt-B 118 upgrade tree built: Mine_Demo_Integration UpgradePanel";
        }

        private static void RestoreScene(string previous)
        {
            if (!string.IsNullOrEmpty(previous)
                && previous != IntegrationScenePath
                && File.Exists(previous))
            {
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            }
        }

        // ------------------------------------------------------------------ 트리 구성

        private static readonly string[] LegacyChildren =
        {
            "CategoryTabBar", "EntryListRoot", "UpgradeList", "UpgradeDetail", "UpgradeResult", "DeepZone", "ProgDeep"
        };

        private static void BuildTree(Transform panel, bool addTitle)
        {
            var view = panel.GetComponent<ProgressionPanelView>();
            if (view == null)
            {
                throw new System.InvalidOperationException("ProgressionPanelView missing on " + panel.name);
            }

            var font = ResolveFont(panel);
            var plate = LoadSprite(ArtFolder + "upgrade-node-plate.png");
            var glow = LoadSprite(ArtFolder + "upgrade-glow.png");

            // 재실행 가능: 이전에 만든 트리 루트만 지운다.
            var old = panel.Find(TreeRootName);
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            for (var i = 0; i < LegacyChildren.Length; i++)
            {
                var legacy = panel.Find(LegacyChildren[i]);
                if (legacy != null)
                {
                    legacy.gameObject.SetActive(false);
                }
            }

            var treeRoot = NewRect(TreeRootName, panel);
            Stretch(treeRoot);
            treeRoot.SetAsFirstSibling();
            var treeView = treeRoot.gameObject.AddComponent<UpgradeTreeView>();

            // 레이아웃 상수 (패널 기준)
            const float margin = 24f;
            const float topInset = 96f;
            const float bottomInset = 56f;
            const float detailWidth = 430f;
            const float gap = 16f;

            // 트리 영역
            var viewport = NewRect("TreeViewport", treeRoot);
            SetRect(viewport, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            viewport.offsetMin = new Vector2(margin, bottomInset);
            viewport.offsetMax = new Vector2(-(margin + detailWidth + gap), -topInset);
            var backdrop = NewImage("TreeBackdrop", viewport, plate, new Color(0.015f, 0.04f, 0.055f, 0.7f));
            Stretch(backdrop.rectTransform);
            backdrop.type = Image.Type.Sliced;
            // prompt-B 118-1: 확대했을 때 트리가 상세 패널·프레임 밖으로 넘치지 않게 자른다.
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = NewRect("TreeContent", viewport);
            SetRect(content, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, ContentSize);
            var connectorsRoot = NewRect("Connectors", content);
            Stretch(connectorsRoot);
            var nodesRoot = NewRect("Nodes", content);
            Stretch(nodesRoot);

            var positions = new Dictionary<string, Vector2>();
            for (var i = 0; i < Nodes.Length; i++)
            {
                positions[Nodes[i].Id] = Nodes[i].Position;
            }

            var connectorList = new List<UpgradeTreeConnector>();
            for (var i = 0; i < Nodes.Length; i++)
            {
                if (!string.IsNullOrEmpty(Nodes[i].ParentId))
                {
                    connectorList.Add(BuildConnector(connectorsRoot, Nodes[i], positions[Nodes[i].ParentId]));
                }
            }

            var nodeList = new List<UpgradeTreeNodeView>();
            for (var i = 0; i < Nodes.Length; i++)
            {
                nodeList.Add(BuildNode(nodesRoot, Nodes[i], plate, glow, font));
            }

            // prompt-B 118-1: 하단 "심층 구역 · 해금됨" 한 줄은 만들지 않는다(요청으로 제거).
            var detail = BuildDetail(treeRoot, panel, font, plate, topInset, margin, detailWidth, treeView);

            if (addTitle && panel.Find("UpgradeTreeTitle") == null)
            {
                var title = NewText("UpgradeTreeTitle", panel, font, 32f, UpgradeTreeTween.TextMain, TextAlignmentOptions.MidlineLeft);
                title.text = "장비 업그레이드";
                SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(40f, -22f), new Vector2(520f, 64f));
            }

            // UpgradeTreeView 직렬화 참조 연결
            var so = new SerializedObject(treeView);
            so.FindProperty("viewport").objectReferenceValue = viewport;
            so.FindProperty("content").objectReferenceValue = content;
            so.FindProperty("contentSize").vector2Value = ContentSize;
            AssignArray(so.FindProperty("nodes"), nodeList);
            AssignArray(so.FindProperty("connectors"), connectorList);
            detail.Apply(so);
            so.FindProperty("deepZoneText").objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();

            // ProgressionPanelView가 트리 View를 사용하도록 연결
            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("treeView").objectReferenceValue = treeView;
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            // 아이콘은 노드 안의 Image.sprite로 직접 저장되어 있다. 변경 표시.
            EditorUtility.SetDirty(treeView);
            EditorUtility.SetDirty(view);
            EditorUtility.SetDirty(panel.gameObject);
        }

        private static void AssignArray<T>(SerializedProperty property, List<T> items) where T : Object
        {
            property.arraySize = items.Count;
            for (var i = 0; i < items.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
        }

        // ------------------------------------------------------------------ 연결선

        private static UpgradeTreeConnector BuildConnector(Transform parent, NodeDef child, Vector2 parentPosition)
        {
            var root = NewRect("Conn_" + child.Id.Replace('.', '_'), parent);
            Stretch(root);
            var connector = root.gameObject.AddComponent<UpgradeTreeConnector>();

            var start = parentPosition;
            var end = child.Position;
            Vector2 corner;
            if (Mathf.Approximately(start.x, end.x) || Mathf.Approximately(start.y, end.y))
            {
                corner = end;
            }
            else
            {
                // 회로형 직각 경로: 가로 먼저, 세로 나중.
                corner = new Vector2(end.x, start.y);
            }

            var dirA = (corner - start);
            var lengthA = dirA.magnitude;
            dirA = lengthA > 0.01f ? dirA / lengthA : Vector2.right;
            var dirB = (end - corner);
            var lengthB = dirB.magnitude;
            dirB = lengthB > 0.01f ? dirB / lengthB : Vector2.up;

            AddSegment(root, "BaseA", start, corner, 4f, LineBase);
            if (lengthB > 0.01f)
            {
                AddSegment(root, "BaseB", corner, end, 4f, LineBase);
            }

            var litA = AddLitSegment(root, "LitA", start, dirA, 4f);
            var litB = AddLitSegment(root, "LitB", corner, dirB, 4f);
            connector.Configure(child.ParentId, child.Id, litA, start, dirA, lengthA, litB, corner, dirB, lengthB);
            return connector;
        }

        private static void AddSegment(Transform parent, string name, Vector2 from, Vector2 to, float thickness, Color color)
        {
            var image = NewImage(name, parent, null, color);
            var delta = to - from;
            var horizontal = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y);
            SetRect(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                (from + to) * 0.5f,
                horizontal ? new Vector2(Mathf.Abs(delta.x), thickness) : new Vector2(thickness, Mathf.Abs(delta.y)));
        }

        private static RectTransform AddLitSegment(Transform parent, string name, Vector2 from, Vector2 dir, float thickness)
        {
            var color = UpgradeTreeTween.Cyan;
            color.a = 0.95f;
            var image = NewImage(name, parent, null, color);
            var horizontal = Mathf.Abs(dir.x) > 0.5f;
            SetRect(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                from, horizontal ? new Vector2(0f, thickness) : new Vector2(thickness, 0f));
            image.enabled = false;
            return image.rectTransform;
        }

        // ------------------------------------------------------------------ 노드

        private static UpgradeTreeNodeView BuildNode(Transform parent, NodeDef def, Sprite plate, Sprite glowSprite, TMP_FontAsset font)
        {
            var root = NewRect("Node_" + def.Id.Replace('.', '_'), parent);
            SetRect(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), def.Position, NodeSize);
            if (def.Id == DataIds.Upgrades.DrillSpeed)
            {
                root.localScale = new Vector3(1.18f, 1.18f, 1f);
            }

            var hit = root.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            hit.raycastTarget = true;
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;
            var nodeView = root.gameObject.AddComponent<UpgradeTreeNodeView>();
            nodeView.Configure(def.Id);

            var glow = NewImage("Glow", root, glowSprite, UpgradeTreeTween.Cyan);
            Stretch(glow.rectTransform);
            glow.rectTransform.offsetMin = new Vector2(-44f, -44f);
            glow.rectTransform.offsetMax = new Vector2(44f, 44f);
            UpgradeTreeTween.SetAlpha(glow, 0f);

            var body = NewRect("Body", root);
            Stretch(body);

            var selection = NewImage("Selection", body, plate, UpgradeTreeTween.Cyan);
            Stretch(selection.rectTransform);
            selection.rectTransform.offsetMin = new Vector2(-6f, -6f);
            selection.rectTransform.offsetMax = new Vector2(6f, 6f);
            selection.type = Image.Type.Sliced;
            selection.color = new Color(UpgradeTreeTween.Cyan.r, UpgradeTreeTween.Cyan.g, UpgradeTreeTween.Cyan.b, 0.75f);
            selection.enabled = false;

            var outline = NewImage("Outline", body, plate, UpgradeTreeTween.CyanDim);
            Stretch(outline.rectTransform);
            outline.type = Image.Type.Sliced;

            var plateImage = NewImage("Plate", body, plate, PlateColor);
            Stretch(plateImage.rectTransform);
            plateImage.rectTransform.offsetMin = new Vector2(3f, 3f);
            plateImage.rectTransform.offsetMax = new Vector2(-3f, -3f);
            plateImage.type = Image.Type.Sliced;

            var bars = new List<Image>();
            // 테두리를 따라 도는 레벨업 빛: 상 → 우 → 하 → 좌 순서.
            bars.Add(EdgeBar(body, "EdgeTop", true, 1f));
            bars.Add(EdgeBar(body, "EdgeRight", false, 1f));
            bars.Add(EdgeBar(body, "EdgeBottom", true, 0f));
            bars.Add(EdgeBar(body, "EdgeLeft", false, 0f));

            // prompt-B 118-1: 호버 빛(아이콘 뒤) → 뒤쪽 연출층 → 아이콘 → 앞쪽 연출층 순서.
            var hoverGlow = PromptB1181UpgradeWindowBuilder.AddHoverGlow(body, glowSprite);
            var fxBack = PromptB1181UpgradeWindowBuilder.AddIconLayer(body, "FxBack");

            // 회전 연출이 아이콘 중심을 기준으로 돌도록 pivot을 가운데에 둔다(위치는 이전과 같다).
            var icon = NewImage("Icon", body, LoadIcon(def.IconKey), Color.white);
            icon.preserveAspect = true;
            SetRect(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                PromptB1181UpgradeWindowBuilder.IconCenter, PromptB1181UpgradeWindowBuilder.IconSize);
            var fxFront = PromptB1181UpgradeWindowBuilder.AddIconLayer(body, "FxFront");

            var nameLabel = NewText("NameLabel", body, font, 16f, UpgradeTreeTween.TextMain, TextAlignmentOptions.Center);
            SetRect(nameLabel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(-8f, 26f));
            nameLabel.enableAutoSizing = true;
            nameLabel.fontSizeMin = 11f;
            nameLabel.fontSizeMax = 16f;
            nameLabel.textWrappingMode = TextWrappingModes.NoWrap;

            var levelLabel = NewText("LevelLabel", body, font, 15f, UpgradeTreeTween.TextDim, TextAlignmentOptions.Center);
            SetRect(levelLabel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(-8f, 22f));

            var ready = NewImage("ReadyMark", body, null, UpgradeTreeTween.Cyan);
            SetRect(ready.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-14f, -14f), new Vector2(9f, 9f));
            ready.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            ready.enabled = false;

            // 블라인드: 어두운 실루엣 + ? + 자물쇠. 독립 오브젝트이며 입력을 받지 않는다.
            var blindRect = NewRect("Blind", body);
            Stretch(blindRect);
            blindRect.offsetMin = new Vector2(3f, 3f);
            blindRect.offsetMax = new Vector2(-3f, -3f);
            var blindImage = blindRect.gameObject.AddComponent<Image>();
            blindImage.sprite = plate;
            blindImage.type = Image.Type.Sliced;
            blindImage.color = BlindColor;
            blindImage.raycastTarget = false;
            var blindGroup = blindRect.gameObject.AddComponent<CanvasGroup>();
            blindGroup.blocksRaycasts = false;
            blindGroup.interactable = false;
            var blindGlow = PromptB1181UpgradeWindowBuilder.AddHoverGlow(blindRect, glowSprite);
            blindGlow.name = "BlindHoverGlow";

            var question = NewText("Question", blindRect, font, 54f, new Color(0.27f, 0.5f, 0.56f, 1f), TextAlignmentOptions.Center);
            question.text = "?";
            SetRect(question.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 14f), new Vector2(90f, 70f));

            var lockRoot = NewRect("Lock", blindRect);
            SetRect(lockRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -36f), new Vector2(30f, 34f));
            var lockColor = new Color(0.5f, 0.62f, 0.68f, 1f);
            var lockParts = new List<Image>
            {
                LockPart(lockRoot, "LockBody", lockColor, new Vector2(0f, -6f), new Vector2(26f, 18f)),
                LockPart(lockRoot, "ShackleLeft", lockColor, new Vector2(-8f, 6f), new Vector2(5f, 16f)),
                LockPart(lockRoot, "ShackleRight", lockColor, new Vector2(8f, 6f), new Vector2(5f, 16f)),
                LockPart(lockRoot, "ShackleTop", lockColor, new Vector2(0f, 14f), new Vector2(21f, 5f))
            };

            var fragments = new List<Image>();
            for (var i = 0; i < 6; i++)
            {
                var fragment = NewImage("Fragment" + i, blindRect, null, lockColor);
                SetRect(fragment.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -36f), new Vector2(7f, 7f));
                fragment.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 25f * i);
                fragment.gameObject.SetActive(false);
                fragments.Add(fragment);
            }

            var so = new SerializedObject(nodeView);
            so.FindProperty("button").objectReferenceValue = button;
            so.FindProperty("body").objectReferenceValue = body;
            so.FindProperty("glow").objectReferenceValue = glow;
            so.FindProperty("outline").objectReferenceValue = outline;
            so.FindProperty("plate").objectReferenceValue = plateImage;
            so.FindProperty("selection").objectReferenceValue = selection;
            so.FindProperty("icon").objectReferenceValue = icon;
            so.FindProperty("nameLabel").objectReferenceValue = nameLabel;
            so.FindProperty("levelLabel").objectReferenceValue = levelLabel;
            so.FindProperty("readyMark").objectReferenceValue = ready;
            so.FindProperty("blind").objectReferenceValue = blindGroup;
            so.FindProperty("questionLabel").objectReferenceValue = question;
            so.FindProperty("lockRoot").objectReferenceValue = lockRoot;
            AssignArray(so.FindProperty("edgeBars"), bars);
            AssignArray(so.FindProperty("lockParts"), lockParts);
            AssignArray(so.FindProperty("fragments"), fragments);
            so.ApplyModifiedPropertiesWithoutUndo();

            PromptB1181UpgradeWindowBuilder.AddHoverFx(root, def.Id, icon, hoverGlow, blindGlow, fxBack, fxFront, glowSprite);
            return nodeView;
        }

        // 가장자리를 따라 늘어난 3px 막대. 늘어난 축은 sizeDelta(-28)로 양끝 14px씩 줄인다.
        private static Image EdgeBar(Transform parent, string name, bool horizontal, float side)
        {
            var bar = NewImage(name, parent, null, UpgradeTreeTween.Cyan);
            var rect = bar.rectTransform;
            if (horizontal)
            {
                rect.anchorMin = new Vector2(0f, side);
                rect.anchorMax = new Vector2(1f, side);
                rect.pivot = new Vector2(0.5f, side);
                rect.sizeDelta = new Vector2(-28f, 3f);
            }
            else
            {
                rect.anchorMin = new Vector2(side, 0f);
                rect.anchorMax = new Vector2(side, 1f);
                rect.pivot = new Vector2(side, 0.5f);
                rect.sizeDelta = new Vector2(3f, -28f);
            }

            rect.anchoredPosition = Vector2.zero;
            UpgradeTreeTween.SetAlpha(bar, 0f);
            return bar;
        }

        private static Image LockPart(Transform parent, string name, Color color, Vector2 position, Vector2 size)
        {
            var image = NewImage(name, parent, null, color);
            SetRect(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
            return image;
        }

        // ------------------------------------------------------------------ 상세 패널

        private sealed class DetailRefs
        {
            public Image Icon;
            public TMP_Text Question, Name, Level, Description;
            public GameObject EffectGroup;
            public TMP_Text EffectName, EffectCurrent, EffectNext;
            public GameObject EffectArrow;
            public GameObject EffectNextCaption;
            public GameObject CostGroup;
            public TMP_Text CostText, Status, Hint, Message;
            public Button Purchase;
            public TMP_Text PurchaseLabel;
            public CanvasGroup PurchaseGroup;
            public GameObject MaxBadge;

            public void Apply(SerializedObject so)
            {
                so.FindProperty("detailIcon").objectReferenceValue = Icon;
                so.FindProperty("detailQuestion").objectReferenceValue = Question;
                so.FindProperty("detailName").objectReferenceValue = Name;
                so.FindProperty("detailLevel").objectReferenceValue = Level;
                so.FindProperty("detailDescription").objectReferenceValue = Description;
                so.FindProperty("effectGroup").objectReferenceValue = EffectGroup;
                so.FindProperty("effectName").objectReferenceValue = EffectName;
                so.FindProperty("effectCurrent").objectReferenceValue = EffectCurrent;
                so.FindProperty("effectNext").objectReferenceValue = EffectNext;
                so.FindProperty("effectArrow").objectReferenceValue = EffectArrow;
                so.FindProperty("effectNextCaption").objectReferenceValue = EffectNextCaption;
                so.FindProperty("costGroup").objectReferenceValue = CostGroup;
                so.FindProperty("costText").objectReferenceValue = CostText;
                so.FindProperty("statusText").objectReferenceValue = Status;
                so.FindProperty("hintText").objectReferenceValue = Hint;
                so.FindProperty("purchaseButton").objectReferenceValue = Purchase;
                so.FindProperty("purchaseLabel").objectReferenceValue = PurchaseLabel;
                so.FindProperty("purchaseGroup").objectReferenceValue = PurchaseGroup;
                so.FindProperty("maxBadge").objectReferenceValue = MaxBadge;
                so.FindProperty("messageText").objectReferenceValue = Message;
            }
        }

        private static DetailRefs BuildDetail(
            Transform treeRoot,
            Transform panel,
            TMP_FontAsset font,
            Sprite plate,
            float topInset,
            float margin,
            float width,
            UpgradeTreeView treeView)
        {
            var refs = new DetailRefs();
            var detail = NewRect("DetailPanel", treeRoot);
            SetRect(detail, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero);
            detail.offsetMin = new Vector2(-(margin + width), 24f);
            detail.offsetMax = new Vector2(-margin, -topInset);

            var frame = NewImage("DetailFrame", detail, plate, UpgradeTreeTween.CyanDim);
            Stretch(frame.rectTransform);
            frame.type = Image.Type.Sliced;
            var bg = NewImage("DetailBackground", detail, plate, new Color(0.03f, 0.075f, 0.09f, 0.97f));
            Stretch(bg.rectTransform);
            bg.rectTransform.offsetMin = new Vector2(2f, 2f);
            bg.rectTransform.offsetMax = new Vector2(-2f, -2f);
            bg.type = Image.Type.Sliced;

            // 상단: 아이콘 + 이름 + 레벨
            refs.Icon = NewImage("DetailIcon", detail, null, Color.white);
            refs.Icon.preserveAspect = true;
            SetTopLeft(refs.Icon.rectTransform, 28f, 28f, 72f, 72f);
            refs.Question = NewText("DetailQuestion", detail, font, 54f, new Color(0.27f, 0.5f, 0.56f, 1f), TextAlignmentOptions.Center);
            refs.Question.text = "?";
            SetTopLeft(refs.Question.rectTransform, 28f, 28f, 72f, 72f);
            refs.Name = NewText("DetailName", detail, font, 30f, UpgradeTreeTween.TextMain, TextAlignmentOptions.MidlineLeft);
            SetTopLeft(refs.Name.rectTransform, 116f, 30f, 290f, 40f);
            refs.Name.enableAutoSizing = true;
            refs.Name.fontSizeMin = 20f;
            refs.Name.fontSizeMax = 30f;
            refs.Level = NewText("DetailLevel", detail, font, 20f, UpgradeTreeTween.Cyan, TextAlignmentOptions.MidlineLeft);
            SetTopLeft(refs.Level.rectTransform, 116f, 72f, 290f, 28f);

            // 설명
            refs.Description = NewText("DetailDescription", detail, font, 17f, UpgradeTreeTween.TextDim, TextAlignmentOptions.TopLeft);
            SetTopLeft(refs.Description.rectTransform, 28f, 116f, 374f, 84f);
            refs.Description.overflowMode = TextOverflowModes.Ellipsis;

            Divider(detail, 206f);

            // 효과: 현재 → 다음
            var effect = NewRect("EffectGroup", detail);
            SetTopLeft(effect, 28f, 216f, 374f, 112f);
            refs.EffectGroup = effect.gameObject;
            refs.EffectName = NewText("EffectName", effect, font, 18f, UpgradeTreeTween.TextDim, TextAlignmentOptions.TopLeft);
            SetTopLeft(refs.EffectName.rectTransform, 0f, 0f, 374f, 26f);
            var currentCaption = NewText("CurrentCaption", effect, font, 15f, UpgradeTreeTween.TextDim, TextAlignmentOptions.TopLeft);
            currentCaption.text = "현재";
            SetTopLeft(currentCaption.rectTransform, 0f, 30f, 150f, 22f);
            refs.EffectCurrent = NewText("EffectCurrent", effect, font, 28f, UpgradeTreeTween.TextMain, TextAlignmentOptions.TopLeft);
            SetTopLeft(refs.EffectCurrent.rectTransform, 0f, 52f, 160f, 60f);
            refs.EffectCurrent.enableAutoSizing = true;
            refs.EffectCurrent.fontSizeMin = 16f;
            refs.EffectCurrent.fontSizeMax = 28f;
            var arrow = NewText("EffectArrow", effect, font, 30f, UpgradeTreeTween.Cyan, TextAlignmentOptions.Center);
            arrow.text = "▶";
            SetTopLeft(arrow.rectTransform, 160f, 56f, 40f, 40f);
            refs.EffectArrow = arrow.gameObject;
            var nextCaption = NewText("NextCaption", effect, font, 15f, UpgradeTreeTween.Cyan, TextAlignmentOptions.TopLeft);
            nextCaption.text = "다음";
            refs.EffectNextCaption = nextCaption.gameObject;
            SetTopLeft(nextCaption.rectTransform, 214f, 30f, 150f, 22f);
            refs.EffectNext = NewText("EffectNext", effect, font, 28f, UpgradeTreeTween.Cyan, TextAlignmentOptions.TopLeft);
            SetTopLeft(refs.EffectNext.rectTransform, 214f, 52f, 160f, 60f);
            refs.EffectNext.enableAutoSizing = true;
            refs.EffectNext.fontSizeMin = 16f;
            refs.EffectNext.fontSizeMax = 28f;

            // 비용
            var cost = NewRect("CostGroup", detail);
            SetTopLeft(cost, 28f, 340f, 374f, 120f);
            refs.CostGroup = cost.gameObject;
            var costTitle = NewText("CostTitle", cost, font, 18f, UpgradeTreeTween.TextDim, TextAlignmentOptions.TopLeft);
            costTitle.text = "필요 재료";
            SetTopLeft(costTitle.rectTransform, 0f, 0f, 374f, 26f);
            refs.CostText = NewText("CostText", cost, font, 22f, UpgradeTreeTween.TextMain, TextAlignmentOptions.TopLeft);
            SetTopLeft(refs.CostText.rectTransform, 0f, 30f, 374f, 90f);
            refs.CostText.richText = true;

            // 상태 + 안내
            refs.Status = NewText("StatusText", detail, font, 24f, UpgradeTreeTween.Cyan, TextAlignmentOptions.MidlineLeft);
            SetTopLeft(refs.Status.rectTransform, 28f, 470f, 374f, 34f);
            refs.Hint = NewText("HintText", detail, font, 18f, UpgradeTreeTween.TextDim, TextAlignmentOptions.TopLeft);
            SetTopLeft(refs.Hint.rectTransform, 28f, 506f, 374f, 64f);

            // 최대 레벨 표시 (구매 버튼 대신)
            var max = NewImage("MaxBadge", detail, plate, new Color(0.35f, 0.27f, 0.08f, 0.9f));
            max.type = Image.Type.Sliced;
            SetRect(max.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(334f, 56f));
            var maxLabel = NewText("Label", max.transform, font, 24f, UpgradeTreeTween.Gold, TextAlignmentOptions.Center);
            Stretch(maxLabel.rectTransform);
            maxLabel.text = "최대 레벨 달성";
            refs.MaxBadge = max.gameObject;
            max.gameObject.SetActive(false);

            refs.Message = NewText("MessageText", detail, font, 16f, UpgradeTreeTween.TextDim, TextAlignmentOptions.Center);
            SetRect(refs.Message.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 92f), new Vector2(374f, 26f));

            // 기존 구매 버튼을 그대로 쓴다(onClick → PurchaseSelected 배선 유지). 상세 패널 하단 중앙으로 옮긴다.
            var purchase = panel.Find("PurchaseButton");
            if (purchase == null)
            {
                throw new System.InvalidOperationException("PurchaseButton not found under " + panel.name);
            }

            purchase.gameObject.SetActive(true);
            SetRect((RectTransform)purchase, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-(margin + width * 0.5f), 36f), new Vector2(280f, 62f));
            purchase.SetAsLastSibling();
            refs.Purchase = purchase.GetComponent<Button>();
            refs.PurchaseLabel = purchase.GetComponentInChildren<TMP_Text>(true);
            refs.PurchaseGroup = purchase.GetComponent<CanvasGroup>();
            if (refs.PurchaseGroup == null)
            {
                refs.PurchaseGroup = purchase.gameObject.AddComponent<CanvasGroup>();
            }

            // 프레임 이미지가 없는 단순 버튼(광산 패널)은 어두운 금속 + 청록 테두리로 맞춘다.
            if (purchase.Find("Frame") == null)
            {
                var image = purchase.GetComponent<Image>();
                if (image != null)
                {
                    image.sprite = plate;
                    image.type = Image.Type.Sliced;
                    image.color = new Color(0.07f, 0.26f, 0.3f, 1f);
                }
            }

            if (refs.PurchaseLabel != null)
            {
                refs.PurchaseLabel.raycastTarget = false;
                refs.PurchaseLabel.fontSize = 24f;
                refs.PurchaseLabel.color = UpgradeTreeTween.TextMain;
                refs.PurchaseLabel.alignment = TextAlignmentOptions.Center;
            }

            return refs;
        }

        private static void Divider(Transform parent, float top)
        {
            var line = NewImage("Divider", parent, null, new Color(0.12f, 0.34f, 0.38f, 0.9f));
            SetTopLeft(line.rectTransform, 28f, top, 374f, 2f);
        }

        // ------------------------------------------------------------------ 에셋

        private static void EnsureArt()
        {
            Directory.CreateDirectory(ArtFolder);
            Directory.CreateDirectory(ArtFolder + "Icons");
            EnsurePlate();
            EnsureGlow();
            EnsureHudIconCrops();
            PromptB1181UpgradeWindowBuilder.EnsureArt();
            AssetDatabase.Refresh();
        }

        // 모서리를 깎은 9-slice 판. 두 모서리는 크게, 나머지는 작게 깎아 각진 느낌을 낸다.
        private static void EnsurePlate()
        {
            var path = ArtFolder + "upgrade-node-plate.png";
            if (!File.Exists(path))
            {
                const int size = 64;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        var cutBig = (x + (size - 1 - y) < 20) || ((size - 1 - x) + y < 20);
                        var cutSmall = ((size - 1 - x) + (size - 1 - y) < 6) || (x + y < 6);
                        tex.SetPixel(x, y, cutBig || cutSmall ? Color.clear : Color.white);
                    }
                }

                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }

            ImportSprite(path, new Vector4(22f, 22f, 22f, 22f));
        }

        private static void EnsureGlow()
        {
            var path = ArtFolder + "upgrade-glow.png";
            if (!File.Exists(path))
            {
                const int size = 64;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        var dx = (x - 31.5f) / 31.5f;
                        var dy = (y - 31.5f) / 31.5f;
                        var r = Mathf.Sqrt(dx * dx + dy * dy);
                        var a = Mathf.Clamp01(1f - r);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                    }
                }

                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }

            ImportSprite(path, Vector4.zero);
        }

        // 기존 HUD 아이콘 시트(hud-icons.png)에서 아이콘만 잘라 개별 스프라이트로 만든다. 원본은 수정하지 않는다.
        private static readonly (string key, int cx, int cyTop)[] HudCrops =
        {
            ("heart", 158, 363), ("bolt", 455, 357), ("arrow", 739, 358),
            ("coins", 1051, 367), ("box", 1371, 361), ("plug", 1972, 349)
        };

        private static void EnsureHudIconCrops()
        {
            var atlasFile = Path.GetFullPath(HudAtlasPath);
            if (!File.Exists(atlasFile))
            {
                return;
            }

            Texture2D atlas = null;
            for (var i = 0; i < HudCrops.Length; i++)
            {
                var path = ArtFolder + "Icons/upgrade-icon-" + HudCrops[i].key + ".png";
                if (!File.Exists(path))
                {
                    if (atlas == null)
                    {
                        atlas = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        ImageConversion.LoadImage(atlas, File.ReadAllBytes(atlasFile));
                    }

                    const int crop = 300;
                    var x0 = Mathf.Clamp(HudCrops[i].cx - crop / 2, 0, atlas.width - crop);
                    var yTop = Mathf.Clamp(HudCrops[i].cyTop - crop / 2, 0, atlas.height - crop);
                    var y0 = atlas.height - yTop - crop;
                    var pixels = atlas.GetPixels(x0, y0, crop, crop);
                    var tex = new Texture2D(crop, crop, TextureFormat.RGBA32, false);
                    tex.SetPixels(pixels);
                    tex.Apply();
                    File.WriteAllBytes(path, tex.EncodeToPNG());
                    Object.DestroyImmediate(tex);
                }

                ImportSprite(path, Vector4.zero);
            }

            if (atlas != null)
            {
                Object.DestroyImmediate(atlas);
            }
        }

        internal static void ImportSprite(string path, Vector4 border)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }

        internal static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite LoadIcon(string key)
        {
            switch (key)
            {
                case "copper":
                    return LoadSprite("Assets/_Project/Art/Icons/icon_copper.png");
                case "reset":
                    return LoadSprite("Assets/_Project/Art/UI/SurfaceBase/Icons/icon-reset.png");
                case "gas":
                    // prompt-B 118-1: 가스 속 캐릭터 전용 아이콘(지형 타일 재사용 대체).
                    return LoadSprite(PromptB1181UpgradeWindowBuilder.GasIconPath);
                case "drone":
                    return LoadSprite("Assets/_Project/Art/Characters/Drone/digger_bot_idle.png");
                case "rescue":
                    return LoadSprite("Assets/_Project/Art/Characters/Drone/digger_bot_side_right.png");
                default:
                    return LoadSprite(ArtFolder + "Icons/upgrade-icon-" + key + ".png");
            }
        }

        // ------------------------------------------------------------------ UI 생성 보조

        private static TMP_FontAsset ResolveFont(Transform panel)
        {
            var labels = panel.GetComponentsInChildren<TMP_Text>(true);
            for (var i = 0; i < labels.Length; i++)
            {
                if (labels[i].font != null)
                {
                    return labels[i].font;
                }
            }

            return TMP_Settings.defaultFontAsset;
        }

        internal static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        internal static Image NewImage(string name, Transform parent, Sprite sprite, Color color)
        {
            var rect = NewRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI NewText(string name, Transform parent, TMP_FontAsset font, float size, Color color, TextAlignmentOptions alignment)
        {
            var rect = NewRect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                text.font = font;
            }

            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        internal static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        internal static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        // 부모 좌상단 기준 배치(x는 오른쪽, top은 아래쪽으로 증가).
        private static void SetTopLeft(RectTransform rect, float x, float top, float width, float height)
        {
            SetRect(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -top), new Vector2(width, height));
        }
    }
}
