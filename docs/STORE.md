# Microsoft Store

Version 0.1.12.0 wurde am 11.10.2026 zur Zertifizierung eingereicht. Die Veröffentlichung erfolgt manuell nach erfolgreicher Prüfung. Das Store-Paket ist kein direkt installierbarer GitHub-Testinstaller.

## Paketidentität

- Name: `MarcinKulmaczewski.PDF-Zipper`
- Publisher: `CN=CF6431F5-0687-4BED-943A-37412E88412C`
- PublisherDisplayName: `Marcin Kulmaczewski`
- Store-ID: `9P3623HLHCSJ`
- Windows 11 x64, Deutsch und Englisch, `runFullTrust` für Win32-Verarbeitung und Explorer-Erweiterung.

Der normale Build verwendet die lokale Testidentität `kulmi84.PDFZipper`. Für eine Store-Einreichung müssen Name, Publisher und PublisherDisplayName im bereitgestellten Paketmanifest exakt den obigen Werten entsprechen. Das Paket danach mit MakeAppx neu erstellen und im Partner Center validieren. Das interne Testzertifikat wird für die Store-Auslieferung nicht benötigt und darf nicht Bestandteil des Store-Pakets sein.

Englische Store-Texte: [STORE-ENGLISH.md](STORE-ENGLISH.md). Die Screenshots unter `screenshots/` zeigen echte Dialoge und das Explorer-Menü mit Testdateien.

## Prüfung für 0.1.12

- Lokaler vollständiger x64-Build einschließlich nativer COM-Auswahltests erfolgreich.
- 165 PDF-Prüfungen: je 55 unter de-DE, en-US und fr-FR (englischer Fallback).
- Paket im Partner Center validiert; deutsche und englische Store-Einträge gespeichert.
- Zertifizierung gestartet; Veröffentlichung bleibt manuell.
- Erstinstallation und Deinstallation auf einem zweiten, sauberen Windows-11-PC stehen noch aus.
