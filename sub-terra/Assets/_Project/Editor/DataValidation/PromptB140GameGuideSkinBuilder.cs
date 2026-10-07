using System.Collections.Generic;
using SubTerra.App.UI.Guide;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// 프롬프트 B-140: 가이드 시연이 쓰는 기존 게임 스프라이트 참조 에셋(GameGuideSkin)만 만든다.
    /// 새 그림은 없고 플레이어 프레임·타일·시설·HUD·아이콘 원본을 가리킨다. 프리팹·씬은 열거나 저장하지 않는다.
    /// </summary>
    public static class PromptB140GameGuideSkinBuilder
    {
        public const string SkinAssetPath = "Assets/_Project/Resources/UI/GameGuideSkin.asset";
        private const string Art = "Assets/_Project/Art/";

        [MenuItem("SubTerra/UI/Build Prompt-B 140 Game Guide Skin")]
        public static void BuildFromMenu()
        {
            Build();
        }

        public static IReadOnlyList<KeyValuePair<string, string>> Map()
        {
            var map = new List<KeyValuePair<string, string>>();
            void Add(string key, string path) => map.Add(new KeyValuePair<string, string>(key, Art + path));

            Add("player.idle", "Characters/Player/Frames/Idle/player_idle_01.png");
            for (var i = 1; i <= 10; i++) Add("player.walk." + i, "Characters/Player/Frames/Walk/walk_" + i.ToString("00") + ".png");
            for (var i = 1; i <= 8; i++) Add("player.mine." + i, "Characters/Player/Frames/Mining/mining_" + i.ToString("00") + ".png");
            for (var i = 1; i <= 5; i++) Add("player.ladder." + i, "Characters/Player/Frames/LadderBack/ladder_back_" + i.ToString("00") + ".png");
            Add("player.jump.4", "Characters/Player/Frames/Jump/jump_04.png");
            Add("player.damage.1", "Characters/Player/Frames/Damage/damage_01.png");
            Add("player.knockout.1", "Characters/Player/Frames/Knockout/knockout_01.png");

            Add("tile.ground", "Tiles/Ground/ground_normal_01.png");
            Add("tile.ground.2", "Tiles/Ground/ground_normal_02.png");
            Add("tile.hard", "Tiles/Ground/ground_hard_01.png");
            Add("tile.gas", "Tiles/Ground/ground_gas_01.png");
            Add("tile.gold", "Tiles/Ground/ground_normal_gold_01.png");
            Add("tile.ore.copper", "Tiles/Ore/ore_copper_01.png");
            Add("tile.ore.iron", "Tiles/Ore/ore_iron_01.png");
            Add("tile.ore.lithium", "Tiles/Ore/ore_lithium_01.png");
            Add("tile.crack.yellow", "Tiles/Crack_Overlay/crack_yellow_overlay.png");
            Add("tile.crack.orange", "Tiles/Crack_Overlay/crack_orange_overlay.png");
            Add("tile.crack.red", "Tiles/Crack_Overlay/crack_red_overlay.png");

            Add("fac.charger", "Facilities/MVP/charger_basic_cartoon_v2.png");
            Add("fac.clinic", "Facilities/MVP/clinic_basic_cartoon_v3.png");
            Add("fac.elevator", "Facilities/MVP/elevator_station_mine.png");
            Add("fac.elevator.door", "Facilities/MVP/elevator_door_panel_mine.png");
            Add("fac.ladder", "Facilities/MVP/ladder_segment_mine.png");
            Add("fac.light", "Facilities/MVP/light_basic_cartoon_v3.png");
            Add("fac.core", "Facilities/MVP/outpost_core_cartoon_v3.png");
            Add("fac.settlement", "Facilities/MVP/settlement_console_cartoon_v3.png");
            Add("fac.storage", "Facilities/MVP/storage_basic_cartoon_v2.png");
            Add("fac.support", "Facilities/MVP/support_pillar_mine.png");

            Add("drone.idle", "Characters/Drone/digger_bot_idle.png");
            Add("fx.coin", "FX/gold_coin_01.png");

            Add("hud.hp.bar", "UI/Gameplay/HUD/hp-bar.png");
            Add("hud.hp.empty", "UI/Gameplay/HUD/hp-empty.png");
            Add("hud.energy.bar", "UI/Gameplay/HUD/energy-bar.png");
            Add("hud.energy.empty", "UI/Gameplay/HUD/energy-empty.png");
            Add("hud.bar.frame", "UI/Gameplay/HUD/bar-frame.png");
            Add("hud.icons", "UI/Gameplay/HUD/hud-icons.png");
            Add("hud.structural.safe", "UI/Gameplay/HUD/structural-safe.png");
            Add("hud.structural.caution", "UI/Gameplay/HUD/structural-caution.png");
            Add("hud.structural.critical", "UI/Gameplay/HUD/structural-critical.png");
            Add("hud.structural.imminent", "UI/Gameplay/HUD/structural-imminent.png");

            Add("icon.mine", "UI/SurfaceBase/Icons/icon-mine.png");
            Add("icon.cargo", "UI/SurfaceBase/Icons/icon-cargo.png");
            Add("icon.sell", "UI/SurfaceBase/Icons/icon-sell.png");
            Add("icon.upgrade", "UI/SurfaceBase/Icons/icon-upgrade.png");
            Add("icon.gold", "UI/SurfaceBase/Icons/icon-gold.png");
            Add("icon.reset", "UI/SurfaceBase/Icons/icon-reset.png");
            Add("icon.guide", "UI/Gameplay/SideMenu/icon-guide.png");
            Add("icon.inventory", "UI/Gameplay/SideMenu/icon-inventory.png");
            Add("icon.facility", "UI/Gameplay/SideMenu/icon-facility.png");
            Add("icon.explore", "UI/SurfaceBase/Exploration-button.png");

            Add("fallback.mineral.copper", "Icons/icon_copper.png");
            Add("fallback.mineral.iron", "Icons/icon_iron.png");
            Add("fallback.mineral.lithium", "Icons/icon_lithium.png");
            Add("fallback.item.rare.engine_fuel", "Icons/icon_engine_fuel.png");
            return map;
        }

        public static GameGuideSkin Build()
        {
            var skin = AssetDatabase.LoadAssetAtPath<GameGuideSkin>(SkinAssetPath);
            if (skin == null)
            {
                skin = ScriptableObject.CreateInstance<GameGuideSkin>();
                AssetDatabase.CreateAsset(skin, SkinAssetPath);
            }

            skin.sprites.Clear();
            foreach (var pair in Map())
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pair.Value);
                if (sprite == null)
                {
                    Debug.LogWarning("[SubTerra] Game guide sprite missing: " + pair.Value);
                    continue;
                }

                skin.sprites.Add(new GuideSpriteEntry { key = pair.Key, sprite = sprite });
            }

            skin.ResetLookup();
            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssetIfDirty(skin);
            return skin;
        }
    }
}
