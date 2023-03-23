if(window.external.receiveMessage !== undefined) {
  window.external.receiveMessage(message => window.dispatchEvent(new Event(message)));
}

export function postMessage(message: string, data?: any) {
  if(window.chrome !== undefined) {
    window.chrome.webview.postMessage({message, data: JSON.stringify(data)});
  }
  if(window.external.sendMessage !== undefined) {
    window.external.sendMessage(JSON.stringify({message, data: JSON.stringify(data)}));
  }
  console.log({message, data});
}

export function addEventListener(message: string, handler: EventListenerOrEventListenerObject) {
  if(window.chrome !== undefined) {
    window.chrome.webview.addEventListener(message, handler);
  }
  if(window.external.receiveMessage !== undefined) {
    window.addEventListener(message, handler);
  }
}

export function removeEventListener(message: string, handler: EventListenerOrEventListenerObject) {
  if(window.chrome !== undefined) {
    window.chrome.webview.removeEventListener(message, handler);
  }
  if(window.external.receiveMessage !== undefined) {
    window.removeEventListener(message, handler);
  }
}