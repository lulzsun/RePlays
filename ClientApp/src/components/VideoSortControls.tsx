import { useTranslation } from 'react-i18next';

import React from 'react';
import { formatBytes } from '../helpers/utils';
import Button from './Button';
import DropDownMenu from './DropDownMenu';
import { postMessage } from '../helpers/messenger';

interface Props {
  gameList: string[];
  game: string;
  sortBy: string;
  size: number;
  setVideoView: React.Dispatch<React.SetStateAction<string>>;
}

export const VideoSortControls: React.FC<Props> = ({
  gameList,
  game,
  sortBy,
  size,
  setVideoView,
}) => {
  const { t } = useTranslation();

  function getGamesDDM() {
    let items = [
      {
        name: t('componentVideoSortControlsItem01'),
        onClick: () => {
          postMessage('RetrieveVideos', { game: 'All Games', sortBy: sortBy });
        },
      },
    ];
    gameList.forEach((game) => {
      items.push({
        name: game,
        onClick: () => {
          postMessage('RetrieveVideos', { game: game, sortBy: sortBy });
        },
      });
    });
    return items;
  }

  return (
    <div className='pt-2 grid grid-flow-col gap-4'>
      <DropDownMenu
        text={game}
        items={getGamesDDM()}
        zIndex={100}
      />
      <DropDownMenu
        text={sortBy}
        zIndex={100}
        items={[
          {
            name: t('componentVideoSortControlsItem02'),
            onClick: () => {
              postMessage('RetrieveVideos', { game: game, sortBy: 'Latest' });
            },
          },
          {
            name: t('componentVideoSortControlsItem03'),
            onClick: () => {
              postMessage('RetrieveVideos', { game: game, sortBy: 'Oldest' });
            },
          },
          {
            name: t('componentVideoSortControlsItem04'),
            onClick: () => {
              postMessage('RetrieveVideos', { game: game, sortBy: 'Smallest' });
            },
          },
          {
            name: t('componentVideoSortControlsItem05'),
            onClick: () => {
              postMessage('RetrieveVideos', { game: game, sortBy: 'Largest' });
            },
          },
        ]}
      />
      <Button
        icon={
          <svg
            className='mt-0.5'
            xmlns='http://www.w3.org/2000/svg'
            width='16'
            height='16'
            fill='currentColor'
            viewBox='0 0 16 16'
          >
            <path d='M1 2.5A1.5 1.5 0 0 1 2.5 1h3A1.5 1.5 0 0 1 7 2.5v3A1.5 1.5 0 0 1 5.5 7h-3A1.5 1.5 0 0 1 1 5.5v-3zM2.5 2a.5.5 0 0 0-.5.5v3a.5.5 0 0 0 .5.5h3a.5.5 0 0 0 .5-.5v-3a.5.5 0 0 0-.5-.5h-3zm6.5.5A1.5 1.5 0 0 1 10.5 1h3A1.5 1.5 0 0 1 15 2.5v3A1.5 1.5 0 0 1 13.5 7h-3A1.5 1.5 0 0 1 9 5.5v-3zm1.5-.5a.5.5 0 0 0-.5.5v3a.5.5 0 0 0 .5.5h3a.5.5 0 0 0 .5-.5v-3a.5.5 0 0 0-.5-.5h-3zM1 10.5A1.5 1.5 0 0 1 2.5 9h3A1.5 1.5 0 0 1 7 10.5v3A1.5 1.5 0 0 1 5.5 15h-3A1.5 1.5 0 0 1 1 13.5v-3zm1.5-.5a.5.5 0 0 0-.5.5v3a.5.5 0 0 0 .5.5h3a.5.5 0 0 0 .5-.5v-3a.5.5 0 0 0-.5-.5h-3zm6.5.5A1.5 1.5 0 0 1 10.5 9h3a1.5 1.5 0 0 1 1.5 1.5v3a1.5 1.5 0 0 1-1.5 1.5h-3A1.5 1.5 0 0 1 9 13.5v-3zm1.5-.5a.5.5 0 0 0-.5.5v3a.5.5 0 0 0 .5.5h3a.5.5 0 0 0 .5-.5v-3a.5.5 0 0 0-.5-.5h-3z' />
          </svg>
        }
        onClick={() => {
          setVideoView('grid');
        }}
      />
      <Button
        icon={
          <svg
            className='mt-0.5'
            xmlns='http://www.w3.org/2000/svg'
            width='16'
            height='16'
            fill='currentColor'
            viewBox='0 0 16 16'
          >
            <path d='M3 4.5h10a2 2 0 0 1 2 2v3a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2v-3a2 2 0 0 1 2-2zm0 1a1 1 0 0 0-1 1v3a1 1 0 0 0 1 1h10a1 1 0 0 0 1-1v-3a1 1 0 0 0-1-1H3zM1 2a.5.5 0 0 1 .5-.5h13a.5.5 0 0 1 0 1h-13A.5.5 0 0 1 1 2zm0 12a.5.5 0 0 1 .5-.5h13a.5.5 0 0 1 0 1h-13A.5.5 0 0 1 1 14z' />
          </svg>
        }
        onClick={() => {
          setVideoView('row');
        }}
      />
      <span className='text-gray-700 dark:text-gray-400 hover:text-gray-700 active:bg-gray-50 active:text-gray-800 mt-0.5 inline-flex justify-center w-full px-4 py-2 text-sm font-medium leading-5 transition duration-150 ease-in-out rounded-md'>
        {formatBytes(size)}
      </span>
      <Button
        onClick={() => window.location.reload()}
        icon={
          <svg
            className='mt-0.5'
            xmlns='http://www.w3.org/2000/svg'
            width='16'
            height='16'
            fill='currentColor'
            viewBox='0 0 16 16'
          >
            <path d='M11.534 7h3.932a.25.25 0 0 1 .192.41l-1.966 2.36a.25.25 0 0 1-.384 0l-1.966-2.36a.25.25 0 0 1 .192-.41zm-11 2h3.932a.25.25 0 0 0 .192-.41L2.692 6.23a.25.25 0 0 0-.384 0L.342 8.59A.25.25 0 0 0 .534 9z' />
            <path
              fillRule='evenodd'
              d='M8 3c-1.552 0-2.94.707-3.857 1.818a.5.5 0 1 1-.771-.636A6.002 6.002 0 0 1 13.917 7H12.9A5.002 5.002 0 0 0 8 3zM3.1 9a5.002 5.002 0 0 0 8.757 2.182.5.5 0 1 1 .771.636A6.002 6.002 0 0 1 2.083 9H3.1z'
            />
          </svg>
        }
      />
    </div>
  );
};

export default VideoSortControls;
