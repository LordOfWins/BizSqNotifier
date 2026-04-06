@echo off
chcp 65001 >nul
echo.
echo ══════════════════════════════════════════
echo   BizSqNotifier 설치
echo ══════════════════════════════════════════
echo.

REM ── 설치 경로 ──
SET INSTALL_DIR=C:\BizSqNotifier
SET SHORTCUT_NAME=BizSqNotifier

echo 설치 경로: %INSTALL_DIR%
echo.

REM ── 기존 프로세스 종료 ──
taskkill /IM BizSqNotifier.exe /F >nul 2>&1

REM ── 폴더 생성 ──
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"
if not exist "%INSTALL_DIR%\Templates" mkdir "%INSTALL_DIR%\Templates"
if not exist "%INSTALL_DIR%\Scripts" mkdir "%INSTALL_DIR%\Scripts"
if not exist "%INSTALL_DIR%\Logs" mkdir "%INSTALL_DIR%\Logs"

REM ── 파일 복사 ──
echo 파일 복사 중...
copy /Y "%~dp0BizSqNotifier.exe" "%INSTALL_DIR%\" >nul
copy /Y "%~dp0BizSqNotifier.exe.config" "%INSTALL_DIR%\" >nul
copy /Y "%~dp0Newtonsoft.Json.dll" "%INSTALL_DIR%\" >nul
copy /Y "%~dp0Templates\*.html" "%INSTALL_DIR%\Templates\" >nul
copy /Y "%~dp0Scripts\*.bat" "%INSTALL_DIR%\Scripts\" >nul

REM ── App.config 초기 설정 (최초 설치시만) ──
if not exist "%INSTALL_DIR%\BizSqNotifier.exe.config" (
    copy /Y "%~dp0BizSqNotifier.exe.config" "%INSTALL_DIR%\" >nul
    echo [안내] App.config 복사 완료 — DB 연결 정보를 확인하세요.
)

REM ── settings.json 보존 (기존 설정 유지) ──
if not exist "%INSTALL_DIR%\settings.json" (
    echo [안내] settings.json 없음 — 첫 실행시 기본값으로 자동 생성됩니다.
)

REM ── 바탕화면 바로가기 생성 ──
echo 바로가기 생성 중...
powershell -Command "$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut([System.IO.Path]::Combine([Environment]::GetFolderPath('Desktop'), '%SHORTCUT_NAME%.lnk')); $s.TargetPath = '%INSTALL_DIR%\BizSqNotifier.exe'; $s.WorkingDirectory = '%INSTALL_DIR%'; $s.Description = 'BizSqNotifier - 공유오피스 자동 메일 발송'; $s.Save()"

REM ── Task Scheduler 등록 ──
echo.
echo Task Scheduler 등록 중...

schtasks /create /tn "BizSqNotifier\AutoStart" ^
    /tr "\"%INSTALL_DIR%\BizSqNotifier.exe\" /silent" ^
    /sc ONLOGON /rl HIGHEST /f >nul 2>&1

if %ERRORLEVEL% EQU 0 (
    echo   [OK] 로그인시 자동 시작 등록 완료
) else (
    echo   [!] 자동 시작 등록 실패 — 관리자 권한으로 다시 실행하세요
)

schtasks /create /tn "BizSqNotifier\DailyRun" ^
    /tr "\"%INSTALL_DIR%\BizSqNotifier.exe\" /run" ^
    /sc DAILY /st 08:55 /rl HIGHEST /f >nul 2>&1

if %ERRORLEVEL% EQU 0 (
    echo   [OK] 매일 08:55 자동 실행 등록 완료
) else (
    echo   [!] 자동 실행 등록 실패 — 관리자 권한으로 다시 실행하세요
)

echo.
echo ══════════════════════════════════════════
echo   설치 완료!
echo.
echo   실행: 바탕화면 BizSqNotifier 아이콘
echo   설정: 프로그램 실행 후 [설정] 버튼
echo   경로: %INSTALL_DIR%
echo ══════════════════════════════════════════
echo.
pause
