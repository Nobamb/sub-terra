using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Tutorial
{
    /// <summary>
    /// 퀘스트 ID에 연결된 기존 스프라이트를 썸네일로 보여 준다.
    /// 스프라이트가 없으면 자리 표시 문구만 남긴다.
    /// </summary>
    public sealed class QuestThumbnailView : MonoBehaviour
    {
        [SerializeField] private Image primaryImage;
        [SerializeField] private Image secondaryImage;
        [SerializeField] private Image tertiaryImage;
        [SerializeField] private TMP_Text placeholderText;
        [SerializeField] private QuestThumbnailEntry[] entries;

        public int EntryCount => entries == null ? 0 : entries.Length;

        public void Show(string objectiveId)
        {
            QuestThumbnailEntry match = null;
            if (entries != null && !string.IsNullOrEmpty(objectiveId))
            {
                for (var i = 0; i < entries.Length; i++)
                {
                    if (entries[i] != null && entries[i].objectiveId == objectiveId)
                    {
                        match = entries[i];
                        break;
                    }
                }
            }

            var primary = match != null ? match.primary : null;
            var secondary = match != null ? match.secondary : null;
            var tertiary = match != null ? match.tertiary : null;
            var count = 0;
            if (primary != null)
            {
                count++;
            }

            if (secondary != null)
            {
                count++;
            }

            if (tertiary != null)
            {
                count++;
            }

            var height = 150f;
            var rect = transform as RectTransform;
            if (rect != null && rect.rect.height > 40f)
            {
                height = Mathf.Clamp(rect.rect.height * 0.72f, 96f, 168f);
            }

            var gap = count >= 3 ? height * 1.35f : height * 0.95f;
            var floor = 18f;
            Place(secondaryImage, secondary, count >= 2 ? -gap : 0f, height, floor);
            Place(primaryImage, primary, 0f, height, floor);
            Place(tertiaryImage, tertiary, gap, height, floor);
            if (placeholderText != null)
            {
                placeholderText.gameObject.SetActive(primary == null);
            }
        }

        private static void Place(Image image, Sprite sprite, float x, float size, float floor)
        {
            if (image == null)
            {
                return;
            }

            var visible = sprite != null;
            image.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = new Vector2(x, floor);
        }
    }

    [Serializable]
    public sealed class QuestThumbnailEntry
    {
        public string objectiveId;
        public Sprite primary;
        public Sprite secondary;
        public Sprite tertiary;
    }
}
