using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using SubTerra.App.Core;
using SubTerra.App.Save;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>Repeatable Windows x64 build profiles for the final MVP2 release gate.</summary>
    public static class PhasePWindowsBuildGate
    {
        public enum Profile
        {
            Development,
            Qa,
            Release
        }

        public readonly struct ProfileDefinition
        {
            public ProfileDefinition(Profile profile, string define, BuildOptions options)
            {
                Profile = profile;
                Define = define;
                Options = options;
            }

            public Profile Profile { get; }
            public string Define { get; }
            public BuildOptions Options { get; }
        }

        private static readonly string[] RequiredScenes =
        {
            "Assets/_Project/Scenes/Bootstrap/Bootstrap.unity",
            "Assets/_Project/Scenes/App/MainMenu.unity",
            "Assets/_Project/Scenes/App/SurfaceBase.unity",
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity"
        };

        public static IReadOnlyList<string> WindowsScenes => RequiredScenes;

        public static ProfileDefinition GetProfile(Profile profile)
        {
            switch (profile)
            {
                case Profile.Development:
                    return new ProfileDefinition(
                        profile,
                        "SUBTERRA_BUILD_DEVELOPMENT",
                        BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler | BuildOptions.StrictMode);
                case Profile.Qa:
                    return new ProfileDefinition(
                        profile,
                        "SUBTERRA_BUILD_QA",
                        BuildOptions.Development | BuildOptions.ConnectWithProfiler | BuildOptions.StrictMode);
                case Profile.Release:
                    return new ProfileDefinition(
                        profile,
                        "SUBTERRA_BUILD_RELEASE",
                        BuildOptions.StrictMode);
                default:
                    throw new ArgumentOutOfRangeException(nameof(profile), profile, null);
            }
        }

        [MenuItem("SubTerra/Phase P/Build Windows/Development")]
        public static void BuildDevelopment() => Build(Profile.Development);

        [MenuItem("SubTerra/Phase P/Build Windows/QA")]
        public static void BuildQa() => Build(Profile.Qa);

        [MenuItem("SubTerra/Phase P/Build Windows/Release")]
        public static void BuildRelease() => Build(Profile.Release);

        public static void Build(Profile profile)
        {
            var errors = Validate(profile);
            if (errors.Count > 0)
            {
                throw new BuildFailedException("Phase P build gate blocked: " + string.Join(" | ", errors));
            }

            var definition = GetProfile(profile);
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var repositoryRoot = Directory.GetParent(projectRoot).FullName;
            var buildId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var packageRoot = Path.Combine(
                projectRoot,
                "Builds",
                "Windows-x64",
                definition.Profile.ToString(),
                buildId,
                "sub-terra");
            Directory.CreateDirectory(packageRoot);

            var exePath = Path.Combine(packageRoot, "sub-terra.exe");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = RequiredScenes,
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                options = definition.Options,
                extraScriptingDefines = new[] { definition.Define }
            });
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException("Windows build failed: " + report.summary.result);
            }

            CopyReleaseDocument(repositoryRoot, "docs/MVP2_WINDOWS_QA.md", packageRoot, "README.txt");
            CopyReleaseDocument(repositoryRoot, "docs/CHANGELOG.md", packageRoot, "CHANGELOG.txt");
            WriteManifest(packageRoot, definition, buildId, report);

            var archivePath = Path.Combine(
                Directory.GetParent(packageRoot).FullName,
                "sub-terra-windows-x64-" + definition.Profile + "-" + buildId + ".zip");
            ZipFile.CreateFromDirectory(
                packageRoot,
                archivePath,
                System.IO.Compression.CompressionLevel.Optimal,
                false);
            File.WriteAllText(archivePath + ".sha256", ComputeSha256(archivePath));
            Debug.Log("[SubTerra] Phase P Windows package created: " + archivePath);
        }

        public static List<string> Validate(Profile profile)
        {
            var errors = new List<string>();
            var definition = GetProfile(profile);
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            if (RequiredScenes.Length != 4)
            {
                errors.Add("Release scene list must contain exactly Bootstrap, MainMenu, SurfaceBase, Mine_Demo_Integration.");
            }

            for (var index = 0; index < RequiredScenes.Length; index++)
            {
                if (!File.Exists(Path.Combine(projectRoot, RequiredScenes[index])))
                {
                    errors.Add("Missing build scene: " + RequiredScenes[index]);
                }
            }

            var hasDevelopment = (definition.Options & BuildOptions.Development) != 0;
            if (profile == Profile.Release && hasDevelopment)
            {
                errors.Add("Release profile must not enable Development Build.");
            }

            if (profile != Profile.Release && !hasDevelopment)
            {
                errors.Add("Development and QA profiles must enable Development Build.");
            }

            if (string.IsNullOrEmpty(definition.Define))
            {
                errors.Add("Build profile define is missing.");
            }

            return errors;
        }

        private static void CopyReleaseDocument(string projectRoot, string relativeSource, string packageRoot, string outputName)
        {
            var source = Path.Combine(projectRoot, relativeSource);
            if (!File.Exists(source))
            {
                throw new BuildFailedException("Release document missing: " + relativeSource);
            }

            File.Copy(source, Path.Combine(packageRoot, outputName), true);
        }

        private static void WriteManifest(
            string packageRoot,
            ProfileDefinition definition,
            string buildId,
            BuildReport report)
        {
            var manifest = new BuildManifest
            {
                productName = PlayerSettings.productName,
                gameVersion = PlayerSettings.bundleVersion,
                buildChannel = definition.Profile.ToString(),
                saveVersion = SaveVersions.Current,
                buildId = buildId,
                target = "Windows x64",
                totalSizeBytes = report.summary.totalSize
            };
            File.WriteAllText(
                Path.Combine(packageRoot, "BUILD_MANIFEST.json"),
                JsonUtility.ToJson(manifest, true));
        }

        private static string ComputeSha256(string filePath)
        {
            using (var hash = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        [Serializable]
        private sealed class BuildManifest
        {
            public string productName;
            public string gameVersion;
            public string buildChannel;
            public int saveVersion;
            public string buildId;
            public string target;
            public ulong totalSizeBytes;
        }
    }
}
