@echo off
chcp 65001 > nul
echo ========================================
echo   DeskWarrior Build and Run
echo ========================================
echo.

echo [0/3] Stopping existing DeskWarrior...
taskkill /IM DeskWarrior.exe /F >nul 2>&1
if %ERRORLEVEL% equ 0 (
    echo Stopped existing process.
) else (
    echo No existing process found.
)
echo.

echo [1/4] Restoring NuGet packages...
dotnet restore
if %ERRORLEVEL% neq 0 (
    echo ERROR: Package restore failed!
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [2/3] Building project (Debug)...
dotnet build --configuration Debug
if %ERRORLEVEL% neq 0 (
    echo ERROR: Build failed!
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [3/3] Starting DeskWarrior...
echo ========================================
start "" "bin\Debug\net9.0-windows\DeskWarrior.exe"
echo Done!
