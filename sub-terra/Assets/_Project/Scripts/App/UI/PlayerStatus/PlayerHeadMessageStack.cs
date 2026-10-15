using System.Collections.Generic;
using SubTerra.App.UI.FacilityNameTag;
using TMPro;
using UnityEngine;

namespace SubTerra.App.UI.PlayerStatus
{
    /// <summary>
    /// 플레이어 머리 위 문구 스택(뷰). 계산은 PlayerHeadMessageStackModel이 하고, 여기서는 이름표 그림을 만들고 옮길 뿐이다.
    /// 그림은 시설 이름표와 같은 FacilityNameTagVisual을 쓰고 MainScreen 레이어에 두어 CCTV 카메라에는 찍히지 않는다.
    /// 아래(먼저 뜬 것)부터 위로 쌓인다. 골드 팝업은 RegisterSlot으로 같은 스택에 자리를 잡는다.
    /// </summary>
    public sealed class PlayerHeadMessageStack : MonoBehaviour, IPlayerStatusWarningSink
    {
        private const float MessageFontSize = 17f;
        private const float MessageMaxWidth = 720f;

        /// <summary>머리와 스택 맨 아래 사이 여백(월드). GoldPickupPresentation.PopupBaseLift와 같은 값을 쓴다.</summary>
        public const float BaseLift = 0.12f;

        private readonly PlayerHeadMessageStackModel model = new PlayerHeadMessageStackModel();
        private readonly Dictionary<PlayerStatusEntry, FacilityNameTagVisual> visuals =
            new Dictionary<PlayerStatusEntry, FacilityNameTagVisual>();
        private Transform playerTarget;
        private Collider2D playerCollider;
        private TMP_FontAsset font;

        public PlayerHeadMessageStackModel Model => model;

        public void SetFont(TMP_FontAsset value)
        {
            font = value;
        }

        public void SetPlayer(Transform player)
        {
            playerTarget = player;
            playerCollider = player != null ? player.GetComponent<Collider2D>() : null;
        }

        public void ShowInfo(string key, string text) => model.ShowInfo(key, text);

        public void ShowWarning(string key, string text) => model.ShowWarning(key, text);

        public void SetProgress(string key, string text, float progress) => model.SetProgress(key, text, progress);

        public void Dismiss(string key) => model.Dismiss(key);

        public void RegisterSlot(string key, float heightPixels) => model.RegisterSlot(key, heightPixels);

        public void UnregisterSlot(string key) => model.UnregisterSlot(key);

        /// <summary>슬롯·문구의 현재 세로 오프셋(디자인 픽셀). 없으면 0.</summary>
        public float OffsetOf(string key)
        {
            return model.OffsetOf(key);
        }

        /// <summary>스택 기준점(플레이어 머리 위 골드 팝업 높이)에서 offsetPixels만큼 올린 월드 위치.</summary>
        public Vector3 WorldPositionOf(float offsetPixels)
        {
            return ComputeAnchor() + Vector3.up * (offsetPixels * FacilityNameTagVisual.WorldPerPixel);
        }

        private void OnEnable()
        {
            PlayerStatusWarnings.Current = this;
        }

        private void OnDisable()
        {
            if (PlayerStatusWarnings.Current == (IPlayerStatusWarningSink)this)
            {
                PlayerStatusWarnings.Current = null;
            }

            ClearVisuals();
            model.Clear();
        }

        private void OnDestroy()
        {
            ClearVisuals();
        }

        private void LateUpdate()
        {
            // unscaled 시간: 일시정지 중에도 문구가 제때 사라지고, 잔류하지 않는다.
            var dt = Time.unscaledDeltaTime;
            model.Advance(dt);

            foreach (var entry in model.Finished)
            {
                if (visuals.TryGetValue(entry, out var finishedVisual))
                {
                    finishedVisual.Destroy();
                    visuals.Remove(entry);
                }
            }

            foreach (var entry in model.Entries)
            {
                if (entry.IsSlot)
                {
                    continue;
                }

                var visual = GetOrCreateVisual(entry);
                visual.SetLabel(entry.Text);
                visual.SetGauge(entry.Progress);
                visual.SetWanted(entry.Wanted);
                visual.Tick(dt);
                if (entry.Style == PlayerStatusStyle.Warning)
                {
                    visual.SetGroupAlpha(entry.Wanted ? PlayerStatusTimeline.FlashAlpha(entry.ShownFor) : 1f);
                }

                visual.Rect.position = WorldPositionOf(entry.Offset);
            }
        }

        private FacilityNameTagVisual GetOrCreateVisual(PlayerStatusEntry entry)
        {
            if (visuals.TryGetValue(entry, out var existing) && existing.IsAlive)
            {
                return existing;
            }

            var isWarning = entry.Style == PlayerStatusStyle.Warning;
            var palette = isWarning ? FacilityNameTagPalette.Warning : FacilityNameTagPalette.Teal;
            var created = FacilityNameTagVisual.CreateWorld(
                transform,
                font,
                FacilityNameTagLayers.MainScreen,
                "PlayerStatus_" + entry.Key,
                palette,
                MessageFontSize,
                MessageMaxWidth);
            if (isWarning)
            {
                created.SetIcon(PlayerStatusWarningIcon.Icon);
            }

            visuals[entry] = created;
            return created;
        }

        private Vector3 ComputeAnchor()
        {
            if (playerTarget == null)
            {
                return transform.position;
            }

            var top = playerCollider != null
                ? playerCollider.bounds.max.y
                : playerTarget.position.y;
            return new Vector3(playerTarget.position.x, top, playerTarget.position.z)
                + Vector3.up * BaseLift;
        }

        private void ClearVisuals()
        {
            foreach (var pair in visuals)
            {
                pair.Value.Destroy();
            }

            visuals.Clear();
        }
    }
}
