# PDF-Zipper

Zwei Scan-PDFs im Windows-11-Explorer markieren → Rechtsklick → **PDF-Zipper** → **PDFs verzahnen**.

PDF-Zipper kombiniert Vorderseiten in normaler Reihenfolge mit Rückseiten in umgekehrter Reihenfolge. Beispiel:

| Eingabe | Seiten |
| --- | --- |
| Vorderseiten | 1, 3, 5 |
| Rückseiten (Scanreihenfolge) | 6, 4, 2 |
| Ergebnis | 1, 2, 3, 4, 5, 6 |

## Screenshots

Windows-11-Aufnahmen aus der lokalen Entwicklung im dunklen Design. Die Dialoge folgen auch dem hellen Windows-Design. Die abschließende Gestaltung von 0.1.10 hat noch dezentere Konturen.

**Vorderseiten auswählen** – bei unklaren Dateinamen genügt ein Klick auf die passende PDF:

![Vorderseitenauswahl im dunklen Windows-Design](docs/screenshots/vorderseiten-dark.png)

**PDFs zusammenführen** – ausgewählte PDFs über „Nach oben“ und „Nach unten“ in die gewünschte Reihenfolge bringen:

![Reihenfolge mehrerer PDFs auswählen](docs/screenshots/reihenfolge-dark.png)

## Bedienung

Beide Dateien müssen im selben Ordner liegen. Gleiche Seitenzahlen sind nur beim Verzahnen erforderlich. Der Eintrag erscheint bei mindestens zwei PDF-Dateien. Bei genau zwei sind beide Funktionen verfügbar; bei mehr als zwei wird nur **PDFs zusammenführen** angeboten. Die Reihenfolge der Explorer-Auswahl spielt keine Rolle.

Eindeutige Wörter im Dateinamen werden erkannt: `Vorderseite`, `Vorderseiten`, `vorne`, `front`, `fronts`, `recto` sowie `Rückseite`, `Rückseiten`, `Rueckseite`, `Rueckseiten`, `hinten`, `back`, `backs`, `verso`. Beide Namen müssen übereinstimmende Hinweise geben. Beispiel: `Scan_Vorderseiten.pdf` und `Scan_Rückseiten.pdf`.

Bei unklaren oder widersprüchlichen Namen öffnet sich eine kleine Auswahl: einmal auf die Datei mit den Vorderseiten klicken. Mit Escape oder „Abbrechen“ wird nichts geschrieben.

Auch fortlaufende Scan-Nummern werden erkannt: `en0018.pdf` und `en0019.pdf` oder `18.pdf` und `19.pdf`. Die kleinere Nummer gilt als Vorderseite (zuerst gescannte Datei). Das gilt nur für direkt aufeinanderfolgende Nummern mit identischem Text davor und danach und genau einem Zahlenblock pro Name. Eindeutige Vorder-/Rückseiten-Wörter haben Vorrang; bei widersprüchlichen Angaben bleibt die Auswahl erhalten.

Das Ergebnis liegt im selben Ordner: `<Vorderseitenname>_verzahnt.pdf`. Existiert dieser Name bereits, folgen ` (1)`, ` (2)` usw. Vorhandene Dateien und Originale werden niemals überschrieben. Erst die fertig geschriebene PDF wird unter ihrem endgültigen Namen sichtbar. Erfolgreiche Aufrufe enden ohne weiteren Dialog.

Keine Hauptoberfläche, kein Autostart, kein eigener Hintergrunddienst, keine Netzwerkanfragen oder Telemetrie. Die Explorer-Erweiterung übergibt nur die Auswahl; die PDF-Verarbeitung läuft in einem eigenen Prozess.

## PDFs zusammenführen

Rechtsklick → **PDF-Zipper** → **PDFs zusammenführen** öffnet einen kompakten Dialog im hellen oder dunklen Windows-Design mit einer nummerierten Liste aller ausgewählten Dateien. Datei markieren und mit **Nach oben** oder **Nach unten** umsortieren; **Zusammenführen** bestätigt, Escape oder **Abbrechen** schreibt nichts. Zuerst werden alle Seiten der ersten Datei, danach alle Seiten der weiteren Dateien in ihrer ursprünglichen Reihenfolge übernommen. Unterschiedliche Seitenzahlen sind erlaubt.

Ausgabe: `<ErsteDatei>_zusammengeführt.pdf` im selben Ordner, bei Namenskollision mit fortlaufendem Zusatz. Originale und vorhandene Dateien bleiben erhalten.

Alle Dialoge verwenden die Windows-Einstellung für Hell/Dunkel, einschließlich der Titelleiste. Bei aktiviertem Kontrastdesign haben die Windows-Systemfarben Vorrang. Die Fenster sind kompakt und skalieren mit der Bildschirm-DPI.

## Installation und Status

Ziel: Windows 11, x64. Der Eintrag soll direkt im neuen Kontextmenü erscheinen. Dafür wird die von Microsoft vorgesehene Kombination aus `IExplorerCommand` und MSIX-Registrierung verwendet. [Microsoft-Dokumentation](https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/desktop-to-uwp-extensions#specify-a-context-menu-handler-for-a-file-type).

**Aktueller Stand: 0.1.10.** Lokal unter Windows 11 installiert und vom Nutzer geprüft. Der neue Kontextmenüeintrag und die kompakten Hell-/Dunkel-Dialoge wurden im Explorer verwendet. Ein Test auf einem frisch eingerichteten zweiten PC steht noch aus.

Der Installer enthält das signierte MSIX und ein **internes Testzertifikat**. Auf einem neuen PC ist eine ausdrückliche Vertrauenseinrichtung erforderlich. Es liegt noch kein öffentlich vertrauenswürdiges Herausgeberzertifikat vor. [Installationsanleitung](installer/INSTALLATION.md) · [Releases und Installer](https://github.com/kulmi84/PDF-Zipper/releases).

Installer-ZIP vollständig entpacken und `Install-PDF-Zipper.cmd` doppelklicken. Bei erstmaliger Einrichtung das Zertifikatsvertrauen mit `JA` und anschließend per Windows-Administratorabfrage bestätigen. Danach läuft PDF-Zipper ohne Build-Werkzeuge und ohne installiertes .NET. Updates verwenden denselben Installer; Deinstallation über Windows-Einstellungen entfernt die Menüregistrierung. Ergebnis-PDFs bleiben erhalten.

## Grenzen

- Beim Verzahnen unterschiedliche Seitenzahlen: Abbruch statt stiller Ergänzung oder Verwerfung. Eine fehlende letzte Rückseite muss vorab ergänzt werden.
- Verschlüsselte/passwortgeschützte und beschädigte PDFs werden abgewiesen.
- Für Scans gedacht: Dokument-Lesezeichen, digitale Signaturen, PDF/A-Konformität und dokumentweite Formularlogik werden nicht garantiert erhalten. Originale aufbewahren.
- Keine automatische Erkennung anhand des Seiteninhalts, keine Drehung, keine OCR und keine Dateikomprimierung.
- Beim Verzahnen wird die Rückseiten-PDF immer umgekehrt. Bereits richtig geordnete Rückseiten vorher umsortieren.

## Entwicklung und Build

Benötigt: .NET 8 SDK, Visual Studio 2022 Build Tools mit „Desktopentwicklung mit C++“, CMake und Windows SDK.

```powershell
dotnet run --project tests/PdfZipper.Tests.csproj -c Release
./scripts/build.ps1
```

Der Build erstellt eine eigenständig lauffähige x64-Anwendung, eine native Shell-DLL mit statischer C++-Laufzeit und ein **unsigniertes** MSIX unter `artifacts/`. Ein installiertes .NET ist auf dem Zielgerät nicht erforderlich. Für erneute Paket-Builds einen frischen Checkout verwenden oder den vorhandenen Build-Ordner entfernen.

Für ein bereits eingerichtetes Code-Signing-Zertifikat im Zertifikatsspeicher des aktuellen Benutzers:

```powershell
# Beim Build -Publisher mit dem exakten Zertifikats-Subject angeben.
./scripts/sign.ps1 -Package ./artifacts/PDF-Zipper-0.1.10.0-x64.msix -CertificateThumbprint '<Thumbprint>'
```

Ein RFC-3161-Zeitstempeldienst kann mit `-TimestampUrl` angegeben werden. Das Skript signiert und prüft die Signatur; es erzeugt keine Zertifikate und richtet kein Vertrauen ein.

GitHub Actions führt dieselben Tests und die Paketvalidierung durch. Es veröffentlicht keine Releases automatisch. Zum direkten Entwicklertest:

```powershell
& ./src/PdfZipper/bin/Release/net8.0-windows/PDF-Zipper.exe 'C:\Scans\Vorderseiten.pdf' 'C:\Scans\Rückseiten.pdf'
```

Die Tests prüfen Seitenreihenfolge anhand unterschiedlicher Seitengrößen, Erkennung, unveränderte Originale, Namenskollisionen einschließlich paralleler Aufrufe und Fehlerfälle.

## Lizenz

MIT, Copyright 2026 kulmi84. Bibliotheken und Laufzeit: siehe [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).




