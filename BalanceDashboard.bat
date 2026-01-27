@echo off
chcp 65001 >nul
title DeskWarrior Balance Dashboard

REM 인자가 있으면 바로 실행 (예: BalanceDashboard.bat 1)
if "%1"=="1" goto RUN_PYTHON
if "%1"=="r" goto RUN_PYTHON
if "%1"=="4" goto RUN_EXE
if "%1"=="e" goto RUN_EXE

:MENU
cls
echo ========================================
echo   DeskWarrior Balance Dashboard
echo ========================================
echo.
echo   [1] 바로 실행 (Python)
echo   [2] 빌드 후 실행 (exe 생성)
echo   [3] 빌드만 (exe 생성)
echo   [4] exe 실행 (기존 빌드)
echo   [5] 실행 중인 대시보드 종료
echo   [6] 종료
echo.
echo   [r] 빠른 재시작 (종료 후 Python 재실행)
echo.
set /p choice="선택하세요 (1-6, r): "

if "%choice%"=="1" goto RUN_PYTHON
if "%choice%"=="2" goto BUILD_AND_RUN
if "%choice%"=="3" goto BUILD_ONLY
if "%choice%"=="4" goto RUN_EXE
if "%choice%"=="5" goto KILL_RUNNING
if "%choice%"=="6" goto END
if "%choice%"=="r" goto QUICK_RESTART
if "%choice%"=="R" goto QUICK_RESTART
goto MENU

:KILL_RUNNING
echo.
echo 실행 중인 대시보드를 종료합니다...
taskkill /F /IM "BalanceDashboard.exe" 2>nul
if %errorlevel%==0 (
    echo   - BalanceDashboard.exe 종료됨
) else (
    echo   - BalanceDashboard.exe 실행 중 아님
)
REM Python 프로세스 중 balance_dashboard_qt.py 실행 중인 것 종료
wmic process where "commandline like '%%balance_dashboard_qt.py%%'" call terminate 2>nul >nul
if %errorlevel%==0 (
    echo   - Python 대시보드 종료됨
) else (
    echo   - Python 대시보드 실행 중 아님
)
echo.
echo 완료!
pause
goto MENU

:QUICK_RESTART
call :KILL_SILENT
echo.
echo 빠른 재시작...
python "%~dp0balance_dashboard_qt.py"
goto MENU

:RUN_PYTHON
call :KILL_SILENT
echo.
echo Python으로 실행 중...
python "%~dp0balance_dashboard_qt.py"
if errorlevel 1 (
    echo.
    echo [오류] Python 실행 실패. Python이 설치되어 있는지 확인하세요.
    pause
)
goto END

:BUILD_AND_RUN
echo.
echo 빌드 중... (시간이 걸릴 수 있습니다)
python -m PyInstaller --noconfirm "%~dp0BalanceDashboard.spec"
if errorlevel 1 (
    echo.
    echo [오류] 빌드 실패. PyInstaller가 설치되어 있는지 확인하세요.
    echo   pip install pyinstaller
    pause
    goto MENU
)
call :KILL_SILENT
echo.
echo 빌드 완료! 실행 중...
start "" "%~dp0dist\BalanceDashboard.exe"
goto END

:BUILD_ONLY
echo.
echo 빌드 중... (시간이 걸릴 수 있습니다)
python -m PyInstaller --noconfirm "%~dp0BalanceDashboard.spec"
if errorlevel 1 (
    echo.
    echo [오류] 빌드 실패. PyInstaller가 설치되어 있는지 확인하세요.
    echo   pip install pyinstaller
) else (
    echo.
    echo 빌드 완료! 파일 위치: dist\BalanceDashboard.exe
)
pause
goto MENU

:RUN_EXE
if exist "%~dp0dist\BalanceDashboard.exe" (
    call :KILL_SILENT
    echo.
    echo exe 실행 중...
    start "" "%~dp0dist\BalanceDashboard.exe"
) else (
    echo.
    echo [오류] exe 파일이 없습니다. 먼저 빌드하세요.
    pause
    goto MENU
)
goto END

:KILL_SILENT
REM 기존 실행 중인 대시보드 조용히 종료
taskkill /F /IM "BalanceDashboard.exe" 2>nul >nul
wmic process where "commandline like '%%balance_dashboard_qt.py%%'" call terminate 2>nul >nul
goto :eof

:END
