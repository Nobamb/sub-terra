$ErrorActionPreference='Stop'
Add-Type -Path (Join-Path $PSScriptRoot 'SpriteMotionAlignment-20261103.cs') -ReferencedAssemblies System.Drawing.Common,System.Drawing.Primitives,System.Private.Windows.GdiPlus,System.Private.Windows.Core,System.Collections
$repo=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$art=Join-Path $repo 'work_process/images/issue266'
$dir=Join-Path $art 'female-motion-alignment-review-20261103'
New-Item -ItemType Directory -Force -Path $dir | Out-Null
$groups=@(@{Name='walk';Dir='female-walk-eight-review-20261103';Files=@('key-01','middle-01','key-02','middle-02','key-03','middle-03','key-04','middle-04')},@{Name='jump';Dir='female-jump-review-20261103';Files=@('pose-01','pose-02','pose-03','pose-04')},@{Name='damage';Dir='female-damage-review-20261103';Files=@('pose-01','pose-02','pose-03','pose-04')},@{Name='knockout';Dir='female-knockout-review-20261103';Files=@('pose-01','pose-02','pose-03','pose-04')})
$measurements=@()
foreach($group in $groups){foreach($name in $group.Files){$relative='work_process/images/issue266/'+$group.Dir+'/'+$name+'.png';$measurements+=[pscustomobject]@{motion=$group.Name;frame=$name;source=$relative;measure=[SpriteMotionAlignment]::Measure((Join-Path $repo $relative))}}}
$measurements | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $dir 'measurements.json') -Encoding utf8
$targetHelmet=[double]($measurements | Where-Object {$_.motion -eq 'walk' -and $_.frame -eq 'key-01'}).measure.Helmet.Width
$size=1024;$rootX=512.0;$groundY=800.0;$scales=@{}
foreach($name in @('walk','jump','damage','knockout')){$ref=if($name -eq 'walk'){'key-01'}else{'pose-01'};$reference=$measurements | Where-Object {$_.motion -eq $name -and $_.frame -eq $ref};$scales[$name]=$targetHelmet/[double]$reference.measure.Helmet.Width}
$groundJump=@($measurements | Where-Object {$_.motion -eq 'jump' -and $_.frame -in @('pose-01','pose-04')} | ForEach-Object {$_.measure.Bounds.Y+$_.measure.Bounds.Height} | Measure-Object -Average).Average
$records=@()
foreach($entry in $measurements) {
 $source=Join-Path $repo $entry.source;$hash=(Get-FileHash -LiteralPath $source).Hash
 $motion=$entry.motion;$frame=$entry.frame;$m=$entry.measure;$scale=$scales[$motion]
 if($null -eq $m.Belt){throw "Missing root anchor $motion/$frame"}
 $anchorX=[double]$m.Belt.CenterX;$method='cyan-belt-center'
 # In collapsed poses the cyan detector sees thigh equipment, not the occluded belt.
 # These whole-cel pelvis-axis annotations are review candidates, not joint rig data.
 if($motion -eq 'knockout' -and $frame -eq 'pose-03'){$anchorX=330.0;$method='manual-pelvis-axis-review'}
 if($motion -eq 'knockout' -and $frame -eq 'pose-04'){$anchorX=290.0;$method='manual-pelvis-axis-review'}
 $grounded=-not ($motion -eq 'jump' -and $frame -in @('pose-02','pose-03'))
 $anchorY=if($grounded){[double]($m.Bounds.Y+$m.Bounds.Height)}else{[double]$groundJump}
 $dx=$rootX-$anchorX*$scale;$dy=$groundY-$anchorY*$scale
 $outFolder=Join-Path $dir $motion;New-Item -ItemType Directory -Force -Path $outFolder | Out-Null
 $output=Join-Path $outFolder ($frame+'.png')
 $null=[SpriteMotionAlignment]::Render($source,$output,$scale,$dx,$dy,$size)
 if((Get-FileHash -LiteralPath $source).Hash -ne $hash){throw 'Source image changed'}
 $after=[SpriteMotionAlignment]::Measure($output)
 if($grounded -and [Math]::Abs(($after.Bounds.Y+$after.Bounds.Height)-$groundY) -gt 2){throw "Ground alignment failed $motion/$frame"}
 $records+=[pscustomobject]@{motion=$motion;frame=$frame;source=$entry.source;sourceSha256=$hash;output=('work_process/images/issue266/female-motion-alignment-review-20261103/'+$motion+'/'+$frame+'.png');scale=$scale;dx=$dx;dy=$dy;sourceRootX=$anchorX;sourceRootY=$anchorY;rootMethod=$method;grounded=$grounded;sourceMeasure=$m;alignedMeasure=$after}
}
$walkRef=$records | Where-Object {$_.motion -eq 'walk' -and $_.frame -eq 'key-01'}
$malePath=Join-Path $repo 'sub-terra/Assets/_Project/Art/Characters/Player/Frames/Walk/walk_01.png'
$maleMeasure=[SpriteMotionAlignment]::Measure($malePath)
$maleMeta=Get-Content ($malePath+'.meta') -Raw
$malePpu=[double]([regex]::Match($maleMeta,'spritePixelsToUnits:\s*([\d.]+)').Groups[1].Value)
$candidatePpu=$walkRef.alignedMeasure.Bounds.Height/($maleMeasure.Bounds.Height/$malePpu)
$maleTexture=[System.Drawing.Bitmap]::new($malePath);$maleWidth=$maleTexture.Width;$maleHeight=$maleTexture.Height;$maleTexture.Dispose()
$pivotMatch=[regex]::Match($maleMeta,'spritePivot:\s*\{x:\s*([\d.]+),\s*y:\s*([\d.]+)\}')
$malePivotX=[double]$pivotMatch.Groups[1].Value;$malePivotY=[double]$pivotMatch.Groups[2].Value
$maleFootLocalY=($maleHeight-($maleMeasure.Bounds.Y+$maleMeasure.Bounds.Height)-$malePivotY*$maleHeight)/$malePpu
$maleBeltLocalX=($maleMeasure.Belt.CenterX-$malePivotX*$maleWidth)/$malePpu
$preservedPivotX=($rootX-$maleBeltLocalX*$candidatePpu)/$size
$preservedPivotY=($size-$groundY-$maleFootLocalY*$candidatePpu)/$size
$manifest=[pscustomobject]@{runtimeApplied=$false;canvasSize=$size;reviewRootTopLeft=@($rootX,$groundY);reviewFootPivot=@(($rootX/$size),(1-$groundY/$size));candidatePreserveExistingVisualPivot=@($preservedPivotX,$preservedPivotY);candidateSpriteAlignment=9;candidatePixelsPerUnit=$candidatePpu;referenceMaleSource='sub-terra/Assets/_Project/Art/Characters/Player/Frames/Walk/walk_01.png';referenceMalePpu=$malePpu;referenceMaleTextureSize=@($maleWidth,$maleHeight);referenceMalePivot=@($malePivotX,$malePivotY);referenceMaleFootLocalY=$maleFootLocalY;referenceMaleBeltLocalX=$maleBeltLocalX;referenceMaleOpaqueHeight=$maleMeasure.Bounds.Height;helmetReferenceOrangeRegionWidth=$targetHelmet;fixedScalePerMotion=$scales;idleReference='damage/pose-01.png';note='Whole-cel uniform scale and translation review only. One fixed scale per motion; no anatomy changes. Airborne poses are not floor-snapped. reviewFootPivot is NOT a drop-in Unity pivot: preserve-existing candidate compensates for current male foot/waist offsets. PPU/pivot are NOT applied to Unity.';frames=$records}
$manifest | ConvertTo-Json -Depth 12 | Set-Content (Join-Path $dir 'alignment.json') -Encoding utf8
$records | Select-Object motion,frame,scale,dx,dy,sourceRootX,sourceRootY,grounded,rootMethod | Export-Csv (Join-Path $dir 'alignment.csv') -NoTypeInformation -Encoding utf8

function Render-Card([string]$Source,[string]$Output,[string]$Label,[bool]$Before=$false) {
 $out=[System.Drawing.Bitmap]::new(384,416);$g=[System.Drawing.Graphics]::FromImage($out)
 $g.Clear([System.Drawing.Color]::FromArgb(32,36,40));$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
 $im=[System.Drawing.Bitmap]::new($Source)
 if($Before){$offset=(1024-$im.Width)/2.0;$g.DrawImage($im,[single]($offset*.375),[single](32+$offset*.375),[single]($im.Width*.375),[single]($im.Height*.375))}
 else{$g.DrawImage($im,0,32,384,384)}
 $im.Dispose()
 $pen=[System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(155,150,150,150),1)
 $g.DrawLine($pen,0,332,384,332);$g.DrawLine($pen,192,326,192,338);$pen.Dispose()
 $font=[System.Drawing.Font]::new('Malgun Gothic',14)
 $g.DrawString($Label,$font,[System.Drawing.Brushes]::White,8,5);$font.Dispose()
 $g.Dispose();$out.Save($Output,[System.Drawing.Imaging.ImageFormat]::Png);$out.Dispose()
}
$order=@('key-01','middle-01','key-02','middle-02','key-03','middle-03','key-04','middle-04')
for($i=0;$i -lt $order.Count;$i++) {
 $r=$records | Where-Object {$_.motion -eq 'walk' -and $_.frame -eq $order[$i]}
 $before=Join-Path $dir 'before-card.png';$after=Join-Path $dir 'after-card.png'
 Render-Card (Join-Path $repo $r.source) $before '정렬 전 · 셀 중앙 배치 가정' $true
 Render-Card (Join-Path $repo $r.output) $after '정렬 후 · 동일한 발 기준'
 $out=[System.Drawing.Bitmap]::new(768,416);$g=[System.Drawing.Graphics]::FromImage($out)
 foreach($item in @(@{Path=$before;X=0},@{Path=$after;X=384})){$im=[System.Drawing.Bitmap]::new($item.Path);$g.DrawImageUnscaled($im,$item.X,0);$im.Dispose()}
 $g.Dispose();$out.Save((Join-Path $dir ('walk-compare-{0:D2}.png' -f ($i+1))));$out.Dispose()
}
$transitions=@(@{Motion='damage';Frame='pose-01';Label='대기 참고'},@{Motion='damage';Frame='pose-01';Label='대기 참고'})
foreach($name in $order){$transitions+=@{Motion='walk';Frame=$name;Label='걷기 · 자세 연결'}}
foreach($pair in @(@('pose-01','점프 준비'),@('pose-02','상승 자세'),@('pose-03','하강 자세'),@('pose-04','착지 자세'))){$transitions+=@{Motion='jump';Frame=$pair[0];Label=$pair[1]+' · 이동 궤적 미적용'}}
$transitions+=@{Motion='damage';Frame='pose-01';Label='대기 참고'};$transitions+=@{Motion='damage';Frame='pose-01';Label='대기 참고'}
for($i=0;$i -lt $transitions.Count;$i++) {
 $t=$transitions[$i];$r=$records | Where-Object {$_.motion -eq $t.Motion -and $_.frame -eq $t.Frame}
 Render-Card (Join-Path $repo $r.output) (Join-Path $dir ('transition-{0:D2}.png' -f ($i+1))) $t.Label
}
# Snapshot compares grounded states and the final collapsed pose at exactly the same review zoom.
$samples=@(@{Motion='walk';Frame='key-01';Label='걷기'},@{Motion='jump';Frame='pose-01';Label='점프 준비'},@{Motion='damage';Frame='pose-01';Label='대기 참고'},@{Motion='knockout';Frame='pose-01';Label='쓰러짐 시작'},@{Motion='knockout';Frame='pose-04';Label='쓰러짐 끝'})
$contact=[System.Drawing.Bitmap]::new(1920,832);$cg=[System.Drawing.Graphics]::FromImage($contact)
for($i=0;$i -lt $samples.Count;$i++){
 $s=$samples[$i];$r=$records | Where-Object {$_.motion -eq $s.Motion -and $_.frame -eq $s.Frame}
 foreach($before in @($true,$false)){
  $card=Join-Path $dir 'overview-card.png';$row=if($before){0}else{1};$path=if($before){Join-Path $repo $r.source}else{Join-Path $repo $r.output}
  $prefix=if($before){'전 · '}else{'후 · '};Render-Card $path $card ($prefix+$s.Label) $before
  $im=[System.Drawing.Bitmap]::new($card);$cg.DrawImageUnscaled($im,$i*384,$row*416);$im.Dispose()
 }
}
$cg.Dispose();$contact.Save((Join-Path $dir 'ground-before-after.png'));$contact.Dispose()
$ffmpeg=Join-Path $PSScriptRoot 'video-deps/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
foreach($clip in @(@{Name='walk-compare';Count=8},@{Name='transition';Count=$transitions.Count})) {
 $input=Join-Path $dir ($clip.Name+'-%02d.png')
 $trim='[0:v]trim=end_frame='+$clip.Count+',setpts=PTS-STARTPTS'
 & $ffmpeg -hide_banner -loglevel error -y -framerate 5 -i $input -filter_complex ($trim+',split[a][b];[a]palettegen=stats_mode=full:reserve_transparent=0[p];[b][p]paletteuse=dither=0') -loop 0 (Join-Path $dir ($clip.Name+'.gif'))
 if($LASTEXITCODE -ne 0){throw 'GIF generation failed'}
 & $ffmpeg -hide_banner -loglevel error -y -framerate 5 -i $input -filter_complex $trim -c:v apng -plays 0 -f apng (Join-Path $dir ($clip.Name+'-lossless.png'))
 if($LASTEXITCODE -ne 0){throw 'APNG generation failed'}
}
@('sourceImagesUnchanged=True','fixedScalePerMotion=True',('alignedCels='+$records.Count),'groundedBottomTolerancePixels=2','clippedCels=0',('candidatePixelsPerUnit='+$candidatePpu),('preserveExistingVisualPivot='+$preservedPivotX+','+$preservedPivotY),'runtimeApplied=False') | Set-Content (Join-Path $dir 'verification.txt') -Encoding utf8
