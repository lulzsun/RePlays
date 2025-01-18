import { useTranslation } from 'react-i18next';

import { postMessage } from '../helpers/messenger';

interface Props {
  updateSettings: () => void;
  settings: KeybindSettings | undefined;
  id: string;
  keybind?: CustomKeybind;
}

interface Keybind {
  action: string;
  description?: string;
}

const keybinds: { [key: string]: Keybind } = {
  StartStopRecording: { action: 'Toggle Recording' },
  CreateBookmark: { action: 'Create Bookmark' },
};

export const KeybindSelector: React.FC<Props> = ({ settings, updateSettings, id, keybind }) => {
  const { t } = useTranslation();

  return (
    <div className='flex py-2 space-x-5 whitespace-nowrap border-t border-gray-700 dark:border-gray-500'>
      <div className='flex-col w-1/2'>
        <span>{t('componentKeybindSelectorItem01')}</span>
        <div
          className={`bg-white text-gray-700 border-gray-500 hover:text-white hover:bg-gray-900 hover:border-gray-900 focus:border-yellow-600 focus:text-white
        dark:bg-gray-900 dark:text-gray-400 dark:border-gray-400 dark:hover:text-gray-400 dark:hover:border-gray-400 dark:focus:border-yellow-600 dark:focus:text-yellow-600
        text-center w-full px-4 py-2 text-sm font-medium leading-5 transition duration-150 ease-in-out border rounded-md`}
        >
          {keybinds[id].action}
        </div>
      </div>
      <div className='flex-col w-1/2'>
        <div>{t('componentKeybindSelectorItem02')}</div>
        <button
          className={`bg-white text-gray-700 border-gray-500 hover:text-white hover:bg-gray-900 hover:border-gray-900 focus:border-yellow-600 focus:text-white
        dark:bg-gray-900 dark:text-gray-400 dark:border-gray-400 dark:hover:text-gray-300 dark:hover:border-gray-300 dark:focus:border-yellow-600 dark:focus:text-yellow-600
          group w-full px-4 py-2 text-sm font-medium leading-5 transition duration-150 ease-in-out border rounded-md`}
          onFocus={(_) => {
            postMessage('EnterEditKeybind', id);
          }}
          onBlur={(_) => {
            postMessage('ExitEditKeybind', id);
          }}
        >
          {keybind ? keybind.keys.join('+') : 'None'}
        </button>
      </div>
      <div className='flex-col w-auto'>
        <span>{t('componentKeybindSelectorItem03')}</span>
        <div className='px-4 py-2'>
          <input
            type='checkbox'
            className='h-4 w-4'
            checked={!keybind?.disabled}
            onChange={(e) => {
              //@ts-ignore lulzsun: hacky
              settings[id].disabled = !e.target.checked;
              updateSettings();
            }}
          />
        </div>
      </div>
    </div>
  );
};

export default KeybindSelector;
