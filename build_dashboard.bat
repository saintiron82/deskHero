@echo off
echo ============================================
echo DeskWarrior Balance Dashboard - Build Script
echo PyQt6 Version (No Server Required)
echo ============================================
echo.

cd /d "%~dp0"

echo Checking dependencies...
pip show PyQt6 >nul 2>&1
if %errorlevel% neq 0 (
    echo Installing PyQt6...
    pip install PyQt6
)

pip show matplotlib >nul 2>&1
if %errorlevel% neq 0 (
    echo Installing matplotlib...
    pip install matplotlib
)

pip show pyinstaller >nul 2>&1
if %errorlevel% neq 0 (
    echo Installing pyinstaller...
    pip install pyinstaller
)

echo.
echo Building executable...
echo.

python -m PyInstaller --noconfirm ^
    --onefile ^
    --windowed ^
    --name "BalanceDashboard" ^
    --add-data "config;config" ^
    balance_dashboard_qt.py

if %errorlevel% equ 0 (
    echo.
    echo ============================================
    echo Build successful!
    echo Executable: dist\BalanceDashboard.exe
    echo ============================================
) else (
    echo.
    echo Build failed. Check errors above.
)

pause
