# RePlays [![Downloads][download-badge]][download-link] [![Discord][discord-badge]][discord-link]

[download-badge]: https://img.shields.io/github/downloads/lulzsun/RePlays/total
[download-link]: https://github.com/lulzsun/RePlays/releases/

[discord-badge]: https://img.shields.io/discord/654698116917886986?label=Discord&logo=discord
[discord-link]: https://discordapp.com/invite/Qj2BmZX

RePlays is a free and open source program that automatically manages recording of detected running games, with a clip editor that allows for quick video sharing.

![Preview](/Resources/preview.png)

## Installation
1. Download the [latest Setup.exe from releases](https://github.com/lulzsun/RePlays/releases)
2. Open Setup.exe
3.  ...profit!

Note: Depending on your Windows version, you may need to download and install [Microsoft Edge WebView2 runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/#download-section).

## Development
Get started with contributing to the project with this quick [getting started guide](https://github.com/lulzsun/RePlays/wiki/Development:-Getting-Started)!

## Technology Stack
RePlays is powered by C# (.NET 7) and Typescript (React). Typescript is used for the interface (frontend), while C# is the core of the program (backend).

The interface is displayed using Microsoft Edge WebView2. It was chosen over Electron in favor of performance, while still allowing for a web-powered interface.

Recording functionality is currently powered using libobs, thanks to the [OBS](https://obsproject.com/) Project!

## End Notes
The project came together in homage to [Plays.tv](https://en.wikipedia.org/wiki/Plays.tv).

This software is not sponsored by or affiliated with Plays.tv or its affiliates. 

This project is licensed under the terms of the GNU license.
