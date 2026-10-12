using System;
using SubTerra.App.UI.Guide;
using SubTerra.App.UI.Sell;
using SubTerra.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SubTerra.App.UI.ControlTerminal
{
    /// <summary>
    /// 입체 키캡 하나. 루트 Rect(호버 판정)는 고정이고 윗면·옆면·그림자 같은 시각 자식만 움직인다.
    /// 글자는 윗면의 자식이라 눌릴 때 함께 내려간다. 호버는 표시 전용이며 입력·저장을 일으키지 않는다.
    /// </summary>
    public sealed class ControlTerminalKeyCap : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private static readonly Color FaceNone = new Color(0.05f, 0.09f, 0.15f, 1f);
        private static readonly Color EdgeNone = new Color(0.30f, 0.40f, 0.50f, 0.55f);
        private static readonly Color FaceMove = new Color(0.05f, 0.30f, 0.38f, 1f);
        private static readonly Color EdgeMove = new Color(0.30f, 0.95f, 0.92f, 1f);
        private static readonly Color FaceJump = new Color(0.10f, 0.40f, 0.56f, 1f);
        private static readonly Color EdgeJump = new Color(0.72f, 0.97f, 1f, 1f);
        private static readonly Color FaceMine = new Color(0.40f, 0.25f, 0.07f, 1f);
        private static readonly Color EdgeMine = new Color(1f, 0.74f, 0.28f, 1f);
        private static readonly Color TextNone = new Color(0.45f, 0.55f, 0.62f, 1f);
        private static readonly Color TextOn = new Color(0.92f, 0.98f, 1f, 1f);

        private readonly ControlTerminalKeyMotion motion = new ControlTerminalKeyMotion();
        private RectTransform visual;
        private CanvasGroup visualGroup;
        private RectTransform top;
        private RectTransform side;
        private RectTransform shadow;
        private Image hit;
        private Image topFill;
        private Image sideFill;
        private Image shadowImage;
        private Image edge;
        private Image highlight;
        private Image mouseBody;
        private Image mouseButton;
        private TMP_Text label;
        private TMP_Text role;
        private Color faceColor;
        private Color edgeColor;
        private ControlTerminalKeyPop pop = new ControlTerminalKeyPop(1f, 0f, 1f);
        private float appliedLevel = float.NaN;
        private bool hovered;
        private bool demoDown;

        /// <summary>호버가 시작/끝났을 때. 기능 키만 보낸다.</summary>
        public event Action<ControlTerminalKeyCap> HoverEntered;
        public event Action<ControlTerminalKeyCap> HoverExited;

        public ControlKeyDef Def { get; private set; }
        public string Id => Def.Id;
        public ControlKeyFunction Function { get; private set; }
        public ControlKeyRole Role => ControlTerminalBindings.RoleOf(Function);
        public bool IsFunctionKey => Function != ControlKeyFunction.None;
        public bool IsHovered => hovered;
        public bool IsDemoDown => demoDown;
        public bool RaycastEnabled => hit != null && hit.raycastTarget;
        public RectTransform HitRect => (RectTransform)transform;
        public RectTransform TopRect => top;
        public string RoleText => role != null ? role.text : string.Empty;
        public Color FaceColor => faceColor;
        public Color EdgeColor => edgeColor;
        public bool IsSettled => motion.IsSettled && IsPopSettled;
        public bool IsPopSettled => pop.Scale == 1f && pop.Lift == 0f && pop.Alpha == 1f;
        public float PopScale => pop.Scale;

        /// <summary>눌림 정도. 호버·자동 시연 모션 값(복귀 탄성은 음수로 남긴다).</summary>
        public float Level => motion.Level;
        /// <summary>윗면이 내려간 거리(디자인 px). 연결선 시작점이 이를 따라간다.</summary>
        public float PressPixels => Level * ControlTerminalLayout.PressTravel;

        internal static ControlTerminalKeyCap Create(Transform parent, in ControlKeyDef def, TMP_FontAsset font)
        {
            var go = new GameObject(def.Id, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = def.Center;
            rect.sizeDelta = def.Size;
            var cap = go.AddComponent<ControlTerminalKeyCap>();
            cap.Def = def;
            cap.Build(rect, font);
            return cap;
        }

        private void Build(RectTransform root, TMP_FontAsset font)
        {
            var depth = ControlTerminalLayout.SideDepth;
            var faceHeight = Def.Size.y - depth;
            hit = root.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            hit.raycastTarget = false;
            hit.canvasRenderer.cullTransparentMesh = false;

            // 등장 때 키가 튀어나오는 크기·높이·알파는 시각 루트만 바꾸고 호버 판정 Rect는 고정한다.
            visual = NewRect("Visual", root);
            Stretch(visual);
            visualGroup = visual.gameObject.AddComponent<CanvasGroup>();
            visualGroup.blocksRaycasts = false;
            visualGroup.interactable = false;

            shadow = NewRect("Shadow", visual);
            Anchor(shadow, 0f, 0f, 1f, 0f, new Vector2(0.5f, 1f));
            shadow.sizeDelta = new Vector2(-4f, 7f);
            shadowImage = AddImage(shadow, ResourceSellArt.Soft(), new Color(0f, 0f, 0f, 0.5f));

            side = NewRect("Side", visual);
            Anchor(side, 0f, 0f, 1f, 0f, new Vector2(0.5f, 0f));
            side.sizeDelta = new Vector2(0f, faceHeight);
            sideFill = AddImage(side, ResourceSellArt.ChamferFill(), Color.white);

            top = NewRect("Top", visual);
            Anchor(top, 0f, 1f, 1f, 1f, new Vector2(0.5f, 1f));
            top.sizeDelta = new Vector2(0f, faceHeight);
            topFill = AddImage(top, ResourceSellArt.ChamferFill(), Color.white);
            var edgeRect = NewRect("Edge", top);
            Stretch(edgeRect);
            edge = AddImage(edgeRect, ResourceSellArt.ChamferOutline(), Color.white);
            var lightRect = NewRect("Highlight", top);
            lightRect.anchorMin = new Vector2(0f, 1f);
            lightRect.anchorMax = new Vector2(1f, 1f);
            lightRect.pivot = new Vector2(0.5f, 1f);
            lightRect.offsetMin = new Vector2(7f, -1.5f);
            lightRect.offsetMax = new Vector2(-7f, 0f);
            highlight = AddImage(lightRect, null, Color.white);

            if (Def.IsMouse)
            {
                BuildMouse(font);
            }
            else
            {
                label = NewText("Label", top, font, 17f, FontStyles.Bold);
                label.text = Def.Id;
                var labelRect = (RectTransform)label.transform;
                labelRect.anchorMin = new Vector2(0f, 0.5f);
                labelRect.anchorMax = new Vector2(1f, 1f);
                labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
                role = NewText("Role", top, font, 11f, FontStyles.Normal);
                var roleRect = (RectTransform)role.transform;
                roleRect.anchorMin = new Vector2(0f, 0f);
                roleRect.anchorMax = new Vector2(1f, 0.55f);
                roleRect.offsetMin = roleRect.offsetMax = Vector2.zero;
            }

            Apply(true);
        }

        private void BuildMouse(TMP_FontAsset font)
        {
            var bodyRect = NewRect("MouseBody", top);
            bodyRect.anchorMin = bodyRect.anchorMax = bodyRect.pivot = new Vector2(0.5f, 0.5f);
            bodyRect.sizeDelta = new Vector2(34f, 45f);
            mouseBody = AddImage(bodyRect, GameGuideArt.Mouse(), Color.white);
            mouseBody.preserveAspect = true;
            // 왼쪽 버튼 위치(스프라이트 24x32의 왼쪽 위 칸)를 별도 면으로 강조한다.
            var buttonRect = NewRect("LeftButton", top);
            buttonRect.anchorMin = buttonRect.anchorMax = buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(-6.2f, 10.5f);
            buttonRect.sizeDelta = new Vector2(10f, 13f);
            mouseButton = AddImage(buttonRect, ResourceSellArt.ChamferFill(), Color.white);
            label = NewText("Label", top, font, 12f, FontStyles.Bold);
            var labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(1f, 0.5f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.anchoredPosition = new Vector2(8f, 6f);
            labelRect.sizeDelta = new Vector2(110f, 18f);
            label.alignment = TextAlignmentOptions.Left;
            label.text = ControlTerminalBindings.MouseKey;
            role = NewText("Role", top, font, 11f, FontStyles.Normal);
            var roleRect = (RectTransform)role.transform;
            roleRect.anchorMin = roleRect.anchorMax = new Vector2(1f, 0.5f);
            roleRect.pivot = new Vector2(0f, 0.5f);
            roleRect.anchoredPosition = new Vector2(8f, -9f);
            roleRect.sizeDelta = new Vector2(110f, 16f);
            role.alignment = TextAlignmentOptions.Left;
        }

        /// <summary>조작 방식이 바뀔 때 기능·역할 글자·색을 갱신한다. 키 오브젝트는 다시 만들지 않는다.</summary>
        public void Bind(ControlScheme scheme)
        {
            Function = ControlTerminalBindings.FunctionOf(scheme, Id);
            if (role != null) role.text = ControlTerminalBindings.RoleLabel(scheme, Id);
            if (hit != null) hit.raycastTarget = IsFunctionKey;
            if (!IsFunctionKey)
            {
                hovered = false;
                demoDown = false;
                motion.SetDown(false);
            }

            var colors = RoleColors(Role);
            faceColor = colors.face;
            edgeColor = colors.edge;
            if (label != null) label.color = IsFunctionKey ? TextOn : TextNone;
            if (role != null) role.color = IsFunctionKey ? colors.edge : TextNone;
            Apply(true);
        }

        public void SetDemoDown(bool value)
        {
            if (value && !IsFunctionKey) return;
            demoDown = value;
            SyncMotion();
        }

        /// <summary>등장 때 키가 튀어나오는 값을 적용한다. 값이 바뀔 때만 건드린다.</summary>
        public void SetIntroPop(ControlTerminalKeyPop value)
        {
            if (pop.Scale == value.Scale && pop.Lift == value.Lift && pop.Alpha == value.Alpha) return;
            pop = value;
            ApplyPop();
        }

        private void ApplyPop()
        {
            if (visual == null) return;
            visual.localScale = new Vector3(pop.Scale, pop.Scale, 1f);
            visual.anchoredPosition = new Vector2(0f, pop.Lift);
            visualGroup.alpha = pop.Alpha;
        }

        /// <summary>호버·시연 상태를 즉시 놓는다(페이지 전환·닫힘). 눌린 모양도 기준 위치로 되돌린다.</summary>
        public void ResetImmediate()
        {
            hovered = false;
            demoDown = false;
            pop = new ControlTerminalKeyPop(1f, 0f, 1f);
            ApplyPop();
            motion.ResetImmediate();
            Apply(true);
        }

        public void Tick(float deltaSeconds)
        {
            motion.Tick(deltaSeconds);
            Apply(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!IsFunctionKey) return;
            hovered = true;
            SyncMotion();
            if (HoverEntered != null) HoverEntered(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!hovered) return;
            hovered = false;
            SyncMotion();
            if (HoverExited != null) HoverExited(this);
        }

        private void OnDisable()
        {
            // 비활성화될 때 호버가 남지 않게 한다(Exit 이벤트가 오지 않는다).
            if (hovered && HoverExited != null) HoverExited(this);
            hovered = false;
            demoDown = false;
            motion.ResetImmediate();
        }

        private void SyncMotion()
        {
            motion.SetDown(hovered || demoDown);
        }

        private void Apply(bool force)
        {
            var level = Level;
            if (!force && Mathf.Approximately(level, appliedLevel)) return;
            appliedLevel = level;
            if (top == null) return;
            var press = level * ControlTerminalLayout.PressTravel;
            top.anchoredPosition = new Vector2(0f, -press);
            // 눌리면 옆면이 얇아지고 그림자가 윗면에 붙는다.
            shadow.anchoredPosition = new Vector2(0f, 1f + Mathf.Max(0f, press) * 0.6f);
            var lit = Mathf.Clamp01(level);
            var face = Color.Lerp(faceColor, edgeColor, 0.14f * lit);
            topFill.color = face;
            sideFill.color = new Color(face.r * 0.42f, face.g * 0.42f, face.b * 0.42f, 1f);
            edge.color = Color.Lerp(edgeColor, Color.white, 0.4f * lit);
            var lightAlpha = IsFunctionKey ? 0.6f : 0.2f;
            highlight.color = new Color(1f, 1f, 1f, lightAlpha);
            shadowImage.color = new Color(0f, 0f, 0f, 0.5f - 0.2f * lit);
            if (mouseBody != null)
            {
                mouseBody.color = IsFunctionKey ? TextOn : TextNone;
                mouseButton.color = IsFunctionKey
                    ? Color.Lerp(edgeColor, Color.white, 0.5f * lit)
                    : new Color(0.3f, 0.4f, 0.5f, 0.5f);
            }
        }

        private static (Color face, Color edge) RoleColors(ControlKeyRole role)
        {
            switch (role)
            {
                case ControlKeyRole.Move: return (FaceMove, EdgeMove);
                case ControlKeyRole.Jump: return (FaceJump, EdgeJump);
                case ControlKeyRole.Mine: return (FaceMine, EdgeMine);
                default: return (FaceNone, EdgeNone);
            }
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Anchor(RectTransform rect, float minX, float minY, float maxX, float maxY, Vector2 pivot)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.pivot = pivot;
            rect.anchoredPosition = Vector2.zero;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static Image AddImage(RectTransform rect, Sprite sprite, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            if (sprite != null && sprite.border != Vector4.zero) image.type = Image.Type.Sliced;
            return image;
        }

        private static TMP_Text NewText(string name, Transform parent, TMP_FontAsset font, float size, FontStyles style)
        {
            var rect = NewRect(name, parent);
            Stretch(rect);
            return ResourceSellUi.Text(rect, font, size, style, Color.white, TextAlignmentOptions.Center);
        }
    }
}
