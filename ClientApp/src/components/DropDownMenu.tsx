import React, { useState, useRef, useEffect } from 'react';

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
    if (!isOpen) return;

    const updateStyle = () => {
      if (!buttonRef.current || !dropdownRef.current) return;

      const buttonRect = buttonRef.current.getBoundingClientRect();
      const spaceBelow = window.innerHeight - buttonRect.bottom;
      if (spaceBelow <= 0) {
        setIsOpen(false);
      }
      const isSpaceBelow = spaceBelow >= 200 || spaceBelow >= (items?.length ?? 0) * 32;

      setStyle({
        top: isSpaceBelow ? `${buttonRect.bottom}px` : undefined,
        bottom: isSpaceBelow ? undefined : `${window.innerHeight - buttonRect.top}px`,
        left: `${buttonRect.left}px`,
        minWidth: `${buttonRect.width}px`,
        maxHeight: `${isSpaceBelow ? spaceBelow - 33 : buttonRect.top - 33}px`,
        overflowY: 'auto',
      });
    };

    updateStyle();

    const handleEvent = () => updateStyle();
    window.addEventListener('resize', handleEvent);
    window.addEventListener('scroll', handleEvent, true);

    const handleFocusOrClickOutside = (e: MouseEvent | FocusEvent) => {
      const target = e.target as Node;
      if (!dropdownRef.current?.contains(target) && !buttonRef.current?.contains(target)) {
        setIsOpen(false);
      }
    };

    document.addEventListener('mousedown', handleFocusOrClickOutside);
    document.addEventListener('focusin', handleFocusOrClickOutside);

    return () => {
      window.removeEventListener('resize', handleEvent);
      window.removeEventListener('scroll', handleEvent, true);
      document.removeEventListener('mousedown', handleFocusOrClickOutside);
      document.removeEventListener('focusin', handleFocusOrClickOutside);
    };
  }, [isOpen]);

  // Toggle dropdown open/close and handle outside click to close
  const toggleDropdown = () => setIsOpen(!isOpen);

  return (
    <div className={`w-${width}`}>
      <button
        ref={buttonRef}
        type='button'
        onClick={toggleDropdown}
        className={`bg-white text-gray-700 border-gray-500 hover:text-white hover:bg-gray-900 hover:border-gray-900 dark:bg-gray-900 dark:text-gray-400 dark:border-gray-400 dark:hover:text-gray-300 dark:hover:border-gray-300 relative w-${width} whitespace-nowrap inline-flex justify-center px-4 py-2 text-sm font-medium leading-5 transition duration-150 ease-in-out border rounded-md`}
      >
        {text}
        <svg
          className={`w-5 h-5 ml-2 -mr-1 ${isOpen ? 'transform rotate-180' : 'transform rotate-0'}`}
          viewBox='0 0 20 20'
          fill='currentColor'
          style={{
            transition: 'transform 0.2s cubic-bezier(0.22, 1, 0.36, 1)',
          }}
        >
          <path
            fillRule='evenodd'
            d='M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z'
            clipRule='evenodd'
          ></path>
        </svg>
      </button>
      {isOpen && (
        <div
          ref={dropdownRef}
          style={{ ...style, zIndex: zIndex }}
          className={
            'absolute bg-white dark:bg-gray-900 mt-1 border border-gray-300 dark:border-gray-700 rounded-md shadow-lg'
          }
        >
          {groups[0] !== null ? (
            groups.map((group, i) => (
              <div key={i} className='py-1' style={{ zIndex: zIndex }}>
                {group && (
                  <div className='px-2 pe-2 text-gray-700 dark:text-gray-200 dark:hover:bg-gray-900'>
                    {group}
                  </div>
                )}
                {items
                  ?.filter((item) => item.group === group)
                  .map((item, index) => (
                    <a
                      key={index}
                      className='block px-4 py-1 text-sm text-gray-700 dark:text-gray-400 hover:text-white focus:text-white dark:hover:text-gray-300 dark:focus:text-gray-300 hover:bg-gray-100 focus:bg-gray-100 dark:hover:bg-gray-800 dark:focus:bg-gray-800 cursor-pointer'
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
            <div style={{ zIndex: zIndex }}>
              {items?.map((item, index) => (
                <a
                  key={index}
                  className='block px-4 py-1 text-sm text-gray-700 dark:text-gray-400 hover:text-white focus:text-white dark:hover:text-gray-300 dark:focus:text-gray-300 hover:bg-gray-100 focus:bg-gray-100 dark:hover:bg-gray-800 dark:focus:bg-gray-800 cursor-pointer'
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
        </div>
      )}
    </div>
  );
};

export default DropDownMenu;
