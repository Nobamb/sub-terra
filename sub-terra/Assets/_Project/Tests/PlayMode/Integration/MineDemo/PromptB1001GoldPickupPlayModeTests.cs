using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Integration;
using SubTerra.App.UI.FacilityNameTag;
using SubTerra.App.UI.HUD;
using SubTerra.Gameplay.Mining;
using SubTerra.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.PlayMode.MineDemo
{
    /// <summary>
    /// 골드 획득 홀로그램 팝업의 실제 프레임·수명주기·이벤트 배선 계약.
    /// 순수 계산은 EditMode(GoldPickupPopupTimelineTests / GoldPickupPopupStateTests)가 검증한다.
    /// </summary>
    public sealed class PromptB1001GoldPickupPlayModeTests
    {
        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (var index = created.Count - 1; index >= 0; index--)
            {
                if (created[index] != null)
                {
                    Object.Destroy(created[index]);
                }
            }

            created.Clear();
        }

        [UnityTest]
        public IEnumerator Play_ShowsMainScreenPopupThenCleansEverythingUp()
        {
            GoldPickupVfx vfx = NewVfx(withCoin: true);
            vfx.Play(35, new Vector3(3f, -4f, 0f), Vector3.up * 2f, 15);
            Assert.That(vfx.ActivePopupCount, Is.EqualTo(1));

            GameObject root = vfx.PopupVisual.Root;
            Assert.That(root.layer, Is.EqualTo(FacilityNameTagLayers.MainScreen));
            Assert.That(root.GetComponentInChildren<GraphicRaycaster>(true), Is.Null);

            yield return new WaitForSecondsRealtime(0.86f);
            Assert.That(vfx.ActiveCoinCount, Is.GreaterThan(0), "착지 순간 팝업에서 금화가 분출한다.");
            Assert.That(vfx.PopupVisual.MainLineRect.anchoredPosition, Is.EqualTo(Vector2.zero), "기본 줄은 정착");

            yield return new WaitForSecondsRealtime(1.7f);
            Assert.That(vfx.ActivePopupCount, Is.EqualTo(0));
            Assert.That(vfx.ActiveCoinCount, Is.EqualTo(0));
            yield return null;
            Assert.That(GameObject.Find("GoldPickupPopup"), Is.Null, "퇴장 뒤 프레임·레버·잔상·금화가 남지 않는다.");
        }

        [UnityTest]
        public IEnumerator TileMined_WithPendingGoldPlaysOnceAndChainedMinesMerge()
        {
            var grid = new GameObject("Grid", typeof(Grid));
            created.Add(grid);
            var mapObject = new GameObject("ForegroundTilemap");
            mapObject.transform.SetParent(grid.transform, false);
            var map = mapObject.AddComponent<Tilemap>();
            var player = new GameObject("Player");
            created.Add(player);
            player.transform.position = new Vector3(1f, 2f, 0f);

            GoldPickupVfx vfx = NewVfx(withCoin: true);
            var mining = vfx.gameObject.AddComponent<MiningSystem>();
            vfx.BindTo(mining, player.transform, map);

            RaiseTileMined(mining, new Vector3Int(3, -4, 0));
            Assert.That(vfx.ActivePopupCount, Is.EqualTo(0), "대기 골드가 없으면 연출하지 않는다.");

            vfx.SetPendingGold(35, 15);
            RaiseTileMined(mining, new Vector3Int(3, -4, 0));
            Assert.That(vfx.PendingGold, Is.EqualTo(0), "한 번 재생하면 대기 골드를 비운다.");
            Assert.That(vfx.ActivePopupCount, Is.EqualTo(1));
            Assert.That(vfx.PopupVisual.MainLabel, Is.EqualTo("+20 G"));
            Assert.That(vfx.PopupVisual.BonusLabel, Is.EqualTo("추가 골드 +15 G"));

            yield return new WaitForSecondsRealtime(0.3f);
            vfx.SetPendingGold(20);
            RaiseTileMined(mining, new Vector3Int(4, -4, 0));
            Assert.That(vfx.ActivePopupCount, Is.EqualTo(1), "연속 획득은 살아 있는 팝업 하나에 합쳐진다.");
            Assert.That(vfx.PopupState.TotalBase, Is.EqualTo(40));
            Assert.That(vfx.PopupState.TotalBonus, Is.EqualTo(15));

            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(vfx.PopupVisual.MainLabel, Is.EqualTo("+40 G"));
            Assert.That(vfx.PopupVisual.BonusLabel, Is.EqualTo("추가 골드 +15 G"));
        }

        [UnityTest]
        public IEnumerator Popup_FollowsMovingPlayerAndSinksInsideBodyWhileExiting()
        {
            var player = new GameObject("Player");
            created.Add(player);
            player.transform.position = new Vector3(0f, 2f, 0f);
            GoldPickupVfx vfx = NewVfx(withCoin: false);
            vfx.BindTo(null, player.transform);
            vfx.Play(20, Vector3.zero, Vector3.up);

            player.transform.position = new Vector3(4f, 2f, 0f);
            yield return null;
            Transform popup = vfx.PopupVisual.Root.transform;
            float restingOffset = popup.position.y - player.transform.position.y;
            Assert.That(popup.position.x, Is.EqualTo(4f).Within(0.01f), "플레이어 머리 위를 추적한다.");

            yield return new WaitForSecondsRealtime(0.6f);
            player.transform.position = new Vector3(6f, 2f, 0f);
            yield return null;
            Assert.That(popup.position.x, Is.EqualTo(6f).Within(0.01f));
            Assert.That(popup.position.y - player.transform.position.y, Is.EqualTo(restingOffset).Within(0.01f), "추적 기준 높이는 변하지 않는다.");
            Assert.That(vfx.PopupVisual.BodyRect.anchoredPosition.y, Is.EqualTo(0f).Within(0.01f), "다 뜬 뒤에는 제자리");

            float deadline = Time.realtimeSinceStartup + 3f;
            while (vfx.ActivePopupCount > 0 && !vfx.PopupState.IsExiting && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            // 퇴장 중 프레임이 접히기 시작할 때까지 기다린 뒤에도 추적과 가라앉음이 따로 적용된다.
            while (vfx.ActivePopupCount > 0 && vfx.PopupState.ExitProgress < 0.7f && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            if (vfx.ActivePopupCount > 0)
            {
                player.transform.position = new Vector3(7f, 2f, 0f);
                yield return null;
                if (vfx.ActivePopupCount > 0)
                {
                    Assert.That(popup.position.x, Is.EqualTo(7f).Within(0.01f), "퇴장 중에도 추적한다.");
                    Assert.That(popup.position.y - player.transform.position.y, Is.EqualTo(restingOffset).Within(0.01f), "루트 높이는 그대로, 가라앉음은 내부에서 처리");
                    Assert.That(vfx.PopupVisual.BodyRect.anchoredPosition.y, Is.LessThan(0f), "프레임이 아래로 가라앉는다.");
                }
            }
        }

        [UnityTest]
        public IEnumerator Merge_DuringExitCancelsExitAndKeepsSinglePopup()
        {
            GoldPickupVfx vfx = NewVfx(withCoin: false);
            vfx.Play(20, Vector3.zero, Vector3.up);
            float deadline = Time.realtimeSinceStartup + 4f;
            while (vfx.ActivePopupCount > 0 && vfx.PopupState.ExitProgress < 0.3f && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(vfx.ActivePopupCount, Is.EqualTo(1));
            Assert.That(vfx.PopupState.IsExiting, Is.True);

            GoldPickupPopupState state = vfx.PopupState;
            vfx.Play(10, Vector3.zero, Vector3.up);
            Assert.That(vfx.PopupState, Is.SameAs(state), "새 팝업을 만들지 않고 같은 팝업에 합친다.");
            Assert.That(state.IsExiting, Is.False);

            yield return new WaitForSecondsRealtime(GoldPickupPopupTimeline.RestoreSeconds + 0.1f);
            Assert.That(state.ExitProgress, Is.EqualTo(0f), "퇴장이 되감겨 다시 완전히 떠 있다.");
            Assert.That(vfx.PopupVisual.BodyRect.anchoredPosition.y, Is.EqualTo(0f).Within(0.01f));
            Assert.That(vfx.PopupVisual.MainLineRect.anchoredPosition, Is.EqualTo(Vector2.zero), "글자가 다시 제자리로 내려온다.");
            Assert.That(state.TotalBase, Is.EqualTo(30));

            yield return new WaitForSecondsRealtime(1.6f);
            Assert.That(vfx.ActivePopupCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Disable_ClearsPopupCoinsAndRestoresHudScale()
        {
            HudRig rig = NewHudRig();
            rig.Vfx.Play(20, Vector3.zero, Vector3.up);

            float deadline = Time.realtimeSinceStartup + 4f;
            while (!rig.Vfx.IsPulsing && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(rig.Vfx.IsPulsing, Is.True, "HUD 입자가 도착해 Gold HUD를 Pulse한다.");
            rig.Vfx.Play(5, Vector3.zero, Vector3.up);
            rig.Vfx.enabled = false;
            yield return null;

            Assert.That(rig.Vfx.ActivePopupCount, Is.EqualTo(0));
            Assert.That(rig.Vfx.ActiveCoinCount, Is.EqualTo(0));
            Assert.That(rig.Vfx.ActiveHudParticleCount, Is.EqualTo(0));
            Assert.That(rig.GoldRect.localScale, Is.EqualTo(Vector3.one), "비활성화해도 HUD 크기를 원복한다.");
            Assert.That(rig.GoldRect.anchoredPosition, Is.EqualTo(new Vector2(60f, -154f)));
            Assert.That(GameObject.Find("GoldPickupPopup"), Is.Null);
        }

        [UnityTest]
        public IEnumerator Destroy_RemovesWorldRootPopupAndHudParticles()
        {
            GoldPickupVfx vfx = NewVfx(withCoin: true);
            vfx.Play(35, Vector3.zero, Vector3.up, 15);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(GameObject.Find("GoldPickupWorldRoot"), Is.Not.Null);

            Object.Destroy(vfx.gameObject);
            yield return null;
            yield return null;
            Assert.That(GameObject.Find("GoldPickupWorldRoot"), Is.Null);
            Assert.That(GameObject.Find("GoldPickupPopup"), Is.Null);
        }

        private GoldPickupVfx NewVfx(bool withCoin)
        {
            var host = new GameObject("GoldPickupHost");
            created.Add(host);
            var vfx = host.AddComponent<GoldPickupVfx>();
            if (withCoin)
            {
                var texture = new Texture2D(8, 8);
                texture.SetPixel(0, 0, Color.yellow);
                texture.Apply();
                created.Add(texture);
                SetPrivate(vfx, "coinSprite", Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 8f));
            }

            return vfx;
        }

        private HudRig NewHudRig()
        {
            GoldPickupVfx vfx = NewVfx(withCoin: true);
            var cameraObject = new GameObject("MainCamera", typeof(Camera)) { tag = "MainCamera" };
            created.Add(cameraObject);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.GetComponent<Camera>().orthographic = true;

            var canvasObject = new GameObject("HudCanvas", typeof(RectTransform), typeof(Canvas));
            created.Add(canvasObject);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var hudObject = new GameObject("BasicHUD", typeof(RectTransform));
            hudObject.transform.SetParent(canvasObject.transform, false);
            var hudView = hudObject.AddComponent<BasicHudView>();
            var goldObject = new GameObject("GoldText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            goldObject.transform.SetParent(hudObject.transform, false);
            var goldText = goldObject.GetComponent<TextMeshProUGUI>();
            var goldRect = goldText.rectTransform;
            goldRect.pivot = new Vector2(0f, 1f);
            goldRect.sizeDelta = new Vector2(146f, 31f);
            goldRect.anchoredPosition = new Vector2(60f, -154f);
            SetPrivate(hudView, "goldText", goldText);
            vfx.SetHudView(hudView);
            return new HudRig { Vfx = vfx, GoldRect = goldRect };
        }

        private static void RaiseTileMined(MiningSystem mining, Vector3Int cell)
        {
            FieldInfo field = typeof(MiningSystem).GetField("TileMined", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "MiningSystem.TileMined 이벤트 필드");
            var handlers = (Action<Vector3Int, MiningTileDto>)field.GetValue(mining);
            Assert.That(handlers, Is.Not.Null, "GoldPickupVfx가 TileMined를 구독해야 한다.");
            handlers.Invoke(cell, default);
        }

        private static void SetPrivate(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private sealed class HudRig
        {
            public GoldPickupVfx Vfx;
            public RectTransform GoldRect;
        }
    }
}
