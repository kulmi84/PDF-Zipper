# 0.1.16

Wordexport verwendet einen lokalen PDF-Drucker nur innerhalb der Wordinstanz. Der Windows-Standarddrucker bleibt unverändert. Der Fix wurde vom Nutzer auf einem betroffenen Rechner mit denselben Eingaben bestätigt (erster Export 4,2 statt rund 54 Sekunden). Agent-Prüfung: 204 PDF-/Bildprüfungen; echter Wordexport wurde anhand der Nutzerprotokolle geprüft. Die native Explorer-DLL bleibt identisch mit 0.1.13.

Fortschritt zeigt Datei, Verarbeitungsschritt und Laufzeit. Diagnose ist optional über --diagnose --choose-files. Keine Diagnoseprotokolle im normalen Explorer-Aufruf. Das neue Bild zu Word/PDF/Bildern ist in README und beiden Store-Sprachen enthalten. Store-Veröffentlichung nach Zertifizierung manuell.
