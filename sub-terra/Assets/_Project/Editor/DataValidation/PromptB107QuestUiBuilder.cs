using System;
using System.IO;
using SubTerra.App.Tutorial;
using SubTerra.App.UI.Tutorial;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// prompt-B 107 퀘스트 창 비주얼만 교체한다.
    /// 퀘스트 순서, 완료 판정, 보상 지급, 시설 창 위치는 건드리지 않는다.
    /// </summary>
    public static class PromptB107QuestUiBuilder
    {
        public const string Art = "Assets/_Project/Art/UI/Gameplay/Quest/";
        public const string IntegrationScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";

        // 가로는 기존 460의 25%. 세로는 HUD 프레임 하단(-264)과 시설 창 상단(-426) 사이만 쓸 수 있어
        // 170(25%)이면 시설 창을 덮으므로, 그 틈에 들어가는 154로 둔다.
        public const float SummaryCardX = 16f;
        public const float SummaryCardY = -268f;
        public const float SummaryCardWidth = 575f;
        public const float SummaryCardHeight = 154f;
        public const float SummaryHeaderInset = 56f;
        public const float SummaryHeaderY = -19f;
        public const float ThumbnailHeight = 236f;
        private const float DetailsW = 880f;
        private const float DetailsH = 718f;

        private static readonly Thumb[] Thumbs =
        {
            new Thumb(DemoObjectiveIds.MineBlock, Mining, GroundNormal),
            new Thumb(DemoObjectiveIds.MineCopper, OreCopper, Mining),
            new Thumb(DemoObjectiveIds.UpgradeDrillSpeed, Mining),
            new Thumb(DemoObjectiveIds.TravelToSurface, Elevator, Idle),
            new Thumb(DemoObjectiveIds.ReturnToMine, Elevator, GroundNormal),
            new Thumb(DemoObjectiveIds.MineIron, OreIron, Mining),
            new Thumb(DemoObjectiveIds.PlaceSupportInDanger, Support, Crack),
            new Thumb(DemoObjectiveIds.PlaceLadder, Ladder, Idle),
            new Thumb(DemoObjectiveIds.PlaceLightAtDepth, Light, GroundDeep),
            new Thumb(DemoObjectiveIds.StoreMineral, Storage, OreCopper),
            new Thumb(DemoObjectiveIds.InstallOutpostCore, Outpost, Idle),
            new Thumb(DemoObjectiveIds.ChargeNearOutpost, Charger, Outpost),
            new Thumb(DemoObjectiveIds.HealNearOutpost, Clinic, Idle),
            new Thumb(DemoObjectiveIds.UnlockDeepZone, Glyph, GroundDeep),
            new Thumb(DemoObjectiveIds.MineLithium, OreLithium, GroundDeep),
            new Thumb(DemoObjectiveIds.PurifyGasWithOutpost, GroundGas, Outpost),
            new Thumb(DemoObjectiveIds.SellAtSettlement, Settlement, OreIron),
            new Thumb(DemoObjectiveIds.EmergencyEscapeReturn, Idle, Elevator, Outpost)
        };

        private const string Mining = "Assets/_Project/Art/Characters/Player/Frames/Mining/mining_01.png";
        private const string Idle = "Assets/_Project/Art/Characters/Player/Frames/Idle/player_idle_01.png";
        private const string GroundNormal = "Assets/_Project/Art/Tiles/Ground/ground_normal_01.png";
        private const string GroundDeep = "Assets/_Project/Art/Tiles/Ground/ground_deep_01.png";
        private const string GroundGas = "Assets/_Project/Art/Tiles/Ground/ground_gas_01.png";
        private const string OreCopper = "Assets/_Project/Art/Tiles/Ore/ore_copper_01.png";
        private const string OreIron = "Assets/_Project/Art/Tiles/Ore/ore_iron_01.png";
        private const string OreLithium = "Assets/_Project/Art/Tiles/Ore/ore_lithium_01.png";
        private const string Crack = "Assets/_Project/Art/Tiles/Crack_Overlay/crack_orange_overlay.png";
        private const string Elevator = "Assets/_Project/Art/Facilities/MVP/elevator_station_mine.png";
        private const string Support = "Assets/_Project/Art/Facilities/MVP/support_pillar_mine.png";
        private const string Ladder = "Assets/_Project/Art/Facilities/MVP/ladder_segment_mine.png";
        private const string Light = "Assets/_Project/Art/Facilities/MVP/light_basic_cartoon_v3.png";
        private const string Storage = "Assets/_Project/Art/Facilities/MVP/storage_basic_cartoon_v2.png";
        private const string Outpost = "Assets/_Project/Art/Facilities/MVP/outpost_core_cartoon_v3.png";
        private const string Charger = "Assets/_Project/Art/Facilities/MVP/charger_basic_cartoon_v2.png";
        private const string Clinic = "Assets/_Project/Art/Facilities/MVP/clinic_basic_cartoon_v3.png";
        private const string Settlement = "Assets/_Project/Art/Facilities/MVP/settlement_console_cartoon_v3.png";
        private const string Glyph = "Assets/_Project/Art/Tiles/SealedGlyph/sealed_glyph_stone_01.png";
        private const string IconCopper = "Assets/_Project/Art/Icons/icon_copper.png";
        private const string IconIron = "Assets/_Project/Art/Icons/icon_iron.png";
        private const string IconLithium = "Assets/_Project/Art/Icons/icon_lithium.png";
        private const string IconGold = Art + "quest-icon-gold.png";

        [MenuItem("SubTerra/UI/Build Prompt-B 107 Quest Window")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + Build());
        }

        public static string Build()
        {
            if (EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("Stop Play Mode first.");
            }

            ImportSprites();
            var scene = SceneManager.GetSceneByPath(IntegrationScenePath);
            if (scene.IsValid() && scene.isLoaded && scene.isDirty)
            {
                throw new InvalidOperationException("Mine_Demo_Integration에 저장되지 않은 변경이 있습니다.");
            }

            var closeAfter = !scene.IsValid() || !scene.isLoaded;
            if (closeAfter)
            {
                scene = EditorSceneManager.OpenScene(IntegrationScenePath, OpenSceneMode.Additive);
            }

            try
            {
                var root = Find(scene, "DemoObjectiveRoot");
                if (root == null)
                {
                    throw new InvalidOperationException("DemoObjectiveRoot가 없습니다.");
                }

                var view = root.GetComponent<DemoObjectiveView>();
                var font = FontOf(root.transform);
                LayoutSummary(root.transform, view, font);
                LayoutDetails(root.transform, view, font);
                EditorUtility.SetDirty(view);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException("Mine_Demo_Integration 저장에 실패했습니다.");
                }

                return "Prompt-B 107 quest window built.";
            }
            finally
            {
                if (closeAfter && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void LayoutSummary(Transform root, DemoObjectiveView view, TMP_FontAsset font)
        {
            var button = EnsureButton(root, "QuestSummaryButton");
            var rect = button.GetComponent<RectTransform>();
            Place(rect, SummaryCardX, SummaryCardY, SummaryCardWidth, SummaryCardHeight, new Vector2(0f, 1f), new Vector2(0f, 1f));
            var hit = ClearHit(button);
            var fade = EnsureFade(button, "quest-active-off", "quest-active-on");
            button.transform.SetAsFirstSibling();
            Wire(button, view.OnObjectiveDetailsClicked);

            // 글자를 카드 자식으로 넣어 프레임 밖으로 나가지 않게 한다.
            // 프레임의 가로 구분선은 높이의 약 34% 지점이다.
            Adopt(button.transform, root, "QuestMissionLabel");
            Adopt(button.transform, root, "ProgressCount");
            Adopt(button.transform, root, "QuestStatusIcon");
            Adopt(button.transform, root, "ObjectiveTitle");
            Adopt(button.transform, root, "ObjectiveBody");

            const float headerFont = 21f;
            var mission = EnsureLabel(button.transform, "QuestMissionLabel", font);
            Style(mission, headerFont, Cyan, TextAlignmentOptions.MidlineLeft, false);
            mission.fontStyle = FontStyles.Bold;
            mission.characterSpacing = 4f;
            mission.text = "MISSION";
            Place(mission.rectTransform, SummaryHeaderInset, SummaryHeaderY, 170f, 32f, new Vector2(0f, 1f), new Vector2(0f, 1f));

            var marks = EnsureLabel(button.transform, "QuestMissionMarks", font);
            marks.text = string.Empty;
            marks.gameObject.SetActive(false);

            var progress = button.transform.Find("ProgressCount").GetComponent<TMP_Text>();
            Style(progress, headerFont, Color.white, TextAlignmentOptions.MidlineRight, false);
            Place(progress.rectTransform, -SummaryHeaderInset, SummaryHeaderY, 240f, 32f, new Vector2(1f, 1f), new Vector2(1f, 1f));

            const float iconSize = 44f;
            var icon = EnsureImage(button.transform, "QuestStatusIcon", QuestSprite("quest-status-ring"));
            icon.preserveAspect = true;
            Place(icon.rectTransform, SummaryHeaderInset - 4f, -74f, iconSize, iconSize, new Vector2(0f, 1f), new Vector2(0f, 1f));

            var titleX = SummaryHeaderInset - 4f + iconSize + 14f;
            var titleWidth = SummaryCardWidth - titleX - SummaryHeaderInset;
            var title = button.transform.Find("ObjectiveTitle").GetComponent<TMP_Text>();
            Style(title, 22f, Color.white, TextAlignmentOptions.MidlineLeft, false);
            title.fontStyle = FontStyles.Bold;
            title.overflowMode = TextOverflowModes.Ellipsis;
            Place(title.rectTransform, titleX, -66f, titleWidth, 32f, new Vector2(0f, 1f), new Vector2(0f, 1f));

            var body = button.transform.Find("ObjectiveBody").GetComponent<TMP_Text>();
            Style(body, 16f, new Color(0.85f, 0.93f, 0.96f), TextAlignmentOptions.TopLeft, true);
            body.overflowMode = TextOverflowModes.Ellipsis;
            Place(body.rectTransform, titleX, -102f, titleWidth, 44f, new Vector2(0f, 1f), new Vector2(0f, 1f));
            CenterOnFirstLines(icon.rectTransform, title.rectTransform, body);

            var nextAction = root.Find("NextAction");
            if (nextAction != null)
            {
                nextAction.gameObject.SetActive(false);
            }

            mission.transform.SetSiblingIndex(2);
            marks.transform.SetSiblingIndex(3);
            icon.transform.SetSiblingIndex(4);

            Assign(view, "objectiveTitleText", title);
            Assign(view, "objectiveBodyText", body);
            Assign(view, "progressCountText", progress);
            Assign(view, "basicStatusIcon", icon);
            Assign(view, "basicStatusClearSprite", QuestSprite("quest-status-check"));
            Assign(view, "basicStatusRingSprite", QuestSprite("quest-status-ring"));
            EditorUtility.SetDirty(hit);
            EditorUtility.SetDirty(fade);
        }

        private static void LayoutDetails(Transform root, DemoObjectiveView view, TMP_FontAsset font)
        {
            var panel = Required(root, "QuestDetailsPanel");
            panel.gameObject.SetActive(true);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(DetailsW, DetailsH);
            panelRect.localScale = Vector3.one;
            var frame = panel.GetComponent<Image>();
            if (frame == null)
            {
                frame = panel.gameObject.AddComponent<Image>();
            }

            frame.sprite = QuestSprite("quest-particular-frame");
            frame.type = Image.Type.Simple;
            frame.color = Color.white;
            frame.preserveAspect = false;
            frame.raycastTarget = true;

            if (panel.GetComponent<Canvas>() == null)
            {
                panel.gameObject.AddComponent<Canvas>();
            }

            if (panel.GetComponent<GraphicRaycaster>() == null)
            {
                panel.gameObject.AddComponent<GraphicRaycaster>();
            }

            var log = EnsureLabel(panel, "QuestMissionLogLabel", font);
            Style(log, 15f, Cyan, TextAlignmentOptions.MidlineLeft, false);
            log.fontStyle = FontStyles.Bold;
            log.characterSpacing = 4f;
            log.text = "MISSION LOG";
            Place(log.rectTransform, 78f, -60f, 300f, 26f, new Vector2(0f, 1f), new Vector2(0f, 1f));

            var title = StyleExisting(panel, "QuestDetailsTitle", 32f, Color.white, TextAlignmentOptions.MidlineLeft, false);
            title.fontStyle = FontStyles.Bold;
            title.overflowMode = TextOverflowModes.Overflow;
            Place(title.rectTransform, 78f, -92f, 640f, 46f, new Vector2(0f, 1f), new Vector2(0f, 1f));

            var statusPlate = EnsureImage(panel, "QuestStatusPlate", QuestSprite("quest-status-plate"));
            statusPlate.type = Image.Type.Sliced;
            Place(statusPlate.rectTransform, 74f, -142f, 732f, 44f, new Vector2(0f, 1f), new Vector2(0f, 1f));

            var statusIcon = EnsureImage(panel, "QuestDetailsStatusIcon", QuestSprite("quest-status-ring"));
            statusIcon.preserveAspect = true;
            Place(statusIcon.rectTransform, 86f, -148f, 32f, 32f, new Vector2(0f, 1f), new Vector2(0f, 1f));

            var status = StyleExisting(panel, "QuestDetailsStatus", 18f, Cyan, TextAlignmentOptions.MidlineLeft, false);
            status.fontStyle = FontStyles.Bold;
            Place(status.rectTransform, 126f, -148f, 260f, 34f, new Vector2(0f, 1f), new Vector2(0f, 1f));

            var progress = EnsureLabel(panel, "QuestDetailsProgress", font);
            Style(progress, 20f, Color.white, TextAlignmentOptions.MidlineRight, false);
            progress.fontStyle = FontStyles.Bold;
            Place(progress.rectTransform, 440f, -148f, 160f, 34f, new Vector2(0f, 1f), new Vector2(0f, 1f));

            var badge = EnsureBadge(panel, font);
            Place(badge.GetComponent<RectTransform>(), 630f, -147f, 128f, 34f, new Vector2(0f, 1f), new Vector2(0f, 1f));

            EnsureDivider(panel, "QuestStatusDivider", 78f, -192f, 724f);

            var missionIcon = EnsureImage(panel, "QuestMissionIcon", QuestSprite("quest-icon-mission"));
            missionIcon.preserveAspect = true;
            Place(missionIcon.rectTransform, 78f, -202f, 24f, 24f, new Vector2(0f, 1f), new Vector2(0f, 1f));
            var missionHeader = EnsureLabel(panel, "QuestMissionHeader", font);
            Style(missionHeader, 17f, Color.white, TextAlignmentOptions.MidlineLeft, false);
            missionHeader.fontStyle = FontStyles.Bold;
            missionHeader.text = "임무 내용";
            Place(missionHeader.rectTransform, 108f, -200f, 80f, 26f, new Vector2(0f, 1f), new Vector2(0f, 1f));

            EnsureDivider(panel, "QuestMissionDivider", 192f, -212f, 610f);

            var body = StyleExisting(panel, "QuestDetailsBody", 16f, new Color(0.88f, 0.95f, 0.98f), TextAlignmentOptions.TopLeft, true);
            body.overflowMode = TextOverflowModes.Overflow;
            Place(body.rectTransform, 78f, -228f, 724f, 42f, new Vector2(0f, 1f), new Vector2(0f, 1f));
            var next = StyleExisting(panel, "QuestDetailsNextAction", 14f, new Color(0.65f, 0.88f, 0.95f), TextAlignmentOptions.TopLeft, true);
            Place(next.rectTransform, 78f, -270f, 724f, 22f, new Vector2(0f, 1f), new Vector2(0f, 1f));

            var thumbnail = EnsureThumbnail(panel, font);
            Place(thumbnail.GetComponent<RectTransform>(), 78f, -296f, 724f, ThumbnailHeight, new Vector2(0f, 1f), new Vector2(0f, 1f));
            HideThumbnailDots(panel);

            var rewardIcon = EnsureImage(panel, "QuestRewardIcon", QuestSprite("quest-icon-reward"));
            rewardIcon.preserveAspect = true;
            Place(rewardIcon.rectTransform, 78f, -536f, 24f, 24f, new Vector2(0f, 1f), new Vector2(0f, 1f));
            var rewardHeader = EnsureLabel(panel, "QuestRewardHeader", font);
            Style(rewardHeader, 17f, Color.white, TextAlignmentOptions.MidlineLeft, false);
            rewardHeader.fontStyle = FontStyles.Bold;
            rewardHeader.text = "클리어 보상";
            Place(rewardHeader.rectTransform, 108f, -534f, 120f, 26f, new Vector2(0f, 1f), new Vector2(0f, 1f));

            EnsureDivider(panel, "QuestRewardDivider", 232f, -546f, 570f);

            var legacyReward = StyleExisting(panel, "QuestDetailsReward", 16f, Color.white, TextAlignmentOptions.MidlineLeft, false);
            Place(legacyReward.rectTransform, 78f, -564f, 400f, 28f, new Vector2(0f, 1f), new Vector2(0f, 1f));
            legacyReward.gameObject.SetActive(false);

            var row = EnsureRect(panel, "QuestRewardRow");
            Place(row, 78f, -564f, 724f, 72f, new Vector2(0f, 1f), new Vector2(0f, 1f));
            var copper = EnsureSlot(row, "RewardSlotCopper", font, "구리", SpriteAt(IconCopper), 0f);
            var iron = EnsureSlot(row, "RewardSlotIron", font, "철", SpriteAt(IconIron), 330f);
            var lithium = EnsureSlot(row, "RewardSlotLithium", font, "리튬", SpriteAt(IconLithium), 0f);
            var gold = EnsureSlot(row, "RewardSlotGold", font, "골드", SpriteAt(IconGold), 330f);

            var index = StyleExisting(panel, "QuestDetailsIndex", 14f, new Color(0.7f, 0.9f, 0.93f), TextAlignmentOptions.Center, false);
            Place(index.rectTransform, 330f, -636f, 220f, 28f, new Vector2(0f, 1f), new Vector2(0f, 1f));
            index.gameObject.SetActive(false);

            var brand = EnsureLabel(panel, "QuestBrandText", font);
            Style(brand, 12f, new Color(0.3f, 0.65f, 0.75f, 0.7f), TextAlignmentOptions.MidlineRight, false);
            brand.text = "PROJECT SUB-TERRA";
            Place(brand.rectTransform, 550f, -654f, 252f, 20f, new Vector2(0f, 1f), new Vector2(0f, 1f));

            var close = EnsureButton(panel, "QuestDetailsCloseButton");
            Place(close.GetComponent<RectTransform>(), -42f, -40f, 68f, 68f, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f));
            ClearHit(close);
            HideLabel(close.transform);
            EnsureFade(close, "quest-close-button-active-off", "quest-close-button-active-on");
            Wire(close, view.OnDetailsDismissClicked);

            var prev = EnsureButton(panel, "QuestDetailsPrevButton");
            Place(prev.GetComponent<RectTransform>(), 18f, 12f, 64f, 258f, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f));
            ClearHit(prev);
            HideLabel(prev.transform);
            EnsureFade(prev, "quest-particular-button-active-off", "quest-particular-button-active-on");
            prev.transform.localScale = Vector3.one;
            Wire(prev, view.OnDetailsPrevClicked);

            var nextButton = EnsureButton(panel, "QuestDetailsNextButton");
            Place(nextButton.GetComponent<RectTransform>(), -18f, 12f, 64f, 258f, new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f));
            ClearHit(nextButton);
            HideLabel(nextButton.transform);
            EnsureFade(nextButton, "quest-particular-button-active-off", "quest-particular-button-active-on");
            nextButton.transform.localScale = new Vector3(-1f, 1f, 1f);
            Wire(nextButton, view.OnDetailsNextClicked);

            close.transform.SetAsLastSibling();
            prev.transform.SetAsLastSibling();
            nextButton.transform.SetAsLastSibling();

            Assign(view, "detailsTitleText", title);
            Assign(view, "detailsBodyText", body);
            Assign(view, "detailsStatusIcon", statusIcon);
            Assign(view, "detailsStatusClearSprite", QuestSprite("quest-status-check"));
            Assign(view, "detailsStatusRingSprite", QuestSprite("quest-status-ring"));
            Assign(view, "detailsClearBadge", badge);
            Assign(view, "detailsProgressText", progress);
            Assign(view, "copperSlot", copper);
            Assign(view, "ironSlot", iron);
            Assign(view, "lithiumSlot", lithium);
            Assign(view, "goldSlot", gold);
            Assign(view, "thumbnailView", thumbnail.GetComponent<QuestThumbnailView>());
            badge.SetActive(false);
            panel.gameObject.SetActive(false);
            // 켜진 상태에서는 부모 HUD 캔버스에 중첩되어 Render Mode 대입이 무시된다.
            // 꺼진 뒤에 루트 캔버스가 되므로, 그때 Overlay로 저장해야 화면 밖에 남지 않는다.
            var canvas = panel.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = UiLayerPriority.QuestPopup;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        private static GameObject EnsureBadge(Transform parent, TMP_FontAsset font)
        {
            var rect = EnsureRect(parent, "QuestClearBadge");
            var image = rect.GetComponent<Image>();
            if (image == null)
            {
                image = rect.gameObject.AddComponent<Image>();
            }

            image.sprite = QuestSprite("quest-clear-badge");
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            var label = EnsureLabel(rect, "Label", font);
            Style(label, 14f, Color.white, TextAlignmentOptions.Center, false);
            label.text = "클리어";
            var labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            return rect.gameObject;
        }

        private static QuestRewardSlotView EnsureSlot(
            Transform row,
            string name,
            TMP_FontAsset font,
            string label,
            Sprite icon,
            float x)
        {
            var rect = EnsureRect(row, name);
            Place(rect, x, 0f, 310f, 72f, new Vector2(0f, 1f), new Vector2(0f, 1f));
            var plate = EnsureImage(rect, "Plate", QuestSprite("quest-reward-plate"));
            plate.type = Image.Type.Sliced;
            Stretch(plate.rectTransform);
            var iconImage = EnsureImage(rect, "Icon", icon);
            iconImage.preserveAspect = true;
            Place(iconImage.rectTransform, 16f, -12f, 52f, 52f, new Vector2(0f, 1f), new Vector2(0f, 1f));
            var nameText = EnsureLabel(rect, "Name", font);
            Style(nameText, 15f, new Color(0.75f, 0.9f, 0.95f), TextAlignmentOptions.MidlineLeft, false);
            nameText.text = label;
            Place(nameText.rectTransform, 78f, -12f, 210f, 24f, new Vector2(0f, 1f), new Vector2(0f, 1f));
            var amount = EnsureLabel(rect, "Amount", font);
            Style(amount, 24f, Color.white, TextAlignmentOptions.MidlineLeft, false);
            amount.fontStyle = FontStyles.Bold;
            amount.text = "0";
            Place(amount.rectTransform, 78f, -38f, 210f, 28f, new Vector2(0f, 1f), new Vector2(0f, 1f));
            var slot = rect.GetComponent<QuestRewardSlotView>();
            if (slot == null)
            {
                slot = rect.gameObject.AddComponent<QuestRewardSlotView>();
            }

            var serialized = new SerializedObject(slot);
            serialized.FindProperty("nameText").objectReferenceValue = nameText;
            serialized.FindProperty("amountText").objectReferenceValue = amount;
            serialized.FindProperty("mineralLabel").stringValue = label;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return slot;
        }

        private static GameObject EnsureThumbnail(Transform parent, TMP_FontAsset font)
        {
            var rect = EnsureRect(parent, "QuestThumbnail");
            var backdrop = EnsureImage(rect, "Backdrop", null);
            backdrop.color = new Color(0.02f, 0.05f, 0.08f, 0.95f);
            Stretch(backdrop.rectTransform);

            var frame = EnsureImage(rect, "Frame", QuestSprite("quest-thumbnail-frame"));
            frame.type = Image.Type.Sliced;
            Stretch(frame.rectTransform);

            var secondary = EnsureImage(rect, "Secondary", null);
            var primary = EnsureImage(rect, "Primary", null);
            var tertiary = EnsureImage(rect, "Tertiary", null);
            secondary.gameObject.SetActive(false);
            primary.gameObject.SetActive(false);
            tertiary.gameObject.SetActive(false);
            var placeholder = EnsureLabel(rect, "Placeholder", font);
            Style(placeholder, 16f, new Color(0.55f, 0.75f, 0.8f), TextAlignmentOptions.Center, false);
            placeholder.text = "장면 준비 중";
            var placeholderRect = placeholder.rectTransform;
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;
            placeholder.gameObject.SetActive(false);
            var view = rect.GetComponent<QuestThumbnailView>();
            if (view == null)
            {
                view = rect.gameObject.AddComponent<QuestThumbnailView>();
            }

            var serialized = new SerializedObject(view);
            serialized.FindProperty("primaryImage").objectReferenceValue = primary;
            serialized.FindProperty("secondaryImage").objectReferenceValue = secondary;
            serialized.FindProperty("tertiaryImage").objectReferenceValue = tertiary;
            serialized.FindProperty("placeholderText").objectReferenceValue = placeholder;
            var entries = serialized.FindProperty("entries");
            entries.arraySize = Thumbs.Length;
            for (var i = 0; i < Thumbs.Length; i++)
            {
                var element = entries.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("objectiveId").stringValue = Thumbs[i].Id;
                element.FindPropertyRelative("primary").objectReferenceValue = SpriteAt(Thumbs[i].Primary);
                element.FindPropertyRelative("secondary").objectReferenceValue = SpriteAt(Thumbs[i].Secondary);
                element.FindPropertyRelative("tertiary").objectReferenceValue = SpriteAt(Thumbs[i].Tertiary);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return rect.gameObject;
        }

        private static Image EnsureDivider(Transform parent, string name, float x, float y, float width)
        {
            var line = EnsureImage(parent, name, null);
            line.color = new Color(0f, 0.85f, 1f, 0.35f);
            Place(line.rectTransform, x, y, width, 1.5f, new Vector2(0f, 1f), new Vector2(0f, 1f));
            return line;
        }

        private static void HideThumbnailDots(Transform parent)
        {
            var dots = parent.Find("QuestThumbnailDots");
            if (dots == null)
            {
                dots = EnsureRect(parent, "QuestThumbnailDots");
            }

            dots.gameObject.SetActive(false);
        }

        private static void CenterOnFirstLines(RectTransform icon, RectTransform title, TMP_Text body)
        {
            var titleCenter = title.anchoredPosition.y - title.sizeDelta.y * 0.5f;
            var bodyCenter = body.rectTransform.anchoredPosition.y - body.fontSize * 0.55f;
            var mid = (titleCenter + bodyCenter) * 0.5f;
            icon.anchoredPosition = new Vector2(icon.anchoredPosition.x, mid + icon.sizeDelta.y * 0.5f);
            EditorUtility.SetDirty(icon);
        }

        private static QuestSpriteCrossfade EnsureFade(Button button, string off, string on)
        {
            var normal = EnsureImage(button.transform, "NormalImage", QuestSprite(off));
            var hover = EnsureImage(button.transform, "HoverImage", QuestSprite(on));
            Stretch(normal.rectTransform);
            Stretch(hover.rectTransform);
            normal.color = Color.white;
            normal.raycastTarget = true;
            hover.color = new Color(1f, 1f, 1f, 0f);
            hover.raycastTarget = false;
            normal.transform.SetAsFirstSibling();
            hover.transform.SetSiblingIndex(1);
            var fade = button.GetComponent<QuestSpriteCrossfade>();
            if (fade == null)
            {
                fade = button.gameObject.AddComponent<QuestSpriteCrossfade>();
            }

            var serialized = new SerializedObject(fade);
            serialized.FindProperty("normalImage").objectReferenceValue = normal;
            serialized.FindProperty("hoverImage").objectReferenceValue = hover;
            serialized.FindProperty("selectable").objectReferenceValue = button;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return fade;
        }

        private static Image ClearHit(Button button)
        {
            var hit = button.GetComponent<Image>();
            if (hit == null)
            {
                hit = button.gameObject.AddComponent<Image>();
            }

            hit.sprite = null;
            hit.type = Image.Type.Simple;
            hit.color = new Color(1f, 1f, 1f, 0f);
            hit.raycastTarget = true;
            var renderer = button.GetComponent<CanvasRenderer>();
            if (renderer != null)
            {
                renderer.cullTransparentMesh = false;
            }

            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;
            return hit;
        }

        private static void HideLabel(Transform button)
        {
            var label = button.Find("Label");
            if (label != null)
            {
                label.gameObject.SetActive(false);
            }
        }

        private static TMP_Text StyleExisting(
            Transform parent,
            string name,
            float size,
            Color color,
            TextAlignmentOptions alignment,
            bool wrap)
        {
            var text = Required(parent, name).GetComponent<TMP_Text>();
            Style(text, size, color, alignment, wrap);
            return text;
        }

        private static void Style(
            TMP_Text text,
            float size,
            Color color,
            TextAlignmentOptions alignment,
            bool wrap)
        {
            text.fontSize = size;
            text.enableAutoSizing = false;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Overflow;
            EditorUtility.SetDirty(text);
        }

        private static TMP_Text EnsureLabel(Transform parent, string name, TMP_FontAsset font)
        {
            var rect = EnsureRect(parent, name);
            var text = rect.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            }

            text.font = font;
            EditorUtility.SetDirty(text);
            return text;
        }

        private static Image EnsureImage(Transform parent, string name, Sprite sprite)
        {
            var rect = EnsureRect(parent, name);
            var image = rect.GetComponent<Image>();
            if (image == null)
            {
                image = rect.gameObject.AddComponent<Image>();
            }

            image.sprite = sprite;
            image.color = Color.white;
            image.raycastTarget = false;
            image.type = Image.Type.Simple;
            EditorUtility.SetDirty(image);
            return image;
        }

        private static Button EnsureButton(Transform parent, string name)
        {
            var existing = parent.Find(name);
            GameObject go;
            if (existing == null)
            {
                go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(parent, false);
            }
            else
            {
                go = existing.gameObject;
                if (go.GetComponent<Image>() == null)
                {
                    go.AddComponent<Image>();
                }

                if (go.GetComponent<Button>() == null)
                {
                    go.AddComponent<Button>();
                }
            }

            go.layer = parent.gameObject.layer;
            EditorUtility.SetDirty(go);
            return go.GetComponent<Button>();
        }

        private static RectTransform EnsureRect(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                return (RectTransform)existing;
            }

            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.gameObject.layer = parent.gameObject.layer;
            EditorUtility.SetDirty(rect.gameObject);
            return rect;
        }

        private static Transform Required(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child == null)
            {
                throw new InvalidOperationException(parent.name + "에 " + name + "이 없습니다.");
            }

            return child;
        }

        private static void Place(
            RectTransform rect,
            float x,
            float y,
            float width,
            float height,
            Vector2 anchor,
            Vector2 pivot)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            EditorUtility.SetDirty(rect);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void Wire(Button button, UnityEngine.Events.UnityAction action)
        {
            while (button.onClick.GetPersistentEventCount() > 0)
            {
                UnityEventTools.RemovePersistentListener(button.onClick, 0);
            }

            UnityEventTools.AddPersistentListener(button.onClick, action);
        }

        private static void Assign(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            if (property == null)
            {
                throw new InvalidOperationException(target.GetType().Name + "." + field + " 필드가 없습니다.");
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static TMP_FontAsset FontOf(Transform root)
        {
            var source = root.Find("ObjectiveTitle");
            if (source == null)
            {
                var button = root.Find("QuestSummaryButton");
                source = button != null ? button.Find("ObjectiveTitle") : null;
            }

            var text = source != null ? source.GetComponent<TMP_Text>() : null;
            if (text == null || text.font == null)
            {
                throw new InvalidOperationException("ObjectiveTitle 폰트가 없습니다.");
            }

            return text.font;
        }

        private static GameObject Find(Scene scene, string name)
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (var j = 0; j < transforms.Length; j++)
                {
                    if (transforms[j].name == name)
                    {
                        return transforms[j].gameObject;
                    }
                }
            }

            return null;
        }

        private static void ImportSprites()
        {
            var folder = Path.Combine(Application.dataPath, "_Project/Art/UI/Gameplay/Quest");
            foreach (var file in Directory.GetFiles(folder, "*.png"))
            {
                var assetPath = Art + Path.GetFileName(file);
                AssetDatabase.ImportAsset(assetPath);
                var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = 2048;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                var border = SliceBorder(Path.GetFileName(file));
                if (border > 0f)
                {
                    settings.spriteBorder = new Vector4(border, border, border, border);
                }

                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }

        private static float SliceBorder(string fileName)
        {
            switch (fileName)
            {
                case "quest-thumbnail-frame.png":
                    return 14f;
                case "quest-status-plate.png":
                    return 18f;
                case "quest-reward-plate.png":
                    return 28f;
                case "quest-clear-badge.png":
                    return 20f;
                default:
                    return 0f;
            }
        }

        private static Transform Adopt(Transform parent, Transform previousParent, string name)
        {
            var here = parent.Find(name);
            if (here != null)
            {
                return here;
            }

            if (previousParent == null)
            {
                return null;
            }

            var there = previousParent.Find(name);
            if (there == null)
            {
                return null;
            }

            there.SetParent(parent, false);
            return there;
        }

        private static Sprite QuestSprite(string name)
        {
            return SpriteAt(Art + name + ".png");
        }

        private static Sprite SpriteAt(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                throw new InvalidOperationException("스프라이트가 없습니다: " + path);
            }

            return sprite;
        }

        private static readonly Color Cyan = new Color(0.45f, 0.95f, 1f, 1f);

        private readonly struct Thumb
        {
            public readonly string Id;
            public readonly string Primary;
            public readonly string Secondary;
            public readonly string Tertiary;

            public Thumb(string id, string primary, string secondary = null, string tertiary = null)
            {
                Id = id;
                Primary = primary;
                Secondary = secondary;
                Tertiary = tertiary;
            }
        }
    }
}
