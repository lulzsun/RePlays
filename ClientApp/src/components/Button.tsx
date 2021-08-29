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
      <button className={`inline-flex justify-center w-${width} h-full px-4 py-2 text-sm font-medium leading-5 text-gray-700 transition duration-150 ease-in-out bg-white border border-gray-300 rounded-md hover:text-gray-500 focus:outline-none focus:border-blue-300 focus:shadow-outline-blue active:bg-gray-50 active:text-gray-800`}
        type="button" onClick={onClick}>
          {text}
          {icon && icon}
      </button>
    </div>
	)
}

export default Button;