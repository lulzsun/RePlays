import React from 'react';
import ReactDOM from 'react-dom';
import './index.css';
import App from './App';

ReactDOM.render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
  document.getElementById('root')
);

declare global {
  interface Window { 
    chrome: {
      webview: {
        postMessage: (message: any) => void
        addEventListener: (message: string, handler: EventListenerOrEventListenerObject) => void
        removeEventListener: (message: string, handler: EventListenerOrEventListenerObject) => void
      }
    }
  }
  interface Webview2Event extends Event {
    data: {
      data: string,
      message: string,
    }
  }
  interface Video {
    game: string,
    fileName: string,
    size: number,
    date: string,
    thumbnail: string,
  }
  interface Clip {
    id: number,
    start: number,
    duration: number,
  }
  interface ContextMenuOptions {
    setItems: (items: ContextMenuItem[]) => any,
    setPosition: (position: ContextMenuPosition) => any,
  }
  interface ContextMenuItem {
    name: string, 
    onClick?: () => any;
  }
  interface ContextMenuPosition {
    x: number, 
    y: number,
  }
}