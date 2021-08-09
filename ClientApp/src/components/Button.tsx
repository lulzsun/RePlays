import React from 'react';
import { ReactElement } from 'react';

interface Props {
  text?: string;
  icon?: ReactElement;
}

export const Button: React.FC<Props> = ({text, icon}) => {
	return (
    <button className="mt-0.5 inline-flex justify-center w-full px-4 py-2 text-sm font-medium leading-5 text-gray-700 transition duration-150 ease-in-out bg-white border border-gray-300 rounded-md hover:text-gray-500 focus:outline-none focus:border-blue-300 focus:shadow-outline-blue active:bg-gray-50 active:text-gray-800" 
      type="button">
        {text}
        {icon && icon}
    </button>
	)
}

export default Button;