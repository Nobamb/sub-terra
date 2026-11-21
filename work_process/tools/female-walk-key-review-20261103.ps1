# Review of four complete new drawings; no body-part assembly, morphing or duplicated inbetweens.
Add-Type -Path (Join-Path $PSScriptRoot 'MiningWhiteMatte-20261103.cs') -ReferencedAssemblies System.Drawing.Common,System.Drawing.Primitives,System.Private.Windows.GdiPlus,System.Private.Windows.Core,System.Collections
$art=Join-Path (Join-Path $PSScriptRoot '..') 'images/issue266'
$source=Join-Path $art 'female-walk-keys-v2-white-20261103.png'
$alpha=Join-Path $art 'female-walk-keys-v3-alpha-20261103.png'
$dir=Join-Path $art 'female-walk-key-review-20261103'
New-Item -ItemType Directory -Force -Path $dir | Out-Null
[MiningWhiteMatte]::Process($source,$alpha,'') | Set-Content (Join-Path $dir 'alpha-regions.txt') -Encoding utf8
[MiningWhiteMatte]::Audit($source,$alpha) | Set-Content (Join-Path $dir 'verification.txt') -Encoding utf8
$sheet=[System.Drawing.Bitmap]::new($alpha)
$cell=[int]($sheet.Width/2)
for($i=0;$i -lt 4;$i++) {
 $r=[System.Drawing.Rectangle]::new(($i%2)*$cell,[int][Math]::Floor($i/2)*$cell,$cell,$cell)
 $cel=$sheet.Clone($r,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $cel.Save((Join-Path $dir ('pose-{0:D2}.png' -f ($i+1))),[System.Drawing.Imaging.ImageFormat]::Png)
 $cel.Dispose()
}
foreach($bg in @('White','Black')) {
 $out=[System.Drawing.Bitmap]::new(628,628)
 $g=[System.Drawing.Graphics]::FromImage($out)
 $g.Clear([System.Drawing.Color]::$bg)
 $g.DrawImage($sheet,0,0,628,628)
 $g.Dispose();$out.Save((Join-Path $dir ('contact-'+$bg.ToLower()+'.png')),[System.Drawing.Imaging.ImageFormat]::Png);$out.Dispose()
}
$sheet.Dispose()
$ffmpeg=Join-Path $PSScriptRoot 'video-deps/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
$base=('[0:v]trim=end_frame=4,setpts=PTS-STARTPTS[fg];color=c=0x202428:s={0}x{0}:r=5/2[bg];[bg][fg]overlay=shortest=1,scale=384:384:flags=lanczos' -f $cell)
& $ffmpeg -hide_banner -loglevel error -y -framerate 5/2 -i (Join-Path $dir 'pose-%02d.png') -filter_complex ($base+',split[a][b];[a]palettegen=stats_mode=full:reserve_transparent=0[p];[b][p]paletteuse=dither=0') -loop 0 (Join-Path $dir 'walk-keys.gif')
if($LASTEXITCODE -ne 0){throw 'GIF generation failed'}
& $ffmpeg -hide_banner -loglevel error -y -framerate 5/2 -i (Join-Path $dir 'pose-%02d.png') -filter_complex $base -c:v apng -plays 0 -f apng (Join-Path $dir 'walk-keys-lossless.png')
if($LASTEXITCODE -ne 0){throw 'APNG generation failed'}
@{poses=@('near-contact','far-pass','far-contact','near-pass');sourceCellPixels=$cell;order=@(1,2,3,4);previewFps=2.5;previewCycleSeconds=1.6;runtimeApplied=$false;note='Four key poses only; held poses are not smooth inbetween animation.'} | ConvertTo-Json -Depth 3 | Set-Content (Join-Path $dir 'sequence.json') -Encoding utf8
