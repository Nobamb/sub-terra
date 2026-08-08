using System.IO;
using System.Linq;
using System.Text;
using SubTerra.App.UI.Economy;
using SubTerra.App.UI.Progression;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// prompt-B 39 / surface-base-sell-system-design PR-2:
    /// Surface Base chrome 권위 좌표 + 자원 판매 진입 버튼/모달 + Progression 배치.
    /// rule 2-5: SurfaceBasePanel.prefab + SurfaceBase.unity 만 수정. 다른 UI Prefab 순회 금지.
    /// </summary>
    public static class PromptB_SellPanelLayoutBuilder
    {
        public const string SurfaceBasePrefabPath =
            "Assets/_Project/Prefabs/UI/SurfaceBasePanel.prefab";
        public const string SurfaceBaseScenePath =
            "Assets/_Project/Scenes/App/SurfaceBase.unity";
        public const string SellRowPrefabPath =
            "Assets/_Project/Prefabs/UI/EconomySellRow.prefab";

        // 권위 좌표표 (design §2)
        public const float GoalsY = 314f;
        public const float EnergyY = 264f;
        public const float DeepZoneY = 218f;
        public const float RecentRunY = 174f;
        public const float ActionY = 120f;
        public const float SellButtonY = 56f;
        public const float MessageY = 0f;
        public const float EconomyY = 0f;
        public const float EconomyW = 760f;
        public const float EconomyH = 520f;
        public const float ProgressionY = -146f;
        public const float ProgressionW = 760f;
        public const float ProgressionH = 220f;
        public const float UpgradeListW = 700f;
        public const float UpgradeListH = 180f;

        [MenuItem("SubTerra/UI/Build Prompt-B Sell Panel Layout (SurfaceBase only)")]
        public static void BuildFromMenu()
        {
            var report = Build();
            Debug.Log("[SubTerra] " + report);
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            Directory.CreateDirectory(Path.Combine(projectRoot, "Temp"));
            File.WriteAllText(
                Path.Combine(projectRoot, "Temp", "prompt-b-sell-panel-layout.txt"),
                report);
        }

        public static string Build()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Prompt-B Sell Panel Layout (Surface Base only)");
            sb.AppendLine(EnsureSellRowPrefab());
            sb.AppendLine(UpdateSurfaceBasePrefab());
            sb.AppendLine(UpdateSurfaceBaseScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return sb.ToString();
        }

        private static string EnsureSellRowPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(SellRowPrefabPath);
            if (existing != null)
            {
                return "SellRow prefab exists";
            }

            var dir = Path.GetDirectoryName(SellRowPrefabPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
            {
                // Prefabs/UI 는 이미 존재
            }

            var go = new GameObject("EconomySellRow", typeof(RectTransform), typeof(Image), typeof(Button));
            try
            {
                var rect = go.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(720f, 40f);
                var bg = go.GetComponent<Image>();
                bg.color = new Color(0.12f, 0.15f, 0.2f, 0.95f);
                var btn = go.GetComponent<Button>();
                btn.targetGraphic = bg;

                var icon = CreateImage(go.transform, "Icon", new Vector2(-330f, 0f), new Vector2(32f, 32f));
                var name = CreateTmp(go.transform, "Name", new Vector2(-120f, 0f), new Vector2(260f, 36f), 16f);
                var owned = CreateTmp(go.transform, "Owned", new Vector2(140f, 0f), new Vector2(90f, 36f), 15f);
                var price = CreateTmp(go.transform, "UnitPrice", new Vector2(250f, 0f), new Vector2(80f, 36f), 15f);
                var chrome = go.GetComponent<Image>();

                var row = go.AddComponent<EconomySellRowView>();
                row.EditorBind(string.Empty, icon, name, owned, price, btn, chrome);

                PrefabUtility.SaveAsPrefabAsset(go, SellRowPrefabPath);
                return "SellRow prefab created";
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static string UpdateSurfaceBasePrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SurfaceBasePrefabPath) == null)
            {
                return "SKIP: SurfaceBase prefab missing";
            }

            var root = PrefabUtility.LoadPrefabContents(SurfaceBasePrefabPath);
            try
            {
                ApplySellChromeAndControls(root);
                PrefabUtility.SaveAsPrefabAsset(root, SurfaceBasePrefabPath);
                return "SurfaceBasePrefab sell chrome + controls";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static string UpdateSurfaceBaseScene()
        {
            // Scene은 SurfaceBasePanel prefab 인스턴스를 사용한다. 인스턴스 자식을 직접
            // 재부모화하면 불법 override가 생기므로 prefab 갱신만 상속받게 둔다.
            return File.Exists(Path.Combine(
                Application.dataPath,
                "_Project",
                "Scenes",
                "App",
                "SurfaceBase.unity"))
                ? "SurfaceBase scene inherits prefab sell modal"
                : "SKIP: SurfaceBase scene missing";
        }

        private static void ApplySellChromeAndControls(GameObject root)
        {
            var host = root.transform.Find("SurfaceBaseContent") ?? root.transform;

            // 상태 밴드 압축 (권위 표)
            PlaceCentered(host, "GoalsText", GoalsY, 720f, 36f);
            PlaceCentered(host, "EnergyText", EnergyY, 720f, 32f);
            PlaceCentered(host, "DeepZoneText", DeepZoneY, 720f, 28f);
            PlaceCentered(host, "RecentRunText", RecentRunY, 720f, 28f);

            // 액션 행
            // 320 + 20 + 140 + 20 + 140 = 640 폭의 액션 행을 x=0 기준으로 맞춘다.
            PlaceAnchored(host, "ExploreButton", new Vector2(-160f, ActionY), new Vector2(320f, 48f));
            PlaceAnchored(host, "SettingsButton", new Vector2(90f, ActionY), new Vector2(140f, 48f));
            PlaceAnchored(host, "QuitButton", new Vector2(250f, ActionY), new Vector2(140f, 48f));

            // 탐사 버튼 바로 아래에 판매 진입점을 둔다.
            var openSell = EnsureButton(
                host,
                "OpenSellButton",
                new Vector2(0f, SellButtonY),
                new Vector2(320f, 48f),
                "자원 판매");

            // 두 액션 행 아래 상태 메시지.
            PlaceCentered(host, "MessageText", MessageY, 720f, 32f);

            var economy = host.Find("EconomyPanel");
            if (economy == null)
            {
                var ecoGo = new GameObject("EconomyPanel", typeof(RectTransform), typeof(Image));
                ecoGo.transform.SetParent(host, false);
                economy = ecoGo.transform;
                if (economy.GetComponent<EconomyPanelView>() == null)
                {
                    ecoGo.AddComponent<EconomyPanelView>();
                }

                if (economy.GetComponent<EconomyPanelBinder>() == null)
                {
                    ecoGo.AddComponent<EconomyPanelBinder>();
                }
            }

            // Economy root는 항상 활성 상태로 바인딩을 유지하되, CanvasGroup으로 모달만 숨긴다.
            var ecoRect = economy as RectTransform;
            if (ecoRect != null)
            {
                ecoRect.anchorMin = Vector2.zero;
                ecoRect.anchorMax = Vector2.one;
                ecoRect.pivot = new Vector2(0.5f, 0.5f);
                ecoRect.anchoredPosition = Vector2.zero;
                ecoRect.sizeDelta = Vector2.zero;
                ecoRect.offsetMin = Vector2.zero;
                ecoRect.offsetMax = Vector2.zero;
                EditorUtility.SetDirty(ecoRect);
            }

            economy.SetAsLastSibling();

            var ecoImage = economy.GetComponent<Image>();
            if (ecoImage == null)
            {
                ecoImage = economy.gameObject.AddComponent<Image>();
            }

            ecoImage.color = new Color(0.025f, 0.035f, 0.055f, 1f);
            ecoImage.raycastTarget = true;

            var modalCanvasGroup = economy.GetComponent<CanvasGroup>();
            if (modalCanvasGroup == null)
            {
                modalCanvasGroup = economy.gameObject.AddComponent<CanvasGroup>();
            }

            modalCanvasGroup.alpha = 0f;
            modalCanvasGroup.interactable = false;
            modalCanvasGroup.blocksRaycasts = false;

            var card = EnsureChild(economy, "SellModalCard", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            PlaceLocal(card, new Vector2(0f, EconomyY), new Vector2(EconomyW, EconomyH));
            var cardImage = card.GetComponent<Image>();
            cardImage.color = new Color(0.075f, 0.1f, 0.145f, 1f);
            cardImage.raycastTarget = true;

            MoveLegacySellChildren(economy, card);

            BuildEconomySellInternals(economy, card, openSell, modalCanvasGroup);

            // 판매 카드가 모달로 빠진 기본 화면의 빈 공간을 사용한다.
            var progression = root.GetComponentsInChildren<ProgressionPanelView>(true).FirstOrDefault();
            if (progression != null)
            {
                var panel = progression.GetComponent<RectTransform>();
                if (panel != null)
                {
                    panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
                    panel.pivot = new Vector2(0.5f, 0.5f);
                    panel.anchoredPosition = new Vector2(0f, ProgressionY);
                    panel.sizeDelta = new Vector2(ProgressionW, ProgressionH);
                    EditorUtility.SetDirty(panel);
                }

                // 탭·구매·엔트리 숨김 유지 (levels-only)
                var tabBar = progression.transform.Find("CategoryTabBar");
                if (tabBar != null)
                {
                    tabBar.gameObject.SetActive(false);
                }

                var purchase = progression.transform.Find("PurchaseButton");
                if (purchase != null)
                {
                    purchase.gameObject.SetActive(false);
                }

                foreach (var entry in progression.GetComponentsInChildren<ProgressionUpgradeEntryButton>(true))
                {
                    entry.gameObject.SetActive(false);
                }

                foreach (var name in new[] { "ProgDeep", "DeepZone", "UpgradeDetail", "UpgradeResult" })
                {
                    var t = FindChildRecursive(progression.transform, name);
                    if (t != null)
                    {
                        t.gameObject.SetActive(false);
                    }
                }

                PlaceCentered(progression.transform, "UpgradeList", 0f, UpgradeListW, UpgradeListH);
                var list = FindChildRecursive(progression.transform, "UpgradeList");
                if (list != null)
                {
                    list.gameObject.SetActive(true);
                    var tmp = list.GetComponent<TMP_Text>();
                    if (tmp != null)
                    {
                        tmp.alignment = TextAlignmentOptions.Top;
                        tmp.fontSize = 16f;
                        tmp.textWrappingMode = TextWrappingModes.Normal;
                        EditorUtility.SetDirty(tmp);
                    }
                }

                progression.EditorSetHideUpgradeEntryList(true);
                progression.EditorSetLevelsOnlySummary(true);
                progression.EditorSetHideDeepZoneTab(true);
                var so = new SerializedObject(progression);
                SetBool(so, "hideUpgradeEntryList", true);
                SetBool(so, "levelsOnlySummary", true);
                SetBool(so, "hideDeepZoneTab", true);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(progression);
            }
        }

        private static void BuildEconomySellInternals(
            Transform modalRoot,
            Transform card,
            Button openSell,
            CanvasGroup modalCanvasGroup)
        {
            var title = EnsureTmp(card, "SellTitle", new Vector2(-205f, 222f), new Vector2(300f, 34f), 22f, "자원 판매");
            var credits = EnsureTmp(card, "CreditsLabel", new Vector2(190f, 222f), new Vector2(230f, 34f), 17f, "골드 0");
            title.alignment = TextAlignmentOptions.MidlineLeft;
            credits.alignment = TextAlignmentOptions.MidlineRight;

            var description = EnsureTmp(
                card,
                "SellDescription",
                new Vector2(0f, 180f),
                new Vector2(680f, 34f),
                15f,
                "보유한 자원을 선택하고 수량을 정해 골드로 판매하세요.");
            description.alignment = TextAlignmentOptions.MidlineLeft;

            var close = EnsureButton(card, "CloseSellButton", new Vector2(344f, 222f), new Vector2(44f, 36f), "×");

            // Scroll list
            var viewport = EnsureChild(card, "SellListViewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            PlaceLocal(viewport, new Vector2(0f, 91f), new Vector2(700f, 132f));
            var vpImage = viewport.GetComponent<Image>();
            vpImage.color = new Color(0.05f, 0.06f, 0.08f, 0.8f);
            var mask = viewport.GetComponent<Mask>();
            mask.showMaskGraphic = false;
            if (viewport.GetComponent<RectMask2D>() == null)
            {
                viewport.gameObject.AddComponent<RectMask2D>();
            }

            var content = EnsureChild(viewport, "SellListContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            var contentRect = content as RectTransform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.spacing = 4f;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var rootScroll = modalRoot.GetComponent<ScrollRect>();
            if (rootScroll != null)
            {
                Object.DestroyImmediate(rootScroll);
            }

            var scroll = card.GetComponent<ScrollRect>();
            if (scroll == null)
            {
                scroll = card.gameObject.AddComponent<ScrollRect>();
            }

            scroll.viewport = viewport as RectTransform;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var empty = EnsureTmp(card, "EmptySellText", new Vector2(0f, 91f), new Vector2(680f, 64f), 15f,
                "판매할 광물이 없습니다. 탐사 후 귀환하세요.");
            empty.alignment = TextAlignmentOptions.Center;

            // Qty row
            var qtyLabel = EnsureTmp(card, "QtyLabel", new Vector2(-235f, 4f), new Vector2(120f, 34f), 15f, "판매 수량");
            qtyLabel.alignment = TextAlignmentOptions.MidlineLeft;
            var qtyMinus = EnsureButton(card, "QtyMinusButton", new Vector2(-105f, 4f), new Vector2(42f, 34f), "-");
            var qtyText = EnsureTmp(card, "QtyText", new Vector2(-35f, 4f), new Vector2(64f, 34f), 16f, "1");
            qtyText.alignment = TextAlignmentOptions.Center;
            var qtyPlus = EnsureButton(card, "QtyPlusButton", new Vector2(35f, 4f), new Vector2(42f, 34f), "+");
            var qtyMax = EnsureButton(card, "QtyMaxButton", new Vector2(115f, 4f), new Vector2(88f, 34f), "최대");

            var preview = EnsureTmp(card, "PreviewText", new Vector2(0f, -43f), new Vector2(680f, 26f), 16f, "예상 골드 +0");
            preview.alignment = TextAlignmentOptions.Center;

            var sellSelected = EnsureButton(card, "SellSelectedButton", new Vector2(-150f, -91f), new Vector2(220f, 42f), "선택 판매");
            var sellAll = EnsureButton(card, "SellAllButton", new Vector2(150f, -91f), new Vector2(220f, 42f), "전체 판매");

            var ecoStatus = EnsureTmp(card, "EcoStatus", new Vector2(0f, -145f), new Vector2(680f, 30f), 15f, string.Empty);
            var ecoDetail = EnsureTmp(card, "EcoDetail", new Vector2(0f, -181f), new Vector2(680f, 34f), 13f, string.Empty);
            ecoStatus.alignment = TextAlignmentOptions.Center;
            ecoDetail.alignment = TextAlignmentOptions.Center;
            ecoStatus.textWrappingMode = TextWrappingModes.Normal;
            ecoDetail.textWrappingMode = TextWrappingModes.Normal;

            var view = modalRoot.GetComponent<EconomyPanelView>();
            if (view == null)
            {
                view = modalRoot.gameObject.AddComponent<EconomyPanelView>();
            }

            var rowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SellRowPrefabPath);
            EconomySellRowView rowView = null;
            if (rowPrefab != null)
            {
                rowView = rowPrefab.GetComponent<EconomySellRowView>();
            }

            var busyControls = new Selectable[]
            {
                qtyMinus, qtyPlus, qtyMax, sellSelected, sellAll
            };

            view.EditorBind(ecoStatus, ecoDetail, null, busyControls);
            view.EditorBindModal(openSell, close, modalCanvasGroup);
            view.EditorBindSell(
                title,
                credits,
                content,
                rowView,
                empty,
                qtyText,
                preview,
                qtyMinus,
                qtyPlus,
                qtyMax,
                sellSelected,
                sellAll);
            EditorUtility.SetDirty(view);

            var binder = modalRoot.GetComponent<EconomyPanelBinder>();
            if (binder == null)
            {
                binder = modalRoot.gameObject.AddComponent<EconomyPanelBinder>();
            }

            // Prefab 직렬화: Binder.view 참조를 명시적으로 연결.
            var binderSo = new SerializedObject(binder);
            var viewProp = binderSo.FindProperty("view");
            if (viewProp != null)
            {
                viewProp.objectReferenceValue = view;
                binderSo.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(binder);
        }

        private static void MoveLegacySellChildren(Transform modalRoot, Transform card)
        {
            var names = new[]
            {
                "SellTitle",
                "CreditsLabel",
                "SellListViewport",
                "EmptySellText",
                "QtyLabel",
                "QtyMinusButton",
                "QtyText",
                "QtyPlusButton",
                "QtyMaxButton",
                "PreviewText",
                "SellSelectedButton",
                "SellAllButton",
                "EcoStatus",
                "EcoDetail"
            };

            foreach (var name in names)
            {
                var child = FindChildRecursive(modalRoot, name);
                if (child == null || child == card || child.IsChildOf(card))
                {
                    continue;
                }

                child.SetParent(card, false);
                EditorUtility.SetDirty(child);
            }
        }

        private static Transform EnsureChild(Transform parent, string name, params System.Type[] components)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(existing.gameObject);
                foreach (var componentType in components)
                {
                    if (componentType == typeof(Transform)
                        || componentType == typeof(RectTransform)
                        || existing.GetComponent(componentType) != null)
                    {
                        continue;
                    }

                    existing.gameObject.AddComponent(componentType);
                }

                return existing;
            }

            var go = new GameObject(name, components.Length > 0 ? components : new[] { typeof(RectTransform) });
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static TMP_Text EnsureTmp(Transform parent, string name, Vector2 pos, Vector2 size, float font, string text)
        {
            var t = FindChildRecursive(parent, name);
            TMP_Text tmp;
            if (t == null)
            {
                tmp = CreateTmp(parent, name, pos, size, font);
            }
            else
            {
                tmp = t.GetComponent<TMP_Text>();
                if (tmp == null)
                {
                    tmp = t.gameObject.AddComponent<TextMeshProUGUI>();
                }

                PlaceLocal(t, pos, size);
            }

            tmp.text = text;
            tmp.fontSize = font;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            EditorUtility.SetDirty(tmp);
            return tmp;
        }

        private static Button EnsureButton(Transform parent, string name, Vector2 pos, Vector2 size, string label)
        {
            var t = FindChildRecursive(parent, name);
            GameObject go;
            if (t == null)
            {
                go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(parent, false);
            }
            else
            {
                go = t.gameObject;
                if (go.GetComponent<Image>() == null)
                {
                    go.AddComponent<Image>();
                }

                if (go.GetComponent<Button>() == null)
                {
                    go.AddComponent<Button>();
                }
            }

            PlaceLocal(go.transform, pos, size);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.2f, 0.35f, 0.5f, 1f);
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = image;

            var labelT = go.transform.Find("Label");
            TMP_Text tmp;
            if (labelT == null)
            {
                tmp = CreateTmp(go.transform, "Label", Vector2.zero, size, 15f);
            }
            else
            {
                tmp = labelT.GetComponent<TMP_Text>();
            }

            if (tmp != null)
            {
                tmp.text = label;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.raycastTarget = false;
                var lr = tmp.rectTransform;
                lr.anchorMin = Vector2.zero;
                lr.anchorMax = Vector2.one;
                lr.offsetMin = Vector2.zero;
                lr.offsetMax = Vector2.zero;
            }

            EditorUtility.SetDirty(btn);
            return btn;
        }

        private static TMP_Text CreateTmp(Transform parent, string name, Vector2 pos, Vector2 size, float font)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            PlaceLocal(go.transform, pos, size);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = font;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Image CreateImage(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            PlaceLocal(go.transform, pos, size);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            return img;
        }

        private static void PlaceLocal(Transform t, Vector2 pos, Vector2 size)
        {
            var rect = t as RectTransform;
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            EditorUtility.SetDirty(rect);
        }

        private static void PlaceCentered(Transform parent, string childName, float y, float width, float height)
        {
            var child = FindChildRecursive(parent, childName) as RectTransform;
            if (child == null)
            {
                return;
            }

            child.anchorMin = child.anchorMax = new Vector2(0.5f, 0.5f);
            child.pivot = new Vector2(0.5f, 0.5f);
            child.anchoredPosition = new Vector2(0f, y);
            child.sizeDelta = new Vector2(width, height);
            EditorUtility.SetDirty(child);
        }

        private static void PlaceAnchored(Transform parent, string childName, Vector2 pos, Vector2 size)
        {
            var child = FindChildRecursive(parent, childName) as RectTransform;
            if (child == null)
            {
                return;
            }

            child.anchorMin = child.anchorMax = new Vector2(0.5f, 0.5f);
            child.pivot = new Vector2(0.5f, 0.5f);
            child.anchoredPosition = pos;
            child.sizeDelta = size;
            EditorUtility.SetDirty(child);
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent == null)
            {
                return null;
            }

            var direct = parent.Find(name);
            if (direct != null)
            {
                return direct;
            }

            for (var i = 0; i < parent.childCount; i++)
            {
                var found = FindChildRecursive(parent.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void SetBool(SerializedObject so, string prop, bool value)
        {
            var p = so.FindProperty(prop);
            if (p != null)
            {
                p.boolValue = value;
            }
        }

    }
}
