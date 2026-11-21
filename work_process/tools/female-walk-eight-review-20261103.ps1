Add-Type -Path (Join-Path $PSScriptRoot 'MiningWhiteMatte-20261103.cs') -ReferencedAssemblies System.Drawing.Common,System.Drawing.Primitives,System.Private.Windows.GdiPlus,System.Private.Windows.Core,System.Collections
Add-Type -Path (Join-Path $PSScriptRoot 'WholeCelLayout-20261103.cs') -ReferencedAssemblies System.Drawing.Common,System.Drawing.Primitives,System.Private.Windows.GdiPlus,System.Private.Windows.Core,System.Collections
$art=Join-Path (Join-Path $PSScriptRoot '..') 'images/issue266'
$dir=Join-Path $art 'female-walk-eight-review-20261103'
New-Item -ItemType Directory -Force -Path $dir | Out-Null
foreach($group in @(@{Name='key';File='female-walk-keys-v2-white-20261103.png'},@{Name='middle';File='female-walk-inbetweens-v2-white-20261103.png'})) {
 $source=Join-Path $art $group.File
 $layout=Join-Path $dir ($group.Name+'-layout-white.png')
 $alpha=Join-Path $dir ($group.Name+'-alpha.png')
 [WholeCelLayout]::Prepare($source,$layout,768) | Set-Content (Join-Path $dir ($group.Name+'-layout.txt')) -Encoding utf8
 [MiningWhiteMatte]::Process($layout,$alpha,'') | Set-Content (Join-Path $dir ($group.Name+'-alpha-regions.txt')) -Encoding utf8
 [MiningWhiteMatte]::Audit($layout,$alpha) | Set-Content (Join-Path $dir ($group.Name+'-verification.txt')) -Encoding utf8
 $sheet=[System.Drawing.Bitmap]::new($alpha)
 for($i=0;$i -lt 4;$i++) {
  $r=[System.Drawing.Rectangle]::new(($i%2)*768,[int][Math]::Floor($i/2)*768,768,768)
  $cel=$sheet.Clone($r,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
  $cel.Save((Join-Path $dir ('{0}-{1:D2}.png' -f $group.Name,($i+1))),[System.Drawing.Imaging.ImageFormat]::Png)
  $cel.Dispose()
 }
 $sheet.Dispose()
}
$order=@('key-01','middle-01','key-02','middle-02','key-03','middle-03','key-04','middle-04')
for($i=0;$i -lt 8;$i++) {Copy-Item -LiteralPath (Join-Path $dir ($order[$i]+'.png')) -Destination (Join-Path $dir ('sequence-{0:D2}.png' -f ($i+1)))}
foreach($bg in @('White','Black')) {
 $contact=[System.Drawing.Bitmap]::new(1024,512);$g=[System.Drawing.Graphics]::FromImage($contact);$g.Clear([System.Drawing.Color]::$bg)
 for($i=0;$i -lt 8;$i++) {$im=[System.Drawing.Bitmap]::new((Join-Path $dir ($order[$i]+'.png')));$g.DrawImage($im,($i%4)*256,[int][Math]::Floor($i/4)*256,256,256);$im.Dispose()}
 $g.Dispose();$contact.Save((Join-Path $dir ('contact-'+$bg.ToLower()+'.png')),[System.Drawing.Imaging.ImageFormat]::Png);$contact.Dispose()
}
$ffmpeg=Join-Path $PSScriptRoot 'video-deps/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
$base='[0:v]trim=end_frame=8,setpts=PTS-STARTPTS[fg];color=c=0x202428:s=768x768:r=5[bg];[bg][fg]overlay=shortest=1,scale=384:384:flags=lanczos'
& $ffmpeg -hide_banner -loglevel error -y -framerate 5 -i (Join-Path $dir 'sequence-%02d.png') -filter_complex ($base+',split[a][b];[a]palettegen=stats_mode=full:reserve_transparent=0[p];[b][p]paletteuse=dither=0') -loop 0 (Join-Path $dir 'walk-eight.gif')
if($LASTEXITCODE -ne 0){throw 'GIF failed'}
& $ffmpeg -hide_banner -loglevel error -y -framerate 5 -i (Join-Path $dir 'sequence-%02d.png') -filter_complex $base -c:v apng -plays 0 -f apng (Join-Path $dir 'walk-eight-lossless.png')
if($LASTEXITCODE -ne 0){throw 'APNG failed'}
@{order=$order;uniqueCels=8;previewFps=5;previewCycleSeconds=1.6;sourcePixelScaleUnchanged=$true;canvasCellPixels=768;runtimeApplied=$false;note='Original whole-body keys plus four newly drawn whole-body inbetweens. No blended or warped limbs; runtime40 not supplied.'} | ConvertTo-Json -Depth 3 | Set-Content (Join-Path $dir 'sequence.json') -Encoding utf8
