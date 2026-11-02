# Whole-cel extraction for review only, not runtime-ready exports.
Add-Type -AssemblyName System.Drawing
$folder=Join-Path $PWD 'work_process/images/issue266/female-original-outfit-ladder-v1-review'
New-Item -ItemType Directory -Path $folder -Force | Out-Null
$source=Join-Path $PWD 'work_process/images/issue266/female-original-outfit-ladder-v1.png'
$sheet=[System.Drawing.Bitmap]::new($source)
if($sheet.Width -ne 1536 -or $sheet.Height -ne 1024){throw 'Expected 3x2, 512px cells'}
$cells=@()
for($i=0;$i -lt 5;$i++) {
    $rect=[System.Drawing.Rectangle]::new(($i%3)*512,[int][Math]::Floor($i/3)*512,512,512)
    $cel=$sheet.Clone($rect,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $cel.Save((Join-Path $folder ('pose-{0:D2}.png' -f ($i+1))),[System.Drawing.Imaging.ImageFormat]::Png)
    $cells+=$cel
}
$sequence=@(0,1,2,1,0,3,4,3)
for($i=0;$i -lt $sequence.Count;$i++) {
    $cells[$sequence[$i]].Save((Join-Path $folder ('sequence-{0:D2}.png' -f ($i+1))),[System.Drawing.Imaging.ImageFormat]::Png)
}
$cells | ForEach-Object {$_.Dispose()};$sheet.Dispose()
$ffmpeg=Join-Path $PWD 'work_process/tools/video-deps/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
& $ffmpeg -hide_banner -loglevel error -y -framerate 5 -i (Join-Path $folder 'sequence-%02d.png') -filter_complex 'color=c=0x202428:s=512x512:r=5[bg];[bg][0:v]overlay=shortest=1,scale=384:384,split[a][b];[a]palettegen=stats_mode=full[p];[b][p]paletteuse=dither=sierra2_4a' -loop 0 (Join-Path $folder 'ladder-review.gif')
if($LASTEXITCODE -ne 0){throw 'GIF generation failed'}
