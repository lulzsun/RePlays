import React from 'react';

interface Props {
  position?: ContextMenuPosition;
  items?: ContextMenuItem[];
}

export const ContextMenu: React.FC<Props> = ({ items, position = { x: 0, y: 0 } }) => {
  return (
    <div
      style={{ left: position.x, top: position.y }}
      className='bg-white dark:bg-gray-900 border-gray-500
    absolute transform -translate-y-full left-0 origin-bottom-left w-auto border divide-y divide-gray-100 rounded-md shadow-lg outline-none'
      aria-labelledby='headlessui-menu-button-1'
      id='headlessui-menu-items-117'
      role='menu'
    >
      {items &&
        items.map((item) => {
          // @ts-ignore
          return (
            <div
              key={item.name}
              onClick={(e) => {
                if (item.onClick) item.onClick();
                if (document.activeElement) document.activeElement.blur();
              }}
              tabIndex={0}
              className='bg-white dark:bg-gray-900 text-gray-700 dark:text-gray-400
        cursor-pointer flex justify-between w-full px-4 py-2 text-sm leading-5 text-left whitespace-nowrap'
            >
              {item.name}
            </div>
          );
        })}
    </div>
  );
};

export default ContextMenu;
