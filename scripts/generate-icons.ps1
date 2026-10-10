param([Parameter(Mandatory)][string]$Destination)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Force $Destination | Out-Null
function Draw-Mark([int]$Size, [bool]$White, [bool]$Badge) {
    $bitmap=[Drawing.Bitmap]::new($Size,$Size)
    $g=[Drawing.Graphics]::FromImage($bitmap)
    $g.Clear([Drawing.Color]::Transparent)
    if ($Badge) { $g.Clear([Drawing.Color]::Black) }
    $ink=if($White -or $Badge){[Drawing.Color]::White}else{[Drawing.Color]::Black}
    $brush=[Drawing.SolidBrush]::new($ink)
    $erase=[Drawing.SolidBrush]::new($(if($Badge){[Drawing.Color]::Black}else{[Drawing.Color]::Transparent}))
    function Rect([double]$X,[double]$Y,[double]$W,[double]$H,[bool]$Hole=$false) {
        $x1=[int][Math]::Round($Size*$X/16)
        $y1=[int][Math]::Round($Size*$Y/16)
        $x2=[int][Math]::Round($Size*($X+$W)/16)
        $y2=[int][Math]::Round($Size*($Y+$H)/16)
        if($Hole) {
            $g.CompositingMode=[Drawing.Drawing2D.CompositingMode]::SourceCopy
            $g.FillRectangle($erase,$x1,$y1,$x2-$x1,$y2-$y1)
            $g.CompositingMode=[Drawing.Drawing2D.CompositingMode]::SourceOver
        }else{$g.FillRectangle($brush,$x1,$y1,$x2-$x1,$y2-$y1)}
    }
    try {
        Rect 1 1 10 2
        Rect 1 3 2 10
        Rect 3 11 2 2
        Rect 9 3 2 1
        Rect 5 4 10 11
        Rect 7 8 6 1 $true
        Rect 7 11 6 1 $true
    }finally{$erase.Dispose();$brush.Dispose();$g.Dispose()}
    return $bitmap
}
function Save-Ico([string]$Path,[bool]$White,[bool]$Badge) {
    $sizes=@(16,20,24,32,40,48,64,128,256)
    $images=@()
    foreach ($size in $sizes) {
        $bitmap=Draw-Mark $size $White $Badge; $memory=[IO.MemoryStream]::new()
        try { $bitmap.Save($memory,[Drawing.Imaging.ImageFormat]::Png); $images+=,@($memory.ToArray()) }
        finally { $memory.Dispose(); $bitmap.Dispose() }
    }
    $file=[IO.File]::Create($Path); $writer=[IO.BinaryWriter]::new($file)
    try {
        $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$sizes.Count)
        $offset=6+16*$sizes.Count
        for ($i=0;$i -lt $sizes.Count;$i++) {
            $dimension=if ($sizes[$i] -eq 256) {0} else {$sizes[$i]}
            $writer.Write([byte]$dimension); $writer.Write([byte]$dimension); $writer.Write([byte]0); $writer.Write([byte]0)
            $writer.Write([uint16]1); $writer.Write([uint16]32); $writer.Write([uint32]$images[$i].Length); $writer.Write([uint32]$offset)
            $offset+=$images[$i].Length
        }
        foreach ($data in $images) { $writer.Write([byte[]]$data) }
    } finally { $writer.Dispose(); $file.Dispose() }
}
Save-Ico (Join-Path $Destination 'menu-light.ico') $false $false
Save-Ico (Join-Path $Destination 'menu-dark.ico') $true $false
Save-Ico (Join-Path $Destination 'PdfZipper.ico') $true $true
foreach ($size in @(16,20,24)) {
    $bitmap=Draw-Mark $size $true $false
    try { $bitmap.Save((Join-Path $Destination "menu-$size.png"),[Drawing.Imaging.ImageFormat]::Png) } finally { $bitmap.Dispose() }
}
foreach ($item in @(@('StoreLogo',50),@('Logo44',44),@('Logo150',150),@('logo-preview',256))) {
    $bitmap=Draw-Mark $item[1] $true $true
    try { $bitmap.Save((Join-Path $Destination ($item[0]+'.png')),[Drawing.Imaging.ImageFormat]::Png) } finally { $bitmap.Dispose() }
}
