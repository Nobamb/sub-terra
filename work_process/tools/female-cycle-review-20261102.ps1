param([Parameter(Mandatory=$true)][string]$Source,[Parameter(Mandatory=$true)][string]$OutputPrefix)
# QA only: whole-cell extraction and background composites; no anatomy edits.
Add-Type -AssemblyName System.Drawing
$src = [System.Drawing.Bitmap]::new((Resolve-Path -LiteralPath $Source).Path)
$cellWidth = [int]($src.Width / 4)
$cellHeight = [int]($src.Height / 2)
$folder = Join-Path $PWD ($OutputPrefix + '-frames')
New-Item -ItemType Directory -Path $folder -Force | Out-Null
$report = @()
$aligned = [System.Drawing.Bitmap]::new($src.Width,$src.Height)
$ag = [System.Drawing.Graphics]::FromImage($aligned)
$sheet = [System.Drawing.Bitmap]::new($src.Width,$src.Height)
$sg = [System.Drawing.Graphics]::FromImage($sheet)
$sg.Clear([System.Drawing.Color]::White)
$sg.DrawImage($src,0,0,$src.Width,$src.Height)
$sheet.Save((Join-Path $PWD ($OutputPrefix + '-white.jpg')),[System.Drawing.Imaging.ImageFormat]::Jpeg)
$sg.Dispose(); $sheet.Dispose()
for($i=0;$i -lt 8;$i++) {
    $rect = [System.Drawing.Rectangle]::new(($i % 4)*$cellWidth,[int][Math]::Floor($i / 4)*$cellHeight,$cellWidth,$cellHeight)
    $cel = $src.Clone($rect,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $left=$cellWidth; $top=$cellHeight; $right=0; $bottom=0
    for($y=0;$y -lt $cellHeight;$y++) {
        for($x=0;$x -lt $cellWidth;$x++) {
            if($cel.GetPixel($x,$y).A -gt 12) {
                $left=[Math]::Min($left,$x); $right=[Math]::Max($right,$x)
                $top=[Math]::Min($top,$y); $bottom=[Math]::Max($bottom,$y)
            }
        }
    }
    $shift = 16-$top
    $report += [pscustomobject]@{frame=$i+1;left=$left;top=$top;right=$right;bottom=$bottom;height=$bottom-$top+1;whole_frame_y_shift=$shift;aligned_bottom=$bottom+$shift}
    $export = [System.Drawing.Bitmap]::new($cellWidth,$cellHeight)
    $eg = [System.Drawing.Graphics]::FromImage($export)
    $eg.DrawImageUnscaled($cel,0,$shift)
    $export.Save((Join-Path $folder ('{0:D2}.png' -f ($i+1))),[System.Drawing.Imaging.ImageFormat]::Png)
    $ag.DrawImageUnscaled($export,$rect.X,$rect.Y)
    $eg.Dispose(); $export.Dispose()
    $cel.Dispose()
}
$src.Dispose()
$ag.Dispose()
$aligned.Save((Join-Path $PWD ($OutputPrefix + '-aligned.png')),[System.Drawing.Imaging.ImageFormat]::Png)
$aligned.Dispose()
$report | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $folder 'bounds.json') -Encoding utf8
$report | Format-Table | Out-String | Write-Output
$ffmpeg = Join-Path $PWD 'work_process/tools/video-deps/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
# Composite only for review GIF. Transparent PNG originals remain untouched.
& $ffmpeg -hide_banner -loglevel error -y -framerate '50/7' -i (Join-Path $folder '%02d.png') -filter_complex "color=c=0x202428:s=${cellWidth}x${cellHeight}:r=50/7[bg];[bg][0:v]overlay=shortest=1,split[a][b];[a]palettegen=stats_mode=full[p];[b][p]paletteuse=dither=sierra2_4a" -loop 0 (Join-Path $PWD ($OutputPrefix + '.gif'))
if($LASTEXITCODE -ne 0) { throw 'GIF export failed' }
