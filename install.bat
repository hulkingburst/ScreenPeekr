@echo off
echo Installing ScreenPeekr...

REM Check if running as administrator
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo Please run this installer as Administrator.
    echo Right-click install.bat and select "Run as administrator"
    pause
    exit /b 1
)

REM Set installation directory
set "INSTALL_DIR=C:\Program Files\ScreenPeekr"

REM Create installation directory
if not exist "%INSTALL_DIR%" (
    mkdir "%INSTALL_DIR%"
)

REM Copy files
echo Copying files to %INSTALL_DIR%...
copy /Y "bin\Release\publish\ScreenPeekr.exe" "%INSTALL_DIR%\"
if %errorLevel% neq 0 (
    echo Failed to copy files.
    pause
    exit /b 1
)

REM Create desktop shortcut
echo Creating desktop shortcut...
powershell -Command "$WshShell = New-Object -ComObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut('%USERPROFILE%\Desktop\ScreenPeekr.lnk'); $Shortcut.TargetPath = '%INSTALL_DIR%\ScreenPeekr.exe'; $Shortcut.Save()"

REM Ask if user wants to start with Windows
set /p STARTUP="Do you want ScreenPeekr to start with Windows? (Y/N): "
if /i "%STARTUP%"=="Y" (
    reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v "ScreenPeekr" /t REG_SZ /d "%INSTALL_DIR%\ScreenPeekr.exe" /f
    echo ScreenPeekr will start with Windows.
)

echo.
echo Installation complete!
echo ScreenPeekr has been installed to: %INSTALL_DIR%
echo A shortcut has been created on your desktop.
echo.
set /p RUN="Do you want to run ScreenPeekr now? (Y/N): "
if /i "%RUN%"=="Y" (
    start "" "%INSTALL_DIR%\ScreenPeekr.exe"
)

pause
