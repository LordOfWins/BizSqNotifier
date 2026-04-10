@echo off
chcp 65001 >nul
echo.
echo ==========================================
echo   BizSqNotifier Uninstall
echo ==========================================
echo.

SET INSTALL_DIR=C:\BizSqNotifier

echo Path: %INSTALL_DIR%
echo.
set /p CONFIRM=Continue? (Y/N):
if /i not "%CONFIRM%"=="Y" (
    echo Cancelled.
    pause
    exit /b 0
)

taskkill /IM BizSqNotifier.exe /F >nul 2>&1

echo Removing Task Scheduler...
schtasks /delete /tn "BizSqNotifier\AutoStart" /f >nul 2>&1
schtasks /delete /tn "BizSqNotifier\DailyRun" /f >nul 2>&1
echo   [OK] Scheduler removed

del "%USERPROFILE%\Desktop\BizSqNotifier.lnk" >nul 2>&1

echo Removing files...
del /Q "%INSTALL_DIR%\BizSqNotifier.exe" >nul 2>&1
del /Q "%INSTALL_DIR%\BizSqNotifier.exe.config" >nul 2>&1
del /Q "%INSTALL_DIR%\Newtonsoft.Json.dll" >nul 2>&1
del /Q "%INSTALL_DIR%\app.ico" >nul 2>&1
del /Q "%INSTALL_DIR%\BizSqNotifier-Guide.html" >nul 2>&1
del /Q "%INSTALL_DIR%\Templates\*.html" >nul 2>&1
rd /Q "%INSTALL_DIR%\Templates" >nul 2>&1
del /Q "%INSTALL_DIR%\Scripts\*.bat" >nul 2>&1
rd /Q "%INSTALL_DIR%\Scripts" >nul 2>&1

echo.
echo ==========================================
echo   Uninstall complete!
echo   settings.json and Logs preserved.
echo   Manual delete: %INSTALL_DIR%
echo ==========================================
echo.
pause
