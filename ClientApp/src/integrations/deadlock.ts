import { useEffect, useState } from 'react';

// Deadlock has no first-party asset CDN; assets.deadlock-api.com serves the
// game's hero data (codenames, names, icon urls) extracted from game files.
// The backend stores the hero codename (e.g. "atlas") in metadata.champion.
const HEROES_URL = 'https://assets.deadlock-api.com/v2/heroes';

interface HeroIconData {
  icons: Record<string, string>; // codename (e.g. "atlas") -> icon url
  timestamp: number;
}

const fetchHeroIcons = async (): Promise<Record<string, string>> => {
  const response = await fetch(HEROES_URL);
  const heroes = await response.json();
  const icons: Record<string, string> = {};
  for (const hero of heroes) {
    if (hero?.class_name?.startsWith('hero_') && hero?.images?.icon_image_small) {
      icons[hero.class_name.substring('hero_'.length)] = hero.images.icon_image_small;
    }
  }
  return icons;
};

const fetchAndUpdateHeroIconsIfNeeded = async (): Promise<Record<string, string>> => {
  const cachedString = localStorage.getItem('deadlockHeroIcons');
  const now = Date.now();
  if (cachedString) {
    const cached: HeroIconData = JSON.parse(cachedString);
    if (now - cached.timestamp < 86400000) {
      return cached.icons;
    }
  }

  const icons = await fetchHeroIcons();
  const newData: HeroIconData = { icons, timestamp: now };
  localStorage.setItem('deadlockHeroIcons', JSON.stringify(newData));
  return icons;
};

export const useDeadlockHeroIcon = (game: string, heroKey?: string) => {
  const [iconUrl, setIconUrl] = useState('');

  useEffect(() => {
    const resolveIcon = async () => {
      try {
        const icons = await fetchAndUpdateHeroIconsIfNeeded();
        setIconUrl((heroKey && icons[heroKey]) || '');
      } catch {
        setIconUrl('');
      }
    };

    if (game === 'Deadlock' && heroKey) {
      resolveIcon();
    }
  }, [game, heroKey]);

  return iconUrl;
};
