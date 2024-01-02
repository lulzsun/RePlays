import { useTranslation } from 'react-i18next';

import { Link, Route, HashRouter as Router, Switch, useParams } from 'react-router-dom';
import About from './Settings/About';
import Storage from './Settings/Storage';
import Capture from './Settings/Capture';
import General from './Settings/General';
import Help from './Settings/Help';
import Upload from './Settings/Upload';
import Detection from './Settings/Detection';
import KeyBind from './Settings/Keybind';
import { postMessage } from '../helpers/messenger';

type SettingsParams = {
  page: string;
};

interface Props {
  userSettings: UserSettings | undefined;
  setUserSettings: React.Dispatch<React.SetStateAction<UserSettings | undefined>>;
}

export const Settings: React.FC<Props> = ({ userSettings, setUserSettings }) => {
  const { t } = useTranslation();

  let { page } = useParams<SettingsParams>();
  function updateSettings() {
    postMessage('UpdateSettings', userSettings);
    let newSettings = Object.assign({}, userSettings);
    setUserSettings(newSettings);
  }
  return (
    <Router>
      <div className='flex flex-col h-full border-0 border-b'>
        <div style={{ height: '50px' }}>
          {t('settingsTitle')}
          <p className='inline-block px-1'>/</p>{' '}
          <div className='inline-block text-base align-bottom'>{page}</div>
        </div>
        <div style={{ height: 'calc(100% - 50px)' }} className='flex flex-row'>
          <div className='w-40 h-full pr-6 border-0 border-r'>
            <Link
              to='/settings/General'
              className='flex items-center block py-2 px-4 rounded transition duration-100 hover:bg-gray-900 hover:text-white text-base font-medium'
            >
              {t('settingsItem01')}
            </Link>
            <Link
              to='/settings/Capture'
              className='flex items-center block py-2 px-4 rounded transition duration-100 hover:bg-gray-900 hover:text-white text-base font-medium'
            >
              {t('settingsItem02')}
            </Link>
            <Link
              to='/settings/Detection'
              className='flex items-center block py-2 px-4 rounded transition duration-100 hover:bg-gray-900 hover:text-white text-base font-medium'
            >
              {t('settingsItem03')}
            </Link>
            <Link
              to='/settings/Keybinds'
              className='flex items-center block py-2 px-4 rounded transition duration-100 hover:bg-gray-900 hover:text-white text-base font-medium'
            >
              {t('settingsItem04')}
            </Link>
            <Link
              to='/settings/Upload'
              className='flex items-center block py-2 px-4 rounded transition duration-100 hover:bg-gray-900 hover:text-white text-base font-medium'
            >
              {t('settingsItem05')}
            </Link>
            <Link
              to='/settings/Storage'
              className='flex items-center block py-2 px-4 rounded transition duration-100 hover:bg-gray-900 hover:text-white text-base font-medium'
            >
              {t('settingsItem06')}
            </Link>
            <Link
              to='/settings/Help'
              className='flex items-center block py-2 px-4 rounded transition duration-100 hover:bg-gray-900 hover:text-white text-base font-medium'
            >
              {t('settingsItem07')}
            </Link>
            <Link
              to='/settings/About'
              className='flex items-center block py-2 px-4 rounded transition duration-100 hover:bg-gray-900 hover:text-white text-base font-medium'
            >
              {t('settingsItem08')}
            </Link>
          </div>
          <div className='flex-auto overflow-auto h-full w-full p-7 pt-0 pb-0'>
            <Switch>
              <Route exact path='/settings/general'>
                <General updateSettings={updateSettings} settings={userSettings?.generalSettings} />
              </Route>
              <Route exact path='/settings/capture'>
                <Capture updateSettings={updateSettings} settings={userSettings?.captureSettings} />
              </Route>
              <Route exact path='/settings/detection'>
                <Detection
                  updateSettings={updateSettings}
                  settings={userSettings?.detectionSettings}
                />
              </Route>
              <Route exact path='/settings/keybinds'>
                <KeyBind updateSettings={updateSettings} settings={userSettings?.keybindSettings} />
              </Route>
              <Route exact path='/settings/upload'>
                <Upload updateSettings={updateSettings} settings={userSettings?.uploadSettings} />
              </Route>
              <Route exact path='/settings/storage'>
                <Storage updateSettings={updateSettings} settings={userSettings?.storageSettings} />
              </Route>
              <Route exact path='/settings/help'>
                <Help />
              </Route>
              <Route exact path='/settings/about'>
                <About />
              </Route>
            </Switch>
          </div>
        </div>
      </div>
    </Router>
  );
};

export default Settings;
