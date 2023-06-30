import { useEffect, useRef, useState } from "react";
import DropDownMenu from "../../components/DropDownMenu";
import AudioDevice from "../../components/AudioDevice";

interface Props {
  updateSettings: () => void;
  settings: CaptureSettings | undefined;
}

export const Capture: React.FC<Props> = ({settings, updateSettings}) => {
  const customVideoQuality = useRef<HTMLInputElement | null>(null);
  const [audioDevices, setAudioDevices] = useState<any[]>();
  const [availableEncoders, setAvailableEncoders] = useState<any[]>();
  const [availableRateControls, setAvailableRateControls] = useState<any[]>();

  useEffect(() => {
    if(settings == null) return;
    if(settings.inputDevicesCache == null || settings.outputDevicesCache == null) return;
    
    let ddmItems: any[] = [];
    
    settings.inputDevicesCache.forEach((device) => {
      // by default, if there does not exist any devices, we will push default device
      // this behaviour may not be ideal if the user does not want any audio devices 
      //  (i.e. they removed all devices)
      if(device.deviceId === "default" && settings.inputDevices.length === 0) {
        settings.inputDevices.push({
          deviceId: device.deviceId,
          deviceLabel: device.deviceLabel,
          deviceVolume: 100,
          denoiser: false,
          isInput: true
        })
        updateSettings();
      }
      
      ddmItems.push({group: "Input", name: device.deviceLabel, onClick: () => {
        if(settings.inputDevices.find((d) => d.deviceId === device.deviceId))
          return;

        settings.inputDevices.push({
          deviceId: device.deviceId,
          deviceLabel: device.deviceLabel,
          deviceVolume: 100,
          denoiser: false,
          isInput: true
        })
        updateSettings();
      }});
    });

    settings.outputDevicesCache.forEach((device) => {
      // by default, if there does not exist any devices, we will push default device
      // this behaviour may not be ideal if the user does not want any audio devices 
      //  (i.e. they removed all devices)
      if(device.deviceId === "default" && settings.outputDevices.length === 0) {
        settings.outputDevices.push({
          deviceId: device.deviceId,
          deviceLabel: device.deviceLabel,
          deviceVolume: 100
        })
        updateSettings();
      }
      
      ddmItems.push({group: "Output", id: device.deviceId, name: device.deviceLabel, onClick: () => {
        if(settings.outputDevices.find((d) => d.deviceId === device.deviceId))
          return;

        settings.outputDevices.push({
          deviceId: device.deviceId,
          deviceLabel: device.deviceLabel,
          deviceVolume: 100
        })
        updateSettings();
      }});
    });

    setAudioDevices(ddmItems);
    return;
  }, [setAudioDevices, updateSettings]);

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


  useEffect(() => {
    if (settings == null) return;
    if (settings.rateControlCache == null) return;

    let rateControlsItems: any[] = [];

    const rateControls = localStorage.getItem("availableRateControls")!.split(',');
    rateControls.forEach((rateControl) => {
      rateControlsItems.push({
        name: rateControl, onClick: () => { 
          settings.rateControl = rateControl;
          updateSettings();
        }
      });
    });

    setAvailableRateControls(rateControlsItems);
    return;            
  }, [setAvailableRateControls, updateSettings]);

  function getQualityPresetName(settings: CaptureSettings | undefined): 'low' | 'medium' | 'high' | 'ultra' | 'custom' {
    if (settings?.resolution === 480 && settings.frameRate === 15 && settings.bitRate === 5) {
      return 'low';
    } else if (settings?.resolution === 720 && settings.frameRate === 30 && settings.bitRate === 25) {
      return 'medium';
    } else if (settings?.resolution === 1080 && settings.frameRate === 60 && settings.bitRate === 35) {
      return 'high';
    } else if ((settings!.maxScreenResolution >= 1440 ? (settings?.resolution === 1440) : (settings?.resolution === 1080)) && settings?.frameRate === 60 && settings.bitRate === 50) {
      return 'ultra';
    } else {
      return 'custom';
    }
  }

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
          <input type="radio" name="recordMode" className="form-checkbox h-4 w-4 text-gray-600" value="whitelist"
            defaultChecked={(settings?.recordingMode === "whitelist" ? true : false)}/>
          <span className="px-2 text-gray-700 dark:text-gray-400">Whitelist</span>
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
              settings!.resolution = 1080; settings!.frameRate = 60; settings!.bitRate = 35;
              break;
            case "ultra":
              settings!.resolution = settings!.maxScreenResolution >= 1440 ? 1440 : 1080; settings!.frameRate = 60; settings!.bitRate = 50;
              break;
            default:
              return;
          }
          updateSettings();
        }
      }>
        <label className="inline-flex items-center">
          <input type="radio" name="quality" className="form-checkbox h-4 w-4 text-gray-600" value="low"
            defaultChecked={getQualityPresetName(settings) === 'low'}/>
          <span className="px-2 text-gray-700 dark:text-gray-400">Low</span>
        </label>
        <label className="inline-flex items-center">
          <input type="radio" name="quality" className="form-checkbox h-4 w-4 text-gray-600" value="medium"
            defaultChecked={getQualityPresetName(settings) === 'medium'}/>
          <span className="px-2 text-gray-700 dark:text-gray-400">Medium</span>
        </label>
        <label className="inline-flex items-center">
          <input type="radio" name="quality" className="form-checkbox h-4 w-4 text-gray-600" value="high"
            defaultChecked={getQualityPresetName(settings) === 'high'}/>
          <span className="px-2 text-gray-700 dark:text-gray-400">High</span>
        </label>
        <label className="inline-flex items-center">
          <input type="radio" name="quality" className="form-checkbox h-4 w-4 text-gray-600" value="ultra"
            defaultChecked={getQualityPresetName(settings) === 'ultra'}/>
          <span className="px-2 text-gray-700 dark:text-gray-400">Ultra</span>
        </label>
        <label className="inline-flex items-center">
          <input type="radio" name="quality" className="form-checkbox h-4 w-4 text-gray-600" value="custom" ref={customVideoQuality}
          defaultChecked={getQualityPresetName(settings) === 'custom'}/>
          <span className="px-2 text-gray-700 dark:text-gray-400">Custom</span>
        </label>
      </div>
      <div className="flex gap-2">
        <div className="flex flex-col">
          Resolution
          <DropDownMenu text={(settings === undefined ? "1080p" : settings.resolution + "p")} width={"auto"}
          items={[
            {name: "480p", onClick: () => {settings!.resolution = 480; customVideoQuality.current!.checked = true; updateSettings();}},
            {name: "720p", onClick: () => {settings!.resolution = 720; updateSettings();}},
            {name: "1080p", onClick: () => {settings!.resolution = 1080; updateSettings();}},
              ...(settings!.maxScreenResolution >= 1440 ? [{ name: "1440p", onClick: () => { settings!.resolution = 1440; updateSettings(); } }] : []),
          ]}/> 
        </div>
        <div className="flex flex-col">
          Framerate
          <DropDownMenu text={(settings === undefined ? "60 fps" : settings.frameRate + " fps")} width={"auto"}
          items={[
            {name: "15 fps", onClick: () => {settings!.frameRate = 15; customVideoQuality.current!.checked = true; updateSettings();}},
            {name: "30 fps", onClick: () => {settings!.frameRate = 30; customVideoQuality.current!.checked = true; updateSettings();}},
            {name: "60 fps", onClick: () => {settings!.frameRate = 60; customVideoQuality.current!.checked = true; updateSettings();}},
          ]}/> 
        </div>
        <div className="flex flex-col">
          Bitrate
          <DropDownMenu text={(settings === undefined ? "50 MB/s" : settings.bitRate + " MB/s")} width={"auto"} zIndex={52}
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
        <div className="flex flex-col">
          Encoder
          <DropDownMenu text={(settings === undefined ? "x264" : settings!.encoder)} width={"auto"}
          items={availableEncoders} />
        </div>
        <div className="flex flex-col">
          Rate Control
          <DropDownMenu text={(settings?.rateControl === undefined ? "VBR" : settings!.rateControl)} width={"auto"}
          items={availableRateControls} />
        </div>
      </div>

      <h1 className="font-semibold text-2xl mt-4">Audio Sources</h1>

      <div className="flex flex-col">Output Devices</div>
      <div className="flex flex-col gap-4">
        {settings?.outputDevices && settings.outputDevices.map((item, i) => {
          return <AudioDevice key={item.deviceId} item={item} defaultValue={settings === undefined ? 100 : settings!.outputDevices[i].deviceVolume} hasNvidiaAudioSDK={settings === undefined ? false : settings!.hasNvidiaAudioSDK}
            onChange={(e) => { let value = parseInt((e.target as HTMLInputElement).value); settings!.outputDevices[i].deviceVolume = value; }}
            onCheck={(e) => { let value = (e.target as HTMLInputElement).checked; settings!.inputDevices[i].denoiser = value; }}
            onMouseUpCapture={(e) => { updateSettings(); }}
            onRemove={() => {settings!.outputDevices = settings!.outputDevices.filter((d) => d.deviceId !== item.deviceId); updateSettings();}}/>
        })}
      </div>

      <div className="flex flex-col">Input Devices</div>
      <div className="flex flex-col gap-4">
        {settings?.inputDevices && settings.inputDevices.map((item, i) => {
          item.isInput = true;
          return <AudioDevice key={item.deviceId} item={item} defaultValue={settings === undefined ? 100 : settings!.inputDevices[i].deviceVolume}  hasNvidiaAudioSDK={settings === undefined ? false : settings!.hasNvidiaAudioSDK}
            onChange={(e) => { let value = parseInt((e.target as HTMLInputElement).value);  settings!.inputDevices[i].deviceVolume = value; }}
            onCheck={(e) => { let value = (e.target as HTMLInputElement).checked; settings!.inputDevices[i].denoiser = value; updateSettings();}}
            onMouseUpCapture={() => {updateSettings(); }}
            onRemove={() => {settings!.inputDevices = settings!.inputDevices.filter((d) => d.deviceId !== item.deviceId); updateSettings();}}/>
        })}
      </div>

      <div className="flex flex-col">
        Add New Audio Source
        <DropDownMenu text={"Select Audio Device"} width={"auto"}
        items={audioDevices} groups={["Output", "Input"]}/> 
      </div>

      <h1 className="font-semibold text-2xl mt-4">Advanced</h1>
      <div className="flex flex-col gap-1">
        <label className="inline-flex items-center">
          <input type="checkbox" className="form-checkbox h-4 w-4 text-gray-600"
            defaultChecked={settings === undefined ? false : settings.useRecordingStartSound}
            onChange={(e) => { settings!.useRecordingStartSound = e.target.checked; updateSettings(); }} />
          <span className="ml-2 text-gray-700 dark:text-gray-400">Start Recording Sound Effect</span>
        </label>
        <label className="inline-flex items-center">
          <input type="checkbox" className="form-checkbox h-4 w-4 text-gray-600"
            defaultChecked={settings === undefined ? false : settings.useDisplayCapture}
            onChange={(e) => { settings!.useDisplayCapture = e.target.checked; updateSettings(); }} />
          <span className="ml-2 text-gray-700 dark:text-gray-400">Use Display Capture As Backup</span>
        </label>
      </div>
    </div>
	)
}

export default Capture;