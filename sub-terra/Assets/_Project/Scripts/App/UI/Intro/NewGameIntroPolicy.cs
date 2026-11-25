using System;
using SubTerra.Shared;

namespace SubTerra.App.UI.Intro
{
    /// <summary>
    /// 새 게임 인트로 영상 재생 여부. B-209부터 영상은 광부 선택 화면의 글리치 전환 직후(새 게임 시작 때)에만 나오고,
    /// 첫 광산 진입·이어하기에서는 나오지 않는다.
    /// </summary>
    public static class NewGameIntroPolicy
    {
        /// <summary>첫 광산 진입에서는 영상을 다시 틀지 않는다(시작 브리핑만 그대로 뜬다).</summary>
        public const bool PlaysOnFirstMineEntry = false;

        /// <summary>
        /// ReduceMotion이면 화면 전체 영상은 생략하고 켜짐 연출만 짧게 쓴다. 영상 파일이 없으면 바로 게임으로 간다.
        /// </summary>
        public static bool ShouldPlayVideo(bool isNewGameStart, bool reduceMotion, bool videoAvailable)
        {
            return isNewGameStart && !reduceMotion && videoAvailable;
        }

        /// <summary>
        /// B-212: 고른 광부 성별로 재생할 영상 상대 경로를 정한다.
        /// 여자 파일이 없으면 남자 파일로 폴백하고(fellBack = true), 둘 다 없으면 null이다.
        /// fileExists는 상대 경로를 받아 실제 파일이 있는지 알려 준다(테스트에서 바꿔 끼운다).
        /// </summary>
        public static string ResolveVideoRelativePath(
            PlayerGender gender,
            string maleRelativePath,
            string femaleRelativePath,
            Func<string, bool> fileExists,
            out bool fellBack)
        {
            fellBack = false;
            if (fileExists == null) return null;

            if (gender == PlayerGender.Female)
            {
                if (Exists(fileExists, femaleRelativePath)) return femaleRelativePath;
                fellBack = true;
            }

            return Exists(fileExists, maleRelativePath) ? maleRelativePath : null;
        }

        private static bool Exists(Func<string, bool> fileExists, string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return false;
            try
            {
                return fileExists(relativePath);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
