import { useState } from "react";
import { Link, Route, BrowserRouter as Router, Switch } from "react-router-dom";

interface Props {
  updateSettings: () => void;
  settings: UploadSettings | undefined;
}

export const Upload: React.FC<Props> = ({settings, updateSettings}) => {
  const [subPage, setSubPage] = useState("General");
  
	return (
    <Router>
      <div className="flex flex-col h-full gap-2 font-medium text-base"> 
        <div className="flex flex-row" style={{height: "calc(100%)"}}>
          <div className="w-40 h-full pr-6 border-0 border-r">
            <div className="inline-block text-base align-bottom pb-2 font-bold">Destinations</div>
            <Link to="/settings/upload/streamable" onClick={() => setSubPage("Streamable")} className="flex items-center block py-2 px-4 rounded transition duration-100 hover:bg-blue-700 hover:text-white text-base font-medium">
              Streamable
            </Link>
            {/* <Link to="/settings/upload/youtube" onClick={() => setSubPage("Youtube")} className="flex items-center block py-2 px-4 rounded transition duration-100 hover:bg-blue-700 hover:text-white text-base font-medium">
              Youtube
            </Link> */}
          </div>
          <div className="flex-auto overflow-auto h-full w-full p-7 pt-0">
            <Switch>
              <Route exact path="/settings/upload">               <Streamable settings={settings} updateSettings={updateSettings}/></Route>
              <Route exact path="/settings/upload/streamable">    <Streamable settings={settings} updateSettings={updateSettings}/></Route>
              {/* <Route exact path="/settings/upload/youtube">       <Youtube settings={settings} updateSettings={updateSettings}/></Route> */}
            </Switch>
          </div>
        </div>
      </div>
    </Router>
	)
}

const Streamable: React.FC<Props> = ({settings, updateSettings}) => {
	return (
    <div className="flex flex-col gap-2 font-medium text-base pb-7">
      <div className="font-bold">Streamable</div>
      <div className="flex flex-col">
        Email
        <div className="flex flex-row">
          <input className={`inline-flex align-middle justify-center w-64 h-full px-4 py-2 text-sm font-medium leading-5 text-gray-700 transition duration-150 ease-in-out bg-white border border-gray-300 rounded-md hover:text-gray-500 focus:outline-none focus:border-blue-300 focus:shadow-outline-blue active:bg-gray-50 active:text-gray-800`}
            type="email" defaultValue={settings === undefined ? "" : settings.streamableSettings.email} onBlur={(e) => {
              if(settings !== undefined)
              settings.streamableSettings.email = e.target.value;
              updateSettings();
            }}/>
        </div>
      </div>
      <div className="flex flex-col">
        Password
        <div className="flex flex-row">
          <input className={`inline-flex align-middle justify-center w-64 h-full px-4 py-2 text-sm font-medium leading-5 text-gray-700 transition duration-150 ease-in-out bg-white border border-gray-300 rounded-md hover:text-gray-500 focus:outline-none focus:border-blue-300 focus:shadow-outline-blue active:bg-gray-50 active:text-gray-800`}
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
      </div>
    </div>
	)
}

const Youtube: React.FC<Props> = ({settings, updateSettings}) => {
	return (
    <div className="flex flex-col gap-2 font-medium text-base pb-7">
      <div className="font-bold">Youtube</div>
    </div>
	)
}

const SMB: React.FC<Props> = ({settings, updateSettings}) => {
	return (
    <div className="flex flex-col gap-2 font-medium text-base pb-7">
      <div className="font-bold">SMB</div>
    </div>
	)
}

export default Upload;