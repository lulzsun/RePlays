import { useTranslation } from 'react-i18next';

import { ChangeEvent, KeyboardEvent, useEffect, useRef, useState } from 'react';
import DropDownMenu from '../../components/DropDownMenu';
import AudioDevice from '../../components/AudioDevice';
import HelpSymbol from '../../components/HelpSymbol';

interface Props {
  updateSettings: () => void;
  settings: CaptureSettings | undefined;
  device: Device | undefined;
}

export const Capture: React.FC<Props> = ({ settings, updateSettings, device }) => {
  const { t } = useTranslation();

  const customVideoQuality = useRef<HTMLInputElement | null>(null);
  const [audioDevices, setAudioDevices] = useState<any[]>();
  const [availableEncoders, setAvailableEncoders] = useState<any[]>();
  const [availableRateControls, setAvailableRateControls] = useState<any[]>();
  const [availableFileFormats, setAvailableFileFormats] = useState<any[]>();

  useEffect(() => {
    if (settings == null) return;
    if (settings.inputDevicesCache == null || settings.outputDevicesCache == null) return;

    let ddmItems: any[] = [];

    settings.inputDevicesCache.forEach((device) => {
      // by default, if there does not exist any devices, we will push default device
      // this behaviour may not be ideal if the user does not want any audio devices
      //  (i.e. they removed all devices)
      if (device.deviceId === 'default' && settings.inputDevices.length === 0) {
        settings.inputDevices.push({
          deviceId: device.deviceId,
          deviceLabel: device.deviceLabel,
          deviceVolume: 100,
          denoiser: false,
          isInput: true,
        });
        updateSettings();
      }

      ddmItems.push({
        group: t('settingsCaptureItem26'), // Input
        name: device.deviceLabel,
        onClick: () => {
          if (settings.inputDevices.find((d) => d.deviceId === device.deviceId)) return;

          settings.inputDevices.push({
            deviceId: device.deviceId,
            deviceLabel: device.deviceLabel,
            deviceVolume: 100,
            denoiser: false,
            isInput: true,
          });
          updateSettings();
        },
      });
    });

    settings.outputDevicesCache.forEach((device) => {
      // by default, if there does not exist any devices, we will push default device
      // this behaviour may not be ideal if the user does not want any audio devices
      //  (i.e. they removed all devices)
      if (device.deviceId === 'default' && settings.outputDevices.length === 0) {
        settings.outputDevices.push({
          deviceId: device.deviceId,
          deviceLabel: device.deviceLabel,
          deviceVolume: 100,
        });
        updateSettings();
      }

      ddmItems.push({
        group: t('settingsCaptureItem25'), // Output
        id: device.deviceId,
        name: device.deviceLabel,
        onClick: () => {
          if (settings.outputDevices.find((d) => d.deviceId === device.deviceId)) return;

          settings.outputDevices.push({
            deviceId: device.deviceId,
            deviceLabel: device.deviceLabel,
            deviceVolume: 100,
          });
          updateSettings();
        },
      });
    });

    setAudioDevices(ddmItems);
    return;
  }, [setAudioDevices, updateSettings]);

  useEffect(() => {
    if (settings == null) return;
    if (settings.encodersCache == null) return;

    let ddmItems: any[] = [];

    settings.encodersCache.forEach((encoder) => {
      ddmItems.push({
        name: encoder,
        onClick: () => {
          settings.encoder = encoder;
          updateSettings();
        },
      });
    });

    setAvailableEncoders(ddmItems);
    return;
  }, [setAvailableEncoders]);

  useEffect(() => {
    if (settings == null) return;
    if (settings.rateControlCache == null) return;

    let rateControlsItems: any[] = [];

    const rateControls = localStorage.getItem('availableRateControls')!.split(',');
    rateControls.forEach((rateControl) => {
      rateControlsItems.push({
        name: rateControl,
        onClick: () => {
          settings.rateControl = rateControl;
          updateSettings();
        },
      });
    });

    setAvailableRateControls(rateControlsItems);
    return;
  }, [setAvailableRateControls, updateSettings]);

  useEffect(() => {
    if (settings == null) return;
    if (settings.fileFormatsCache == null) return;

    let fileFormatItems: any[] = [];
    var availableFormats = localStorage.getItem('availableFileFormats');

    // If we dont have any file formats in the request, default to MP4 and MKV.
    if (
      availableFormats === null ||
      availableFormats === undefined ||
      availableFormats.toString().trim() === ''
    ) {
      fileFormatItems.push({
        name: 'Fragmented MPEG-4 (.mp4) (Default)',
        onClick: () => {
          settings.fileFormat = {
            title: 'MPEG-4 (.mp4)',
            format: 'fragmented_mp4',
            isReplayBufferCompatible: false
          };
          updateSettings();
        },
      });

      // Disable the MKV file by default if no list provided, this may not be supported in PlaysTV
      // fileFormatItems.push({
      //   name: "Matroska Video (.mkv)",
      //   onClick: () => {
      //     settings.fileFormat = {
      //       title: "Matroska Video (.mkv)",
      //       format: "mkv"
      //     };
      //     updateSettings();
      //   }
      // })
    }

    // We recieved the file formats from the settings
    else {
      const fileFormats = JSON.parse(localStorage.getItem('availableFileFormats')!);
      fileFormats.forEach((fmt: FileFormat) => {
        fileFormatItems.push({
          name: fmt.title,
          onClick: () => {
            settings.fileFormat = fmt;
            updateSettings();
          },
          isReplayBufferCompatible: fmt.isReplayBufferCompatible
        });
      });
    }

    setAvailableFileFormats(fileFormatItems);
    return;
  }, [setAvailableFileFormats, updateSettings]);

  function getQualityPresetName(
    settings: CaptureSettings | undefined,
  ): 'low' | 'medium' | 'high' | 'ultra' | 'custom' {
    if (settings?.resolution === 480 && settings.frameRate === 15 && settings.bitRate === 5) {
      return 'low';
    } else if (
      settings?.resolution === 720 &&
      settings.frameRate === 30 &&
      settings.bitRate === 25
    ) {
      return 'medium';
    } else if (
      settings?.resolution === 1080 &&
      settings.frameRate === 60 &&
      settings.bitRate === 35
    ) {
      return 'high';
    } else if (
      settings?.maxScreenResolution &&
      (settings?.maxScreenResolution >= 1440
        ? settings?.resolution === 1440
        : settings?.resolution === 1080) &&
      settings?.frameRate === 60 &&
      settings.bitRate === 50
    ) {
      return 'ultra';
    } else {
      return 'custom';
    }
  }

  var current_format: any = settings?.fileFormat?.format;
  var is_interupttable: boolean = (
    current_format === 'mkv' ||
    current_format === 'fragmented_mp4' ||
    current_format === 'fragmented_mov') ||
    settings?.useReplayBuffer == true;


  const validate = (e: ChangeEvent<HTMLInputElement>) => {
    let value: number | string = parseInt(e.target.value);
    if (isNaN(value) || value <= 0) {
      value = -1;
      e.target.value = '';
    }
  }

  const validateAndUpdateSettings = (e: ChangeEvent<HTMLInputElement>, field: keyof CaptureSettings) => {
    let value: number | string = parseInt(e.target.value);
    if (field == "replayBufferDuration" || field == "replayBufferSize") {
      if (isNaN(value) || value <= 10) {
        value = 10;
        e.target.value = '10';
      }
    }

    if (field == "bitRate" || field == "maxBitRate") {
      if (isNaN(value) || value <= 1) {
        value = 1;
        e.target.value = '1';
      }
    }

    if (field == "cqLevel") {
      if (isNaN(value) || value <= 1) {
        value = 1;
        e.target.value = '1';
      }
      else if (value >= 51) {
        value = 51;
        e.target.value = '51';
      }
    }

    if (settings) {
      (settings[field] as number) = value;
      updateSettings();
    }
  };

  const handleOnChange = (e: ChangeEvent<HTMLInputElement>) => {
    validate(e);
  };

  const handleInputBlur = (e: ChangeEvent<HTMLInputElement>, field: keyof CaptureSettings) => {
    validateAndUpdateSettings(e, field);
  };

  const handleInputKeyPress = (e: KeyboardEvent<HTMLInputElement>, field: keyof CaptureSettings) => {
    if (e.key === 'Enter') {
      validateAndUpdateSettings(e as unknown as ChangeEvent<HTMLInputElement>, field);
      e.preventDefault();
    }
  };

  return (
    <div className='flex flex-col gap-2 font-medium text-base pb-7'>
      <h1 className='font-semibold text-2xl'>{t('settingsCaptureItem01')}</h1>
      <div
        onChange={(e) => {
          if (settings) settings.recordingMode = (e?.target as HTMLInputElement).value;
          updateSettings();
        }}
      >
        <label className='inline-flex items-center'>
          <input
            type='radio'
            name='recordMode'
            className='form-checkbox h-4 w-4 text-gray-600'
            value='automatic'
            defaultChecked={settings?.recordingMode === 'automatic' ? true : false}
          />
          <span className='px-2 text-gray-700 dark:text-gray-400'>
            {t('settingsCaptureItem02')}
          </span>
        </label>
        <label className='inline-flex items-center'>
          <input
            type='radio'
            name='recordMode'
            className='form-checkbox h-4 w-4 text-gray-600'
            value='whitelist'
            defaultChecked={settings?.recordingMode === 'whitelist' ? true : false}
          />
          <span className='px-2 text-gray-700 dark:text-gray-400'>
            {t('settingsCaptureItem03')}
          </span>
        </label>
        <label className='inline-flex items-center'>
          <input
            type='radio'
            name='recordMode'
            className='form-checkbox h-4 w-4 text-gray-600'
            value='manual'
            defaultChecked={settings?.recordingMode === 'manual' ? true : false}
          />
          <span className='px-2 text-gray-700 dark:text-gray-400'>
            {t('settingsCaptureItem04')}
          </span>
        </label>
        <label className='inline-flex items-center'>
          <input
            type='radio'
            name='recordMode'
            className='form-checkbox h-4 w-4 text-gray-600'
            value='off'
            defaultChecked={settings?.recordingMode === 'off' ? true : false}
          />
          <span className='px-2 text-gray-700 dark:text-gray-400'>
            {t('settingsCaptureItem05')}
          </span>
        </label>
      </div>

      <h1 className='font-semibold text-2xl'>{t('settingsCaptureItem30')}</h1>
      <div className='flex flex-col gap-2'>
        <span className='text-gray-700 dark:text-gray-400'>
          {t('settingsCaptureItem31')}
        </span>
        <label className='inline-flex items-center'>
          <input
            type='checkbox'
            name='enableReplayBuffer'
            className='form-checkbox h-4 w-4 text-gray-600'
            defaultChecked={settings === undefined ? false : settings.useReplayBuffer}
            onChange={e => {
              settings!.useReplayBuffer = (e?.target as HTMLInputElement).checked
              updateSettings()
            }}
          />
          <span className='px-2 text-gray-700 dark:text-gray-400'>
            {t('settingsCaptureItem32')}
          </span>
        </label>

        {
          settings?.useReplayBuffer && <div className='flex flex-row items-center gap-4'>
            <div className='flex flex-col'>
              {t('settingsCaptureItem33')}
              <div className='flex flex-row items-center'>
                <input
                  className="inline-flex w-24 px-2 py-2 text-sm font-medium leading-5 text-gray-700 dark:text-gray-600 transition duration-150 ease-in-out bg-white border border-gray-300 rounded-l-md hover:text-gray-700 focus:outline-none focus:border-blue-300 focus:shadow-outline-blue"
                  type="number"
                  min="10"
                  defaultValue={settings?.replayBufferDuration}
                  onBlur={(e) => handleInputBlur(e, 'replayBufferDuration')}
                  onKeyPress={(e) => handleInputKeyPress(e, 'replayBufferDuration')}
                  onChange={(e) => handleOnChange(e)}
                />
                <span className="inline-flex items-center py-2 px-3 border border-l-0 border-gray-300 bg-gray-50 text-gray-500 text-sm rounded-r-md">
                  seconds
                </span>
              </div>
            </div>
            <div className='flex flex-col'>
              {t('settingsCaptureItem34')}
              <div className='flex flex-row items-center'>
                <input
                  className="inline-flex w-24 px-2 py-2 text-sm font-medium leading-5 text-gray-700 dark:text-gray-600 transition duration-150 ease-in-out bg-white border border-gray-300 rounded-l-md hover:text-gray-700 focus:outline-none focus:border-blue-300 focus:shadow-outline-blue"
                  type="number"
                  min="10"
                  defaultValue={settings?.replayBufferSize}
                  onBlur={(e) => handleInputBlur(e, 'replayBufferSize')}
                  onKeyPress={(e) => handleInputKeyPress(e, 'replayBufferSize')}
                  onChange={(e) => handleOnChange(e)}
                />
                <span className="inline-flex items-center py-2 px-3 border border-l-0 border-gray-300 bg-gray-50 text-gray-500 text-sm rounded-r-md">
                  MB
                </span>
              </div>
            </div>
          </div>
        }
      </div>

      <h1 className='font-semibold text-2xl mt-4'>{t('settingsCaptureItem06')}</h1>
      <div
        className='flex gap-4'
        onChange={(e) => {
          let value = (e?.target as HTMLInputElement).value;
          switch (value) {
            case 'low':
              settings!.resolution = 480;
              settings!.frameRate = 15;
              settings!.bitRate = 5;
              break;
            case 'medium':
              settings!.resolution = 720;
              settings!.frameRate = 30;
              settings!.bitRate = 25;
              break;
            case 'high':
              settings!.resolution = 1080;
              settings!.frameRate = 60;
              settings!.bitRate = 35;
              break;
            case 'ultra':
              settings!.resolution =
                settings!.maxScreenResolution && settings!.maxScreenResolution >= 1440
                  ? 1440
                  : 1080;
              settings!.frameRate = 60;
              settings!.bitRate = 50;
              break;
            default:
              return;
          }
          updateSettings();
        }}
      >
        <label className='inline-flex items-center'>
          <input
            type='radio'
            name='quality'
            className='form-checkbox h-4 w-4 text-gray-600'
            value='low'
            defaultChecked={getQualityPresetName(settings) === 'low'}
          />
          <span className='px-2 text-gray-700 dark:text-gray-400'>
            {t('settingsCaptureItem07')}
          </span>
        </label>
        <label className='inline-flex items-center'>
          <input
            type='radio'
            name='quality'
            className='form-checkbox h-4 w-4 text-gray-600'
            value='medium'
            defaultChecked={getQualityPresetName(settings) === 'medium'}
          />
          <span className='px-2 text-gray-700 dark:text-gray-400'>
            {t('settingsCaptureItem08')}
          </span>
        </label>
        <label className='inline-flex items-center'>
          <input
            type='radio'
            name='quality'
            className='form-checkbox h-4 w-4 text-gray-600'
            value='high'
            defaultChecked={getQualityPresetName(settings) === 'high'}
          />
          <span className='px-2 text-gray-700 dark:text-gray-400'>
            {t('settingsCaptureItem09')}
          </span>
        </label>
        <label className='inline-flex items-center'>
          <input
            type='radio'
            name='quality'
            className='form-checkbox h-4 w-4 text-gray-600'
            value='ultra'
            defaultChecked={getQualityPresetName(settings) === 'ultra'}
          />
          <span className='px-2 text-gray-700 dark:text-gray-400'>
            {t('settingsCaptureItem10')}
          </span>
        </label>
        <label className='inline-flex items-center'>
          <input
            type='radio'
            name='quality'
            className='form-checkbox h-4 w-4 text-gray-600'
            value='custom'
            ref={customVideoQuality}
            defaultChecked={getQualityPresetName(settings) === 'custom'}
          />
          <span className='px-2 text-gray-700 dark:text-gray-400'>
            {t('settingsCaptureItem11')}
          </span>
        </label>
      </div>
      <div className='flex gap-2'>
        <div className='flex flex-col'>
          {t('settingsCaptureItem12')}
          <DropDownMenu
            text={settings === undefined ? '1080p' : settings.resolution + 'p'}
            width={'auto'}
            items={[
              {
                name: '480p',
                onClick: () => {
                  settings!.resolution = 480;
                  customVideoQuality.current!.checked = true;
                  updateSettings();
                },
              },
              {
                name: '720p',
                onClick: () => {
                  settings!.resolution = 720;
                  updateSettings();
                },
              },
              {
                name: '1080p',
                onClick: () => {
                  settings!.resolution = 1080;
                  updateSettings();
                },
              },
              ...(settings && settings.maxScreenResolution && settings.maxScreenResolution >= 1440
                ? [
                  {
                    name: '1440p',
                    onClick: () => {
                      settings.resolution = 1440;
                      updateSettings();
                    },
                  },
                ]
                : []),
            ]}
          />
        </div>
        <div className='flex flex-col'>
          {t('settingsCaptureItem13')}
          <DropDownMenu
            text={settings === undefined ? '60 FPS' : settings.frameRate + ' FPS'}
            width={'auto'}
            items={[
              {
                name: '15 FPS',
                onClick: () => {
                  settings!.frameRate = 15;
                  customVideoQuality.current!.checked = true;
                  updateSettings();
                },
              },
              {
                name: '30 FPS',
                onClick: () => {
                  settings!.frameRate = 30;
                  customVideoQuality.current!.checked = true;
                  updateSettings();
                },
              },
              {
                name: '60 FPS',
                onClick: () => {
                  settings!.frameRate = 60;
                  customVideoQuality.current!.checked = true;
                  updateSettings();
                },
              },
              {
                name: '120 FPS',
                onClick: () => {
                  settings!.frameRate = 120;
                  customVideoQuality.current!.checked = true;
                  updateSettings();
                },
              },
              {
                name: '144 FPS',
                onClick: () => {
                  settings!.frameRate = 144;
                  customVideoQuality.current!.checked = true;
                  updateSettings();
                },
              },
            ]}
          />
        </div>
      </div>

      <div className='flex gap-2'>
        <div className='flex flex-col'>
          {t('settingsCaptureItem15')}
          <DropDownMenu
            text={settings === undefined ? 'x264' : settings!.encoder}
            width={'auto'}
            items={availableEncoders}
          />
        </div>


        <div className='flex flex-col'>
          {t('settingsCaptureItem17')}
          <DropDownMenu
            text={settings?.fileFormat === undefined ? 'MPEG-4 (.mp4)' : settings!.fileFormat.title}
            width={'auto'}
            items={settings?.useReplayBuffer
              ? availableFileFormats?.filter(format => format.isReplayBufferCompatible)
              : availableFileFormats}
          />
        </div>
      </div>

      <div className='flex gap-2'>
        <div className='flex flex-col'>
          {t('settingsCaptureItem16')}
          <DropDownMenu
            text={settings?.rateControl === undefined ? 'VBR' : settings!.rateControl}
            width={'auto'}
            items={availableRateControls}
          />
        </div>
        {(settings?.rateControl == "CBR" || settings?.rateControl == "VBR") &&
        <div className='flex flex-col'>
          {t('settingsCaptureItem14')}
          <div className='flex flex-row items-center'>
            <input
              className="inline-flex w-24 px-2 py-2 text-sm font-medium leading-5 text-gray-700 dark:text-gray-600 transition duration-150 ease-in-out bg-white border border-gray-500 rounded-l-md hover:text-gray-700 focus:outline-none focus:border-blue-300 focus:shadow-outline-blue"
              type="number"
              min="1"
              defaultValue={settings?.bitRate}
              onBlur={(e) => handleInputBlur(e, 'bitRate')}
              onKeyPress={(e) => handleInputKeyPress(e, 'bitRate')}
              onChange={(e) => handleOnChange(e)}
            />
            <span className="inline-flex items-center py-2 px-3 border border-l-0 border-gray-500 bg-gray-50 text-gray-500 text-sm rounded-r-md">
              MB/s
            </span>
          </div>
        </div>
        }
        {
          settings?.rateControl == "VBR" &&
          <div className='flex flex-col'>
          {t('settingsCaptureItem35')}
          <div className='flex flex-row items-center'>
            <input
              className="inline-flex w-24 px-2 py-2 text-sm font-medium leading-5 text-gray-700 dark:text-gray-600 transition duration-150 ease-in-out bg-white border border-gray-500 rounded-l-md hover:text-gray-700 focus:outline-none focus:border-blue-300 focus:shadow-outline-blue"
              type="number"
              min="1"
              defaultValue={settings?.maxBitRate}
              onBlur={(e) => handleInputBlur(e, 'maxBitRate')}
              onKeyPress={(e) => handleInputKeyPress(e, 'maxBitRate')}
              onChange={(e) => handleOnChange(e)}
            />
            <span className="inline-flex items-center py-2 px-3 border border-l-0 border-gray-500 bg-gray-50 text-gray-500 text-sm rounded-r-md">
              MB/s
            </span>
          </div>
        </div>
        }
        {
          (settings?.rateControl == "CQP" || settings?.rateControl == "CRF") &&
          <div className='flex flex-col'>
          {t('settingsCaptureItem36')}
          <div className='flex flex-row items-center'>
            <input
              className="inline-flex w-24 px-2 py-2 text-sm font-medium leading-5 text-gray-700 dark:text-gray-600 transition duration-150 ease-in-out bg-white border border-gray-500 rounded-l-md hover:text-gray-700 focus:outline-none focus:border-blue-300 focus:shadow-outline-blue"
              type="number"
              min="1"
              max="51"
              defaultValue={settings?.cqLevel}
              onBlur={(e) => handleInputBlur(e, 'cqLevel')}
              onKeyPress={(e) => handleInputKeyPress(e, 'cqLevel')}
              onChange={(e) => handleOnChange(e)}
            />
            <span className="inline-flex items-center py-2 px-3 border border-l-0 border-gray-500 bg-gray-50 text-gray-500 text-sm rounded-r-md">
              CQP
            </span>
          </div>
        </div>
        }
      </div>

      {is_interupttable == false && (
        <div
          className='flex flex-col items-center bg-blue-500 text-white px-3 py-3 max-w-md'
          role='alert'
        >
          <div className='flex pb-2'>
            <svg
              className='fill-current h-6 w-6 text-white mr-4'
              xmlns='http://www.w3.org/2000/svg'
              viewBox='0 0 20 20'
            >
              <path d='M2.93 17.07A10 10 0 1 1 17.07 2.93 10 10 0 0 1 2.93 17.07zm12.73-1.41A8 8 0 1 0 4.34 4.34a8 8 0 0 0 11.32 11.32zM9 11V9h2v6H9v-4zm0-6h2v2H9V5z' />
            </svg>
            <p className='font-bold'>
              {settings!.fileFormat.title} {t('settingsCaptureItem18')}
            </p>
          </div>
          <div>
            <p className='text-xs text-center'>{t('settingsCaptureItem19')}</p>
          </div>
        </div>
      )}

      <h1 className='font-semibold text-2xl mt-4'>{t('settingsCaptureItem20')}</h1>
      <label className='inline-flex items-center'>
        <input
          type='checkbox'
          className='form-checkbox h-4 w-4 text-gray-600'
          defaultChecked={settings === undefined ? false : settings.captureGameAudio}
          onChange={(e) => {
            settings!.captureGameAudio = e.target.checked;
            updateSettings();
          }}
        />
        <span className='ml-2 text-gray-700 dark:text-gray-400'>
          {t('settingsCaptureItem37')}
        </span>
      </label>
      {settings?.captureGameAudio == false && (
        <>
        <div className='flex flex-col'>{t('settingsCaptureItem21')}</div>
        <div className='flex flex-col gap-4'>
          {settings?.outputDevices &&
            settings.outputDevices.map((item, i) => {
              const isRemovable = !(
                settings.outputDevices.length === 1 && item.deviceId === 'default'
              );

              return (
                <AudioDevice
                  key={item.deviceId}
                  item={item}
                  defaultValue={
                    settings === undefined ? 100 : settings!.outputDevices[i].deviceVolume
                  }
                  device={device}
                  hasNvidiaAudioSDK={settings === undefined ? false : settings!.hasNvidiaAudioSDK}
                  isRemovable={isRemovable}
                  onChange={(e) => {
                    let value = parseInt((e.target as HTMLInputElement).value);
                    settings!.outputDevices[i].deviceVolume = value;
                  }}
                  onCheck={(e) => {
                    let value = (e.target as HTMLInputElement).checked;
                    settings!.inputDevices[i].denoiser = value;
                  }}
                  onMouseUpCapture={(e) => {
                    updateSettings();
                  }}
                  onRemove={() => {
                    settings!.outputDevices = settings!.outputDevices.filter(
                      (d) => d.deviceId !== item.deviceId,
                    );
                    updateSettings();
                  }}
                />
              );
            })}
        </div>

        <div className='flex flex-col'>{t('settingsCaptureItem22')}</div>
        <div className='flex flex-col gap-4'>
          {settings?.inputDevices &&
            settings.inputDevices.map((item, i) => {
              item.isInput = true;

              const isRemovable = !(
                settings.inputDevices.length === 1 && item.deviceId === 'default'
              );

              return (
                <AudioDevice
                  key={item.deviceId}
                  item={item}
                  defaultValue={settings === undefined ? 100 : settings!.inputDevices[i].deviceVolume}
                  device={device}
                  hasNvidiaAudioSDK={settings === undefined ? false : settings!.hasNvidiaAudioSDK}
                  isRemovable={isRemovable}
                  onChange={(e) => {
                    let value = parseInt((e.target as HTMLInputElement).value);
                    settings!.inputDevices[i].deviceVolume = value;
                  }}
                  onCheck={(e) => {
                    let value = (e.target as HTMLInputElement).checked;
                    settings!.inputDevices[i].denoiser = value;
                    updateSettings();
                  }}
                  onMouseUpCapture={() => {
                    updateSettings();
                  }}
                  onRemove={() => {
                    settings!.inputDevices = settings!.inputDevices.filter(
                      (d) => d.deviceId !== item.deviceId,
                    );
                    updateSettings();
                  }}
                />
              );
            })}
        </div>

        <div className='flex flex-col'>
          {t('settingsCaptureItem23')}
          <DropDownMenu
            text={t('settingsCaptureItem24')}
            width={'auto'}
            items={audioDevices}
            groups={[t('settingsCaptureItem25'), t('settingsCaptureItem26')]}
          />
        </div>
        </>
      )}
      <h1 className='font-semibold text-2xl mt-4'>{t('settingsCaptureItem27')}</h1>
      <div className='flex flex-col gap-1'>
        <label className='inline-flex items-center'>
          <input
            type='checkbox'
            className='form-checkbox h-4 w-4 text-gray-600'
            defaultChecked={settings === undefined ? false : settings.useRecordingStartSound}
            onChange={(e) => {
              settings!.useRecordingStartSound = e.target.checked;
              updateSettings();
            }}
          />
          <span className='ml-2 text-gray-700 dark:text-gray-400'>
            {t('settingsCaptureItem28')}
          </span>
        </label>
        <label className='inline-flex items-center'>
          <input
            type='checkbox'
            className='form-checkbox h-4 w-4 text-gray-600'
            defaultChecked={settings === undefined ? false : settings.useDisplayCapture}
            onChange={(e) => {
              settings!.useDisplayCapture = e.target.checked;
              updateSettings();
            }}
          />
          <span className='ml-2 text-gray-700 dark:text-gray-400'>
            {t('settingsCaptureItem29')}
          </span>
        </label>
        <label className='inline-flex items-center'>
          <input
            type='checkbox'
            className='form-checkbox h-4 w-4 text-gray-600'
            defaultChecked={settings === undefined ? false : settings.captureHdr}
            onChange={(e) => {
              settings!.captureHdr = e.target.checked;
              updateSettings();
            }}
          />
          <span className='ml-2 text-gray-700 dark:text-gray-400'>
            {t('settingsCaptureItem38')}
          </span>
        </label>
      </div>
    </div>
  );
};

export default Capture;
