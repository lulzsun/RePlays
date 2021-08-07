declare global {
  interface Window { 
    chrome: {
      webview: {
        postMessage: (message: any) => void
        addEventListener: (message: string, event: Event) => void
      }
    }
  }
}

export function postMessage(message: string, data?: any) {
  if(window.chrome === undefined) return;
  window.chrome.webview.postMessage({message, data});
}

export function addEventListener(message: string, event: Event) {
  if(window.chrome === undefined) return;
  window.chrome.webview.addEventListener(message, event);
}