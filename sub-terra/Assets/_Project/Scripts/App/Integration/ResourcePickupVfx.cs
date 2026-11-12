using System;
using System.Collections.Generic;
using SubTerra.App.Core.Data;
using SubTerra.App.UI.FacilityNameTag;
using SubTerra.App.UI.PlayerStatus;
using SubTerra.Gameplay.Mining;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// 지급이 확정된 채굴 결과 위에 자원 아이콘 홀로그램을 띄운다(청록 덮개 → 위에서 아래로 스캔 → 원래 색).
    /// 엔진 연료는 크게 그리고 반짝임 3개와, 스캔이 끝난 뒤 따뜻한 후광 한 번을 더한다.
    /// 보상·소리·채굴 파편·골드 팝업은 만들지 않는다. 아이콘은 머리 위 수확 문구(PlayerStatusEntry)에 붙어
    /// 함께 뜨고 함께 사라진다. 모션 감소 설정은 보지 않는다.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public sealed class ResourcePickupVfx : MonoBehaviour
    {
        public const int Capacity = 8;
        public const float NormalSize = 0.46f;
        public const float RareSize = 0.62f;
        /// <summary>문구 프레임과 아이콘 사이 간격(디자인 픽셀). 프로젝터 빔이 이 사이를 채운다.</summary>
        public const float GapPixels = 14f;
        /// <summary>브래킷·둥실거림 때문에 아이콘 위에 더 비워 두는 높이(디자인 픽셀).</summary>
        public const float TopMarginPixels = 12f;

        [SerializeField, Range(ResourceHologramTimeline.MinScanSeconds, ResourceHologramTimeline.MaxScanSeconds)]
        private float scanSeconds = ResourceHologramTimeline.DefaultScanSeconds;

        private readonly ResourceHologramIcon[] icons = new ResourceHologramIcon[Capacity];
        private readonly Dictionary<Sprite, ResourceIconSlices> slices = new Dictionary<Sprite, ResourceIconSlices>();
        private MiningSystem mining;
        private PlayerHeadMessageStack stack;
        private Func<string, Sprite> iconResolver;
        private string pendingId;
        private int pendingQuantity;
        private string confirmedId;
        private int confirmedQuantity;
        private int next;

        public bool ManualTick { get; set; }
        public float ScanSeconds
        {
            get => scanSeconds;
            set => scanSeconds = ResourceHologramTimeline.ClampScan(value);
        }

        public int ActiveCount
        {
            get
            {
                var n = 0;
                foreach (var icon in icons) if (icon != null && icon.IsActive) n++;
                return n;
            }
        }

        public int AllocatedCount
        {
            get
            {
                var n = 0;
                foreach (var icon in icons) if (icon != null) n++;
                return n;
            }
        }

        public IReadOnlyList<ResourceHologramIcon> Icons => icons;

        public void BindTo(MiningSystem source, PlayerHeadMessageStack messages, Func<string, Sprite> resolveIcon)
        {
            Unbind();
            mining = source;
            stack = messages;
            iconResolver = resolveIcon;
            if (mining == null)
            {
                return;
            }

            mining.TileMined += OnMined;
            mining.ProgressChanged += OnProgress;
        }

        public void Unbind()
        {
            if (mining != null)
            {
                mining.TileMined -= OnMined;
                mining.ProgressChanged -= OnProgress;
            }

            mining = null;
            iconResolver = null;
            ClearPending();
            foreach (var icon in icons)
            {
                if (icon != null && icon.IsActive) Release(icon);
            }

            stack = null;
        }

        /// <summary>채굴 지급 결과를 맡겨 둔다. 수량 0 이하(실패·0개·골드만 있는 블록)는 아이콘을 만들지 않는다.</summary>
        public void QueueAccepted(string itemId, int quantity)
        {
            pendingId = quantity > 0 ? itemId : null;
            pendingQuantity = Mathf.Max(0, quantity);
        }

        private void OnProgress(MiningProgressState state)
        {
            switch (state.Phase)
            {
                case MiningPhase.Failed:
                case MiningPhase.Cancelled:
                    ClearPending();
                    break;
                case MiningPhase.Completed:
                    // 머리 위 문구 쪽 Completed 처리가 끝난 뒤(구독 순서)에도 항목이 없었다면 마지막으로 한 번 더 시도한다.
                    TryPlayConfirmed();
                    ClearPending();
                    break;
            }
        }

        private void OnMined(Vector3Int cell, MiningTileDto tile)
        {
            var id = pendingId;
            var quantity = pendingQuantity;
            QueueAccepted(null, 0);
            confirmedId = null;
            confirmedQuantity = 0;
            if (quantity <= 0 || !string.Equals(id, tile.mineralId, StringComparison.Ordinal))
            {
                return;
            }

            confirmedId = id;
            confirmedQuantity = quantity;
            TryPlayConfirmed();
        }

        private void TryPlayConfirmed()
        {
            if (confirmedId == null)
            {
                return;
            }

            if (PlayAccepted(confirmedId, confirmedQuantity))
            {
                confirmedId = null;
                confirmedQuantity = 0;
            }
        }

        private void ClearPending()
        {
            pendingId = null;
            pendingQuantity = 0;
            confirmedId = null;
            confirmedQuantity = 0;
        }

        /// <summary>지금 떠 있는 채굴 수확 문구(entryKey) 위에 아이콘을 띄운다. 항목이 없거나 아이콘을 못 찾으면 false.</summary>
        public bool PlayAccepted(string itemId, int quantity, string entryKey = MiningProgressHud.ProgressKey)
        {
            if (!isActiveAndEnabled || stack == null || quantity <= 0 || string.IsNullOrEmpty(itemId))
            {
                return false;
            }

            var sprite = iconResolver != null ? iconResolver(itemId) : null;
            if (sprite == null)
            {
                return false;
            }

            var entry = stack.Model.FindShown(entryKey);
            if (entry == null)
            {
                return false;
            }

            // 같은 항목에 붙어 있던 이전 아이콘은 새 아이콘이 대신한다.
            foreach (var other in icons)
            {
                if (other != null && other.IsActive && ReferenceEquals(other.Owner, entry))
                {
                    Release(other);
                }
            }

            var icon = icons[next];
            if (icon == null)
            {
                icons[next] = icon = new ResourceHologramIcon(transform);
            }
            else if (icon.IsActive)
            {
                Release(icon);
            }

            next = (next + 1) % Capacity;
            if (!slices.TryGetValue(sprite, out var cut) || cut.Rows == null)
            {
                slices[sprite] = cut = ResourceIconSlices.Build(sprite);
            }

            var rare = itemId == DataIds.RareItems.EngineFuel;
            var size = rare ? RareSize : NormalSize;
            var headroom = Mathf.Ceil(size / FacilityNameTagVisual.WorldPerPixel) + GapPixels + TopMarginPixels;
            var version = stack.Model.AttachDecoration(entry, headroom);
            icon.Begin(cut, size, GapPixels * FacilityNameTagVisual.WorldPerPixel, rare, entry, version, scanSeconds);
            Place(icon, entry);
            return true;
        }

        private void LateUpdate()
        {
            if (!ManualTick)
            {
                Tick(Time.unscaledDeltaTime);
            }
        }

        private void OnDisable() => Unbind();

        private void OnDestroy()
        {
            Unbind();
            foreach (var icon in icons)
            {
                if (icon != null) icon.Destroy();
            }

            foreach (var cut in slices.Values) cut.Dispose();
            slices.Clear();
        }

        public void Tick(float deltaSeconds)
        {
            var dt = PlayerStatusTimeline.ClampStep(deltaSeconds);
            foreach (var icon in icons)
            {
                if (icon == null || !icon.IsActive)
                {
                    continue;
                }

                var entry = icon.Owner;
                if (stack == null || entry == null || !stack.Model.Contains(entry))
                {
                    icon.Hide();
                    continue;
                }

                if (entry.DecorationVersion != icon.Version)
                {
                    // 다음 채굴이 같은 항목을 진행 문구로 되돌렸다. 짧게 흐려지며 물러난다.
                    icon.StartRelease();
                }

                Place(icon, entry);
                var visibility = entry.Wanted ? 1f : FacilityNameTagTimeline.Evaluate(entry.Level, false).TextAlpha;
                icon.Advance(dt, visibility);
            }
        }

        private void Place(ResourceHologramIcon icon, PlayerStatusEntry entry)
        {
            icon.SetPosition(stack.WorldPositionOf(entry.Offset + entry.Height));
        }

        private void Release(ResourceHologramIcon icon)
        {
            if (stack != null)
            {
                stack.Model.DetachDecoration(icon.Owner, icon.Version);
            }

            icon.Hide();
        }
    }
}
