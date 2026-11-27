using System;
using System.Collections.Generic;
using SubTerra.Gameplay.Power;
using UnityEngine;

namespace SubTerra.Gameplay.Building
{
    /// <summary>
    /// B-214: 설치한 시설이 약 30초마다 그 시설다운 짧은 연출을 한 번 보인다(그림 전용, 효과음 없음).
    /// 시설이 생길 때 BuildingInstance.Initialize가 런타임에 붙인다(프리팹에 넣지 않는다).
    /// 연출은 전력이 켜져 있고 건설·켜짐 연출·이동 중이 아닐 때만 재생하며, 안 되면 그 주기만 건너뛴다.
    /// 재생 중 조건이 깨지면 0.15초 안에 사라지고 원래 그림으로 돌아온다. 저장할 상태는 없다.
    /// 포탈은 주기 연출이 없고 PortalHoleSpin(켜짐에서만 내부 홀 회전)을 붙인다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FacilityIdleAnimation : MonoBehaviour
    {
        // ★ PM 조정용 숫자(인스펙터에서도 조절 가능)
        public const float DefaultPeriodSeconds = 30f;         // ★ 주기
        public const float DefaultJitterSeconds = 3f;          // ★ 매 주기 ±무작위
        public const float DefaultFirstStartMin = 3f;          // ★ 설치·복원 뒤 첫 시작 범위
        public const float DefaultFirstStartMax = 30f;
        public const float StaggerWindowSeconds = 0.5f;        // ★ 같은 종류끼리 이 안으로 겹치면
        public const float StaggerPushMin = 1f;                // ★ 이만큼 밀어 준다
        public const float StaggerPushMax = 2f;
        public const float InterruptFadeSeconds = 0.15f;       // ★ 중단되면 이 안에 사라진다
        public const float MinDebugPeriodSeconds = 2f;         // 디버그 주기 최솟값
        public const string EffectRootName = "FacilityIdleEffect";

        private static readonly List<FacilityIdleAnimation> all = new();
        private static float periodOverride = -1f;
        private static int seedCounter;

        [SerializeField, Min(2f)] private float periodSeconds = DefaultPeriodSeconds;
        [SerializeField, Min(0f)] private float jitterSeconds = DefaultJitterSeconds;
        [SerializeField, Min(0f)] private float firstStartMin = DefaultFirstStartMin;
        [SerializeField, Min(0f)] private float firstStartMax = DefaultFirstStartMax;

        private FacilityIdleKind kind;
        private BuildingInstance instance;
        private PowerFacility facility;
        private System.Random rng;
        private float remaining;
        private IdleFx effect;
        private IdleFxContext context;
        private float elapsed;
        private bool fading;
        private float fadeElapsed;

        public static IReadOnlyList<FacilityIdleAnimation> All => all;
        public FacilityIdleKind Kind => kind;
        public bool IsPlaying => effect != null;
        public bool IsFading => effect != null && fading;
        public float Elapsed => elapsed;
        public float SecondsUntilNext => remaining;
        public GameObject EffectRoot => context != null && context.EffectRoot != null ? context.EffectRoot.gameObject : null;

        /// <summary>디버그로 바꾼 주기. 0 이하면 기본값을 쓴다.</summary>
        public static float PeriodOverride => periodOverride;
        public float CurrentPeriod => periodOverride > 0f ? periodOverride : periodSeconds;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            all.Clear();
            periodOverride = -1f;
        }

        /// <summary>이번 실행 동안만 주기를 바꾼다(최소 2초). 0 이하를 주면 기본값으로 되돌린다.</summary>
        public static float SetPeriodOverride(float seconds)
        {
            periodOverride = seconds <= 0f ? -1f : Mathf.Max(MinDebugPeriodSeconds, seconds);
            return periodOverride;
        }

        /// <summary>
        /// 시설 루트에 연출 장치를 붙인다(이미 있으면 다시 일정만 잡는다: 옮기기·복원 뒤 다시 시작).
        /// 포탈은 주기 연출 대신 홀 회전 컴포넌트를 붙이고 null을 돌려준다. 대상이 아니면 null.
        /// </summary>
        public static FacilityIdleAnimation EnsureFor(GameObject root, string buildingId)
        {
            if (root == null) return null;
            FacilityIdleKind kind = FacilityIdleAnchors.KindFor(buildingId);
            if (kind == FacilityIdleKind.None) return null;
            if (kind == FacilityIdleKind.Portal)
            {
                if (root.GetComponent<PortalHoleSpin>() == null) root.AddComponent<PortalHoleSpin>();
                return null;
            }

            var animation = root.GetComponent<FacilityIdleAnimation>();
            if (animation == null) animation = root.AddComponent<FacilityIdleAnimation>();
            animation.Configure(kind);
            return animation;
        }

        private void Configure(FacilityIdleKind nextKind)
        {
            kind = nextKind;
            instance = GetComponent<BuildingInstance>();
            facility = GetComponent<PowerFacility>();
            if (rng == null)
            {
                string id = instance != null ? instance.InstanceId : name;
                rng = new System.Random(unchecked(Environment.TickCount * 31 + (id != null ? id.GetHashCode() : 0) + ++seedCounter * 7919));
            }
            Register();
            if (kind == FacilityIdleKind.Light) FacilityVisualDim.GetOrAdd(gameObject);
            // 새로 생겼거나 옮겨졌으니 진행 중 연출은 정리하고 처음부터 다시 기다린다.
            ScheduleFirst();
        }

        private void Register()
        {
            all.RemoveAll(entry => entry == null);
            if (!all.Contains(this)) all.Add(this);
        }

        /// <summary>시설이 옮겨지거나 복원된 뒤 연출을 정리하고 첫 시작을 다시 잡는다.</summary>
        public void ScheduleFirst()
        {
            EndEffect();
            ScheduleAfter(Mathf.Lerp(Mathf.Min(firstStartMin, firstStartMax), Mathf.Max(firstStartMin, firstStartMax), NextFloat()));
        }

        /// <summary>다음 연출까지 시간을 정하고 같은 종류와 겹치면 밀어 준다.</summary>
        public void ScheduleAfter(float seconds)
        {
            remaining = Mathf.Max(0f, seconds);
            Stagger();
        }

        private void ScheduleNext()
        {
            float period = CurrentPeriod;
            float jitter = Mathf.Min(jitterSeconds, period * 0.5f);
            ScheduleAfter(Mathf.Max(1f, period + Mathf.Lerp(-jitter, jitter, NextFloat())));
        }

        private void Stagger()
        {
            all.RemoveAll(entry => entry == null);
            for (int guard = 0; guard < 16; guard++)
            {
                bool clash = false;
                foreach (FacilityIdleAnimation other in all)
                {
                    if (other == this || other.kind != kind || other.effect != null) continue;
                    if (Mathf.Abs(other.remaining - remaining) < StaggerWindowSeconds) { clash = true; break; }
                }
                if (!clash) return;
                remaining += Mathf.Lerp(StaggerPushMin, StaggerPushMax, NextFloat());
            }
        }

        private float NextFloat() => (float)rng.NextDouble();

        private void Update() => Tick(Time.deltaTime);

        /// <summary>시간을 진행한다. 정지 중(timeScale 0)에는 deltaTime이 0이라 멈추고, 테스트는 직접 부른다.</summary>
        public void Tick(float deltaTime)
        {
            if (kind == FacilityIdleKind.None) return;
            deltaTime = Mathf.Max(0f, deltaTime);
            if (effect != null)
            {
                TickPlaying(deltaTime);
                return;
            }

            remaining -= deltaTime;
            if (remaining > 0f) return;
            // 시작 시각이 됐을 때 조건이 안 맞으면 이번 주기만 건너뛴다.
            if (!StartEffect()) ScheduleNext();
        }

        private void TickPlaying(float deltaTime)
        {
            elapsed += deltaTime;
            if (!fading && !CanPlay())
            {
                fading = true;
                fadeElapsed = 0f;
            }

            float amount = 1f;
            float duration = FacilityIdleAnchors.DurationOf(kind);
            if (fading)
            {
                fadeElapsed += deltaTime;
                amount = 1f - fadeElapsed / InterruptFadeSeconds;
                if (amount <= 0f)
                {
                    EndEffect();
                    ScheduleNext();
                    return;
                }
            }
            else if (elapsed >= duration)
            {
                EndEffect();
                ScheduleNext();
                return;
            }

            try
            {
                float t = Mathf.Min(elapsed, duration);
                effect.Apply(context, t, duration > 0f ? t / duration : 1f, amount);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                EndEffect();
                ScheduleNext();
            }
        }

        /// <summary>전력이 켜져 있다고 보는가. 가스 시야와 같이 상호작용 가능 또는 공급 범위 안. 전진기지는 늘 켜짐.</summary>
        public bool IsPowered
        {
            get
            {
                if (kind == FacilityIdleKind.Outpost) return true;
                if (facility == null) facility = GetComponent<PowerFacility>();
                // 조명·보관함은 IsInSupplyRange를 쓰지 않으므로 연출 전용 범위 값(IsInVisualSupplyRange)도 함께 본다.
                return facility != null
                    && (facility.IsInteractionAvailable || facility.IsInSupplyRange || facility.IsInVisualSupplyRange);
            }
        }

        /// <summary>지금 연출을 시작해도 되는가(전력·건설/켜짐 연출·이동·비활성).</summary>
        public bool CanPlay()
        {
            if (kind == FacilityIdleKind.None || !isActiveAndEnabled) return false;
            if (instance == null) instance = GetComponent<BuildingInstance>();
            if (instance != null && BuildingPlacementSystem.MovingInstance == instance) return false;
            var construction = GetComponent<FacilityConstructionEffect>();
            if (construction != null && construction.IsPlaying) return false;
            var powerOn = GetComponent<PowerOnEffect>();
            if (powerOn != null && powerOn.IsPlaying) return false;
            return IsPowered;
        }

        /// <summary>엇갈림과 주기를 무시하고 지금 한 번 재생한다(디버그·테스트). 조건이 안 맞으면 false.</summary>
        public bool PlayNow()
        {
            if (kind == FacilityIdleKind.None || effect != null) return false;
            if (!StartEffect()) return false;
            return true;
        }

        private bool StartEffect()
        {
            if (!CanPlay()) return false;
            IdleFx fx = CreateFx(kind);
            if (fx == null) return false;
            IdleFxContext next = BuildContext();
            if (next == null) return false;

            bool started = false;
            try
            {
                started = fx.Begin(next);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }

            if (!started)
            {
                Cleanup(fx, next);
                return false;
            }

            effect = fx;
            context = next;
            elapsed = 0f;
            fading = false;
            fadeElapsed = 0f;
            try
            {
                fx.Apply(next, 0f, 0f, 1f);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                EndEffect();
                return false;
            }
            return true;
        }

        private static IdleFx CreateFx(FacilityIdleKind kind)
        {
            switch (kind)
            {
                case FacilityIdleKind.Light: return new LightIdleFx();
                case FacilityIdleKind.Charger: return new ChargerIdleFx();
                case FacilityIdleKind.Clinic: return new ClinicIdleFx();
                case FacilityIdleKind.Storage: return new StorageIdleFx();
                case FacilityIdleKind.Settlement: return new SettlementIdleFx();
                case FacilityIdleKind.Outpost: return new OutpostIdleFx();
                default: return null;
            }
        }

        private IdleFxContext BuildContext()
        {
            Transform artworkTransform = transform.Find("VisualRoot/Artwork");
            if (artworkTransform == null) artworkTransform = transform.Find("VisualRoot/Diamond");
            if (artworkTransform == null || !artworkTransform.TryGetComponent(out SpriteRenderer artwork) || artwork.sprite == null)
                return null;

            Bounds visible = artwork.drawMode == SpriteDrawMode.Simple
                ? FacilityGroundedVisual.GetVisibleBounds(artwork.sprite)
                : new Bounds(Vector3.zero, artwork.size);
            Vector3 a = artwork.transform.TransformPoint(visible.min);
            Vector3 b = artwork.transform.TransformPoint(visible.max);
            Rect world = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y),
                Mathf.Max(Mathf.Max(a.x, b.x), Mathf.Min(a.x, b.x) + 0.01f), Mathf.Max(Mathf.Max(a.y, b.y), Mathf.Min(a.y, b.y) + 0.01f));

            // 시설 그림 바로 위 순서. 가스 후광과 다른 연출 루트의 렌더러는 기준에서 뺀다.
            int maxOrder = artwork.sortingOrder;
            foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer == null || renderer.sprite == null || renderer.sprite.name == "gas-soft-radial") continue;
                if (IsEffectChild(renderer.transform)) continue;
                maxOrder = Mathf.Max(maxOrder, renderer.sortingOrder);
            }

            var go = new GameObject(EffectRootName) { layer = gameObject.layer };
            go.transform.SetParent(transform, false);
            Vector3 parentScale = transform.lossyScale;
            go.transform.localScale = new Vector3(SafeInverse(parentScale.x), SafeInverse(parentScale.y), 1f);
            return new IdleFxContext
            {
                Root = transform,
                EffectRoot = go.transform,
                Artwork = artwork,
                World = world,
                LayerId = artwork.sortingLayerID,
                ArtworkOrder = artwork.sortingOrder,
                Order = maxOrder + FacilityConstructionEffect.ShapeSortingOffset,
                Rng = rng
            };
        }

        private bool IsEffectChild(Transform value)
        {
            for (Transform t = value; t != null && t != transform; t = t.parent)
            {
                string n = t.name;
                if (n == EffectRootName || n == FacilityConstructionEffect.EffectRootName
                    || n == PowerOnEffect.EffectRootName || n == "PortalHole")
                    return true;
            }
            return false;
        }

        private static float SafeInverse(float value) => Mathf.Abs(value) < 0.0001f ? 1f : 1f / value;

        /// <summary>연출을 즉시 정리한다(조각·Sprite 파괴, 가려 둔 그림 복원).</summary>
        public void EndEffect()
        {
            IdleFx fx = effect;
            IdleFxContext ctx = context;
            effect = null;
            context = null;
            elapsed = 0f;
            fading = false;
            fadeElapsed = 0f;
            if (fx != null || ctx != null) Cleanup(fx, ctx);
        }

        private void Cleanup(IdleFx fx, IdleFxContext ctx)
        {
            try
            {
                if (fx != null && ctx != null) fx.End(ctx);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }

            if (ctx == null) return;
            foreach (UnityEngine.Object owned in ctx.Owned) DestroyTarget(owned);
            ctx.Owned.Clear();
            if (ctx.EffectRoot != null) DestroyTarget(ctx.EffectRoot.gameObject);
        }

        private void OnDisable() => EndEffect();

        private void OnDestroy()
        {
            EndEffect();
            all.Remove(this);
        }

        private static void DestroyTarget(UnityEngine.Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Destroy(value); else DestroyImmediate(value);
        }
    }
}
