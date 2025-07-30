@echo off

set "OBS_STUDIO_VERSION=30.1.1"

set "BASE_DIR=%CD%"
set "OBS_STUDIO_BUILD_DIR=%BASE_DIR%\obs-studio-build"
set "OBS_STUDIO_DIR=%OBS_STUDIO_BUILD_DIR%\obs-studio-%OBS_STUDIO_VERSION%"
set "OBS_STUDIO_RELEASE_DIR=%OBS_STUDIO_BUILD_DIR%\obs-studio-release"
set "OBS_INSTALL_PREFIX=%OBS_STUDIO_DIR%\build"

echo "Building obs-studio-%OBS_STUDIO_VERSION%"
mkdir "%OBS_STUDIO_BUILD_DIR%" 2>NUL
cd "%OBS_STUDIO_BUILD_DIR%"
if not exist "%OBS_STUDIO_DIR%" (
	git clone --recursive -b %OBS_STUDIO_VERSION% --single-branch https://github.com/obsproject/obs-studio.git "obs-studio-%OBS_STUDIO_VERSION%"
)
:: download the official release of obs studio (to copy signed win-capture)
if not exist "%OBS_STUDIO_RELEASE_DIR%" (
	if not exist "OBS-Studio-%OBS_STUDIO_VERSION%.zip" (
		curl -kLO "https://github.com/obsproject/obs-studio/releases/download/%OBS_STUDIO_VERSION%/OBS-Studio-%OBS_STUDIO_VERSION%.zip" -f --retry 5 -C -
	)
	7z x "OBS-Studio-%OBS_STUDIO_VERSION%.zip" -o"%OBS_STUDIO_RELEASE_DIR%"
)

:: clean build folder if it exists from previous attempt
rmdir /s /q %OBS_INSTALL_PREFIX%"" 2>NUL
mkdir "%OBS_INSTALL_PREFIX%" 2>NUL
cd "%OBS_STUDIO_DIR%"

:: build for Win64
cmake -S . -B "%OBS_INSTALL_PREFIX%" --preset windows-x64 ^
	-DENABLE_BROWSER:BOOL=OFF ^
	-DENABLE_VLC:BOOL=OFF ^
	-DENABLE_UI:BOOL=OFF ^
	-DENABLE_VST:BOOL=OFF ^
	-DENABLE_SCRIPTING:BOOL=OFF ^
	-DCOPIED_DEPENDENCIES:BOOL=OFF ^
    -DCOPY_DEPENDENCIES:BOOL=ON ^
	-DBUILD_FOR_DISTRIBUTION:BOOL=ON

cmake --build "%OBS_INSTALL_PREFIX%" --config Release

cd "%BASE_DIR%"

if not exist "%OBS_INSTALL_PREFIX%\rundir\Release\bin\64bit\obs.dll" (
	echo "Fatal error: Failed to find obs.dll"
	exit /b 1
)

setlocal enabledelayedexpansion

set "deps_dir=%OBS_STUDIO_DIR%\.deps"
set "newest_date="
set "newest_dir="

:: Iterate through directories in .deps/ to fetch the latest x64 deps
for /f "tokens=*" %%a in ('dir /b /ad "%deps_dir%\obs-deps-*" ^| findstr /r /c:"obs-deps-[0-9][0-9][0-9][0-9]-[0-1][0-9]-[0-3][0-9]-x64$"') do (
    :: Extract the date part from the directory name
    set "dir_name=%%~nxa"
    set "date_part=!dir_name:~9,10!"

    :: Compare the date with the current newest one
    if "!date_part!" gtr "!newest_date!" (
        set "newest_date=!date_part!"
        set "newest_dir=%%a"
    )
)

:: Set the variable to the newest directory
set "WINDOWS_DEPS_DIR=%deps_dir%\%newest_dir%"

:: copy rest of the required dependencies ourselves, because cmake doesn't automatically do them for some reason?
robocopy "%WINDOWS_DEPS_DIR%\bin " "%OBS_INSTALL_PREFIX%\rundir\Release\bin\64bit\ " /E /IS /IT /R:0 /W:0 /XD "%WINDOWS_DEPS_DIR%\bin\Lib" /XF *.pdb || IF %ERRORLEVEL% GEQ 8 goto:copy_error
:: copy plugins & data to bin directory to make loadModule work
robocopy "%OBS_INSTALL_PREFIX%\rundir\Release\obs-plugins " "%OBS_INSTALL_PREFIX%\rundir\Release\bin\64bit\obs-plugins\ " /E /IS /IT /R:0 /W:0 /XF *.ini *.pdb || IF %ERRORLEVEL% GEQ 8 goto:copy_error
robocopy "%OBS_INSTALL_PREFIX%\rundir\Release\data " "%OBS_INSTALL_PREFIX%\rundir\Release\bin\64bit\data\ " /E /IS /IT /R:0 /W:0 /XF *.ini *.pdb || IF %ERRORLEVEL% GEQ 8 goto:copy_error
:: copy win-capture from official release to our build (because we need signed files for better compatibility)
robocopy "%OBS_STUDIO_RELEASE_DIR%\data\obs-plugins\win-capture " "%OBS_INSTALL_PREFIX%\rundir\Release\bin\64bit\data\obs-plugins\win-capture\ " /E /IS /IT /R:0 /W:0 /XF *.ini *.pdb || IF %ERRORLEVEL% GEQ 8 goto:copy_error
:: (HOTFIX, remove this later when we figure out issue #287) copy encoder test executables
robocopy "%OBS_STUDIO_RELEASE_DIR%\bin\64bit " "%OBS_INSTALL_PREFIX%\rundir\Release\bin\64bit\ " "obs-*-mux.exe" "obs-ffmpeg-mux.exe" /IS /IT /R:0 /W:0 || IF %ERRORLEVEL% GEQ 8 goto:copy_error

echo "OBS build completed successfully"
exit /b 0

:copy_error
echo "Fatal error: Failed to copy files (code: %ERRORLEVEL%)"
exit /B 1
