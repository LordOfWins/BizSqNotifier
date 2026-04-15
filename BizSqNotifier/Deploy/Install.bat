@echo off
cd /d "%~dp0"
@echo off
chcp 65001 >nul
echo.
echo ==========================================
echo   BizSqNotifier Install
echo ==========================================
echo.

SET INSTALL_DIR=C:\Users\%USERNAME%\BizSqNotifier
SET SHORTCUT_NAME=BizSqNotifier

echo Install path: %INSTALL_DIR%
echo.

taskkill /IM BizSqNotifier.exe /F >nul 2>&1

if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"
if not exist "%INSTALL_DIR%\Templates" mkdir "%INSTALL_DIR%\Templates"
if not exist "%INSTALL_DIR%\Scripts" mkdir "%INSTALL_DIR%\Scripts"
if not exist "%INSTALL_DIR%\Logs" mkdir "%INSTALL_DIR%\Logs"

echo Copying files...
copy /Y "%~dp0BizSqNotifier.exe" "%INSTALL_DIR%\" >nul
copy /Y "%~dp0BizSqNotifier.exe.config" "%INSTALL_DIR%\" >nul
copy /Y "%~dp0Newtonsoft.Json.dll" "%INSTALL_DIR%\" >nul
copy /Y "%~dp0BizSqNotifier.ico" "%INSTALL_DIR%\" >nul
copy /Y "%~dp0BizSqNotifier-Guide.html" "%INSTALL_DIR%\" >nul
copy /Y "%~dp0Templates\*.html" "%INSTALL_DIR%\Templates\" >nul
copy /Y "%~dp0Scripts\*.bat" "%INSTALL_DIR%\Scripts\" >nul

if not exist "%INSTALL_DIR%\BizSqNotifier.exe.config" (
    copy /Y "%~dp0BizSqNotifier.exe.config" "%INSTALL_DIR%\" >nul
    echo [Info] App.config copied.
)

if not exist "%INSTALL_DIR%\settings.json" (
    echo [Info] settings.json not found. Will be created on first run.
)

echo Creating shortcut...
powershell -Command "$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut([System.IO.Path]::Combine([Environment]::GetFolderPath('Desktop'), '%SHORTCUT_NAME%.lnk')); $s.TargetPath = '%INSTALL_DIR%\BizSqNotifier.exe'; $s.WorkingDirectory = '%INSTALL_DIR%'; $s.IconLocation = '%INSTALL_DIR%\BizSqNotifier.ico,0'; $s.Description = 'BizSqNotifier'; $s.Save()"

echo.
echo ==========================================
echo   Install complete!
echo   Path: %INSTALL_DIR%
echo.
echo   [참고] 자동 발송 스케줄러는 프로그램 내
echo          설정 > 연결 테스트 탭에서 등록하세요.
echo ==========================================
echo.
pause
