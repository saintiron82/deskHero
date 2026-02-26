@echo off
title DeskWarrior Monster Editor
cd /d "%~dp0"
python tools/sprite_manager.py
if %ERRORLEVEL% neq 0 (
    echo.
    echo [오류] 실행 실패. Python 및 PyQt6 설치 여부를 확인하세요.
    echo   pip install PyQt6 Pillow
    pause
)
