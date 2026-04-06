@echo off
chcp 65001 >nul
echo.
echo ══════════════════════════════════════════
echo   BizSqNotifier 업데이트
echo ══════════════════════════════════════════
echo.

SET INSTALL_DIR=C:\BizSqNotifier

if not exist "%INSTALL_DIR%\BizSqNotifier.exe" (
    echo [오류] 기존 설치를 찾을 수 없습니다.
    echo        먼저 Install.bat을 실행하세요.
    pause
    exit /b 1
)

REM ── 프로세스 종료 ──
taskkill /IM BizSqNotifier.exe /F >nul 2>&1
timeout /t 2 /nobreak >nul

REM ── 파일 업데이트 (settings.json 보존) ──
echo 파일 업데이트 중...
copy /Y "%~dp0BizSqNotifier.exe" "%INSTALL_DIR%\" >nul
copy /Y "%~dp0Newtonsoft.Json.dll" "%INSTALL_DIR%\" >nul
copy /Y "%~dp0Templates\*.html" "%INSTALL_DIR%\Templates\" >nul
copy /Y "%~dp0Scripts\*.bat" "%INSTALL_DIR%\Scripts\" >nul

echo.
echo ══════════════════════════════════════════
echo   업데이트 완료!
echo.
echo   [참고] settings.json / App.config은
echo          기존 설정이 유지됩니다.
echo ══════════════════════════════════════════
echo.

REM ── 자동 재시작 ──
echo 프로그램을 다시 시작합니다...
start "" "%INSTALL_DIR%\BizSqNotifier.exe" /silent
pause
