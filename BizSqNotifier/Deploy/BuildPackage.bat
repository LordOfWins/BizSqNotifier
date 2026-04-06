@echo off
chcp 65001 >nul
echo.
echo ══════════════════════════════════════════
echo   배포 패키지 생성
echo ══════════════════════════════════════════
echo.

SET SRC=..\bin\Release
SET OUT=.\Package

if not exist "%SRC%\BizSqNotifier.exe" (
    echo [오류] Release 빌드를 먼저 실행하세요.
    pause
    exit /b 1
)

if exist "%OUT%" rd /S /Q "%OUT%"
mkdir "%OUT%"
mkdir "%OUT%\Templates"
mkdir "%OUT%\Scripts"

echo 파일 복사 중...
copy /Y "%SRC%\BizSqNotifier.exe" "%OUT%\" >nul
copy /Y "%SRC%\BizSqNotifier.exe.config" "%OUT%\" >nul
copy /Y "%SRC%\Newtonsoft.Json.dll" "%OUT%\" >nul
copy /Y "%SRC%\Templates\*.html" "%OUT%\Templates\" >nul
copy /Y "%SRC%\Scripts\*.bat" "%OUT%\Scripts\" >nul
copy /Y "Install.bat" "%OUT%\" >nul
copy /Y "Uninstall.bat" "%OUT%\" >nul
copy /Y "Update.bat" "%OUT%\" >nul

echo.
echo ══════════════════════════════════════════
echo   패키지 생성 완료: %OUT%
echo.
echo   Package\
echo     BizSqNotifier.exe
echo     BizSqNotifier.exe.config
echo     Newtonsoft.Json.dll
echo     Install.bat
echo     Uninstall.bat
echo     Update.bat
echo     Templates\  (7개 HTML)
echo     Scripts\    (2개 BAT)
echo ══════════════════════════════════════════
echo.
pause
