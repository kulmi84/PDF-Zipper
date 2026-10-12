# Release-Prüfung für 0.1.13

## Geprüft

- Privates Repository kulmi84/PDF-Zipper, MIT-Lizenz.
- Windows 11 x64, moderner Explorer-Eintrag PDF-Zipper mit Untermenü.
- Genau zwei PDFs: Verzahnen und Zusammenführen; mehr als zwei: nur Zusammenführen.
- Dateinamen-Erkennung einschließlich fortlaufender Scan-Nummern; Rückseiten beim Verzahnen rückwärts.
- Frei wählbare Reihenfolge beim Zusammenführen; Ausgabe ohne Überschreiben.
- Kompakte Windows-Hell-/Dunkel-Dialoge mit abgerundeten Buttons und Schatten.
- Deutsch bei deutscher Windows-Anzeigesprache, sonst Englisch.
- 198 PDF-/Bildprüfungen in de-DE, en-US und fr-FR sowie native COM-Auswahltests erfolgreich.
- Lokaler vollständiger Build erfolgreich, Store-MSIX im Partner Center validiert.
- Deutsche und englische Store-Einträge gespeichert, echte aktuelle Screenshots ergänzt.
- Am 12.10.2026 zur Zertifizierung eingereicht; Veröffentlichung bleibt manuell.

## Noch offen

- [ ] CI-Prüfung auf dem aktualisierten Repository-Commit.
- [ ] Erstinstallation und Deinstallation auf einem zweiten, sauberen Windows-11-x64-PC.
- [ ] Kontrastdesign am Zielgerät prüfen.
- [ ] Microsoft-Store-Zertifizierung erfolgreich abgeschlossen.
- [ ] Nach erfolgreicher Zertifizierung manuell im Store veröffentlichen.

Der GitHub-Testinstaller für 0.1.13 wurde mit dem bestehenden internen Testzertifikat signiert; die Signatur wurde erfolgreich geprüft. Das Store-Paket wird nicht als direkt installierbarer GitHub-Installer angeboten. Private Signierschlüssel und Passwörter bleiben lokal. Keine automatische Release-Veröffentlichung im Workflow.
## Word und Bilder

Word (.doc/.docx), PDF und JPG/PNG können gemeinsam zusammengeführt werden. Die Word-Voraussetzung wird im Store-Eintrag genannt. Die nativen Tests prüfen den ausgegrauten Word-Menüpunkt bei simulierter fehlender Wordinstallation. Ein echter Wordexport wird beim lokalen Build geprüft. Der Nutzer hat den vollständigen Build ausgeführt und die neue Funktion erfolgreich verwendet.
