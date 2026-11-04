param([string]$ReviewFolder='issue266-distance-ground-20261104')
$ErrorActionPreference='Stop'
$dir=(Resolve-Path (Join-Path $PSScriptRoot ('../'+$ReviewFolder))).Path
$rows=@(Import-Csv (Join-Path $dir 'movement-trace.csv'))
$stats=@()
foreach($mode in @('time-original','distance-ground','mine')) {
 $part=@($rows | Where-Object {$_.mode -eq $mode})
 $grounded=@($part | Where-Object {$_.grounded -eq 'True' -and $_.pose -ne '' -and $_.groundY -ne 'NaN'})
 $gaps=@($grounded | ForEach-Object {[Math]::Abs([double]$_.soleY-[double]$_.groundY)})
 $maleGaps=@($grounded | ForEach-Object {[Math]::Abs([double]$_.maleIdleFootY-[double]$_.groundY)})
 $blocked=@($part | Where-Object {$_.grounded -eq 'True' -and [double]$_.speed -le 0.02 -and $_.pose -ne ''})
 $badBlocked=@($blocked | Where-Object {$_.pose -like 'walk/*' -or $_.pose -like 'entry/*'})
 $drifts=@()
 for($i=1;$i -lt $part.Count;$i++){
  $a=$part[$i-1];$b=$part[$i]
  if($a.pose -like 'walk/*' -and $a.pose -eq $b.pose -and $a.grounded -eq 'True' -and $b.grounded -eq 'True'){
   $drifts += [Math]::Abs([double]$b.soleX-[double]$a.soleX)
  }
 }
 $stats+=[pscustomobject]@{mode=$mode;samples=$part.Count;groundedSamples=$grounded.Count;maxGroundGap=($gaps | Measure-Object -Maximum).Maximum;maxMaleIdleReferenceGap=($maleGaps | Measure-Object -Maximum).Maximum;stationarySamples=$blocked.Count;stationaryMovingPoses=$badBlocked.Count;heldWalkPairs=$drifts.Count;maxHeldSoleDrift=($drifts | Measure-Object -Maximum).Maximum;maxSpeed=($part | Measure-Object speed -Maximum).Maximum}
}
$stats | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $dir 'comparison.json') -Encoding utf8
Add-Type -AssemblyName System.Drawing
foreach($mode in @('time-original','distance-ground')){
 if(-not (Test-Path (Join-Path $dir ($mode+'-00.png')))){continue}
 $contact=[System.Drawing.Bitmap]::new(960,576);$g=[System.Drawing.Graphics]::FromImage($contact)
 for($i=0;$i -lt 9;$i++){
  $im=[System.Drawing.Bitmap]::new((Join-Path $dir ('{0}-{1:D2}.png' -f $mode,$i)))
  $g.DrawImage($im,($i%3)*320,[int][Math]::Floor($i/3)*192,320,192);$im.Dispose()
 }
 $g.Dispose();$contact.Save((Join-Path $dir ($mode+'-contact.png')));$contact.Dispose()
}
$ffmpeg=Join-Path $PSScriptRoot 'video-deps/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
foreach($mode in @('time-original','distance-ground','mine')){
 $part=@($rows | Where-Object {$_.mode -eq $mode -and $_.pose -ne ''})
 if($part.Count -eq 0){continue}
 $concat=@()
 for($i=0;$i -lt $part.Count;$i++){
  $concat+="file '$($part[$i].image)'"
  $duration=if($i -lt $part.Count-1){[Math]::Max(0.01,([double]$part[$i+1].time-[double]$part[$i].time))}else{0.1}
  $concat+='duration '+$duration.ToString('F5',[Globalization.CultureInfo]::InvariantCulture)
 }
 $concat+="file '$($part[-1].image)'"
 $input=Join-Path $dir ($mode+'.ffconcat');$concat | Set-Content $input -Encoding utf8
 & $ffmpeg -hide_banner -loglevel error -y -f concat -safe 0 -i $input -filter_complex '[0:v]split[a][b];[a]palettegen=stats_mode=full:reserve_transparent=0[p];[b][p]paletteuse=dither=0' -fps_mode vfr -loop -1 (Join-Path $dir ($mode+'.gif'))
 if($LASTEXITCODE -ne 0){throw 'GIF failed'}
}
$stats | Format-List
