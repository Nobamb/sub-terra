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
    /// <summary>prompt-B 49: 시설 비주얼·근접 말풍선·가이드 E키 안내.</summary>
    public sealed class PromptB49FacilityVisualTests
    {
        [Test]
        public void OutpostOriginalArtwork_FitsTwoByTwoWithoutDistortion()
        {
            Sprite sprite = PromptB49FacilityVisualBuilder.GetArtworkSprite(FacilityVisualKind.OutpostCore);
            Assert.That(sprite, Is.Not.Null);
            Assert.That(FacilityGroundedVisual.ResolveArtwork(sprite, DataIds.Buildings.OutpostCoreBasic), Is.SameAs(sprite));
            Assert.That(FacilityGroundedVisual.TryGetGeometry(sprite, DataIds.Buildings.OutpostCoreBasic,
                new Vector2Int(2, 2), out _, out var scale), Is.True);
            var bounds = FacilityGroundedVisual.GetVisibleBounds(sprite);
            Assert.That(bounds.size.x * scale.x, Is.InRange(1.7f, 1.81f));
            Assert.That(bounds.size.y * scale.y, Is.InRange(1.55f, 1.79f));
            Assert.That(scale.x, Is.EqualTo(scale.y));
        }

        private static readonly string[] TargetPrefabPaths =
        {
            PromptB49FacilityVisualBuilder.LightPrefabPath,
            PromptB49FacilityVisualBuilder.ChargerPrefabPath,
            PromptB49FacilityVisualBuilder.StoragePrefabPath,
            PromptB49FacilityVisualBuilder.SettlementPrefabPath,
            PromptB49FacilityVisualBuilder.OutpostPrefabPath,
            PromptB49FacilityVisualBuilder.ClinicPrefabPath
        };

        [TestCase("Charger", "ChargerGrounded", 2, 2)]
        [TestCase("Storage", "StorageGrounded", 1, 1)]
        [TestCase("OutpostCore", "outpost_core_cartoon_v3", 2, 2)]
        public void GroundedFacility_MenuPreviewAndPlacedArtworkMatch(string dataName, string spriteName, int width, int height)
        {
            var data = AssetDatabase.LoadAssetAtPath<BuildingData>(
                "Assets/_Project/Data/Buildings/Building_" + dataName + "_Basic.asset");
            var expected = dataName == "OutpostCore" ? PromptB49FacilityVisualBuilder.GetArtworkSprite(FacilityVisualKind.OutpostCore)
                : Resources.Load<Sprite>("Facilities/" + spriteName);
            Assert.That(data, Is.Not.Null);
            Assert.That(data.Icon, Is.SameAs(expected), "Menu list and details use the catalog icon.");
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            GameObject placed = null;
            GameObject previewRoot = null;
            try
            {
                placed = (GameObject)PrefabUtility.InstantiatePrefab(data.RuntimePrefab, scene);
                var footprint = new Vector2Int(width, height);
                FacilityGroundedVisual.Apply(placed.transform, data.Id, footprint);
                var artwork = placed.transform.Find("VisualRoot/Artwork").GetComponent<SpriteRenderer>();
                previewRoot = new GameObject("GroundedPreview", typeof(SpriteRenderer), typeof(BuildingPlacementPreview));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(previewRoot, scene);
                var preview = previewRoot.GetComponent<BuildingPlacementPreview>();
                preview.ConfigureFromPrefab(data.RuntimePrefab, data.Id, footprint);
                Assert.That(artwork.sprite, Is.SameAs(expected));
                Assert.That(previewRoot.GetComponent<SpriteRenderer>().sprite, Is.SameAs(expected));
            }
            finally
            {
                if (placed != null) Object.DestroyImmediate(placed);
                if (previewRoot != null) Object.DestroyImmediate(previewRoot);
                UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static readonly string[] TargetBuildingDataPaths =
        {
            "Assets/_Project/Data/Buildings/Building_Light_Basic.asset",
            "Assets/_Project/Data/Buildings/Building_Charger_Basic.asset",
            "Assets/_Project/Data/Buildings/Building_Storage_Basic.asset",
            "Assets/_Project/Data/Buildings/Building_Settlement_Basic.asset",
            "Assets/_Project/Data/Buildings/Building_OutpostCore_Basic.asset",
            "Assets/_Project/Data/Buildings/Building_Clinic_Basic.asset"
        };

        [TestCase("charger", 2, 2)]
        [TestCase("charger", 1, 1)]
        [TestCase("storage", 1, 1)]
        [TestCase("ChargerGrounded", 2, 2)]
        [TestCase("ChargerGrounded", 1, 1)]
        [TestCase("StorageGrounded", 1, 1)]
        [TestCase("outpost_core_cartoon_v3", 2, 2)]
        [TestCase("outpost_core_cartoon_v3", 1, 2)]
        public void Facility_ActualOpaquePixelsTouchFoundation(string name, int width, int height)
        {
            bool outpost = name == "outpost_core_cartoon_v3";
            bool replacement = name.EndsWith("Grounded");
            string pngPath = outpost ? "Assets/_Project/Art/Facilities/MVP/outpost_core_cartoon_v3.png"
                : replacement ? "Assets/_Project/Resources/Facilities/" + name + ".png"
                : "Assets/_Project/Art/Facilities/MVP/" + name + "_basic_cartoon_v2.png";
            var texture = new Texture2D(2, 2);
            Sprite sprite = null;
            try
            {
                Assert.That(texture.LoadImage(System.IO.File.ReadAllBytes(pngPath)), Is.True);
                texture.name = replacement || outpost ? name : name + "_basic_cartoon_v2";
                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.2f, 0.8f), 100f, 0, SpriteMeshType.FullRect);
                Color32[] pixels = texture.GetPixels32();
                int bottom = texture.height;
                for (int y = 0; y < texture.height; y++)
                    for (int x = 0; x < texture.width; x++)
                        if (pixels[y * texture.width + x].a >= 64) bottom = Mathf.Min(bottom, y);
                Assert.That(bottom, Is.GreaterThan(0), "This source PNG has transparent space below its feet.");
                string kind = name == "ChargerGrounded" ? "charger" : name == "StorageGrounded" ? "storage"
                    : outpost ? "outpost_core" : name;
                string id = "building." + kind + ".basic";
                Assert.That(FacilityGroundedVisual.TryGetGeometry(sprite, id, new Vector2Int(width, height),
                    out var position, out var scale), Is.True);
                float actualFeet = position.y + (bottom - sprite.pivot.y) / sprite.pixelsPerUnit * scale.y;
                float ground = -height * 0.5f;
                float contact = outpost ? -0.08f : FacilityFoundationVisual.ArtworkContactHeight;
                Assert.That(actualFeet, Is.EqualTo(ground + contact).Within(0.002f));
                Assert.That(actualFeet, Is.LessThan(ground + 0.04f), "Feet must overlap the top of the plate.");
            }
            finally { if (sprite != null) Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); }
        }

        [TestCase("ChargerGrounded", "building.charger.basic", 2)]
        [TestCase("StorageGrounded", "building.storage.basic", 1)]
        public void Facility_ReplacementBaseIsLevelAndNeedsNoSeparatePlate(string name, string id, int size)
        {
            var texture = new Texture2D(2, 2);
            Sprite sprite = null;
            var root = new GameObject("GroundedBaseTest");
            try
            {
                texture.LoadImage(System.IO.File.ReadAllBytes("Assets/_Project/Resources/Facilities/" + name + ".png"));
                texture.name = name;
                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                var source = root.AddComponent<SpriteRenderer>();
                source.sprite = sprite;
                Bounds bounds = FacilityGroundedVisual.GetVisibleBounds(sprite);
                FacilityGroundedVisual.TryGetGeometry(sprite, id, new Vector2Int(size, size), out _, out var scale);
                var pixels = texture.GetPixels32();
                int lowest = texture.height, highest = 0;
                foreach (float fraction in new[] { 0.15f, 0.3f, 0.5f, 0.7f, 0.85f })
                {
                    int x = Mathf.RoundToInt((bounds.min.x + bounds.size.x * fraction) * 100f + sprite.pivot.x);
                    int bottom = texture.height;
                    for (int y = 0; y < texture.height; y++)
                        if (pixels[y * texture.width + x].a >= 64) { bottom = y; break; }
                    Assert.That(bottom, Is.LessThan(texture.height));
                    lowest = Mathf.Min(lowest, bottom);
                    highest = Mathf.Max(highest, bottom);
                }
                Assert.That((highest - lowest) / 100f * scale.y, Is.LessThan(0.035f), "The full base must be level, not one diagonal foot.");
                var foundation = FacilityGroundedVisual.ApplyFoundation(root.transform, source, id, new Vector2Int(size, size));
                foundation.BuildVisuals();
                Assert.That(foundation.transform.Find("ContactShadow").GetComponent<SpriteRenderer>().enabled, Is.True);
                Assert.That(foundation.transform.Find("PlateOutline").GetComponent<SpriteRenderer>().enabled, Is.False);
                Assert.That(foundation.transform.Find("PlateBody").GetComponent<SpriteRenderer>().enabled, Is.False);
                Assert.That(FacilityGroundedVisual.ResolveArtwork(null, id), Is.Not.Null);
            }
            finally { Object.DestroyImmediate(root); if (sprite != null) Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); }
        }

        [Test]
        public void PromptB49_OutpostPlacement_UsesOriginalTwoByTwoFootprint()
        {
            BuildingPlacementDefinition definition =
                AssetDatabase.LoadAssetAtPath<BuildingPlacementDefinition>(
                    PromptB49FacilityVisualBuilder.OutpostPlacementPath);

            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.Footprint, Is.EqualTo(new Vector2Int(2, 2)));
        }

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
                    string facilityId = path == PromptB49FacilityVisualBuilder.ChargerPrefabPath ? DataIds.Buildings.ChargerBasic
                        : path == PromptB49FacilityVisualBuilder.StoragePrefabPath ? DataIds.Buildings.StorageBasic
                        : path == PromptB49FacilityVisualBuilder.OutpostPrefabPath ? DataIds.Buildings.OutpostCoreBasic : null;
                    if (facilityId != null)
                        FacilityGroundedVisual.Apply(instance.transform, facilityId,
                            path == PromptB49FacilityVisualBuilder.ChargerPrefabPath ? new Vector2Int(2, 2)
                                : path == PromptB49FacilityVisualBuilder.OutpostPrefabPath ? new Vector2Int(2, 2) : Vector2Int.one);
                    var visualRoot = instance.transform.Find(
                        PromptB49FacilityVisualBuilder.VisualRootName);
                    var bounds = GetActiveSpriteBounds(visualRoot.gameObject);
                    if (facilityId != null)
                    {
                        SpriteRenderer visibleArt = FindPrimaryRenderer(visualRoot);
                        Bounds localBounds = FacilityGroundedVisual.GetVisibleBounds(visibleArt.sprite);
                        bounds = new Bounds(visibleArt.transform.TransformPoint(localBounds.center),
                            Vector3.Scale(localBounds.size, visibleArt.transform.lossyScale));
                    }
                    bool outpost = path == PromptB49FacilityVisualBuilder.OutpostPrefabPath;
                    bool largeFacility = path == PromptB49FacilityVisualBuilder.ClinicPrefabPath
                        || path == PromptB49FacilityVisualBuilder.ChargerPrefabPath;
                    Assert.That(bounds.size.x,
                        outpost ? Is.InRange(1.70f, 1.82f)
                            : path == PromptB49FacilityVisualBuilder.ChargerPrefabPath ? Is.InRange(1.30f, 1.82f)
                            : largeFacility ? Is.InRange(1.70f, 1.82f)
                                : Is.InRange(0.80f, 0.98f),
                        path + " width");
                    Assert.That(bounds.min.y,
                        Is.EqualTo(path == PromptB49FacilityVisualBuilder.ClinicPrefabPath ? -1.2f
                            : path == PromptB49FacilityVisualBuilder.ChargerPrefabPath ? -1.040f
                                : outpost ? -1.08f : path == PromptB49FacilityVisualBuilder.StoragePrefabPath ? -0.540f : -0.68f).Within(0.015f),
                        path + " rock overlap");

                    SpriteRenderer artwork = FindPrimaryRenderer(visualRoot);
                    Assert.That(artwork, Is.Not.Null, path + " authored artwork");
                    string artworkPath = AssetDatabase.GetAssetPath(artwork.sprite);
                    string replacementName = path == PromptB49FacilityVisualBuilder.ChargerPrefabPath ? "ChargerGrounded"
                            : path == PromptB49FacilityVisualBuilder.StoragePrefabPath ? "StorageGrounded" : null;
                    Assert.That(artworkPath, replacementName != null
                        ? Is.EqualTo("Assets/_Project/Resources/Facilities/" + replacementName + ".png")
                        : Does.StartWith("Assets/_Project/Art/Facilities/MVP/"));
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
                Assert.That(data.Icon, Is.SameAs(FacilityGroundedVisual.ResolveArtwork(artwork.sprite, data.Id)), TargetBuildingDataPaths[i]);
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
