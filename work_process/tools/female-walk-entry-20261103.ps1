$ErrorActionPreference='Stop'
$assemblies=@('System.Drawing.Common','System.Drawing.Primitives','System.Private.Windows.GdiPlus','System.Private.Windows.Core','System.Collections')
Add-Type -Path (Join-Path $PSScriptRoot 'LandingRecoveryReview-20261103.cs') -ReferencedAssemblies $assemblies
Add-Type -Path (Join-Path $PSScriptRoot 'SpriteMotionAlignment-20261103.cs') -ReferencedAssemblies $assemblies
$art=(Resolve-Path (Join-Path $PSScriptRoot '../images/issue266')).Path
$source=Join-Path $art 'female-walk-entry-v1-alpha-20261103.png'
$baseline=Join-Path $art 'female-motion-alignment-review-20261103'
$dir=Join-Path $art 'female-walk-entry-review-20261103'
New-Item -ItemType Directory -Force -Path $dir | Out-Null
$hash=(Get-FileHash -LiteralPath $source).Hash
$reports=@();$records=@();$paths=@()
for($i=0;$i -lt 2;$i++) {
 $raw=Join-Path $dir ('entry-source-{0:D2}.png' -f ($i+1))
 $reports+=[LandingRecoveryReview]::Extract($source,$raw,$i)
 $paths+=$raw
}
# Fixed whole-cel scale for both new drawings, using the established helmet reference.
$scale=161.0/([SpriteMotionAlignment]::Measure($paths[0])).Helmet.Width
for($i=0;$i -lt 2;$i++) {
 $m=[SpriteMotionAlignment]::Measure($paths[$i]);$output=Join-Path $dir ('entry-{0:D2}.png' -f ($i+1))
 $dx=512-$m.Belt.CenterX*$scale;$dy=800-($m.Bounds.Y+$m.Bounds.Height)*$scale
 $reports+=[SpriteMotionAlignment]::Render($paths[$i],$output,$scale,$dx,$dy)
 $after=[SpriteMotionAlignment]::Measure($output)
 if([Math]::Abs($after.Bounds.Y+$after.Bounds.Height-800)>2){throw 'Ground mismatch'}
 $records+=@{pose=$i+1;scale=$scale;dx=$dx;dy=$dy;measure=$after}
}
$poses=@((Join-Path $baseline 'damage/pose-01.png'),(Join-Path $dir 'entry-01.png'),(Join-Path $dir 'entry-02.png'),(Join-Path $baseline 'walk/key-01.png'))
$labels=@('대기 참고','진입 1','진입 2','첫 걸음')
foreach($bg in @('White','Black')) {
 $contact=[System.Drawing.Bitmap]::new(1024,288);$g=[System.Drawing.Graphics]::FromImage($contact);$g.Clear([System.Drawing.Color]::$bg)
 $brush=if($bg -eq 'White'){[System.Drawing.Brushes]::Black}else{[System.Drawing.Brushes]::White};$font=[System.Drawing.Font]::new('Malgun Gothic',12)
 for($i=0;$i -lt 4;$i++){$im=[System.Drawing.Bitmap]::new($poses[$i]);$g.DrawImage($im,$i*256,32,256,256);$im.Dispose();$g.DrawString($labels[$i],$font,$brush,$i*256+8,5)}
 $font.Dispose();$g.Dispose();$contact.Save((Join-Path $dir ('contact-'+$bg.ToLower()+'.png')));$contact.Dispose()
}
$order=@('key-01','middle-01','key-02','middle-02','key-03','middle-03','key-04','middle-04')
$walk=@($order | ForEach-Object {Join-Path $baseline ('walk/'+$_+'.png')})
$afterPaths=@($poses[0],$poses[0],$poses[1],$poses[2])+$walk
$beforePaths=@($poses[0],$poses[0],$poses[0],$poses[0])+$walk
for($i=0;$i -lt $afterPaths.Count;$i++) {
 $card=[System.Drawing.Bitmap]::new(640,352);$g=[System.Drawing.Graphics]::FromImage($card);$g.Clear([System.Drawing.Color]::FromArgb(32,36,40));$font=[System.Drawing.Font]::new('Malgun Gothic',12)
 $g.DrawString('기존 · 바로 첫 걸음',$font,[System.Drawing.Brushes]::White,8,5);$g.DrawString('진입 2자세 추가',$font,[System.Drawing.Brushes]::White,328,5)
 foreach($pair in @(@{Path=$beforePaths[$i];X=0},@{Path=$afterPaths[$i];X=320})){$im=[System.Drawing.Bitmap]::new($pair.Path);$g.DrawImage($im,$pair.X,32,320,320);$im.Dispose()}
 $font.Dispose();$g.Dispose();$card.Save((Join-Path $dir ('compare-{0:D2}.png' -f ($i+1))));$card.Dispose()
}
$ffmpeg=Join-Path $PSScriptRoot 'video-deps/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
$input=Join-Path $dir 'compare-%02d.png';$trim='[0:v]trim=end_frame=12,setpts=PTS-STARTPTS'
& $ffmpeg -hide_banner -loglevel error -y -framerate 6 -i $input -filter_complex ($trim+',split[a][b];[a]palettegen=stats_mode=full:reserve_transparent=0[p];[b][p]paletteuse=dither=0') -loop -1 (Join-Path $dir 'entry-compare.gif')
if($LASTEXITCODE -ne 0){throw 'GIF failed'}
& $ffmpeg -hide_banner -loglevel error -y -framerate 6 -i $input -filter_complex $trim -c:v apng -plays 1 -f apng (Join-Path $dir 'entry-compare-lossless.png')
if($LASTEXITCODE -ne 0){throw 'APNG failed'}
foreach($name in @('entry-compare.gif','entry-compare-lossless.png')){& $ffmpeg -hide_banner -loglevel error -i (Join-Path $dir $name) -f null -;if($LASTEXITCODE -ne 0){throw 'Decode failed'}}
if((Get-FileHash -LiteralPath $source).Hash -ne $hash){throw 'Source changed'}
@{runtimeApplied=$false;sourceSha256=$hash;canvas=1024;reviewGroundY=800;fixedScale=$scale;newWholeBodyCels=2;previewFps=6;previewFrames=12;plays=1;after=@('idle','idle','entry-01','entry-02')+$order;frames=$records;note='Whole-body drawings; shared uniform scale and translation only. Single-play held review; game acceleration/foot lock not applied.'} | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $dir 'sequence.json') -Encoding utf8
$reports+=@('sourceUnchanged=True','newWholeBodyCels=2','runtimeApplied=False','gifApngDecode=True','groundTolerancePixels=2')
$reports | Set-Content (Join-Path $dir 'verification.txt') -Encoding utf8
$reports
