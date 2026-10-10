@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-PDF-Zipper.ps1"
if errorlevel 1 echo Installation fehlgeschlagen. Bitte die Meldung oben beachten.
pause

