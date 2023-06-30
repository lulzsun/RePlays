import React from 'react';
import ReactDOM from 'react-dom';
import './index.css';
import App from './App';

// https://github.com/Microsoft/TypeScript/issues/14975#issuecomment-290995090
// TODO: don't use enums, change this to a type (see ModalIcon for example)
// this is a dumb typescript fix for enums, not sure why but 
// if the two lines below aren't here, it will cause runtime issues
enum BookmarkType { Manual, Kill }
(window as { BookmarkType?: typeof BookmarkType }).BookmarkType = BookmarkType;

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
  interface External {
    sendMessage: (message: any) => void
    receiveMessage(callback: (message: string) => void): void
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
    metadata: {
      duration: number,
      kills?: number,
      assists?: number,
      deaths?: number,
    };
    folder: string,
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
  interface BookmarkInterface {
    id: number,
    type: BookmarkType
    time: number,
  }
  enum BookmarkType { // this exact same enum is to stop typescript IntelliSense from complaining
    Manual,
    Kill
  }
  // userSettings
  interface UserSettings {
    generalSettings: GeneralSettings
    captureSettings: CaptureSettings
    keybindingsSettings: KeybindingsSettings
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
    deviceVolume: number,
    denoiser?: boolean,
    isInput?: boolean
  }
  interface CaptureSettings {
    recordingMode: string,
    useDisplayCapture: boolean,
    useRecordingStartSound: boolean,
    maxScreenResolution: number, 
    resolution: number, frameRate: number, bitRate: number,
    encodersCache: string[]
    encoder: string,
    rateControlCache: string[]
    rateControl: string,
    inputDevices: AudioDevice[],
    inputDevicesCache: AudioDevice[],
    outputDevices: AudioDevice[],
    outputDevicesCache: AudioDevice[],
    hasNvidiaAudioSDK: boolean,
  }
  interface KeybindingsSettings {
      keybindings: Keybindings
  }
  interface UploadSettings {
    recentLinks: string[],
    streamableSettings: {
      email: string,
      password: string,
    },
    rePlaysSettings: {
      email: string,
      password: string,
    },
    youtubeSettings: {
      token: string,
    },
    localFolderSettings: {
      dir: string,
    },
    customUploaderSettings: {
      url: string,
      method: string,
      headers:  {Key: string, Value: string}[],
      urlparams: {Key: string, Value: string}[],
      responseType: string,
      responsePath: string,
    }
    
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

ReactDOM.render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
  document.getElementById('root')
);