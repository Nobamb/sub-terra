using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Integration;
using SubTerra.App.UI.HUD;
using SubTerra.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    public sealed class PromptB94MinimapTests
    {
        private readonly List<Object> objects = new();
        private ExplorationMinimap map;
        private Camera camera;
        private Tilemap terrain;
        private Transform player;

        [SetUp]
        public void SetUp()
        {
            var canvas = Create("Canvas", typeof(RectTransform), typeof(Canvas));
            canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(1920, 1080);
            var root = Create("Minimap", typeof(RectTransform), typeof(CanvasRenderer), typeof(CanvasGroup));
            root.transform.SetParent(canvas.transform, false);
            map = root.AddComponent<ExplorationMinimap>();
            map.enabled = false;
            camera = Create("Camera", typeof(Camera)).GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.aspect = 1.6f;
            camera.transform.position = new Vector3(0, 0, -10);
            var grid = Create("Grid", typeof(Grid));
            terrain = Create("Terrain", typeof(Tilemap)).GetComponent<Tilemap>();
            terrain.transform.SetParent(grid.transform, false);
            player = Create("Player").transform;
            map.Bind(terrain, player, null);
            typeof(ExplorationMinimap).GetField("worldCamera", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(map, camera);
            Tick("LateUpdate");
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = objects.Count - 1; i >= 0; i--)
                if (objects[i] != null) Object.DestroyImmediate(objects[i]);
            objects.Clear();
        }

        [Test]
        public void RestoreAndEvents_OnlyDestroyedCellsAndNoDuplicates()
        {
            map.RestoreMining(new WorldSnapshotDto { miningChanges = new List<MiningSnapshotDto>
            {
                new() { x = 1, y = 1, isDestroyed = true },
                new() { x = 2, y = 1, isDestroyed = false }
            }});
            map.RecordMining(new GameplayEventDto { type = GameplayEventType.TileMined, x = 1, y = 1 });
            map.RecordMining(new GameplayEventDto { type = GameplayEventType.GasTriggered, x = 3 });
            Assert.That(map.MinedCellCount, Is.EqualTo(1));
            map.RecordMining(new GameplayEventDto { type = GameplayEventType.TileMined, x = -1, y = -2 });
            Assert.That(map.MinedCellCount, Is.EqualTo(2));
            map.RestoreMining(new WorldSnapshotDto());
            Assert.That(map.MinedCellCount, Is.Zero);
        }

        [Test]
        public void Layout_UsesBottomRightCornerAndHalfOpacity()
        {
            map.rectTransform.anchorMin = map.rectTransform.anchorMax = Vector2.one;
            map.rectTransform.pivot = Vector2.one;
            map.rectTransform.anchoredPosition = new Vector2(-220, -100);
            map.color = Color.white;
            Tick("LateUpdate");
            Assert.That(map.rectTransform.anchorMin, Is.EqualTo(ExplorationMinimap.BottomRightCorner));
            Assert.That(map.rectTransform.anchorMax, Is.EqualTo(ExplorationMinimap.BottomRightCorner));
            Assert.That(map.rectTransform.pivot, Is.EqualTo(ExplorationMinimap.BottomRightCorner));
            Assert.That(map.rectTransform.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(map.color.a, Is.EqualTo(1f).Within(0.01f));
            Assert.That(map.DisplayOpacity, Is.EqualTo(ExplorationMinimap.PanelOpacity).Within(0.01f));
            Assert.That(map.rectTransform.rect.width * map.rectTransform.rect.height,
                Is.EqualTo(1920 * 1080 * ExplorationMinimap.ScreenAreaRatio).Within(1));
        }

        [Test]
        public void Opacity_StaysHalfWithoutCtrlM_AndIgnoresGraphicTint()
        {
            map.color = new Color(1f, 1f, 1f, 0.2f);
            Tick("LateUpdate");
            Assert.That(map.color.a, Is.EqualTo(1f).Within(0.01f));
            Assert.That(map.DisplayOpacity, Is.EqualTo(ExplorationMinimap.PanelOpacity).Within(0.01f));
            Assert.That(map.GetComponent<CanvasGroup>().ignoreParentGroups, Is.False);
        }

        [Test]
        public void CloseHint_ShowsMShortcutOnMap()
        {
            Assert.That(map.CloseHintText, Is.EqualTo(ExplorationMinimap.CloseHintLabel));
            Assert.That(map.CloseHintText, Does.Contain("M"));
            var hint = map.transform.Find("CloseHint");
            Assert.That(hint, Is.Not.Null);
            Assert.That(hint.GetComponent<TMP_Text>(), Is.Not.Null);
            map.ToggleMap();
            Tick("LateUpdate");
            Assert.That(map.DisplayOpacity, Is.EqualTo(0f).Within(0.01f));
            map.ToggleMap();
            Tick("LateUpdate");
            Assert.That(map.DisplayOpacity, Is.EqualTo(ExplorationMinimap.PanelOpacity).Within(0.01f));
            Assert.That(map.CloseHintText, Is.EqualTo(ExplorationMinimap.CloseHintLabel));
        }

        [Test]
        public void Layout_SitsAboveDarknessOverlay()
        {
            var overlay = Create("DepthDarknessOverlay", typeof(RectTransform));
            overlay.transform.SetParent(map.rectTransform.parent, false);
            overlay.transform.SetAsFirstSibling();
            Tick("LateUpdate");
            Assert.That(map.transform.GetSiblingIndex(), Is.EqualTo(overlay.transform.GetSiblingIndex() + 1));
        }

        [Test]
        public void Mesh_TracksCameraMiningAndPlayer_AndUsesTenPercentArea()
        {
            Assert.That(map.rectTransform.rect.width * map.rectTransform.rect.height,
                Is.EqualTo(1920 * 1080 * 0.1f).Within(1));
            int baseline = MeshCount();
            map.RecordMining(new GameplayEventDto { type = GameplayEventType.TileMined, x = 1, y = 1 });
            Assert.That(MeshCount(), Is.EqualTo(baseline + 4));
            map.RecordMining(new GameplayEventDto { type = GameplayEventType.TileMined, x = 100, y = 100 });
            Assert.That(MeshCount(), Is.EqualTo(baseline + 4));
            camera.transform.position += new Vector3(100, 100, 0);
            player.position += new Vector3(100, 100, 0);
            Assert.That(MeshCount(), Is.EqualTo(baseline + 4));
            player.position = Vector3.zero;
            Assert.That(MeshCount(), Is.LessThan(baseline));
            map.ToggleMap();
            Assert.That(MeshCount(), Is.Zero);
            map.ToggleMap();
            Assert.That(MeshCount(), Is.GreaterThan(0));
            Assert.That(map.raycastTarget, Is.False);
        }

        [Test]
        public void MKey_ReopensHiddenMap_AndIgnoresTextInput()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                PressKeys(keyboard, Key.M);
                Assert.That(map.IsMapVisible, Is.False);
                PressKeys(keyboard, Key.M);
                Assert.That(map.IsMapVisible, Is.True);
                var events = Create("Events", typeof(EventSystem)).GetComponent<EventSystem>();
                typeof(EventSystem).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(events, null);
                var input = Create("Search", typeof(RectTransform), typeof(TMP_InputField));
                input.GetComponent<TMP_InputField>().enabled = false;
                EventSystem.current = events;
                events.SetSelectedGameObject(input);
                Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(input));
                PressKeys(keyboard, Key.M);
                Assert.That(map.IsMapVisible, Is.True);
                events.SetSelectedGameObject(null);
                typeof(EventSystem).GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(events, null);
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
            }
        }

        [Test]
        public void CtrlM_HoldMakesOpaqueWithoutToggling()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                Assert.That(map.IsMapVisible, Is.True);
                Assert.That(map.DisplayOpacity, Is.EqualTo(ExplorationMinimap.PanelOpacity).Within(0.01f));
                PressKeys(keyboard, Key.LeftCtrl, Key.M);
                Tick("LateUpdate");
                Assert.That(map.IsMapVisible, Is.True);
                Assert.That(map.IsOpaqueHold, Is.True);
                Assert.That(map.DisplayOpacity, Is.EqualTo(ExplorationMinimap.OpaqueOpacity).Within(0.01f));
                ReleaseKeys(keyboard);
                Tick("LateUpdate");
                Assert.That(map.IsOpaqueHold, Is.False);
                Assert.That(map.IsMapVisible, Is.True);
                Assert.That(map.DisplayOpacity, Is.EqualTo(ExplorationMinimap.PanelOpacity).Within(0.01f));
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
            }
        }

        [Test]
        public void Guide_DocumentsMinimapShortcuts()
        {
            var controls = GameGuidePanelView.GetTabBody(GameGuidePanelView.GuideTab.Controls);
            Assert.That(controls, Does.Contain("미니맵 켜기/끄기"));
            Assert.That(controls, Does.Contain("M 키"));
            Assert.That(controls, Does.Contain("Ctrl + M"));
            Assert.That(controls, Does.Contain("50%"));
        }

        private void PressKeys(Keyboard keyboard, params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
            Tick("Update");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
            InputSystem.Update();
            keyboard.MakeCurrent();
            Tick("Update");
        }

        private void ReleaseKeys(Keyboard keyboard)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
            Tick("Update");
        }

        private int MeshCount()
        {
            using var mesh = new VertexHelper();
            typeof(ExplorationMinimap).GetMethod("OnPopulateMesh", BindingFlags.NonPublic | BindingFlags.Instance,
                null, new[] { typeof(VertexHelper) }, null)
                .Invoke(map, new object[] { mesh });
            return mesh.currentVertCount;
        }

        private void Tick(string method) => typeof(ExplorationMinimap)
            .GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(map, null);

        private GameObject Create(string name, params System.Type[] components)
        {
            var value = new GameObject(name, components);
            objects.Add(value);
            return value;
        }
    }
}
