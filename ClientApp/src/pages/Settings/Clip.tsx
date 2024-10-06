import {useTranslation} from 'react-i18next';

import DropDownMenu from '../../components/DropDownMenu';
import HelpSymbol from '../../components/HelpSymbol';

interface Props {
  updateSettings: () => void;
  settings: ClipSettings | undefined;
  device: Device | undefined;
}

export const Clip: React.FC<Props> = ({settings, updateSettings, device}) => {
  const {t} = useTranslation();

  const setRenderHardware = (hardware: 'CPU' | 'GPU') => {
    if (settings) {
      settings.renderHardware = hardware;
      settings.renderCodec = getCodecOptions(hardware, device?.gpuType)[0].value;
      updateSettings();
    }
  };

  const setRenderQuality = (quality: number) => {
    if (settings) {
      settings.renderQuality = quality;
      updateSettings();
    }
  };

  const qualityOptions = [
    {value: 17, label: '17 (High Quality)'},
    {value: 18, label: '18'},
    {value: 19, label: '19'},
    {value: 20, label: '20'},
    {value: 21, label: '21'},
    {value: 22, label: '22'},
    {value: 23, label: '23 (Normal Quality)'},
    {value: 24, label: '24'},
    {value: 25, label: '25'},
    {value: 26, label: '26'},
    {value: 27, label: '27'},
    {value: 28, label: '28 (Low Quality)'},
  ];

  const setRenderCodec = (codec: string) => {
    if (settings) {
      settings.renderCodec = codec;
      updateSettings();
    }
  };

  const allCodecOptions = {
    NVIDIA: [
      {name: 'H.264 (NVENC)', value: 'h264_nvenc'},
      {name: 'H.265 (NVENC)', value: 'hevc_nvenc'},
    ],
    AMD: [
      {name: 'H.264 (AMF)', value: 'h264_amf'},
      {name: 'H.265 (AMF)', value: 'hevc_amf'},
    ],
    Intel: [
      {name: 'H.264 (Quick Sync)', value: 'h264_qsv'},
      {name: 'H.265 (Quick Sync)', value: 'hevc_qsv'},
    ],
    CPU: [
      {name: 'H.264 (libx264)', value: 'libx264'},
      {name: 'H.265 (libx265)', value: 'libx265'},
    ],
  };

  const getCodecOptions = (hardware: 'CPU' | 'GPU', gpuType?: 'NVIDIA' | 'AMD' | 'Intel') => {
    if (hardware === "GPU") {
      return gpuType ? allCodecOptions[gpuType] || allCodecOptions.CPU : allCodecOptions.CPU;
    }
    return allCodecOptions.CPU;
  };

  const codecOptions = settings ? getCodecOptions(settings.renderHardware, device?.gpuType) : [];

  const fpsOptions = [
    {value: 15, label: '15 FPS'},
    {value: 30, label: '30 FPS'},
    {value: 60, label: '60 FPS'},
    {value: 120, label: '120 FPS'},
    {value: 144, label: '144 FPS'},
  ];

  const setRenderCustomFps = (fps: number | undefined) => {
    if (settings) {
      settings.renderCustomFps = fps
      updateSettings();
    }
  };

  return (
    <div className='flex flex-col gap-2 font-medium text-base pb-7'>
      <label className='inline-flex items-center'>
        <input
          type='checkbox'
          className='form-checkbox h-4 w-4 text-gray-600'
          defaultChecked={settings === undefined ? false : settings.reEncode}
          onChange={(e) => {
            settings!.reEncode = e.target.checked;
            updateSettings();
          }}
        />
        <div className="flex items-center">
          <span className='ml-2 text-gray-700 dark:text-gray-400'>
            {t('settingsClipItem01')}
          </span>
        </div>
      </label>
      {settings!.reEncode && (
        <>
          <div className='flex flex-col'>
            <span>{t('settingsClipItem02')}</span>
            <span className='flex flex-row items-center'>
              <DropDownMenu
                text={settings === undefined ? 'CPU' : settings!.renderHardware}
                width={'auto'}
                items={[
                  {name: 'CPU', onClick: () => setRenderHardware('CPU')},
                  {name: 'GPU', onClick: () => setRenderHardware('GPU')},
                ]}
              />
              <HelpSymbol text={t('settingsClipItem03')} />
            </span>
          </div>
          <div className='flex flex-col'>
            <span>{t('settingsClipItem04')}</span>
            <DropDownMenu
              text={qualityOptions.find(option => option.value === settings?.renderQuality)?.label ||
                settings?.renderQuality.toString()}
              width={'auto'}
              items={qualityOptions.map(option => ({
                name: option.label,
                onClick: () => setRenderQuality(option.value),
              }))} />
          </div>
          <div className='flex flex-col'>
            <span>{t('settingsClipItem05')}</span>
            <span className='flex flex-row items-center'>
              <DropDownMenu
                text={codecOptions.find(option => option.value === settings?.renderCodec)?.name ||
                  'Select Codec'}
                width={'auto'}
                items={codecOptions.map(option => ({
                  name: option.name,
                  onClick: () => setRenderCodec(option.value),
                }))} />
              <HelpSymbol text={t('settingsClipItem06')} />
            </span>
          </div>
          <div className='flex flex-col'>
            <span>{t('settingsClipItem07')}</span>
            <DropDownMenu
              text={settings?.renderCustomFps
                ? `${settings?.renderCustomFps} FPS`
                : 'Original FPS'}
              width={'auto'}
              items={[
                {name: 'Original FPS', onClick: () => setRenderCustomFps(undefined)},
                ...fpsOptions.map(option => ({
                  name: option.label,
                  onClick: () => setRenderCustomFps(option.value),
                })),
              ]} />
          </div>
        </>
      )}
    </div>
  );
};

export default Clip;
