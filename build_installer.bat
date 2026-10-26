@echo off
setlocal
cd /d "%~dp0"
title Building Mawaqit Installer...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build_installer.ps1" %*
pause
