using SubTerra.App.Audio;
using SubTerra.Shared.Audio;
using UnityEngine;

namespace SubTerra.App.UI.EmergencyEscape
{
    /// <summary>
    /// 전력 소진 비상 탈출·광산 초기화 글리치 암전에 붙는 효과음.
    /// 게임 시작 브리핑 팝업을 닫을 때 나는 글리치 소리(BriefingClose)를 그대로 재사용한다. 새 음원·SfxId 없음.
    /// 글리치가 없는 모션 감소에서는 내지 않는다.
    /// </summary>
    public static class EscapeGlitchSfx
    {
        public const SfxId Id = SfxId.BriefingClose;

        /// <summary>글리치 암전이 시작될 때 한 번 부른다. 소리를 냈으면 true.</summary>
        public static bool Play(bool reduceMotion)
        {
            if (reduceMotion || !Application.isPlaying)
            {
                return false;
            }

            SfxPlayer.Play(Id);
            return true;
        }
    }
}
