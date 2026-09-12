@echo off
chcp 65001 >nul
cd /d "%~dp0"
py -3 picker.py
if errorlevel 1 pause
