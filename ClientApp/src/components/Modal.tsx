import { useTranslation } from 'react-i18next';

import { Fragment, useEffect, useState } from 'react';
import { Dialog, Transition } from '@headlessui/react';

interface Props {
  modalData: ModalData;
  open: boolean;
  setOpen: React.Dispatch<React.SetStateAction<boolean>>;
  onConfirm?: () => any;
}

const iconColors = {
  none: 'blue',
  info: 'blue',
  warning: 'red',
  question: 'blue',
  success: 'green',
  update: 'blue'
};

export const Modal: React.FC<Props> = ({ modalData, open, setOpen, onConfirm }) => {
  const { t } = useTranslation();

  const title = modalData.title;
  const context = modalData.context;
  const icon = modalData.icon;
  const progress = modalData.progress;
  const progressMax = modalData.progressMax;
  const cancel = modalData.cancel;

  return (
    <Transition.Root show={open} as={Fragment}>
      <Dialog
        static
        as='div'
        style={{ zIndex: 99999 }}
        className='fixed inset-0 overflow-y-auto'
        onClose={() => null}
      >
        <div className='flex items-end justify-center min-h-screen pt-4 px-4 pb-20 text-center sm:block sm:p-0'>
          <Transition.Child
            as={Fragment}
            enter='ease-out duration-300'
            enterFrom='opacity-0'
            enterTo='opacity-100'
            leave='ease-in duration-200'
            leaveFrom='opacity-100'
            leaveTo='opacity-0'
          >
            <div className='fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity' />
          </Transition.Child>

          {/* This element is to trick the browser into centering the modal contents. */}
          <span className='hidden sm:inline-block sm:align-middle sm:h-screen' aria-hidden='true'>
            &#8203;
          </span>
          <Transition.Child
            as={Fragment}
            enter='ease-out duration-300'
            enterFrom='opacity-0 translate-y-4 sm:translate-y-0 sm:scale-95'
            enterTo='opacity-100 translate-y-0 sm:scale-100'
            leave='ease-in duration-200'
            leaveFrom='opacity-100 translate-y-0 sm:scale-100'
            leaveTo='opacity-0 translate-y-4 sm:translate-y-0 sm:scale-95'
          >
            <div className='inline-block align-bottom bg-white rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-lg sm:w-full'>
              <div className='bg-white px-4 pt-5 pb-4 sm:p-6 sm:pb-4'>
                <div className='sm:flex sm:items-start'>
                  {icon !== 'none' && icon !== undefined && (
                    <div
                      className={`mx-auto flex-shrink-0 flex items-center justify-center h-12 w-12 rounded-full sm:mx-0 sm:w-10 bg-${
                        iconColors[icon!]
                      }-100`}
                    >
                      <div className={`h-6 w-6 text-${iconColors[icon!]}-600`} aria-hidden='true'>
                        {icon === 'info' && (
                          <svg
                            xmlns='http://www.w3.org/2000/svg'
                            className='h-6 w-6'
                            fill='none'
                            viewBox='0 0 24 24'
                            stroke='currentColor'
                          >
                            <path
                              strokeLinecap='round'
                              strokeLinejoin='round'
                              strokeWidth='2'
                              d='M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z'
                            />
                          </svg>
                        )}
                        {icon === 'warning' && (
                          <svg
                            xmlns='http://www.w3.org/2000/svg'
                            className='h-6 w-6'
                            fill='none'
                            viewBox='0 0 24 24'
                            stroke='currentColor'
                          >
                            <path
                              strokeLinecap='round'
                              strokeLinejoin='round'
                              strokeWidth='2'
                              d='M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z'
                            />
                          </svg>
                        )}
                        {icon === 'question' && (
                          <svg
                            xmlns='http://www.w3.org/2000/svg'
                            className='h-6 w-6'
                            fill='none'
                            viewBox='0 0 24 24'
                            stroke='currentColor'
                          >
                            <path
                              strokeLinecap='round'
                              strokeLinejoin='round'
                              strokeWidth='2'
                              d='M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z'
                            />
                          </svg>
                        )}
                        {icon === 'success' && (
                          <svg
                            xmlns='http://www.w3.org/2000/svg'
                            className='h-6 w-6'
                            fill='none'
                            viewBox='0 0 24 24'
                            stroke='currentColor'
                          >
                            <path
                              strokeLinecap='round'
                              strokeLinejoin='round'
                              strokeWidth='2'
                              d='M5 13l4 4L19 7'
                            />
                          </svg>
                        )}
                        {icon === 'update' && (
                          <svg
                            xmlns='http://www.w3.org/2000/svg'
                            className='h-6 w-6'
                            fill='#17b2b0'
                            viewBox='0 0 24 24'
                          >
                            <path
                              d="M 20.382812 15.296875 L 20.382812 20.777344 L 3.613281 20.777344 L 3.613281 15.296875 L 0.390625 15.296875 L 0.390625 22.390625 C 0.390625 23.277344 1.113281 24 2.003906 24 L 21.992188 24 C 22.886719 24 23.609375 23.28125 23.609375 22.390625 L 23.609375 15.296875 Z M 11.703125 14.75 L 7.089844 9.175781 C 7.089844 9.175781 6.386719 8.511719 7.148438 8.511719 L 9.75 8.511719 L 9.75 0.394531 C 9.75 0.394531 9.644531 0 10.242188 0 L 13.902344 0 C 14.332031 0 14.320312 0.332031 14.320312 0.332031 L 14.320312 8.34375 L 16.722656 8.34375 C 17.644531 8.34375 16.949219 9.039062 16.949219 9.039062 L 12.476562 14.796875 C 12.082031 15.195312 11.703125 14.75 11.703125 14.75 Z M 11.703125 14.75 "
                            />
                          </svg>
                        )}
                      </div>
                    </div>
                  )}
                  <div className='mt-3 text-center sm:mt-0 sm:ml-4 sm:mr-4 sm:text-left w-full'>
                    <Dialog.Title as='h3' className='text-lg leading-6 font-medium text-gray-900'>
                      {title}
                    </Dialog.Title>
                    <div className='mt-2'>
                      <div className='text-sm text-gray-700'>
                        {context}
                        {progressMax !== 0 && progressMax !== undefined && (
                          <div className='w-full bg-gray-200 rounded'>
                            <div
                              style={{
                                width: `${(progress! / progressMax!) * 100}%`,
                              }}
                              className='absolute top-0 h-4 rounded shim-blue'
                            ></div>
                          </div>
                        )}
                      </div>
                    </div>
                  </div>
                </div>
              </div>
              <div className='bg-gray-50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse'>
                {title !== 'Downloading' && (
                  <button
                    type='button'
                    className='w-full inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 bg-blue-600 text-base font-medium text-white hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 sm:ml-3 sm:w-auto sm:text-sm'
                    onClick={() => {
                      setOpen(false);
                      if (onConfirm !== undefined) onConfirm();
                    }}
                  >
                    {t('componentModalItem01')}
                  </button>
                )}
                {(icon === 'question' || icon === 'update' || cancel) && (
                  <button
                    type='button'
                    className='mt-3 w-full inline-flex justify-center rounded-md border border-gray-300 shadow-sm px-4 py-2 bg-white text-base font-medium text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 sm:mt-0 sm:ml-3 sm:w-auto sm:text-sm'
                    onClick={() => setOpen(false)}
                  >
                    {t('componentModalItem02')}
                  </button>
                )}
              </div>
            </div>
          </Transition.Child>
        </div>
      </Dialog>
    </Transition.Root>
  );
};

export default Modal;
