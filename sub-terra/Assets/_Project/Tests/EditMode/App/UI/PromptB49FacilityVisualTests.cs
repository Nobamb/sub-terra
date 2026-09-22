using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Integration;
using SubTerra.App.UI.HUD;
using SubTerra.Gameplay.Building;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Tests.UI
{
    /// <summary>prompt-B 49: 1칸 시설 비주얼·근접 말풍선·가이드 E키 안내.</summary>
    public sealed class PromptB49FacilityVisualTests
    {
        private static readonly string[] TargetPrefabPaths =
        {
            PromptB49FacilityVisualBuilder.LightPrefabPath,
            PromptB49FacilityVisualBuilder.ChargerPrefabPath,
            PromptB49FacilityVisualBuilder.StoragePrefabPath,
            PromptB49FacilityVisualBuilder.SettlementPrefabPath,
            PromptB49FacilityVisualBuilder.OutpostPrefabPath,
            PromptB49FacilityVisualBuilder.ClinicPrefabPath
        };

        private static readonly string[] TargetBuildingDataPaths =
        {
            "Assets/_Project/Data/Buildings/Building_Light_Basic.asset",
            "Assets/_Project/Data/Buildings/Building_Charger_Basic.asset",
            "Assets/_Project/Data/Buildings/Building_Storage_Basic.asset",
            "Assets/_Project/Data/Buildings/Building_Settlement_Basic.asset",
            "Assets/_Project/Data/Buildings/Building_OutpostCore_Basic.asset",
            "Assets/_Project/Data/Buildings/Building_Clinic_Basic.asset"
        };

        [Test]
        public void PromptB49_GuideControls_ExplainsNearbyEKeyAndNameBubble()
        {
            var controls = GameGuidePanelView.GetTabBody(GameGuidePanelView.GuideTab.Controls);
            Assert.That(controls, Does.Contain("E 키"));
            Assert.That(controls, Does.Contain("근처에서 E 키"));
            Assert.That(controls, Does.Contain("충전기"));
            Assert.That(controls, Does.Contain("보관함"));
            Assert.That(controls, Does.Contain("정산 콘솔"));
            Assert.That(controls, Does.Contain("전진기지 코어"));
            Assert.That(controls, Does.Contain("긴급 탈출 포탈"));
            Assert.That(controls, Does.Contain("말풍선"));
            Assert.That(controls, Does.Contain("버팀목·사다리 제외"));
        }

        [Test]
        public void PromptB49_DisplayNames_ExcludeSupportAndLadderFromProximity()
        {
            Assert.That(ItemDisplayNames.ShowsProximityName(DataIds.Buildings.SupportBasic), Is.False);
            Assert.That(ItemDisplayNames.ShowsProximityName(DataIds.Buildings.LadderBasic), Is.False);
            Assert.That(ItemDisplayNames.ShowsProximityName(DataIds.Buildings.LightBasic), Is.True);
            Assert.That(ItemDisplayNames.ShowsProximityName(DataIds.Buildings.ChargerBasic), Is.True);
            Assert.That(ItemDisplayNames.ShowsProximityName(DataIds.Buildings.StorageBasic), Is.True);
            Assert.That(ItemDisplayNames.ShowsProximityName(DataIds.Buildings.SettlementBasic), Is.True);
            Assert.That(ItemDisplayNames.ShowsProximityName(DataIds.Buildings.OutpostCoreBasic), Is.True);
            Assert.That(ItemDisplayNames.ShowsProximityName(DataIds.Buildings.EmergencyEscapePortal), Is.True);

            Assert.That(ItemDisplayNames.Building(DataIds.Buildings.LightBasic), Is.EqualTo("조명"));
            Assert.That(ItemDisplayNames.Building(DataIds.Buildings.ChargerBasic), Is.EqualTo("충전기"));
            Assert.That(ItemDisplayNames.Building(DataIds.Buildings.StorageBasic), Is.EqualTo("보관함"));
            Assert.That(ItemDisplayNames.Building(DataIds.Buildings.SettlementBasic), Is.EqualTo("정산 콘솔"));
            Assert.That(ItemDisplayNames.Building(DataIds.Buildings.OutpostCoreBasic), Is.EqualTo("전진기지 코어"));
        }

        [Test]
        public void PromptB49_Prefabs_UseDistinctAuthoredArtWithShallowRockOverlap()
        {
            var created = new List<GameObject>();
            try
            {
                var artworkPaths = new HashSet<string>();
                foreach (var path in TargetPrefabPaths)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    Assert.That(prefab, Is.Not.Null, path);
                    Assert.That(
                        prefab.transform.Find(PromptB49FacilityVisualBuilder.VisualRootName),
                        Is.Not.Null,
                        path + " needs VisualRoot");

                    var instance = Object.Instantiate(prefab);
                    created.Add(instance);
                    var visualRoot = instance.transform.Find(
                        PromptB49FacilityVisualBuilder.VisualRootName);
                    var bounds = GetActiveSpriteBounds(visualRoot.gameObject);
                    bool clinic = path == PromptB49FacilityVisualBuilder.ClinicPrefabPath;
                    Assert.That(bounds.size.x,
                        clinic ? Is.InRange(1.75f, 1.82f) : Is.InRange(0.80f, 0.98f),
                        path + " width");
                    Assert.That(bounds.min.y,
                        Is.EqualTo(clinic ? -1.2f : -0.68f).Within(0.015f),
                        path + " rock overlap");

                    SpriteRenderer artwork = FindPrimaryRenderer(visualRoot);
                    Assert.That(artwork, Is.Not.Null, path + " authored artwork");
                    string artworkPath = AssetDatabase.GetAssetPath(artwork.sprite);
                    Assert.That(artworkPath, Does.StartWith("Assets/_Project/Art/Facilities/MVP/"));
                    artworkPaths.Add(artworkPath);
                }

                Assert.That(artworkPaths.Count, Is.EqualTo(TargetPrefabPaths.Length));
            }
            finally
            {
                for (var i = 0; i < created.Count; i++)
                {
                    Object.DestroyImmediate(created[i]);
                }
            }
        }

        [Test]
        public void PromptB49_BuildingMenuIcons_MatchEachFacilityArtwork()
        {
            Assert.That(TargetBuildingDataPaths.Length, Is.EqualTo(TargetPrefabPaths.Length));
            for (int i = 0; i < TargetBuildingDataPaths.Length; i++)
            {
                BuildingData data = AssetDatabase.LoadAssetAtPath<BuildingData>(TargetBuildingDataPaths[i]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TargetPrefabPaths[i]);
                Assert.That(data, Is.Not.Null, TargetBuildingDataPaths[i]);
                Assert.That(prefab, Is.Not.Null, TargetPrefabPaths[i]);
                SpriteRenderer artwork = FindPrimaryRenderer(prefab.transform.Find("VisualRoot"));
                Assert.That(artwork, Is.Not.Null, TargetPrefabPaths[i]);
                Assert.That(data.Icon, Is.SameAs(artwork.sprite), TargetBuildingDataPaths[i]);
            }
        }

        [Test]
        public void PromptB49_ProximityLabel_ShowsNameExceptSupportAndLadder()
        {
            var host = new GameObject("PromptB49_LabelHost");
            var player = new GameObject("PromptB49_Player");
            var light = CreateBuilding("Light", DataIds.Buildings.LightBasic, Vector3.zero);
            var support = CreateBuilding("Support", DataIds.Buildings.SupportBasic, new Vector3(0.4f, 0f, 0f));
            var ladder = CreateBuilding("Ladder", DataIds.Buildings.LadderBasic, new Vector3(-0.4f, 0f, 0f));
            var farCharger = CreateBuilding(
                "FarCharger",
                DataIds.Buildings.ChargerBasic,
                new Vector3(20f, 0f, 0f));
            try
            {
                var controller = host.AddComponent<FacilityProximityLabelController>();
                controller.SetPlayer(player.transform);

                player.transform.position = Vector3.zero;
                controller.Refresh();
                Assert.That(controller.VisibleBubbleCount, Is.EqualTo(1));
                Assert.That(controller.TryGetVisibleLabel(DataIds.Buildings.LightBasic, out var lightName), Is.True);
                Assert.That(lightName, Is.EqualTo("조명"));
                Assert.That(controller.TryGetVisibleLabel(DataIds.Buildings.SupportBasic, out _), Is.False);
                Assert.That(controller.TryGetVisibleLabel(DataIds.Buildings.LadderBasic, out _), Is.False);
                Assert.That(controller.TryGetVisibleLabel(DataIds.Buildings.ChargerBasic, out _), Is.False);

                player.transform.position = farCharger.transform.position;
                controller.Refresh();
                Assert.That(controller.VisibleBubbleCount, Is.EqualTo(1));
                Assert.That(controller.TryGetVisibleLabel(DataIds.Buildings.ChargerBasic, out var chargerName), Is.True);
                Assert.That(chargerName, Is.EqualTo("충전기"));
                Assert.That(controller.TryGetVisibleLabel(DataIds.Buildings.LightBasic, out _), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(farCharger);
                Object.DestroyImmediate(ladder);
                Object.DestroyImmediate(support);
                Object.DestroyImmediate(light);
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void PromptB49_ProximityLabel_UsesNotoSansKoreanFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/_Project/Fonts/NotoSansKR-Regular_SDF.asset");
            Assert.That(font, Is.Not.Null);
            Assert.That(font.atlasTextures, Is.Not.Null.And.Not.Empty);
            Assert.That(font.atlasTextures[0], Is.Not.Null);
            Assert.That(font.atlasTextures[0].width, Is.GreaterThan(1));
            Assert.That(font.atlasTextures[0].height, Is.GreaterThan(1));
            Assert.That(font.characterTable.Count, Is.GreaterThan(50));

            var hudText = new GameObject("PromptB49_HudText");
            var tmp = hudText.AddComponent<TextMeshProUGUI>();
            tmp.font = font;

            var host = new GameObject("PromptB49_FontHost");
            var player = new GameObject("PromptB49_FontPlayer");
            var light = CreateBuilding("LightFont", DataIds.Buildings.LightBasic, Vector3.zero);
            try
            {
                var controller = host.AddComponent<FacilityProximityLabelController>();
                controller.SetPlayer(player.transform);
                controller.Refresh();

                Assert.That(controller.VisibleBubbleCount, Is.EqualTo(1));
                Assert.That(
                    FacilityProximityLabelController.IsKoreanFont(controller.ActiveFont),
                    Is.True);
                Assert.That(controller.ActiveFont.name, Does.Contain("NotoSansKR"));
            }
            finally
            {
                Object.DestroyImmediate(light);
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(hudText);
            }
        }

        private static GameObject CreateBuilding(string name, string buildingId, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            go.AddComponent<BuildingInstance>().Initialize(name + "-id", buildingId);
            return go;
        }

        private static Bounds GetActiveSpriteBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<SpriteRenderer>(false)
                .Where(renderer => renderer != null && renderer.enabled)
                .ToArray();
            Assert.That(renderers.Length, Is.GreaterThan(0), root.name + " has no active sprites");
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static SpriteRenderer FindPrimaryRenderer(Transform visualRoot)
        {
            if (visualRoot == null) return null;
            var renderers = visualRoot.GetComponentsInChildren<SpriteRenderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].enabled && renderers[i].sprite != null)
                {
                    return renderers[i];
                }
            }

            return null;
        }
    }
}
