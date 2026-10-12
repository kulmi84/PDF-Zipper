# PDF-Zipper 0.1.13 – Installation für Windows 11 x64

1. Das Installer-ZIP vollständig in einen Ordner entpacken.
2. Offene PDF-Zipper-Dialoge schließen und eine laufende PDF-Verarbeitung fertig werden lassen.
3. `Install-PDF-Zipper.cmd` doppelklicken, als normal angemeldeter Windows-Benutzer.
4. Nur auf einem neuen PC: Hinweise zum lokalen Testzertifikat lesen, mit `JA` bestätigen und anschließend die Windows-Administratorabfrage bestätigen. Die App selbst wird für den ursprünglich angemeldeten Benutzer installiert.
5. Nach erfolgreicher Installation ein neues Explorer-Fenster öffnen. Mindestens zwei PDFs im selben Ordner markieren → PDF-Zipper.

Das ZIP enthält das signierte MSIX, den Installer, das öffentliche Zertifikat und Lizenzhinweise. Kein .NET und keine Build-Werkzeuge erforderlich.

## Testsignierung

Diese Version ist mit einem **internen Testzertifikat** signiert, nicht mit einem öffentlich vertrauenswürdigen Herausgeberzertifikat. Das Zertifikat `CN=kulmi84` muss auf jedem neuen PC ausdrücklich unter **Lokaler Computer → Vertrauenswürdige Personen** eingerichtet werden. Das Vertrauen umfasst auch weitere Pakete, die mit demselben Testschlüssel signiert werden. Der Installer prüft Paket-SHA-256 und Zertifikat-Fingerabdruck vor der Installation. Ein SmartScreen-Hinweis kann trotzdem erscheinen.

Fingerabdruck: `013012837D1FF2119EE33A45A33E0ABDF52AAFB3`. Gültig bis 10.10.2027, ohne Zeitstempel. Der private Schlüssel ist nicht enthalten.

PowerShell-Skriptausführung wird nur für den Installer-Prozess freigegeben. Eine Unternehmensrichtlinie kann die Installation weiterhin sperren.

## Update

Den Installer einer neueren Version erneut starten. Offene PDF-Zipper-Dialoge werden regulär geschlossen; für eine laufende PDF-Verarbeitung wartet der Installer bis zu 15 Sekunden. Die alte Paketinstanz wird anschließend gezielt für das Update freigegeben. Originale und Ergebnis-PDFs werden nicht geändert.

## Deinstallation

Windows → Einstellungen → Apps → PDF-Zipper → Deinstallieren. Dadurch verschwindet auch der Explorer-Eintrag. Ergebnis-PDFs bleiben erhalten.

Das separat eingerichtete Zertifikatsvertrauen bleibt bestehen. Zum Entfernen in einer Administrator-PowerShell:

```powershell
Remove-Item -LiteralPath 'Cert:/LocalMachine/TrustedPeople/013012837D1FF2119EE33A45A33E0ABDF52AAFB3'
```

## Bekannte Grenzen

Windows 11 x64; keine ARM64-Version. Alle Eingaben müssen im selben Ordner liegen. Beim Verzahnen genau zwei PDFs mit gleichen Seitenzahlen; beim Zusammenführen mindestens zwei PDFs, auch mit unterschiedlichen Seitenzahlen. Geschützte und beschädigte PDFs werden abgewiesen. Dokumentweite Spezialfunktionen wie Signaturen, Lesezeichen und Formulare werden nicht garantiert erhalten.
