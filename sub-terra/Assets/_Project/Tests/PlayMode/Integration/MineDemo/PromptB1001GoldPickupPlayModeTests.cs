using System.Collections;
using NUnit.Framework;
using SubTerra.App.Integration;
using SubTerra.App.UI.FacilityNameTag;
using SubTerra.App.UI.HUD;
using SubTerra.Gameplay.Mining;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace SubTerra.App.Tests.PlayMode.MineDemo
{
    /// <summary>
    /// 골드 획득 팝업 라이프사이클·배선 계약 (prompt-B 142). 실제 프레임(Update)으로 흐름을 확인한다.
    /// 이번 작업 환경에서는 PlayMode를 실행하지 않았고 코드 작성과 정적 검토만 했다.
    /// </summary>
    public sealed class PromptB1001GoldPickupPlayModeTests
    {
        private const string FontPath = "Assets/_Project/Fonts/SeoulAlrimTTF-Heavy_SDF.asset";

        [UnityTest]
        public IEnumerator Play_ShowsSinglePopupAboveHeadAndNoCoinsAtMinedCell()
        {
            using (var scene = new PickupScene())
            {
                Vector3 origin = scene.Map.GetCellCenterWorld(new Vector3Int(3, -4, 0));
                scene.Vfx.Play(35, origin, scene.Player.position + Vector3.up, 15);

                Assert.That(scene.Vfx.ActivePopupCount, Is.EqualTo(1));
                Assert.That(scene.Vfx.ActiveDustCount, Is.EqualTo(1), "작은 금빛 가루는 채굴 칸에 유지된다.");
                Assert.That(scene.Vfx.ActiveCoinCount, Is.Zero, "금화는 채굴 칸이 아니라 팝업에서 분출한다.");
                Assert.That(Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude), Is.Empty);

                GoldPickupPopupVisual visual = scene.Vfx.PopupVisual;
                Assert.That(visual.Root.layer, Is.EqualTo(FacilityNameTagLayers.MainScreen));
                Assert.That(FacilityNameTagLayers.IsHiddenFromCctv(visual.Root), Is.True);
                Assert.That(visual.Root.GetComponentsInChildren<GraphicRaycaster>(true), Is.Empty);
                foreach (Graphic graphic in visual.Root.GetComponentsInChildren<Graphic>(true))
                {
                    Assert.That(graphic.raycastTarget, Is.False, graphic.name);
                }

                yield return null;
                Assert.That(visual.Root.transform.position.y, Is.GreaterThan(scene.Player.position.y));
                Assert.That(Mathf.Abs(visual.Root.transform.position.x - scene.Player.position.x), Is.LessThan(0.01f));
            }
        }

        [UnityTest]
        public IEnumerator Popup_FollowsMovingPlayerAndLeverReturnsInRealFrames()
        {
            using (var scene = new PickupScene())
            {
                scene.Vfx.Play(20, Vector3.zero, scene.Player.position + Vector3.up);
                GoldPickupPopupVisual visual = scene.Vfx.PopupVisual;

                yield return new WaitForSecondsRealtime(0.08f);
                scene.Player.position += new Vector3(2f, 0f, 0f);
                yield return null;
                Assert.That(
                    Mathf.Abs(visual.Root.transform.position.x - scene.Player.position.x),
                    Is.LessThan(0.01f),
                    "플레이어 머리 위를 추적한다.");

                yield return new WaitForSecondsRealtime(0.4f);
                Assert.That(visual.LeverAngleNow, Is.EqualTo(0f).Within(0.01f));
                Assert.That(visual.MainFill.text, Is.EqualTo("+20 G"));
                Assert.That(visual.MainLineRect.localScale, Is.EqualTo(Vector3.one));
                Assert.That(visual.MainLineRect.anchoredPosition, Is.EqualTo(Vector2.zero));
                Assert.That(visual.MainGhostActiveCount, Is.Zero);
            }
        }

        [UnityTest]
        public IEnumerator Popup_ExitsAndLeavesNothingBehind()
        {
            using (var scene = new PickupScene())
            {
                scene.Vfx.Play(20, Vector3.zero, scene.Player.position + Vector3.up, 5);
                float timeout = Time.realtimeSinceStartup + 3f;
                while (scene.Vfx.IsPopupPlaying && Time.realtimeSinceStartup < timeout)
                {
                    yield return null;
                }

                Assert.That(scene.Vfx.IsPopupPlaying, Is.False);
                Assert.That(GameObject.Find("GoldPickupPopup"), Is.Null);
                Assert.That(scene.Vfx.ActiveCoinCount, Is.Zero);
            }
        }

        [UnityTest]
        public IEnumerator Merge_DuringExitKeepsOnePopupAndRestoresAlpha()
        {
            using (var scene = new PickupScene())
            {
                scene.Vfx.Play(20, Vector3.zero, scene.Player.position + Vector3.up);
                float timeout = Time.realtimeSinceStartup + 3f;
                while (scene.Vfx.IsPopupPlaying && !scene.Vfx.PopupState.Exiting && Time.realtimeSinceStartup < timeout)
                {
                    yield return null;
                }

                Assume.That(scene.Vfx.PopupState != null && scene.Vfx.PopupState.Exiting, Is.True);
                GoldPickupPopupState state = scene.Vfx.PopupState;
                Assert.That(state.HudSequenceStarted, Is.True);

                scene.Vfx.Play(30, Vector3.zero, scene.Player.position + Vector3.up);
                Assert.That(scene.Vfx.ActivePopupCount, Is.EqualTo(1));
                Assert.That(state.BaseTotal, Is.EqualTo(50));
                Assert.That(state.Exiting, Is.False);

                yield return new WaitForSecondsRealtime(GoldPickupPopupTimeline.MergeRestoreDuration + 0.1f);
                Assert.That(state.ExitAmount, Is.Zero);
                Assert.That(scene.Vfx.PopupVisual.CanvasAlpha, Is.EqualTo(1f).Within(0.001f));
                yield return new WaitForSecondsRealtime(GoldPickupPopupTimeline.MergeRollDuration + 0.05f);
                Assert.That(scene.Vfx.PopupVisual.MainFill.text, Is.EqualTo("+50 G"));
            }
        }

        [UnityTest]
        public IEnumerator HudPulse_RestoresGoldTextAfterRealFrames()
        {
            using (var scene = new PickupScene(withHud: true))
            {
                Vector3 baseScale = scene.GoldRect.localScale;
                Vector2 basePosition = scene.GoldRect.anchoredPosition;
                scene.Vfx.Play(20, Vector3.zero, scene.Player.position + Vector3.up);

                var sawPulse = false;
                float timeout = Time.realtimeSinceStartup + 4f;
                while ((scene.Vfx.IsPopupPlaying || scene.Vfx.ActiveHudParticleCount > 0 || scene.Vfx.IsPulsing)
                    && Time.realtimeSinceStartup < timeout)
                {
                    sawPulse |= scene.Vfx.IsPulsing && scene.GoldRect.localScale.x > baseScale.x;
                    yield return null;
                }

                Assert.That(sawPulse, Is.True, "HUD 입자가 도착하면 Gold 영역이 Pulse한다.");
                Assert.That(scene.GoldRect.localScale, Is.EqualTo(baseScale));
                Assert.That(scene.GoldRect.anchoredPosition, Is.EqualTo(basePosition));
            }
        }

        [UnityTest]
        public IEnumerator DisableAndDestroy_ClearPopupAndRestoreHud()
        {
            using (var scene = new PickupScene(withHud: true))
            {
                Vector3 baseScale = scene.GoldRect.localScale;
                scene.Vfx.Play(20, Vector3.zero, scene.Player.position + Vector3.up, 5);
                yield return new WaitForSecondsRealtime(0.2f);
                Assert.That(scene.Vfx.ActivePopupCount, Is.EqualTo(1));

                scene.Host.SetActive(false);
                yield return null;
                Assert.That(scene.Vfx.ActivePopupCount, Is.Zero);
                Assert.That(scene.Vfx.ActiveCoinCount, Is.Zero);
                Assert.That(scene.Vfx.ActiveHudParticleCount, Is.Zero);
                Assert.That(GameObject.Find("GoldPickupPopup"), Is.Null, "비활성화 시 남은 연출을 즉시 정리한다.");
                Assert.That(scene.GoldRect.localScale, Is.EqualTo(baseScale));

                scene.Host.SetActive(true);
                Object.Destroy(scene.Host);
                yield return null;
                Assert.That(GameObject.Find("GoldPickupWorldRoot"), Is.Null);
            }
        }

        [UnityTest]
        public IEnumerator FailedMining_ClearsPendingGoldWithoutPopup()
        {
            using (var scene = new PickupScene())
            {
                var mining = scene.Host.AddComponent<MiningSystem>();
                scene.Vfx.BindTo(mining, scene.Player, scene.Map);
                scene.Vfx.SetPendingGold(50, 10);
                Assert.That(scene.Vfx.PendingGold, Is.EqualTo(50));

                mining.TryStartMining(Vector3Int.zero);
                yield return null;
                Assert.That(scene.Vfx.PendingGold, Is.Zero);
                Assert.That(scene.Vfx.ActivePopupCount, Is.Zero);
            }
        }

        private static TMP_FontAsset LoadFont()
        {
            TMP_FontAsset font = null;
#if UNITY_EDITOR
            font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
#endif
            return font != null ? font : TMP_Settings.defaultFontAsset;
        }

        private static void SetPrivate(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        /// <summary>테스트용 최소 장면. Dispose가 만든 오브젝트를 모두 지운다.</summary>
        private sealed class PickupScene : System.IDisposable
        {
            private readonly Texture2D texture;
            private readonly Sprite sprite;
            private readonly GameObject grid;
            private readonly GameObject hudCanvas;
            private readonly GameObject cameraObject;

            public GameObject Host { get; }
            public GoldPickupVfx Vfx { get; }
            public Transform Player { get; }
            public Tilemap Map { get; }
            public RectTransform GoldRect { get; }

            public PickupScene(bool withHud = false)
            {
                TMP_FontAsset font = LoadFont();
                Assume.That(font, Is.Not.Null, "TMP 폰트를 찾지 못했다.");

                texture = new Texture2D(8, 8);
                texture.SetPixel(0, 0, Color.yellow);
                texture.Apply();
                sprite = Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 8f);

                grid = new GameObject("Grid", typeof(Grid));
                var mapObject = new GameObject("ForegroundTilemap");
                mapObject.transform.SetParent(grid.transform, false);
                Map = mapObject.AddComponent<Tilemap>();

                var playerObject = new GameObject("Player");
                playerObject.transform.position = new Vector3(1f, 2f, 0f);
                Player = playerObject.transform;

                Host = new GameObject("GoldPickupHost");
                Vfx = Host.AddComponent<GoldPickupVfx>();
                SetPrivate(Vfx, "coinSprite", sprite);
                SetPrivate(Vfx, "pickupFont", font);
                SetPrivate(Vfx, "foregroundTilemap", Map);
                Vfx.BindTo(null, Player, Map);

                if (withHud)
                {
                    cameraObject = new GameObject("MainCamera", typeof(Camera)) { tag = "MainCamera" };
                    cameraObject.transform.position = new Vector3(0f, 0f, -10f);
                    cameraObject.GetComponent<Camera>().orthographic = true;

                    hudCanvas = new GameObject("HudCanvas", typeof(RectTransform), typeof(Canvas));
                    hudCanvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                    var hudObject = new GameObject("BasicHUD", typeof(RectTransform));
                    hudObject.transform.SetParent(hudCanvas.transform, false);
                    var hudView = hudObject.AddComponent<BasicHudView>();
                    var goldObject = new GameObject("GoldText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                    goldObject.transform.SetParent(hudObject.transform, false);
                    var goldText = goldObject.GetComponent<TextMeshProUGUI>();
                    goldText.font = font;
                    GoldRect = goldText.rectTransform;
                    GoldRect.pivot = new Vector2(0f, 1f);
                    GoldRect.sizeDelta = new Vector2(146f, 31f);
                    GoldRect.anchoredPosition = new Vector2(60f, -154f);
                    SetPrivate(hudView, "goldText", goldText);
                    Vfx.SetHudView(hudView);
                }
            }

            public void Dispose()
            {
                if (Host != null)
                {
                    Object.Destroy(Host);
                }

                Object.Destroy(Player.gameObject);
                Object.Destroy(grid);
                if (hudCanvas != null)
                {
                    Object.Destroy(hudCanvas);
                }

                if (cameraObject != null)
                {
                    Object.Destroy(cameraObject);
                }

                Object.Destroy(sprite);
                Object.Destroy(texture);
            }
        }
    }
}
