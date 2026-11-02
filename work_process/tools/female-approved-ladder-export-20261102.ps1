param(
 [string]$SourceDirectory='work_process/images/issue266/female-original-outfit-ladder-v1-review',
 [string]$OutputDirectory='work_process/images/issue266/approved-ladder-whole-cels'
)
# Whole-body cel normalization only. No rig parts or body-part transforms.
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing.Common,System.Drawing.Primitives -TypeDefinition @'
using System; using System.Drawing;
public static class ApprovedLadderBounds {
 public static Rectangle Measure(Bitmap im) {
  int l=im.Width,t=im.Height,r=-1,b=-1;
  for(int y=0;y<im.Height;y++) for(int x=0;x<im.Width;x++) if(im.GetPixel(x,y).A>12) {
   l=Math.Min(l,x);r=Math.Max(r,x);t=Math.Min(t,y);b=Math.Max(b,y);
  }
  if(r<l)throw new InvalidOperationException("Empty cel");
  return new Rectangle(l,t,r-l+1,b-t+1);
 }
}
'@
$source=Join-Path $PWD $SourceDirectory
$target=Join-Path $PWD $OutputDirectory
New-Item -ItemType Directory -Path $target -Force | Out-Null
$female=[System.Drawing.Bitmap]::new((Join-Path $source 'pose-01.png'))
$male=[System.Drawing.Bitmap]::new((Join-Path $PWD 'sub-terra/Assets/_Project/Art/Characters/Player/Frames/LadderBack/ladder_back_neutral_v2.png'))
$fb=[ApprovedLadderBounds]::Measure($female);$mb=[ApprovedLadderBounds]::Measure($male)
$scale=$mb.Height/[double]$fb.Height
$dx=$mb.Left+$mb.Width/2.0-($fb.Left+$fb.Width/2.0)*$scale
$dy=$mb.Top-$fb.Top*$scale
$width=$male.Width;$height=$male.Height
$female.Dispose();$male.Dispose()
$records=@()
for($i=1;$i -le 5;$i++) {
 $path=Join-Path $source ('pose-{0:D2}.png' -f $i)
 $im=[System.Drawing.Bitmap]::new($path)
 $out=[System.Drawing.Bitmap]::new($width,$height)
 $g=[System.Drawing.Graphics]::FromImage($out)
 $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
 $g.DrawImage($im,[single]$dx,[single]$dy,[single]($im.Width*$scale),[single]($im.Height*$scale))
 $bounds=[ApprovedLadderBounds]::Measure($out)
 if($bounds.Left -le 0 -or $bounds.Top -le 0 -or $bounds.Right -ge $width -or $bounds.Bottom -ge $height){throw "Export clipped pose $i"}
 $out.Save((Join-Path $target ('ladder_{0:D2}.png' -f $i)),[System.Drawing.Imaging.ImageFormat]::Png)
 $records+=[pscustomobject]@{pose=$i;source=$path.Substring($PWD.Path.Length+1);sourceSha256=(Get-FileHash -LiteralPath $path).Hash;scale=$scale;dx=$dx;dy=$dy;width=$width;height=$height;bounds=$bounds}
 $g.Dispose();$out.Dispose();$im.Dispose()
}
$records | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $target 'export.json') -Encoding utf8
Copy-Item -LiteralPath (Join-Path $source 'ladder-review.gif') -Destination (Join-Path $target 'approved-motion-reference.gif')
Write-Output "5 approved whole cels exported ${width}x${height}, fixed scale=$scale. Original preview copied unchanged."
