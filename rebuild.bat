@echo off
echo Stopping DeskWarrior...
taskkill /f /im DeskWarrior.exe 2>nul

echo Building...
dotnet build DeskWarrior.csproj -c Debug
if %ERRORLEVEL% NEQ 0 (
    echo Build failed!
    pause
    exit /b 1
)

echo Starting DeskWarrior...
start "" "bin\Debug\net9.0-windows\DeskWarrior.exe"
