param([string]$RepositoryRoot = (Join-Path $PSScriptRoot '../..'), [string]$UnityEditorPath)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath($RepositoryRoot)
$project = Join-Path $repo 'sub-terra'
$version = [regex]::Match((Get-Content "$project/ProjectSettings/ProjectVersion.txt" -Raw), 'm_EditorVersion:\s*(\S+)').Groups[1].Value
if (!$UnityEditorPath) { $UnityEditorPath = "C:/Program Files/Unity/Hub/Editor/$version/Editor/Unity.exe" }
$data = Join-Path ([IO.Path]::GetDirectoryName($UnityEditorPath)) 'Data'
$sdk = Get-ChildItem "$data/DotNetSdk/sdk" -Directory | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
$out = Join-Path $repo 'work_process/tools/facility-save-check-output'
New-Item -ItemType Directory -Path $out -Force | Out-Null
[xml]$xml = Get-Content "$project/SubTerra.App.Tests.EditMode.csproj" -Raw
$references = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($node in $xml.SelectNodes('//Reference/HintPath')) {
    $path = $node.InnerText
    if (![IO.Path]::IsPathRooted($path)) { $path = Join-Path $project $path }
    if (!(Test-Path -LiteralPath $path)) { throw "Missing reference: $path. Generate Unity project files on this PC." }
    $null = $references.Add([IO.Path]::GetFullPath($path))
}
foreach ($file in Get-ChildItem "$project/Library/ScriptAssemblies" -Filter '*.dll') {
    if ($file.BaseName -ne 'SubTerra.App.Tests.EditMode') { $null = $references.Add($file.FullName) }
}
$runner = @'
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
public static class FacilitySaveMapperCheck {
    public static int Main(string[] args) {
        var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in File.ReadAllLines(args[0])) paths[Path.GetFileNameWithoutExtension(path)] = path;
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) => paths.TryGetValue(new AssemblyName(e.Name).Name, out var path) ? Assembly.LoadFrom(path) : null;
        try {
            var type = Assembly.GetExecutingAssembly().GetType("SubTerra.App.Tests.Save.SaveServiceTests", true);
            type.GetMethod("MovedFacilities_MapperPreservesStorageAndCooldownIdentity").Invoke(Activator.CreateInstance(type), null);
            Console.WriteLine("PASS MovedFacilities_MapperPreservesStorageAndCooldownIdentity");
            Console.WriteLine("Managed mapper only; cached App DLL. No Unity JSON, file-slot, runtime move, or Continue validation.");
            return 0;
        } catch (Exception ex) { Console.WriteLine("FAIL: " + (ex.InnerException ?? ex)); return 1; }
    }
}
'@
[IO.File]::WriteAllText("$out/Runner.cs", $runner, [Text.UTF8Encoding]::new($false))
$dll = "$out/SubTerra.App.Tests.EditMode.dll"
$rsp = @('/nologo', '/target:exe', '/nostdlib+', '/warn:0', '/langversion:9.0', ('/out:"' + $dll + '"'))
foreach ($file in Get-ChildItem "$data/NetStandard/ref/2.1.0" -Filter '*.dll') { $rsp += '/reference:"' + $file.FullName + '"' }
foreach ($path in $references) { $rsp += '/reference:"' + $path + '"' }
$rsp += '"' + "$project/Assets/_Project/Tests/EditMode/App/Save/SaveServiceTests.cs" + '"'
$rsp += '"' + "$out/Runner.cs" + '"'
[IO.File]::WriteAllLines("$out/check.rsp", $rsp, [Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllLines("$out/references.txt", [string[]]$references, [Text.UTF8Encoding]::new($false))
& "$data/DotNetSdk/dotnet.exe" "$($sdk.FullName)/Roslyn/bincore/csc.dll" ("@$out/check.rsp")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
[IO.File]::WriteAllText("$out/SubTerra.App.Tests.EditMode.runtimeconfig.json", '{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.0"}}}')
& "$data/DotNetSdk/dotnet.exe" $dll "$out/references.txt"
exit $LASTEXITCODE
