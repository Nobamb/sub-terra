param([switch]$Prepare,[string]$Motion='ladder',[string]$Sheet)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$assemblies=@('System.Drawing.Common','System.Drawing.Primitives','System.Private.Windows.GdiPlus','System.Private.Windows.Core','System.Collections')
Add-Type -Path (Join-Path $PSScriptRoot 'LandingRecoveryReview-20261103.cs') -ReferencedAssemblies $assemblies
Add-Type -Path (Join-Path $PSScriptRoot 'SpriteMotionAlignment-20261103.cs') -ReferencedAssemblies $assemblies
$art=(Resolve-Path (Join-Path $PSScriptRoot '../images/issue266')).Path
$root=Join-Path $art 'female-matte-surface-review-20261104'
New-Item -ItemType Directory -Force -Path $root | Out-Null
$walkNames=@('key-01','middle-01','key-02','middle-02','key-03','middle-03','key-04','middle-04')
$mining=Get-Content (Join-Path $art 'female-mining-inbetween-review-20261103/sequence.json') -Raw | ConvertFrom-Json
if($Prepare){
 foreach($kind in @('walk','mining')){
  $names=if($kind -eq 'walk'){$walkNames}else{@($mining.uniqueCels)}
  $rows=[int][Math]::Ceiling($names.Count/3.0)
  $canvas=[System.Drawing.Bitmap]::new(1536,$rows*512);$g=[System.Drawing.Graphics]::FromImage($canvas);$g.Clear([System.Drawing.Color]::Transparent)
  $records=@()
  for($i=0;$i -lt $names.Count;$i++){
   $name=$names[$i]
   $relative=if($kind -eq 'mining'){'female-mining-inbetween-review-20261103/'+$name+'.png'}elseif($name -in @('middle-02','middle-04')){'female-gait-repair-review-20261104/'+$name+'.png'}else{'female-motion-alignment-review-20261103/walk/'+$name+'.png'}
   $path=Join-Path $art $relative;$m=[SpriteMotionAlignment]::Measure($path)
   $im=[System.Drawing.Bitmap]::new($path)
   $scale=[Math]::Min(430.0/$m.Bounds.Height,450.0/$m.Bounds.Width)
   $w=[single]($m.Bounds.Width*$scale);$h=[single]($m.Bounds.Height*$scale)
   $dest=[System.Drawing.RectangleF]::new([single](($i%3)*512+(512-$w)/2),[single]([Math]::Floor($i/3)*512+480-$h),$w,$h)
   $sourceRect=[System.Drawing.RectangleF]::new([single]$m.Bounds.X,[single]$m.Bounds.Y,[single]$m.Bounds.Width,[single]$m.Bounds.Height)
   $g.DrawImage($im,$dest,$sourceRect,[System.Drawing.GraphicsUnit]::Pixel);$im.Dispose()
   $records+=@{name=$name;source=$relative;sha256=(Get-FileHash -LiteralPath $path).Hash;cell=$i;sheetScale=$scale}
  }
  $g.Dispose();$canvas.Save((Join-Path $root ($kind+'-edit-target.png')));$canvas.Dispose()
  @{motion=$kind;columns=3;rows=$rows;frames=$records;runtimeApplied=$false} | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $root ($kind+'-source-map.json')) -Encoding utf8
 }
 exit
}
if(-not $Sheet){throw 'Provide generated sheet'}
$names=if($Motion -eq 'ladder'){@('pose-01','pose-02','pose-03','pose-04','pose-05')}elseif($Motion -eq 'walk'){$walkNames}else{@($mining.uniqueCels)}
$rows=[int][Math]::Ceiling($names.Count/3.0)
$dir=Join-Path $root $Motion;New-Item -ItemType Directory -Force -Path $dir | Out-Null
$im=[System.Drawing.Bitmap]::new((Resolve-Path $Sheet).Path)
$cw=[int][Math]::Floor($im.Width/3.0);$ch=[int][Math]::Floor($im.Height/[double]$rows);$size=[Math]::Max($cw,$ch)
$checks=@();$records=@()
for($i=0;$i -lt $names.Count;$i++){
 $raw=[System.Drawing.Bitmap]::new($size,$size);$g=[System.Drawing.Graphics]::FromImage($raw);$g.Clear([System.Drawing.Color]::Transparent)
 $g.DrawImage($im,[System.Drawing.Rectangle]::new([int](($size-$cw)/2),[int](($size-$ch)/2),$cw,$ch),($i%3)*$cw,[int][Math]::Floor($i/3)*$ch,$cw,$ch,[System.Drawing.GraphicsUnit]::Pixel)
 $g.Dispose();$rawPath=Join-Path $dir ($names[$i]+'-raw.png');$raw.Save($rawPath);$raw.Dispose()
 $clean=Join-Path $dir ($names[$i]+'-clean.png');$checks+=[LandingRecoveryReview]::CleanCel($rawPath,$clean)
 $m=[SpriteMotionAlignment]::Measure($clean)
 $records+=@{name=$names[$i];sourceCell=$i;measure=$m;clean=$clean}
}
$im.Dispose()
$neutral=$records[0];$scale=584.0/$neutral.measure.Bounds.Height
$neutralX=[LandingRecoveryReview]::FootSpanCenter($neutral.clean);$neutralY=$neutral.measure.Bounds.Y+$neutral.measure.Bounds.Height
foreach($r in $records){
 $m=$r.measure
 $rx=if($Motion -eq 'ladder'){$neutralX}else{[LandingRecoveryReview]::FootSpanCenter($r.clean)}
 $ry=if($Motion -eq 'ladder'){$neutralY}else{$m.Bounds.Y+$m.Bounds.Height}
 $r.output=Join-Path $dir ($r.name+'.png');$r.scale=$scale;$r.dx=512-$rx*$scale;$r.dy=800-$ry*$scale
 $checks+=[SpriteMotionAlignment]::Render($r.clean,$r.output,$scale,$r.dx,$r.dy)
}
foreach($bg in @('White','Black')){
 $card=[System.Drawing.Bitmap]::new(3*256,$rows*280);$g=[System.Drawing.Graphics]::FromImage($card);$g.Clear([System.Drawing.Color]::$bg)
 for($i=0;$i -lt $records.Count;$i++){$cel=[System.Drawing.Bitmap]::new($records[$i].output);$g.DrawImage($cel,($i%3)*256,[int][Math]::Floor($i/3)*280,256,256);$cel.Dispose()}
 $g.Dispose();$card.Save((Join-Path $root ($Motion+'-contact-'+$bg.ToLower()+'.png')));$card.Dispose()
}
$order=if($Motion -eq 'ladder'){@('pose-01','pose-02','pose-03','pose-02','pose-01','pose-04','pose-05','pose-04')}elseif($Motion -eq 'walk'){$walkNames}else{@($mining.work)}
$fps=if($Motion -eq 'ladder'){5}elseif($Motion -eq 'walk'){8}else{8}
for($i=0;$i -lt $order.Count;$i++){Copy-Item -LiteralPath (Join-Path $dir ($order[$i]+'.png')) -Destination (Join-Path $dir ('clip-{0:D2}.png' -f $i))}
$ffmpeg=Join-Path $PSScriptRoot 'video-deps/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
$filter='[0:v]trim=end_frame='+$order.Count+',setpts=PTS-STARTPTS[fg];color=c=0x202428:s=1024x1024:r='+$fps+'[bg];[bg][fg]overlay=shortest=1,scale=384:384:flags=lanczos'
& $ffmpeg -hide_banner -loglevel error -y -framerate $fps -i (Join-Path $dir 'clip-%02d.png') -filter_complex ($filter+',split[a][b];[a]palettegen=stats_mode=full:reserve_transparent=0[p];[b][p]paletteuse=dither=0') -loop 0 (Join-Path $root ($Motion+'.gif'))
if($LASTEXITCODE -ne 0){throw 'GIF export failed'}
& $ffmpeg -hide_banner -loglevel error -y -framerate $fps -i (Join-Path $dir 'clip-%02d.png') -filter_complex $filter -c:v apng -plays 0 -f apng (Join-Path $root ($Motion+'-lossless.png'))
if($LASTEXITCODE -ne 0){throw 'APNG export failed'}
& $ffmpeg -v error -i (Join-Path $root ($Motion+'.gif')) -f null -
if($LASTEXITCODE -ne 0){throw 'GIF decode failed'}
& $ffmpeg -v error -i (Join-Path $root ($Motion+'-lossless.png')) -f null -
if($LASTEXITCODE -ne 0){throw 'APNG decode failed'}
if($Motion -eq 'mining'){
 $finish=@($mining.finish)
 for($i=0;$i -lt $finish.Count;$i++){Copy-Item -LiteralPath (Join-Path $dir ($finish[$i]+'.png')) -Destination (Join-Path $dir ('finish-{0:D2}.png' -f $i))}
 $finishFilter='[0:v]trim=end_frame='+$finish.Count+',setpts=PTS-STARTPTS[fg];color=c=0x202428:s=1024x1024:r=6[bg];[bg][fg]overlay=shortest=1,scale=384:384:flags=lanczos'
 & $ffmpeg -hide_banner -loglevel error -y -framerate 6 -i (Join-Path $dir 'finish-%02d.png') -filter_complex ($finishFilter+',split[a][b];[a]palettegen=stats_mode=full:reserve_transparent=0[p];[b][p]paletteuse=dither=0') -loop -1 (Join-Path $root 'mining-finish.gif')
 if($LASTEXITCODE -ne 0){throw 'Mining finish GIF export failed'}
 & $ffmpeg -hide_banner -loglevel error -y -framerate 6 -i (Join-Path $dir 'finish-%02d.png') -filter_complex $finishFilter -c:v apng -plays 1 -f apng (Join-Path $root 'mining-finish-lossless.png')
 if($LASTEXITCODE -ne 0){throw 'Mining finish APNG export failed'}
 foreach($file in @('mining-finish.gif','mining-finish-lossless.png')){
  & $ffmpeg -v error -i (Join-Path $root $file) -f null -
  if($LASTEXITCODE -ne 0){throw ('Decode failed: '+$file)}
 }
}
@{runtimeApplied=$false;motion=$Motion;sheet=$Sheet;sheetSha256=(Get-FileHash -LiteralPath $Sheet).Hash;count=$records.Count;order=$order;previewFps=$fps;frames=$records;note='Whole-cel uniform scaling and translation only. Surface repaint by image tool; original masters preserved. Background-only cleanup retains foreground RGB. Animation cadence is a review preview, not runtime.'} | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $root ($Motion+'-sequence.json')) -Encoding utf8
$checks | Set-Content (Join-Path $root ($Motion+'-verification.txt')) -Encoding utf8
Write-Output ($Motion+': '+$records.Count+' cels, GIF/APNG decode passed')
