using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Tutorial
{
    /// <summary>
    /// 퀘스트 ID에 연결된 게임 장면을 썸네일 영역 전체에 보여 준다.
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
            if (secondaryImage != null) secondaryImage.gameObject.SetActive(false);
            if (tertiaryImage != null) tertiaryImage.gameObject.SetActive(false);
            Place(primaryImage, primary);
            if (placeholderText != null)
            {
                placeholderText.gameObject.SetActive(primary == null);
            }
        }

        private static void Place(Image image, Sprite sprite)
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
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(6f, 6f);
            rect.offsetMax = new Vector2(-6f, -6f);
        }
    }

    [Serializable]
    public sealed class QuestThumbnailEntry
    {
        public string objectiveId;
        public Sprite primary;
    }
}
