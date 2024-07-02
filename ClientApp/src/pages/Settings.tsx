import { useTranslation } from 'react-i18next';

import { Link, Route, Routes, Navigate, useLocation } from 'react-router-dom';
import About from './Settings/About';
import Storage from './Settings/Storage';
import Capture from './Settings/Capture';
import General from './Settings/General';
import Help from './Settings/Help';
import Upload from './Settings/Upload';
import Detection from './Settings/Detection';
import KeyBind from './Settings/Keybind';
import { postMessage } from '../helpers/messenger';

interface Props {
  userSettings: UserSettings | undefined;
  setUserSettings: React.Dispatch<React.SetStateAction<UserSettings | undefined>>;
}

export const Settings: React.FC<Props> = ({ userSettings, setUserSettings }) => {
  const { t } = useTranslation();
  const location = useLocation();
  function updateSettings() {
    postMessage('UpdateSettings', userSettings);
    let newSettings = Object.assign({}, userSettings);
    setUserSettings(newSettings);
  }

  const formatPageName = (pathname: string) => {
    return pathname.replace("/settings/", "");
  };

  return (
    <div className='flex flex-col h-full border-0 border-b'>
      <div style={{ height: '50px' }}>
        {t('settingsTitle')}
        <p className='inline-block px-1'>/</p>{' '}
        <div className='inline-block text-base align-bottom'>
          {formatPageName(location.pathname) }
        </div>
      </div>
      <div style={{ height: 'calc(100% - 50px)' }} className='flex flex-row'>
        <div className='w-40 h-full pr-6 border-0 border-r'>
          <Link
            to='General'
            className='flex items-center block py-2 px-4 rounded transition duration-100 hover:bg-gray-900 hover:text-white text-base font-medium'
          >
            {t('settingsItem01')}
          </Link>
          <Link
            to='Capture'
            className='flex items-center block py-2 px-4 rounded transition duration-100 hover:bg-gray-900 hover:text-white text-base font-medium'
          >
            {t('settingsItem02')}
          </Link>
          <Link
            to='Detection'
            className='flex items-center block py-2 px-4 rounded transition duration-100 hover:bg-gray-900 hover:text-white text-base font-medium'
          >
            {t('settingsItem03')}
          </Link>
          <Link
            to='Keybinds'
            className='flex items-center block py-2 px-4 rounded transition duration-100 hover:bg-gray-900 hover:text-white text-base font-medium'
          >
            {t('settingsItem04')}
          </Link>
          <Link
            to='Upload'
            className='flex items-center block py-2 px-4 rounded transition duration-100 hover:bg-gray-900 hover:text-white text-base font-medium'
          >
            {t('settingsItem05')}
          </Link>
          <Link
            to='Storage'
            className='flex items-center block py-2 px-4 rounded transition duration-100 hover:bg-gray-900 hover:text-white text-base font-medium'
          >
            {t('settingsItem06')}
          </Link>
          <Link
            to='Help'
            className='flex items-center block py-2 px-4 rounded transition duration-100 hover:bg-gray-900 hover:text-white text-base font-medium'
          >
            {t('settingsItem07')}
          </Link>
          <Link
            to='About'
            className='flex items-center block py-2 px-4 rounded transition duration-100 hover:bg-gray-900 hover:text-white text-base font-medium'
          >
            {t('settingsItem08')}
          </Link>
        </div>
        <div className='flex-auto overflow-auto h-full w-full p-7 pt-0 pb-0'>
          <Routes>
            <Route path='*' element={<Navigate to='General' />} />
            <Route path='General' element={<General updateSettings={updateSettings} settings={userSettings?.generalSettings} />} />
            <Route path='Capture' element={<Capture updateSettings={updateSettings} settings={userSettings?.captureSettings} />} />
            <Route path='Detection' element={<Detection updateSettings={updateSettings} settings={userSettings?.detectionSettings} />} />
            <Route path='Keybinds' element={<KeyBind updateSettings={updateSettings} settings={userSettings?.keybindSettings} />} />
            <Route path='Upload' element={<Upload updateSettings={updateSettings} settings={userSettings?.uploadSettings} />} />
            <Route path='Storage' element={<Storage updateSettings={updateSettings} settings={userSettings?.storageSettings} />} />
            <Route path='Help' element={<Help />} />
            <Route path='About' element={<About />} />
          </Routes>
        </div>
      </div>
    </div>
  );
};

export default Settings;
