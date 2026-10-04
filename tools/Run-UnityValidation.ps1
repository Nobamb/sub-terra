param(
    [ValidateSet('quick', 'daily', 'visual', 'full')][string]$Scope = 'daily',
    [ValidateSet('EditMode', 'PlayMode', 'Both')][string]$Mode = 'PlayMode',
    [string]$Filter = '',
    [string]$Output = '',
    [int]$TimeoutSeconds = 1200,
    [switch]$Batch,
    [string]$EditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.5.4f1\Editor\Unity.exe'
)

$ErrorActionPreference = 'Stop'
$projectPath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\sub-terra'))
if ($Scope -eq 'quick' -and [string]::IsNullOrWhiteSpace($Filter)) { throw 'quick requires -Filter.' }
if ($Batch -and ($Scope -eq 'visual' -or $Scope -eq 'full')) { throw 'Visual/full requires the rendering Editor; omit -Batch.' }
if ([string]::IsNullOrWhiteSpace($Output)) {
    $Output = Join-Path $projectPath ('Temp\validation-' + [Guid]::NewGuid().ToString('N'))
}
$Output = [IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Force -Path $Output | Out-Null
$complete = Join-Path $Output 'complete.txt'
if (Test-Path -LiteralPath $complete) { throw 'Use a fresh output folder to avoid stale results.' }
$request = @{ scope = $Scope; mode = $Mode; filter = $Filter; output = $Output; quit = $false }

if ($Batch) {
    $arguments = @('-batchmode', '-projectPath', ('"' + $projectPath + '"'),
        '-executeMethod', 'SubTerra.App.Editor.DataValidation.TestValidationRunner.Batch',
        '-validationScope', $Scope, '-validationMode', $Mode,
        '-subterra-save-root', ('"' + (Join-Path $Output 'isolated-saves') + '"'),
        '-validationOutput', ('"' + $Output + '"'), '-logFile', ('"' + (Join-Path $Output 'Editor.log') + '"'))
    if ($Filter) { $arguments += @('-validationFilter', ('"' + $Filter + '"')) }
    $process = Start-Process -FilePath $EditorPath -ArgumentList $arguments -PassThru -WindowStyle Hidden
} else {
    $flag = Join-Path $projectPath 'Temp\subterra-validation.json'
    if (Test-Path -LiteralPath $flag) { throw 'A validation request is already pending.' }
    [IO.File]::WriteAllText($flag, ($request | ConvertTo-Json -Compress))
}

$timer = [Diagnostics.Stopwatch]::StartNew()
while (-not (Test-Path -LiteralPath $complete)) {
    if ($Batch -and $process.HasExited) {
        Write-Error "Unity exited before producing complete.txt (exit $($process.ExitCode))."
        exit 1
    }
    if ($timer.Elapsed.TotalSeconds -ge $TimeoutSeconds) {
        [IO.File]::WriteAllText((Join-Path $projectPath 'Temp\subterra-validation-cancel.flag'), '')
        Write-Error "Validation timed out after $TimeoutSeconds seconds. Results: $Output"
        exit 1
    }
    Start-Sleep -Milliseconds 500
}
foreach ($testMode in @('EditMode', 'PlayMode')) {
    $summary = Join-Path $Output ($testMode + '-summary.txt')
    if (Test-Path -LiteralPath $summary) { Get-Content -LiteralPath $summary }
}
Write-Output "Results: $Output"
$code = if ((Get-Content -Raw -LiteralPath $complete).Trim() -eq 'ExitCode: 0') { 0 } else { 1 }
if ($Batch) {
    if (-not $process.WaitForExit(60000)) { Write-Error 'Unity did not exit after completing tests.'; exit 1 }
    if ($process.ExitCode -ne 0) { $code = 1 }
}
exit $code
