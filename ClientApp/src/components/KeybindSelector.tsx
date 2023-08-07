import { postMessage } from '../helpers/messenger';
import DropDownMenu from './DropDownMenu';

interface Props {
  id: string;
  keybind?: string[];
}

interface Keybind {
  action: string;
  description?: string;
}

const keybinds: { [key: string]: Keybind } = {
  "StartStopRecording": { action: "Toggle Recording" },
  "CreateBookmark": { action: "Create Bookmark" }
};

export const KeybindSelector: React.FC<Props> = ({id, keybind}) => {
	return (
    <div className="flex py-2 space-x-5 whitespace-nowrap border-t border-gray-700 dark:border-white">
      <div className="flex-col w-1/2">
        <span>Action</span>
        <DropDownMenu text={keybinds[id].action}
        items={[
          {name: "480p", onClick: () => {}},
          {name: "720p", onClick: () => {}},
        ]}/>
      </div>
      <div className="flex-col w-1/2">
        <span>Keybind</span>
        <div className={`cursor-pointer bg-white dark:bg-gray-900 text-gray-700 dark:text-white border-gray-500 dark:border-gray-400 hover:text-gray-700 focus:border-blue-300
          px-4 py-2 text-sm font-medium leading-5 transition duration-150 ease-in-out border rounded-md focus:outline-none focus:shadow-outline-blue`}
          onClick={(e) => { e.currentTarget.style.borderColor = "#efaf2b" }} 
          onFocus={(e) => { e.currentTarget.style.borderColor = "rgba(156, 163, 175, var(--tw-border-opacity))" }}
        >
          {keybind ? keybind.join("+") : "None"}
        </div>
      </div>
      <div className="flex-col w-auto">
        <span>Enable</span>
        <div className="px-4 py-2">
          <input type="checkbox" className="h-4 w-4" checked={true} onChange={(e) => {}}/>
        </div>
      </div>
    </div>
	)
}

export default KeybindSelector;