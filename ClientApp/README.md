# RePlays - ClientApp

This is the web front end portion of RePlays.

It is built upon React and Vite. Recommended Node version 17+.

## Intracommunication

> How does the frontend communicate with the backend?

On Windows, we communicate through [WebView2 messaging](https://learn.microsoft.com/en-us/microsoft-edge/webview2/how-to/communicate-btwn-web-native). We pass json object strings back and forth from native (C#) to web.

(EXPERIMENTAL) On Linux, we communicate the same way, but with the WebKitGTK+ 2 (at the time of writing, we are using [Photino](https://www.tryphotino.io/) to get the native browser control of the OS)

Linux communication is incomplete, thus warning that you do not try to develop features that require native calls. Only develop front-end on unix systems for basic frontend work.

## Learn More

To learn React, check out the [React documentation](https://reactjs.org/).
