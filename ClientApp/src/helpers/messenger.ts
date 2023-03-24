if(window.external.receiveMessage !== undefined) {
  window.external.receiveMessage(message => {
    let event = new Event("message");
    // @ts-ignore
    event.data = JSON.parse(message);
    window.dispatchEvent(event);
  });
}

export function postMessage(message: string, data?: any) {
  if(window.external.sendMessage !== undefined) {
    window.external.sendMessage(JSON.stringify({message, data: JSON.stringify(data)}));
    return;
  }
  if(window.chrome !== undefined) {
    window.chrome.webview.postMessage({message, data: JSON.stringify(data)});
  }
  console.log({message, data});
}

export function addEventListener(message: string, handler: EventListenerOrEventListenerObject) {
  if(window.external.receiveMessage !== undefined) {
    window.addEventListener(message, handler);
    return;
  }
  if(window.chrome !== undefined) {
    window.chrome.webview.addEventListener(message, handler);
  }
}

export function removeEventListener(message: string, handler: EventListenerOrEventListenerObject) {
  if(window.external.receiveMessage !== undefined) {
    window.removeEventListener(message, handler);
    return;
  }
  if(window.chrome !== undefined) {
    window.chrome.webview.removeEventListener(message, handler);
  }
}