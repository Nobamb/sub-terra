using System;
using System.Collections.Generic;
using SubTerra.Gameplay.Power;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.Gameplay.Building
{
    /// <summary>
    /// B-206: 새로 지은 시설이 완성본으로 팝인하지 않고 약 0.5초 동안 시설별 건설 과정을 보여 준다.
    /// 시각 전용이다. 점유·비용·전력·충돌·상호작용은 설치 즉시 그대로 동작하고, 여기서는 렌더러만 잠시 가린다
    /// (Renderer.forceRenderingOff라 enabled 값은 건드리지 않는다). 복원·모션 감소에서는 만들지 않는다.
    /// 끝나면 PowerOnEffect(IPowerOnVisualGate)와 범위 퍼짐이 이어서 시작되도록 기다려 준다.
    /// 새로 지을 때(TryPlaceAt)와 옮길 때(TryMoveAt, 이동 가능 시설)에만 시작한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FacilityConstructionEffect : MonoBehaviour, IPowerOnVisualGate
    {
        // ★ PM 조정용 숫자.
        public const float BuildAnimSeconds = 0.5f;           // ★ 이슈 값. 시설 공통 길이
        public const int ShapeSortingOffset = 1;              // ★ 시설 최대 sortingOrder + 1
        public const string EffectRootName = "ConstructionEffect";
        private const float HologramPieceAlpha = 0.85f;       // 조각이 홀로그램일 때 최대 알파
        private const string FoundationRootName = "FacilityFoundation";
        private const string GroundingFoundationPath = "MVP_Grounding/FoundationTile";

        private static readonly List<FacilityConstructionEffect> active = new();
        private static readonly Color HologramTint = new(0.26f, 0.9f, 1f, 1f);
        private static Sprite squareSprite, glowSprite, ringSprite;

        private sealed class Body
        {
            public SpriteRenderer Renderer;
            public Sprite Sprite;
            public Bounds Visible;
        }

        private sealed class PieceRuntime
        {
            public ConstructionPiece Spec;
            public SpriteRenderer Renderer;
            public Vector3 BaseLocal;
            public Vector2 InverseScale;
        }

        private sealed class ShapeRuntime
        {
            public ConstructionShape Spec;
            public SpriteRenderer Renderer;
        }

        private readonly List<SpriteRenderer> hidden = new();
        private readonly List<PieceRuntime> pieces = new();
        private readonly List<ShapeRuntime> shapes = new();
        private readonly List<Sprite> createdSprites = new();
        private readonly List<Action> holds = new();
        private GameObject effectRoot;
        private Rect worldRect;
        private float elapsed;

        public bool IsPlaying { get; private set; }
        public string Style { get; private set; }
        public float Elapsed => elapsed;
        public float Duration => BuildAnimSeconds;
        public float Remaining => IsPlaying ? Mathf.Max(0f, BuildAnimSeconds - elapsed) : 0f;
        public int PieceCount => pieces.Count;
        public int ShapeCount => shapes.Count;
        public int HiddenRendererCount => hidden.Count;
        public GameObject EffectRoot => effectRoot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            active.Clear();
            squareSprite = glowSprite = ringSprite = null;
        }

        /// <summary>
        /// 시설 루트에 건설 연출을 시작한다. 모션 감소이거나 레시피·몸체 그림이 없으면 아무것도 하지 않고 null.
        /// 시작하는 프레임에 이미 완성본이 가려지므로 팝인이 보이지 않는다.
        /// </summary>
        public static FacilityConstructionEffect Begin(GameObject building, string buildingId)
        {
            if (building == null || AccessibilityPreferences.ReduceMotion) return null;
            if (!FacilityConstructionRecipes.TryGet(buildingId, out ConstructionRecipe recipe)) return null;

            var effect = building.GetComponent<FacilityConstructionEffect>();
            if (effect == null) effect = building.AddComponent<FacilityConstructionEffect>();
            // 연출 도중 다시 옮기면 앞 연출을 마무리(대기 중인 켜짐 연출 시작)하고 새 위치에서 처음부터 다시 보여 준다.
            else if (effect.IsPlaying) effect.Finish(true);
            bool started = false;
            try
            {
                started = effect.Setup(recipe);
            }
            catch (Exception exception)
            {
                // 연출 실패가 설치·전력 판정을 막지 않게 완성본을 바로 보여 준다.
                Debug.LogException(exception, building);
            }

            if (!started)
            {
                effect.Finish(false);
                DestroyObject(effect);
                return null;
            }
            return effect;
        }

        /// <summary>해당 인스턴스의 건설 연출이 끝나기까지 남은 시간(초). 진행 중이 아니면 0.</summary>
        public static float RemainingSecondsFor(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId)) return 0f;
            for (int i = 0; i < active.Count; i++)
            {
                FacilityConstructionEffect effect = active[i];
                if (effect == null || !effect.IsPlaying) continue;
                var instance = effect.GetComponent<BuildingInstance>();
                if (instance != null && instance.InstanceId == instanceId) return effect.Remaining;
            }
            return 0f;
        }

        public bool TryHold(Action release)
        {
            if (!IsPlaying || release == null) return false;
            holds.Add(release);
            return true;
        }

        /// <summary>Time.deltaTime만큼 진행한다. 정지 중에는 멈추고, 테스트는 직접 부른다.</summary>
        public void Tick(float deltaTime)
        {
            if (!IsPlaying) return;
            elapsed += Mathf.Max(0f, deltaTime);
            if (elapsed >= BuildAnimSeconds)
            {
                Finish(true);
                return;
            }
            Apply(elapsed / BuildAnimSeconds);
        }

        /// <summary>연출을 건너뛰고 완성본을 바로 보여 준다(대기 중인 켜짐 연출은 이어서 시작).</summary>
        public void Complete() => Finish(true);

        private void Update()
        {
            if (IsPlaying) Tick(Time.deltaTime);
        }

        private void OnDisable()
        {
            // 건설 도중 시설이 꺼지거나 철거되면 정리만 하고 기다리던 켜짐 연출은 시작하지 않는다.
            if (IsPlaying) Finish(false);
        }

        private void OnDestroy() => active.Remove(this);

        private bool Setup(ConstructionRecipe recipe)
        {
            Style = recipe.Style;
            var bodies = new Dictionary<string, Body>();
            int maxOrder = 0;
            int layerId = 0;
            bool hasLayer = false;
            Transform foundation = transform.Find(FoundationRootName);
            Transform grounding = transform.Find(GroundingFoundationPath);

            foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer == null || !renderer.enabled || renderer.sprite == null) continue;
                if (effectRoot != null && renderer.transform.IsChildOf(effectRoot.transform)) continue;
                // 바닥 받침(그림자·판)은 먼저 놓인 채로 두고, 가스 시야 후광은 건드리지 않는다.
                if (foundation != null && renderer.transform.IsChildOf(foundation)) continue;
                if (grounding != null && renderer.transform.IsChildOf(grounding)) continue;
                if (renderer.sprite.name == "gas-soft-radial") continue;

                hidden.Add(renderer);
                if (!hasLayer) { layerId = renderer.sortingLayerID; hasLayer = true; }
                maxOrder = Mathf.Max(maxOrder, renderer.sortingOrder);

                string key = ResolveBodyKey(renderer);
                if (key != null && !bodies.ContainsKey(key))
                    bodies[key] = new Body
                    {
                        Renderer = renderer,
                        Sprite = renderer.sprite,
                        Visible = renderer.drawMode == SpriteDrawMode.Simple
                            ? FacilityGroundedVisual.GetVisibleBounds(renderer.sprite)
                            : GetDrawnBounds(renderer)
                    };
            }

            if (bodies.Count == 0) return false;

            effectRoot = new GameObject(EffectRootName) { layer = gameObject.layer };
            effectRoot.transform.SetParent(transform, false);
            Vector3 parentScale = transform.lossyScale;
            effectRoot.transform.localScale = new Vector3(SafeInverse(parentScale.x), SafeInverse(parentScale.y), 1f);

            worldRect = ComputeWorldRect(bodies.Values);
            foreach (ConstructionPiece spec in recipe.Pieces)
            {
                if (!bodies.TryGetValue(spec.Source, out Body body)) continue;
                PieceRuntime piece = CreatePiece(spec, body);
                if (piece != null) pieces.Add(piece);
            }
            if (pieces.Count == 0) return false;

            foreach (ConstructionShape spec in recipe.Shapes) shapes.Add(CreateShape(spec, layerId, maxOrder + ShapeSortingOffset));

            foreach (SpriteRenderer renderer in hidden) renderer.forceRenderingOff = true;
            elapsed = 0f;
            IsPlaying = true;
            if (!active.Contains(this)) active.Add(this);
            enabled = true;
            Apply(0f);
            return true;
        }

        private string ResolveBodyKey(SpriteRenderer renderer)
        {
            string name = renderer.gameObject.name;
            if (name == "Artwork" || name == "Diamond") return FacilityConstructionRecipes.Body;
            if (name == "Cap" || name == "Post") return name;
            // 사다리처럼 시설 루트 렌더러가 그림 자체인 시설.
            return renderer.transform == transform ? FacilityConstructionRecipes.Body : null;
        }

        /// <summary>타일·슬라이스 모드(사다리)는 size 만큼 늘려 그리므로 그 영역이 곧 보이는 범위다.</summary>
        private static Bounds GetDrawnBounds(SpriteRenderer renderer)
        {
            Sprite sprite = renderer.sprite;
            Vector2 size = renderer.size;
            Vector2 pivot01 = new(sprite.pivot.x / Mathf.Max(1f, sprite.rect.width), sprite.pivot.y / Mathf.Max(1f, sprite.rect.height));
            Vector2 center = new((0.5f - pivot01.x) * size.x, (0.5f - pivot01.y) * size.y);
            return new Bounds(center, size);
        }

        private static Rect ComputeWorldRect(IEnumerable<Body> bodies)
        {
            bool first = true;
            Vector2 min = default, max = default;
            foreach (Body body in bodies)
            {
                Transform t = body.Renderer.transform;
                Vector3 a = t.TransformPoint(body.Visible.min);
                Vector3 b = t.TransformPoint(body.Visible.max);
                Vector2 lo = Vector2.Min(a, b), hi = Vector2.Max(a, b);
                min = first ? lo : Vector2.Min(min, lo);
                max = first ? hi : Vector2.Max(max, hi);
                first = false;
            }
            return Rect.MinMaxRect(min.x, min.y, Mathf.Max(max.x, min.x + 0.01f), Mathf.Max(max.y, min.y + 0.01f));
        }

        private PieceRuntime CreatePiece(ConstructionPiece spec, Body body)
        {
            Sprite source = body.Sprite;
            Sprite sprite;
            Vector3 localPosition = Vector3.zero;
            Vector2 tiledSize = default;
            bool tiled = body.Renderer.drawMode != SpriteDrawMode.Simple;
            if (tiled)
            {
                // 타일·슬라이스로 늘려 그리는 그림은 같은 모드의 띠로 나눠 그린다(띠 높이가 타일 단위와 맞아야 이음새가 없다).
                sprite = source;
                tiledSize = new Vector2(body.Visible.size.x * (spec.X1 - spec.X0), body.Visible.size.y * (spec.Y1 - spec.Y0));
                localPosition = new Vector3(
                    body.Visible.min.x + body.Visible.size.x * (spec.X0 + spec.X1) * 0.5f,
                    body.Visible.min.y + body.Visible.size.y * (spec.Y0 + spec.Y1) * 0.5f, 0f);
            }
            else if (source.packed || source.texture == null)
            {
                // 아틀라스에 묶인 그림은 자르지 않고 통째로 쓴다.
                sprite = source;
            }
            else
            {
                Rect rect = source.rect;
                float ppu = source.pixelsPerUnit;
                Vector2 origin = rect.min + source.pivot;
                Vector2 visMin = origin + (Vector2)body.Visible.min * ppu;
                Vector2 visMax = origin + (Vector2)body.Visible.max * ppu;
                float x0 = spec.X0 <= 0.001f ? rect.xMin : Mathf.Round(Mathf.Lerp(visMin.x, visMax.x, spec.X0));
                float x1 = spec.X1 >= 0.999f ? rect.xMax : Mathf.Round(Mathf.Lerp(visMin.x, visMax.x, spec.X1));
                float y0 = spec.Y0 <= 0.001f ? rect.yMin : Mathf.Round(Mathf.Lerp(visMin.y, visMax.y, spec.Y0));
                float y1 = spec.Y1 >= 0.999f ? rect.yMax : Mathf.Round(Mathf.Lerp(visMin.y, visMax.y, spec.Y1));
                x0 = Mathf.Clamp(x0, rect.xMin, rect.xMax - 1f);
                y0 = Mathf.Clamp(y0, rect.yMin, rect.yMax - 1f);
                x1 = Mathf.Clamp(x1, x0 + 1f, rect.xMax);
                y1 = Mathf.Clamp(y1, y0 + 1f, rect.yMax);
                var sub = Rect.MinMaxRect(x0, y0, x1, y1);
                // 바닥 중앙을 기준점으로 삼아 위로 자라는 연출이 땅에 붙어 있게 한다.
                var anchor = new Vector2(0.5f, 0f);
                sprite = Sprite.Create(source.texture, sub, anchor, ppu, 0, SpriteMeshType.FullRect);
                sprite.name = source.name + "_BuildPiece";
                createdSprites.Add(sprite);
                Vector2 anchorPixels = sub.min + new Vector2(sub.width * anchor.x, sub.height * anchor.y);
                Vector2 offset = (anchorPixels - origin) / ppu;
                localPosition = new Vector3(offset.x, offset.y, 0f);
            }

            var go = new GameObject("BuildPiece") { layer = gameObject.layer };
            go.transform.SetParent(body.Renderer.transform, false);
            go.transform.localPosition = localPosition;
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            if (tiled)
            {
                renderer.drawMode = body.Renderer.drawMode;
                renderer.tileMode = body.Renderer.tileMode;
                renderer.size = tiledSize;
            }
            renderer.sharedMaterial = body.Renderer.sharedMaterial;
            renderer.sortingLayerID = body.Renderer.sortingLayerID;
            renderer.sortingOrder = body.Renderer.sortingOrder;
            renderer.flipX = body.Renderer.flipX;
            renderer.enabled = false;
            Vector3 lossy = body.Renderer.transform.lossyScale;
            return new PieceRuntime
            {
                Spec = spec,
                Renderer = renderer,
                BaseLocal = localPosition,
                InverseScale = new Vector2(SafeInverse(lossy.x), SafeInverse(lossy.y))
            };
        }

        private ShapeRuntime CreateShape(ConstructionShape spec, int layerId, int order)
        {
            EnsureShapeSprites();
            var go = new GameObject("BuildFx") { layer = gameObject.layer };
            go.transform.SetParent(effectRoot.transform, false);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = spec.Kind == ConstructionShapeKind.Glow ? glowSprite
                : spec.Kind == ConstructionShapeKind.Ring ? ringSprite : squareSprite;
            renderer.sortingLayerID = layerId;
            renderer.sortingOrder = order;
            renderer.enabled = false;
            return new ShapeRuntime { Spec = spec, Renderer = renderer };
        }

        private void Apply(float t)
        {
            foreach (PieceRuntime piece in pieces) ApplyPiece(piece, t);
            foreach (ShapeRuntime shape in shapes) ApplyShape(shape, t);
        }

        private static void ApplyPiece(PieceRuntime piece, float t)
        {
            ConstructionPiece spec = piece.Spec;
            float u = Mathf.Clamp01((t - spec.Delay) / Mathf.Max(0.0001f, spec.Duration));
            SpriteRenderer renderer = piece.Renderer;
            if (renderer == null) return;
            if (u <= 0f)
            {
                renderer.enabled = false;
                return;
            }

            float progress = Evaluate(spec.Ease, u);
            Vector2 offset = Vector2.LerpUnclamped(spec.From, Vector2.zero, progress);
            renderer.transform.localPosition = piece.BaseLocal
                + new Vector3(offset.x * piece.InverseScale.x, offset.y * piece.InverseScale.y, 0f);
            Vector2 scale = Vector2.LerpUnclamped(spec.ScaleFrom, Vector2.one, progress);
            renderer.transform.localScale = new Vector3(scale.x, scale.y, 1f);

            float alpha = Mathf.Lerp(spec.AlphaFrom, 1f, Mathf.Clamp01(u * 2.5f));
            // 청록 홀로그램으로 나타났다가 후반에 원래 그림 색으로 굳는다.
            float solid = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1f, u));
            Color color = Color.Lerp(HologramTint, Color.white, solid);
            color.a = Mathf.Lerp(alpha * HologramPieceAlpha, 1f, solid);
            renderer.color = color;
            renderer.enabled = true;
        }

        private void ApplyShape(ShapeRuntime shape, float t)
        {
            ConstructionShape spec = shape.Spec;
            SpriteRenderer renderer = shape.Renderer;
            if (renderer == null) return;
            float s = Mathf.InverseLerp(spec.Start, spec.End, t);
            if (t < spec.Start || t >= spec.End)
            {
                renderer.enabled = false;
                return;
            }

            float eased = EaseOut(s);
            Vector2 position = Vector2.Lerp(spec.PosFrom, spec.PosTo, eased);
            Vector2 size = Vector2.Lerp(spec.SizeFrom, spec.SizeTo, eased) * worldRect.width;
            renderer.transform.position = new Vector3(
                Mathf.LerpUnclamped(worldRect.xMin, worldRect.xMax, position.x),
                Mathf.LerpUnclamped(worldRect.yMin, worldRect.yMax, position.y),
                transform.position.z);
            renderer.transform.localScale = new Vector3(size.x, size.y, 1f);

            float curve = s < spec.Peak ? s / spec.Peak : 1f - (s - spec.Peak) / (1f - spec.Peak);
            float flicker = spec.Flicker && Mathf.FloorToInt(t * 90f) % 2 == 1 ? 0.55f : 1f;
            Color color = spec.Color;
            color.a *= Mathf.Clamp01(curve) * flicker;
            renderer.color = color;
            renderer.enabled = color.a > 0.01f;
        }

        private void Finish(bool runHolds)
        {
            bool wasPlaying = IsPlaying;
            IsPlaying = false;
            active.Remove(this);

            foreach (SpriteRenderer renderer in hidden)
                if (renderer != null) renderer.forceRenderingOff = false;
            hidden.Clear();

            // 조각 오브젝트는 몸체 렌더러 아래에 있어 효과 루트와 따로 지운다.
            foreach (PieceRuntime piece in pieces)
                if (piece.Renderer != null) DestroyObject(piece.Renderer.gameObject);
            pieces.Clear();
            shapes.Clear();
            if (effectRoot != null) DestroyObject(effectRoot);
            effectRoot = null;
            foreach (Sprite sprite in createdSprites) DestroyObject(sprite);
            createdSprites.Clear();
            elapsed = 0f;

            Action[] pending = holds.ToArray();
            holds.Clear();
            if (wasPlaying && runHolds)
            {
                foreach (Action release in pending)
                {
                    try { release(); }
                    catch (Exception exception) { Debug.LogException(exception, this); }
                }
            }
            if (this != null && enabled) enabled = false;
        }

        private static float Evaluate(ConstructionEase ease, float u)
        {
            switch (ease)
            {
                case ConstructionEase.OutBack:
                {
                    const float c1 = 1.70158f;
                    const float c3 = c1 + 1f;
                    float x = u - 1f;
                    return 1f + c3 * x * x * x + c1 * x * x;
                }
                case ConstructionEase.SlamBounce:
                {
                    const float fall = 0.62f;
                    if (u < fall) return (u / fall) * (u / fall);
                    float k = (u - fall) / (1f - fall);
                    return 1f - 0.1f * Mathf.Sin(Mathf.PI * k);
                }
                default:
                    return EaseOut(u);
            }
        }

        private static float EaseOut(float x)
        {
            float inverse = 1f - Mathf.Clamp01(x);
            return 1f - inverse * inverse * inverse;
        }

        private static float SafeInverse(float value) => Mathf.Abs(value) < 0.0001f ? 1f : 1f / value;

        private static void DestroyObject(UnityEngine.Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Destroy(value); else DestroyImmediate(value);
        }

        private static void EnsureShapeSprites()
        {
            if (squareSprite == null) squareSprite = CreateSprite("RuntimeBuildSquare", 4, (x, y, size) => 1f);
            if (glowSprite == null)
                glowSprite = CreateSprite("RuntimeBuildGlow", 64, (x, y, size) =>
                {
                    float r = Radius(x, y, size);
                    return (1f - r) * (1f - r);
                });
            if (ringSprite == null)
                ringSprite = CreateSprite("RuntimeBuildRing", 64, (x, y, size) =>
                {
                    float r = Radius(x, y, size);
                    return Mathf.Clamp01(1f - Mathf.Abs(r - 0.82f) / 0.16f);
                });
        }

        private static float Radius(int x, int y, int size)
        {
            float half = size * 0.5f;
            float dx = (x + 0.5f - half) / half;
            float dy = (y + 0.5f - half) / half;
            return Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
        }

        /// <summary>지름 1 월드 단위의 흰색 알파 스프라이트(색은 렌더러 색으로 입힌다).</summary>
        private static Sprite CreateSprite(string name, int size, Func<int, int, int, float> alpha)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha(x, y, size)));
            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
