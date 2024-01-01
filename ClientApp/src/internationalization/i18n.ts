
import i18n from "i18next";
import XHR from "i18next-http-backend";
import LanguageDetector from 'i18next-browser-languagedetector';
import { initReactI18next } from "react-i18next";
import * as locales from "./locales";

const options = {
  order: ['querystring', 'navigator'],
  lookupQuerystring: 'lng'
};

type Translation = { [key: string]: string };

type Translations = {
  [key: string]: { translation: Translation };
};

const typedTranslations: Translations = locales;

const resources: Translations = Object.keys(typedTranslations).reduce((acc, key) => {
  acc[key] = typedTranslations[key];
  return acc;
}, {} as Translations);

i18n
  .use(XHR)
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    debug: false,
    fallbackLng: 'en',
    interpolation: {
      escapeValue: false,
    },
    detection: options,
    resources: resources
  });

export default i18n;