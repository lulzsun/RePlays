import Button from "../../components/Button";
import DropDownMenu from "../../components/DropDownMenu";

interface Props {
}

export const General: React.FC<Props> = ({}) => {
	return (
    <div className="flex flex-col gap-2 font-medium text-base"> 
      <h1 className="font-bold text-2xl">Startup Settings</h1>
      <label className="inline-flex items-center">
        <input type="checkbox" className="form-checkbox h-4 w-4 text-gray-600"/><span className="ml-2 text-gray-700">Launch app when Windows starts</span>
      </label>
      <label className="inline-flex items-center">
        <input type="checkbox" className="form-checkbox h-4 w-4 text-gray-600"/><span className="ml-2 text-gray-700">Start minimized</span>
      </label>
      <h1 className="font-bold text-2xl mt-4">Theme</h1>

      <DropDownMenu text={"System"} width={"auto"}
      items={[
        {name: "System", onClick: () => { }},
        {name: "Light", onClick: () => { }},
        {name: "Dark", onClick: () => { }},
      ]}/> 

      <h1 className="font-bold text-2xl mt-4">Update</h1>
      <label className="inline-flex items-center">
        <input type="radio" className="form-checkbox h-4 w-4 text-gray-600" name="update"/><span className="ml-2 text-gray-700">Automatic</span>
      </label>
      <label className="inline-flex items-center">
        <input type="radio" className="form-checkbox h-4 w-4 text-gray-600" name="update"/><span className="ml-2 text-gray-700">Prompt</span>
      </label>
      <label className="inline-flex items-center">
        <input type="radio" className="form-checkbox h-4 w-4 text-gray-600" name="update"/><span className="ml-2 text-gray-700">Off</span>
      </label>

      <h1 className="font-bold text-2xl mt-4">Version</h1>
      <span className="ml-2 text-gray-700">Current Version: 0.0.1</span>
      <span className="ml-2 text-gray-700">Latest Version: 0.0.1</span>
      <Button text="Change logs" width={"auto"}/>
    </div>
	)
}

export default General;