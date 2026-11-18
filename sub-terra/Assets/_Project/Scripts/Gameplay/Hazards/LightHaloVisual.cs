using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.Gameplay.Hazards
{
    /// <summary>
    /// B-196-1: 전원이 켜진 조명 시설 주변(판정 반지름 5칸 안쪽)을 가스와 무관하게 따뜻하게 밝히는 후광.
    /// 가산 블렌드 공유 머티리얼 + 부드러운 방사형 스프라이트. 전원이 꺼지면 짧게 사라지고,
    /// 화면 밖이거나 전역 상한(MaxVisibleLightHalos)을 넘으면 렌더러를 끈다.
    /// 판정(IsCleared·GasZone)과 무관한 시각 전용 컴포넌트다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LightHaloVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer haloRenderer;
        [SerializeField] private GasVisionClearanceSource source;
        [SerializeField] private Sprite softSprite;
        [SerializeField, Range(0f, 1f)] private float peakOpacity = GasVisualRules.LightHaloOpacity;
        [SerializeField] private Color tint = GasVisualRules.LightHaloTint;
        [SerializeField, Range(0f, 0.3f)] private float flickerStrength = GasVisualRules.LightHaloFlickerStrength;
        [SerializeField, Min(0f)] private float fadeSeconds = GasVisualRules.LightHaloFadeSeconds;
        [SerializeField] private int sortingOrder = 23;

        private static Material sharedMaterial;
        private static int budgetFrame = -1;
        private static int budgetUsed;

        private float level;
        private float seed;
        private float fittedRadius = -1f;

        /// <summary>현재 후광 밝기 비율(0~1, 깜빡임 제외). 테스트·디버그용.</summary>
        public float Level => level;

        /// <summary>마지막으로 렌더러에 적용한 알파(깜빡임 포함).</summary>
        public float CurrentAlpha { get; private set; }

        public SpriteRenderer Renderer => haloRenderer;

        /// <summary>조명 소스에 후광이 없으면 만든다. 이미 있으면(빌더가 넣은 경우 포함) 아무것도 하지 않는다.</summary>
        public static LightHaloVisual EnsureFor(GasVisionClearanceSource owner)
        {
            if (owner == null)
            {
                return null;
            }

            var root = owner.transform.parent != null ? owner.transform.parent : owner.transform;
            var existing = root.GetComponentInChildren<LightHaloVisual>(true);
            if (existing != null)
            {
                existing.Bind(owner);
                return existing;
            }

            var go = new GameObject(GasVisualRules.LightHaloChildName);
            go.transform.SetParent(root, false);
            go.transform.localPosition = owner.transform.localPosition;
            var halo = go.AddComponent<LightHaloVisual>();
            halo.Bind(owner);
            return halo;
        }

        public void Bind(GasVisionClearanceSource owner)
        {
            source = owner;
            EnsureRenderer();
        }

        /// <summary>한 프레임에 보일 수 있는 후광 수를 센다. 프레임이 바뀌면 0부터 다시 센다.</summary>
        public static bool TryTakeBudget(int frame)
        {
            if (budgetFrame != frame)
            {
                budgetFrame = frame;
                budgetUsed = 0;
            }

            if (budgetUsed >= GasVisualRules.MaxVisibleLightHalos)
            {
                return false;
            }

            budgetUsed++;
            return true;
        }

        public static void ResetBudgetForTests()
        {
            budgetFrame = -1;
            budgetUsed = 0;
        }

        private void Awake()
        {
            seed = Mathf.Abs(transform.position.x * 0.137f + transform.position.y * 0.291f) % 1f;
            EnsureRenderer();
        }

        private void LateUpdate()
        {
            Tick(Time.deltaTime, Time.time, Time.frameCount, Camera.main);
        }

        private void OnDisable()
        {
            if (haloRenderer != null)
            {
                haloRenderer.enabled = false;
            }
        }

        /// <summary>한 프레임 갱신. 테스트에서 시간·카메라를 직접 넣을 수 있다.</summary>
        public void Tick(float deltaTime, float time, int frame, Camera camera)
        {
            EnsureRenderer();
            if (haloRenderer == null)
            {
                return;
            }

            var lit = source != null && source.isActiveAndEnabled && source.IsLit;
            var lightLevel = lit ? source.LightLevel : 0f;
            var visible = lit;
            if (visible && GasVisionHoleEvaluator.TryGetCameraWorldView(camera, out var view))
            {
                visible = GasVisualMath.CircleIntersectsRect(transform.position, source.Radius, view);
            }

            if (visible && !TryTakeBudget(frame))
            {
                visible = false;
            }

            var reduceMotion = AccessibilityPreferences.ReduceMotion;
            if (!visible)
            {
                // 꺼졌거나 화면 밖이면 비용을 쓰지 않는다. 켜진 채 화면 밖이면 다음에 보일 때 바로 보이게 한다.
                level = lit ? lightLevel : Mathf.MoveTowards(level, 0f, FadeStep(deltaTime, reduceMotion));
                if (lit || level <= 0.0001f)
                {
                    haloRenderer.enabled = false;
                    CurrentAlpha = 0f;
                    return;
                }
            }
            else
            {
                level = Mathf.MoveTowards(level, lightLevel, FadeStep(deltaTime, reduceMotion));
            }

            FitToRadius();
            var flicker = GasVisualMath.HaloFlicker(time, seed, flickerStrength, reduceMotion);
            var color = tint;
            color.a = Mathf.Clamp01(peakOpacity) * level * flicker * source.VisualOpacity;
            haloRenderer.color = color;
            haloRenderer.enabled = color.a > 0.002f;
            CurrentAlpha = color.a;
        }

        private float FadeStep(float deltaTime, bool reduceMotion)
        {
            // 모션 감소에서는 페이드 없이 바로 켜고 끈다.
            if (reduceMotion || fadeSeconds <= 0.0001f)
            {
                return 1f;
            }

            return Mathf.Max(0f, deltaTime) / fadeSeconds;
        }

        private void FitToRadius()
        {
            if (source == null)
            {
                return;
            }

            // B-204: 켠 직후에는 시각 반지름이 커진다. 판정 반지름(source.Radius)은 바꾸지 않는다.
            var radius = source.VisualRadius;
            if (Mathf.Abs(radius - fittedRadius) < 0.0001f)
            {
                return;
            }

            // 지름 = 판정 반지름 * 2. 스프라이트 알파가 가장자리에서 0이라 시각이 판정 범위보다 커지지 않는다.
            GasZoneVisual.FitSpriteToWorldDiameter(haloRenderer, radius * 2f);
            fittedRadius = radius;
        }

        private void EnsureRenderer()
        {
            if (haloRenderer == null)
            {
                haloRenderer = GetComponent<SpriteRenderer>();
                if (haloRenderer == null)
                {
                    haloRenderer = gameObject.AddComponent<SpriteRenderer>();
                }
            }

            if (haloRenderer.sprite == null || haloRenderer.sprite.name == "Knob")
            {
                haloRenderer.sprite = softSprite != null ? softSprite : GasSoftSprites.Radial;
                fittedRadius = -1f;
            }

            var material = ResolveSharedMaterial();
            if (material != null && haloRenderer.sharedMaterial != material)
            {
                haloRenderer.sharedMaterial = material;
            }

            haloRenderer.sortingOrder = sortingOrder;
            haloRenderer.maskInteraction = SpriteMaskInteraction.None;
        }

        // 모든 후광이 머티리얼 하나를 공유한다. 색·알파는 SpriteRenderer.color(정점 색)로만 준다.
        private static Material ResolveSharedMaterial()
        {
            if (sharedMaterial != null)
            {
                return sharedMaterial;
            }

            var shader = Resources.Load<Shader>(GasVisualRules.LightHaloShaderResourceName);
            if (shader == null)
            {
                shader = Shader.Find(GasVisualRules.LightHaloShaderName);
            }

            if (shader == null)
            {
                return null;
            }

            sharedMaterial = new Material(shader)
            {
                name = "LightHaloRuntimeShared",
                hideFlags = HideFlags.HideAndDontSave
            };
            return sharedMaterial;
        }
    }
}
