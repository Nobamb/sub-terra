using System;
using System.Collections.Generic;
using SubTerra.App.Core.Data;
using SubTerra.Gameplay.Building;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// 플레이어가 시설에 가까워지면 시설명 말풍선을 띄운다.
    /// 버팀목·사다리는 제외한다.
    /// </summary>
    public sealed class FacilityProximityLabelController : MonoBehaviour
    {
        public const float DefaultRange = 2f;

        [SerializeField] private Transform player;
        [SerializeField] private TMP_FontAsset koreanFont;
        [SerializeField, Min(0.1f)] private float range = DefaultRange;
        [SerializeField] private Vector3 bubbleOffset = new Vector3(0f, 1.15f, 0f);

        private readonly Dictionary<EntityId, NameBubble> bubbles = new Dictionary<EntityId, NameBubble>();
        private Transform bubbleRoot;

        public int VisibleBubbleCount { get; private set; }
        public TMP_FontAsset ActiveFont => koreanFont;

        public void SetPlayer(Transform origin)
        {
            player = origin;
        }

        public void SetFont(TMP_FontAsset font)
        {
            if (!IsKoreanFont(font))
            {
                return;
            }

            koreanFont = font;
            ApplyFontToBubbles();
        }

        public void Refresh()
        {
            ResolvePlayer();
            EnsureKoreanFont();
            EnsureBubbleRoot();
            VisibleBubbleCount = 0;

            if (player == null)
            {
                HideAll();
                return;
            }

            var instances = FindObjectsByType<BuildingInstance>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            var seen = new HashSet<EntityId>();
            var squaredRange = range * range;

            for (var i = 0; i < instances.Length; i++)
            {
                var instance = instances[i];
                if (instance == null || !ItemDisplayNames.ShowsProximityName(instance.BuildingId))
                {
                    continue;
                }

                var id = instance.GetEntityId();
                seen.Add(id);
                var delta = (Vector2)(instance.transform.position - player.position);
                var inRange = delta.sqrMagnitude <= squaredRange;
                var bubble = GetOrCreateBubble(id);
                if (!inRange)
                {
                    bubble.SetVisible(false);
                    continue;
                }

                bubble.SetLabel(ItemDisplayNames.Building(instance.BuildingId));
                bubble.Follow(instance.transform.position + bubbleOffset);
                bubble.SetVisible(true);
                VisibleBubbleCount++;
            }

            RemoveStale(seen);
        }

        public bool TryGetVisibleLabel(string buildingId, out string label)
        {
            label = string.Empty;
            foreach (var pair in bubbles)
            {
                if (!pair.Value.IsVisible)
                {
                    continue;
                }

                if (pair.Value.Label == ItemDisplayNames.Building(buildingId))
                {
                    label = pair.Value.Label;
                    return true;
                }
            }

            return false;
        }

        private void LateUpdate()
        {
            Refresh();
        }

        private void OnDisable()
        {
            HideAll();
            VisibleBubbleCount = 0;
        }

        private void ResolvePlayer()
        {
            if (player != null)
            {
                return;
            }

            var movement = FindFirstObjectByType<SubTerra.Gameplay.Player.PlayerMovement>();
            if (movement != null)
            {
                player = movement.transform;
            }
        }

        private void EnsureKoreanFont()
        {
            if (IsKoreanFont(koreanFont))
            {
                return;
            }

            koreanFont = ResolveKoreanFont();
            ApplyFontToBubbles();
        }

        private void ApplyFontToBubbles()
        {
            if (!IsKoreanFont(koreanFont))
            {
                return;
            }

            foreach (var pair in bubbles)
            {
                if (pair.Value != null)
                {
                    pair.Value.SetFont(koreanFont);
                }
            }
        }

        public static TMP_FontAsset ResolveKoreanFont()
        {
            var loaded = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            for (var i = 0; i < loaded.Length; i++)
            {
                if (IsKoreanFont(loaded[i]))
                {
                    return loaded[i];
                }
            }

            var texts = FindObjectsByType<TMP_Text>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < texts.Length; i++)
            {
                var text = texts[i];
                if (text != null && IsKoreanFont(text.font))
                {
                    return text.font;
                }
            }

            return TMP_Settings.defaultFontAsset;
        }

        public static bool IsKoreanFont(TMP_FontAsset font)
        {
            return font != null
                && font.name.IndexOf("NotoSansKR", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void EnsureBubbleRoot()
        {
            if (bubbleRoot != null)
            {
                return;
            }

            var existing = transform.Find("FacilityNameBubbles");
            if (existing != null)
            {
                bubbleRoot = existing;
                return;
            }

            var root = new GameObject("FacilityNameBubbles");
            root.transform.SetParent(transform, false);
            bubbleRoot = root.transform;
        }

        private NameBubble GetOrCreateBubble(EntityId id)
        {
            if (bubbles.TryGetValue(id, out var existing) && existing != null && existing.IsAlive)
            {
                return existing;
            }

            var created = NameBubble.Create(bubbleRoot, koreanFont);
            bubbles[id] = created;
            return created;
        }

        private void HideAll()
        {
            foreach (var pair in bubbles)
            {
                if (pair.Value != null)
                {
                    pair.Value.SetVisible(false);
                }
            }
        }

        private void RemoveStale(HashSet<EntityId> seen)
        {
            var stale = new List<EntityId>();
            foreach (var pair in bubbles)
            {
                if (seen.Contains(pair.Key) && pair.Value != null && pair.Value.IsAlive)
                {
                    continue;
                }

                if (pair.Value != null)
                {
                    pair.Value.Destroy();
                }

                stale.Add(pair.Key);
            }

            for (var i = 0; i < stale.Count; i++)
            {
                bubbles.Remove(stale[i]);
            }
        }

        private sealed class NameBubble
        {
            private readonly GameObject root;
            private readonly TMP_Text text;

            public string Label { get; private set; } = string.Empty;
            public bool IsVisible { get; private set; }
            public bool IsAlive => root != null;

            private NameBubble(GameObject root, TMP_Text text)
            {
                this.root = root;
                this.text = text;
            }

            public static NameBubble Create(Transform parent, TMP_FontAsset font)
            {
                var root = new GameObject("FacilityNameBubble", typeof(RectTransform));
                if (parent != null)
                {
                    root.transform.SetParent(parent, false);
                }

                var canvas = root.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.sortingOrder = 80;
                canvas.overrideSorting = true;

                var rect = root.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(180f, 56f);
                root.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

                var body = new GameObject("Body", typeof(RectTransform), typeof(Image));
                body.transform.SetParent(root.transform, false);
                var bodyRect = body.GetComponent<RectTransform>();
                bodyRect.anchorMin = Vector2.zero;
                bodyRect.anchorMax = Vector2.one;
                bodyRect.offsetMin = new Vector2(0f, 10f);
                bodyRect.offsetMax = Vector2.zero;
                var bodyImage = body.GetComponent<Image>();
                bodyImage.color = new Color(0.06f, 0.09f, 0.13f, 0.92f);
                bodyImage.raycastTarget = false;

                var tail = new GameObject("Tail", typeof(RectTransform), typeof(Image));
                tail.transform.SetParent(root.transform, false);
                var tailRect = tail.GetComponent<RectTransform>();
                tailRect.anchorMin = new Vector2(0.5f, 0f);
                tailRect.anchorMax = new Vector2(0.5f, 0f);
                tailRect.pivot = new Vector2(0.5f, 1f);
                tailRect.anchoredPosition = new Vector2(0f, 12f);
                tailRect.sizeDelta = new Vector2(14f, 12f);
                tailRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
                var tailImage = tail.GetComponent<Image>();
                tailImage.color = new Color(0.06f, 0.09f, 0.13f, 0.92f);
                tailImage.raycastTarget = false;

                var textGo = new GameObject("Label", typeof(RectTransform));
                textGo.transform.SetParent(body.transform, false);
                var textRect = textGo.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(8f, 4f);
                textRect.offsetMax = new Vector2(-8f, -4f);
                var tmp = textGo.AddComponent<TextMeshProUGUI>();
                tmp.fontSize = 22f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = new Color(0.96f, 0.98f, 1f, 1f);
                tmp.raycastTarget = false;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.overflowMode = TextOverflowModes.Ellipsis;
                ApplyFont(tmp, font);

                root.SetActive(false);
                return new NameBubble(root, tmp);
            }

            public void SetFont(TMP_FontAsset font)
            {
                ApplyFont(text, font);
            }

            private static void ApplyFont(TMP_Text target, TMP_FontAsset font)
            {
                if (target == null || font == null)
                {
                    return;
                }

                target.font = font;
            }

            public void SetLabel(string label)
            {
                Label = label ?? string.Empty;
                if (text != null)
                {
                    text.text = Label;
                }
            }

            public void Follow(Vector3 worldPosition)
            {
                if (root != null)
                {
                    root.transform.position = worldPosition;
                }
            }

            public void SetVisible(bool visible)
            {
                IsVisible = visible;
                if (root != null && root.activeSelf != visible)
                {
                    root.SetActive(visible);
                }
            }

            public void Destroy()
            {
                if (root == null)
                {
                    return;
                }

                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(root);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }
    }
}
