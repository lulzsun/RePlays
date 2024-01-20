import { useTranslation } from 'react-i18next';

import { useContext, useEffect, useState } from 'react';
import { postMessage } from '../helpers/messenger';
import { ModalContext } from '../Contexts';

interface Props {
  video: string;
  game: string;
  thumb?: string;
  makePublic: boolean;
}

export const UploadModal: React.FC<Props> = ({ video, game, thumb }) => {
  const { t } = useTranslation();

  const modalCtx = useContext(ModalContext);

  const [destination, setDestination] = useState('RePlays');
  const [makePublic, setMakePublic] = useState(true);
  const [title, setTitle] = useState(video);

  useEffect(() => {
    modalCtx?.setConfirm(() => {
      postMessage('UploadVideo', {
        destination,
        title,
        file: `${game}\\${video}`,
        game,
        makePublic,
      });
    });
  }, [destination, title, game, video, makePublic]);

  return (
    <div className='flex flex-row gap-6'>
      <img className='h-28 border' alt={'video_thumbnail_placeholder.png'} src={thumb} />
      <div className='flex flex-col gap-2'>
        <div className='flex flex-col'>
          {t('componentUploadModalItem01')}
          <input
            className={`inline-flex align-middle justify-center w-full h-full px-4 py-2 text-sm font-medium leading-5 text-gray-700 transition duration-150 ease-in-out bg-white border border-gray-300 rounded-md hover:text-gray-700 focus:outline-none focus:border-blue-300 focus:shadow-outline-blue active:bg-gray-50 active:text-gray-800`}
            type='text'
            defaultValue={title}
            onChange={(e) => {
              setTitle(e.target.value);
            }}
          />
        </div>

        <div className='flex flex-col'>
          {t('componentUploadModalItem02')}
          <div className='group relative w-full'>
            <svg
              className='absolute w-5 h-5 -ml-8 mt-2 top-px left-full text-gray-700 group-hover:text-gray-700 cursor-pointer pointer-events-none'
              viewBox='0 0 20 20'
              fill='currentColor'
            >
              <path
                fillRule='evenodd'
                d='M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z'
                clipRule='evenodd'
              />
            </svg>
            <select
              className={`inline-flex justify-center w-full px-4 py-2 text-sm font-medium leading-5 text-gray-700 group-hover:text-gray-700 transition duration-150 ease-in-out bg-white border border-gray-300 rounded-md focus:outline-none focus:border-blue-300 focus:shadow-outline-blue active:bg-gray-50 active:text-gray-800 cursor-pointer`}
              name='destinationDdm'
              id='destinationDdm'
              defaultValue={destination}
              onChange={(e) => setDestination(e.target.value)}
            >
              <option value='RePlays'>{t('componentUploadModalItem03')}</option>
              <option value='Streamable'>{t('componentUploadModalItem04')}</option>
              <option value='LocalFolder'>{t('componentUploadModalItem05')}</option>
              <option value='Custom'>{t('componentUploadModalItem06')}</option>
            </select>
            {destination === "RePlays" && (
              <label className='inline-flex items-center pt-2'>
                <input
                  type='checkbox'
                  className='form-checkbox h-4 w-4 text-gray-600'
                  defaultChecked={makePublic}
                  onChange={(e) => setMakePublic(e.target.checked)}
                />
                <span className='ml-2 text-gray-700 dark:text-gray-400'>{t("componentUploadModalItem07")}</span>
              </label>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

export default UploadModal;
