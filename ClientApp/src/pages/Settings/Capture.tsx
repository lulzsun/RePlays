import { useEffect, useRef, useState } from "react";
import DropDownMenu from "../../components/DropDownMenu";
import HotkeySelector from "../../components/HotkeySelector";

interface Props {
  updateSettings: () => void;
  settings: CaptureSettings | undefined;
  keybindings: Keybindings | undefined;
}

export const Capture: React.FC<Props> = ({settings, keybindings, updateSettings}) => {
  const customVideoQuality = useRef<HTMLInputElement | null>(null);
  const [gameAudioVolume, setGameAudioVolume] = useState(settings!.gameAudioVolume);
  const [micAudioVolume, setMicAudioVolume] = useState(settings!.micAudioVolume);
  const [inputAudioDevices, setInputAudioDevices] = useState<any[]>();
  const [outputAudioDevices, setOutputAudioDevices] = useState<any[]>();
  const [availableEncoders, setAvailableEncoders] = useState<any[]>();

  useEffect(() => {
    if(settings == null) return;
    if(settings.inputDevicesCache == null) return;
    
    let ddmItems: any[] = [];
    
    settings.inputDevicesCache.forEach((device) => {
      console.log(device);

      if(device.deviceId === "default" && settings.inputDevice.deviceId === "") {
        settings.inputDevice.deviceId = device.deviceId; 
        settings.inputDevice.deviceLabel = device.deviceLabel;
        updateSettings();
      }
      
      ddmItems.push({name: device.deviceLabel, onClick: () => {
        settings.inputDevice.deviceId = device.deviceId; 
        settings.inputDevice.deviceLabel = device.deviceLabel; 
        updateSettings();
      }});
    });

    setInputAudioDevices(ddmItems);
    return;
  }, [setInputAudioDevices]);

  useEffect(() => {
    if(settings == null) return;
    if(settings.outputDevicesCache == null) return;
    
    let ddmItems: any[] = [];
    
    settings.outputDevicesCache.forEach((device) => {
      console.log(device);

      if(device.deviceId === "default" && settings.outputDevice.deviceId === "") {
        settings.outputDevice.deviceId = device.deviceId; 
        settings.outputDevice.deviceLabel = device.deviceLabel;
        updateSettings();
      }
      
      ddmItems.push({name: device.deviceLabel, onClick: () => {
        settings.outputDevice.deviceId = device.deviceId; 
        settings.outputDevice.deviceLabel = device.deviceLabel; 
        updateSettings();
      }});
    });

    setOutputAudioDevices(ddmItems);
    return;
  }, [setOutputAudioDevices]);

  useEffect(() => {
    if(settings == null) return;
    if(settings.encodersCache == null) return;
    
    let ddmItems: any[] = [];
    
    settings.encodersCache.forEach((encoder) => {
      
      ddmItems.push({name: encoder, onClick: () => {
        settings.encoder = encoder;
        updateSettings();
      }});
    });

    setAvailableEncoders(ddmItems);
    return;
  }, [setAvailableEncoders]);

	return (
    <div className="flex flex-col gap-2 font-medium text-base pb-7"> 
      <h1 className="font-semibold text-2xl">Capture Mode</h1>
      <div onChange={e => {if(settings) settings.recordingMode = (e?.target as HTMLInputElement).value; updateSettings();}}>
        <label className="inline-flex items-center">
          <input type="radio" name="recordMode" className="form-checkbox h-4 w-4 text-gray-600" value="automatic"
            defaultChecked={(settings?.recordingMode === "automatic" ? true : false)}/>
          <span className="px-2 text-gray-700 dark:text-gray-400">Automatic</span>
        </label>
        <label className="inline-flex items-center">
          <input type="radio" name="recordMode" className="form-checkbox h-4 w-4 text-gray-600" value="manual"
            defaultChecked={(settings?.recordingMode === "manual" ? true : false)}/>
          <span className="px-2 text-gray-700 dark:text-gray-400">Manual</span>
        </label>
        <label className="inline-flex items-center">
          <input type="radio" name="recordMode" className="form-checkbox h-4 w-4 text-gray-600" value="off"
            defaultChecked={(settings?.recordingMode === "off" ? true : false)}/>
          <span className="px-2 text-gray-700 dark:text-gray-400">Off</span>
        </label>
      </div>
      <div className="flex flex-col gap-1">
        Toggle Recording Keybind
        <HotkeySelector id="StartStopRecording" width="auto" keybind={keybindings?.StartStopRecording}/> 
      </div>
      <div className="flex flex-col gap-1">
         Bookmark Keybind
         <HotkeySelector id="CreateBookmark" width="auto" keybind={keybindings?.CreateBookmark} />
      </div>

      <h1 className="font-semibold text-2xl mt-4">Video Quality</h1>
      <div className="flex gap-4" 
        onChange={e => {
          let value = (e?.target as HTMLInputElement).value;
          switch (value) {
            case "low":
              settings!.resolution = 480; settings!.frameRate = 15; settings!.bitRate = 5;
              break;
            case "medium":
              settings!.resolution = 720; settings!.frameRate = 30; settings!.bitRate = 25;
              break;
            case "high":
              settings!.resolution = 1080; settings!.frameRate = 60; settings!.bitRate = 50;
              break;
            case "ultra":
              settings!.resolution = 1440; settings!.frameRate = 60; settings!.bitRate = 50;
              break;
            default:
              return;
          }
          updateSettings();
        }
      }>
        <label className="inline-flex items-center">
          <input type="radio" name="quality" className="form-checkbox h-4 w-4 text-gray-600" value="low"
            defaultChecked={(settings?.resolution === 480 && settings?.frameRate === 15 && settings?.bitRate === 5 ? true : false)}/>
          <span className="px-2 text-gray-700 dark:text-gray-400">Low</span>
        </label>
        <label className="inline-flex items-center">
          <input type="radio" name="quality" className="form-checkbox h-4 w-4 text-gray-600" value="medium"
            defaultChecked={(settings?.resolution === 720 && settings?.frameRate === 30 && settings?.bitRate === 25 ? true : false)}/>
          <span className="px-2 text-gray-700 dark:text-gray-400">Medium</span>
        </label>
        <label className="inline-flex items-center">
          <input type="radio" name="quality" className="form-checkbox h-4 w-4 text-gray-600" value="high"
            defaultChecked={(settings?.resolution === 1080 && settings?.frameRate === 60 && settings?.bitRate === 50 ? true : false)}/>
          <span className="px-2 text-gray-700 dark:text-gray-400">High</span>
        </label>
        <label className="inline-flex items-center">
            <input type="radio" name="quality" className="form-checkbox h-4 w-4 text-gray-600" value="ultra"
                defaultChecked={(settings?.resolution === 1440 && settings?.frameRate === 60 && settings?.bitRate === 50 ? true : false)} />
            <span className="px-2 text-gray-700 dark:text-gray-400">Ultra</span>
        </label>
        <label className="inline-flex items-center">
          <input type="radio" name="quality" className="form-checkbox h-4 w-4 text-gray-600" value="custom" ref={customVideoQuality}/>
          <span className="px-2 text-gray-700 dark:text-gray-400">Custom</span>
        </label>
      </div>
      <div className="flex gap-8">
      <div className="flex flex-col">
          Encoder
          <DropDownMenu text={(settings === undefined? "x264" : settings!.encoder)} width={"auto"}
          items={availableEncoders}/> 
        </div>
        <div className="flex flex-col">
          Resolution
          <DropDownMenu text={(settings === undefined ? "1080p" : settings.resolution + "p")} width={"auto"}
          items={[
            {name: "480p", onClick: () => {settings!.resolution = 480; customVideoQuality.current!.checked = true; updateSettings();}},
            {name: "720p", onClick: () => {settings!.resolution = 720; updateSettings();}},
            {name: "1080p", onClick: () => {settings!.resolution = 1080; updateSettings();}},
            {name: "1440p", onClick: () => {settings!.resolution = 1440; updateSettings();}},
          ]}/> 
        </div>
        <div className="flex flex-col">
          Framerate
          <DropDownMenu text={(settings === undefined ? "60 fps" : settings.frameRate + " fps")} width={"auto"}
          items={[
            {name: "15 fps", onClick: () => {settings!.frameRate = 15; customVideoQuality.current!.checked = true; updateSettings();}},
            {name: "30 fps", onClick: () => {settings!.frameRate = 30; customVideoQuality.current!.checked = true; updateSettings();}},
            {name: "60 fps", onClick: () => {settings!.frameRate = 60; customVideoQuality.current!.checked = true; updateSettings();}},
            //{name: "144 fps", onClick: () => {settings!.frameRate = 144; updateSettings();}},
          ]}/> 
        </div>
        <div className="flex flex-col">
          Bitrate
          <DropDownMenu text={(settings === undefined ? "50 MB/s" : settings.bitRate + " MB/s")} width={"auto"} zIndex={51}
          items={[
            {name: "5 MB/s", onClick: () => {settings!.bitRate = 5; customVideoQuality.current!.checked = true; updateSettings();}},
            {name: "10 MB/s", onClick: () => {settings!.bitRate = 10; customVideoQuality.current!.checked = true; updateSettings();}},
            {name: "15 MB/s", onClick: () => {settings!.bitRate = 15; customVideoQuality.current!.checked = true; updateSettings();}},
            {name: "20 MB/s", onClick: () => {settings!.bitRate = 20; customVideoQuality.current!.checked = true; updateSettings();}},
            {name: "25 MB/s", onClick: () => {settings!.bitRate = 25; customVideoQuality.current!.checked = true; updateSettings();}},
            {name: "30 MB/s", onClick: () => {settings!.bitRate = 30; customVideoQuality.current!.checked = true; updateSettings();}},
            {name: "35 MB/s", onClick: () => {settings!.bitRate = 35; customVideoQuality.current!.checked = true; updateSettings();}},
            {name: "40 MB/s", onClick: () => {settings!.bitRate = 40; customVideoQuality.current!.checked = true; updateSettings();}},
            {name: "45 MB/s", onClick: () => {settings!.bitRate = 45; customVideoQuality.current!.checked = true; updateSettings();}},
            {name: "50 MB/s", onClick: () => {settings!.bitRate = 50; customVideoQuality.current!.checked = true; updateSettings();}},
          ]}/> 
        </div>
      </div>

      <h1 className="font-semibold text-2xl mt-4">Game Audio Settings</h1>
      <div className="flex gap-2">
        <svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" fill="currentColor" viewBox="0 0 16 16">
          <path d="M11.536 14.01A8.473 8.473 0 0 0 14.026 8a8.473 8.473 0 0 0-2.49-6.01l-.708.707A7.476 7.476 0 0 1 13.025 8c0 2.071-.84 3.946-2.197 5.303l.708.707z"/>
          <path d="M10.121 12.596A6.48 6.48 0 0 0 12.025 8a6.48 6.48 0 0 0-1.904-4.596l-.707.707A5.483 5.483 0 0 1 11.025 8a5.483 5.483 0 0 1-1.61 3.89l.706.706z"/>
          <path d="M10.025 8a4.486 4.486 0 0 1-1.318 3.182L8 10.475A3.489 3.489 0 0 0 9.025 8c0-.966-.392-1.841-1.025-2.475l.707-.707A4.486 4.486 0 0 1 10.025 8zM7 4a.5.5 0 0 0-.812-.39L3.825 5.5H1.5A.5.5 0 0 0 1 6v4a.5.5 0 0 0 .5.5h2.325l2.363 1.89A.5.5 0 0 0 7 12V4zM4.312 6.39 6 5.04v5.92L4.312 9.61A.5.5 0 0 0 4 9.5H2v-3h2a.5.5 0 0 0 .312-.11z"/>
        </svg>
        <input className="w-72" type="range" min="0" max="100" step="1" defaultValue={settings === undefined ? 100 : settings!.gameAudioVolume}
          onChange={(e) => { let value = parseInt((e.target as HTMLInputElement).value); setGameAudioVolume(value); settings!.gameAudioVolume = value; updateSettings();}}/>
        {gameAudioVolume + "%"}
      </div>
      
      <h1 className="font-semibold text-2xl mt-4">Microphone Audio Settings</h1>
      <div className="flex gap-2">
        <svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" fill="currentColor" viewBox="0 0 16 16">
          <path d="M11.536 14.01A8.473 8.473 0 0 0 14.026 8a8.473 8.473 0 0 0-2.49-6.01l-.708.707A7.476 7.476 0 0 1 13.025 8c0 2.071-.84 3.946-2.197 5.303l.708.707z"/>
          <path d="M10.121 12.596A6.48 6.48 0 0 0 12.025 8a6.48 6.48 0 0 0-1.904-4.596l-.707.707A5.483 5.483 0 0 1 11.025 8a5.483 5.483 0 0 1-1.61 3.89l.706.706z"/>
          <path d="M10.025 8a4.486 4.486 0 0 1-1.318 3.182L8 10.475A3.489 3.489 0 0 0 9.025 8c0-.966-.392-1.841-1.025-2.475l.707-.707A4.486 4.486 0 0 1 10.025 8zM7 4a.5.5 0 0 0-.812-.39L3.825 5.5H1.5A.5.5 0 0 0 1 6v4a.5.5 0 0 0 .5.5h2.325l2.363 1.89A.5.5 0 0 0 7 12V4zM4.312 6.39 6 5.04v5.92L4.312 9.61A.5.5 0 0 0 4 9.5H2v-3h2a.5.5 0 0 0 .312-.11z"/>
        </svg>
        <input className="w-72" type="range" min="0" max="100" step="1" defaultValue={settings === undefined ? 100 : settings!.micAudioVolume}
          onChange={(e) => { let value = parseInt((e.target as HTMLInputElement).value); setMicAudioVolume(value); settings!.micAudioVolume = value; updateSettings();}}/>
        {micAudioVolume + "%"}
      </div>
      <div className="flex flex-col">
        Output Source
        <DropDownMenu text={(settings === undefined ? "Default Device" : settings.outputDevice.deviceLabel)} width={"auto"}
        items={outputAudioDevices} zIndex={51}/> 
      </div>
      <div className="flex flex-col">
        Input Source
        <DropDownMenu text={(settings === undefined ? "Default Device" : settings.inputDevice.deviceLabel)} width={"auto"}
        items={inputAudioDevices}/> 
      </div>
    </div>
	)
}

export default Capture;