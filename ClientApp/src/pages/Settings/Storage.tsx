import { useTranslation } from 'react-i18next';

import Button from '../../components/Button';
import DirectoryBrowser from '../../components/DirectoryBrowser';
import { postMessage } from '../../helpers/messenger';

interface Props {
  updateSettings: () => void;
  settings: StorageSettings | undefined;
}

export const Storage: React.FC<Props> = ({ settings, updateSettings }) => {
  const { t } = useTranslation();

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
      {/* <div className="flex flex-col gap-2">
        <span className="text-gray-700 dark:text-gray-400">Additional Video Folders</span>
        <span className="font-normal text-sm">Add folders to sync additional videos to your sessions and clips.</span>
        <Button text="Add Folder" width={"auto"}/>
      </div> */}

      <h1 className='font-semibold text-2xl mt-4'>{t('settingsStorageItem05')}</h1>
      <div>
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
          <span className='ml-2 text-gray-700 dark:text-gray-400'>
            {t('settingsStorageItem06')}
          </span>
        </label>
        <div className='flex flex-col gap-2 ml-6'>
          <span className='font-normal text-sm'>
            {t('settingsStorageItem07')}
            <br />
            {t('settingsStorageItem08')}
          </span>
          <div className='flex flex-row gap-2 text-sm'>
            <div className='flex flex-col'>
              {t('settingsStorageItem09')}
              <div className='flex flex-row'>
                <input
                  className={`inline-flex align-middle justify-center w-32 h-full px-4 py-2 text-sm font-medium leading-5 text-gray-700 dark:text-gray-400 transition duration-150 ease-in-out bg-white border border-gray-300 rounded-md hover:text-gray-700 focus:outline-none focus:border-blue-300 focus:shadow-outline-blue active:bg-gray-50 active:text-gray-800`}
                  type='number'
                  defaultValue={
                    settings === undefined || settings.manageSpaceLimit === -1
                      ? ''
                      : settings.manageSpaceLimit
                  }
                  onChange={(e) => {
                    if (parseInt(e.target.value) < 0) e.target.value = '';
                    else {
                      if (settings) {
                        if (Number.isInteger(parseInt(e.target.value)))
                          settings.manageSpaceLimit = parseInt(e.target.value);
                        else settings.manageSpaceLimit = -1;
                        updateSettings();
                      }
                    }
                  }}
                />
                <span className='inline-flex align-middle justify-center h-full px-2 py-2 text-sm font-medium'>
                  GB
                </span>
              </div>
            </div>
            <div className='flex flex-col'>
              {t('settingsStorageItem10')}
              <div className='flex flex-row'>
                <input
                  className={`inline-flex align-middle justify-center w-32 h-full px-4 py-2 text-sm font-medium leading-5 text-gray-700 dark:text-gray-400 transition duration-150 ease-in-out bg-white border border-gray-300 rounded-md hover:text-gray-700 focus:outline-none focus:border-blue-300 focus:shadow-outline-blue active:bg-gray-50 active:text-gray-800`}
                  type='number'
                  defaultValue={
                    settings === undefined || settings.manageTimeLimit === -1
                      ? ''
                      : settings.manageTimeLimit
                  }
                  onChange={(e) => {
                    if (parseInt(e.target.value) < 0) e.target.value = '';
                    else {
                      if (settings) {
                        if (Number.isInteger(parseInt(e.target.value)))
                          settings.manageTimeLimit = parseInt(e.target.value);
                        else settings.manageTimeLimit = -1;
                        updateSettings();
                      }
                    }
                  }}
                />
                <div className='flex flex-col'>
                  <span className='relative inline-flex align-middle justify-left h-full px-2 py-2 text-sm font-medium'>
                    {t('settingsStorageItem11')}
                    <span className='absolute mt-5 inline-flex align-middle justify-left w-32 h-full text-xs font-normal'>
                      {t('settingsStorageItem12')}
                    </span>
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Storage;
