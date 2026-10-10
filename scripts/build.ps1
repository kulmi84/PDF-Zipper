[CmdletBinding()]
param([string]$Version = '0.1.0.0', [string]$Publisher = 'CN=kulmi84')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$stage = Join-Path $root 'artifacts/package'
$native = Join-Path $root 'artifacts/native'
if (Test-Path $stage) { throw 'artifacts/package existiert bereits. Für einen neuen Build einen frischen Checkout verwenden oder diesen Build-Ordner vorher entfernen.' }
New-Item -ItemType Directory -Force $stage | Out-Null
function Run([string]$Program, [string[]]$Arguments) {
    & $Program @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Program fehlgeschlagen: $LASTEXITCODE" }
}
Run 'dotnet' @('run', '--project', "$root/tests/PdfZipper.Tests.csproj", '-c', 'Release')
Run 'dotnet' @('publish', "$root/src/PdfZipper/PdfZipper.csproj", '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true', '-p:PublishSingleFile=true', '-p:IncludeNativeLibrariesForSelfExtract=true', '-p:DebugType=None', '-o', $stage)
Run 'cmake' @('-S', "$root/native", '-B', $native, '-A', 'x64')
Run 'cmake' @('--build', $native, '--config', 'Release')
Copy-Item -LiteralPath "$native/Release/PdfZipperShell.dll" -Destination $stage
[xml]$manifest = Get-Content -Raw -LiteralPath "$root/packaging/AppxManifest.xml"
$manifest.Package.Identity.Version = $Version
$manifest.Package.Identity.Publisher = $Publisher
$manifest.Save("$stage/AppxManifest.xml")
Add-Type -AssemblyName System.Drawing
$assets = New-Item -ItemType Directory -Force "$stage/Assets"
foreach ($item in @(@('StoreLogo',50), @('Logo44',44), @('Logo150',150))) {
    $size = [int]$item[1]
    $bitmap = [System.Drawing.Bitmap]::new($size, $size)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.Clear([System.Drawing.Color]::FromArgb(24, 55, 84))
        $pen = [System.Drawing.Pen]::new([System.Drawing.Color]::White, [single]($size / 10))
        try {
            $graphics.DrawLine($pen, [single]($size*.35), [single]($size*.2), [single]($size*.35), [single]($size*.8))
            $graphics.DrawLine($pen, [single]($size*.65), [single]($size*.2), [single]($size*.65), [single]($size*.8))
            $graphics.DrawLine($pen, [single]($size*.35), [single]($size*.5), [single]($size*.65), [single]($size*.5))
        } finally { $pen.Dispose() }
        $bitmap.Save("$assets/$($item[0]).png", [System.Drawing.Imaging.ImageFormat]::Png)
    } finally { $graphics.Dispose(); $bitmap.Dispose() }
}
Copy-Item "$root/LICENSE", "$root/THIRD-PARTY-NOTICES.md" -Destination $stage
$sdkRoot = "${env:ProgramFiles(x86)}/Windows Kits/10/bin"
$makeappx = Get-ChildItem "$sdkRoot/*/x64/makeappx.exe" | Sort-Object FullName -Descending | Select-Object -First 1
if (!$makeappx) { throw 'Windows SDK mit makeappx.exe fehlt.' }
Run $makeappx.FullName @('pack', '/d', $stage, '/p', "$root/artifacts/PDF-Zipper-$Version-x64.msix", '/o')
Get-FileHash "$root/artifacts/PDF-Zipper-$Version-x64.msix" -Algorithm SHA256 | Format-List
