@echo off
cd /d "%~dp0"
@echo off
chcp 65001 >nul
echo.
echo ==========================================
echo   BuildPackage
echo ==========================================
echo.

SET SRC=..\bin\Release
SET OUT=.\Package

if not exist "%SRC%\BizSqNotifier.exe" (
    echo [Error] Release build first.
    pause
    exit /b 1
)

if exist "%OUT%" rd /S /Q "%OUT%"
mkdir "%OUT%"
mkdir "%OUT%\Templates"
mkdir "%OUT%\Scripts"

echo Copying files...
copy /Y "%SRC%\BizSqNotifier.exe" "%OUT%\" >nul
copy /Y "%SRC%\BizSqNotifier.exe.config" "%OUT%\" >nul
copy /Y "%SRC%\Newtonsoft.Json.dll" "%OUT%\" >nul
copy /Y "%SRC%\Templates\*.html" "%OUT%\Templates\" >nul
copy /Y "%SRC%\Scripts\*.bat" "%OUT%\Scripts\" >nul
copy /Y "Install.bat" "%OUT%\" >nul
copy /Y "Uninstall.bat" "%OUT%\" >nul
copy /Y "Update.bat" "%OUT%\" >nul
copy /Y "..\..\BizSqNotifier.ico" "%OUT%\BizSqNotifier.ico" >nul
copy /Y "..\..\BizSqNotifier-Guide.html" "%OUT%\" >nul

echo.
echo ==========================================
echo   Package created: %OUT%
echo ==========================================
echo.
pause
