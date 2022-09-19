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
  type ModalIcon = 'none' | 'info' | 'warning' | 'question' | 'success';
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
    folder: string,
  }
  interface Clip {
    id: number,
    start: number,
    duration: number,
    }
  interface Bookmark {
    id: number,
    time: number,
    }
  interface ContextMenuOptions {
    setItems: (items: ContextMenuItem[]) => any,
    setPosition: (position: ContextMenuPosition) => any,
  }
  interface ModalOptions {
    setData: (data: ModalData) => any,
    setOpen: (open: boolean) => any,
    isOpen: boolean,
    setConfirm: (confirm: () => any) => any,
  }
  interface ModalData {
    id?: string | "none";
    title?: string | "Title";
    context?: string | any;
    icon?: ModalIcon | 'none';
    progress?: number | 0;
    progressMax?: number | 0;
    cancel?: boolean | false;
  }
  interface ContextMenuItem {
    name: string, 
    onClick?: () => any;
  }
  interface ContextMenuPosition {
    x: number, 
    y: number,
  }

  // userSettings
  interface UserSettings {
    generalSettings: GeneralSettings
    captureSettings: CaptureSettings
    uploadSettings: UploadSettings
    storageSettings: StorageSettings
    keybindings: Keybindings
    detectionSettings: DetectionSettings
  }
  interface GeneralSettings {
    launchStartup: boolean, 
    startMinimized: boolean, 
    theme: string, 
    update: string, 
    updateChannel: string, 
    currentVersion: string, 
    latestVersion: string, 
  }
  interface AudioDevice {
    deviceId: string,
    deviceLabel: string,
  }
  interface CaptureSettings {
    recordingMode: string,
    resolution: number, frameRate: number, bitRate: number,
    encodersCache: string[]
    encoder: string,

    gameAudioVolume: number,
    micAudioVolume: number,
    inputDevice: AudioDevice,
    inputDevicesCache: AudioDevice[],
    outputDevice: AudioDevice,
    outputDevicesCache: AudioDevice[],
  }
  interface UploadSettings {
    recentLinks: string[],
    streamableSettings: {
      email: string,
      password: string,
    },
    youtubeSettings: {
      token: string,
    },
    localFolderSettings: {
      dir: string,
    },
  }
  interface StorageSettings {
    videoSaveDir: string,
    tempSaveDir: string,
    extraVideoSaveDir: string[],
    autoManageSpace: boolean,
    manageSpaceLimit: number,
    manageTimeLimit: number,
  }
  interface DetectionSettings{
    whitelist: CustomGame[],
    blacklist: string[],
  }
  interface CustomGame {
    gameExe: string,
    gameName: string,
  }
  interface Keybindings {
    StartStopRecording: string[],
    CreateBookmark: string[],
  }
}