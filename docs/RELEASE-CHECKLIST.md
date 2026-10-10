# Release-Prüfung für 0.1.0

## Bereits entschieden

- Repository privat: `kulmi84/PDF-Zipper`.
- Projektlizenz MIT.
- Direkter Eintrag im neuen Windows-11-Kontextmenü, MSIX-Paket.
- Keine unnötige UI und kein eigener Hintergrunddienst.
- Version 0.1.0 für Windows 11 x64; ARM64 ist zunächst nicht vorgesehen.
- Unterschiedliche Seitenzahlen führen zu einem Abbruch.
- Signierung wird später geklärt. Release bleibt ausdrücklich ein unveröffentlichter Entwurf.

## Vor Veröffentlichung klären und prüfen

- [ ] Paketsignierung: vorhandenes Code-Signing-Zertifikat oder internes Testzertifikat? Publisher im Manifest muss exakt zum Zertifikat passen. Keine privaten Schlüssel im Repository.
- [x] Zielarchitektur x64 für 0.1.0 bestätigt.
- [x] Sicherer Abbruch bei unterschiedlichen Seitenzahlen für 0.1.0 bestätigt.
- [ ] Automatisch kopierte Drittanbieter-Lizenzen aus dem tatsächlichen Build auf Vollständigkeit prüfen.
- [x] CI: C++-Build, 22 PDF-Prüfungen, native COM-/Auswahltests und MakeAppx-Validierung erfolgreich. [Geprüfter Build](https://github.com/kulmi84/PDF-Zipper/actions/runs/38071151844).
- [ ] Windows-11-Test auf sauberem Gerät: signiertes Paket installieren; Menü direkt sichtbar, nur bei zwei PDFs; deutsch/englisch erkannte Namen und Auswahl testen.
- [ ] Unklarer Name: Escape und Fenster schließen erzeugen keine Datei.
- [ ] Große Scans, Unicode/Leerzeichen im Pfad, Schreibschutz, gesperrte Ausgabe, defekte und geschützte PDFs testen.
- [ ] Beide Originale unverändert, Wiederholung überschreibt nichts.
- [ ] Paket deinstallieren: Eintrag verschwindet, erstellte PDFs bleiben.
- [ ] Release-Freigabe nach Prüfung der offenen Punkte. Keine Veröffentlichung durch den Build-Workflow.

## Entwurf der Release Notes

PDF-Zipper 0.1.0 für Windows 11 x64: zwei PDF-Scans direkt im Explorer verzahnen. Vorderseiten bleiben in normaler Reihenfolge, Rückseiten werden rückwärts abwechselnd eingefügt. Automatische Rollen-Erkennung anhand eindeutiger Dateinamen; eine kleine Auswahl bei Unklarheit. Ausgabe im Quellordner ohne Überschreiben. Keine eigene dauerhafte Hintergrundkomponente.

Installation: signiertes MSIX öffnen. Bekannte Einschränkungen: gleiche Seitenzahl erforderlich; keine geschützten PDFs; Scan-Dokumente, keine garantierte Erhaltung dokumentweiter Spezialfunktionen.

