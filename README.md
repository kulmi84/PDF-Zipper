# PDF-Zipper

Zwei Scan-PDFs im Windows-11-Explorer markieren → Rechtsklick → **PDFs verzahnen**.

PDF-Zipper kombiniert Vorderseiten in normaler Reihenfolge mit Rückseiten in umgekehrter Reihenfolge. Beispiel:

| Eingabe | Seiten |
| --- | --- |
| Vorderseiten | 1, 3, 5 |
| Rückseiten (Scanreihenfolge) | 6, 4, 2 |
| Ergebnis | 1, 2, 3, 4, 5, 6 |

## Bedienung

Beide Dateien müssen im selben Ordner liegen und gleich viele Seiten enthalten. Der Eintrag erscheint nur bei genau zwei PDF-Dateien. Die Reihenfolge der Explorer-Auswahl spielt keine Rolle.

Eindeutige Wörter im Dateinamen werden erkannt: `Vorderseite`, `Vorderseiten`, `vorne`, `front`, `fronts`, `recto` sowie `Rückseite`, `Rückseiten`, `Rueckseite`, `Rueckseiten`, `hinten`, `back`, `backs`, `verso`. Beide Namen müssen übereinstimmende Hinweise geben. Beispiel: `Scan_Vorderseiten.pdf` und `Scan_Rückseiten.pdf`.

Bei unklaren oder widersprüchlichen Namen öffnet sich eine kleine Auswahl: einmal auf die Datei mit den Vorderseiten klicken. Mit Escape oder „Abbrechen“ wird nichts geschrieben.

Das Ergebnis liegt im selben Ordner: `<Vorderseitenname>_verzahnt.pdf`. Existiert dieser Name bereits, folgen ` (1)`, ` (2)` usw. Vorhandene Dateien und Originale werden niemals überschrieben. Erst die fertig geschriebene PDF wird unter ihrem endgültigen Namen sichtbar. Erfolgreiche Aufrufe enden ohne weiteren Dialog.

Keine Hauptoberfläche, kein Autostart, kein eigener Hintergrunddienst, keine Netzwerkanfragen oder Telemetrie. Die Explorer-Erweiterung übergibt nur die Auswahl; die PDF-Verarbeitung läuft in einem eigenen Prozess.

## Installation und Status

Ziel: Windows 11, x64. Der Eintrag soll direkt im neuen Kontextmenü erscheinen. Dafür wird die von Microsoft vorgesehene Kombination aus `IExplorerCommand` und MSIX-Registrierung verwendet. [Microsoft-Dokumentation](https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/desktop-to-uwp-extensions#specify-a-context-menu-handler-for-a-file-type).

**0.1.0 ist ein vorbereiteter Release-Kandidat.** Ein signiertes, auf Windows 11 geprüftes Installationspaket wird erst nach Abschluss der [Release-Prüfung](docs/RELEASE-CHECKLIST.md) freigegeben. Ein unsigniertes CI-Paket ist kein fertiger Installer.

Nach Signierung mit einem auf dem Zielgerät vertrauenswürdigen Zertifikat: MSIX doppelklicken und installieren. Bei einem internen Testzertifikat muss dessen Vertrauen ausdrücklich eingerichtet werden; das Build-Skript verändert keinen Zertifikatsspeicher. Deinstallation über Windows → Einstellungen → Apps → PDF-Zipper entfernt auch die Menüregistrierung. PDF-Ergebnisse bleiben bestehen.

## Grenzen

- Unterschiedliche Seitenzahlen: Abbruch statt stiller Ergänzung oder Verwerfung. Eine fehlende letzte Rückseite muss vorab ergänzt werden.
- Verschlüsselte/passwortgeschützte und beschädigte PDFs werden abgewiesen.
- Für Scans gedacht: Dokument-Lesezeichen, digitale Signaturen, PDF/A-Konformität und dokumentweite Formularlogik werden nicht garantiert erhalten. Originale aufbewahren.
- Keine automatische Erkennung anhand des Seiteninhalts, keine Drehung, keine OCR und keine Dateikomprimierung.
- Die Rückseiten-PDF wird immer umgekehrt. Bereits richtig geordnete Rückseiten vorher umsortieren.

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
./scripts/sign.ps1 -Package ./artifacts/PDF-Zipper-0.1.0.0-x64.msix -CertificateThumbprint '<Thumbprint>'
```

Ein RFC-3161-Zeitstempeldienst kann mit `-TimestampUrl` angegeben werden. Das Skript signiert und prüft die Signatur; es erzeugt keine Zertifikate und richtet kein Vertrauen ein.

GitHub Actions führt dieselben Tests und die Paketvalidierung durch. Es veröffentlicht keine Releases automatisch. Zum direkten Entwicklertest:

```powershell
& ./src/PdfZipper/bin/Release/net8.0-windows/PDF-Zipper.exe 'C:\Scans\Vorderseiten.pdf' 'C:\Scans\Rückseiten.pdf'
```

Die Tests prüfen Seitenreihenfolge anhand unterschiedlicher Seitengrößen, Erkennung, unveränderte Originale, Namenskollisionen einschließlich paralleler Aufrufe und Fehlerfälle.

## Lizenz

MIT, Copyright 2026 kulmi84. Bibliotheken und Laufzeit: siehe [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
