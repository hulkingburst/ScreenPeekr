@echo off
echo Installing ScreenPeekr...

REM Set installation directory (user profile to avoid admin requirement)
set "INSTALL_DIR=%LOCALAPPDATA%\ScreenPeekr"

REM Create installation directory
if not exist "%INSTALL_DIR%" (
    mkdir "%INSTALL_DIR%"
)

REM Determine build configuration
if exist "bin\Debug\net8.0-windows\ScreenPeekr.exe" (
    set "SOURCE_DIR=bin\Debug\net8.0-windows"
) else if exist "bin\Release\net8.0-windows\ScreenPeekr.exe" (
    set "SOURCE_DIR=bin\Release\net8.0-windows"
) else if exist "bin\Release\publish\ScreenPeekr.exe" (
    set "SOURCE_DIR=bin\Release\publish"
) else (
    echo Error: Could not find built executable.
    echo Please build the project first using: dotnet build -c Release
    pause
    exit /b 1
)

REM Copy files
echo Copying files from %SOURCE_DIR% to %INSTALL_DIR%...
xcopy /Y /E /I "%SOURCE_DIR%\*" "%INSTALL_DIR%\"
if %errorLevel% neq 0 (
    echo Failed to copy files.
    pause
    exit /b 1
)

REM Create desktop shortcut
echo Creating desktop shortcut...
powershell -Command "$WshShell = New-Object -ComObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut('%USERPROFILE%\Desktop\ScreenPeekr.lnk'); $Shortcut.TargetPath = '%INSTALL_DIR%\ScreenPeekr.exe'; $Shortcut.WorkingDirectory = '%INSTALL_DIR%'; $Shortcut.Save()"

REM Ask if user wants to start with Windows
set /p STARTUP="Do you want ScreenPeekr to start with Windows? (Y/N): "
if /i "%STARTUP%"=="Y" (
    reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v "ScreenPeekr" /t REG_SZ /d "\"%INSTALL_DIR%\ScreenPeekr.exe\"" /f
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
