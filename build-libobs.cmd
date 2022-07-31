@echo off

set OBS_STUDIO_VERSION=27.2.4
set OBS_DEPS_RELEASE_DATE=win-2022-05-23
set WINDOWS_DEPS_VERSION=windows-deps-2022-05-23
set WIN_CAPTURE_AUDIO_VERSION=2.2.2-beta

set BASE_DIR=%CD%
set BUILD_DIR=%BASE_DIR%\build
set OBS_STUDIO_BUILD_DIR=%BASE_DIR%\obs-studio-build
set OBS_STUDIO_DIR=%OBS_STUDIO_BUILD_DIR%\obs-studio-%OBS_STUDIO_VERSION%
set WINDOWS_DEPS_DIR=%OBS_STUDIO_BUILD_DIR%\windows-deps
set OBS_STUDIO_RELEASE_DIR=%OBS_STUDIO_BUILD_DIR%\obs-studio-release
set WIN_CAPTURE_AUDIO_ZIP=win-capture-audio-%WIN_CAPTURE_AUDIO_VERSION%
set WIN_CAPTURE_AUDIO_DIR=%OBS_STUDIO_BUILD_DIR%\win-capture-audio
set OBS_INSTALL_PREFIX=%OBS_STUDIO_BUILD_DIR%\build

echo "Building obs-studio"
mkdir "%OBS_STUDIO_BUILD_DIR%" 2>NUL
cd "%OBS_STUDIO_BUILD_DIR%"
if not exist "%OBS_STUDIO_DIR%" (
	git clone --recursive -b %OBS_STUDIO_VERSION% --single-branch https://github.com/obsproject/obs-studio.git "obs-studio-%OBS_STUDIO_VERSION%"
)
:: download obs windows deps
if not exist "%WINDOWS_DEPS_DIR%" (
	if not exist "%WINDOWS_DEPS_VERSION%.zip" (
		curl -kLO "https://github.com/obsproject/obs-deps/releases/download/%OBS_DEPS_RELEASE_DATE%/%WINDOWS_DEPS_VERSION%.zip" -f --retry 5 -C -
	)
	7z x "%WINDOWS_DEPS_VERSION%.zip" -o"%WINDOWS_DEPS_DIR%"
)
:: download the official release of obs studio (to copy signed win-capture)
if not exist "%OBS_STUDIO_RELEASE_DIR%" (
	if not exist "OBS-Studio-%OBS_STUDIO_VERSION%-Full-x64.zip" (
		curl -kLO "https://github.com/obsproject/obs-studio/releases/download/%OBS_STUDIO_VERSION%/OBS-Studio-%OBS_STUDIO_VERSION%-Full-x64.zip" -f --retry 5 -C -
	)
	7z x "OBS-Studio-%OBS_STUDIO_VERSION%-Full-x64.zip" -o"%OBS_STUDIO_RELEASE_DIR%"
)
:: download and include win-capture-audio
if not exist "%WIN_CAPTURE_AUDIO_DIR%.zip" (
	curl -kLO "https://github.com/bozbez/win-capture-audio/releases/download/v%WIN_CAPTURE_AUDIO_VERSION%/%WIN_CAPTURE_AUDIO_ZIP%.zip" -f --retry 5 -C -
	7z x "%WIN_CAPTURE_AUDIO_ZIP%.zip" -o"%WIN_CAPTURE_AUDIO_DIR%"
)

:: clean build folder if it exists from previous attempt
rmdir /s /q %OBS_INSTALL_PREFIX% 2>NUL

:: build for Win64
rmdir /s /q "%OBS_STUDIO_DIR%\build" 2>NUL
mkdir "%OBS_STUDIO_DIR%\build" 2>NUL
cd "%OBS_STUDIO_DIR%\build"

cmake -G"Visual Studio 17 2022" ^
	-DCMAKE_GENERATOR_PLATFORM="x64" ^
	-DCMAKE_INSTALL_PREFIX="%OBS_INSTALL_PREFIX%\win64" ^
	-DCMAKE_SYSTEM_VERSION=10.0 ^
	-DDepsPath="%WINDOWS_DEPS_DIR%\win64" ^
	-DDISABLE_UI=TRUE ^
	-DBUILD_BROWSER=OFF ^
	-DENABLE_SCRIPTING=OFF ^
	-DCMAKE_BUILD_TYPE="Release" ^
	..
cmake --build . --target install --config Release -v

:: build for Win32
rmdir /s /q "%OBS_STUDIO_DIR%\build" 2>NUL
mkdir "%OBS_STUDIO_DIR%\build" 2>NUL
cd "%OBS_STUDIO_DIR%\build"

cmake -G"Visual Studio 17 2022" ^
	-DCMAKE_GENERATOR_PLATFORM="win32" ^
	-DCMAKE_INSTALL_PREFIX="%OBS_INSTALL_PREFIX%\win32" ^
	-DCMAKE_SYSTEM_VERSION=10.0 ^
	-DDepsPath="%WINDOWS_DEPS_DIR%\win32" ^
	-DDISABLE_UI=TRUE ^
	-DBUILD_BROWSER=OFF ^
	-DENABLE_SCRIPTING=OFF ^
	-DCMAKE_BUILD_TYPE="Release" ^
	..
cmake --build . --target install --config Release -v

:: copy win32's data folder to win64 (required for game capturing 32bit applications)
xcopy "%OBS_INSTALL_PREFIX%\win32\data" "%OBS_INSTALL_PREFIX%\win64\data\" /E /Y
:: copy plugins & data to bin directory to make loadModule work
xcopy "%OBS_INSTALL_PREFIX%\win64\obs-plugins" "%OBS_INSTALL_PREFIX%\win64\bin\64bit\obs-plugins\" /E /Y /I
xcopy "%OBS_INSTALL_PREFIX%\win64\data" "%OBS_INSTALL_PREFIX%\win64\bin\64bit\data\" /E /Y /I
xcopy "%WIN_CAPTURE_AUDIO_DIR%" "%OBS_INSTALL_PREFIX%\win64\bin\64bit\" /E /Y /I
:: copy win-capture from official release to our build (because we need signed files for better compatibility)
xcopy "%OBS_STUDIO_RELEASE_DIR%\data\obs-plugins\win-capture" "%OBS_INSTALL_PREFIX%\win64\data\obs-plugins\win-capture\" /E /Y /I

cd %BASE_DIR%