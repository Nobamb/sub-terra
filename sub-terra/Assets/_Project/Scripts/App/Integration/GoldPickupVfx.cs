using System.Collections.Generic;
using SubTerra.Gameplay.Mining;
using SubTerra.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// 골드 지급이 확정된 채굴 칸에서 금화를 솟구치게 하고,
    /// 플레이어 머리 위에 획득 문구를 띄운다. HUD Canvas 자식으로 두지 않는다.
    /// </summary>
    public sealed class GoldPickupVfx : MonoBehaviour
    {
        public const int WorldSortingOrder = 120;

        [SerializeField] private Sprite coinSprite;
        [SerializeField] private TMP_FontAsset pickupFont;
        [SerializeField] private Tilemap foregroundTilemap;
        [SerializeField] private float coinWorldScale = GoldPickupPresentation.CoinWorldScale;
        [SerializeField] private float textFontSize = 32f;

        private MiningSystem boundSystem;
        private Transform playerTarget;
        private Collider2D playerCollider;
        private int pendingGold;
        private Transform worldRoot;
        private readonly List<CoinAnim> coins = new List<CoinAnim>(5);
        private TextAnim textAnim;

        public int PendingGold => pendingGold;
        public int ActiveCoinCount => coins.Count;
        public bool IsTextPlaying => textAnim != null && textAnim.Root != null;

        public void BindTo(MiningSystem system, Transform player, Tilemap tilemap = null)
        {
            if (boundSystem != null)
            {
                boundSystem.TileMined -= OnTileMined;
                boundSystem.ProgressChanged -= OnProgressChanged;
            }

            boundSystem = system;
            playerTarget = player;
            playerCollider = playerTarget != null
                ? playerTarget.GetComponent<Collider2D>()
                : null;
            if (tilemap != null)
            {
                foregroundTilemap = tilemap;
            }

            if (foregroundTilemap == null)
            {
                foregroundTilemap = FindForegroundTilemap();
            }

            if (boundSystem != null)
            {
                boundSystem.TileMined += OnTileMined;
                boundSystem.ProgressChanged += OnProgressChanged;
            }
        }

        public void SetPendingGold(int amount)
        {
            pendingGold = amount > 0 ? amount : 0;
        }

        public void Play(int goldAmount, Vector3 origin, Vector3 playerHead)
        {
            if (goldAmount <= 0)
            {
                return;
            }

            EnsureWorldRoot();
            SpawnCoins(origin);
            SpawnText(goldAmount, playerHead);
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (unscaledDeltaTime < 0f)
            {
                unscaledDeltaTime = 0f;
            }

            TickCoins(unscaledDeltaTime);
            TickText(unscaledDeltaTime);
        }

        private void Update()
        {
            Tick(Time.unscaledDeltaTime);
        }

        private void OnDisable()
        {
            if (boundSystem != null)
            {
                boundSystem.TileMined -= OnTileMined;
                boundSystem.ProgressChanged -= OnProgressChanged;
                boundSystem = null;
            }
        }

        private void OnDestroy()
        {
            ClearAll();
            if (worldRoot != null)
            {
                DestroyHelper(worldRoot.gameObject);
                worldRoot = null;
            }
        }

        private void OnTileMined(Vector3Int cell, MiningTileDto tile)
        {
            if (pendingGold <= 0)
            {
                return;
            }

            int gold = pendingGold;
            pendingGold = 0;
            Play(gold, CellCenter(cell), HeadPosition());
        }

        private void OnProgressChanged(MiningProgressState state)
        {
            if (state.Phase == MiningPhase.Failed || state.Phase == MiningPhase.Cancelled)
            {
                pendingGold = 0;
            }
        }

        private void SpawnCoins(Vector3 origin)
        {
            if (coinSprite == null)
            {
                return;
            }

            float baseScale = coinWorldScale > 0f
                ? coinWorldScale
                : GoldPickupPresentation.CoinWorldScale;
            float scaleRatio = baseScale / GoldPickupPresentation.CoinWorldScale;
            for (var index = 0; index < GoldPickupPresentation.CoinCount; index++)
            {
                var coinObject = new GameObject("GoldPickupCoin");
                coinObject.transform.SetParent(worldRoot, false);
                var renderer = coinObject.AddComponent<SpriteRenderer>();
                renderer.sprite = coinSprite;
                renderer.sortingOrder = WorldSortingOrder + GoldPickupPresentation.CoinSortingBias(index);
                renderer.color = new Color(1f, 1f, 1f, 0f);
                float scale = GoldPickupPresentation.CoinScale(index) * scaleRatio;
                coinObject.transform.localScale = new Vector3(scale, scale, 1f);

                var anim = new CoinAnim
                {
                    Renderer = renderer,
                    Index = index,
                    Origin = origin,
                    Delay = GoldPickupPresentation.CoinDelay(index),
                    Duration = GoldPickupPresentation.CoinFlightDuration(index),
                    Elapsed = 0f
                };
                anim.Renderer.transform.position = GoldPickupPresentation.CoinStart(origin, index);
                coins.Add(anim);
            }
        }

        private void SpawnText(int goldAmount, Vector3 playerHead)
        {
            ClearText();
            string label = GoldPickupPresentation.FormatPickupText(goldAmount);
            if (string.IsNullOrEmpty(label) || pickupFont == null)
            {
                return;
            }

            var root = new GameObject("GoldPickupText", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            root.transform.SetParent(worldRoot, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = WorldSortingOrder + 1;
            canvas.additionalShaderChannels =
                AdditionalCanvasShaderChannels.TexCoord1
                | AdditionalCanvasShaderChannels.Normal
                | AdditionalCanvasShaderChannels.Tangent;

            var group = root.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(480f, 64f);
            root.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            root.transform.position = playerHead;

            var textObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(root.transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = pickupFont;
            text.fontSize = textFontSize > 0f ? textFontSize : 32f;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            text.text = label;
            text.color = GoldPickupPresentation.FillColor;
            text.outlineWidth = 0f;

            textAnim = new TextAnim
            {
                Root = root,
                Group = group,
                Elapsed = 0f
            };
        }

        private void TickCoins(float dt)
        {
            for (var index = coins.Count - 1; index >= 0; index--)
            {
                CoinAnim anim = coins[index];
                if (anim.Renderer == null)
                {
                    coins.RemoveAt(index);
                    continue;
                }

                anim.Elapsed += dt;
                float local = anim.Elapsed - anim.Delay;
                if (local < 0f)
                {
                    continue;
                }

                float duration = anim.Duration > 0f
                    ? anim.Duration
                    : GoldPickupPresentation.CoinFlightDuration(anim.Index);
                anim.Renderer.transform.position = GoldPickupPresentation.EvaluateCoinPosition(
                    anim.Origin,
                    anim.Index,
                    local);
                float height = GoldPickupPresentation.EvaluateCoinHeightNormalized(anim.Index, local);
                Color color = anim.Renderer.color;
                color.a = GoldPickupPresentation.CoinAlpha(height);
                anim.Renderer.color = color;

                if (local >= duration)
                {
                    DestroyHelper(anim.Renderer.gameObject);
                    coins.RemoveAt(index);
                }
            }
        }

        private void TickText(float dt)
        {
            if (textAnim == null || textAnim.Root == null)
            {
                textAnim = null;
                return;
            }

            textAnim.Elapsed += dt;
            GoldPickupPresentation.EvaluateText(textAnim.Elapsed, out float alpha, out float yOffset);
            if (textAnim.Group != null)
            {
                textAnim.Group.alpha = alpha;
            }

            textAnim.Root.transform.position = HeadPosition() + Vector3.up * yOffset;
            if (textAnim.Elapsed >= GoldPickupPresentation.TextDuration)
            {
                ClearText();
            }
        }

        private Vector3 CellCenter(Vector3Int cell)
        {
            if (foregroundTilemap != null)
            {
                return foregroundTilemap.GetCellCenterWorld(cell);
            }

            return new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
        }

        private Vector3 HeadPosition()
        {
            if (playerTarget == null)
            {
                return Vector3.zero;
            }

            float top = playerCollider != null
                ? playerCollider.bounds.max.y
                : playerTarget.position.y;
            return new Vector3(playerTarget.position.x, top + 0.12f, playerTarget.position.z);
        }

        private void EnsureWorldRoot()
        {
            if (worldRoot != null)
            {
                return;
            }

            var root = new GameObject("GoldPickupWorldRoot");
            worldRoot = root.transform;
        }

        private void ClearText()
        {
            if (textAnim != null && textAnim.Root != null)
            {
                DestroyHelper(textAnim.Root);
            }

            textAnim = null;
        }

        private void ClearAll()
        {
            for (var index = 0; index < coins.Count; index++)
            {
                if (coins[index].Renderer != null)
                {
                    DestroyHelper(coins[index].Renderer.gameObject);
                }
            }

            coins.Clear();
            ClearText();
        }

        private static Tilemap FindForegroundTilemap()
        {
            Tilemap[] maps = FindObjectsByType<Tilemap>(FindObjectsInactive.Include);
            for (var index = 0; index < maps.Length; index++)
            {
                Tilemap map = maps[index];
                if (map != null && map.name.IndexOf("Foreground", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return map;
                }
            }

            return maps.Length > 0 ? maps[0] : null;
        }

        private static void DestroyHelper(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private sealed class CoinAnim
        {
            public SpriteRenderer Renderer;
            public int Index;
            public Vector3 Origin;
            public float Delay;
            public float Duration;
            public float Elapsed;
        }

        private sealed class TextAnim
        {
            public GameObject Root;
            public CanvasGroup Group;
            public float Elapsed;
        }
    }
}
