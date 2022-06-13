@echo off

set OBS_STUDIO_VERSION=26.0.2-patch1
set WINDOWS_DEPS_VERSION=dependencies2017

set BASE_DIR=%CD%
set BUILD_DIR=%BASE_DIR%\build
set OBS_STUDIO_BUILD_DIR=%BASE_DIR%\obs-studio-build
set OBS_STUDIO_DIR=%OBS_STUDIO_BUILD_DIR%\obs-studio-%OBS_STUDIO_VERSION%
set WINDOWS_DEPS_DIR=%OBS_STUDIO_BUILD_DIR%\%WINDOWS_DEPS_VERSION%
set OBS_INSTALL_PREFIX=%OBS_STUDIO_BUILD_DIR%\obs-installed
set PREBUILD_DIR=%BASE_DIR%\prebuild

set BUILD_TYPE=%1
set BUILD_OBS_STUDIO=false
set BUILD_OBS_NODE=false
if "%BUILD_TYPE%" == "all" (
    set BUILD_OBS_STUDIO=true
    set BUILD_OBS_NODE=true
)
if "%BUILD_TYPE%" == "obs-studio" set BUILD_OBS_STUDIO=true
if "%BUILD_TYPE%" == "obs-node" set BUILD_OBS_NODE=true
if not "%BUILD_OBS_STUDIO%" == "true" (
    if not "%BUILD_OBS_NODE%" == "true" (
        echo "The first argument should be 'all', 'obs-studio' or 'obs-node'"
        exit /B 1
    )
)

set RELEASE_TYPE=%2
if "%RELEASE_TYPE%" == "" set RELEASE_TYPE=Release
if not "%RELEASE_TYPE%" == "Release" (
    if not "%RELEASE_TYPE%" == "Debug" (
        echo "The second argument should be 'Release' or 'Debug'"
        exit /B 1
    )
)

mkdir "%PREBUILD_DIR%" 2>NUL
if "%BUILD_OBS_STUDIO%" == "true" (
    echo "Building obs-studio"
    mkdir "%OBS_STUDIO_BUILD_DIR%" 2>NUL
    cd "%OBS_STUDIO_BUILD_DIR%"
    if not exist "%OBS_STUDIO_DIR%" (
        git clone --recursive -b %OBS_STUDIO_VERSION% --single-branch https://github.com/MengLi619/obs-studio.git "obs-studio-%OBS_STUDIO_VERSION%"
    )
    if not exist "%WINDOWS_DEPS_DIR%" (
        if not exist "%WINDOWS_DEPS_VERSION%.zip" (
            curl -kLO "https://obsproject.com/downloads/%WINDOWS_DEPS_VERSION%.zip" -f --retry 5 -C -
        )
        7z x "%WINDOWS_DEPS_VERSION%.zip" -o"%WINDOWS_DEPS_DIR%"
    )
    mkdir "%OBS_STUDIO_DIR%\build" 2>NUL
    cd "%OBS_STUDIO_DIR%\build"
    cmake -G"Visual Studio 16 2019" ^
        -A x64 ^
        -DCMAKE_INSTALL_PREFIX="%OBS_INSTALL_PREFIX%" ^
        -DCMAKE_SYSTEM_VERSION=10.0 ^
        -DDepsPath="%WINDOWS_DEPS_DIR%\win64" ^
        -DDISABLE_UI=TRUE ^
        -DDISABLE_PYTHON=ON ^
        -DCMAKE_BUILD_TYPE="%RELEASE_TYPE%" ^
        ..
    rmdir /s /q %OBS_INSTALL_PREFIX% 2>NUL
    cmake --build . --target install --config %RELEASE_TYPE% -v

    cd "%BASE_DIR%"
    :: Copy obs files to prebuild
    echo "Copy obs installed files to prebuild"
    rmdir /s /q "%PREBUILD_DIR%\obs-studio" 2>NUL
    mkdir "%PREBUILD_DIR%\obs-studio" 2>NUL
    xcopy "%OBS_INSTALL_PREFIX%\bin" "%PREBUILD_DIR%\obs-studio\bin\" /E /Y
    xcopy "%OBS_INSTALL_PREFIX%\data" "%PREBUILD_DIR%\obs-studio\data\" /E /Y
    :: copy plugins to bin directory to make loadModule work
    xcopy "%OBS_INSTALL_PREFIX%\obs-plugins\64bit" "%PREBUILD_DIR%\obs-studio\bin\64bit\" /E /Y
)
