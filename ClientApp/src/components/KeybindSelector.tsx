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
        <div>Keybind</div>
        <button className={`bg-white text-gray-700 border-gray-500 hover:text-white hover:bg-gray-900 hover:border-gray-900 focus:border-yellow-600 focus:bg-yellow-600 focus:text-white
        dark:bg-gray-900 dark:text-gray-400 dark:border-gray-400 dark:hover:text-gray-300 dark:hover:border-gray-300 dark:focus:border-yellow-600 dark:focus:text-yellow-600
          group w-full px-4 py-2 text-sm font-medium leading-5 transition duration-150 ease-in-out border rounded-md`}
          onBlur={(e) => { console.log("I'm out of focus") }} 
          onFocus={(e) => { console.log("I'm in focus") }}
        >
          <a>
            {keybind ? keybind.join("+") : "None"}
          </a>
        </button>
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