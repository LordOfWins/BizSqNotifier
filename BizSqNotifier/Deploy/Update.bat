@echo off
chcp 65001 >nul
echo.
echo ==========================================
echo   BizSqNotifier Update
echo ==========================================
echo.

SET INSTALL_DIR=C:\Users\%USERNAME%\BizSqNotifier

if not exist "%INSTALL_DIR%\BizSqNotifier.exe" (
    echo [Error] No existing install found.
    echo         Run Install.bat first.
    pause
    exit /b 1
)

taskkill /IM BizSqNotifier.exe /F >nul 2>&1
timeout /t 2 /nobreak >nul

echo Updating files...
copy /Y "%~dp0BizSqNotifier.exe" "%INSTALL_DIR%\" >nul
copy /Y "%~dp0Newtonsoft.Json.dll" "%INSTALL_DIR%\" >nul
copy /Y "%~dp0BizSqNotifier.ico" "%INSTALL_DIR%\" >nul
copy /Y "%~dp0BizSqNotifier-Guide.html" "%INSTALL_DIR%\" >nul
copy /Y "%~dp0Templates\*.html" "%INSTALL_DIR%\Templates\" >nul
copy /Y "%~dp0Scripts\*.bat" "%INSTALL_DIR%\Scripts\" >nul

echo.
echo ==========================================
echo   Update complete!
echo   settings.json / App.config preserved.
echo ==========================================
echo.

echo Restarting...
start "" "%INSTALL_DIR%\BizSqNotifier.exe" /silent
pause
