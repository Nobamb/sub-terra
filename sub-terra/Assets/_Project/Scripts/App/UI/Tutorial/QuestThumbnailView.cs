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

            var size = count >= 3 ? 110f : count == 2 ? 130f : 150f;
            var gap = count >= 3 ? 160f : 110f;
            Place(secondaryImage, secondary, count >= 2 ? -gap : 0f, size);
            Place(primaryImage, primary, 0f, size);
            Place(tertiaryImage, tertiary, gap, size);
            if (placeholderText != null)
            {
                placeholderText.gameObject.SetActive(primary == null);
            }
        }

        private static void Place(Image image, Sprite sprite, float x, float size)
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
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = new Vector2(x, 0f);
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
