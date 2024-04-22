# Work-in-progress

# Install

This guide will be written for Arch based Linux distros. Feel free to add sections for other specific Linux distros.

You need the following packages for both runtime and development:

```bash
$ sudo pacman -Syu dotnet-runtime dotnet-sdk gtk3 webkit2gtk gst-libav libayatana-appindicator
```

You will also need Node 18+ installed.

# IDE

While the choice of IDE is a personal preference, It is recommended to use VS Code, as plugins and project files are setup to be used with it. This development guide will also be tailored for VS Code.

# Debug

Start debugging by pressing F5 on any `.cs` file. This will launch RePlays but not launch the front end dev server (vite). Set working directory to `ClientApp` and run the following to initialize the front end project:

```bash
$ npm ci
$ npm run start
```
