using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// 통합 장면의 그레이박스 지형을 읽을 수 있게 만들고 표식 범례를 HUD에 배치한다.
    /// Gameplay 계산이나 Runtime Prefab 내부는 변경하지 않는다.
    /// </summary>
    public static class PhaseOUiPolishBuilder
    {
        public const string IntegrationScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";
        public const string TerrainVisualFolder =
            "Assets/_Project/Visuals/Graybox/Terrain";

        [MenuItem("SubTerra/UI/Build Phase O UI Polish")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + Build());
        }

        public static string Build()
        {
            var previousScene = SceneManager.GetActiveScene().path;
            EnsureFolder(TerrainVisualFolder);
            var sprites = CreateTerrainSprites();
            var tiles = ApplyTerrainSprites(sprites);

            var scene = EditorSceneManager.OpenScene(
                IntegrationScenePath,
                OpenSceneMode.Single);
            PolishTerrain(scene, tiles);
            EnsureTerrainLegend(scene);
            PolishWorldMarkers(scene, sprites[0]);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!string.IsNullOrEmpty(previousScene)
                && previousScene != IntegrationScenePath
                && File.Exists(previousScene))
            {
                EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
            }

            return "Phase O centered menus, readable terrain, route boundaries, and terrain legend built.";
        }

        private static Sprite[] CreateTerrainSprites()
        {
            return new[]
            {
                CreatePatternSprite("Rock", TerrainPattern.Rock),
                CreatePatternSprite("Copper", TerrainPattern.Copper),
                CreatePatternSprite("Iron", TerrainPattern.Iron),
                CreatePatternSprite("Lithium", TerrainPattern.Lithium),
                CreatePatternSprite("GasPocket", TerrainPattern.Gas),
                CreatePatternSprite("LockedSignal", TerrainPattern.Locked)
            };
        }

        private static Tile[] ApplyTerrainSprites(Sprite[] sprites)
        {
            var names = new[]
            {
                "Rock", "Copper", "Iron", "Lithium", "GasPocket", "LockedSignal"
            };
            var result = new Tile[names.Length];
            for (var i = 0; i < names.Length; i++)
            {
                var path = "Assets/_Project/Tilemaps/DemoWorld/" + names[i] + ".asset";
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
                if (tile == null)
                {
                    throw new System.InvalidOperationException("Terrain tile missing: " + path);
                }

                tile.sprite = sprites[i];
                tile.colliderType = Tile.ColliderType.Grid;
                EditorUtility.SetDirty(tile);
                result[i] = tile;
            }

            // BoundaryRock keeps a darker Rock-pattern look so edges stay visually distinct.
            var boundaryPath = "Assets/_Project/Tilemaps/DemoWorld/BoundaryRock.asset";
            var boundary = AssetDatabase.LoadAssetAtPath<Tile>(boundaryPath);
            if (boundary != null && sprites.Length > 0)
            {
                boundary.sprite = sprites[0];
                boundary.color = new Color(0.45f, 0.48f, 0.55f, 1f);
                boundary.colliderType = Tile.ColliderType.Grid;
                EditorUtility.SetDirty(boundary);
            }

            return result;
        }

        private static void PolishTerrain(Scene scene, Tile[] tiles)
        {
            var tilemap = FindInScene<Tilemap>(scene, "ForegroundTilemap");
            if (tilemap == null)
            {
                throw new System.InvalidOperationException("ForegroundTilemap missing.");
            }

            // Phase B owns the authored 40m mine layer (including BoundaryRock edges).
            // Never ClearAllTiles or repaint Rock fill — only overlay tutorial markers + polish.
            if (tiles != null && tiles.Length >= 6)
            {
                var elevatorProtectedBlock = AssetDatabase.LoadAssetAtPath<TileBase>(
                    "Assets/_Project/Tilemaps/DemoWorld/ElevatorProtectedBlock.asset");
                tilemap.SetTile(new Vector3Int(-8, -2, 0), elevatorProtectedBlock);
                tilemap.SetTile(new Vector3Int(-7, -2, 0), elevatorProtectedBlock);
                tilemap.SetTile(new Vector3Int(-6, -2, 0), elevatorProtectedBlock);
                tilemap.SetTile(new Vector3Int(-7, -3, 0), tiles[1]);
                tilemap.SetTile(new Vector3Int(-3, -3, 0), tiles[2]); // Iron
                tilemap.SetTile(new Vector3Int(2, -5, 0), tiles[3]); // Lithium
                tilemap.SetTile(new Vector3Int(8, -4, 0), tiles[4]); // GasPocket
                tilemap.SetTile(new Vector3Int(14, -7, 0), tiles[5]); // LockedSignal
            }

            tilemap.RefreshAllTiles();
            EditorUtility.SetDirty(tilemap);
            var tileRenderer = tilemap.GetComponent<TilemapRenderer>();
            if (tileRenderer != null)
            {
                tileRenderer.sortingOrder = 0;
                EditorUtility.SetDirty(tileRenderer);
            }

            var tileCollider = tilemap.GetComponent<TilemapCollider2D>();
            if (tileCollider != null)
            {
                tileCollider.compositeOperation =
                    Collider2D.CompositeOperation.None;
                EditorUtility.SetDirty(tileCollider);
            }

            var terrainBody = tilemap.GetComponent<Rigidbody2D>();
            if (terrainBody != null)
            {
                terrainBody.bodyType = RigidbodyType2D.Static;
                EditorUtility.SetDirty(terrainBody);
            }

            var composite = tilemap.GetComponent<CompositeCollider2D>();
            if (composite != null)
            {
                Object.DestroyImmediate(composite);
            }

            var safetyGround = FindInScene<Transform>(scene, "SafetyGround");
            if (safetyGround != null)
            {
                Object.DestroyImmediate(safetyGround.gameObject);
            }

            var camera = FindInScene<Camera>(scene, "Main Camera");
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.025f, 0.045f, 0.075f, 1f);
                EditorUtility.SetDirty(camera);
            }
        }

        private static void EnsureTerrainLegend(Scene scene)
        {
            var canvas = FindInScene<Canvas>(scene, "HUDCanvas");
            if (canvas == null)
            {
                var canvasGo = new GameObject(
                    "HUDCanvas",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
            }

            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var existing = canvas.transform.Find("TerrainLegendPanel");
            GameObject panel;
            if (existing == null)
            {
                panel = new GameObject(
                    "TerrainLegendPanel",
                    typeof(RectTransform),
                    typeof(Image));
                panel.transform.SetParent(canvas.transform, false);
            }
            else
            {
                panel = existing.gameObject;
            }

            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 24f);
            panelRect.sizeDelta = new Vector2(1280f, 72f);
            var panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0.035f, 0.06f, 0.09f, 0.94f);
            panelImage.raycastTarget = false;

            var textTransform = panel.transform.Find("LegendText");
            GameObject textGo;
            if (textTransform == null)
            {
                textGo = new GameObject("LegendText", typeof(RectTransform));
                textGo.transform.SetParent(panel.transform, false);
            }
            else
            {
                textGo = textTransform.gameObject;
            }

            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(18f, 10f);
            textRect.offsetMax = new Vector2(-18f, -10f);
            var text = textGo.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                text = textGo.AddComponent<TextMeshProUGUI>();
            }

            var font = KoreanFontAssetUtility.GetOrCreateKoreanFontAsset();
            if (font != null)
            {
                text.font = font;
            }

            text.text =
                "[##] 암반=기본 채굴  |  (Cu) 구리=판매  |  [Fe] 철=중량  |  "
                + "<Li> 리튬=희귀  |  ~~~ 가스=위험  |  X 봉인 신호=진입 불가";
            text.fontSize = 18f;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            EditorUtility.SetDirty(panel);
            EditorUtility.SetDirty(text);
        }

        private static void PolishWorldMarkers(Scene scene, Sprite markerSprite)
        {
            var player = FindInScene<Transform>(scene, "Player");
            if (player != null)
            {
                player.position = new Vector3(-9.5f, -0.65f, 0f);
                var playerCollider = player.GetComponent<CapsuleCollider2D>();
                if (playerCollider != null)
                {
                    playerCollider.size = new Vector2(0.6f, 0.7f);
                    playerCollider.offset = Vector2.zero;
                    EditorUtility.SetDirty(playerCollider);
                }

                var groundCheck = player.Find("GroundCheck");
                if (groundCheck != null)
                {
                    groundCheck.localPosition = new Vector3(0f, -0.37f, 0f);
                    EditorUtility.SetDirty(groundCheck);
                }

                var miningController = player.GetComponents<MonoBehaviour>()
                    .FirstOrDefault(component =>
                        component != null
                        && component.GetType().FullName
                        == "SubTerra.Gameplay.Mining.PlayerMiningController");
                if (miningController != null)
                {
                    miningController.enabled = true;
                    var serialized = new SerializedObject(miningController);
                    serialized.FindProperty("reach").floatValue = 1.35f;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(miningController);
                }

                EditorUtility.SetDirty(player);
            }

            var drone = FindInScene<Transform>(scene, "DiggerBot_Runtime");
            if (drone != null && player != null)
            {
                drone.position = player.position + new Vector3(-0.8f, 0.55f, 0f);
                drone.localScale = Vector3.one;
                EditorUtility.SetDirty(drone);
            }

            foreach (var renderer in scene.GetRootGameObjects()
                         .SelectMany(root => root.GetComponentsInChildren<SpriteRenderer>(true)))
            {
                var path = GetHierarchyPath(renderer.transform);
                if (path.Contains("Player/VisualRoot"))
                {
                    renderer.drawMode = SpriteDrawMode.Sliced;
                    renderer.size = new Vector2(0.7f, 0.7f);
                    renderer.sortingOrder = 10;
                }
                else if (path.Contains("DiggerBot_Runtime"))
                {
                    renderer.sprite = markerSprite;
                    renderer.drawMode = SpriteDrawMode.Sliced;
                    renderer.size = new Vector2(0.45f, 0.35f);
                    renderer.sortingOrder = 9;
                }
                else if (path.Contains("OutpostCore_Demo"))
                {
                    renderer.sprite = markerSprite;
                    renderer.drawMode = SpriteDrawMode.Sliced;
                    renderer.size = new Vector2(1.2f, 1.4f);
                    renderer.sortingOrder = 8;
                }

                EditorUtility.SetDirty(renderer);
            }
        }

        private static Sprite CreatePatternSprite(string name, TerrainPattern pattern)
        {
            const int size = 16;
            var path = TerrainVisualFolder + "/" + name + "Visual.asset";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    name = name + "VisualTexture",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                AssetDatabase.CreateAsset(texture, path);
            }

            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var border = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                    var shade = border ? 0.42f : PatternShade(pattern, x, y);
                    pixels[y * size + x] = new Color(shade, shade, shade, 1f);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            EditorUtility.SetDirty(texture);

            var sprite = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .FirstOrDefault();
            if (sprite == null)
            {
                sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, size, size),
                    new Vector2(0.5f, 0.5f),
                    size,
                    0,
                    SpriteMeshType.FullRect);
                sprite.name = name + "Visual";
                AssetDatabase.AddObjectToAsset(sprite, texture);
            }

            EditorUtility.SetDirty(sprite);
            return sprite;
        }

        private static float PatternShade(TerrainPattern pattern, int x, int y)
        {
            switch (pattern)
            {
                case TerrainPattern.Copper:
                    return (x - 4) * (x - 4) + (y - 5) * (y - 5) <= 5
                        || (x - 11) * (x - 11) + (y - 10) * (y - 10) <= 6
                        ? 0.48f
                        : 1f;
                case TerrainPattern.Iron:
                    return y == 4 || y == 8 || y == 12 ? 0.5f : 1f;
                case TerrainPattern.Lithium:
                    return Mathf.Abs(x - 8) + Mathf.Abs(y - 8) <= 3 ? 0.42f : 1f;
                case TerrainPattern.Gas:
                    return ((x + y) % 6 == 0) || ((x - y + 16) % 7 == 0) ? 0.45f : 1f;
                case TerrainPattern.Locked:
                    return Mathf.Abs(x - y) <= 1 || Mathf.Abs(15 - x - y) <= 1
                        ? 0.35f
                        : 1f;
                default:
                    return ((x * 3 + y * 5) % 17 == 0) ? 0.58f : 0.88f;
            }
        }

        private static string GetHierarchyPath(Transform current)
        {
            var path = current.name;
            while (current.parent != null)
            {
                current = current.parent;
                path = current.name + "/" + path;
            }

            return path;
        }

        private static T FindInScene<T>(Scene scene, string objectName)
            where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var component in root.GetComponentsInChildren<T>(true))
                {
                    if (component.name == objectName)
                    {
                        return component;
                    }
                }
            }

            return null;
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private enum TerrainPattern
        {
            Rock,
            Copper,
            Iron,
            Lithium,
            Gas,
            Locked
        }
    }
}
