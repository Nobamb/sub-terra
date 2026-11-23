using System;
using System.Collections.Generic;
using SubTerra.Shared;

namespace SubTerra.App.UI.Guide
{
    /// <summary>
    /// B-208: 가이드·조작 안내 시연의 플레이어 그림을 성별에 맞춘다.
    /// 장면은 남자 키(player.*)로 만들고, 여자일 때는 player.female.* 키와 여자 실제 장수로 바꾼 복사본을 쓴다.
    /// 여자 장면은 여자 키만 가리키므로 남자 그림이 섞이지 않는다.
    /// </summary>
    public static class GuidePlayerArt
    {
        public const string PlayerKeyPrefix = "player.";
        public const string FemaleKeyPrefix = "player.female.";
        public const int MaleWalkFrameCount = SubTerra.Shared.PlayerWalkCycle.FrameCount;
        public const int FemaleWalkFrameCount = 39;
        public const int MaleMineFrameCount = 24;
        public const int FemaleMineFrameCount = 23;

        private static readonly Dictionary<DemoClip, DemoClip> FemaleClips = new Dictionary<DemoClip, DemoClip>();

        public static int WalkFrameCount(PlayerGender gender) =>
            gender == PlayerGender.Female ? FemaleWalkFrameCount : MaleWalkFrameCount;

        public static int MineFrameCount(PlayerGender gender) =>
            gender == PlayerGender.Female ? FemaleMineFrameCount : MaleMineFrameCount;

        /// <summary>남자 플레이어 키를 성별 키로 바꾼다. 플레이어 키가 아니면 그대로 둔다.</summary>
        public static string KeyFor(string key, PlayerGender gender)
        {
            if (gender != PlayerGender.Female || string.IsNullOrEmpty(key)
                || !key.StartsWith(PlayerKeyPrefix, StringComparison.Ordinal)
                || key.StartsWith(FemaleKeyPrefix, StringComparison.Ordinal))
            {
                return key;
            }

            return FemaleKeyPrefix + key.Substring(PlayerKeyPrefix.Length);
        }

        /// <summary>남자 장면이면 그대로, 여자면 키·장수를 바꾼 복사본(장면마다 한 번만 만든다).</summary>
        public static DemoClip ForGender(DemoClip clip, PlayerGender gender)
        {
            if (clip == null || gender != PlayerGender.Female)
            {
                return clip;
            }

            if (!FemaleClips.TryGetValue(clip, out var female))
            {
                female = CreateFemale(clip);
                FemaleClips[clip] = female;
            }

            return female;
        }

        private static DemoClip CreateFemale(DemoClip source)
        {
            var clip = new DemoClip { Id = source.Id, Duration = source.Duration, ThumbTime = source.ThumbTime };
            clip.Keys.AddRange(source.Keys);
            for (var i = 0; i < source.Layers.Count; i++)
            {
                clip.Layers.Add(ToFemale(source.Layers[i]));
            }

            return clip;
        }

        private static DemoLayer ToFemale(DemoLayer source)
        {
            if (!UsesPlayerArt(source))
            {
                return source;
            }

            var layer = source.Clone();
            layer.SpriteKey = KeyFor(source.SpriteKey, PlayerGender.Female);
            layer.IdleKey = KeyFor(source.IdleKey, PlayerGender.Female);
            if (source.FrameKeys == null)
            {
                return layer;
            }

            var femalePrefix = KeyFor(source.AnimPrefix, PlayerGender.Female);
            if (source.MiningStart >= 0f)
            {
                layer.AnimateMining(source.MiningStart, source.MiningCompletedAt, femalePrefix,
                    FemaleMineFrameCount, layer.IdleKey);
            }
            else if (source.AnimPrefix == "player.walk")
            {
                layer.Animate(femalePrefix, FemaleWalkFrameCount, source.Fps, layer.IdleKey, source.FrameActive);
            }
            else
            {
                // 사다리처럼 남녀 장수가 같은 장면은 순서를 그대로 두고 키만 바꾼다.
                for (var i = 0; i < layer.FrameKeys.Length; i++)
                {
                    layer.FrameKeys[i] = KeyFor(source.FrameKeys[i], PlayerGender.Female);
                }
            }

            return layer;
        }

        private static bool UsesPlayerArt(DemoLayer layer)
        {
            if (IsPlayerKey(layer.SpriteKey) || IsPlayerKey(layer.IdleKey))
            {
                return true;
            }

            if (layer.FrameKeys != null)
            {
                for (var i = 0; i < layer.FrameKeys.Length; i++)
                {
                    if (IsPlayerKey(layer.FrameKeys[i]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsPlayerKey(string key) =>
            !string.IsNullOrEmpty(key) && key.StartsWith(PlayerKeyPrefix, StringComparison.Ordinal);
    }
}
