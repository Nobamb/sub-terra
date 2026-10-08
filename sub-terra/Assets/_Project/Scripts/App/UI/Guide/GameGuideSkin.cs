using System;
using System.Collections.Generic;
using UnityEngine;

namespace SubTerra.App.UI.Guide
{
    [Serializable]
    public struct GuideSpriteEntry
    {
        public string key;
        public Sprite sprite;
    }

    /// <summary>
    /// 가이드 시연이 런타임에 쓰는 실제 게임 스프라이트 참조(플레이어 프레임·타일·시설·HUD·아이콘).
    /// 가이드 창이 코드로 만들어지므로 빌드에서도 찾을 수 있게 Resources에 둔다. 새 그림 파일은 없다.
    /// </summary>
    public sealed class GameGuideSkin : ScriptableObject
    {
        public const string ResourcePath = "UI/GameGuideSkin";

        public List<GuideSpriteEntry> sprites = new List<GuideSpriteEntry>();

        private Dictionary<string, Sprite> lookup;

        public Sprite Get(string key)
        {
            if (lookup == null)
            {
                lookup = new Dictionary<string, Sprite>(StringComparer.Ordinal);
                for (var i = 0; i < sprites.Count; i++)
                {
                    if (!string.IsNullOrEmpty(sprites[i].key))
                    {
                        lookup[sprites[i].key] = sprites[i].sprite;
                    }
                }
            }

            return !string.IsNullOrEmpty(key) && lookup.TryGetValue(key, out var sprite) ? sprite : null;
        }

        public void ResetLookup()
        {
            lookup = null;
        }
    }

    public interface IGuideSprites
    {
        Sprite Get(string key);
    }

    /// <summary>
    /// 키로 스프라이트를 찾는다. 'item:ID'·'bld:ID'는 게임 카탈로그의 실제 아이콘, 'hud.*'는 HUD 아이콘 시트의 한 칸,
    /// 나머지는 GameGuideSkin이다.
    /// </summary>
    public sealed class GuideSprites : IGuideSprites
    {
        // hud-icons.png(7개 아이콘 가로 배열)에서 각 아이콘이 차지하는 가로 구간.
        private static readonly Dictionary<string, Rect> HudCells = new Dictionary<string, Rect>(StringComparer.Ordinal)
        {
            { "hud.heart", new Rect(0.005f, 0.2f, 0.13f, 0.62f) },
            { "hud.bolt", new Rect(0.155f, 0.2f, 0.115f, 0.62f) },
            { "hud.coins", new Rect(0.425f, 0.2f, 0.14f, 0.62f) },
            { "hud.crate", new Rect(0.58f, 0.2f, 0.14f, 0.62f) }
        };

        private readonly GameGuideSkin skin;
        private readonly IGuideData data;

        public GuideSprites(GameGuideSkin skin, IGuideData data)
        {
            this.skin = skin;
            this.data = data;
        }

        public static GuideSprites Load(IGuideData data)
        {
            return new GuideSprites(Resources.Load<GameGuideSkin>(GameGuideSkin.ResourcePath), data);
        }

        public Sprite Get(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            if (key.StartsWith("item:", StringComparison.Ordinal))
            {
                var id = key.Substring(5);
                if (data != null && data.TryGetItem(id, out var item) && item.Icon != null)
                {
                    return item.Icon;
                }

                return skin != null ? skin.Get("fallback." + id) : null;
            }

            if (key.StartsWith("bld:", StringComparison.Ordinal))
            {
                var id = key.Substring(4);
                if (data != null && data.TryGetBuilding(id, out var building) && IsRealIcon(building.Icon))
                {
                    return building.Icon;
                }

                return Get(SceneKeyFor(id));
            }

            if (HudCells.TryGetValue(key, out var cell))
            {
                return GameGuideArt.Sub(skin != null ? skin.Get("hud.icons") : null, cell);
            }

            if (key.StartsWith("art.", StringComparison.Ordinal))
            {
                return Art(key);
            }

            return skin != null ? skin.Get(key) : null;
        }

        /// <summary>카탈로그의 자리표시 아이콘(2x2 DataPlaceholder 등)은 실제 아이콘이 아니다.</summary>
        private static bool IsRealIcon(Sprite icon)
        {
            return icon != null && icon.rect.width > 8f && icon.rect.height > 8f;
        }

        /// <summary>카탈로그 아이콘이 없을 때(테스트·데이터 누락) 대신 쓸 같은 시설의 월드 스프라이트 키.</summary>
        public static string SceneKeyFor(string buildingId)
        {
            switch (buildingId)
            {
                case Core.Data.DataIds.Buildings.ChargerBasic: return "fac.charger";
                case Core.Data.DataIds.Buildings.ClinicBasic: return "fac.clinic";
                case Core.Data.DataIds.Buildings.StorageBasic: return "fac.storage";
                case Core.Data.DataIds.Buildings.SettlementBasic: return "fac.settlement";
                case Core.Data.DataIds.Buildings.OutpostCoreBasic: return "fac.core";
                case Core.Data.DataIds.Buildings.LadderBasic: return "fac.ladder";
                case Core.Data.DataIds.Buildings.SupportBasic: return "fac.support";
                case Core.Data.DataIds.Buildings.LightBasic: return "fac.light";
                case Core.Data.DataIds.Buildings.EmergencyEscapePortal: return "art.portal";
                default: return "icon.facility";
            }
        }

        public static Sprite Art(string key)
        {
            switch (key)
            {
                case "art.arrow": return GameGuideArt.ArrowRight();
                case "art.arrowup": return GameGuideArt.ArrowUp();
                case "art.info": return GameGuideArt.Info();
                case "art.warning": return GameGuideArt.Warning();
                case "art.diamond": return GameGuideArt.Diamond();
                case "art.portal": return GameGuideArt.Portal();
                case "art.ring": return GameGuideArt.Ring();
                case "art.book": return GameGuideArt.BookGlyph();
                case "art.soft": return Sell.ResourceSellArt.Soft();
                case "art.softrect": return Sell.ResourceSellArt.SoftRect();
                case "art.fill": return Sell.ResourceSellArt.ChamferFill();
                case "art.line": return Sell.ResourceSellArt.ChamferOutline();
                case "art.hex": return Sell.ResourceSellArt.HexTile();
                default: return null;
            }
        }
    }
}
