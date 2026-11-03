# User explicitly approved code cleanup of background/edges, not body-part editing.
Add-Type -Path (Join-Path $PSScriptRoot 'MiningWhiteMatte-20261103.cs') -ReferencedAssemblies System.Drawing.Common,System.Drawing.Primitives,System.Private.Windows.GdiPlus,System.Private.Windows.Core,System.Collections
$art=Join-Path (Join-Path $PSScriptRoot '..') 'images/issue266'
# Explicit enclosed background regions identified in per-cel white-component reports.
# Eyes/lens/skin/specular highlights are intentionally NOT included.
$keyHoles='1:44,1:49,2:43,2:44,3:33,3:34,4:35,4:36,6:18'
$middleHoles='1:48,1:51,2:54,2:55,3:52,3:54,4:58,4:59,4:62,6:25'
[MiningWhiteMatte]::Process((Join-Path $art 'female-mining-v6-white-20261103.png'),(Join-Path $art 'female-mining-v8-clean-20261103.png'),$keyHoles) | Set-Content (Join-Path $art 'mining-key-alpha-regions-20261103.txt') -Encoding utf8
[MiningWhiteMatte]::Process((Join-Path $art 'female-mining-inbetweens-v2-white-20261103.png'),(Join-Path $art 'female-mining-inbetweens-v3-clean-20261103.png'),$middleHoles) | Set-Content (Join-Path $art 'mining-middle-alpha-regions-20261103.txt') -Encoding utf8
