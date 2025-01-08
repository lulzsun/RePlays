import { useTranslation } from 'react-i18next';

import Button from '../../components/Button';
import DropDownMenu from '../../components/DropDownMenu';
import { postMessage } from '../../helpers/messenger';
import {languages, getLanguageName } from '../../internationalization/i18n';

interface Props {
  updateSettings: () => void;
  settings: GeneralSettings | undefined;
}

export const General: React.FC<Props> = ({ settings, updateSettings }) => {
  const { t } = useTranslation();

  return (
    <div className='flex flex-col gap-2 font-medium text-base pb-7'>
      <h1 className='font-semibold text-2xl mt-4'>{t('settingsGeneralItem19')}</h1>
      <DropDownMenu
        text={getLanguageName(settings!.language)}
        width='auto'
        items={[
          {
            name: 'English',
            onClick: () => {
              settings!.language = 'en';
              updateSettings();
            },
          },
          {
            name: 'German',
            onClick: () => {
              settings!.language = 'de';
              updateSettings();
            },
          },
          {
            name: 'Spanish',
            onClick: () => {
              settings!.language = 'es';
              updateSettings();
            },
          },
          {
            name: 'French',
            onClick: () => {
              settings!.language = 'fr';
              updateSettings();
            },
          },
          {
            name: 'Italian',
            onClick: () => {
              settings!.language = 'it';
              updateSettings();
            },
          },
          {
            name: 'Portuguese',
            onClick: () => {
              settings!.language = 'pt';
              updateSettings();
            },
          },
          {
            name: 'Russian',
            onClick: () => {
              settings!.language = 'ru';
              updateSettings();
            },
          },
        ]}
      />
      <h1 className='font-semibold text-2xl'>{t('settingsGeneralItem01')}</h1>
      <label className='inline-flex items-center'>
        <input
          type='checkbox'
          className='form-checkbox h-4 w-4 text-gray-600'
          defaultChecked={settings === undefined ? false : settings.launchStartup}
          onChange={(e) => {
            settings!.launchStartup = e.target.checked;
            updateSettings();
          }}
        />
        <span className='ml-2 text-gray-700 dark:text-gray-400'>{t('settingsGeneralItem02')}</span>
      </label>
      <label className='inline-flex items-center'>
        <input
          type='checkbox'
          className='form-checkbox h-4 w-4 text-gray-600'
          defaultChecked={settings === undefined ? false : settings.startMinimized}
          onChange={(e) => {
            settings!.startMinimized = e.target.checked;
            updateSettings();
          }}
        />
        <span className='ml-2 text-gray-700 dark:text-gray-400'>{t('settingsGeneralItem03')}</span>
      </label>
      <label className='inline-flex items-center'>
        <input
          type='checkbox'
          className='form-checkbox h-4 w-4 text-gray-600'
          defaultChecked={settings === undefined ? false : settings.closeToTray}
          onChange={(e) => {
            settings!.closeToTray = e.target.checked;
            updateSettings();
          }}
        />
        {/* This setting does not have translations, this is temporary */}
        <span className='ml-2 text-gray-700 dark:text-gray-400'>Close to Tray</span>
      </label>

      <h1 className='font-semibold text-2xl mt-4'>{t('settingsGeneralItem04')}</h1>
      <DropDownMenu
        text={settings === undefined ? 'System' : settings.theme}
        width={'auto'}
        items={[
          {
            name: t('settingsGeneralItem05'),
            onClick: () => {
              settings!.theme = 'System';
              updateSettings();
            },
          },
          {
            name: t('settingsGeneralItem06'),
            onClick: () => {
              settings!.theme = 'Light';
              updateSettings();
            },
          },
          {
            name: t('settingsGeneralItem07'),
            onClick: () => {
              settings!.theme = 'Dark';
              updateSettings();
            },
          },
        ]}
      />

      <h1 className='font-semibold text-2xl mt-4'>{t('settingsGeneralItem08')}</h1>
      <div
        onChange={(e) => {
          if (settings) settings.update = (e?.target as HTMLInputElement).value;
          updateSettings();
        }}
      >
        <label className='inline-flex items-center'>
          <input
            type='radio'
            name='update'
            className='form-checkbox h-4 w-4 text-gray-600'
            value='automatic'
            defaultChecked={settings?.update === 'automatic' ? true : false}
          />
          <span className='px-2 text-gray-700 dark:text-gray-400'>
            {t('settingsGeneralItem09')}
          </span>
        </label>
        <label className='inline-flex items-center'>
          <input
            type='radio'
            name='update'
            className='form-checkbox h-4 w-4 text-gray-600'
            value='prompt'
            defaultChecked={settings?.update === 'prompt' ? true : false}
          />
          <span className='px-2 text-gray-700 dark:text-gray-400'>
            {t('settingsGeneralItem10')}
          </span>
        </label>
        <label className='inline-flex items-center'>
          <input
            type='radio'
            name='update'
            className='form-checkbox h-4 w-4 text-gray-600'
            value='off'
            defaultChecked={settings?.update === 'off' ? true : false}
          />
          <span className='px-2 text-gray-700 dark:text-gray-400'>
            {t('settingsGeneralItem11')}
          </span>
        </label>
      </div>

      <div className='flex flex-col gap-1'>
        {t('settingsGeneralItem12')}
        <DropDownMenu
          text={settings === undefined ? 'Stable' : settings.updateChannel}
          width={'auto'}
          items={[
            {
              name: t('settingsGeneralItem13'),
              onClick: () => {
                settings!.updateChannel = 'Stable';
                updateSettings();
              },
            },
            {
              name: t('settingsGeneralItem14'),
              onClick: () => {
                settings!.updateChannel = 'Nightly';
                updateSettings();
              },
            },
          ]}
        />
      </div>

      <span className='text-gray-700 dark:text-gray-400'>
        {t('settingsGeneralItem15')} {settings?.currentVersion}
      </span>
      <span className='text-gray-700 dark:text-gray-400'>
        {t('settingsGeneralItem16')} {settings?.latestVersion}
      </span>
      {/* <Button text="Change logs" width={"auto"}/> */}
      <Button
        text={
          settings?.currentVersion === settings?.latestVersion
            ? t('settingsGeneralItem17')
            : t('settingsGeneralItem18')
        }
        width={'auto'}
        onClick={() => {
          let forceUpdate = settings?.currentVersion !== settings?.latestVersion;
          postMessage('CheckForUpdates', forceUpdate.toString());
        }}
      />
    </div>
  );
};

export default General;
