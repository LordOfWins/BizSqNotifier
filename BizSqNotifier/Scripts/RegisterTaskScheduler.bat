@echo off
REM ════════════════════════════════════════════════════════
REM  BizSqNotifier — Windows Task Scheduler 등록 스크립트
REM  관리자 권한으로 실행 필요
REM ════════════════════════════════════════════════════════

echo.
echo [BizSqNotifier] Task Scheduler 등록
echo ──────────────────────────────────────
echo.

REM 설치 경로 설정 (MSI 설치 후 기본 경로)
SET INSTALL_DIR=%~dp0..
SET EXE_PATH=%INSTALL_DIR%\BizSqNotifier.exe

IF NOT EXIST "%EXE_PATH%" (
    echo [오류] BizSqNotifier.exe를 찾을 수 없습니다.
    echo   경로: %EXE_PATH%
    echo   스크립트를 BizSqNotifier 설치 폴더의 Scripts 하위에서 실행하세요.
    pause
    exit /b 1
)

echo 실행 파일: %EXE_PATH%
echo.

REM ── 작업 1: 시스템 시작 시 /silent 모드로 자동 실행 ──
echo [1/2] 시스템 시작 시 자동 실행 등록...
schtasks /create /tn "BizSqNotifier\AutoStart" ^
    /tr "\"%EXE_PATH%\" /silent" ^
    /sc ONLOGON ^
    /rl HIGHEST ^
    /f

IF %ERRORLEVEL% EQU 0 (
    echo   → 등록 완료: 로그인 시 /silent 모드로 자동 시작
) ELSE (
    echo   → 등록 실패! 관리자 권한으로 다시 실행하세요.
)
echo.

REM ── 작업 2: 매일 08:55에 /run 모드 실행 (백업용 — 내부 스케줄러 보조) ──
echo [2/2] 매일 08:55 백업 실행 등록...
schtasks /create /tn "BizSqNotifier\DailyRun" ^
    /tr "\"%EXE_PATH%\" /run" ^
    /sc DAILY ^
    /st 08:55 ^
    /rl HIGHEST ^
    /f

IF %ERRORLEVEL% EQU 0 (
    echo   → 등록 완료: 매일 08:55에 /run 모드 실행
) ELSE (
    echo   → 등록 실패! 관리자 권한으로 다시 실행하세요.
)

echo.
echo ──────────────────────────────────────
echo 등록 완료. Task Scheduler에서 확인하세요.
echo   시작 > taskschd.msc > BizSqNotifier 폴더
echo.
pause
