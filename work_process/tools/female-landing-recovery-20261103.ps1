$ErrorActionPreference='Stop'
$assemblies=@('System.Drawing.Common','System.Drawing.Primitives','System.Private.Windows.GdiPlus','System.Private.Windows.Core','System.Collections')
Add-Type -Path (Join-Path $PSScriptRoot 'LandingRecoveryReview-20261103.cs') -ReferencedAssemblies $assemblies
Add-Type -Path (Join-Path $PSScriptRoot 'SpriteMotionAlignment-20261103.cs') -ReferencedAssemblies $assemblies
$art=(Resolve-Path (Join-Path $PSScriptRoot '../images/issue266')).Path
$sources=@((Join-Path $art 'female-landing-recovery-v2-alpha-20261103.png'),(Join-Path $art 'female-landing-recovery-v1-alpha-20261103.png'))
$columns=@(1,0)
$baseline=Join-Path $art 'female-motion-alignment-review-20261103'
$dir=Join-Path $art 'female-landing-recovery-review-20261103'
New-Item -ItemType Directory -Force -Path $dir | Out-Null
$hashes=@($sources | ForEach-Object {(Get-FileHash -LiteralPath $_).Hash})
$reports=@();$records=@();$paths=@()
for($i=0;$i -lt 2;$i++) {
 $raw=Join-Path $dir ('recovery-source-{0:D2}.png' -f ($i+1))
 $reports+=[LandingRecoveryReview]::Extract($sources[$i],$raw,$columns[$i])
 $paths+=$raw
}
# One shared uniform scale for the new pair; never resize separate body parts.
$first=[SpriteMotionAlignment]::Measure($paths[1])
$scale=161.0/$first.Helmet.Width
for($i=0;$i -lt 2;$i++) {
 $m=[SpriteMotionAlignment]::Measure($paths[$i]);$output=Join-Path $dir ('recovery-{0:D2}.png' -f ($i+1))
 $dx=512-$m.Belt.CenterX*$scale;$dy=800-($m.Bounds.Y+$m.Bounds.Height)*$scale
 $reports+=[SpriteMotionAlignment]::Render($paths[$i],$output,$scale,$dx,$dy)
 $after=[SpriteMotionAlignment]::Measure($output)
 if([Math]::Abs($after.Bounds.Y+$after.Bounds.Height-800)>2){throw 'Ground mismatch'}
 $records+=@{pose=$i+1;scale=$scale;dx=$dx;dy=$dy;measure=$after}
}
$poses=@((Join-Path $baseline 'jump/pose-04.png'),(Join-Path $dir 'recovery-01.png'),(Join-Path $dir 'recovery-02.png'),(Join-Path $baseline 'damage/pose-01.png'))
$heights=@($poses | ForEach-Object {([SpriteMotionAlignment]::Measure($_)).Bounds.Height})
$monotonic=($heights[0] -lt $heights[1] -and $heights[1] -lt $heights[2] -and $heights[2] -lt $heights[3])
$reports+='heightSequence='+($heights -join ',')
$reports+='monotonicRecoveryHeight='+$monotonic
$labels=@('착지','회복 1','회복 2','대기 참고')
foreach($bg in @('White','Black')) {
 $contact=[System.Drawing.Bitmap]::new(1024,288);$g=[System.Drawing.Graphics]::FromImage($contact);$g.Clear([System.Drawing.Color]::$bg)
 $brush=if($bg -eq 'White'){[System.Drawing.Brushes]::Black}else{[System.Drawing.Brushes]::White}
 $font=[System.Drawing.Font]::new('Malgun Gothic',12)
 for($i=0;$i -lt 4;$i++){$im=[System.Drawing.Bitmap]::new($poses[$i]);$g.DrawImage($im,$i*256,32,256,256);$im.Dispose();$g.DrawString($labels[$i],$font,$brush,$i*256+8,5)}
 $font.Dispose();$g.Dispose();$contact.Save((Join-Path $dir ('contact-'+$bg.ToLower()+'.png')));$contact.Dispose()
}
# Two held endpoints plus two recovery cels; before/after on the same time grid.
$afterOrder=@(0,0,1,2,3,3,3,3);$beforeOrder=@(0,0,3,3,3,3,3,3)
for($i=0;$i -lt $afterOrder.Count;$i++) {
 $card=[System.Drawing.Bitmap]::new(640,352);$g=[System.Drawing.Graphics]::FromImage($card);$g.Clear([System.Drawing.Color]::FromArgb(32,36,40))
 $font=[System.Drawing.Font]::new('Malgun Gothic',12)
 $g.DrawString('기존 · 바로 대기',$font,[System.Drawing.Brushes]::White,8,5);$g.DrawString('회복 2자세 추가',$font,[System.Drawing.Brushes]::White,328,5)
 foreach($pair in @(@{Index=$beforeOrder[$i];X=0},@{Index=$afterOrder[$i];X=320})){$im=[System.Drawing.Bitmap]::new($poses[$pair.Index]);$g.DrawImage($im,$pair.X,32,320,320);$im.Dispose()}
 $font.Dispose();$g.Dispose();$card.Save((Join-Path $dir ('compare-{0:D2}.png' -f ($i+1))));$card.Dispose()
}
$ffmpeg=Join-Path $PSScriptRoot 'video-deps/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
$input=Join-Path $dir 'compare-%02d.png';$trim='[0:v]trim=end_frame=8,setpts=PTS-STARTPTS'
& $ffmpeg -hide_banner -loglevel error -y -framerate 8 -i $input -filter_complex ($trim+',split[a][b];[a]palettegen=stats_mode=full:reserve_transparent=0[p];[b][p]paletteuse=dither=0') -loop 0 (Join-Path $dir 'recovery-compare.gif')
if($LASTEXITCODE -ne 0){throw 'GIF failed'}
& $ffmpeg -hide_banner -loglevel error -y -framerate 8 -i $input -filter_complex $trim -c:v apng -plays 0 -f apng (Join-Path $dir 'recovery-compare-lossless.png')
if($LASTEXITCODE -ne 0){throw 'APNG failed'}
foreach($name in @('recovery-compare.gif','recovery-compare-lossless.png')){& $ffmpeg -hide_banner -loglevel error -i (Join-Path $dir $name) -f null -;if($LASTEXITCODE -ne 0){throw 'Decode failed'}}
for($i=0;$i -lt 2;$i++){if((Get-FileHash -LiteralPath $sources[$i]).Hash -ne $hashes[$i]){throw 'Source changed'}}
@{runtimeApplied=$false;sourceSha256=$hashes;selectedSources=@('v2 column 1','v1 column 0');canvas=1024;reviewGroundY=800;fixedScale=$scale;newWholeBodyCels=2;monotonicRecoveryHeight=$monotonic;heights=$heights;previewFps=8;before=$beforeOrder;after=$afterOrder;frames=$records;note='Two selected whole-body drawings from the current recovery trials. No body-part assembly. Shared uniform scale, belt root and foot alignment only. Preview timing is review-only, not game timing.'} | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $dir 'sequence.json') -Encoding utf8
$reports+=@('sourceUnchanged=True','newWholeBodyCels=2','runtimeApplied=False','gifApngDecode=True','groundTolerancePixels=2')
$reports | Set-Content (Join-Path $dir 'verification.txt') -Encoding utf8
$reports
