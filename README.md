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
RePlays is powered by C# (.NET 5) and Typescript (React). Typescript is used for the interface (frontend), while C# is the core of the program (backend).

The interface is displayed using Microsoft Edge WebView2. It was chosen over Electron in favor of performance, while still allowing for a web-powered interface.

The project came together as homage to [Plays.tv](https://en.wikipedia.org/wiki/Plays.tv), and is even powered by one of it's modules.

This software is not sponsored by or affiliated with Plays.tv or its affiliates. 
