export function postMessage(message: string, data?: any) {
  if(window.chrome === undefined) return;
  console.log({message, data});
  window.chrome.webview.postMessage({message, data: JSON.stringify(data)});
}

export function addEventListener(message: string, handler: EventListenerOrEventListenerObject) {
  if(window.chrome === undefined) return;
  window.chrome.webview.addEventListener(message, handler);
}

export function removeEventListener(message: string, handler: EventListenerOrEventListenerObject) {
  if(window.chrome === undefined) return;
  window.chrome.webview.removeEventListener(message, handler);
}