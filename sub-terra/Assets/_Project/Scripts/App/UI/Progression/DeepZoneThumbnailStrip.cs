using System;
using System.Collections.Generic;
using SubTerra.App.Audio;
using SubTerra.App.Progression;
using SubTerra.Shared.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Progression
{
    /// <summary>
    /// B-155: 업그레이드 창 아래 심층 구역 레벨별 썸네일 줄. 표시 전용이며 해금 판정·저장은 하지 않는다.
    /// 잠김은 흑백 + 회색 반짝임 + 자물쇠, 해금 직후 한 번만 자물쇠가 떨어지며 색이 퍼진다.
    /// 프리팹·씬에는 없고 UpgradeTreeView가 런타임에 만든다.
    /// </summary>
    public sealed class DeepZoneThumbnailStrip : MonoBehaviour
    {
        public const string ShaderName = "SubTerra/DeepZoneThumbnailUI";
        public const string ShaderResourceName = "DeepZoneThumbnailUI";

        private static readonly int SaturationId = Shader.PropertyToID("_Saturation");
        private static readonly int GrayBrightnessId = Shader.PropertyToID("_GrayBrightness");
        private static readonly int RevealCenterId = Shader.PropertyToID("_RevealCenter");
        private static readonly int RevealRadiusId = Shader.PropertyToID("_RevealRadius");
        private static readonly int RevealFeatherId = Shader.PropertyToID("_RevealFeather");
        private static readonly int RevealAspectId = Shader.PropertyToID("_RevealAspect");
        private static readonly int RevealRimId = Shader.PropertyToID("_RevealRim");
        private static readonly int RevealRimColorId = Shader.PropertyToID("_RevealRimColor");

        private static readonly Color LockColor = new Color(0.5f, 0.62f, 0.68f, 1f);
        private static readonly Color LockedFrame = new Color(0.16f, 0.21f, 0.25f, 1f);
        private static readonly Color BandColor = new Color(0f, 0f, 0f, 0.62f);

        private static bool shaderWarned;
        private static bool artWarned;
        private static Shader cachedShader;

        // 자물쇠 4조각은 업그레이드 노드 LockPart(PromptB118UpgradeTreeBuilder)의 1.3배.
        private const float LockScale = 1.3f;
        private const float BandHeight = 44f;
        // 떨어지며 옆으로 밀리는 거리(px).
        public const float LockDrift = 4f;

        private readonly List<Cell> cells = new List<Cell>();
        private readonly DeepZoneRevealTracker tracker = new DeepZoneRevealTracker();
        private DeepZoneThumbnailStyle style = new DeepZoneThumbnailStyle();
        private TMP_FontAsset font;
        private Func<bool> revealBlocked;
        private readonly List<SfxId> cueBuffer = new List<SfxId>();
        private readonly SfxRepeatGate cueGate = new SfxRepeatGate(UnlockRevealTimeline.SameCueSeconds);
        private bool revealing;
        private float revealElapsed;
        private float revealU;
        private int revealFrom;
        private int revealTo;

        public int CellCount => cells.Count;
        public bool HasPendingReveal => tracker.HasPending;
        public bool IsRevealing => revealing;

        /// <summary>확인용: true면 Update가 연출을 진행하지 않고 테스트가 TickReveal로 직접 진행한다.</summary>
        public bool ManualTick { get; set; }

        /// <summary>테스트용 소리 시계(같은 소리 0.1초 제한).</summary>
        public Func<float> CueClock
        {
            get => cueGate.Clock;
            set => cueGate.Clock = value ?? (() => Time.realtimeSinceStartup);
        }
        public bool HasShader => ResolveShader() != null;

        /// <summary>해금 창(B-154)이 보이는 동안 true. 그동안 연출을 미룬다.</summary>
        public Func<bool> RevealBlocked
        {
            get => revealBlocked;
            set => revealBlocked = value;
        }

        public static DeepZoneThumbnailStrip Create(RectTransform parent, TMP_FontAsset font)
        {
            var row = new GameObject("Thumbnails", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            var strip = row.AddComponent<DeepZoneThumbnailStrip>();
            strip.font = font;
            return strip;
        }

        public void Apply(DeepZoneDisplaySettings settings, ZoneAccessResult access)
        {
            if (settings == null)
            {
                return;
            }

            style = settings.ThumbnailStyle;
            EnsureCells(settings.Levels.Count);
            tracker.Apply(settings.UnlockedLevelCount(access), access.DidUnlockNow);
            var missingArt = false;
            for (var i = 0; i < cells.Count; i++)
            {
                var level = settings.Levels[i];
                var cell = cells[i];
                cell.Title.text = settings.LevelTitle(i);
                cell.LockedCondition = settings.LevelCondition(i, unlocked: false);
                cell.UnlockedCondition = settings.LevelCondition(i, unlocked: true);
                missingArt |= ApplyArt(cell, level, i);
                if (IsRevealing && i >= revealFrom && i < revealTo)
                {
                    continue;
                }

                if (tracker.IsRevealed(i))
                {
                    DrawUnlocked(cell);
                }
                else
                {
                    DrawLocked(cell);
                }
            }

            if (missingArt && !artWarned)
            {
                artWarned = true;
                Debug.LogWarning("[SubTerra] DeepZoneDisplaySettings 썸네일 sprite가 비어 있어 단색 칸으로 표시합니다.");
            }
        }

        /// <summary>대기 중인 해금 연출을 조건이 맞으면 한 번 시작한다. 시작했으면 true.</summary>
        public bool TryPlayPending()
        {
            if (!tracker.HasPending || IsRevealing || !isActiveAndEnabled || IsBlocked())
            {
                return false;
            }

            if (!tracker.TryTakePending(out var from, out var to))
            {
                return false;
            }

            revealFrom = from;
            revealTo = Mathf.Min(to, cells.Count);
            if (!Application.isPlaying && !ManualTick)
            {
                // 에디트 모드에는 Update 루프가 없으므로 끝 상태로 바로 맞춘다(무음).
                FinishReveal();
                return true;
            }

            // B-169: 한 번에 여러 칸이 풀려도 소리는 이 연출 한 세트만 낸다.
            revealing = true;
            revealElapsed = 0f;
            revealU = UnlockRevealTimeline.BeforeStart;
            StepReveal(0f);
            return true;
        }

        private bool IsBlocked()
        {
            try
            {
                return revealBlocked != null && revealBlocked();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return false;
            }
        }

        private void Update()
        {
            // 시작한 프레임에 한 번 더 진행하지 않도록 진행을 먼저 한다.
            if (revealing && !ManualTick)
            {
                TickReveal(Time.unscaledDeltaTime);
            }

            if (tracker.HasPending && !IsRevealing)
            {
                TryPlayPending();
            }

            var now = Time.unscaledTime;
            for (var i = 0; i < cells.Count; i++)
            {
                UpdateGlint(cells[i], i, now);
            }
        }

        private void OnDisable()
        {
            // 연출 도중 창이 꺼지면 바로 끝 상태로 맞춘다. 아직 시작 안 한 대기는 다음에 열 때 재생한다.
            if (revealing)
            {
                FinishReveal();
            }
        }

        private void OnDestroy()
        {
            for (var i = 0; i < cells.Count; i++)
            {
                DestroyMaterial(cells[i].ThumbnailMaterial);
                DestroyMaterial(cells[i].GlintMaterial);
            }

            cells.Clear();
        }

        /// <summary>진행 중인 해금 연출을 dt만큼 진행한다. Update와 테스트가 같은 경로를 쓴다.</summary>
        public void TickReveal(float deltaTime)
        {
            if (!revealing)
            {
                return;
            }

            revealElapsed += Mathf.Max(0f, deltaTime);
            var duration = Mathf.Max(0.0001f, style.unlockDuration);
            StepReveal(Mathf.Clamp01(revealElapsed / duration));
        }

        private void StepReveal(float u)
        {
            cueBuffer.Clear();
            UnlockRevealTimeline.CollectCues(revealU, u, cueBuffer);
            revealU = u;
            for (var i = revealFrom; i < revealTo; i++)
            {
                SampleReveal(cells[i], u);
            }

            for (var i = 0; i < cueBuffer.Count; i++)
            {
                cueGate.TryPlay(cueBuffer[i]);
            }

            if (u >= 1f)
            {
                FinishReveal();
            }
        }

        private void FinishReveal()
        {
            revealing = false;
            for (var i = revealFrom; i < revealTo && i < cells.Count; i++)
            {
                DrawUnlocked(cells[i]);
            }

            revealFrom = revealTo = 0;
        }

        // ---------- 상태 그리기 ----------

        private void DrawLocked(Cell cell)
        {
            cell.Unlocked = false;
            SetLockPose(cell, Vector2.zero, 0f, 1f, 0f, 1f);
            cell.LockRoot.gameObject.SetActive(true);
            SetColorState(cell, 0f, 0f, 0f);
            cell.Frame.color = LockedFrame;
            cell.GlintBlend = 0f;
            cell.GlintBoost = 0f;
            SetCaption(cell, false);
            UpdateGlint(cell, cells.IndexOf(cell), 0f);
        }

        private void DrawUnlocked(Cell cell)
        {
            cell.Unlocked = true;
            SetLockPose(cell, Vector2.zero, 0f, 1f, 0f, 1f);
            cell.LockRoot.gameObject.SetActive(false);
            SetColorState(cell, 1f, 0f, 0f);
            cell.Frame.color = UpgradeTreeTween.CyanDim;
            cell.GlintBlend = 1f;
            cell.GlintBoost = 0f;
            SetCaption(cell, true);
            UpdateGlint(cell, cells.IndexOf(cell), 0f);
        }

        /// <summary>
        /// 해금 연출 한 장면(u: 0~1). #188 노드 해금과 같은 톤:
        /// 진동 → 딸깍 → 떨어짐, 자물쇠 자리부터 색 퍼짐, 끝에 광물이 한 번 밝게 빛난다. u=1은 해금 상태와 같다.
        /// </summary>
        public void SampleReveal(int index, float u)
        {
            if (index >= 0 && index < cells.Count)
            {
                SampleReveal(cells[index], u);
            }
        }

        private void SampleReveal(Cell cell, float u)
        {
            if (u >= 1f)
            {
                DrawUnlocked(cell);
                return;
            }

            var s = style;
            // B-169: 자물쇠 자세는 업그레이드 노드와 같은 공용 시간표를 쓴다.
            var pose = UnlockRevealTimeline.SampleLock(u, s.shackleKick, s.lockDrop, s.lockDropAngle, LockDrift);
            if (pose.Visible)
            {
                SetLockPose(cell, pose.Offset, pose.Angle, pose.Scale, pose.ShackleLift, pose.Alpha);
            }

            cell.LockRoot.gameObject.SetActive(pose.Visible);

            var spread = u < 0.3f ? 0f : UpgradeTreeTween.EaseOut((u - 0.3f) / 0.7f);
            var rim = spread > 0f ? 1f - spread * spread : 0f;
            SetColorState(cell, 0f, spread * s.revealRadius, rim, spread);

            var flash = u < 0.3f ? 0f : UpgradeTreeTween.Pulse(Mathf.Clamp01((u - 0.3f) / 0.4f));
            var frame = Color.Lerp(LockedFrame, UpgradeTreeTween.CyanDim, spread);
            cell.Frame.color = Color.Lerp(frame, UpgradeTreeTween.Cyan, flash * 0.7f);

            cell.GlintBlend = spread;
            cell.GlintBoost = u >= 0.85f ? UpgradeTreeTween.Pulse((u - 0.85f) / 0.15f) : 0f;
            SetCaption(cell, u >= 0.65f);
        }

        private void SetLockPose(Cell cell, Vector2 offset, float angle, float scale, float shackleLift, float alpha)
        {
            cell.LockRoot.anchoredPosition = offset;
            cell.LockRoot.localRotation = Quaternion.Euler(0f, 0f, angle);
            cell.LockRoot.localScale = new Vector3(scale, scale, 1f);
            cell.ShackleTop.rectTransform.anchoredPosition = cell.ShackleTopBase + new Vector2(0f, shackleLift);
            cell.ShackleRight.rectTransform.anchoredPosition = cell.ShackleRightBase + new Vector2(0f, shackleLift);
            for (var i = 0; i < cell.LockParts.Length; i++)
            {
                UpgradeTreeTween.SetAlpha(cell.LockParts[i], alpha);
            }
        }

        private void SetColorState(Cell cell, float saturation, float radius, float rim, float fallbackBlend = -1f)
        {
            var baseColor = cell.HasSprite ? Color.white : cell.FallbackColor;
            if (cell.ThumbnailMaterial != null)
            {
                cell.Thumbnail.color = baseColor;
                ApplyMaterial(cell.ThumbnailMaterial, saturation, radius, rim, style.lockedGrayBrightness, cell);
                ApplyMaterial(cell.GlintMaterial, saturation, radius, 0f, style.glintGrayBrightness, cell);
                return;
            }

            // 셰이더 대체: 잠김은 회색 틴트, 해금 연출은 색 페이드로만 한다.
            var amount = fallbackBlend >= 0f ? fallbackBlend : saturation;
            var locked = baseColor * style.fallbackLockedTint;
            locked.a = 1f;
            cell.Thumbnail.color = Color.Lerp(locked, baseColor, amount);
        }

        private void ApplyMaterial(Material material, float saturation, float radius, float rim, float grayBrightness, Cell cell)
        {
            if (material == null)
            {
                return;
            }

            material.SetFloat(SaturationId, saturation);
            material.SetFloat(GrayBrightnessId, grayBrightness);
            material.SetFloat(RevealRadiusId, radius);
            material.SetFloat(RevealFeatherId, style.revealFeather);
            material.SetFloat(RevealRimId, rim);
            material.SetColor(RevealRimColorId, UpgradeTreeTween.Cyan);
            material.SetVector(RevealCenterId, new Vector4(style.lockCenter.x, style.lockCenter.y, 0f, 0f));
            var size = cell.Thumbnail.rectTransform.rect.size;
            material.SetFloat(RevealAspectId, size.y > 1f ? size.x / size.y : 3f);
        }

        private void SetCaption(Cell cell, bool unlocked)
        {
            cell.Condition.text = unlocked ? cell.UnlockedCondition : cell.LockedCondition;
            cell.Condition.color = unlocked ? UpgradeTreeTween.Cyan : UpgradeTreeTween.TextDim;
            cell.Title.color = unlocked ? UpgradeTreeTween.TextMain : UpgradeTreeTween.TextDim;
        }

        private void UpdateGlint(Cell cell, int index, float now)
        {
            if (cell == null || cell.Glint == null || !cell.Glint.enabled)
            {
                return;
            }

            // 느린 사인. 칸마다 위상과 주기를 조금씩 어긋내 세 장이 함께 번쩍이지 않게 한다.
            var s = style;
            var period = Mathf.Max(2f, s.glintPeriod) * (1f + s.glintPeriodSpread * index);
            var phase = index * 0.37f;
            var wave = 0.5f - 0.5f * Mathf.Cos((now / period + phase) * Mathf.PI * 2f);
            var range = Vector2.Lerp(s.lockedGlintAlpha, s.unlockedGlintAlpha, cell.GlintBlend);
            var alpha = Mathf.Lerp(range.x, range.y, wave);
            alpha = Mathf.Lerp(alpha, 1f, cell.GlintBoost);
            var color = Color.Lerp(s.lockedGlintColor, s.unlockedGlintColor, cell.GlintBlend);
            UpgradeTreeTween.SetColor(cell.Glint, color, alpha);
        }

        // ---------- 그림 ----------

        private bool ApplyArt(Cell cell, DeepZoneDisplayLevel level, int index)
        {
            var thumbnail = level != null ? level.Thumbnail : null;
            var mask = level != null ? level.MineralMask : null;
            cell.HasSprite = thumbnail != null;
            cell.FallbackColor = style.FallbackColorFor(index);
            cell.Thumbnail.sprite = thumbnail;
            cell.Fitter.enabled = thumbnail != null && thumbnail.rect.height > 0f;
            if (cell.Fitter.enabled)
            {
                cell.Fitter.aspectRatio = thumbnail.rect.width / thumbnail.rect.height;
            }
            else
            {
                var rect = cell.Thumbnail.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            cell.Glint.sprite = mask;
            cell.Glint.enabled = mask != null;
            return thumbnail == null;
        }

        private void EnsureCells(int count)
        {
            while (cells.Count > count)
            {
                var last = cells[cells.Count - 1];
                cells.RemoveAt(cells.Count - 1);
                DestroyMaterial(last.ThumbnailMaterial);
                DestroyMaterial(last.GlintMaterial);
                DestroyObject(last.Root.gameObject);
            }

            while (cells.Count < count)
            {
                cells.Add(CreateCell(cells.Count));
            }
        }

        private Cell CreateCell(int index)
        {
            var cell = new Cell();
            var root = NewImage("DeepZoneLevel" + (index + 1), transform, LockedFrame);
            cell.Root = root.rectTransform;
            cell.Frame = root;
            root.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            var clip = NewRect("Clip", cell.Root);
            Stretch(clip, new Vector2(1f, 1f), new Vector2(-1f, -1f));
            clip.gameObject.AddComponent<RectMask2D>();

            cell.Thumbnail = NewImage("Thumbnail", clip, Color.white);
            Stretch(cell.Thumbnail.rectTransform, Vector2.zero, Vector2.zero);
            cell.Fitter = cell.Thumbnail.gameObject.AddComponent<AspectRatioFitter>();
            cell.Fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            cell.Fitter.aspectRatio = 3f;

            cell.Glint = NewImage("MineralGlint", cell.Thumbnail.rectTransform, Color.clear);
            Stretch(cell.Glint.rectTransform, Vector2.zero, Vector2.zero);

            var shader = ResolveShader();
            if (shader != null)
            {
                cell.ThumbnailMaterial = new Material(shader) { name = "DeepZoneThumbnail (Instance)", hideFlags = HideFlags.DontSave };
                cell.GlintMaterial = new Material(shader) { name = "DeepZoneGlint (Instance)", hideFlags = HideFlags.DontSave };
                cell.Thumbnail.material = cell.ThumbnailMaterial;
                cell.Glint.material = cell.GlintMaterial;
            }

            cell.LockRoot = NewRect("Lock", clip);
            cell.LockRoot.anchorMin = cell.LockRoot.anchorMax = style.lockCenter;
            cell.LockRoot.pivot = new Vector2(0.5f, 0.5f);
            cell.LockRoot.sizeDelta = new Vector2(30f, 34f) * LockScale;
            cell.LockParts = new[]
            {
                LockPart(cell.LockRoot, "LockBody", new Vector2(0f, -6f), new Vector2(26f, 18f)),
                LockPart(cell.LockRoot, "ShackleLeft", new Vector2(-8f, 6f), new Vector2(5f, 16f)),
                LockPart(cell.LockRoot, "ShackleRight", new Vector2(8f, 6f), new Vector2(5f, 16f)),
                LockPart(cell.LockRoot, "ShackleTop", new Vector2(0f, 14f), new Vector2(21f, 5f))
            };
            cell.ShackleRight = cell.LockParts[2];
            cell.ShackleTop = cell.LockParts[3];
            cell.ShackleRightBase = cell.ShackleRight.rectTransform.anchoredPosition;
            cell.ShackleTopBase = cell.ShackleTop.rectTransform.anchoredPosition;

            var band = NewImage("Band", clip, BandColor);
            band.rectTransform.anchorMin = Vector2.zero;
            band.rectTransform.anchorMax = new Vector2(1f, 0f);
            band.rectTransform.pivot = new Vector2(0.5f, 0f);
            band.rectTransform.offsetMin = Vector2.zero;
            band.rectTransform.offsetMax = new Vector2(0f, BandHeight);

            cell.Title = NewText("Title", band.rectTransform, 16f, FontStyles.Bold);
            cell.Title.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            cell.Title.rectTransform.anchorMax = Vector2.one;
            cell.Title.rectTransform.offsetMin = new Vector2(8f, -3f);
            cell.Title.rectTransform.offsetMax = new Vector2(-8f, 0f);

            cell.Condition = NewText("Condition", band.rectTransform, 13f, FontStyles.Normal);
            cell.Condition.rectTransform.anchorMin = Vector2.zero;
            cell.Condition.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            cell.Condition.rectTransform.offsetMin = new Vector2(8f, 1f);
            cell.Condition.rectTransform.offsetMax = new Vector2(-8f, 1f);
            return cell;
        }

        private Image LockPart(RectTransform parent, string name, Vector2 position, Vector2 size)
        {
            var image = NewImage(name, parent, LockColor);
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position * LockScale;
            rect.sizeDelta = size * LockScale;
            var shadow = image.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            return image;
        }

        private TextMeshProUGUI NewText(string name, RectTransform parent, float size, FontStyles fontStyle)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            if (font != null)
            {
                text.font = font;
            }

            text.fontSize = size;
            text.fontStyle = fontStyle;
            text.enableAutoSizing = true;
            text.fontSizeMin = 11f;
            text.fontSizeMax = size;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        private static Image NewImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static Shader ResolveShader()
        {
            if (cachedShader != null)
            {
                return cachedShader;
            }

            try
            {
                cachedShader = Resources.Load<Shader>(ShaderResourceName);
                if (cachedShader == null)
                {
                    cachedShader = Shader.Find(ShaderName);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                cachedShader = null;
            }

            if ((cachedShader == null || !cachedShader.isSupported) && !shaderWarned)
            {
                shaderWarned = true;
                Debug.LogWarning("[SubTerra] " + ShaderName + " 셰이더를 읽지 못해 심층 구역 썸네일을 회색 틴트로 대체합니다.");
            }

            if (cachedShader != null && !cachedShader.isSupported)
            {
                cachedShader = null;
            }

            return cachedShader;
        }

        private static void DestroyMaterial(Material material)
        {
            if (material != null)
            {
                DestroyObject(material);
            }
        }

        private static void DestroyObject(UnityEngine.Object target)
        {
            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        // ---------- 테스트·점검용 읽기 ----------

        public readonly struct CellState
        {
            public readonly bool Unlocked;
            public readonly bool LockVisible;
            public readonly float Saturation;
            public readonly float RevealRadius;
            public readonly Color ThumbnailColor;
            public readonly Color FrameColor;
            public readonly bool GlintEnabled;
            public readonly Color GlintColor;
            public readonly bool HasSprite;
            public readonly bool HasMaterial;
            public readonly string Title;
            public readonly string Condition;

            public CellState(Cell cell)
            {
                Unlocked = cell.Unlocked;
                LockVisible = cell.LockRoot.gameObject.activeSelf;
                HasMaterial = cell.ThumbnailMaterial != null;
                Saturation = HasMaterial ? cell.ThumbnailMaterial.GetFloat(SaturationId) : -1f;
                RevealRadius = HasMaterial ? cell.ThumbnailMaterial.GetFloat(RevealRadiusId) : -1f;
                ThumbnailColor = cell.Thumbnail.color;
                FrameColor = cell.Frame.color;
                GlintEnabled = cell.Glint.enabled;
                GlintColor = cell.Glint.color;
                HasSprite = cell.HasSprite;
                Title = cell.Title.text;
                Condition = cell.Condition.text;
            }
        }

        public CellState GetCellState(int index) => new CellState(cells[index]);
        public RectTransform GetCellRect(int index) => cells[index].Root;

        public sealed class Cell
        {
            internal RectTransform Root;
            internal Image Frame;
            internal Image Thumbnail;
            internal AspectRatioFitter Fitter;
            internal Image Glint;
            internal Material ThumbnailMaterial;
            internal Material GlintMaterial;
            internal RectTransform LockRoot;
            internal Image[] LockParts;
            internal Image ShackleTop;
            internal Image ShackleRight;
            internal Vector2 ShackleTopBase;
            internal Vector2 ShackleRightBase;
            internal TextMeshProUGUI Title;
            internal TextMeshProUGUI Condition;
            internal string LockedCondition = string.Empty;
            internal string UnlockedCondition = string.Empty;
            internal bool HasSprite;
            internal Color FallbackColor;
            internal bool Unlocked;
            internal float GlintBlend;
            internal float GlintBoost;
        }
    }
}
