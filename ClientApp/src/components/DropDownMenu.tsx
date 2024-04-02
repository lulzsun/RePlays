import React, { useState, useRef, useEffect } from 'react';
import { motion } from 'framer-motion';

interface Props {
  text?: string;
  width?: string;
  zIndex?: number;
  groups?: string[];
  items?: { id?: string; name: string; onClick?: () => any; group?: string }[];
}

export const DropDownMenu: React.FC<Props> = ({
  text,
  groups = [null],
  items,
  width = 'full',
  zIndex = 50,
}) => {
  const [isOpen, setIsOpen] = useState(false);
  const buttonRef = useRef<HTMLButtonElement>(null);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const [style, setStyle] = useState({});

  // Update dropdown position and size on open, scroll or window changes
  useEffect(() => {
    const updateStyle = () => {
      if (!isOpen || !buttonRef.current || !dropdownRef.current) return;

      const buttonRect = buttonRef.current.getBoundingClientRect();
      const dropdownHeight = dropdownRef.current.offsetHeight;
      const spaceBelow = window.innerHeight - buttonRect.bottom;
      const spaceAbove = buttonRect.top;

      // Check for enough space below the button for the dropdown, preferring below with a 100px buffer.
      const isSpaceBelow = spaceBelow + 100 >= dropdownHeight;

      setStyle({
        top: isSpaceBelow ? `${buttonRect.bottom}px` : undefined,
        bottom: isSpaceBelow ? undefined : `${window.innerHeight - buttonRect.top}px`,
        left: `${buttonRect.left}px`,
        minWidth: `${buttonRect.width}px`,
        maxHeight: isSpaceBelow ? `${spaceBelow - 33}px` : `${spaceAbove - 33}px`,
        overflowY: 'auto',
      });
    };

    if (isOpen) {
      updateStyle();
      const handleEvent = () => updateStyle();
      window.addEventListener('resize', handleEvent);
      window.addEventListener('scroll', handleEvent, true);
      return () => {
        window.removeEventListener('resize', handleEvent);
        window.removeEventListener('scroll', handleEvent, true);
      };
    }
  }, [isOpen]);

  // Toggle dropdown open/close and handle outside click to close
  const toggleDropdown = () => setIsOpen(!isOpen);
  const closeDropdownOnBlur = () => {
    setTimeout(() => {
      if (!dropdownRef.current?.contains(document.activeElement)) {
        setIsOpen(false);
      }
    }, 150);
  };

  return (
    <div className={`w-${width}`}>
      <button
        ref={buttonRef}
        type='button'
        onClick={toggleDropdown}
        onBlur={closeDropdownOnBlur}
        className={`bg-white text-gray-700 border-gray-500 hover:text-white hover:bg-gray-900 hover:border-gray-900 dark:bg-gray-900 dark:text-gray-400 dark:border-gray-400 dark:hover:text-gray-300 dark:hover:border-gray-300 relative w-${width} whitespace-nowrap inline-flex justify-center px-4 py-2 text-sm font-medium leading-5 transition duration-150 ease-in-out border rounded-md`}
      >
        {text}
        <motion.svg
          className='w-5 h-5 ml-2 -mr-1'
          viewBox='0 0 20 20'
          fill='currentColor'
          animate={{rotate: isOpen ? 180 : 0}}
          transition={{
            type: 'spring',
            duration: 0.2,
            stiffness: 350,
            damping: 20
          }}
        >
          <path
            fillRule='evenodd'
            d='M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z'
            clipRule='evenodd'
          ></path>
        </motion.svg>
      </button>
      {isOpen && (
        <motion.div
          initial={{opacity: 0, y: 8}}
          animate={{opacity: 1, y: 0}}
          exit={{opacity: 0, y: 10}}
          transition={{duration: 0.15}}
          ref={dropdownRef}
          style={{...style, zIndex: zIndex}}
          className={'absolute bg-white dark:bg-gray-900 mt-1 border border-gray-300 dark:border-gray-700 rounded-md shadow-lg'}>
          {groups[0] !== null ? (
            groups.map((group, i) => (
              <div key={i} className='py-1' style={{zIndex: zIndex}}>
                {group && <div className='px-2 py-2 text-gray-700 dark:text-gray-200 dark:hover:bg-gray-900'>{group}</div>}
                {items?.filter(item => item.group === group).map((item, index) => (
                  <a
                    
                    key={index}
                    className='block px-4 py-2 text-sm text-gray-700 dark:text-gray-400 hover:text-white dark:hover:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 cursor-pointer'
                    onClick={(e) => {
                      e.preventDefault();
                      item?.onClick?.();
                      setIsOpen(false);
                    }}
                  >
                    {item.name}
                  </a>
                ))}
              </div>
            ))
          ) : (
            <div
              style={{zIndex: zIndex}}>
              {items?.map((item, index) => (
                <a
                  key={index}
                  className='block px-4 py-2 text-sm text-gray-700 dark:text-gray-400 hover:text-white dark:hover:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 cursor-pointer'
                  onClick={(e) => {
                    e.preventDefault();
                    item?.onClick?.();
                    setIsOpen(false);
                  }}
                >
                  {item.name}
                </a>
              ))}
            </div>
          )}
        </motion.div>
      )}
    </div>
  );
};

export default DropDownMenu;