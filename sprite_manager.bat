@echo off
cd /d "%~dp0"
python tools/sprite_manager.py
if %ERRORLEVEL% neq 0 (
    echo.
    echo [오류] 실행 실패. 아래 메시지를 확인하세요.
    pause
)
r