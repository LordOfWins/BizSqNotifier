@echo off
REM  BizSqNotifier — Task Scheduler 등록 해제

echo.
echo [BizSqNotifier] Task Scheduler 등록 해제
echo ──────────────────────────────────────
echo.

schtasks /delete /tn "BizSqNotifier\AutoStart" /f 2>nul
IF %ERRORLEVEL% EQU 0 ( echo   AutoStart 삭제 완료 ) ELSE ( echo   AutoStart 없음 또는 삭제 실패 )

schtasks /delete /tn "BizSqNotifier\DailyRun" /f 2>nul
IF %ERRORLEVEL% EQU 0 ( echo   DailyRun 삭제 완료 ) ELSE ( echo   DailyRun 없음 또는 삭제 실패 )

REM 폴더 삭제 시도
schtasks /delete /tn "BizSqNotifier" /f 2>nul

echo.
echo 등록 해제 완료.
pause
