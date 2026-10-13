param([switch]$TrustOnly)
$ErrorActionPreference = 'Stop'
$expected = '013012837D1FF2119EE33A45A33E0ABDF52AAFB3'
$package = Join-Path $PSScriptRoot 'PDF-Zipper-0.1.16.0-x64-test.msix'
if ((Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash -ne 'DDF4D219049E92492CFD4D4A81F11782CF25A844229916E71D339D9C9A1B7707') {
    throw 'Das Installationspaket wurde veraendert. Abbruch.'
}
$certificatePath = Join-Path $PSScriptRoot 'PDF-Zipper-test.cer'
$certificate = [Security.Cryptography.X509Certificates.X509Certificate2]::new($certificatePath)
if ($certificate.Thumbprint -ne $expected) { throw 'Unerwartetes Zertifikat: Abbruch.' }
if ($TrustOnly) {
    Import-Certificate -FilePath $certificatePath -CertStoreLocation Cert:/LocalMachine/TrustedPeople | Out-Null
    if (!(Test-Path "Cert:/LocalMachine/TrustedPeople/$expected")) { throw 'Zertifikat fehlt nach Import.' }
    exit 0
}
if (!(Test-Path "Cert:/LocalMachine/TrustedPeople/$expected")) {
    Write-Host 'Dieses Paket verwendet ein internes Testzertifikat (CN=kulmi84).'
    Write-Host ('Fingerabdruck: ' + $expected)
    Write-Host 'Es wird unter Lokaler Computer / Vertrauenswuerdige Personen eingerichtet.'
    Write-Host 'Das Vertrauen gilt auch fuer weitere Pakete mit diesem Testschluessel.'
    if ((Read-Host 'Zertifikat einrichten und PDF-Zipper installieren? Zum Bestaetigen JA eingeben') -cne 'JA') {
        Write-Host 'Installation abgebrochen. Es wurde kein Zertifikat eingerichtet.'
        exit 0
    }
    Write-Host 'Bitte die Windows-Administratorabfrage fuer das lokale Testzertifikat bestaetigen.'
    $powershell = "$env:SystemRoot/System32/WindowsPowerShell/v1.0/powershell.exe"
    $arguments = '-NoProfile -ExecutionPolicy Bypass -File "' + $PSCommandPath + '" -TrustOnly'
    $job = Start-Process $powershell -ArgumentList $arguments -Verb RunAs -WindowStyle Hidden -Wait -PassThru
    if ($job.ExitCode -ne 0 -or !(Test-Path "Cert:/LocalMachine/TrustedPeople/$expected")) {
        throw 'Das Testzertifikat wurde nicht eingerichtet. Installation abgebrochen.'
    }
}
$running = @(Get-Process -Name 'PDF-Zipper' -ErrorAction SilentlyContinue)
if ($running.Count -gt 0) {
    Write-Host 'Offene PDF-Zipper-Dialoge werden geschlossen. Laufende Verarbeitung darf fertig werden.'
    foreach ($process in $running) {
        try {
            if ($process.MainWindowHandle -ne [IntPtr]::Zero) { $null = $process.CloseMainWindow() }
        } catch [InvalidOperationException] { }
    }
    $deadline = [DateTime]::UtcNow.AddSeconds(15)
    do {
        $remaining = @(Get-Process -Name 'PDF-Zipper' -ErrorAction SilentlyContinue)
        if ($remaining.Count -eq 0) { break }
        Start-Sleep -Milliseconds 500
    } while ([DateTime]::UtcNow -lt $deadline)
    if ($remaining.Count -gt 0) {
        $remaining | Select-Object Id,MainWindowTitle | Format-Table -AutoSize
        throw 'PDF-Zipper laeuft weiterhin (Prozess-ID siehe oben). Bitte Verarbeitung abwarten oder den angezeigten Dialog schliessen und erneut installieren.'
    }
}
Write-Host 'Die bisherige Explorer-Erweiterung wird fuer das Update freigegeben.'
Add-AppxPackage -Path $package -ForceTargetApplicationShutdown
$installed = Get-AppxPackage -Name kulmi84.PDFZipper
if (!$installed) { throw 'PDF-Zipper wurde nicht gefunden.' }
Write-Host ('PDF-Zipper installiert: ' + $installed.Version)
Write-Host 'Bitte ein neues Explorer-Fenster oeffnen und mindestens zwei PDFs im selben Ordner markieren.'
$installed | Select-Object Name,Version,InstallLocation | Format-List
