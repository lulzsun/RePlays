import { useTranslation } from 'react-i18next';

import { useEffect, useRef } from 'react';
import { postMessage } from '../helpers/messenger';
import { secondsToHHMMSS } from '../helpers/utils';

interface Props {
  toastData: ModalData;
  onClick?: () => any;
}

export const Toast: React.FC<Props> = ({ toastData, onClick }) => {
  const { t } = useTranslation();

  var circle = useRef<SVGCircleElement>(null);

  useEffect(() => {
    if (circle === null || circle.current === null) return;
    var radius = circle.current.r.baseVal.value;
    var circumference = radius * 2 * Math.PI;

    circle.current.style.strokeDasharray = `${circumference} ${circumference}`;
    circle.current.style.strokeDashoffset = `${circumference}`;
    const offset = circumference - (toastData.progress! / toastData!.progressMax!) * circumference;
    circle.current.style.strokeDashoffset = offset + '';
  }, [toastData]);

  return (
    <div className='flex w-full overflow-hidden bg-white rounded-lg shadow-md dark:bg-gray-800'>
      {toastData.progressMax !== 0 && (
        <div className='p-1.5 flex items-center justify-center bg-blue-500'>
          <svg className='progress-ring' width='30' height='30'>
            <circle
              ref={circle}
              className='progress-ring__circle'
              stroke='white'
              strokeWidth='2'
              fill='transparent'
              r='12'
              cx='15'
              cy='15'
            />
          </svg>
        </div>
      )}
      <div className='p-1.5'>
        <div className='flex'>
          <span className='flex text-sm font-semibold text-blue-500 dark:text-blue-400 grow'>
            {toastData.id === 'Recording' && (
              <svg
                xmlns='http://www.w3.org/2000/svg'
                width='16'
                height='16'
                fill='currentColor'
                className='text-red-500 m-0.5 mr-1'
                viewBox='0 0 16 16'
              >
                <circle cx='8' cy='8' r='8' />
              </svg>
            )}
            {toastData.id === 'Recording' ? 'Recording' : toastData.title}
          </span>
          {toastData.id === 'Recording' && (
            <div className='mr-1 dropdown' style={{ zIndex: 50 }}>
              <button className='p-1 hover:bg-gray-700 hover:text-white rounded-full'>
                <svg
                  xmlns='http://www.w3.org/2000/svg'
                  width='16'
                  height='16'
                  fill='currentColor'
                  viewBox='0 0 16 16'
                >
                  <path d='m7.247 4.86-4.796 5.481c-.566.647-.106 1.659.753 1.659h9.592a1 1 0 0 0 .753-1.659l-4.796-5.48a1 1 0 0 0-1.506 0z' />
                </svg>
              </button>
              <div className='absolute opacity-0 invisible dropdown-menu transition-all duration-300 transform scale-95'>
                <div
                  className='absolute bottom-6 -right-6 w-auto bg-white border border-gray-500 divide-y divide-gray-100 rounded-md shadow-lg outline-none'
                  role='menu'
                >
                  {/* <div
                    className='cursor-pointer text-gray-700 flex justify-between w-full px-4 py-2 text-sm leading-5 text-right whitespace-nowrap'
                    onClick={() => {}}
                  >
                    {t('playerItem02')}
                  </div> */}
                  <div
                    className='cursor-pointer text-gray-700 flex justify-between w-full px-4 py-2 text-sm leading-5 text-right whitespace-nowrap'
                    onClick={() => postMessage('StopRecording')}
                  >
                    Stop
                  </div>
                </div>
              </div>
            </div>
          )}
        </div>
        <p
          className={`h-auto ${
            toastData.progressMax !== 0 ? 'w-28' : 'w-40'
          } text-xs text-gray-600 dark:text-gray-200 truncate`}
        >
          {toastData.id === 'Recording' && secondsToHHMMSS(toastData.progress!) + ' '}
          {toastData.context}
        </p>
      </div>
    </div>
  );
};

export default Toast;
