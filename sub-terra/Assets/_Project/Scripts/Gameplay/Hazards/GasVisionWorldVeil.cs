using UnityEngine;

namespace SubTerra.Gameplay.Hazards
{
    /// <summary>
    /// 유독가스 접근 시 카메라 전체를 덮는 월드 암전.
    /// 조명 SpriteMask 범위(5칸)만 뚫려 붉은 5% 빛이 보인다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class GasVisionWorldVeil : MonoBehaviour
    {
        public const string ObjectName = "GasWorldVeil";
        public const int SortingOrder = 21;

        [SerializeField] private SpriteRenderer veilRenderer;
        [SerializeField] private float opacity;

        public float Opacity => opacity;

        private void Awake()
        {
            EnsureRenderer();
            ApplyVisual();
        }

        private void LateUpdate()
        {
            FollowCamera();
            ApplyVisual();
        }

        public void SetOpacity(float value)
        {
            opacity = Mathf.Clamp01(value);
            ApplyVisual();
        }

        private void FollowCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            var position = cam.transform.position;
            transform.position = new Vector3(position.x, position.y, 0f);

            var height = cam.orthographic
                ? cam.orthographicSize * 2.4f
                : 40f;
            var width = height * Mathf.Max(1f, cam.aspect);
            FitSpriteToWorldSize(veilRenderer, width, height);
        }

        private void ApplyVisual()
        {
            EnsureRenderer();
            if (veilRenderer == null)
            {
                return;
            }

            veilRenderer.color = new Color(0.04f, 0.05f, 0.05f, opacity);
            veilRenderer.sortingOrder = SortingOrder;
            veilRenderer.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
            veilRenderer.enabled = opacity > 0.001f;
        }

        private void EnsureRenderer()
        {
            if (veilRenderer == null)
            {
                veilRenderer = GetComponent<SpriteRenderer>();
            }

            if (veilRenderer.sprite == null)
            {
                veilRenderer.sprite = CreateSquareSprite();
            }
        }

        private static void FitSpriteToWorldSize(SpriteRenderer renderer, float worldWidth, float worldHeight)
        {
            if (renderer == null)
            {
                return;
            }

            var sprite = renderer.sprite;
            var bounds = sprite != null ? sprite.bounds.size : Vector3.one;
            var parent = renderer.transform.parent;
            var parentScale = parent != null ? parent.lossyScale : Vector3.one;
            var worldX = Mathf.Max(0.0001f, bounds.x * Mathf.Abs(parentScale.x));
            var worldY = Mathf.Max(0.0001f, bounds.y * Mathf.Abs(parentScale.y));
            renderer.transform.localScale = new Vector3(
                Mathf.Max(0f, worldWidth) / worldX,
                Mathf.Max(0f, worldHeight) / worldY,
                1f);
        }

        private static Sprite cachedSquare;

        private static Sprite CreateSquareSprite()
        {
            if (cachedSquare != null)
            {
                return cachedSquare;
            }

            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave
            };
            for (var y = 0; y < 4; y++)
            {
                for (var x = 0; x < 4; x++)
                {
                    texture.SetPixel(x, y, Color.white);
                }
            }

            texture.Apply(false, true);
            cachedSquare = Sprite.Create(
                texture,
                new Rect(0f, 0f, 4f, 4f),
                new Vector2(0.5f, 0.5f),
                4f);
            cachedSquare.hideFlags = HideFlags.HideAndDontSave;
            return cachedSquare;
        }
    }
}
