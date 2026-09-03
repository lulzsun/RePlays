Want to help contribute to RePlays? Great! This page will help you get started on how to setup your dev environment and debug the RePlays app.

Feel free to reach out on Discord if you have issues or questions about the setup or general development.

Prerequisites:

*   Visual Studio 17 2022
    *   [.NET SDK 8.0.100](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-8.0.100-windows-x64-installer)
*   Node 18+
*   Knowledge in C# and Typescript (React.js)

Optional:

*   Visual Studio Code
*   OBS Studio 30.0.0
*   Cmake, git, & 7zip (if you are planning to build libobs yourself)
    *   Make sure 7z is in your system environment PATH

Visual Studio will be the main IDE for this guide, however feel free to follow along using Visual Studio Code, Jetbrains Rider, etc.

# 1. Clone the repository

Using git cli or using your preferred method of cloning. Make sure to include submodules.

*   `git clone --recursive https://github.com/lulzsun/RePlays.git`

# 2. Open the Visual Studio project

Open `RePlays.sln` using Visual Studio

# 3. (Optional) Run npm ci

*   In the project's ClientApp folder, run `npm ci` (from cmd/powershell)
    *   This will download the necessary node modules for the React.js portion of the app

This step can be skipped and is optional because when you start debugging, it will automatically run this command for you if you haven't already

# 4. Copy libobs to Debug folder

libobs is necessary for debugging and is not included with the project, the required files must be placed here:

`~/bin/Debug/net8.0-windows/win-x64/`

At the time of writing this guide, you have a few options for where to get libobs:

1.  Build it yourself (**RECOMMENDED**)
2.  Copy libobs from other sources

### 1. Build libobs yourself

This is the best way if you want to match similar results to production.

There is an included build script in the root folder to make life a lot easier.

This requires you to have Cmake, git, 7zip, and Visual Studio 17 2022 installed on your system.

Make sure Cmake, git and 7zip are in your system environmental variables (so they can be accessed through cmd)

Provided that you have all this, the build script is a one-click solution.

1.  Run `build-libobs.cmd` in cmd/powershell from root folder
2.  Build should be successful if the file `~/obs-studio-build/obs-studio-release/bin/64bit/obs.dll` exists

The script takes care of everything (cloning, building, downloading and copying certain third party obs plugins if necessary) for you and is what production builds run, so this will be matching results from production.

### 2. Copy libobs from RePlays/OBS Studio/Streamlabs

This step will be using Streamlabs as an example, you can download their libobs release [here](https://obsstudios3.streamlabs.com/libobs-windows64-release-27.5.32.7z).

NOTE: Copying other versions of libobs that do not match the current release may cause different debugging results. It is recommended that you build libobs yourself or at the very least copy from RePlays production release.

The required files and folders are as follows:

    - /packed_build
        - /bin
            - /64bit
                - obs.dll & ~dependencies/.dlls, etc. files~
        - /cmake
        - /data
        - /include
        - /obs-plugins

and must be copied to the debug folder like so:

    - /bin/Debug/net8.0-windows/win-x64
        - /data
        - /obs-plugins
        - obs.dll & ~dependencies/.dlls, etc. files~

# 5. Start debugging!

You are now ready to start debugging! Upon initial debugging, the msbuild pre-build will take care of any missing files and provide warnings/errors if those files cannot be found (libobs files, React app files).

# 6. Building a release

To manually build a release on your machine, run the following command at the root of the project

`dotnet publish /p:Configuration=Release /p:Version=X.X.X /p:PublishProfile=FolderProfile`

You must specific a version in the numeral format of X.X.X (e.g. 1.69.420). Version numbering does not really matter for local release builds.

The build will make use of Squirrel and create deltas or full packages, including a Setup file. You can find these files in `/bin/Deployment/Releases/`.

You can learn more about the Squirrel deployment process [here](https://github.com/Squirrel/Squirrel.Windows/blob/develop/docs/getting-started/0-overview.md#overview).

# 7. (Optional) Using Visual Studio Code

Visual Studio Code may be preferred when working with the React.js portion of the app. The React.js is the interface or front-end of the application.

Open `~/ClientApp` in Visual Studio Code and begin working on the interface like any normal React.js workflow!

You can also use VSCode to debug and make changes to C# related files, however the VSCode debug tools may not be as powerful and convenient compared to Visual Studio.
