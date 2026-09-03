@echo off
setlocal enabledelayedexpansion

set "OBS_STUDIO_VERSION=32.0.2"

set "BASE_DIR=%CD%"
set "OBS_STUDIO_BUILD_DIR=%BASE_DIR%\obs-studio-build"
set "OBS_STUDIO_DIR=%OBS_STUDIO_BUILD_DIR%\obs-studio-%OBS_STUDIO_VERSION%"
set "OBS_STUDIO_RELEASE_ZIP=%OBS_STUDIO_BUILD_DIR%\OBS-Studio-%OBS_STUDIO_VERSION%-Windows-x64.zip"
set "OBS_STUDIO_RELEASE_DIR=%OBS_STUDIO_BUILD_DIR%\obs-studio-release"
set "OBS_INSTALL_PREFIX=%OBS_STUDIO_DIR%\build"
set "OBS_RUNDIR=%OBS_INSTALL_PREFIX%\rundir\Release\bin\64bit"

echo Building obs-studio-%OBS_STUDIO_VERSION%
mkdir "%OBS_STUDIO_BUILD_DIR%" 2>NUL
cd /d "%OBS_STUDIO_BUILD_DIR%" || goto :error
if not exist "%OBS_STUDIO_DIR%" (
	git clone --recursive -b %OBS_STUDIO_VERSION% --single-branch https://github.com/obsproject/obs-studio.git "obs-studio-%OBS_STUDIO_VERSION%" || goto :error
)

:: download the official release of obs studio (to copy signed win-capture hooks)
:: the download and the extraction each go to a ".part" location and are only
:: renamed into place once complete, so an interrupted run can never leave a
:: truncated zip or a half extracted folder behind that later runs would reuse
if not exist "%OBS_STUDIO_RELEASE_DIR%\" (
	if not exist "%OBS_STUDIO_RELEASE_ZIP%" (
		curl -kL -f --retry 5 -o "%OBS_STUDIO_RELEASE_ZIP%.part" "https://github.com/obsproject/obs-studio/releases/download/%OBS_STUDIO_VERSION%/OBS-Studio-%OBS_STUDIO_VERSION%-Windows-x64.zip" || (
			del /q "%OBS_STUDIO_RELEASE_ZIP%.part" 2>NUL
			goto :error
		)
		move /y "%OBS_STUDIO_RELEASE_ZIP%.part" "%OBS_STUDIO_RELEASE_ZIP%" >NUL || goto :error
	)
	rmdir /s /q "%OBS_STUDIO_RELEASE_DIR%.part" 2>NUL
	7z x "%OBS_STUDIO_RELEASE_ZIP%" -o"%OBS_STUDIO_RELEASE_DIR%.part" -y >NUL || (
		rmdir /s /q "%OBS_STUDIO_RELEASE_DIR%.part" 2>NUL
		goto :error
	)
	move /y "%OBS_STUDIO_RELEASE_DIR%.part" "%OBS_STUDIO_RELEASE_DIR%" >NUL || goto :error
)

:: clean build folders if they exist from a previous attempt (build_x86 is the
:: 32-bit sub-build that obs configures itself for the win-capture helpers)
:: rmdir does not report failures through its exit code, so check the result
if exist "%OBS_INSTALL_PREFIX%\" rmdir /s /q "%OBS_INSTALL_PREFIX%"
if exist "%OBS_INSTALL_PREFIX%\" (
	echo Fatal error: could not remove the previous build folder "%OBS_INSTALL_PREFIX%"
	goto :error
)
if exist "%OBS_STUDIO_DIR%\build_x86\" rmdir /s /q "%OBS_STUDIO_DIR%\build_x86"
if exist "%OBS_STUDIO_DIR%\build_x86\" (
	echo Fatal error: could not remove the previous build folder "%OBS_STUDIO_DIR%\build_x86"
	goto :error
)
mkdir "%OBS_INSTALL_PREFIX%" || goto :error
cd /d "%OBS_STUDIO_DIR%" || goto :error

:: build for Win64
:: RePlays only needs libobs plus the capture, audio, encoder, filter and
:: file output modules. everything else (streaming outputs, capture cards,
:: websocket, virtual camera, text, nvidia fx, ...) is turned off so a
:: dependency or toolchain hiccup in a module we never load cannot break the build
cmake -S . -B "%OBS_INSTALL_PREFIX%" --preset windows-x64 ^
	-DENABLE_BROWSER:BOOL=OFF ^
	-DENABLE_VLC:BOOL=OFF ^
	-DENABLE_FRONTEND:BOOL=OFF ^
	-DENABLE_UI:BOOL=OFF ^
	-DENABLE_VST:BOOL=OFF ^
	-DENABLE_SCRIPTING:BOOL=OFF ^
	-DENABLE_AJA:BOOL=OFF ^
	-DENABLE_DECKLINK:BOOL=OFF ^
	-DENABLE_COREAUDIO_ENCODER:BOOL=OFF ^
	-DENABLE_WEBSOCKET:BOOL=OFF ^
	-DENABLE_WEBRTC:BOOL=OFF ^
	-DENABLE_NEW_MPEGTS_OUTPUT:BOOL=OFF ^
	-DENABLE_SERVICE_UPDATES:BOOL=OFF ^
	-DENABLE_VIRTUALCAM:BOOL=OFF ^
	-DENABLE_FREETYPE:BOOL=OFF ^
	-DENABLE_NVAFX:BOOL=OFF ^
	-DENABLE_NVVFX:BOOL=OFF ^
	|| goto :error

cmake --build "%OBS_INSTALL_PREFIX%" --config Release --parallel || goto :error

cd /d "%BASE_DIR%" || goto :error

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

if "%newest_dir%"=="" (
	echo Fatal error: could not find the obs-deps x64 directory under "%deps_dir%"
	goto :error
)

:: Set the variable to the newest directory
set "WINDOWS_DEPS_DIR=%deps_dir%\%newest_dir%"

:: copy the runtime dependencies (ffmpeg, x264, curl, zlib) next to obs.dll.
:: excluded: swig.exe (build tool), lua51.dll (scripting) and datachannel.dll
:: (webrtc), which nothing imports now that those features are disabled above.
:: librist.dll and srt.dll stay because avformat itself links against them
call :copy "%WINDOWS_DEPS_DIR%\bin" "%OBS_RUNDIR%" /E /IS /IT /XD "%WINDOWS_DEPS_DIR%\bin\Lib" /XF *.pdb swig.exe lua51.dll datachannel.dll || goto :error
:: copy plugins & data to bin directory to make loadModule work
call :copy "%OBS_INSTALL_PREFIX%\rundir\Release\obs-plugins" "%OBS_RUNDIR%\obs-plugins" /E /IS /IT /XF *.ini *.pdb || goto :error
call :copy "%OBS_INSTALL_PREFIX%\rundir\Release\data" "%OBS_RUNDIR%\data" /E /IS /IT /XF *.ini *.pdb || goto :error
:: copy win-capture from official release to our build (because we need signed files for better compatibility)
call :copy "%OBS_STUDIO_RELEASE_DIR%\data\obs-plugins\win-capture" "%OBS_RUNDIR%\data\obs-plugins\win-capture" /E /IS /IT /XF *.ini *.pdb || goto :error
:: the encoder probes and the muxer are spawned as separate processes, so replace
:: the ones we just built with the signed official copies until we can sign our own
call :copy "%OBS_STUDIO_RELEASE_DIR%\bin\64bit" "%OBS_RUNDIR%" "obs-amf-test.exe" "obs-nvenc-test.exe" "obs-qsv-test.exe" "obs-ffmpeg-mux.exe" /IS /IT || goto :error

:: everything RePlays loads at runtime has to be here, otherwise the publish
:: would silently ship a broken libobs (see issue #287)
set "MISSING="
for %%f in (
	obs.dll
	libobs-d3d11.dll
	libobs-winrt.dll
	w32-pthreads.dll
	avcodec-61.dll
	avformat-61.dll
	avutil-59.dll
	swresample-5.dll
	swscale-8.dll
	libx264-164.dll
	libcurl.dll
	librist.dll
	srt.dll
	zlib.dll
	ffmpeg.exe
	ffprobe.exe
	obs-ffmpeg-mux.exe
	obs-amf-test.exe
	obs-nvenc-test.exe
	obs-qsv-test.exe
	obs-plugins\64bit\obs-ffmpeg.dll
	obs-plugins\64bit\obs-filters.dll
	obs-plugins\64bit\obs-nvenc.dll
	obs-plugins\64bit\obs-qsv11.dll
	obs-plugins\64bit\obs-x264.dll
	obs-plugins\64bit\win-capture.dll
	obs-plugins\64bit\win-wasapi.dll
	data\libobs\default.effect
	data\obs-plugins\win-capture\graphics-hook32.dll
	data\obs-plugins\win-capture\graphics-hook64.dll
	data\obs-plugins\win-capture\inject-helper32.exe
	data\obs-plugins\win-capture\inject-helper64.exe
	data\obs-plugins\win-capture\get-graphics-offsets32.exe
	data\obs-plugins\win-capture\get-graphics-offsets64.exe
) do (
	if not exist "%OBS_RUNDIR%\%%~f" (
		echo Fatal error: expected build output is missing: %%~f
		set "MISSING=1"
	)
)
if defined MISSING goto :error

echo OBS build completed successfully
exit /b 0

:: usage: call :copy <source> <destination> [robocopy options]
:: robocopy exit codes below 8 only report what was copied, 8 and above are failures
:copy
robocopy %* /R:0 /W:0 /NFL /NDL /NP
if %ERRORLEVEL% GEQ 8 (
	echo Fatal error: failed to copy %1 to %2 ^(robocopy code %ERRORLEVEL%^)
	exit /b 1
)
exit /b 0

:error
echo Fatal error: libobs build failed
cd /d "%BASE_DIR%"
exit /b 1
