[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Package,
    [Parameter(Mandatory)][string]$CertificateThumbprint,
    [string]$TimestampUrl
)
$ErrorActionPreference = 'Stop'
$file = (Resolve-Path -LiteralPath $Package).Path
if ([IO.Path]::GetExtension($file) -ne '.msix') { throw 'Bitte ein MSIX-Paket angeben.' }
$certificate = Get-Item -LiteralPath "Cert:/CurrentUser/My/$CertificateThumbprint"
if (!$certificate.HasPrivateKey) { throw 'Das Zertifikat benötigt einen privaten Schlüssel.' }
$sdkRoot = "${env:ProgramFiles(x86)}/Windows Kits/10/bin"
$tool = Get-ChildItem "$sdkRoot/*/x64/signtool.exe" | Sort-Object FullName -Descending | Select-Object -First 1
if (!$tool) { throw 'Windows SDK mit signtool.exe fehlt.' }
$arguments = @('sign', '/fd', 'SHA256', '/sha1', $CertificateThumbprint, '/s', 'My')
if ($TimestampUrl) { $arguments += @('/tr', $TimestampUrl, '/td', 'SHA256') }
$arguments += $file
& $tool.FullName @arguments
if ($LASTEXITCODE -ne 0) { throw 'Signierung fehlgeschlagen. Publisher im Paket und Zertifikat müssen übereinstimmen.' }
& $tool.FullName verify /pa /v $file
if ($LASTEXITCODE -ne 0) { throw 'Signaturprüfung fehlgeschlagen. Vertrauen des Zertifikats auf dem Zielgerät prüfen.' }
Get-FileHash -LiteralPath $file -Algorithm SHA256 | Format-List
