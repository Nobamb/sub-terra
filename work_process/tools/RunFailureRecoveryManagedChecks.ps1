param([string]$CompiledDirectory = (Join-Path $PSScriptRoot 'failure-recovery-check-output'), [string]$UnityEditorPath = 'C:/Program Files/Unity/Hub/Editor/6000.5.4f1/Editor/Unity.exe')
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$project = Join-Path $repo 'sub-terra'
$out = [IO.Path]::GetFullPath($CompiledDirectory)
if (!$out.StartsWith($repo + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Use repository-local compilation output.' }
if (!(Test-Path "$out/SubTerra.App.Tests.EditMode.dll")) { throw 'Compile current App and EditMode tests with CompileUnityChanges.ps1 first.' }
$data = Join-Path ([IO.Path]::GetDirectoryName($UnityEditorPath)) 'Data'
$sdk = Get-ChildItem "$data/DotNetSdk/sdk" -Directory | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
$paths = @{}
[xml]$testProject = Get-Content "$project/SubTerra.App.Tests.EditMode.csproj" -Raw
foreach ($node in $testProject.SelectNodes('//Reference/HintPath')) {
    $path = $node.InnerText
    if (![IO.Path]::IsPathRooted($path)) { $path = Join-Path $project $path }
    if (Test-Path $path) { $paths[[IO.Path]::GetFileNameWithoutExtension($path)] = [IO.Path]::GetFullPath($path) }
}
foreach ($directory in @("$project/Library/ScriptAssemblies", $out)) {
    foreach ($file in Get-ChildItem $directory -Filter '*.dll') { $paths[$file.BaseName] = $file.FullName }
}
$source = @'
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
public static class FailureRecoveryChecks {
    public static int Main(string[] args) {
        var paths = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in File.ReadAllLines(args[1])) paths[Path.GetFileNameWithoutExtension(path)] = path;
        AppDomain.CurrentDomain.AssemblyResolve += (s,e) => paths.TryGetValue(new AssemblyName(e.Name).Name, out var p) ? Assembly.LoadFrom(p) : null;
        var tests = Assembly.LoadFrom(args[0]);
        var cases = new Dictionary<string,string[]> {
            ["SubTerra.App.Tests.Save.ElevatorTravelSessionTests"] = new[] {
                "CallAndDepart_TransitionsThroughExplicitStatesAndChargesOnce", "Call_ZeroDepartureCost_DoesNotSpendEnergy",
                "Call_BlockedExitOrInsufficientEnergy_DoesNotCharge", "Depart_WhenSceneLoadFails_RefundsEnergyAndBlocks",
                "DeferredDepart_FailureRefundsOnceAndAllowsRetry", "DeferredDepart_SuccessCommitsOnlyOnCompletion", "Depart_MissingLoaderRefundsAndAllowsRetry" },
            ["SubTerra.App.Tests.Save.RunLifecycleServiceTests"] = new[] { "InvalidCheckpoint_FallsBackAndFailedTransitionPreservesActiveRun" },
            ["SubTerra.App.Tests.Run.EmergencyRescueServiceTests"] = new[] { "PromptB81_ZeroAssets_RescueIsFree", "PromptB81_EnergyAvailable_DisablesRescueWithoutCharging" }
        };
        int passed = 0;
        foreach (var entry in cases) foreach (var method in entry.Value) {
            try { var type = tests.GetType(entry.Key, true); type.GetMethod(method).Invoke(Activator.CreateInstance(type), null); Console.WriteLine("PASS " + method); passed++; }
            catch (Exception ex) { Console.WriteLine("FAIL " + method + ": " + (ex.InnerException ?? ex)); return 1; }
        }
        Console.WriteLine(passed + " managed checks passed; Unity physics, scenes and popup lifecycle are not executed.");
        return passed == 10 ? 0 : 1;
    }
}
'@
[IO.File]::WriteAllText("$out/FailureRecoveryChecks.cs", $source)
[IO.File]::WriteAllLines("$out/managed-resolver.txt", [string[]]$paths.Values)
$rsp = @('/nologo','/target:exe','/nostdlib+',('/out:"' + "$out/FailureRecoveryChecks.dll" + '"'))
foreach ($file in Get-ChildItem "$data/NetStandard/ref/2.1.0" -Filter '*.dll') { $rsp += '/reference:"' + $file.FullName + '"' }
$rsp += '"' + "$out/FailureRecoveryChecks.cs" + '"'
[IO.File]::WriteAllLines("$out/FailureRecoveryChecks.rsp", $rsp)
& "$data/DotNetSdk/dotnet.exe" "$($sdk.FullName)/Roslyn/bincore/csc.dll" ("@$out/FailureRecoveryChecks.rsp")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
[IO.File]::WriteAllText("$out/FailureRecoveryChecks.runtimeconfig.json", '{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.0"}}}')
& "$data/DotNetSdk/dotnet.exe" "$out/FailureRecoveryChecks.dll" "$out/SubTerra.App.Tests.EditMode.dll" "$out/managed-resolver.txt"
exit $LASTEXITCODE
