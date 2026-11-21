param([switch]$Prepare,[string]$TransitionSheet)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$assemblies=@('System.Drawing.Common','System.Drawing.Primitives','System.Private.Windows.GdiPlus','System.Private.Windows.Core','System.Collections')
Add-Type -Path (Join-Path $PSScriptRoot 'LandingRecoveryReview-20261103.cs') -ReferencedAssemblies $assemblies
Add-Type -Path (Join-Path $PSScriptRoot 'SpriteMotionAlignment-20261103.cs') -ReferencedAssemblies $assemblies
$art=(Resolve-Path (Join-Path $PSScriptRoot '../images/issue266')).Path
$root=Join-Path $art 'female-matte-transition-contact-review-20261104'
New-Item -ItemType Directory -Force -Path $root | Out-Null
$transitionNames=@('idle','entry-01','entry-02','stop-near','stop-far')
$sourcePaths=@('female-motion-alignment-review-20261103/damage/pose-01.png','female-walk-entry-review-20261103/entry-01.png','female-walk-entry-review-20261103/entry-02.png','female-gait-repair-review-20261104/stop-near.png','female-gait-repair-review-20261104/stop-far.png')
if($Prepare){
 $sheet=[System.Drawing.Bitmap]::new(1536,1024);$g=[System.Drawing.Graphics]::FromImage($sheet);$g.Clear([System.Drawing.Color]::Transparent)
 $mapping=@()
 for($i=0;$i -lt 5;$i++){
  $path=Join-Path $art $sourcePaths[$i];$m=[SpriteMotionAlignment]::Measure($path);$im=[System.Drawing.Bitmap]::new($path)
  $s=[Math]::Min(430.0/$m.Bounds.Height,450.0/$m.Bounds.Width);$w=[single]($m.Bounds.Width*$s);$h=[single]($m.Bounds.Height*$s)
  $dest=[System.Drawing.RectangleF]::new([single](($i%3)*512+(512-$w)/2),[single]([Math]::Floor($i/3)*512+480-$h),$w,$h)
  $src=[System.Drawing.RectangleF]::new($m.Bounds.X,$m.Bounds.Y,$m.Bounds.Width,$m.Bounds.Height)
  $g.DrawImage($im,$dest,$src,[System.Drawing.GraphicsUnit]::Pixel);$im.Dispose()
  $mapping+=@{name=$transitionNames[$i];source=$sourcePaths[$i];sha256=(Get-FileHash -LiteralPath $path).Hash;cell=$i}
 }
 $g.Dispose();$sheet.Save((Join-Path $root 'transition-edit-target.png'));$sheet.Dispose()
 $mapping | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $root 'transition-source-map.json') -Encoding utf8
 exit
}
if(-not $TransitionSheet){throw 'Provide transition sheet'}
$groupSpecs=@(
 @{Motion='transition';Names=$transitionNames;Sheet=$TransitionSheet},
 @{Motion='walk';Names=@('key-01','middle-01','key-02','middle-02','key-03','middle-03','key-04','middle-04');Sheet=(Join-Path $art 'female-walk-matte-sheet-v1-20261104.png')},
 @{Motion='ladder';Names=@('pose-01','pose-02','pose-03','pose-04','pose-05');Sheet=(Join-Path $art 'female-ladder-matte-sheet-v1-20261104.png')},
 @{Motion='mining';Names=@('key-01','middle-01','key-02','middle-02','key-03','key-04','middle-04','middle-05','key-05','middle-06','key-06');Sheet=(Join-Path $art 'female-mining-matte-sheet-v1-20261104.png')}
)
$records=@();$checks=@()
foreach($spec in $groupSpecs){
 $dir=Join-Path $root $spec.Motion;New-Item -ItemType Directory -Force -Path $dir | Out-Null
 $sheetPath=(Resolve-Path $spec.Sheet).Path;$hash=(Get-FileHash -LiteralPath $sheetPath).Hash
 $im=[System.Drawing.Bitmap]::new($sheetPath);$rows=[int][Math]::Ceiling($spec.Names.Count/3.0)
 $cw=[int][Math]::Floor($im.Width/3.0);$ch=[int][Math]::Floor($im.Height/[double]$rows);$size=[Math]::Max($cw,$ch)
 $group=@()
 for($i=0;$i -lt $spec.Names.Count;$i++){
  $name=$spec.Names[$i];$raw=[System.Drawing.Bitmap]::new($size,$size);$g=[System.Drawing.Graphics]::FromImage($raw);$g.Clear([System.Drawing.Color]::Transparent)
  $g.DrawImage($im,[System.Drawing.Rectangle]::new([int](($size-$cw)/2),[int](($size-$ch)/2),$cw,$ch),($i%3)*$cw,[int][Math]::Floor($i/3)*$ch,$cw,$ch,[System.Drawing.GraphicsUnit]::Pixel)
  $g.Dispose();$rawPath=Join-Path $dir ($name+'-raw.png');$raw.Save($rawPath);$raw.Dispose()
  $clean=Join-Path $dir ($name+'-clean.png');$checks+=[LandingRecoveryReview]::CleanCel($rawPath,$clean)
  $m=[SpriteMotionAlignment]::Measure($clean)
  $group+=@{motion=$spec.Motion;name=$name;cell=$i;sourceSheet=[IO.Path]::GetFileName($sheetPath);sourceSha256=$hash;clean=$clean;before=$m}
 }
 $im.Dispose()
 # One fixed scale per motion. Median resists separated orange helmet regions.
 $widths=@($group | ForEach-Object {$_.before.Helmet.Width} | Sort-Object)
 $median=if($widths.Count%2){[double]$widths[[int][Math]::Floor($widths.Count/2)]}else{($widths[$widths.Count/2-1]+$widths[$widths.Count/2])/2.0}
 # Rear view excludes most of the side-mounted lamp from the orange region.
 # Keep the rear-view calibration separate rather than shrinking the entire climber.
 $targetHelmetWidth=if($spec.Motion -eq 'ladder'){183.0}else{161.0}
 $scale=if($spec.Motion -eq 'transition'){611.0/$group[0].before.Bounds.Height}else{$targetHelmetWidth/$median}
 $neutralFootX=[LandingRecoveryReview]::FootSpanCenter($group[0].clean)
 $neutralFootY=$group[0].before.Bounds.Y+$group[0].before.Bounds.Height
 foreach($r in $group){
  $m=$r.before
  if($spec.Motion -eq 'ladder'){$rootX=$neutralFootX;$rootY=$neutralFootY;$method='fixed-neutral-body-axis; lifted feet preserved'}
  elseif($spec.Motion -eq 'mining'){$rootX=[LandingRecoveryReview]::FootSpanCenter($r.clean);$rootY=$m.Bounds.Y+$m.Bounds.Height;$method='shoe-ground-span; drill display excluded'}
  else{
   if(-not $m.Belt){throw ('Belt anchor missing: '+$r.name)}
   $relativeBeltY=($m.Belt.CenterY-$m.Bounds.Y)/[double]$m.Bounds.Height
   if($relativeBeltY -lt .35 -or $relativeBeltY -gt .7){throw ('Belt anchor outside torso: '+$r.name)}
   $rootX=$m.Belt.CenterX;$rootY=$m.Bounds.Y+$m.Bounds.Height;$method='waist-x and supporting shoe ground; whole-cel only'
  }
  $r.scale=$scale;$r.targetHelmetWidth=$targetHelmetWidth;$r.medianHelmetSourceWidth=$median;$r.rootMethod=$method
  $r.scaleMethod=if($spec.Motion -eq 'transition'){'fixed neutral stature 611; orange seam components are not anatomical head width'}else{'motion-specific median orange helmet diagnostic, visually reviewed'}
  $r.dx=512-$rootX*$scale;$r.dy=800-$rootY*$scale
  $r.output=Join-Path $dir ($r.name+'.png');$checks+=[SpriteMotionAlignment]::Render($r.clean,$r.output,$scale,$r.dx,$r.dy)
  $r.after=[SpriteMotionAlignment]::Measure($r.output)
  $r.groundError=($r.after.Bounds.Y+$r.after.Bounds.Height)-800
  if($spec.Motion -ne 'ladder' -and [Math]::Abs($r.groundError)>2){throw ('Ground alignment failed: '+$r.name)}
  if($spec.Motion -in @('walk','transition') -and [Math]::Abs($r.after.Belt.CenterX-512)>2){throw ('Waist alignment failed: '+$r.name)}
 }
 if((Get-FileHash -LiteralPath $sheetPath).Hash -ne $hash){throw 'Source sheet changed'}
 $records+=$group
}
function Contact([object[]]$Items,[string]$Name,[bool]$White){
 $columns=3;$rows=[int][Math]::Ceiling($Items.Count/3.0)
 $card=[System.Drawing.Bitmap]::new(960,$rows*352);$g=[System.Drawing.Graphics]::FromImage($card)
 $g.Clear($(if($White){[System.Drawing.Color]::White}else{[System.Drawing.Color]::FromArgb(32,36,40)}))
 $font=[System.Drawing.Font]::new('Arial',12);$brush=if($White){[System.Drawing.Brushes]::Black}else{[System.Drawing.Brushes]::White}
 $pen=[System.Drawing.Pen]::new([System.Drawing.Color]::Gray,1)
 for($i=0;$i -lt $Items.Count;$i++){
  $r=$Items[$i];$im=[System.Drawing.Bitmap]::new($r.output);$x=($i%3)*320;$y=[int][Math]::Floor($i/3)*352
  $g.DrawImage($im,$x,$y+32,320,320);$im.Dispose();$g.DrawString(($r.motion+'/'+$r.name),$font,$brush,$x+6,$y+7)
  $g.DrawLine($pen,$x,$y+282,$x+320,$y+282)
 }
 $pen.Dispose();$font.Dispose();$g.Dispose();$card.Save((Join-Path $root $Name));$card.Dispose()
}
foreach($motion in @('transition','walk','ladder','mining')){
 $items=@($records | Where-Object {$_.motion -eq $motion})
 Contact $items ($motion+'-contact-white.png') $true
 Contact $items ($motion+'-contact-dark.png') $false
}
$examples=@($records | Where-Object {($_.motion -eq 'transition' -and $_.name -eq 'idle') -or ($_.motion -eq 'walk' -and $_.name -eq 'key-01') -or ($_.motion -eq 'mining' -and $_.name -eq 'key-01') -or ($_.motion -eq 'ladder' -and $_.name -in @('pose-01','pose-03'))})
Contact $examples 'cross-motion-scale.png' $false
$lookup=@{};foreach($r in $records){$lookup[$r.motion+'/'+$r.name]=$r.output}
$clips=@(
 @{Name='entry-walk-stop-near';Fps=8;Order=@('transition/idle','transition/idle','transition/idle','transition/idle','transition/entry-01','transition/entry-02','walk/key-01','walk/middle-01','walk/key-02','walk/middle-02','walk/key-03','walk/middle-03','walk/key-04','walk/middle-04','transition/entry-01','transition/stop-near','transition/idle','transition/idle','transition/idle')},
 @{Name='entry-walk-stop-far';Fps=8;Order=@('transition/idle','transition/idle','transition/entry-01','transition/entry-02','walk/key-01','walk/middle-01','walk/key-02','walk/middle-02','walk/key-03','transition/entry-01','transition/stop-far','transition/idle','transition/idle')},
 @{Name='ladder';Fps=5;Order=@('ladder/pose-01','ladder/pose-02','ladder/pose-03','ladder/pose-02','ladder/pose-01','ladder/pose-04','ladder/pose-05','ladder/pose-04')},
 @{Name='mining';Fps=8;Order=@('mining/key-01','mining/middle-01','mining/key-02','mining/middle-02','mining/key-03','mining/key-04','mining/key-03','mining/middle-02','mining/key-02','mining/middle-01')}
)
$ffmpeg=Join-Path $PSScriptRoot 'video-deps/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
foreach($clip in $clips){
 $clipDir=Join-Path $root ($clip.Name+'-frames');New-Item -ItemType Directory -Force -Path $clipDir | Out-Null
 for($i=0;$i -lt $clip.Order.Count;$i++){Copy-Item -LiteralPath $lookup[$clip.Order[$i]] -Destination (Join-Path $clipDir ('frame-{0:D2}.png' -f $i))}
 $filter='[0:v]trim=end_frame='+$clip.Order.Count+',setpts=PTS-STARTPTS[fg];color=c=0x202428:s=1024x1024:r='+$clip.Fps+'[bg];[bg][fg]overlay=shortest=1,scale=384:384:flags=lanczos'
 & $ffmpeg -hide_banner -loglevel error -y -framerate $clip.Fps -i (Join-Path $clipDir 'frame-%02d.png') -filter_complex ($filter+',split[a][b];[a]palettegen=stats_mode=full:reserve_transparent=0[p];[b][p]paletteuse=dither=0') -loop 0 (Join-Path $root ($clip.Name+'.gif'))
 if($LASTEXITCODE -ne 0){throw 'GIF export failed'}
 & $ffmpeg -hide_banner -loglevel error -y -framerate $clip.Fps -i (Join-Path $clipDir 'frame-%02d.png') -filter_complex $filter -c:v apng -plays 0 -f apng (Join-Path $root ($clip.Name+'-lossless.png'))
 if($LASTEXITCODE -ne 0){throw 'APNG export failed'}
 foreach($suffix in @('.gif','-lossless.png')){& $ffmpeg -v error -i (Join-Path $root ($clip.Name+$suffix)) -f null -;if($LASTEXITCODE -ne 0){throw 'Animation decode failed'}}
}
# Store portable paths and explicit limits; hand/rung contact is not proven by anchors.
foreach($r in $records){$r.clean=$r.clean.Substring($root.Length+1);$r.output=$r.output.Substring($root.Length+1)}
@{runtimeApplied=$false;canvas=1024;groundLine=800;frontMedianHelmetWidth=161;rearMedianHelmetWidth=183;frames=$records;clips=$clips;note='One uniform scale per motion, whole-cel translation only. Front/rear head projections use separate visual calibration. Raised ladder feet preserved. Waist x anchoring excludes held drill display. Ground and helmet metrics are image-space review checks, not runtime collision/IK validation.'} | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $root 'alignment.json') -Encoding utf8
$checks | Set-Content (Join-Path $root 'verification.txt') -Encoding utf8
$records | ForEach-Object {[PSCustomObject]@{motion=$_.motion;name=$_.name;helmet=$_.after.Helmet.Width;height=$_.after.Bounds.Height;groundError=$_.groundError;scale=[Math]::Round($_.scale,5)}} | Format-Table -AutoSize
