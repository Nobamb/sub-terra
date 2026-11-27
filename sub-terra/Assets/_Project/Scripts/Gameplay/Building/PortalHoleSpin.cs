using System;
using UnityEngine;

namespace SubTerra.Gameplay.Building
{
    /// <summary>
    /// B-214: 긴급탈출포탈이 켜져 있을 때(웜홀 그림)만 내부 홀이 천천히 돈다. 주기 연출이 아니라 계속 돈다.
    /// 원형 SpriteMask(홀보다 살짝 작게) 안에서 웜홀 그림 복사본만 시계방향으로 천천히 돌고(별도 소용돌이 무늬는 없다),
    /// 링 테두리는 가리지 않는다. 꺼짐(PortalInactive 빈 링)·건설 연출 중에는 그리지 않고 링은 그대로 둔다.
    /// 다시 켜지면 같은 각도에서 이어 돈다. 포탈 이동·판정·그림 전환 코드는 건드리지 않고 읽기만 한다.
    /// 오브젝트는 켜진 상태에서 처음 필요할 때 한 번 만든다(건설 연출이 가리는 렌더러 목록에 끼지 않게).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PortalHoleSpin : MonoBehaviour
    {
        // ★ PM 조정용 숫자
        public const float DefaultSecondsPerTurn = 8f;        // ★ 한 바퀴 시간
        public const float MaskRadiusFactor = 0.97f;          // ★ 홀 반지름 대비 마스크 반지름
        public const string HoleRootName = "PortalHole";
        private const string InactiveTextureName = "PortalInactive";

        [SerializeField, Min(0.5f)] private float secondsPerTurn = DefaultSecondsPerTurn;

        private EmergencyEscapePortal portal;
        private SpriteRenderer artwork;
        private Transform holeRoot;
        private Transform pivot;
        private SpriteMask mask;
        private SpriteRenderer copy;
        private float angle;
        private bool angleReady;

        /// <summary>현재 회전 각도(0~360). 꺼져 있는 동안은 멈춰 있고 다시 켜지면 이어진다.</summary>
        public float Angle => angle;
        public bool IsHoleVisible => copy != null && copy.enabled;
        public GameObject HoleRoot => holeRoot != null ? holeRoot.gameObject : null;

        private void Update() => Tick(Time.deltaTime);

        /// <summary>시간을 진행한다. timeScale 0이면 deltaTime이 0이라 멈추고, 테스트는 직접 부른다.</summary>
        public void Tick(float deltaTime)
        {
            try
            {
                if (!ShouldShow())
                {
                    SetVisible(false);
                    return;
                }

                if (!EnsureObjects())
                {
                    return;
                }

                if (!angleReady)
                {
                    angle = (float)new System.Random(unchecked(Environment.TickCount * 31 + (int)(transform.position.x * 1000f) * 7919 + (int)(transform.position.y * 1000f))).NextDouble() * 360f;
                    angleReady = true;
                }

                SyncSprite();
                SetVisible(true);
                angle = Mathf.Repeat(angle + 360f / Mathf.Max(0.5f, secondsPerTurn) * Mathf.Max(0f, deltaTime), 360f);
                // 시계방향 = Unity z축 음의 방향.
                pivot.localRotation = Quaternion.Euler(0f, 0f, -angle);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                enabled = false;
            }
        }

        private bool ShouldShow()
        {
            if (!isActiveAndEnabled) return false;
            if (portal == null) portal = GetComponent<EmergencyEscapePortal>();
            if (artwork == null)
            {
                Transform t = transform.Find("VisualRoot/Artwork");
                if (t != null) artwork = t.GetComponent<SpriteRenderer>();
            }

            if (portal == null || artwork == null || artwork.sprite == null) return false;
            // 켜짐(웜홀 그림)에서만. 꺼짐은 빈 링이라 소용돌이를 그리지 않는다.
            if (!portal.IsPowered || IsInactiveSprite(artwork.sprite)) return false;
            // 건설 연출 중에는 완성본이 가려지므로 회전도 숨긴다.
            var construction = GetComponent<FacilityConstructionEffect>();
            return construction == null || !construction.IsPlaying;
        }

        private static bool IsInactiveSprite(Sprite sprite) =>
            sprite.texture != null && sprite.texture.name == InactiveTextureName;

        private bool EnsureObjects()
        {
            if (holeRoot != null) return true;
            Sprite sprite = artwork.sprite;
            Bounds visible = FacilityGroundedVisual.GetVisibleBounds(sprite);
            Vector2 center = new(
                visible.min.x + FacilityIdleAnchors.PortalHole.x * visible.size.x,
                visible.min.y + FacilityIdleAnchors.PortalHole.y * visible.size.y);
            float radius = FacilityIdleAnchors.PortalHoleRadius * visible.size.x * MaskRadiusFactor;
            int layerId = artwork.sortingLayerID;
            int order = artwork.sortingOrder;

            var rootObject = new GameObject(HoleRootName) { layer = gameObject.layer };
            holeRoot = rootObject.transform;
            holeRoot.SetParent(artwork.transform, false);
            holeRoot.localPosition = new Vector3(center.x, center.y, 0f);

            var maskObject = new GameObject("HoleMask") { layer = gameObject.layer };
            maskObject.transform.SetParent(holeRoot, false);
            maskObject.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
            mask = maskObject.AddComponent<SpriteMask>();
            mask.sprite = FacilityIdleSprites.Mask;
            // 이 마스크는 웜홀 복사본이 있는 정렬 구간에만 영향을 준다(다른 마스크·스프라이트와 섞이지 않게).
            mask.isCustomRangeActive = true;
            mask.frontSortingLayerID = layerId;
            mask.frontSortingOrder = order + 1;
            mask.backSortingLayerID = layerId;
            mask.backSortingOrder = order;   // 아래 경계는 포함되지 않아 복사본(order+1)보다 한 칸 낮춰야 보인다

            var pivotObject = new GameObject("HoleSpin") { layer = gameObject.layer };
            pivot = pivotObject.transform;
            pivot.SetParent(holeRoot, false);

            // 웜홀 복사본은 원래 그림과 같은 자리에 겹치고(홀 중심을 축으로 회전), 홀 밖은 마스크가 가린다.
            var copyObject = new GameObject("WormholeCopy") { layer = gameObject.layer };
            copyObject.transform.SetParent(pivot, false);
            copyObject.transform.localPosition = new Vector3(-center.x, -center.y, 0f);
            copy = copyObject.AddComponent<SpriteRenderer>();
            copy.sprite = sprite;
            copy.sharedMaterial = artwork.sharedMaterial;
            copy.sortingLayerID = layerId;
            copy.sortingOrder = order + 1;
            copy.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

            return true;
        }

        // 켜짐 그림이 바뀌었으면 복사본도 같은 그림으로 맞춘다.
        private void SyncSprite()
        {
            if (copy != null && artwork.sprite != null && copy.sprite != artwork.sprite) copy.sprite = artwork.sprite;
        }

        private void SetVisible(bool visible)
        {
            if (holeRoot == null) return;
            if (copy != null && copy.enabled != visible) copy.enabled = visible;
            if (mask != null && mask.enabled != visible) mask.enabled = visible;
        }

        private void OnDisable() => SetVisible(false);
    }
}
