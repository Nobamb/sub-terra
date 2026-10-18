param([string]$RepositoryRoot = (Join-Path $PSScriptRoot '../..'), [string]$UnityEditorPath)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath($RepositoryRoot)
$project = Join-Path $repo 'sub-terra'
$version = [regex]::Match((Get-Content "$project/ProjectSettings/ProjectVersion.txt" -Raw), 'm_EditorVersion:\s*(\S+)').Groups[1].Value
if (!$UnityEditorPath) { $UnityEditorPath = "C:/Program Files/Unity/Hub/Editor/$version/Editor/Unity.exe" }
$data = Join-Path ([IO.Path]::GetDirectoryName($UnityEditorPath)) 'Data'
$sdk = Get-ChildItem "$data/DotNetSdk/sdk" -Directory | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
$out = Join-Path $repo 'work_process/tools/demo-managed-check-output'
New-Item -ItemType Directory -Path $out -Force | Out-Null
[xml]$projectFile = Get-Content "$project/SubTerra.App.csproj" -Raw
$references = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($node in $projectFile.SelectNodes('//Reference/HintPath')) {
    $path = $node.InnerText
    if (![IO.Path]::IsPathRooted($path)) { $path = Join-Path $project $path }
    if (Test-Path -LiteralPath $path) { $null = $references.Add([IO.Path]::GetFullPath($path)) }
}
foreach ($file in Get-ChildItem "$project/Library/ScriptAssemblies" -Filter '*.dll') {
    if ($file.BaseName -ne 'SubTerra.App.Tests.EditMode') { $null = $references.Add($file.FullName) }
}
$runner = @'
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
public static class DemoManagedChecks {
    public static int Main(string[] args) {
        var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in File.ReadAllLines(args[0])) paths[Path.GetFileNameWithoutExtension(path)] = path;
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) => paths.TryGetValue(new AssemblyName(e.Name).Name, out var path) ? Assembly.LoadFrom(path) : null;
        int passed = 0, failed = 0;
        foreach (string name in new[] { "ElevatorTravelSessionTests", "RunLifecycleServiceTests" }) {
            var type = Assembly.GetExecutingAssembly().GetType("SubTerra.App.Tests.Save." + name, true);
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)) {
                if (method.GetParameters().Length != 0 || method.ReturnType != typeof(void)) continue;
                try { method.Invoke(Activator.CreateInstance(type), null); Console.WriteLine("PASS " + name + "." + method.Name); passed++; }
                catch (Exception ex) { Console.WriteLine("FAIL " + method.Name + ": " + (ex.InnerException ?? ex)); failed++; }
            }
        }
        Console.WriteLine("Managed checks: " + passed + " passed, " + failed + " failed. Cached App DLL; no Unity runtime or file-save validation.");
        return failed == 0 && passed == 7 ? 0 : 1;
    }
}
'@
[IO.File]::WriteAllText("$out/Runner.cs", $runner, [Text.UTF8Encoding]::new($false))
$rsp = @('/nologo', '/target:exe', '/nostdlib+', '/warn:0', ('/out:"' + "$out/Runner.dll" + '"'))
foreach ($file in Get-ChildItem "$data/NetStandard/ref/2.1.0" -Filter '*.dll') { $rsp += '/reference:"' + $file.FullName + '"' }
foreach ($path in $references) { $rsp += '/reference:"' + $path + '"' }
foreach ($name in @('ElevatorTravelSessionTests', 'RunLifecycleServiceTests')) { $rsp += '"' + "$project/Assets/_Project/Tests/EditMode/App/Save/$name.cs" + '"' }
$rsp += '"' + "$out/Runner.cs" + '"'
[IO.File]::WriteAllLines("$out/Runner.rsp", $rsp, [Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllLines("$out/References.txt", [string[]]$references, [Text.UTF8Encoding]::new($false))
& "$data/DotNetSdk/dotnet.exe" "$($sdk.FullName)/Roslyn/bincore/csc.dll" ("@$out/Runner.rsp")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
[IO.File]::WriteAllText("$out/Runner.runtimeconfig.json", '{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.0"}}}')
& "$data/DotNetSdk/dotnet.exe" "$out/Runner.dll" "$out/References.txt"
exit $LASTEXITCODE
