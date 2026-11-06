Add-Type -AssemblyName System.Drawing
$root='sub-terra/Assets/_Project/Art/Characters/Player/Frames/'
$groups=@(
    @{Name='walk';Dir='Walk';Prefix='walk';Frames=@(1,11,21,31);Columns=2},
    @{Name='ladder';Dir='LadderBack';Prefix='ladder_back';Frames=@(1,2,3,4,5);Columns=3},
    @{Name='mining';Dir='Mining';Prefix='mining';Frames=@(1,5,9,13,17,24);Columns=3}
)
$records=@()
foreach($group in $groups) {
    $cols=$group.Columns;$rows=[int][Math]::Ceiling($group.Frames.Count/$cols)
    $sheet=[System.Drawing.Bitmap]::new($cols*512,$rows*512)
    $g=[System.Drawing.Graphics]::FromImage($sheet)
    $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    for($i=0;$i -lt $group.Frames.Count;$i++) {
        $path=$root+$group.Dir+'/'+$group.Prefix+'_'+('{0:D2}' -f $group.Frames[$i])+'.png'
        if($group.Name -eq 'ladder') {
            $name=if($i -eq 0){'ladder_back_neutral_v2.png'}else{'ladder_back_style_'+('{0:D2}' -f ($i+1))+'.png'}
            $path=$root+$group.Dir+'/'+$name
        }
        $im=[System.Drawing.Bitmap]::new((Join-Path $PWD $path))
        $g.DrawImage($im,($i%$cols)*512,[int][Math]::Floor($i/$cols)*512,512,512)
        $im.Dispose()
        $records+=[pscustomobject]@{group=$group.Name;cell=$i+1;source=$path}
    }
    $sheet.Save((Join-Path $PWD ('work_process/images/issue266/male-'+$group.Name+'-original-outfit-reference.png')),[System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose();$sheet.Dispose()
}
$records | ConvertTo-Json | Set-Content work_process/images/issue266/original-outfit-source-cells.json -Encoding utf8
