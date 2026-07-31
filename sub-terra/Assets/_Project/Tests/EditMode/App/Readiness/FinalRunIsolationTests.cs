using System.IO;
using NUnit.Framework;
using SubTerra.App.Readiness;
using UnityEngine;

namespace SubTerra.App.Tests.Readiness
{
    /// <summary>A-S05 테스트 격리 및 최종 완주 뼈대 결과 형식.</summary>
    public sealed class FinalRunIsolationTests
    {
        [Test]
        public void A_S05_IsolatedSaveRoot_IsTempAndNotUserPersistentSlot()
        {
            var root = FinalRunTestPaths.CreateIsolatedSaveRoot("a-s05");
            try
            {
                Assert.That(Directory.Exists(root), Is.True);
                Assert.That(FinalRunTestPaths.IsIsolatedTempRoot(root), Is.True);

                var persistent = Application.persistentDataPath;
                Assert.That(
                    FinalRunTestPaths.IsUserPersistentSavePath(root, persistent),
                    Is.False,
                    "Isolated temp root must not be treated as user save path");

                var userSlot = Path.Combine(persistent, "save_slot_1.json");
                Assert.That(
                    FinalRunTestPaths.IsUserPersistentSavePath(userSlot, persistent),
                    Is.True);

                // 격리 루트 아래 파일이라도 사용자 persistent 슬롯 패턴이 아니면 false.
                var nested = Path.Combine(root, "save_slot_1.json");
                Assert.That(
                    FinalRunTestPaths.IsUserPersistentSavePath(nested, persistent),
                    Is.False);
            }
            finally
            {
                FinalRunTestPaths.TryDeleteRoot(root);
            }
        }

        [Test]
        public void A_S05_FinalRunSkeleton_UsesIsolatedRootAndSharedEntryPath()
        {
            var root = FinalRunTestPaths.CreateIsolatedSaveRoot("skeleton");
            try
            {
                var record = FinalRunResultRecord.CreateSkeleton(root);
                Assert.That(record.EntryPath, Is.EqualTo(FinalRunResultRecord.EntryPathContract));
                Assert.That(record.UsedIsolatedSaveRoot, Is.True);
                Assert.That(record.IsolatedSaveRoot, Is.EqualTo(Path.GetFullPath(root)));
                Assert.That(record.Steps.Count, Is.EqualTo(12));
                Assert.That(record.OverallStatus, Is.EqualTo("skeleton"));

                var text = record.FormatText();
                Assert.That(text, Does.Contain("01-new-game"));
                Assert.That(text, Does.Contain("12-continue"));
                Assert.That(text, Does.Contain("UsedIsolatedSaveRoot: True"));
            }
            finally
            {
                FinalRunTestPaths.TryDeleteRoot(root);
            }
        }

        [Test]
        public void A_S05_TryDeleteRoot_IgnoresNonIsolatedPaths()
        {
            var persistent = Application.persistentDataPath;
            // 프로덕션 경로를 삭제 시도해도 무시되어야 한다.
            FinalRunTestPaths.TryDeleteRoot(persistent);
            Assert.That(Directory.Exists(persistent), Is.True);
        }
    }
}
