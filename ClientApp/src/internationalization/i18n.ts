import i18n from 'i18next';
import XHR from 'i18next-http-backend';
import { initReactI18next } from 'react-i18next';
import * as locales from './locales';

const options = {
  order: ['querystring', 'navigator'],
  lookupQuerystring: 'lng',
};

type Translation = { [key: string]: string };

type Translations = {
  [key: string]: { translation: Translation };
};

export const languages = [
  {code: 'en', name: 'English'},
  {code: 'de', name: 'German'},
  {code: 'es', name: 'Spanish'},
  {code: 'fr', name: 'French'},
  {code: 'it', name: 'Italian'},
  {code: 'pt', name: 'Portuguese'},
  {code: 'ru', name: 'Russian'},
];

export const getLanguageName = (code: any) => {
  const language = languages.find((lang) => lang.code === code);
  return language ? language.name : 'English';
}

const typedTranslations: Translations = locales;

const resources: Translations = Object.keys(typedTranslations).reduce((acc, key) => {
  acc[key] = typedTranslations[key];
  return acc;
}, {} as Translations);

i18n
  .use(XHR)
  .use(initReactI18next)
  .init({
    debug: false,
    lng: 'en',
    fallbackLng: 'en',
    interpolation: {
      escapeValue: false,
    },
    detection: options,
    resources: resources,
  });

export default i18n;
