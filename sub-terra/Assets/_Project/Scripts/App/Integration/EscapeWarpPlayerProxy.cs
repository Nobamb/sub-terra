using System.Collections.Generic;
using SubTerra.App.UI.EmergencyEscape;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// 포탈 워프 동안 캐릭터를 "복제 그림"으로 바꿔 화면 위 연출 레이어에서 줄이고 돌린다.
    /// 월드의 스프라이트는 검은 전체화면 아래에 있어 어둡게 가려지므로, 모습을 오버레이로 옮겨 웜홀 속으로
    /// 빨려 드는 장면이 검은 화면 위에서도 보이게 한다. 콜라이더·물리가 붙은 플레이어 루트는 건드리지 않는다.
    /// 실제 렌더러는 연출 동안만 끄고 <see cref="Restore"/>로 원래 상태로 되돌린다.
    /// </summary>
    public sealed class EscapeWarpPlayerProxy
    {
        private readonly Transform player;
        private readonly Vector3 centerOffset;
        private readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>();
        private readonly List<bool> baseEnabled = new List<bool>();
        private readonly List<EscapeWarpProxySprite> sprites = new List<EscapeWarpProxySprite>();
        private bool hidden;
        private bool restored;

        public IReadOnlyList<EscapeWarpProxySprite> Sprites => sprites;

        /// <summary>캐릭터 몸통의 월드 중심. 플레이어가 옮겨지면 같이 따라간다.</summary>
        public Vector3 WorldCenter => player != null ? player.position + centerOffset : Vector3.zero;

        public EscapeWarpPlayerProxy(Transform player, Camera view)
        {
            this.player = player;
            if (player == null)
            {
                return;
            }

            var all = player.GetComponentsInChildren<SpriteRenderer>(false);
            var hasBounds = false;
            var bounds = new Bounds();
            for (var i = 0; i < all.Length; i++)
            {
                var renderer = all[i];
                if (renderer == null || !renderer.enabled || renderer.sprite == null)
                {
                    continue;
                }

                renderers.Add(renderer);
                baseEnabled.Add(renderer.enabled);
                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            centerOffset = hasBounds
                ? new Vector3(bounds.center.x - player.position.x, bounds.center.y - player.position.y, 0f)
                : Vector3.zero;

            if (view == null || !hasBounds)
            {
                return;
            }

            var center = bounds.center;
            var centerScreen = (Vector2)view.WorldToScreenPoint(center);
            var unitScreen = (Vector2)view.WorldToScreenPoint(center + Vector3.right);
            var pixelsPerUnit = Mathf.Abs(unitScreen.x - centerScreen.x);
            if (pixelsPerUnit <= 0.0001f)
            {
                return;
            }

            // 정렬 순서(레이어 → 오더)대로 쌓아 원래 겹침을 유지한다.
            var ordered = new List<SpriteRenderer>(renderers);
            ordered.Sort((a, b) => SortKey(a).CompareTo(SortKey(b)));
            for (var i = 0; i < ordered.Count; i++)
            {
                sprites.Add(Capture(ordered[i], view, centerScreen, pixelsPerUnit));
            }
        }

        /// <summary>월드의 캐릭터 스프라이트를 끈다. 오버레이의 복제 그림이 대신 보인다.</summary>
        public void HideRenderers()
        {
            if (restored)
            {
                return;
            }

            hidden = true;
            for (var i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].enabled = false;
                }
            }
        }

        public void Restore()
        {
            if (restored)
            {
                return;
            }

            restored = true;
            if (!hidden)
            {
                return;
            }

            for (var i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].enabled = baseEnabled[i];
                }
            }
        }

        private static EscapeWarpProxySprite Capture(
            SpriteRenderer renderer,
            Camera view,
            Vector2 centerScreen,
            float pixelsPerUnit)
        {
            var sprite = renderer.sprite;
            var lossy = renderer.transform.lossyScale;
            var unitSize = sprite.rect.size / sprite.pixelsPerUnit;
            var pivotScreen = (Vector2)view.WorldToScreenPoint(renderer.transform.position);
            return new EscapeWarpProxySprite
            {
                Sprite = sprite,
                Size = new Vector2(
                    unitSize.x * Mathf.Abs(lossy.x) * pixelsPerUnit,
                    unitSize.y * Mathf.Abs(lossy.y) * pixelsPerUnit),
                Pivot = new Vector2(
                    sprite.rect.width > 0f ? sprite.pivot.x / sprite.rect.width : 0.5f,
                    sprite.rect.height > 0f ? sprite.pivot.y / sprite.rect.height : 0.5f),
                Offset = pivotScreen - centerScreen,
                RotationZ = renderer.transform.eulerAngles.z,
                FlipX = (lossy.x < 0f) != renderer.flipX,
                Tint = renderer.color
            };
        }

        private static long SortKey(SpriteRenderer renderer)
        {
            return ((long)SortingLayer.GetLayerValueFromID(renderer.sortingLayerID) * 100000L)
                + renderer.sortingOrder;
        }
    }
}
