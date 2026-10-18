param(
    [string]$RepositoryRoot = (Join-Path $PSScriptRoot '../..'),
    [string]$UnityEditorPath,
    [string[]]$Assemblies = @('SubTerra.Gameplay.Building', 'SubTerra.App.GameplayIntegration', 'SubTerra.Gameplay.Building.EditModeTests', 'SubTerra.Gameplay.Snapshot.EditModeTests', 'SubTerra.App.Tests.EditMode'),
    [string]$OutputDirectory,
    [switch]$CheckClinicCooldown
)
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath($RepositoryRoot)
$projectRoot = Join-Path $repoRoot 'sub-terra'
if (!(Test-Path (Join-Path $projectRoot 'Assets/_Project'))) { throw 'Open the Git repository root, which contains the sub-terra Unity project.' }
$versionText = Get-Content (Join-Path $projectRoot 'ProjectSettings/ProjectVersion.txt') -Raw
$version = [regex]::Match($versionText, 'm_EditorVersion:\s*(\S+)').Groups[1].Value
if (!$UnityEditorPath) { $UnityEditorPath = "C:/Program Files/Unity/Hub/Editor/$version/Editor/Unity.exe" }
if (!(Test-Path -LiteralPath $UnityEditorPath)) { throw 'Provide -UnityEditorPath for this machine. Do not reuse the laptop path blindly.' }
$editorData = Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($UnityEditorPath))) 'Data'
$dotnet = Join-Path $editorData 'DotNetSdk/dotnet.exe'
$sdk = Get-ChildItem (Join-Path $editorData 'DotNetSdk/sdk') -Directory | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
$csc = Join-Path $sdk.FullName 'Roslyn/bincore/csc.dll'
$frameworkRefs = Get-ChildItem (Join-Path $editorData 'NetStandard/ref/2.1.0') -Filter '*.dll' -File
if (!$OutputDirectory) { $OutputDirectory = Join-Path $repoRoot 'work_process/tools/unity-light-check-output' }
$outputRoot = [IO.Path]::GetFullPath($OutputDirectory)
if (!$outputRoot.StartsWith($repoRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Output directory must be inside the repository.' }
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
$resolverPaths = @{}
foreach ($assembly in $Assemblies) {
    $csproj = Join-Path $projectRoot ($assembly + '.csproj')
    if (!(Test-Path -LiteralPath $csproj)) { throw "Missing $assembly.csproj. Generate project files with this machine's Unity Editor once; do not copy laptop Library/csproj files." }
    [xml]$project = Get-Content $csproj -Raw
    $response = [Collections.Generic.List[string]]::new()
    foreach ($option in @('/nologo', '/target:library', '/nostdlib+', '/langversion:9.0', '/warn:0')) { $response.Add($option) }
    $assemblyOutput = Join-Path $outputRoot ($assembly + '.dll')
    $response.Add('/out:"' + $assemblyOutput + '"')
    $defines = $project.SelectSingleNode('//DefineConstants')
    if ($defines) { $response.Add('/define:' + $defines.InnerText) }
    foreach ($reference in $frameworkRefs) { $response.Add('/reference:"' + $reference.FullName + '"') }
    foreach ($node in $project.SelectNodes('//Reference/HintPath')) {
        $path = $node.InnerText
        if (![IO.Path]::IsPathRooted($path)) { $path = Join-Path $projectRoot $path }
        if (!(Test-Path -LiteralPath $path)) { throw "Missing reference $path; regenerate project files on this machine." }
        $path = [IO.Path]::GetFullPath($path)
        $response.Add('/reference:"' + $path + '"')
        $resolverPaths[[IO.Path]::GetFileNameWithoutExtension($path)] = $path
    }
    foreach ($node in $project.SelectNodes('//ProjectReference')) {
        $name = [IO.Path]::GetFileNameWithoutExtension($node.Include)
        $path = Join-Path $outputRoot ($name + '.dll')
        if (!(Test-Path -LiteralPath $path)) { $path = Join-Path $projectRoot ('Library/ScriptAssemblies/' + $name + '.dll') }
        if (!(Test-Path -LiteralPath $path)) { throw "Missing cached dependency $name. Run a normal Unity compilation once on this machine." }
        $response.Add('/reference:"' + $path + '"')
        $resolverPaths[$name] = $path
    }
    foreach ($node in $project.SelectNodes('//Compile')) {
        $path = Join-Path $projectRoot $node.Include
        if (!(Test-Path -LiteralPath $path)) { throw "Missing source $path; regenerate project files on this machine." }
        $response.Add('"' + [IO.Path]::GetFullPath($path) + '"')
    }
    $rsp = Join-Path $outputRoot ($assembly + '.rsp')
    [IO.File]::WriteAllLines($rsp, $response, [Text.UTF8Encoding]::new($false))
    $compilerOutput = & $dotnet $csc ('@' + $rsp) 2>&1
    $compileExit = $LASTEXITCODE
    $compilerOutput | Set-Content (Join-Path $outputRoot ($assembly + '.log')) -Encoding UTF8
    Write-Output "$assembly compile exit=$compileExit"
    if ($compileExit -ne 0) { $compilerOutput | Select-Object -First 10; exit $compileExit }
    $resolverPaths[$assembly] = $assemblyOutput
}
if ($CheckClinicCooldown) {
    # This one test uses only managed state/services. Never run Unity Object/scene tests here.
    $runnerSource = @'
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
public static class PureClinicCheck
{
    public static int Main(string[] args)
    {
        var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in File.ReadAllLines(args[1])) paths[Path.GetFileNameWithoutExtension(path)] = path;
        AppDomain.CurrentDomain.AssemblyResolve += (sender, request) =>
            paths.TryGetValue(new AssemblyName(request.Name).Name, out var path) ? Assembly.LoadFrom(path) : null;
        try
        {
            var assembly = Assembly.LoadFrom(args[0]);
            var type = assembly.GetType("SubTerra.App.Tests.PromptB99FacilityCooldownTests", true);
            type.GetMethod("ClinicMovement_ReturnToRangeDoesNotResetHealingCooldown")
                .Invoke(Activator.CreateInstance(type), null);
            Console.WriteLine("PASS: ClinicMovement_ReturnToRangeDoesNotResetHealingCooldown (managed state only)");
            return 0;
        }
        catch (Exception error)
        {
            Console.WriteLine("FAIL: " + (error.InnerException ?? error));
            return 1;
        }
    }
}
'@
    $sourcePath = Join-Path $outputRoot 'PureClinicCheck.cs'
    [IO.File]::WriteAllText($sourcePath, $runnerSource, [Text.UTF8Encoding]::new($false))
    $runnerDll = Join-Path $outputRoot 'PureClinicCheck.dll'
    $runnerRsp = @('/nologo', '/target:exe', '/nostdlib+', ('/out:"' + $runnerDll + '"'))
    foreach ($reference in $frameworkRefs) { $runnerRsp += '/reference:"' + $reference.FullName + '"' }
    $runnerRsp += '"' + $sourcePath + '"'
    $runnerRspPath = Join-Path $outputRoot 'PureClinicCheck.rsp'
    [IO.File]::WriteAllLines($runnerRspPath, $runnerRsp, [Text.UTF8Encoding]::new($false))
    & $dotnet $csc ('@' + $runnerRspPath)
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    [IO.File]::WriteAllText((Join-Path $outputRoot 'PureClinicCheck.runtimeconfig.json'), '{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.0"}}}')
    $pathsFile = Join-Path $outputRoot 'resolver-paths.txt'
    [IO.File]::WriteAllLines($pathsFile, [string[]]$resolverPaths.Values)
    & $dotnet $runnerDll (Join-Path $outputRoot 'SubTerra.App.Tests.EditMode.dll') $pathsFile
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
Write-Output 'Lightweight compilation complete. This does not validate Unity import, generated code, scenes or native runtime behavior.'
