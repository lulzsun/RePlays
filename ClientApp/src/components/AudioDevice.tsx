import { useTranslation } from 'react-i18next';

import { postMessage } from '../helpers/messenger';

interface Props {
  onChange: React.ChangeEventHandler<HTMLInputElement>;
  onCheck: React.ChangeEventHandler<HTMLInputElement>;
  onMouseUpCapture: React.MouseEventHandler<HTMLInputElement>;
  onRemove: React.MouseEventHandler<HTMLDivElement>;
  defaultValue: number;
  isRemovable: boolean;
  item: AudioDevice;
  hasNvidiaAudioSDK: boolean;
}

export const AudioDevice: React.FC<Props> = ({
  onChange,
  onCheck,
  onMouseUpCapture,
  onRemove,
  defaultValue,
  isRemovable,
  item,
  hasNvidiaAudioSDK,
}) => {
  const { t } = useTranslation();

  return (
    <div
      className={`group px-6 py-2 self-start bg-white border rounded-md dark:bg-gray-900 text-gray-700 dark:text-gray-400 border-gray-500 dark:border-gray-400`}
    >
      <div className='flex flex-row gap-2'>
        <span className='whitespace-nowrap'>{item.deviceLabel}</span>
        <div className='w-full' />
        {isRemovable && (
          <div
            className={'group-hover:opacity-100 opacity-0 cursor-pointer hover:text-red-500'}
            onClick={onRemove}
          >
            {t('componentsAudioDeviceItem01')}
          </div>
        )}
      </div>
      <div className='flex flex-row gap-2'>
        <svg
          xmlns='http://www.w3.org/2000/svg'
          width='32'
          height='32'
          fill='currentColor'
          viewBox='0 0 16 16'
        >
          {item.deviceVolume >= 66 && (
            <path d='M11.536 14.01A8.473 8.473 0 0 0 14.026 8a8.473 8.473 0 0 0-2.49-6.01l-.708.707A7.476 7.476 0 0 1 13.025 8c0 2.071-.84 3.946-2.197 5.303l.708.707z' />
          )}
          {item.deviceVolume >= 33 && (
            <path d='M10.121 12.596A6.48 6.48 0 0 0 12.025 8a6.48 6.48 0 0 0-1.904-4.596l-.707.707A5.483 5.483 0 0 1 11.025 8a5.483 5.483 0 0 1-1.61 3.89l.706.706z' />
          )}
          {item.deviceVolume > 0 && (
            <path d='M10.025 8a4.486 4.486 0 0 1-1.318 3.182L8 10.475A3.489 3.489 0 0 0 9.025 8c0-.966-.392-1.841-1.025-2.475l.707-.707A4.486 4.486 0 0 1 10.025 8zM7 4a.5.5 0 0 0-.812-.39L3.825 5.5H1.5A.5.5 0 0 0 1 6v4a.5.5 0 0 0 .5.5h2.325l2.363 1.89A.5.5 0 0 0 7 12V4zM4.312 6.39 6 5.04v5.92L4.312 9.61A.5.5 0 0 0 4 9.5H2v-3h2a.5.5 0 0 0 .312-.11z' />
          )}
          {item.deviceVolume == 0 && (
            <path d='M 7 4 C 6.999 3.615 6.582 3.375 6.249 3.568 C 6.228 3.58 6.207 3.595 6.188 3.61 L 3.825 5.5 L 1.5 5.5 C 1.224 5.5 1 5.724 1 6 L 1 10 C 1 10.276 1.224 10.5 1.5 10.5 L 3.825 10.5 L 6.188 12.39 C 6.489 12.63 6.937 12.455 6.994 12.074 C 6.998 12.05 7 12.025 7 12 L 7 4 Z M 4.312 6.39 L 6 5.04 L 6 10.96 L 4.312 9.61 C 4.223 9.539 4.113 9.5 4 9.5 L 2 9.5 L 2 6.5 L 4 6.5 C 4.113 6.5 4.223 6.461 4.312 6.39 Z'></path>
          )}
        </svg>
        <input
          className='w-96'
          type='range'
          min='0'
          max='100'
          step='1'
          defaultValue={defaultValue}
          onChange={onChange}
          onMouseUpCapture={onMouseUpCapture}
        />
        {item.deviceVolume + '%'}
      </div>

      {item.isInput && (
        <div className='flex flex-row gap-2'>
          <label className='inline-flex items-center'>
            <input
              type='checkbox'
              disabled={!hasNvidiaAudioSDK}
              className='form-checkbox h-4 w-4 text-gray-600'
              defaultChecked={item.denoiser ?? false}
              onChange={onCheck}
              onMouseUpCapture={onMouseUpCapture}
            />
            <span className='ml-2 text-gray-700 dark:text-gray-400'>NVIDIA Noise Removal</span>
          </label>
          {!hasNvidiaAudioSDK && (
            <a
              onClick={() => {
                postMessage('DownloadNvidiaAudioSDK');
              }}
              className='text-blue-700 dark:text-blue-400 cursor-pointer'
            >
              Download SDK
            </a>
          )}
        </div>
      )}
    </div>
  );
};

export default AudioDevice;
