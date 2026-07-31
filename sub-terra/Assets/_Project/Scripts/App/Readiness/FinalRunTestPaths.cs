using System;
using System.IO;

namespace SubTerra.App.Readiness
{
    /// <summary>
    /// B~P가 공유할 최종 완주 테스트 격리 경로.
    /// 사용자 persistentDataPath 슬롯을 열지 않고 임시 루트만 사용한다.
    /// </summary>
    public static class FinalRunTestPaths
    {
        public const string IsolationFolderPrefix = "subterra-mvp2-finalrun-";
        public const string UserSaveFilePrefix = "save_slot_";

        /// <summary>테스트 전용 임시 세이브 루트를 생성한다. Application.persistentDataPath 하위가 아니다.</summary>
        public static string CreateIsolatedSaveRoot(string purpose = "phase-a")
        {
            var safePurpose = string.IsNullOrWhiteSpace(purpose) ? "run" : purpose.Trim();
            var root = Path.Combine(
                Path.GetTempPath(),
                IsolationFolderPrefix + safePurpose + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return Path.GetFullPath(root);
        }

        /// <summary>경로가 사용자 프로덕션 세이브 슬롯을 가리키는지 판정한다.</summary>
        public static bool IsUserPersistentSavePath(string candidatePath, string persistentDataPath)
        {
            if (string.IsNullOrWhiteSpace(candidatePath) || string.IsNullOrWhiteSpace(persistentDataPath))
            {
                return false;
            }

            string fullCandidate;
            string fullPersistent;
            try
            {
                fullCandidate = Path.GetFullPath(candidatePath);
                fullPersistent = Path.GetFullPath(persistentDataPath);
            }
            catch (Exception)
            {
                return false;
            }

            if (!fullCandidate.StartsWith(fullPersistent, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var fileName = Path.GetFileName(fullCandidate);
            return !string.IsNullOrEmpty(fileName)
                && fileName.StartsWith(UserSaveFilePrefix, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>격리 루트가 임시 경로 아래에 있는지 확인한다.</summary>
        public static bool IsIsolatedTempRoot(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                return false;
            }

            try
            {
                var full = Path.GetFullPath(rootPath);
                var tempRoot = Path.GetFullPath(Path.GetTempPath());
                return full.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase)
                    && full.IndexOf(IsolationFolderPrefix, StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void TryDeleteRoot(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
            {
                return;
            }

            try
            {
                if (!IsIsolatedTempRoot(rootPath))
                {
                    return;
                }

                Directory.Delete(rootPath, true);
            }
            catch (Exception)
            {
                // 테스트 정리 실패는 본 검증을 막지 않는다.
            }
        }
    }
}
