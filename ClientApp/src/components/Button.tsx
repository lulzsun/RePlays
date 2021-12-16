import { ReactElement } from 'react';

interface Props {
  onClick?: (e: React.MouseEvent<HTMLButtonElement>) => void;
  text?: string;
  width?: string;
  icon?: ReactElement;
}

export const Button: React.FC<Props> = ({onClick, text, icon, width="full"}) => {
	return (
    <div className="relative inline-block text-left dropdown">
      <button className={`bg-white dark:bg-gray-900 text-gray-700 dark:text-gray-400 border-gray-500 dark:border-gray-400 hover:text-gray-700
      inline-flex justify-center w-${width} h-full px-4 py-2 text-sm font-medium leading-5 transition duration-150 ease-in-out border rounded-md focus:outline-none focus:border-blue-300 focus:shadow-outline-blue active:bg-gray-50 active:text-gray-800`}
        type="button" onClick={onClick}>
          {text}
          {icon && icon}
      </button>
    </div>
	)
}

export default Button;