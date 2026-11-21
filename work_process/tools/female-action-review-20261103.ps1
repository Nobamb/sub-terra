param([string[]]$Actions=@('jump','damage','knockout'))
# Four whole-drawing key poses per action. Runtime assets and timing are untouched.
Add-Type -Path (Join-Path $PSScriptRoot 'MiningWhiteMatte-20261103.cs') -ReferencedAssemblies System.Drawing.Common,System.Drawing.Primitives,System.Private.Windows.GdiPlus,System.Private.Windows.Core,System.Collections
Add-Type -Path (Join-Path $PSScriptRoot 'WholeCelLayout-20261103.cs') -ReferencedAssemblies System.Drawing.Common,System.Drawing.Primitives,System.Private.Windows.GdiPlus,System.Private.Windows.Core,System.Collections
$art=Join-Path (Join-Path $PSScriptRoot '..') 'images/issue266'
$ffmpeg=Join-Path $PSScriptRoot 'video-deps/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
foreach($action in $Actions) {
 $source=Join-Path $art ('female-'+$action+'-keys-v1-white-20261103.png')
 $layout=Join-Path $art ('female-'+$action+'-keys-layout-white-20261103.png')
 $alpha=Join-Path $art ('female-'+$action+'-keys-v2-alpha-20261103.png')
 $dir=Join-Path $art ('female-'+$action+'-review-20261103')
 New-Item -ItemType Directory -Force -Path $dir | Out-Null
 [WholeCelLayout]::Prepare($source,$layout,768) | Set-Content (Join-Path $dir 'layout.txt') -Encoding utf8
 [MiningWhiteMatte]::Process($layout,$alpha,'') | Set-Content (Join-Path $dir 'alpha-regions.txt') -Encoding utf8
 [MiningWhiteMatte]::Audit($layout,$alpha) | Set-Content (Join-Path $dir 'verification.txt') -Encoding utf8
 $sheet=[System.Drawing.Bitmap]::new($alpha)
 $cell=[int]($sheet.Width/2)
 if($sheet.Width -ne $sheet.Height){throw 'Expected square2x2 action sheet'}
 for($i=0;$i -lt 4;$i++) {
  $r=[System.Drawing.Rectangle]::new(($i%2)*$cell,[int][Math]::Floor($i/2)*$cell,$cell,$cell)
  $cel=$sheet.Clone($r,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
  $cel.Save((Join-Path $dir ('pose-{0:D2}.png' -f ($i+1))),[System.Drawing.Imaging.ImageFormat]::Png)
  $cel.Dispose()
 }
 foreach($bg in @('White','Black')) {
  $out=[System.Drawing.Bitmap]::new(628,628);$g=[System.Drawing.Graphics]::FromImage($out)
  $g.Clear([System.Drawing.Color]::$bg);$g.DrawImage($sheet,0,0,628,628);$g.Dispose()
  $out.Save((Join-Path $dir ('contact-'+$bg.ToLower()+'.png')),[System.Drawing.Imaging.ImageFormat]::Png);$out.Dispose()
 }
 $sheet.Dispose()
 $sequence=if($action -eq 'damage'){@(1,2,3,4,1)}else{@(1,2,3,4,4,4)}
 for($i=0;$i -lt $sequence.Count;$i++) {
  Copy-Item -LiteralPath (Join-Path $dir ('pose-{0:D2}.png' -f $sequence[$i])) -Destination (Join-Path $dir ('sequence-{0:D2}.png' -f ($i+1)))
 }
 $base=('[0:v]trim=end_frame={1},setpts=PTS-STARTPTS[fg];color=c=0x202428:s={0}x{0}:r=4[bg];[bg][fg]overlay=shortest=1,scale=384:384:flags=lanczos' -f $cell,$sequence.Count)
 & $ffmpeg -hide_banner -loglevel error -y -framerate 4 -i (Join-Path $dir 'sequence-%02d.png') -filter_complex ($base+',split[a][b];[a]palettegen=stats_mode=full:reserve_transparent=0[p];[b][p]paletteuse=dither=0') -loop -1 (Join-Path $dir ($action+'-keys.gif'))
 if($LASTEXITCODE -ne 0){throw 'GIF failed'}
 & $ffmpeg -hide_banner -loglevel error -y -framerate 4 -i (Join-Path $dir 'sequence-%02d.png') -filter_complex $base -c:v apng -plays 1 -f apng (Join-Path $dir ($action+'-keys-lossless.png'))
 if($LASTEXITCODE -ne 0){throw 'APNG failed'}
 @{action=$action;sequence=$sequence;uniqueCels=4;previewFps=4;plays=1;runtimeApplied=$false;note='Slow key-pose review only; not full runtime frames or timing.'} | ConvertTo-Json -Depth 3 | Set-Content (Join-Path $dir 'sequence.json') -Encoding utf8
}
