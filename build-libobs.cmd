@echo off

set OBS_STUDIO_VERSION=29.1.1
set OBS_DEPS_RELEASE_DATE=2023-04-12

set BASE_DIR=%CD%
set OBS_STUDIO_BUILD_DIR=%BASE_DIR%\obs-studio-build
set OBS_STUDIO_DIR=%OBS_STUDIO_BUILD_DIR%\obs-studio-%OBS_STUDIO_VERSION%
set WINDOWS_DEPS_DIR=%OBS_STUDIO_BUILD_DIR%\windows-deps
set OBS_STUDIO_RELEASE_DIR=%OBS_STUDIO_BUILD_DIR%\obs-studio-release
set OBS_INSTALL_PREFIX=%OBS_STUDIO_DIR%\build

echo "Building obs-studio"
mkdir "%OBS_STUDIO_BUILD_DIR%" 2>NUL
cd "%OBS_STUDIO_BUILD_DIR%"
if not exist "%OBS_STUDIO_DIR%" (
	git clone --recursive -b %OBS_STUDIO_VERSION% --single-branch https://github.com/obsproject/obs-studio.git "obs-studio-%OBS_STUDIO_VERSION%"
)
:: download obs windows deps
if not exist "%WINDOWS_DEPS_DIR%" (
	if not exist "%WINDOWS_DEPS_VERSION%.zip" (
		curl -kLO "https://github.com/obsproject/obs-deps/releases/download/%OBS_DEPS_RELEASE_DATE%/windows-deps-%OBS_DEPS_RELEASE_DATE%-x64.zip" -f --retry 5 -C -
	)
	7z x "windows-deps-%OBS_DEPS_RELEASE_DATE%-x64.zip" -o"%WINDOWS_DEPS_DIR%"
)
:: download the official release of obs studio (to copy signed win-capture)
if not exist "%OBS_STUDIO_RELEASE_DIR%" (
	if not exist "OBS-Studio-29.1.1.zip" (
		curl -kLO "https://github.com/obsproject/obs-studio/releases/download/%OBS_STUDIO_VERSION%/OBS-Studio-29.1.1.zip" -f --retry 5 -C -
	)
	7z x "OBS-Studio-29.1.1.zip" -o"%OBS_STUDIO_RELEASE_DIR%"
)

:: clean build folder if it exists from previous attempt
rmdir /s /q %OBS_INSTALL_PREFIX% 2>NUL
mkdir "%OBS_INSTALL_PREFIX%" 2>NUL
cd "%OBS_STUDIO_DIR%"

:: build for Win64
cmake -S . -B "%OBS_INSTALL_PREFIX%" -G "Visual Studio 17 2022" ^
	-DCMAKE_GENERATOR_PLATFORM="x64" ^
	-DCMAKE_SYSTEM_VERSION="10.0.18363.657" ^
	-DCMAKE_PREFIX_PATH:PATH="%WINDOWS_DEPS_DIR%" ^
	-DCMAKE_INSTALL_PREFIX="%OBS_INSTALL_PREFIX%\install" ^
	-DENABLE_BROWSER=OFF ^
	-DENABLE_VLC=OFF ^
	-DENABLE_UI=OFF ^
	-DENABLE_VST=OFF ^
	-DENABLE_SCRIPTING=OFF ^
	-DCOPIED_DEPENDENCIES=OFF ^
    -DCOPY_DEPENDENCIES=ON ^
	-DBUILD_FOR_DISTRIBUTION=ON

cmake --build "%OBS_INSTALL_PREFIX%" --config Release

:: copy rest of the required dependencies ourselves, because cmake doesn't automatically do them for some reason?
:: this task essentially replicates (some of) CopyMSVCBins.cmake
xcopy "%WINDOWS_DEPS_DIR%\bin" "%OBS_INSTALL_PREFIX%\rundir\Release\bin\64bit\" /E /Y /I
:: copy plugins & data to bin directory to make loadModule work
xcopy "%OBS_INSTALL_PREFIX%\rundir\Release\obs-plugins" "%OBS_INSTALL_PREFIX%\rundir\Release\bin\64bit\obs-plugins\" /E /Y /I
xcopy "%OBS_INSTALL_PREFIX%\rundir\Release\data" "%OBS_INSTALL_PREFIX%\rundir\Release\bin\64bit\data\" /E /Y /I
:: copy win-capture from official release to our build (because we need signed files for better compatibility)
xcopy "%OBS_STUDIO_RELEASE_DIR%\data\obs-plugins\win-capture" "%OBS_INSTALL_PREFIX%\rundir\Release\bin\64bit\data\obs-plugins\win-capture\" /E /Y /I

cd %BASE_DIR%