using SubTerra.App.Save;
using UnityEngine;

namespace SubTerra.App.UI.MainMenu
{
    public sealed class SaveSlotCardView : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.RawImage thumbnail;
        [SerializeField] private GameObject placeholder;
        [SerializeField] private CanvasGroup selection;
        [SerializeField] private UnityEngine.UI.RawImage innerGlow;
        private Texture2D loadedTexture;
        private bool loaded;
        private float targetAlpha;

        public void ShowThumbnail(int slotId, bool canContinue)
        {
            if (!loaded && canContinue)
            {
                var runtime = SaveRuntimeController.Instance;
                if (runtime != null && runtime.Thumbnails != null)
                {
                    loadedTexture = runtime.Thumbnails.Load(slotId);
                    loaded = true;
                }
            }
            if (thumbnail != null)
            {
                thumbnail.texture = canContinue ? loadedTexture : null;
                thumbnail.color = canContinue && loadedTexture != null ? Color.white : new Color(0.04f, 0.09f, 0.12f);
            }
            if (placeholder != null) placeholder.SetActive(!canContinue || loadedTexture == null);
        }

        public void SetSelected(bool selected)
        {
            targetAlpha = selected ? 1 : 0;
        }

        private void Update()
        {
            if (selection != null)
            {
                selection.alpha = Mathf.MoveTowards(selection.alpha, targetAlpha, Time.unscaledDeltaTime / 0.2f);
            }
            if (innerGlow != null && targetAlpha > 0.5f)
            {
                var pulse = 0.26f + Mathf.Sin(Time.unscaledTime * 2.2f) * 0.06f;
                var c = innerGlow.color;
                c.a = pulse;
                innerGlow.color = c;
            }
        }

        private void OnDestroy()
        {
            if (loadedTexture != null) Destroy(loadedTexture);
        }
    }
}
