import React from 'react';

interface Props {
  text?: string;
  width?: string; zIndex?: number;
  groups?: string[];
  items?: { id?: string, name: string, onClick?: () => any, group?: string }[];
}

// TODO: Make use of headlessui instead of hacky css props
export const DropDownMenu: React.FC<Props> = ({text, groups=[null], items, width="full", zIndex=50}) => {
	return (
    <div className={`w-${width}`} style={{zIndex: zIndex}}>
      <button className={`bg-white text-gray-700 border-gray-500 hover:text-white hover:bg-gray-900 hover:border-gray-900
      dark:bg-gray-900 dark:text-gray-400 dark:border-gray-400 dark:hover:text-gray-300 dark:hover:border-gray-300
      relative w-${width} whitespace-nowrap inline-flex justify-center px-4 py-2 text-sm font-medium leading-5 transition duration-150 ease-in-out border rounded-md`}
      type="button">
        {text}
        <svg className="w-5 h-5 ml-2 -mr-1" viewBox="0 0 20 20" fill="currentColor"><path fillRule="evenodd" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" clipRule="evenodd"></path></svg>
        <select className={`absolute w-full left-0 top-0 opacity-0 cursor-pointer`}
          onChange={(e) => {
            items?.find(item => {
              if(item.id != undefined) return item.id === e.target.value;
              else return item.name === e.target.value;
            })?.onClick!();
          }}>
          {groups[0] !== null ? groups.map((group, i) => {
            return <optgroup key={i} label={group!}><option hidden/>
              {items && items.map((item, i) => {
                if(group != item.group) return;
                return <option key={i} tabIndex={i} value={item.id == undefined ? item.name : item.id}
                className={`bg-white dark:bg-gray-900 text-gray-700 dark:text-gray-400`}>{item.name}</option>
              })}
            </optgroup>
          })
          :
          <><option hidden/>
            {items && items.map((item, i) => {
              return <option key={i} tabIndex={i} value={item.id == undefined ? item.name : item.id}
              className={`bg-white dark:bg-gray-900 text-gray-700 dark:text-gray-400`}>{item.name}</option>
            })}
          </>
          }
        </select>
      </button>
    </div>
	)
}

export default DropDownMenu;