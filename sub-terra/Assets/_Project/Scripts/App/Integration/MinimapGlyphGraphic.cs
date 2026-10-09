using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Integration
{
    /// <summary>범례 아이콘·플레이어 실루엣·위치 링 같은 단일 기호. 표시 전용이다.</summary>
    public sealed class MinimapGlyphGraphic : MaskableGraphic
    {
        private MinimapGlyph glyph;
        private Color tint = Color.white;
        private Color halo = Color.clear;

        public MinimapGlyph Glyph => glyph;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        /// <summary>halo는 기호 뒤에 한 번 더 그리는 얇은 청록 테두리(가독성용)다.</summary>
        public void Set(MinimapGlyph nextGlyph, Color nextTint, Color nextHalo = default)
        {
            raycastTarget = false;
            if (glyph == nextGlyph && tint == nextTint && halo == nextHalo) return;
            glyph = nextGlyph;
            tint = nextTint;
            halo = nextHalo;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = rectTransform.rect;
            if (halo.a > 0f && glyph == MinimapGlyph.Person)
            {
                float pad = Mathf.Max(1f, rect.width * 0.1f);
                MinimapMeshKit.Person(mesh, Rect.MinMaxRect(rect.xMin - pad, rect.yMin, rect.xMax + pad, rect.yMax + pad), halo);
            }

            if (glyph == MinimapGlyph.Person)
            {
                MinimapMeshKit.Person(mesh, rect, tint);
                return;
            }

            MinimapMeshKit.Glyph(mesh, glyph, rect, tint);
        }
    }
}
