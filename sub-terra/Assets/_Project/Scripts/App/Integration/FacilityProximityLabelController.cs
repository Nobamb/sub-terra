using System;
using System.Collections.Generic;
using SubTerra.App.Core.Data;
using SubTerra.App.UI.FacilityNameTag;
using SubTerra.Gameplay.Building;
using TMPro;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// 플레이어가 시설에 가까워지면 시설명 홀로그램 이름표를 띄운다.
    /// 버팀목·사다리는 제외한다. 이름표는 시설마다 하나만 만들어 재사용하고, 등장·퇴장 연출은 상태가 바뀔 때만 진행한다.
    /// </summary>
    public sealed class FacilityProximityLabelController : MonoBehaviour
    {
        public const float DefaultRange = 2f;

        [SerializeField] private Transform player;
        [SerializeField] private TMP_FontAsset koreanFont;
        [SerializeField, Min(0.1f)] private float range = DefaultRange;

        private readonly Dictionary<EntityId, NameBubble> bubbles = new Dictionary<EntityId, NameBubble>();
        private readonly List<Candidate> candidates = new List<Candidate>();
        private readonly List<Rect> accepted = new List<Rect>();
        private Transform bubbleRoot;

        public int VisibleBubbleCount { get; private set; }
        public TMP_FontAsset ActiveFont => koreanFont;
        /// <summary>등장·표시·퇴장 중으로 화면에 그려지는 이름표 수. 접히는 중인 것도 포함한다.</summary>
        public int ActiveTagCount
        {
            get
            {
                var count = 0;
                foreach (var pair in bubbles)
                {
                    count += pair.Value != null && pair.Value.IsVisible ? 1 : 0;
                }

                return count;
            }
        }

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
                TickBubbles();
                return;
            }

            var instances = FindObjectsByType<BuildingInstance>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            var seen = new HashSet<EntityId>();
            candidates.Clear();
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
                if (delta.sqrMagnitude <= squaredRange)
                {
                    candidates.Add(new Candidate(id, instance, delta.sqrMagnitude));
                }
            }

            // 가까운 시설부터 확정하고, 이미 확정된 이름표와 겹치는 먼 시설의 이름표는 숨겨 가독성을 지킨다.
            candidates.Sort(CompareByDistance);
            accepted.Clear();
            var wantedIds = new HashSet<EntityId>();
            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                var bubble = GetOrCreateBubble(candidate.Id);
                bubble.SetLabel(ItemDisplayNames.Building(candidate.Instance.BuildingId));
                var anchor = FacilityNameTagAnchor.Compute(candidate.Instance);
                var rect = TagRect(anchor, bubble.Width);
                if (Overlaps(rect))
                {
                    continue;
                }

                accepted.Add(rect);
                wantedIds.Add(candidate.Id);
                bubble.Follow(anchor);
                VisibleBubbleCount++;
            }

            foreach (var pair in bubbles)
            {
                if (pair.Value != null)
                {
                    pair.Value.SetWanted(wantedIds.Contains(pair.Key) && seen.Contains(pair.Key));
                }
            }

            RemoveStale(seen);
            TickBubbles();
        }

        public bool TryGetVisibleLabel(string buildingId, out string label)
        {
            label = string.Empty;
            foreach (var pair in bubbles)
            {
                if (!pair.Value.IsWanted)
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
            // 비활성화·씬 전환 때 남은 이름표와 연출을 모두 정리한다.
            foreach (var pair in bubbles)
            {
                if (pair.Value != null)
                {
                    pair.Value.Destroy();
                }
            }

            bubbles.Clear();
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
                    pair.Value.SetWanted(false);
                }
            }
        }

        private void TickBubbles()
        {
            var step = Time.unscaledDeltaTime;
            foreach (var pair in bubbles)
            {
                if (pair.Value != null)
                {
                    pair.Value.Tick(step);
                }
            }
        }

        // 이미 사라진 시설의 이름표는 접힌 뒤 지운다.
        private void RemoveStale(HashSet<EntityId> seen)
        {
            var stale = new List<EntityId>();
            foreach (var pair in bubbles)
            {
                var alive = pair.Value != null && pair.Value.IsAlive;
                if (alive && seen.Contains(pair.Key))
                {
                    continue;
                }

                if (alive)
                {
                    pair.Value.SetWanted(false);
                    if (pair.Value.IsVisible)
                    {
                        continue;
                    }

                    pair.Value.Destroy();
                }

                stale.Add(pair.Key);
            }

            for (var i = 0; i < stale.Count; i++)
            {
                bubbles.Remove(stale[i]);
            }
        }

        private static int CompareByDistance(Candidate left, Candidate right)
        {
            return left.SquaredDistance.CompareTo(right.SquaredDistance);
        }

        private static Rect TagRect(Vector2 anchor, float widthPixels)
        {
            var width = widthPixels * FacilityNameTagVisual.WorldPerPixel;
            var height = FacilityNameTagVisual.Height * FacilityNameTagVisual.WorldPerPixel;
            return new Rect(anchor.x - width * 0.5f, anchor.y, width, height);
        }

        private bool Overlaps(Rect rect)
        {
            for (var i = 0; i < accepted.Count; i++)
            {
                if (accepted[i].Overlaps(rect))
                {
                    return true;
                }
            }

            return false;
        }

        private readonly struct Candidate
        {
            public readonly EntityId Id;
            public readonly BuildingInstance Instance;
            public readonly float SquaredDistance;

            public Candidate(EntityId id, BuildingInstance instance, float squaredDistance)
            {
                Id = id;
                Instance = instance;
                SquaredDistance = squaredDistance;
            }
        }

        private sealed class NameBubble
        {
            private readonly FacilityNameTagVisual visual;

            public string Label => visual.LabelText;
            public bool IsWanted => visual.Wanted;
            public bool IsVisible => visual.IsVisible;
            public bool IsAlive => visual.IsAlive;
            public float Width => visual.Width;

            private NameBubble(FacilityNameTagVisual visual)
            {
                this.visual = visual;
            }

            public static NameBubble Create(Transform parent, TMP_FontAsset font)
            {
                return new NameBubble(FacilityNameTagVisual.CreateWorld(
                    parent, font, FacilityNameTagLayers.MainScreen, "FacilityNameTag"));
            }

            public void SetFont(TMP_FontAsset font)
            {
                visual.SetFont(font);
            }

            public void SetLabel(string label)
            {
                visual.SetLabel(label);
            }

            public void Follow(Vector2 anchor)
            {
                if (visual.IsAlive)
                {
                    visual.Rect.position = new Vector3(anchor.x, anchor.y, 0f);
                }
            }

            public void SetWanted(bool wanted)
            {
                visual.SetWanted(wanted);
            }

            public void Tick(float deltaSeconds)
            {
                visual.Tick(deltaSeconds);
            }

            public void Destroy()
            {
                visual.Destroy();
            }
        }
    }
}
