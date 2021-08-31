interface Props {
}

export const Capture: React.FC<Props> = ({}) => {
	return (
    <div className="flex flex-col gap-2 font-medium text-base"> 
      <h1 className="font-bold text-2xl">Capture Mode</h1>
      <label className="inline-flex items-center">
        <input type="radio" className="form-checkbox h-4 w-4 text-gray-600" name="recordMode"/><span className="ml-2 text-gray-700">Automatic</span>
      </label>
      <label className="inline-flex items-center">
        <input type="radio" className="form-checkbox h-4 w-4 text-gray-600" name="recordMode"/><span className="ml-2 text-gray-700">Manual</span>
      </label>
      <label className="inline-flex items-center">
        <input type="radio" className="form-checkbox h-4 w-4 text-gray-600" name="recordMode"/><span className="ml-2 text-gray-700">Off</span>
      </label>

      <h1 className="font-bold text-2xl mt-4">Video Quality</h1>
      <div className="flex gap-4">
        <label className="inline-flex items-center">
          <input type="radio" className="form-checkbox h-4 w-4 text-gray-600" name="quality"/><span className="ml-2 text-gray-700">Low</span>
        </label>
        <label className="inline-flex items-center">
          <input type="radio" className="form-checkbox h-4 w-4 text-gray-600" name="quality"/><span className="ml-2 text-gray-700">Medium</span>
        </label>
        <label className="inline-flex items-center">
          <input type="radio" className="form-checkbox h-4 w-4 text-gray-600" name="quality"/><span className="ml-2 text-gray-700">High</span>
        </label>
        <label className="inline-flex items-center">
          <input type="radio" className="form-checkbox h-4 w-4 text-gray-600" name="quality"/><span className="ml-2 text-gray-700">Custom</span>
        </label>
      </div>
      <div className="flex gap-4">

      </div>

      <h1 className="font-bold text-2xl mt-4">Audio Settings</h1>
      <h1 className="font-bold text-2xl mt-4">Microphone Settings</h1>
    </div>
	)
}

export default Capture;