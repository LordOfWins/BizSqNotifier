@echo off
chcp 65001 >nul
echo.
echo ══════════════════════════════════════════
echo   BizSqNotifier 제거
echo ══════════════════════════════════════════
echo.

SET INSTALL_DIR=C:\BizSqNotifier

echo 프로그램을 제거하시겠습니까?
echo 경로: %INSTALL_DIR%
echo.
set /p CONFIRM=계속하려면 Y 입력: 
if /i not "%CONFIRM%"=="Y" (
    echo 취소되었습니다.
    pause
    exit /b 0
)

REM ── 프로세스 종료 ──
taskkill /IM BizSqNotifier.exe /F >nul 2>&1

REM ── Task Scheduler 해제 ──
echo Task Scheduler 해제 중...
schtasks /delete /tn "BizSqNotifier\AutoStart" /f >nul 2>&1
schtasks /delete /tn "BizSqNotifier\DailyRun" /f >nul 2>&1
echo   [OK] 스케줄러 해제 완료

REM ── 바탕화면 바로가기 삭제 ──
del "%USERPROFILE%\Desktop\BizSqNotifier.lnk" >nul 2>&1

REM ── 파일 삭제 (settings.json/Logs 보존) ──
echo 파일 삭제 중...
del /Q "%INSTALL_DIR%\BizSqNotifier.exe" >nul 2>&1
del /Q "%INSTALL_DIR%\BizSqNotifier.exe.config" >nul 2>&1
del /Q "%INSTALL_DIR%\Newtonsoft.Json.dll" >nul 2>&1
del /Q "%INSTALL_DIR%\Templates\*.html" >nul 2>&1
rd /Q "%INSTALL_DIR%\Templates" >nul 2>&1
del /Q "%INSTALL_DIR%\Scripts\*.bat" >nul 2>&1
rd /Q "%INSTALL_DIR%\Scripts" >nul 2>&1

echo.
echo ══════════════════════════════════════════
echo   제거 완료!
echo.
echo   [참고] settings.json과 Logs 폴더는
echo          보존되었습니다. 필요시 수동 삭제:
echo          %INSTALL_DIR%
echo ══════════════════════════════════════════
echo.
pause
