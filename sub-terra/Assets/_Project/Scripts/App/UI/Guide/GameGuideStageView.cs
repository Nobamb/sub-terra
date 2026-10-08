using System.Collections.Generic;
using SubTerra.App.UI.Sell;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Guide
{
    /// <summary>
    /// 시연 무대. 320x120 가상 좌표의 장면(DemoClip)을 이미지·글자 층으로 그리고 시각 t의 값을 적용한다.
    /// 목록 썸네일은 Show(thumbTime)로 한 장면만 그리고, 상세 화면은 Play로 반복 재생한다.
    /// 연출 시간은 unscaledDeltaTime이라 정지 상태에서도 진행된다.
    /// </summary>
    public sealed class GameGuideStageView : MonoBehaviour
    {
        private sealed class LayerView
        {
            public DemoLayer Def;
            public RectTransform Rect;
            public Image Image;
            public TMP_Text Text;
            public Sprite[] Frames;
            public Sprite Idle;
            public Sprite Single;
            public float Aspect = 1f;
            public bool Visible = true;
        }

        private readonly List<LayerView> layers = new List<LayerView>();
        private readonly List<GuideKeyCapView> keyViews = new List<GuideKeyCapView>();
        private readonly List<DemoKeyCap> keyDefs = new List<DemoKeyCap>();

        private RectTransform world;
        private RectTransform keyLayer;
        private Image frame;
        private Image frameBorder;
        private TMP_FontAsset font;
        private IGuideSprites sprites;
        private DemoClip clip;
        private bool playing;
        private bool showKeys = true;
        private float clock;
        private float lastScaleWidth = -1f;

        public DemoClip Clip => clip;
        public bool IsPlaying => playing;
        public float Clock => clock;
        public int LayerCount => layers.Count;
        public int KeyCount => keyViews.Count;
        public bool KeysVisible => keyLayer != null && keyLayer.gameObject.activeSelf;

        /// <summary>parent 안에 왼쪽 위 (x, y) 크기 (w, h) 무대를 만든다. 가로세로 비는 8:3을 권한다.</summary>
        internal static GameGuideStageView Create(Transform parent, string name, float x, float y, float w, float h,
            TMP_FontAsset font, IGuideSprites sprites)
        {
            var rect = ResourceSellUi.Place(parent, name, x, y, w, h);
            rect.gameObject.AddComponent<RectMask2D>();
            var view = rect.gameObject.AddComponent<GameGuideStageView>();
            view.font = font;
            view.sprites = sprites;
            view.world = ResourceSellUi.Centered(rect, "World", Vector2.zero,
                new Vector2(GuideDemoLibrary.StageWidth, GuideDemoLibrary.StageHeight));
            view.keyLayer = ResourceSellUi.Centered(rect, "Keys", Vector2.zero, new Vector2(w, h));
            view.frame = ResourceSellUi.Image(rect, "FrameFill", null, Color.clear);
            view.frame.enabled = false;
            view.frameBorder = ResourceSellUi.Image(rect, "FrameLine", ResourceSellArt.ChamferOutline(),
                ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.5f));
            view.frameBorder.type = Image.Type.Sliced;
            view.FitWorld();
            return view;
        }

        public void SetSprites(IGuideSprites value)
        {
            sprites = value;
        }

        /// <summary>장면을 바꾼다. 이전 장면의 층은 모두 정리하고 재생은 멈춘다.</summary>
        public void Bind(DemoClip next, bool withKeys)
        {
            Clear();
            clip = next;
            showKeys = withKeys;
            clock = 0f;
            playing = false;
            FitWorld();
            if (clip == null)
            {
                return;
            }

            for (var i = 0; i < clip.Layers.Count; i++)
            {
                layers.Add(BuildLayer(clip.Layers[i], i));
            }

            keyLayer.gameObject.SetActive(withKeys && clip.Keys.Count > 0);
            if (withKeys)
            {
                var scale = world.localScale.x;
                for (var i = 0; i < clip.Keys.Count; i++)
                {
                    var def = clip.Keys[i];
                    var size = 26f;
                    var px = (def.X * scale) + ((RectTransform)transform).rect.width * 0.5f;
                    var py = ((RectTransform)transform).rect.height * 0.5f - def.Y * scale;
                    var cap = GuideKeyCapView.Create(keyLayer, def.Label, def.Mouse, px - 13f, py - 13f, size, font);
                    keyViews.Add(cap);
                    keyDefs.Add(def);
                }
            }
        }

        public void Show(float time)
        {
            clock = time;
            Apply(time);
        }

        /// <summary>정지 썸네일 장면(ThumbTime)을 그린다.</summary>
        public void ShowThumb()
        {
            playing = false;
            Show(clip != null ? clip.ThumbTime : 0f);
        }

        public void Play()
        {
            if (clip == null)
            {
                return;
            }

            playing = true;
            clock = 0f;
            Apply(0f);
        }

        public void Stop()
        {
            playing = false;
        }

        /// <summary>재생 중일 때만 시간을 진행한다. 반복 장면은 끝나면 처음으로 돌아간다.</summary>
        public void Tick(float dt)
        {
            if (!playing || clip == null)
            {
                return;
            }

            clock += dt;
            if (clip.Duration > 0.01f && clock >= clip.Duration)
            {
                clock %= clip.Duration;
            }

            Apply(clock);
        }

        public void Clear()
        {
            playing = false;
            clip = null;
            for (var i = 0; i < layers.Count; i++)
            {
                if (layers[i].Rect != null)
                {
                    GuideUtil.Dispose(layers[i].Rect.gameObject);
                }
            }

            layers.Clear();
            for (var i = 0; i < keyViews.Count; i++)
            {
                if (keyViews[i] != null)
                {
                    GuideUtil.Dispose(keyViews[i].gameObject);
                }
            }

            keyViews.Clear();
            keyDefs.Clear();
        }

        private void OnDisable()
        {
            playing = false;
        }

        private void FitWorld()
        {
            var rect = (RectTransform)transform;
            var width = rect.rect.width;
            if (width <= 1f)
            {
                width = rect.sizeDelta.x;
            }

            if (Mathf.Approximately(width, lastScaleWidth))
            {
                return;
            }

            lastScaleWidth = width;
            var scale = width / GuideDemoLibrary.StageWidth;
            world.localScale = new Vector3(scale, scale, 1f);
        }

        private LayerView BuildLayer(DemoLayer def, int index)
        {
            var go = new GameObject("L" + index + "_" + (def.Kind == DemoLayerKind.Text ? "text" : def.SpriteKey), typeof(RectTransform));
            go.layer = world.gameObject.layer;
            go.transform.SetParent(world, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = def.Pivot;
            var view = new LayerView { Def = def, Rect = rect };

            switch (def.Kind)
            {
                case DemoLayerKind.Rect:
                    view.Image = go.AddComponent<Image>();
                    view.Image.color = def.Tint;
                    view.Image.raycastTarget = false;
                    rect.sizeDelta = def.Size;
                    break;
                case DemoLayerKind.Text:
                    view.Text = ResourceSellUi.Text(rect, font, def.FontSize, def.Bold ? FontStyles.Bold : FontStyles.Normal,
                        def.Tint, TextAlignmentOptions.Center);
                    view.Text.text = def.Content;
                    rect.sizeDelta = def.Size;
                    break;
                default:
                    view.Image = go.AddComponent<Image>();
                    view.Image.raycastTarget = false;
                    view.Image.preserveAspect = !def.SizeIsRect;
                    view.Single = sprites != null ? sprites.Get(def.SpriteKey) : null;
                    if (def.FrameKeys != null && sprites != null)
                    {
                        view.Frames = new Sprite[def.FrameKeys.Length];
                        for (var i = 0; i < def.FrameKeys.Length; i++)
                        {
                            view.Frames[i] = sprites.Get(def.FrameKeys[i]);
                        }

                        view.Idle = sprites.Get(def.IdleKey);
                    }

                    var first = view.Single ?? view.Idle ?? (view.Frames != null && view.Frames.Length > 0 ? view.Frames[0] : null);
                    view.Image.sprite = first;
                    view.Image.enabled = first != null;
                    view.Image.color = def.Tint;
                    if (first != null && first.border != Vector4.zero)
                    {
                        view.Image.type = Image.Type.Sliced;
                    }

                    if (def.SizeIsRect)
                    {
                        rect.sizeDelta = def.Size;
                    }
                    else
                    {
                        view.Aspect = first != null && first.rect.height > 0f ? first.rect.width / first.rect.height : 1f;
                        rect.sizeDelta = new Vector2(def.Size.y * view.Aspect, def.Size.y);
                    }

                    break;
            }

            return view;
        }

        private void Apply(float time)
        {
            for (var i = 0; i < layers.Count; i++)
            {
                var layer = layers[i];
                var def = layer.Def;
                var alpha = def.Alpha.Eval(time);
                var visible = alpha > 0.004f;
                if (visible != layer.Visible)
                {
                    layer.Visible = visible;
                    layer.Rect.gameObject.SetActive(visible);
                }

                if (!visible)
                {
                    continue;
                }

                layer.Rect.anchoredPosition = new Vector2(def.X.Eval(time), def.Y.Eval(time));
                layer.Rect.localScale = new Vector3(def.ScaleX.Eval(time), def.ScaleY.Eval(time), 1f);
                layer.Rect.localRotation = Quaternion.Euler(0f, 0f, def.Rotation.Eval(time));

                if (layer.Text != null)
                {
                    var color = def.Tint;
                    color.a *= alpha;
                    layer.Text.color = color;
                    continue;
                }

                if (layer.Image == null)
                {
                    continue;
                }

                var tint = def.Tint;
                tint.a *= alpha;
                layer.Image.color = tint;
                if (layer.Frames != null && layer.Frames.Length > 0)
                {
                    var active = def.FrameActive == null || def.FrameActive.Eval(time) > 0.5f;
                    Sprite next;
                    if (active)
                    {
                        var index = Mathf.FloorToInt(time * def.Fps) % layer.Frames.Length;
                        next = layer.Frames[index];
                    }
                    else
                    {
                        next = layer.Idle != null ? layer.Idle : layer.Frames[0];
                    }

                    if (next != null && layer.Image.sprite != next)
                    {
                        layer.Image.sprite = next;
                    }
                }
            }

            if (showKeys)
            {
                for (var i = 0; i < keyViews.Count; i++)
                {
                    keyViews[i].SetPress(keyDefs[i].Press.Eval(time));
                }
            }
        }
    }
}
