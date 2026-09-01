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
    const icon = hero?.images?.icon_image_small;
    if (!icon) continue;
    if (hero?.class_name?.startsWith('hero_')) {
      icons[hero.class_name.substring('hero_'.length)] = icon;
    }
    // The game logs some heroes under a codename that differs from the API's
    // class_name (e.g. Pocket's class is hero_synth but its models/log lines say
    // "pocket"), so also index by display name. class_name keys take precedence.
    if (hero?.name) {
      const nameKey = hero.name
        .toLowerCase()
        .replace(/^the\s+/, '')
        .replace(/[^a-z0-9]/g, '');
      if (nameKey && !icons[nameKey]) icons[nameKey] = icon;
    }
  }
  return icons;
};

// v2: cache key bumped when name-based fallback keys were added to the icon map
const ICON_CACHE_KEY = 'deadlockHeroIcons.v2';

const fetchAndUpdateHeroIconsIfNeeded = async (): Promise<Record<string, string>> => {
  const cachedString = localStorage.getItem(ICON_CACHE_KEY);
  const now = Date.now();
  if (cachedString) {
    const cached: HeroIconData = JSON.parse(cachedString);
    if (now - cached.timestamp < 86400000) {
      return cached.icons;
    }
  }

  const icons = await fetchHeroIcons();
  const newData: HeroIconData = { icons, timestamp: now };
  localStorage.setItem(ICON_CACHE_KEY, JSON.stringify(newData));
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
