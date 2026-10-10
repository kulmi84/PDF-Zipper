# Release-Prüfung für 0.1.0

## Bereits entschieden

- Repository privat: `kulmi84/PDF-Zipper`.
- Projektlizenz MIT.
- Direkter Eintrag im neuen Windows-11-Kontextmenü, MSIX-Paket.
- Keine unnötige UI und kein eigener Hintergrunddienst.

## Vor Veröffentlichung klären und prüfen

- [ ] Paketsignierung: vorhandenes Code-Signing-Zertifikat oder internes Testzertifikat? Publisher im Manifest muss exakt zum Zertifikat passen. Keine privaten Schlüssel im Repository.
- [ ] Zielarchitektur x64 zunächst ausreichend oder zusätzlich ARM64?
- [ ] Unterschiedliche Seitenzahlen: derzeit sicherer Abbruch. Soll eine einzelne fehlende letzte Rückseite später erlaubt werden?
- [ ] Automatisch kopierte Drittanbieter-Lizenzen aus dem tatsächlichen Build auf Vollständigkeit prüfen.
- [ ] CI: C++-Build ohne Warnungen, PDF-Tests und MakeAppx-Validierung erfolgreich.
- [ ] Windows-11-Test auf sauberem Gerät: signiertes Paket installieren; Menü direkt sichtbar, nur bei zwei PDFs; deutsch/englisch erkannte Namen und Auswahl testen.
- [ ] Unklarer Name: Escape und Fenster schließen erzeugen keine Datei.
- [ ] Große Scans, Unicode/Leerzeichen im Pfad, Schreibschutz, gesperrte Ausgabe, defekte und geschützte PDFs testen.
- [ ] Beide Originale unverändert, Wiederholung überschreibt nichts.
- [ ] Paket deinstallieren: Eintrag verschwindet, erstellte PDFs bleiben.
- [ ] Release-Freigabe nach Prüfung der offenen Punkte. Keine Veröffentlichung durch den Build-Workflow.

## Entwurf der Release Notes

PDF-Zipper 0.1.0 für Windows 11 x64: zwei PDF-Scans direkt im Explorer verzahnen. Vorderseiten bleiben in normaler Reihenfolge, Rückseiten werden rückwärts abwechselnd eingefügt. Automatische Rollen-Erkennung anhand eindeutiger Dateinamen; eine kleine Auswahl bei Unklarheit. Ausgabe im Quellordner ohne Überschreiben. Keine eigene dauerhafte Hintergrundkomponente.

Installation: signiertes MSIX öffnen. Bekannte Einschränkungen: gleiche Seitenzahl erforderlich; keine geschützten PDFs; Scan-Dokumente, keine garantierte Erhaltung dokumentweiter Spezialfunktionen.
