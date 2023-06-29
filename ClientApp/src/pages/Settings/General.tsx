import Button from "../../components/Button";
import DropDownMenu from "../../components/DropDownMenu";
import { postMessage } from '../../helpers/messenger';

interface Props {
  updateSettings: () => void;
  settings: GeneralSettings | undefined;
}

export const General: React.FC<Props> = ({settings, updateSettings}) => {
	return (
    <div className="flex flex-col gap-2 font-medium text-base pb-7"> 
      <h1 className="font-semibold text-2xl">Startup Settings</h1>
      <label className="inline-flex items-center">
        <input type="checkbox" className="form-checkbox h-4 w-4 text-gray-600"
          defaultChecked={settings === undefined ? false : settings.launchStartup}
          onChange={(e) => {settings!.launchStartup = e.target.checked; updateSettings();}}/>
        <span className="ml-2 text-gray-700 dark:text-gray-400">Launch app on system startup</span>
      </label>
      <label className="inline-flex items-center">
        <input type="checkbox" className="form-checkbox h-4 w-4 text-gray-600"
         defaultChecked={settings === undefined ? false : settings.startMinimized}
         onChange={(e) => {settings!.startMinimized = e.target.checked; updateSettings();}}/>
        <span className="ml-2 text-gray-700 dark:text-gray-400">Start minimized</span>
      </label>
      
      <h1 className="font-semibold text-2xl mt-4">Theme</h1>
      <DropDownMenu text={(settings === undefined ? "System" : settings.theme)} width={"auto"}
      items={[
        {name: "System", onClick: () => {settings!.theme = "System"; updateSettings();}},
        {name: "Light", onClick: () => {settings!.theme = "Light"; updateSettings();}},
        {name: "Dark", onClick: () => {settings!.theme = "Dark"; updateSettings();}},
      ]}/> 

      <h1 className="font-semibold text-2xl mt-4">Update</h1>
      <div onChange={e => {if(settings) settings.update = (e?.target as HTMLInputElement).value; updateSettings();}}>
        <label className="inline-flex items-center">
          <input type="radio" name="update" className="form-checkbox h-4 w-4 text-gray-600" value="automatic"
            defaultChecked={(settings?.update === "automatic" ? true : false)}/>
          <span className="px-2 text-gray-700 dark:text-gray-400">Automatic</span>
        </label>
        <label className="inline-flex items-center">
          <input type="radio" name="update" className="form-checkbox h-4 w-4 text-gray-600" value="prompt"
            defaultChecked={(settings?.update === "prompt" ? true : false)}/>
          <span className="px-2 text-gray-700 dark:text-gray-400">Prompt</span>
        </label>
        <label className="inline-flex items-center">
          <input type="radio" name="update" className="form-checkbox h-4 w-4 text-gray-600" value="off" 
            defaultChecked={(settings?.update === "off" ? true : false)}/>
          <span className="px-2 text-gray-700 dark:text-gray-400">Off</span>
        </label>
      </div>

      <div className="flex flex-col gap-1">
        Update Channel
        <DropDownMenu text={(settings === undefined ? "Stable" : settings.updateChannel)} width={"auto"}
        items={[
          {name: "Stable", onClick: () => {settings!.updateChannel = "Stable"; updateSettings();}},
          {name: "Nightly", onClick: () => {settings!.updateChannel = "Nightly"; updateSettings();}},
        ]}/> 
      </div>

      <span className="text-gray-700 dark:text-gray-400">Current Version: {settings?.currentVersion}</span>
      <span className="text-gray-700 dark:text-gray-400">Latest Version: {settings?.latestVersion}</span>
      {/* <Button text="Change logs" width={"auto"}/> */}
      <Button text={settings?.currentVersion === settings?.latestVersion ? "Check for Updates" : "Update to Latest"} 
      width={"auto"} onClick={() => { 
        let forceUpdate = settings?.currentVersion !== settings?.latestVersion;
        postMessage("CheckForUpdates", forceUpdate.toString());
      }}/>
    </div>
	)
}

export default General;