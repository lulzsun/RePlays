if(window.external?.receiveMessage == undefined || window.chrome?.webview?.postMessage == undefined) {
  const socket = new WebSocket('ws://localhost:3001/ws');
  // Connection opened
  socket.addEventListener('open', (event) => {
    // @ts-ignore
    window.external = {};
    window.external.sendMessage = (message: string) => {
      socket.send(message);
    }
    // @ts-ignore
    window.external.receiveMessage = {};
  });
  // Listen for messages
  socket.addEventListener('message', (message) => {
    let event = new Event("message");
    // @ts-ignore
    event.data = JSON.parse(message.data);
    window.dispatchEvent(event);
  });
  // Connection closed
  socket.addEventListener('close', (event) => {
    console.log('WebSocket connection closed:', event);
  });
}
else if(window.external?.receiveMessage !== undefined) {
  window.external.receiveMessage(message => {
    let event = new Event("message");
    // @ts-ignore
    event.data = JSON.parse(message);
    window.dispatchEvent(event);
  });
}

export function postMessage(message: string, data?: any) {
  let userAgent = window.navigator?.userAgent ?? null;
  if(window.chrome?.webview?.postMessage !== undefined) {
    window.chrome.webview.postMessage({message, data: JSON.stringify(data), userAgent});
  }
  if(window.external == undefined) {
    return;
  }
  if(window.external.sendMessage !== undefined) {
    window.external.sendMessage(JSON.stringify({message, data: JSON.stringify(data), userAgent}));
  }
  console.log({message, data, userAgent});
}

export function addEventListener(message: string, handler: EventListenerOrEventListenerObject) {
  if(window.chrome?.webview?.postMessage !== undefined) {
    window.chrome.webview.addEventListener(message, handler);
  }
  if(window.external == undefined) {
    return;
  }
  if(window.external.receiveMessage !== undefined) {
    window.addEventListener(message, handler);
    return;
  }
}

export function removeEventListener(message: string, handler: EventListenerOrEventListenerObject) {
  if(window.chrome !== undefined) {
    window.chrome.webview.removeEventListener(message, handler);
  }
  if(window.external == undefined) {
    return;
  }
  if(window.external.receiveMessage !== undefined) {
    window.removeEventListener(message, handler);
    return;
  }
}