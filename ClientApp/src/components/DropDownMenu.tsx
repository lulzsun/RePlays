import React from 'react';

interface Props {
  text?: string;
  width?: string; zIndex?: number;
  items?: { name: string, onClick?: () => any }[];
}

// TODO: Make use of headlessui instead of hacky css props
export const DropDownMenu: React.FC<Props> = ({text, items, width="full", zIndex=50}) => {
	return (
    <div className="relative inline-block text-left dropdown" style={{zIndex: zIndex}}>
      <button className={`inline-flex justify-center w-${width} px-4 py-2 text-sm font-medium leading-5 text-gray-700 transition duration-150 ease-in-out bg-white border border-gray-300 rounded-md hover:text-gray-500 focus:outline-none focus:border-blue-300 focus:shadow-outline-blue active:bg-gray-50 active:text-gray-800`}
      type="button" aria-haspopup="true" aria-expanded="true" aria-controls="headlessui-menu-items-117">
        {text}
        <svg className="w-5 h-5 ml-2 -mr-1" viewBox="0 0 20 20" fill="currentColor"><path fillRule="evenodd" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" clipRule="evenodd"></path></svg>
      </button>
      <div className="opacity-0 invisible dropdown-menu transition-all duration-300 transform origin-top-right -translate-y-2 scale-95">
        <div className="absolute left-0 w-auto mt-2 origin-top-left bg-white border border-gray-200 divide-y divide-gray-100 rounded-md shadow-lg outline-none" aria-labelledby="headlessui-menu-button-1" id="headlessui-menu-items-117" role="menu">
          {items && items.map((item) => {
            // @ts-ignore
            return <div key={item.name} onClick={(e) => {if(item.onClick) item.onClick(); if(document.activeElement) document.activeElement.blur()}} tabIndex={0} className="cursor-pointer text-gray-700 flex justify-between w-full px-4 py-2 text-sm leading-5 text-left whitespace-nowrap">{item.name}</div>
          })}
        </div>
      </div>
    </div>
	)
}

export default DropDownMenu;