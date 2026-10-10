# Drittanbieter

- PDFsharp 6.2.4: MIT, empira Software GmbH. [Quellcode und Lizenz](https://github.com/empira/PDFsharp/blob/master/LICENSE).
- .NET 8 Laufzeit und Windows Forms: MIT, .NET Foundation und Mitwirkende. [Lizenz](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT), [Drittanbieterhinweise](https://github.com/dotnet/runtime/blob/main/THIRD-PARTY-NOTICES.TXT).
- Microsoft C/C++-Laufzeit: bei MSVC-Build statisch eingebunden; Weitergabe gemäß den Visual-Studio-Lizenzbedingungen.

Das Build-Skript legt den PDFsharp-Lizenztext unter `licenses/` bei und kopiert dort außerdem Lizenz- und Drittanbieterdateien aus den tatsächlich verwendeten NuGet-Paketen einschließlich der Laufzeitpakete. Vor einer Weitergabe ist deren Vollständigkeit zu prüfen. Diese Übersicht ersetzt die vollständigen Lizenztexte nicht.
