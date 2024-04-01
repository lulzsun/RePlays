import { useTranslation } from 'react-i18next';

import Button from '../../components/Button';
import DirectoryBrowser from '../../components/DirectoryBrowser';
import { postMessage } from '../../helpers/messenger';
import React, {KeyboardEvent, ChangeEvent} from 'react';

interface Props {
  updateSettings: () => void;
  settings: StorageSettings | undefined;
}

export const Storage: React.FC<Props> = ({ settings, updateSettings }) => {
  const { t } = useTranslation();

  const validate = (e: ChangeEvent<HTMLInputElement>) => {
    let value: number | string = parseInt(e.target.value);
    if (isNaN(value) || value <= 0) {
      value = -1;
      e.target.value = '';
    }
  }

  const validateAndUpdateSettings = (e: ChangeEvent<HTMLInputElement>, field: keyof StorageSettings) => {
    let value: number | string = parseInt(e.target.value);
    if (isNaN(value) || value <= 0) {
      value = -1;
      e.target.value = '';
    }

    if (settings) {
      (settings[field] as number) = value;
      updateSettings();
    }
  };

  const handleOnChange = (e: ChangeEvent<HTMLInputElement>) => {
    validate(e);
  };

  const handleInputBlur = (e: ChangeEvent<HTMLInputElement>, field: keyof StorageSettings) => {
    validateAndUpdateSettings(e, field);
  };

  const handleInputKeyPress = (e: KeyboardEvent<HTMLInputElement>, field: keyof StorageSettings) => {
    if (e.key === 'Enter') {
      validateAndUpdateSettings(e as unknown as ChangeEvent<HTMLInputElement>, field);
      e.preventDefault();
    }
  };

  return (
    <div className='flex flex-col gap-2 font-medium text-base pb-7'>
      <h1 className='font-semibold text-2xl'>{t('settingsStorageItem01')}</h1>
      <div className='flex flex-col'>
        <span className='text-gray-700 dark:text-gray-400'>{t('settingsStorageItem02')}</span>
        <div className='flex flex-row gap-2'>
          <DirectoryBrowser
            id='videoSaveDir'
            path={settings === undefined ? undefined : settings.videoSaveDir}
          />
          <Button
            text={t('settingsStorageItem03')}
            width={'auto'}
            onClick={(e) => {
              postMessage('ShowFolder', settings === undefined ? 'C:\\' : settings.videoSaveDir);
            }}
          />
        </div>
      </div>
      <div className='flex flex-col'>
        <span className='text-gray-700 dark:text-gray-400'>{t('settingsStorageItem04')}</span>
        <div className='flex flex-row gap-2'>
          <DirectoryBrowser
            id='tempSaveDir'
            path={settings === undefined ? undefined : settings.tempSaveDir}
          />
          <Button
            text={t('settingsStorageItem03')}
            width={'auto'}
            onClick={(e) => {
              postMessage('ShowFolder', settings === undefined ? 'C:\\' : settings.tempSaveDir);
            }}
          />
        </div>
      </div>
      <h1 className='font-semibold text-2xl'>{t('settingsStorageItem05')}</h1>
      <label className='inline-flex items-center'>
        <input
          type='checkbox'
          className='form-checkbox h-4 w-4 text-gray-600'
          defaultChecked={settings === undefined ? false : settings.autoManageSpace}
          onChange={(e) => {
            settings!.autoManageSpace = e.target.checked;
            updateSettings();
          }}
        />
        <span className='ml-2 inline-flex items-center text-gray-700 dark:text-gray-400'>
          {t('settingsStorageItem06')}
        </span>
      </label>
      <span className='font-normal text-sm'>
        {t('settingsStorageItem07')}
        <br />
        {t('settingsStorageItem08')}
      </span>
      <label className='inline-flex items-center text-gray-700 dark:text-gray-400'>{t('settingsStorageItem10')}</label>
      <span className='font-normal text-sm'>
        {t('settingsStorageItem09')}
      </span>
      <div className='flex flex-row items-center'>
        <input
          className="inline-flex w-24 px-2 py-2 text-sm font-medium leading-5 text-gray-700 dark:text-gray-600 transition duration-150 ease-in-out bg-white border border-gray-300 rounded-l-md hover:text-gray-700 focus:outline-none focus:border-blue-300 focus:shadow-outline-blue"
          type="number"
          min="-1"
          placeholder="No Limit"
          defaultValue={settings?.manageSpaceLimit === -1 ? '' : settings?.manageSpaceLimit}
          onBlur={(e) => handleInputBlur(e, 'manageSpaceLimit')}
          onKeyPress={(e) => handleInputKeyPress(e, 'manageSpaceLimit')}
          onChange={(e) => handleOnChange(e)}
        />
        <span className="inline-flex items-center py-2 px-3 border border-l-0 border-gray-300 bg-gray-50 text-gray-500 text-sm rounded-r-md">
          GB
        </span>
      </div>
      <label className='inline-flex items-center text-gray-700 dark:text-gray-400'>{t('settingsStorageItem12')}</label>
      <span className='font-normal text-sm'>
        {t('settingsStorageItem13')}
      </span>
      <div className='flex flex-row items-center'>
        <input
          className="inline-flex w-24 px-2 py-2 text-sm font-medium leading-5 text-gray-700 dark:text-gray-600 transition duration-150 ease-in-out bg-white border border-gray-300 rounded-l-md hover:text-gray-700 focus:outline-none focus:border-blue-300 focus:shadow-outline-blue"
          type="number"
          min="-1"
          placeholder="No Limit"
          defaultValue={settings?.manageTimeLimit ?? -1 === -1 ? '' : settings?.manageTimeLimit}
          onBlur={(e) => handleInputBlur(e, 'manageTimeLimit')}
          onKeyPress={(e) => handleInputKeyPress(e, 'manageTimeLimit')}
          onChange={(e) => handleOnChange(e)}
        />
        <span className="inline-flex items-center py-2 px-3 border border-l-0 border-gray-300 bg-gray-50 text-gray-500 text-sm rounded-r-md">
          {t('settingsStorageItem11')}
        </span>
      </div>
    </div>
  );
};

export default Storage;
