import { useState } from "react";
import Button from "../../components/Button";
import DirectoryBrowser from "../../components/DirectoryBrowser";
import { postMessage } from "../../helpers/messenger";

interface Props {
  updateSettings: () => void;
  settings: UploadSettings | undefined;
}

export const Upload: React.FC<Props> = ({settings, updateSettings}) => {
    const [subPage, setSubPage] = useState('RePlays');

	return (
    <div className="flex flex-col h-full gap-2 font-medium text-base"> 
      <div className="flex flex-row" style={{height: "calc(100%)"}}>
        <div className="w-48 h-full pr-6 border-0 border-r">
          <div className="inline-block text-base align-bottom pb-2 font-semibold">Destinations</div>
          <div onClick={() => setSubPage("RePlays")} className="cursor-pointer flex items-center block py-2 px-4 rounded transition duration-100 hover:bg-blue-700 hover:text-white text-base font-medium">
            RePlays
          </div>
          <div onClick={() => setSubPage("Streamable")} className="cursor-pointer flex items-center block py-2 px-4 rounded transition duration-100 hover:bg-blue-700 hover:text-white text-base font-medium">
            Streamable
          </div>
          {/* <div onClick={() => setSubPage("YouTube")} className="cursor-pointer flex items-center block py-2 px-4 rounded transition duration-100 hover:bg-blue-700 hover:text-white text-base font-medium">
            YouTube
          </div> */}
          <div onClick={() => setSubPage("LocalFolder")} className="cursor-pointer flex items-center block py-2 px-4 rounded transition duration-100 hover:bg-blue-700 hover:text-white text-base font-medium">
            Local Folder
          </div>
        </div>
        <div className="flex-auto overflow-auto h-full w-full p-7 pt-0">
          {(subPage === 'RePlays' ? <RePlays settings={settings} updateSettings={updateSettings} /> : "")}
          {(subPage === 'Streamable' ? <Streamable settings={settings} updateSettings={updateSettings}/> : "")}
          {(subPage === 'LocalFolder' ? <LocalFolder settings={settings} updateSettings={updateSettings}/> : "")}
        </div>
      </div>
    </div>
	)
}

const RePlays: React.FC<Props> = ({ settings, updateSettings }) => {
    return (
        <div className="flex flex-col gap-2 font-medium text-base pb-7">
            <div className="font-semibold">RePlays</div>
            <div className="flex flex-col">
                Email
                <input className={`inline-flex align-middle justify-center w-64 h-full px-4 py-2 text-sm font-medium leading-5 text-gray-700 transition duration-150 ease-in-out bg-white border border-gray-300 rounded-md hover:text-gray-700 focus:outline-none focus:border-blue-300 focus:shadow-outline-blue active:bg-gray-50 active:text-gray-800`}
                    type="email" defaultValue={settings === undefined ? "" : settings.rePlaysSettings.email} onBlur={(e) => {
                        if (settings !== undefined)
                            settings.rePlaysSettings.email = e.target.value;
                        updateSettings();
                    }} />
            </div>
            <div className="flex flex-col">
                Password
                <input className={`inline-flex align-middle justify-center w-64 h-full px-4 py-2 text-sm font-medium leading-5 text-gray-700 transition duration-150 ease-in-out bg-white border border-gray-300 rounded-md hover:text-gray-700 focus:outline-none focus:border-blue-300 focus:shadow-outline-blue active:bg-gray-50 active:text-gray-800`}
                    type="password" defaultValue={settings === undefined ? "" : settings.rePlaysSettings.password}
                    onFocus={(e) => {
                        e.target.value = "";
                    }}
                    onBlur={(e) => {
                        if (settings !== undefined)
                            settings.rePlaysSettings.password = e.target.value;
                        updateSettings();
                    }} />
            </div>
            <a onClick={(e) => { postMessage("OpenLink", "https://replays.app") }} className="underline text-gray-300 hover:text-gray-200 cursor-pointer">RePlays.app</a>
        </div>
    )
}

const Streamable: React.FC<Props> = ({settings, updateSettings}) => {
	return (
    <div className="flex flex-col gap-2 font-medium text-base pb-7">
      <div className="font-semibold">Streamable</div>
      <div className="flex flex-col">
        Email
        <input className={`inline-flex align-middle justify-center w-64 h-full px-4 py-2 text-sm font-medium leading-5 text-gray-700 transition duration-150 ease-in-out bg-white border border-gray-300 rounded-md hover:text-gray-700 focus:outline-none focus:border-blue-300 focus:shadow-outline-blue active:bg-gray-50 active:text-gray-800`}
          type="email" defaultValue={settings === undefined ? "" : settings.streamableSettings.email} onBlur={(e) => {
            if(settings !== undefined)
            settings.streamableSettings.email = e.target.value;
            updateSettings();
          }}/>
      </div>
      <div className="flex flex-col">
        Password
          <input className={`inline-flex align-middle justify-center w-64 h-full px-4 py-2 text-sm font-medium leading-5 text-gray-700 transition duration-150 ease-in-out bg-white border border-gray-300 rounded-md hover:text-gray-700 focus:outline-none focus:border-blue-300 focus:shadow-outline-blue active:bg-gray-50 active:text-gray-800`}
            type="password" defaultValue={settings === undefined ? "" : settings.streamableSettings.password}
            onFocus={(e) => {
              e.target.value = "";
            }}
            onBlur={(e) => {
              if(settings !== undefined)
              settings.streamableSettings.password = e.target.value;
              updateSettings();
            }}/>
      </div>
      <a onClick={(e) => { postMessage("OpenLink", "https://streamable.com/") }} className="underline text-gray-300 hover:text-gray-200 cursor-pointer">Streamable.com</a>
    </div>
	)
}



const Youtube: React.FC<Props> = ({settings, updateSettings}) => {
	return (
    <div className="flex flex-col gap-2 font-medium text-base pb-7">
      <div className="font-semibold">Youtube</div>
    </div>
	)
}

const LocalFolder: React.FC<Props> = ({settings, updateSettings}) => {
	return (
    <div className="flex flex-col gap-2 font-medium text-base pb-7">
      <div className="font-semibold">Local Folder</div>
      <div className="flex flex-col">
        <div className="flex flex-row">
          <DirectoryBrowser id="localFolderDir" path={settings === undefined ? undefined : settings.localFolderSettings.dir}/>
          <p>&nbsp;&nbsp;</p>
          <Button text="Open Folder"  width={"auto"} onClick={(e) => {postMessage("ShowFolder", (settings === undefined ? "C:\\" : settings.localFolderSettings.dir))}}/>
        </div>
      </div>
    </div>
	)
}

export default Upload;