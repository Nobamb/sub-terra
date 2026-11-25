using System;
using System.Collections;
using System.IO;
using SubTerra.App.Audio;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

namespace SubTerra.App.UI.Intro
{
    /// <summary>
    /// 새 게임 인트로. 화면 전체 오버레이에서 StreamingAssets의 MP4를 재생하고,
    /// 영상 끝의 꺼짐을 되감듯 게임 화면이 켜지는 연출로 이어 준다.
    /// B-209부터 광부 선택 화면의 글리치 전환 직후(새 게임 바로 뒤) RunNewGameLaunch로 재생한다:
    /// 검은 화면 → 영상 → 새 게임 시작(씬 로드, 그동안 검은 화면 유지) → 켜짐 연출.
    /// 오버레이는 코드로 만들어 Scene/프리팹 YAML을 건드리지 않는다.
    /// </summary>
    public sealed class NewGameIntroPlayer : MonoBehaviour
    {
        public const string PauseOwner = "new-game-intro";
        public const string VideoRelativePath = "Video/sub-terra-intro.mp4";
        /// <summary>B-212: 여자 광부를 고르면 재생하는 영상. 없으면 남자 영상으로 폴백한다.</summary>
        public const string FemaleVideoRelativePath = "Video/sub-terra-intro-female.mp4";
        /// <summary>Canvas.sortingOrder 최댓값. 설정창(32_760)보다도 위에서 화면을 덮는다.</summary>
        public const int SortingOrder = 32_767;
        /// <summary>선택 화면에서 누른 키가 바로 건너뛰기로 읽히지 않게 하는 시간.</summary>
        public const float SkipGraceSeconds = 0.6f;
        public const float PrepareTimeoutSeconds = 10f;
        /// <summary>켜진 게임 화면을 잠깐 보여 준 뒤 HUD·브리핑으로 넘긴다.</summary>
        public const float HoldAfterTurnOnSeconds = 0.35f;
        /// <summary>새 게임 씬 로드·첫 저장이 끝나기를 기다리는 상한. 넘으면 그냥 켠다.</summary>
        public const float LoadTimeoutSeconds = 20f;
        private const int NoiseBarCount = 8;
        private static readonly Color Cyan = new Color(0.42f, 0.94f, 1f, 1f);

        private RectTransform shutterTop;
        private RectTransform shutterBottom;
        private RectTransform line;
        private Image lineImage;
        private Image flashImage;
        private RawImage videoImage;
        private RectTransform[] noiseBars;
        private Image[] noiseImages;
        private VideoPlayer videoPlayer;
        private RenderTexture renderTexture;
        private bool loopPointReached;
        private bool videoFailed;
        private bool holdsPause;
        private string videoUrl;

        public static string VideoPath => PathFor(VideoRelativePath);

        public static string PathFor(string relativePath) => Path.Combine(Application.streamingAssetsPath, relativePath);

        /// <summary>성별이 원래 재생해야 하는 영상(파일 유무와 무관).</summary>
        public static string VideoRelativePathFor(PlayerGender gender)
        {
            return gender == PlayerGender.Female ? FemaleVideoRelativePath : VideoRelativePath;
        }

        /// <summary>
        /// 실제로 재생할 영상 전체 경로. 여자 파일이 없으면 남자 파일로 폴백하고 경고를 남긴다. 둘 다 없으면 null.
        /// </summary>
        public static string ResolveVideoPath(PlayerGender gender)
        {
            var relative = NewGameIntroPolicy.ResolveVideoRelativePath(
                gender, VideoRelativePath, FemaleVideoRelativePath, FileExistsInStreamingAssets, out var fellBack);
            if (fellBack)
            {
                Debug.LogWarning("[SubTerra] Intro video for " + gender + " not found at " + PathFor(VideoRelativePathFor(gender))
                    + (relative != null ? "; falling back to " + relative + "." : "; no intro video available."));
            }
            else if (relative == null)
            {
                Debug.LogWarning("[SubTerra] Intro video not found at " + PathFor(VideoRelativePathFor(gender)) + "; skipping to turn-on.");
            }

            return relative == null ? null : PathFor(relative);
        }

        private static bool FileExistsInStreamingAssets(string relativePath)
        {
            try
            {
                return File.Exists(PathFor(relativePath));
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool VideoAvailable()
        {
            try
            {
                return File.Exists(VideoPath);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static NewGameIntroPlayer Create()
        {
            var go = new GameObject("NewGameIntroOverlay", typeof(RectTransform));
            var player = go.AddComponent<NewGameIntroPlayer>();
            player.BuildOverlay();
            return player;
        }

        /// <summary>영상(선택) → 켜짐 연출 → 정리. 끝나면 정지를 풀고 오버레이를 없앤다.</summary>
        public IEnumerator Run(bool playVideo)
        {
            AcquirePause();
            // 셔터를 먼저 닫아 영상 준비 중에도 게임 화면이 비치지 않게 한다.
            ApplyTurnOn(new NewGameTurnOnFrame(0f, 0f, 0f, 0f, 0f, 0, false));
            try
            {
                if (playVideo)
                {
                    yield return PlayVideo();
                }

                yield return PlayTurnOn();
                yield return new WaitForSecondsRealtime(HoldAfterTurnOnSeconds);
            }
            finally
            {
                ReleasePause();
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// B-209 새 게임 시작 순서. 오버레이는 씬이 바뀌어도 남아(DontDestroyOnLoad) 메인 메뉴가 다시 비치지 않는다.
        /// startGame이 false면(또는 예외) onStartFailed를 부르고 오버레이를 걷는다. 그때 화면은 아직 메인 메뉴다.
        /// isGameReady가 true가 될 때까지 검은 화면을 유지한 뒤 켜짐 연출을 한다.
        /// videoPath(B-212)는 재생할 영상 전체 경로이고, 비우면 남자 영상을 연다.
        /// </summary>
        public IEnumerator RunNewGameLaunch(bool playVideo, Func<bool> startGame, Func<bool> isGameReady, Action onStartFailed, string videoPath = null)
        {
            if (transform.parent == null) DontDestroyOnLoad(gameObject);
            // B-212: 고른 광부 성별로 정한 파일. 비어 있으면 남자 영상(기존 동작).
            videoUrl = videoPath;
            // 셔터를 먼저 닫아 영상 준비·씬 로드 중에도 뒤 화면이 비치지 않게 한다.
            ApplyTurnOn(new NewGameTurnOnFrame(0f, 0f, 0f, 0f, 0f, 0, false));
            var started = false;
            try
            {
                if (playVideo)
                {
                    yield return PlayVideo();
                }

                try
                {
                    started = startGame != null && startGame();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[SubTerra] New game start failed during intro. " + ex);
                    started = false;
                }

                if (!started)
                {
                    yield break;
                }

                var giveUp = Time.realtimeSinceStartup + LoadTimeoutSeconds;
                while (Time.realtimeSinceStartup < giveUp)
                {
                    var ready = false;
                    try
                    {
                        ready = isGameReady == null || isGameReady();
                    }
                    catch (Exception)
                    {
                        ready = true;
                    }

                    if (ready) break;
                    yield return null;
                }

                // 새 씬의 첫 배치·레이아웃이 끝난 프레임에서 켠다.
                yield return null;
                AcquirePause();
                yield return PlayTurnOn();
                yield return new WaitForSecondsRealtime(HoldAfterTurnOnSeconds);
            }
            finally
            {
                ReleasePause();
                Destroy(gameObject);
                if (!started && onStartFailed != null)
                {
                    try
                    {
                        onStartFailed();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
            }
        }

        private IEnumerator PlayVideo()
        {
            if (!SetUpVideo())
            {
                yield break;
            }

            var timeout = Time.realtimeSinceStartup + PrepareTimeoutSeconds;
            while (!videoPlayer.isPrepared && !videoFailed && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            if (!videoPlayer.isPrepared)
            {
                Debug.LogWarning("[SubTerra] Intro video was not ready; skipping to turn-on.");
                yield break;
            }

            videoImage.texture = renderTexture;
            videoImage.enabled = true;
            videoPlayer.Play();

            var started = Time.realtimeSinceStartup;
            var maxEnd = started + (float)videoPlayer.length + 3f;
            while (!loopPointReached && !videoFailed && Time.realtimeSinceStartup < maxEnd)
            {
                if (Time.realtimeSinceStartup - started >= SkipGraceSeconds && SkipPressed())
                {
                    break;
                }

                yield return null;
            }

            videoPlayer.Stop();
            videoImage.enabled = false;
        }

        private IEnumerator PlayTurnOn()
        {
            SfxPlayer.Play(SfxId.UiCctvOn);
            var timeline = new NewGameTurnOnTimeline();
            var frame = timeline.Evaluate();
            while (!frame.IsComplete)
            {
                frame = timeline.Advance(Time.unscaledDeltaTime);
                ApplyTurnOn(frame);
                yield return null;
            }
        }

        private bool SetUpVideo()
        {
            try
            {
                renderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
                renderTexture.Create();

                var audio = gameObject.AddComponent<AudioSource>();
                audio.playOnAwake = false;
                audio.spatialBlend = 0f;

                videoPlayer = gameObject.AddComponent<VideoPlayer>();
                videoPlayer.playOnAwake = false;
                videoPlayer.isLooping = false;
                videoPlayer.skipOnDrop = true;
                videoPlayer.waitForFirstFrame = true;
                videoPlayer.source = VideoSource.Url;
                videoPlayer.url = string.IsNullOrEmpty(videoUrl) ? VideoPath : videoUrl;
                videoPlayer.renderMode = VideoRenderMode.RenderTexture;
                videoPlayer.targetTexture = renderTexture;
                videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                videoPlayer.controlledAudioTrackCount = 1;
                videoPlayer.SetTargetAudioSource(0, audio);
                videoPlayer.loopPointReached += OnLoopPointReached;
                videoPlayer.errorReceived += OnVideoError;
                videoPlayer.Prepare();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[SubTerra] Intro video setup failed: " + ex.Message);
                return false;
            }
        }

        private void OnLoopPointReached(VideoPlayer source) => loopPointReached = true;

        private void OnVideoError(VideoPlayer source, string message)
        {
            Debug.LogWarning("[SubTerra] Intro video error: " + message);
            videoFailed = true;
        }

        private static bool SkipPressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.anyKey.wasPressedThisFrame) return true;
            var mouse = Mouse.current;
            return mouse != null
                && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame);
        }

        private void ApplyTurnOn(NewGameTurnOnFrame frame)
        {
            if (shutterTop == null) return;

            // 셔터는 중앙에서 위아래로 갈라진다. Open=0이면 화면 전체가 검게 덮인다.
            var half = Mathf.Max(0f, 1f - frame.Open) * 0.5f;
            SetAnchors(shutterTop, 1f - half, 1f);
            SetAnchors(shutterBottom, 0f, half);

            line.anchorMin = new Vector2(0.5f - frame.LineWidth * 0.5f, 0.5f);
            line.anchorMax = new Vector2(0.5f + frame.LineWidth * 0.5f, 0.5f);
            var lineColor = Color.Lerp(Color.white, Cyan, 0.35f);
            lineColor.a = frame.LineAlpha;
            lineImage.color = lineColor;

            var flash = Color.white;
            flash.a = frame.FlashAlpha;
            flashImage.color = flash;

            var random = new System.Random(frame.NoiseSeed * 7919 + 13);
            for (var i = 0; i < NoiseBarCount; i++)
            {
                var y = (float)random.NextDouble();
                var h = 0.004f + (float)random.NextDouble() * 0.03f;
                var w = 0.35f + (float)random.NextDouble() * 0.65f;
                var x = (float)random.NextDouble() * (1f - w);
                noiseBars[i].anchorMin = new Vector2(x, y);
                noiseBars[i].anchorMax = new Vector2(x + w, Mathf.Min(1f, y + h));
                var tint = Color.Lerp(Cyan, Color.white, (float)random.NextDouble() * 0.6f);
                tint.a = frame.NoiseAlpha * (0.35f + (float)random.NextDouble() * 0.65f);
                noiseImages[i].color = frame.NoiseAlpha > 0f ? tint : Color.clear;
            }
        }

        private static void SetAnchors(RectTransform rect, float minY, float maxY)
        {
            rect.anchorMin = new Vector2(0f, Mathf.Clamp01(minY));
            rect.anchorMax = new Vector2(1f, Mathf.Clamp01(maxY));
        }

        private void BuildOverlay()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            // 셔터가 클릭을 받아 아래 월드/HUD로 입력이 새지 않게 한다.
            shutterTop = NewImage("ShutterTop", Color.black, true).rectTransform;
            shutterBottom = NewImage("ShutterBottom", Color.black, true).rectTransform;

            var frame = new GameObject("VideoFrame", typeof(RectTransform));
            frame.transform.SetParent(transform, false);
            var frameRect = (RectTransform)frame.transform;
            frameRect.anchorMin = frameRect.anchorMax = new Vector2(0.5f, 0.5f);
            frameRect.sizeDelta = Vector2.zero;
            var fitter = frame.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 16f / 9f;
            videoImage = frame.AddComponent<RawImage>();
            videoImage.raycastTarget = false;
            videoImage.enabled = false;

            noiseBars = new RectTransform[NoiseBarCount];
            noiseImages = new Image[NoiseBarCount];
            for (var i = 0; i < NoiseBarCount; i++)
            {
                var bar = NewImage("Noise" + i, Color.clear, false);
                noiseImages[i] = bar;
                noiseBars[i] = bar.rectTransform;
            }

            flashImage = NewImage("Flash", Color.clear, false);
            lineImage = NewImage("Line", Color.clear, false);
            line = lineImage.rectTransform;
            line.offsetMin = new Vector2(0f, -2f);
            line.offsetMax = new Vector2(0f, 2f);
        }

        private Image NewImage(string childName, Color color, bool raycast)
        {
            var go = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            var rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return image;
        }

        private void AcquirePause()
        {
            if (holdsPause) return;
            holdsPause = true;
            UiPauseGate.Acquire(PauseOwner);
        }

        private void ReleasePause()
        {
            if (!holdsPause) return;
            holdsPause = false;
            UiPauseGate.Release(PauseOwner);
        }

        private void OnDestroy()
        {
            ReleasePause();
            if (videoPlayer != null)
            {
                videoPlayer.loopPointReached -= OnLoopPointReached;
                videoPlayer.errorReceived -= OnVideoError;
                videoPlayer.Stop();
            }

            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }
        }
    }
}
