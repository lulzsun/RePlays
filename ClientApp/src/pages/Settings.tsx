import { useState } from "react";
import { Link, Route, HashRouter as Router, Switch, useParams } from "react-router-dom";
import About from "./Settings/About";
import Advanced from "./Settings/Advanced";
import Capture from "./Settings/Capture";
import General from "./Settings/General";
import Help from "./Settings/Help";
import Upload from "./Settings/Upload";
import Games from "./Settings/Games"
import { postMessage } from '../helpers/messenger';

type SettingsParams = {
  page: string;
};

interface Props {
  userSettings: UserSettings | undefined
  setUserSettings: React.Dispatch<React.SetStateAction<UserSettings | undefined>>
}

export const Settings: React.FC<Props> = ({userSettings, setUserSettings}) => {
  let { page } = useParams<SettingsParams>();
  function updateSettings() {
    postMessage("UpdateSettings", userSettings);
    let newSettings = Object.assign({}, userSettings);
    setUserSettings(newSettings);
  }
	return (
    <Router>
      <div className="flex flex-col h-full border-0 border-b"> 
        <div style={{height: "50px"}}>Settings <p className="inline-block px-1">/</p> <div className="inline-block text-base align-bottom">{page}</div></div>
        <div style={{height: "calc(100% - 50px)"}} className="flex flex-row">
          <div className="w-40 h-full pr-6 border-0 border-r">
            <Link to="/settings/General" className="flex items-center block py-2 px-4 rounded transition duration-100 hover:bg-gray-900 hover:text-white text-base font-medium">
              General
            </Link>
            <Link to="/settings/Capture" className="flex items-center block py-2 px-4 rounded transition duration-100 hover:bg-gray-900 hover:text-white text-base font-medium">
              Capture
            </Link>
            <Link to="/settings/Upload" className="flex items-center block py-2 px-4 rounded transition duration-100 hover:bg-gray-900 hover:text-white text-base font-medium">
              Upload
            </Link>
            <Link to="/settings/CustomGames" className="flex items-center block py-2 px-4 rounded transition duration-100 hover:bg-gray-900 hover:text-white text-base font-medium">
              Games
            </Link>
            <Link to="/settings/Advanced" className="flex items-center block py-2 px-4 rounded transition duration-100 hover:bg-gray-900 hover:text-white text-base font-medium">
              Advanced
            </Link>
            <Link to="/settings/Help" className="flex items-center block py-2 px-4 rounded transition duration-100 hover:bg-gray-900 hover:text-white text-base font-medium">
              Help
            </Link>
            <Link to="/settings/About" className="flex items-center block py-2 px-4 rounded transition duration-100 hover:bg-gray-900 hover:text-white text-base font-medium">
              About
            </Link>
          </div>
          <div className="flex-auto overflow-auto h-full w-full p-7 pt-0 pb-0">
            <Switch>
              <Route exact path="/settings/general"> <General updateSettings={updateSettings} settings={userSettings?.generalSettings}/></Route>
              <Route exact path="/settings/capture"> <Capture updateSettings={updateSettings} settings={userSettings?.captureSettings} keybindings={userSettings?.keybindings}/></Route>
              <Route exact path="/settings/upload">  <Upload updateSettings={updateSettings} settings={userSettings?.uploadSettings}/></Route>
              <Route exact path="/settings/Games">  <Games updateSettings={updateSettings} games={userSettings?.customGames}/></Route>
              <Route exact path="/settings/advanced"><Advanced updateSettings={updateSettings} settings={userSettings?.advancedSettings}/></Route>
              <Route exact path="/settings/help">    <Help/></Route>
              <Route exact path="/settings/about">   <About/></Route>
            </Switch>
          </div>
        </div>
      </div>
    </Router>
	)
}

export default Settings;