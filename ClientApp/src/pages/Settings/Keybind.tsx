import { useTranslation } from 'react-i18next';

import KeybindSelector from '../../components/KeybindSelector';

interface Props {
  updateSettings: () => void;
  settings: KeybindSettings | undefined;
}

export const Keybind: React.FC<Props> = ({ settings, updateSettings }) => {
  const { t } = useTranslation();

  console.log(settings);

  return (
    <div className='flex flex-col gap-0 font-medium text-base pb-7 lg:max-w-screen-md'>
      <h1 className='font-semibold text-2xl'>{t('settingsKeybindsItem01')}</h1>
      <div className='flex flex-col gap-1 pt-4'>
        <KeybindSelector
          id='StartStopRecording'
          settings={settings}
          updateSettings={updateSettings}
          keybind={settings?.StartStopRecording}
        />
      </div>
      <div className='flex flex-col gap-1 border-b border-gray-500 dark:border-gray-500'>
        <KeybindSelector
          id='CreateBookmark'
          settings={settings}
          updateSettings={updateSettings}
          keybind={settings?.CreateBookmark}
        />
      </div>
    </div>
  );
};

export default Keybind;
