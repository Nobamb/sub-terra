using System.Collections.Generic;
using SubTerra.Shared.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.MainMenu
{
    /// <summary>
    /// 설정창 오디오 칸의 배경음·효과음 줄. 마스터 줄(캡션, 슬라이더, % 글자)을 런타임에 복제해 붙이고
    /// 카드를 두 줄만큼 늘린다. 프리팹과 씬 직렬화는 건드리지 않으며 한 번만 만든다.
    /// </summary>
    public sealed class SettingsAudioRows
    {
        public const float RowSpacing = 52f;
        public const int ExtraRowCount = 2;
        public const float CardHeightGrowth = RowSpacing * ExtraRowCount;

        public const string BgmSliderName = "BgmVolumeSlider";
        public const string SfxSliderName = "SfxVolumeSlider";
        public const string StretchedArtName = "CardArtStretched";

        // 카드 그림(1448×1086)에서 오디오 칸 본문 안쪽의 테두리·글자 없는 가로 띠. 위에서부터의 px.
        private const float ArtReferenceHeight = 1086f;
        private const float ArtBandTopPx = 276f;
        private const float ArtBandBottomPx = 298f;

        public sealed class Row
        {
            public AudioChannel Channel;
            public Slider Slider;
            public TMP_Text Caption;
            public TMP_Text Label;
        }

        public Row Bgm { get; private set; }
        public Row Sfx { get; private set; }

        public IEnumerable<Row> Rows
        {
            get
            {
                yield return Bgm;
                yield return Sfx;
            }
        }

        public bool IsAlive => Bgm != null && Bgm.Slider != null && Sfx != null && Sfx.Slider != null;

        /// <summary>이미 만든 줄을 찾는다. 없으면 null.</summary>
        public static SettingsAudioRows Find(RectTransform card)
        {
            if (card == null) return null;
            var bgm = FindRow(card, AudioChannel.Bgm, BgmSliderName);
            var sfx = FindRow(card, AudioChannel.Sfx, SfxSliderName);
            if (bgm == null || sfx == null) return null;
            return new SettingsAudioRows { Bgm = bgm, Sfx = sfx };
        }

        /// <summary>줄이 없으면 만들고, 있으면 그대로 돌려준다. 두 번 불러도 줄은 늘지 않는다.</summary>
        public static SettingsAudioRows Ensure(RectTransform card, Slider masterSlider, TMP_Text masterCaption)
        {
            if (card == null || masterSlider == null || masterCaption == null) return null;
            var existing = Find(card);
            if (existing != null) return existing;

            var masterLabel = FindChild<TMP_Text>(card, "MasterVolumeLabel");
            if (masterLabel == null) return null;

            // 늘린 카드 안에서 기존 칸이 제자리에 있도록 마스터 줄과 그 위는 위로, 아래는 아래로 민다.
            float masterY = masterSlider.GetComponent<RectTransform>().anchoredPosition.y;
            ShiftExistingChildren(card, masterY);
            card.sizeDelta = new Vector2(card.sizeDelta.x, card.sizeDelta.y + CardHeightGrowth);
            StretchCardArt(card);

            var rows = new SettingsAudioRows
            {
                Bgm = CloneRow(card, masterSlider, masterCaption, masterLabel, AudioChannel.Bgm, "Bgm", 1),
                Sfx = CloneRow(card, masterSlider, masterCaption, masterLabel, AudioChannel.Sfx, "Sfx", 2)
            };
            rows.RefreshCaptions();
            return rows;
        }

        /// <summary>슬라이더 값과 % 글자를 함께 넣는다. 미리듣기는 일으키지 않는다.</summary>
        public void SetValues(float bgmVolume, float sfxVolume)
        {
            SetValue(Bgm, bgmVolume);
            SetValue(Sfx, sfxVolume);
        }

        public void RefreshLabels()
        {
            foreach (var row in Rows)
            {
                if (row != null && row.Slider != null && row.Label != null)
                    row.Label.text = FormatPercent(row.Slider.value);
            }
        }

        public void RefreshCaptions()
        {
            if (Bgm != null && Bgm.Caption != null)
                Bgm.Caption.text = LocalizationService.Get("settings.bgm_volume", "배경음");
            if (Sfx != null && Sfx.Caption != null)
                Sfx.Caption.text = LocalizationService.Get("settings.sfx_volume", "효과음");
        }

        public static string FormatPercent(float volume01)
        {
            return Mathf.RoundToInt(Mathf.Clamp01(volume01) * 100f) + "%";
        }

        private static void SetValue(Row row, float volume)
        {
            if (row == null || row.Slider == null) return;
            row.Slider.SetValueWithoutNotify(Mathf.Clamp01(volume));
            if (row.Label != null) row.Label.text = FormatPercent(row.Slider.value);
        }

        private static Row FindRow(RectTransform card, AudioChannel channel, string sliderName)
        {
            var slider = FindChild<Slider>(card, sliderName);
            if (slider == null) return null;
            var prefix = channel.ToString();
            return new Row
            {
                Channel = channel,
                Slider = slider,
                Caption = FindChild<TMP_Text>(card, prefix + "VolumeCaption"),
                Label = FindChild<TMP_Text>(card, prefix + "VolumeLabel")
            };
        }

        private static T FindChild<T>(Transform parent, string childName) where T : Component
        {
            var child = parent.Find(childName);
            return child != null ? child.GetComponent<T>() : null;
        }

        private static void ShiftExistingChildren(RectTransform card, float masterY)
        {
            for (var i = 0; i < card.childCount; i++)
            {
                var rect = card.GetChild(i) as RectTransform;
                // 가운데 앵커로 놓인 칸만 옮긴다. 파티클 루트처럼 늘어난 것은 그대로 둔다.
                if (rect == null || rect.anchorMin != new Vector2(0.5f, 0.5f) || rect.anchorMax != new Vector2(0.5f, 0.5f))
                    continue;
                var position = rect.anchoredPosition;
                position.y += position.y >= masterY - 0.5f ? RowSpacing : -RowSpacing;
                rect.anchoredPosition = position;
            }
        }

        private static Row CloneRow(
            RectTransform card,
            Slider masterSlider,
            TMP_Text masterCaption,
            TMP_Text masterLabel,
            AudioChannel channel,
            string prefix,
            int rowIndex)
        {
            float offset = -RowSpacing * rowIndex;
            var caption = Object.Instantiate(masterCaption, card, false);
            caption.name = prefix + "VolumeCaption";
            Offset(caption.rectTransform, offset);

            var label = Object.Instantiate(masterLabel, card, false);
            label.name = prefix + "VolumeLabel";
            Offset(label.rectTransform, offset);

            var slider = Object.Instantiate(masterSlider, card, false);
            slider.name = prefix + "VolumeSlider";
            Offset((RectTransform)slider.transform, offset);
            // 복제본에는 마스터 전용 확성기·이벤트가 따라온다. 지우고 이 채널용으로 다시 만든다.
            var copiedMute = slider.transform.Find(SettingsMuteButton.ObjectNameFor(AudioChannel.Master));
            if (copiedMute != null) Object.DestroyImmediate(copiedMute.gameObject);
            var copiedTrigger = slider.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (copiedTrigger != null) Object.DestroyImmediate(copiedTrigger);
            slider.onValueChanged.RemoveAllListeners();
            slider.SetValueWithoutNotify(1f);
            SettingsMuteButton.Ensure(slider, channel);

            return new Row { Channel = channel, Slider = slider, Caption = caption, Label = label };
        }

        // 복제 직후라 마스터 줄 위치(이미 +52 반영)에서 줄 수만큼 내린다.
        private static void Offset(RectTransform rect, float deltaY)
        {
            var position = rect.anchoredPosition;
            position.y += deltaY;
            rect.anchoredPosition = position;
        }

        /// <summary>
        /// 카드 그림의 오디오 칸 본문 가로 띠만 세로로 늘려 테두리가 세 줄을 감싸게 한다.
        /// 띠 위·아래는 원래 크기이고, 가로는 그대로다.
        /// </summary>
        private static void StretchCardArt(RectTransform card)
        {
            var art = card.GetComponent<Image>();
            if (art == null || art.sprite == null) return;
            var source = art.sprite;
            var rect = source.rect;
            if (rect.width <= 0f || rect.height <= 0f) return;

            float k = rect.height / ArtReferenceHeight;
            float top = ArtBandTopPx * k;
            float bottom = rect.height - ArtBandBottomPx * k;
            var border = new Vector4(0f, bottom, 0f, top);
            var sliced = Sprite.Create(source.texture, rect, new Vector2(0.5f, 0.5f), source.pixelsPerUnit, 0,
                SpriteMeshType.FullRect, border);
            sliced.name = source.name + "_AudioStretch";

            // 그림 한 픽셀이 카드 가로 폭 기준 원래 배율로 그려지도록 맞춘다.
            float unitsPerPixel = card.sizeDelta.x / rect.width;
            var canvas = card.GetComponentInParent<Canvas>();
            float referencePpu = canvas != null ? canvas.referencePixelsPerUnit : 100f;
            float spritePpu = sliced.pixelsPerUnit / referencePpu;

            // 카드 Image는 원본 그림·색을 그대로 두고(클릭 막기 겸용) 그리기만 끈다. 늘린 그림은 자식으로 따로 그린다.
            var stretched = card.Find(StretchedArtName) as RectTransform;
            if (stretched == null)
            {
                var go = new GameObject(StretchedArtName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.layer = card.gameObject.layer;
                stretched = (RectTransform)go.transform;
                stretched.SetParent(card, false);
            }

            stretched.anchorMin = Vector2.zero;
            stretched.anchorMax = Vector2.one;
            stretched.offsetMin = Vector2.zero;
            stretched.offsetMax = Vector2.zero;
            stretched.SetAsFirstSibling();
            var image = stretched.GetComponent<Image>();
            image.sprite = sliced;
            image.type = Image.Type.Sliced;
            image.fillCenter = true;
            image.preserveAspect = false;
            image.pixelsPerUnitMultiplier = 1f / (unitsPerPixel * spritePpu);
            image.color = art.color;
            image.raycastTarget = false;
            art.canvasRenderer.SetAlpha(0f);
        }
    }
}
