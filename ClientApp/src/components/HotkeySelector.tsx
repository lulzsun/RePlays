import { ReactElement } from 'react';
import { postMessage } from '../helpers/messenger';

interface Props {
  id: string;
  keybind?: string[];
  width?: string;
  icon?: ReactElement;
}

export const HotkeySelector: React.FC<Props> = ({id, keybind, icon, width="full"}) => {
	return (
    <div className="relative inline-block text-left dropdown">
      <button className={`bg-white dark:bg-gray-900 text-gray-700 dark:text-gray-400 border-gray-500 dark:border-gray-400 hover:text-gray-700 focus:border-blue-300
      inline-flex justify-center w-${width} h-full px-4 py-2 text-sm font-medium leading-5 dark:text-white transition duration-150 ease-in-out border rounded-md focus:outline-none focus:shadow-outline-blue `}
                type="button" onClick={(e) => { postMessage("EditKeybind", id); e.currentTarget.style.borderColor = "#efaf2b" }} onFocus={(e) => { postMessage("EditKeybind", id); e.currentTarget.style.borderColor = "rgba(156, 163, 175, var(--tw-border-opacity))" }}>
          {keybind?.join("+")}
          {icon && icon}
      </button>
    </div>
	)
}

export default HotkeySelector;