import { ReactElement } from 'react';

interface Props {
  onClick?: (e: React.MouseEvent<HTMLButtonElement>) => void;
  text?: string;
  width?: string;
  icon?: ReactElement;
}

export const Button: React.FC<Props> = ({ onClick, text, icon, width = 'full' }) => {
  return (
    <div className='relative inline-block text-left dropdown'>
      <button
        className={`bg-white text-gray-700 border-gray-500 hover:text-white hover:bg-gray-900 hover:border-gray-900
      dark:bg-gray-900 dark:text-gray-400 dark:border-gray-400 dark:hover:text-gray-300 dark:hover:border-gray-300
      inline-flex whitespace-nowrap justify-center w-${width} h-full px-4 py-2 text-sm font-medium leading-5 transition duration-150 ease-in-out border rounded-md`}
        type='button'
        onClick={onClick}
      >
        {text}
        {icon && icon}
      </button>
    </div>
  );
};

export default Button;
