# Release-Prüfung für 0.1.12

## Geprüft

- Privates Repository kulmi84/PDF-Zipper, MIT-Lizenz.
- Windows 11 x64, moderner Explorer-Eintrag PDF-Zipper mit Untermenü.
- Genau zwei PDFs: Verzahnen und Zusammenführen; mehr als zwei: nur Zusammenführen.
- Dateinamen-Erkennung einschließlich fortlaufender Scan-Nummern; Rückseiten beim Verzahnen rückwärts.
- Frei wählbare Reihenfolge beim Zusammenführen; Ausgabe ohne Überschreiben.
- Kompakte Windows-Hell-/Dunkel-Dialoge mit abgerundeten Buttons und Schatten.
- Deutsch bei deutscher Windows-Anzeigesprache, sonst Englisch.
- 165 PDF-Prüfungen in de-DE, en-US und fr-FR sowie native COM-Auswahltests erfolgreich.
- Lokaler vollständiger Build erfolgreich, Store-MSIX im Partner Center validiert.
- Deutsche und englische Store-Einträge gespeichert, echte aktuelle Screenshots ergänzt.
- Am 11.10.2026 zur Zertifizierung eingereicht; Veröffentlichung bleibt manuell.

## Noch offen

- [ ] CI-Prüfung auf dem aktualisierten Repository-Commit.
- [ ] Erstinstallation und Deinstallation auf einem zweiten, sauberen Windows-11-x64-PC.
- [ ] Kontrastdesign am Zielgerät prüfen.
- [ ] Microsoft-Store-Zertifizierung erfolgreich abgeschlossen.
- [ ] Nach erfolgreicher Zertifizierung manuell im Store veröffentlichen.

Der GitHub-Testinstaller für 0.1.12 wurde mit dem bestehenden internen Testzertifikat signiert; die Signatur wurde erfolgreich geprüft. Das Store-Paket wird nicht als direkt installierbarer GitHub-Installer angeboten. Private Signierschlüssel und Passwörter bleiben lokal. Keine automatische Release-Veröffentlichung im Workflow.