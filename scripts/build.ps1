[CmdletBinding()]
param([string]$Version = '0.1.10.0', [string]$Publisher = 'CN=kulmi84')
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
Run 'ctest' @('--test-dir', $native, '-C', 'Release', '--output-on-failure')
Copy-Item -LiteralPath "$native/Release/PdfZipperShell.dll" -Destination $stage
[xml]$manifest = Get-Content -Raw -LiteralPath "$root/packaging/AppxManifest.xml"
$manifest.Package.Identity.Version = $Version
$manifest.Package.Identity.Publisher = $Publisher
$manifest.Save("$stage/AppxManifest.xml")
& "$root/scripts/generate-icons.ps1" -Destination "$stage/Assets"
Copy-Item "$root/LICENSE", "$root/THIRD-PARTY-NOTICES.md" -Destination $stage
$licenses = New-Item -ItemType Directory -Force "$stage/licenses"
Copy-Item "$root/licenses/*" -Destination $licenses
$dependencyManifest = Get-Content -Raw "$root/src/PdfZipper/obj/project.assets.json" | ConvertFrom-Json
foreach ($library in $dependencyManifest.libraries.PSObject.Properties) {
    foreach ($folder in $dependencyManifest.packageFolders.PSObject.Properties.Name) {
        $package = Join-Path $folder $library.Value.path
        if (Test-Path $package) {
            foreach ($notice in Get-ChildItem -LiteralPath $package -Recurse -File | Where-Object { $_.Name -match '^(LICENSE|THIRD.?PARTY.?NOTICES)' }) {
                $destination = Join-Path $licenses ($library.Name.Replace('/', '-') + '-' + $notice.Name)
                Copy-Item -LiteralPath $notice.FullName -Destination $destination
            }
        }
    }
}
$sdkRoot = "${env:ProgramFiles(x86)}/Windows Kits/10/bin"
$makeappx = Get-ChildItem "$sdkRoot/*/x64/makeappx.exe" | Sort-Object FullName -Descending | Select-Object -First 1
if (!$makeappx) { throw 'Windows SDK mit makeappx.exe fehlt.' }
Run $makeappx.FullName @('pack', '/d', $stage, '/p', "$root/artifacts/PDF-Zipper-$Version-x64.msix", '/o')
Get-FileHash "$root/artifacts/PDF-Zipper-$Version-x64.msix" -Algorithm SHA256 | Format-List




