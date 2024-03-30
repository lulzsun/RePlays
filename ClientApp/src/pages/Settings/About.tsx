import { useTranslation } from 'react-i18next';

import Button from '../../components/Button';
import { postMessage } from '../../helpers/messenger';

interface Props {}

export const About: React.FC<Props> = ({}) => {
  const { t } = useTranslation();

  return (
    <div className='flex flex-col gap-2 font-medium text-base pb-7'>
      <h1 className='font-semibold text-2xl'>{t('settingsAboutItem01')}</h1>
      <div
        className='cursor-pointer flex flex-row gap-2'
        onClick={(e) => {
          postMessage('ShowFolder', 'https://github.com/lulzsun/RePlays');
        }}
      >
        <img
          className='rounded-full'
          alt='GitHub-Mark-120px-plus.png'
          src='https://avatars.githubusercontent.com/u/28168454?v=4'
          width='32'
          height='32'
        />
        <span className='font-normal text-sm'>{t('settingsAboutItem02')}</span>
      </div>

      <span className='text-gray-700 dark:text-gray-400 mt-2'>{t('settingsAboutItem03')}</span>
      <span className='font-normal text-sm'>
        {t('settingsAboutItem04')}
        <a
          className='cursor-pointer underline pl-2'
          onClick={(e) => {
            postMessage('ShowFolder', 'https://github.com/lulzsun/RePlays');
          }}
        >
          https://github.com/lulzsun/RePlays
        </a>
      </span>

      <span className='text-gray-700 dark:text-gray-400 mt-2'>{t('settingsAboutItem05')}</span>
      <span className='font-normal text-sm'>
        lulzsun:
        <a
          className='cursor-pointer underline pl-2'
          onClick={(e) => {
            postMessage('ShowFolder', 'https://github.com/lulzsun');
          }}
        >
          https://github.com/lulzsun
        </a>
      </span>
      <span className='font-normal text-sm'>
        {t('settingsAboutItem06')}
        <a
          className='cursor-pointer underline pl-2'
          onClick={(e) => {
            postMessage('ShowFolder', 'https://github.com/lulzsun/RePlays/graphs/contributors');
          }}
        >
          https://github.com/lulzsun/RePlays/graphs/contributors
        </a>
      </span>

      <span className='text-gray-700 dark:text-gray-400 mt-2'>{t('settingsAboutItem07')}</span>
      <span className='font-normal text-sm'>
        7-zip:
        <a
          className='cursor-pointer underline pl-2'
          onClick={(e) => {
            postMessage('ShowFolder', 'https://www.7-zip.org');
          }}
        >
          https://www.7-zip.org
        </a>
      </span>
      <span className='font-normal text-sm'>
        FFmpeg:
        <a
          className='cursor-pointer underline pl-2'
          onClick={(e) => {
            postMessage('ShowFolder', 'https://ffmpeg.org');
          }}
        >
          https://ffmpeg.org
        </a>
      </span>
      <span className='font-normal text-sm'>
        Squirrel:
        <a
          className='cursor-pointer underline pl-2'
          onClick={(e) => {
            postMessage('ShowFolder', 'https://github.com/Squirrel/Squirrel.Windows');
          }}
        >
          https://github.com/Squirrel/Squirrel.Windows
        </a>
      </span>
      <span className='font-normal text-sm'>
        Tailwind:
        <a
          className='cursor-pointer underline pl-2'
          onClick={(e) => {
            postMessage('ShowFolder', 'https://tailwindcss.com');
          }}
        >
          https://tailwindcss.com
        </a>
      </span>
      <span className='font-normal text-sm'>
        NHotkey:
        <a
          className='cursor-pointer underline pl-2'
          onClick={(e) => {
            postMessage('ShowFolder', 'https://github.com/thomaslevesque/NHotkey');
          }}
        >
          https://github.com/thomaslevesque/NHotkey
        </a>
      </span>

      <span className='text-gray-700 dark:text-gray-400 mt-2'>{t('settingsAboutItem08')}</span>
      <Button
        text={t('settingsAboutItem09')}
        width={'auto'}
        onClick={(e) => {
          postMessage('ShowLicense');
        }}
      />
    </div>
  );
};

export default About;
