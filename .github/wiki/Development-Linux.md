The following instructions are currently work-in-progress, feel free to make a pull request with changes to this page located [here](https://github.com/lulzsun/RePlays/blob/main/.github/wiki/Development-Linux.md).

# Install

This guide will be written for Arch based Linux distros. Feel free to add sections for other specific Linux distros.

You need the following packages for both runtime and development:

```bash
$ sudo pacman -Syu dotnet-runtime dotnet-sdk gtk3 webkit2gtk gst-libav libayatana-appindicator
```

You will also need Node 18+ installed. Recommended to install using a version manager such as nvm:

```bash
$ nvm install 18.14.2
```

# IDE

This development guide will be tailored for VSCode, but feel free to follow along with your favorite code editor.

It is recommended to use VSCode, as plugins and project files are setup to be used with it.

# Debug

Start debugging by pressing F5 on any `.cs` file. This will launch RePlays but not launch the front end dev server (vite).

Set working directory to `ClientApp` and run the following to initialize the front end project:

```bash
$ npm ci
$ npm run start
```
