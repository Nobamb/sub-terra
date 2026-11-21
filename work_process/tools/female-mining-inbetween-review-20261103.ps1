param(
 [string]$Keys='female-mining-v8-clean-20261103.png',
 [string]$Middles='female-mining-inbetweens-v3-clean-20261103.png'
)
# Crops and reorders WHOLE redraw cels. No blending, limb transforms, or rigging.
Add-Type -AssemblyName System.Drawing
$root=Join-Path $PSScriptRoot '..'
$art=Join-Path $root 'images/issue266'
$folder=Join-Path $art 'female-mining-inbetween-review-20261103'
New-Item -ItemType Directory -Force -Path $folder | Out-Null
foreach($entry in @(@{Name='key';File=$Keys},@{Name='middle';File=$Middles})) {
 $sheet=[System.Drawing.Bitmap]::new((Join-Path $art $entry.File))
 if($sheet.Width -ne 1536 -or $sheet.Height -ne 1024){throw 'Expected 1536x1024 six-cel sheet'}
 for($i=0;$i -lt 6;$i++) {
  $r=[System.Drawing.Rectangle]::new(($i%3)*512,[int][Math]::Floor($i/3)*512,512,512)
  $cel=$sheet.Clone($r,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
  $cel.Save((Join-Path $folder ('{0}-{1:D2}.png' -f $entry.Name,($i+1))),[System.Drawing.Imaging.ImageFormat]::Png)
  $cel.Dispose()
 }
 $sheet.Dispose()
}
# middle-03 is rejected: excessive knee flexion introduces an unwanted body-height dip.
$work=@('key-01','middle-01','key-02','middle-02','key-03','key-04','key-03','middle-02','key-02','middle-01')
$finish=@('key-04','middle-04','middle-05','key-05','middle-06','key-06','key-06','key-06')
$order=@('key-01','middle-01','key-02','middle-02','key-03','key-04','middle-04','middle-05','key-05','middle-06','key-06')
$ffmpeg=Join-Path $PSScriptRoot 'video-deps/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
foreach($clip in @(@{Name='work';Frames=$work;Fps=8},@{Name='finish';Frames=$finish;Fps=6})) {
 for($i=0;$i -lt $clip.Frames.Count;$i++) {
  Copy-Item -LiteralPath (Join-Path $folder ($clip.Frames[$i]+'.png')) -Destination (Join-Path $folder ('{0}-{1:D2}.png' -f $clip.Name,($i+1)))
 }
 $base=('[0:v]trim=end_frame={1},setpts=PTS-STARTPTS[fg];color=c=0x202428:s=512x512:r={0}[bg];[bg][fg]overlay=shortest=1,scale=384:384:flags=lanczos' -f $clip.Fps,$clip.Frames.Count)
 $input=Join-Path $folder ($clip.Name+'-%02d.png')
 $gifLoop=if($clip.Name -eq 'finish'){-1}else{0}
 $pngPlays=if($clip.Name -eq 'finish'){1}else{0}
 & $ffmpeg -hide_banner -loglevel error -y -framerate $clip.Fps -i $input -filter_complex ($base+',split[a][b];[a]palettegen=stats_mode=full:reserve_transparent=0[p];[b][p]paletteuse=dither=0') -loop $gifLoop (Join-Path $folder ($clip.Name+'.gif'))
 if($LASTEXITCODE -ne 0){throw 'GIF failed'}
 & $ffmpeg -hide_banner -loglevel error -y -framerate $clip.Fps -i $input -filter_complex $base -c:v apng -plays $pngPlays -f apng (Join-Path $folder ($clip.Name+'-lossless.png'))
 if($LASTEXITCODE -ne 0){throw 'APNG failed'}
}
# White and black contact sheets for 11 selected full redraw cels; last slot stays blank.
foreach($bg in @('White','Black')) {
 $contact=[System.Drawing.Bitmap]::new(1024,768)
 $g=[System.Drawing.Graphics]::FromImage($contact)
 $g.Clear([System.Drawing.Color]::$bg)
 for($i=0;$i -lt $order.Count;$i++) {
  $im=[System.Drawing.Bitmap]::new((Join-Path $folder ($order[$i]+'.png')))
  $g.DrawImage($im,($i%4)*256,[int][Math]::Floor($i/4)*256,256,256)
  $im.Dispose()
 }
 $g.Dispose()
 $contact.Save((Join-Path $folder ('contact-'+$bg.ToLower()+'.png')),[System.Drawing.Imaging.ImageFormat]::Png)
 $contact.Dispose()
}
@{work=$work;finish=$finish;uniqueCels=$order;excludedCels=@('middle-03');workPreviewFps=8;finishPreviewFps=6;runtimeApplied=$false;note='Finish clip plays once and holds. Preview speed is not runtime timing. middle-03 rejected for excessive crouch.'} | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $folder 'sequence.json') -Encoding utf8
