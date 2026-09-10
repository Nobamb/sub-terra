using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.UI.MainMenu;
using SubTerra.App.UI.SurfaceBase;
using SubTerra.Gameplay.Mining;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    public sealed class PromptB93ControlSchemeTests
    {
        private readonly List<Object> spawned = new();
        private Keyboard keyboard;
        private ControlScheme originalScheme;
        private bool originalBlocked;
        private bool hadControlsPref;
        private int originalControlsPref;

        [SetUp]
        public void SetUp()
        {
            originalScheme = ControlPreferences.Scheme;
            originalBlocked = ControlPreferences.IsSettingsOpen;
            hadControlsPref = PlayerPrefs.HasKey(SettingsRuntimeApplier.PrefControls);
            originalControlsPref = PlayerPrefs.GetInt(SettingsRuntimeApplier.PrefControls, 0);
            keyboard = InputSystem.AddDevice<Keyboard>();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = spawned.Count - 1; i >= 0; i--)
                if (spawned[i] != null) Object.DestroyImmediate(spawned[i]);
            spawned.Clear();
            InputSystem.RemoveDevice(keyboard);
            ControlPreferences.Scheme = originalScheme;
            ControlPreferences.IsSettingsOpen = originalBlocked;
            if (hadControlsPref)
            {
                PlayerPrefs.SetInt(SettingsRuntimeApplier.PrefControls, originalControlsPref);
            }
            else
            {
                PlayerPrefs.DeleteKey(SettingsRuntimeApplier.PrefControls);
            }

            PlayerPrefs.Save();
        }

        [TestCase(ControlScheme.Classic, Key.W, 0, 1, 0, 0)]
        [TestCase(ControlScheme.Classic, Key.LeftArrow, -1, 0, 0, 0)]
        [TestCase(ControlScheme.WasdMove, Key.D, 1, 0, 0, 0)]
        [TestCase(ControlScheme.WasdMove, Key.UpArrow, 0, 0, 0, 1)]
        [TestCase(ControlScheme.WasdMove, Key.DownArrow, 0, 0, 0, -1)]
        [TestCase(ControlScheme.WasdMove, Key.LeftArrow, 0, 0, -1, 0)]
        [TestCase(ControlScheme.WasdMove, Key.RightArrow, 0, 0, 1, 0)]
        [TestCase(ControlScheme.ArrowsMove, Key.LeftArrow, -1, 0, 0, 0)]
        [TestCase(ControlScheme.ArrowsMove, Key.W, 0, 0, 0, 1)]
        [TestCase(ControlScheme.ArrowsMove, Key.S, 0, 0, 0, -1)]
        [TestCase(ControlScheme.ArrowsMove, Key.A, 0, 0, -1, 0)]
        [TestCase(ControlScheme.ArrowsMove, Key.D, 0, 0, 1, 0)]
        public void KeyRoles_AreExclusive(ControlScheme scheme, Key key, int mx, int my, int dx, int dy)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            InputSystem.Update();
            Assert.That(PlayerKeyboardControls.ReadMovement(keyboard, scheme), Is.EqualTo(new Vector2(mx, my)));
            Assert.That(PlayerKeyboardControls.ReadMiningDirection(keyboard, scheme), Is.EqualTo(new Vector2(dx, dy)));
        }

        [Test]
        public void PlayerController_MiningKeysDoNotMoveAndSettingsBlocksMovement()
        {
            var player = new GameObject("Player", typeof(Rigidbody2D), typeof(PlayerMovement), typeof(PlayerController));
            spawned.Add(player);
            var controller = player.GetComponent<PlayerController>();
            var movement = player.GetComponent<PlayerMovement>();
            SetField(controller, "movement", movement);
            ControlPreferences.Scheme = ControlScheme.WasdMove;
            ControlPreferences.IsSettingsOpen = false;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.RightArrow));
            InputSystem.Update();
            typeof(PlayerController).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, null);
            Assert.That(movement.IsMovementRequested, Is.False);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
            InputSystem.Update();
            typeof(PlayerController).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, null);
            Assert.That(movement.IsMovementRequested, Is.True);
            ControlPreferences.IsSettingsOpen = true;
            typeof(PlayerController).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, null);
            Assert.That(movement.IsMovementRequested, Is.False);
        }

        [Test]
        public void PlayerController_HonorsAppliedSchemeWhenMoveActionIncludesArrows()
        {
            var player = new GameObject("Player", typeof(Rigidbody2D), typeof(PlayerMovement), typeof(PlayerController));
            spawned.Add(player);
            var controller = player.GetComponent<PlayerController>();
            var movement = player.GetComponent<PlayerMovement>();
            var asset = CreateWasdAndArrowMoveAsset();
            spawned.Add(asset);
            asset.Enable();
            SetField(controller, "inputActions", asset);
            Invoke(controller, "Awake");
            Invoke(controller, "OnEnable");

            var values = SettingsValues.CreateDefaults();
            values.Controls = ControlScheme.WasdMove;
            SettingsRuntimeApplier.Apply(values, applyResolution: false);
            ControlPreferences.IsSettingsOpen = false;

            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.RightArrow));
            InputSystem.Update();
            Invoke(controller, "Update");
            Assert.That(movement.IsMovementRequested, Is.False);
            Assert.That(
                PlayerKeyboardControls.ReadMiningDirection(ControlPreferences.Scheme),
                Is.EqualTo(new Vector2(1f, 0f)));

            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
            InputSystem.Update();
            Invoke(controller, "Update");
            Assert.That(movement.IsMovementRequested, Is.True);

            values.Controls = ControlScheme.ArrowsMove;
            SettingsRuntimeApplier.Apply(values, applyResolution: false);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
            InputSystem.Update();
            Invoke(controller, "Update");
            Assert.That(movement.IsMovementRequested, Is.False);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.RightArrow));
            InputSystem.Update();
            Invoke(controller, "Update");
            Assert.That(movement.IsMovementRequested, Is.True);

            Invoke(controller, "OnDisable");
        }

        [Test]
        public void Cycle_SavesImmediately_AndReopenIgnoresStaleSession()
        {
            var root = new GameObject("Settings", typeof(RectTransform), typeof(Canvas));
            spawned.Add(root);
            var panel = ControlSchemePanel.Attach(root);
            panel.SetDraft(ControlScheme.Classic);
            panel.Cycle(1);
            Assert.That(panel.Selected, Is.EqualTo(ControlScheme.WasdMove));
            Assert.That(ControlPreferences.Scheme, Is.EqualTo(ControlScheme.WasdMove));
            Assert.That(SettingsRuntimeApplier.LoadControlScheme(), Is.EqualTo(ControlScheme.WasdMove));
            panel.Cycle(1);
            Assert.That(panel.Selected, Is.EqualTo(ControlScheme.ArrowsMove));
            Assert.That(ControlPreferences.Scheme, Is.EqualTo(ControlScheme.ArrowsMove));

            var stale = new SettingsSession();
            Assert.That(stale.Applied.Controls, Is.EqualTo(ControlScheme.Classic));
            stale.Open();
            Assert.That(stale.Draft.Controls, Is.EqualTo(ControlScheme.ArrowsMove));
            panel.SetDraft(stale.Draft.Controls);
            Assert.That(panel.Selected, Is.EqualTo(ControlScheme.ArrowsMove));
            Assert.That(
                ControlSchemePanel.PeekSelected(root, ControlScheme.Classic),
                Is.EqualTo(ControlScheme.ArrowsMove));
        }

        [Test]
        public void ApplyPersistedControlScheme_RestoresSavedSelection()
        {
            var values = SettingsValues.CreateDefaults();
            values.Controls = ControlScheme.ArrowsMove;
            SettingsRuntimeApplier.Apply(values, applyResolution: false);
            ControlPreferences.Scheme = ControlScheme.Classic;
            ControlPreferences.IsSettingsOpen = true;
            SettingsRuntimeApplier.ApplyPersistedControlScheme();
            Assert.That(ControlPreferences.Scheme, Is.EqualTo(ControlScheme.ArrowsMove));
            Assert.That(ControlPreferences.IsSettingsOpen, Is.False);
        }

        [Test]
        public void ControlPanel_RendersThreeSchemesForVisualReview()
        {
            var root = new GameObject("SettingsRender", typeof(RectTransform), typeof(Canvas));
            spawned.Add(root);
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1280, 720);
            root.transform.position = new Vector3(10000, 10000, 0);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var sample = new GameObject("FontSample", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            sample.transform.SetParent(root.transform, false);
            sample.GetComponent<TMPro.TMP_Text>().font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(
                "Assets/_Project/Fonts/NotoSansKR-Regular_SDF.asset");
            var panel = ControlSchemePanel.Attach(root);
            panel.Open();
            var cameraObject = new GameObject("UiRenderCamera", typeof(Camera));
            spawned.Add(cameraObject);
            var camera = cameraObject.GetComponent<Camera>();
            camera.transform.position = root.transform.position + new Vector3(0, 0, -10);
            camera.orthographic = true;
            camera.orthographicSize = 360;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            var target = new RenderTexture(1280, 720, 24);
            spawned.Add(target);
            camera.targetTexture = target;
            canvas.worldCamera = camera;
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            spawned.Add(texture);
            var previous = RenderTexture.active;
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    panel.SetDraft((ControlScheme)i);
                    Canvas.ForceUpdateCanvases();
                    camera.Render();
                    RenderTexture.active = target;
                    texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                    texture.Apply();
                    System.IO.File.WriteAllBytes("Temp/prompt-b93-controls-" + (i + 1) + ".png", texture.EncodeToPNG());
                }
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = null;
            }
        }

        [Test]
        public void Settings_ApplyCancelDefaultsAndCycle()
        {
            var session = new SettingsSession();
            Assert.That(session.Applied.Controls, Is.EqualTo(ControlScheme.Classic));
            session.Open();
            session.Draft.Controls = ControlScheme.ArrowsMove;
            session.Cancel();
            Assert.That(session.Applied.Controls, Is.EqualTo(ControlScheme.Classic));
            session.Open();
            session.Draft.Controls = ControlScheme.WasdMove;
            session.Apply();
            Assert.That(session.Applied.Controls, Is.EqualTo(ControlScheme.WasdMove));
            session.Open();
            session.ResetDefaults();
            Assert.That(session.Draft.Controls, Is.EqualTo(ControlScheme.Classic));
            Assert.That(ControlPreferences.Cycle(ControlScheme.Classic, -1), Is.EqualTo(ControlScheme.ArrowsMove));
            Assert.That(ControlPreferences.Cycle(ControlScheme.ArrowsMove, 1), Is.EqualTo(ControlScheme.Classic));
            Assert.That(ControlPreferences.FromIndex(99), Is.EqualTo(ControlScheme.Classic));
        }

        [TestCase(1, 0)]
        [TestCase(-1, 0)]
        [TestCase(0, 1)]
        [TestCase(0, -1)]
        public void DirectionalMining_RemovesOnlyAdjacentTile(int x, int y)
        {
            var root = new GameObject("Grid", typeof(Grid));
            spawned.Add(root);
            var mapObject = new GameObject("Map", typeof(Tilemap));
            mapObject.transform.SetParent(root.transform);
            var map = mapObject.GetComponent<Tilemap>();
            var resolver = root.AddComponent<MiningTileResolver>();
            var mining = root.AddComponent<MiningSystem>();
            SetField(mining, "foregroundTilemap", map);
            SetField(mining, "tileResolver", resolver);
            var tile = ScriptableObject.CreateInstance<Tile>();
            spawned.Add(tile);
            resolver.RegisterRuntime(tile, new MiningTileDto("tile.rock.normal", "", 0, true, 1f, 0.2f, 0f, false));
            var center = new Vector3Int(-3, -5, 0);
            var target = center + new Vector3Int(x, y, 0);
            map.SetTile(target, tile);
            map.SetTile(center, tile);
            Assert.That(mining.TryStartMiningInDirection(map.GetCellCenterWorld(center), new Vector2(x, y), 1.35f), Is.True);
            mining.TickMining(1f);
            Assert.That(map.GetTile(target), Is.Null);
            Assert.That(map.GetTile(center), Is.SameAs(tile));
            map.SetTile(target, tile);
            mining.SetCellProtectionPredicate(cell => cell == target);
            Assert.That(mining.TryStartMiningInDirection(map.GetCellCenterWorld(center), new Vector2(x, y), 1.35f), Is.False);
            Assert.That(map.GetTile(target), Is.SameAs(tile));
        }

        [TestCase("Assets/_Project/Prefabs/UI/MainMenuPanel.prefab")]
        [TestCase("Assets/_Project/Prefabs/UI/SurfaceBasePanel.prefab")]
        public void ExistingSettingsView_WiresSelectionAndClosesChildFirst(string path)
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            spawned.Add(root);
            var main = root.GetComponentInChildren<MainMenuView>(true);
            var surface = root.GetComponentInChildren<SurfaceBaseView>(true);
            var values = SettingsValues.CreateDefaults();
            if (main != null) { main.SetSettingsDraft(values); main.SetSettingsVisible(true); }
            else { surface.SetSettingsDraft(values); surface.SetSettingsVisible(true); }
            var panel = root.GetComponentInChildren<ControlSchemePanel>(true);
            Assert.That(panel, Is.Not.Null);
            panel.Open();
            var keys = panel.transform.Find("ControlSchemeOverlay/KeyboardCard").childCount;
            panel.Cycle(1);
            Assert.That(panel.transform.Find("ControlSchemeOverlay/KeyboardCard").childCount, Is.EqualTo(keys));
            var draft = main != null ? main.ReadSettingsDraft(values) : surface.ReadSettingsDraft(values);
            Assert.That(draft.Controls, Is.EqualTo(ControlScheme.WasdMove));
            Assert.That(main != null ? main.TryCloseControlSchemePanel() : surface.TryCloseControlSchemePanel(), Is.True);
            Assert.That(panel.gameObject.activeSelf, Is.True);
            Assert.That(panel.IsOpen, Is.False);
            if (main != null) main.SetSettingsVisible(false); else surface.SetSettingsVisible(false);
            Assert.That(ControlPreferences.IsSettingsOpen, Is.False);
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
        }

        private static void Invoke(object target, string name)
        {
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
        }

        private static InputActionAsset CreateWasdAndArrowMoveAsset()
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = asset.AddActionMap("Player");
            var move = map.AddAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            return asset;
        }
    }
}
