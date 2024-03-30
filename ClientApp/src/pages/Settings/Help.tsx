import { useTranslation } from 'react-i18next';

import Button from '../../components/Button';
import { postMessage } from '../../helpers/messenger';

interface Props {}

export const Help: React.FC<Props> = ({}) => {
  const { t } = useTranslation();

  return (
    <div className='flex flex-col gap-2 font-medium text-base pb-7'>
      <h1 className='font-semibold text-2xl'>{t('settingsHelpItem01')}</h1>
      <span className='font-normal text-sm'>{t('settingsHelpItem02')}</span>

      <span className='text-gray-700 dark:text-gray-400 mt-4'>{t('settingsHelpItem03')}</span>
      <span className='font-normal text-sm'>{t('settingsHelpItem04')}</span>
      <Button
        text={t('settingsHelpItem05')}
        width={'auto'}
        onClick={(e) => {
          postMessage('ShowFolder', 'https://github.com/lulzsun/RePlays/issues');
        }}
      />

      <span className='text-gray-700 dark:text-gray-400 mt-4'>{t('settingsHelpItem06')}</span>
      <span className='font-normal text-sm'>{t('settingsHelpItem07')}</span>
      <Button
        text={t('settingsHelpItem08')}
        width={'auto'}
        onClick={(e) => {
          postMessage('ShowFolder', 'https://discordapp.com/invite/Qj2BmZX');
        }}
      />

      <span className='text-gray-700 dark:text-gray-400 mt-4'>{t('settingsHelpItem09')}</span>
      <span className='font-normal text-sm'>{t('settingsHelpItem10')}</span>
      <Button
        text={t('settingsHelpItem11')}
        width={'auto'}
        onClick={(e) => {
          postMessage('ShowLogs');
        }}
      />
    </div>
  );
};

export default Help;
