# Release-Prüfung für 0.1.10

## Festgelegt und geprüft

- Privates Repository kulmi84/PDF-Zipper, MIT-Lizenz.
- Windows 11 x64, direkter moderner Explorer-Eintrag PDF-Zipper.
- Genau zwei PDFs: Verzahnen und Zusammenführen; mehr als zwei PDFs: nur Zusammenführen.
- Dateinamen-Erkennung inklusive fortlaufender Scan-Nummern; Rückseiten beim Verzahnen rückwärts.
- Zusammenführen in frei wählbarer Reihenfolge, unterschiedliche Seitenzahlen erlaubt.
- Ausgabe im selben Ordner ohne Überschreiben, Originale bleiben erhalten.
- Kompakte Windows-Hell-/Dunkel-Dialoge und abgerundete flache Buttons, Layout auch bei 150 % geprüft.
- Lokal 53 PDF-Prüfungen bestanden. Erweiterte native COM-/Untermenütests liegen im Quellcode und werden in CI ausgeführt.
- Windows-11-Installation und Explorer-Bedienung auf dem Entwicklungs-PC durch Nutzer ausprobiert; Gestaltung von 0.1.10 akzeptiert.
- MSIX validiert und lokal mit CN=kulmi84 signiert; privater Schlüssel bleibt außerhalb von Repository und Installer.
- Installer mit Paketprüfsumme, expliziter Zustimmung zum Testzertifikatsvertrauen, Update-Freigabe und deutschen Hinweisen vorbereitet.

## Noch offen

- [ ] Aktuelle CI-Prüfung auf dem finalen Repository-Commit erfolgreich.
- [ ] Erstinstallation auf einem zweiten, sauberen Windows-11-x64-PC einschließlich Zertifikatszustimmung und Deinstallation.
- [ ] Helles Windows-Design und Kontrastdesign am Zielgerät prüfen; bisherige Nutzerrückmeldung betrifft dunkles Design.
- [ ] Veröffentlichung mit klar benannter interner Testsignierung freigeben oder auf ein öffentlich vertrauenswürdiges Herausgeberzertifikat warten.

Kein privater Signierschlüssel und kein PFX-Passwort wird hochgeladen. Automatische Veröffentlichung ist im Build-Workflow nicht eingerichtet.
