import {useEffect, useState} from "react";

const LATEST_VERSION_URL = 'https://ddragon.leagueoflegends.com/api/versions.json';

const fetchLatestVersion = async () => {
  const response = await fetch(LATEST_VERSION_URL);
  const versions = await response.json();
  return versions[0];
};

interface VersionData {
  latestVersion: string;
  timestamp: number;
}

export const fetchAndUpdateLatestLeagueVersionIfNeeded = async () => {
  const versionDataString = localStorage.getItem('latestLeagueVersion');
  const now = Date.now();
  if (versionDataString) {
    const versionData: VersionData = JSON.parse(versionDataString);
    if (now - versionData.timestamp < 86400000) {
      return versionData.latestVersion;
    }
  }

  const latestVersion = await fetchLatestVersion();
  const newVersionData: VersionData = {
    latestVersion,
    timestamp: now,
  };

  localStorage.setItem('latestLeagueVersion', JSON.stringify(newVersionData));
  return latestVersion;
};

export const useLatestLeagueVersion = (game: string) => {
  const [latestLeagueVersion, setLatestLeagueVersion] = useState('');

  useEffect(() => {
    const getLatestLeagueVersionFromLocalStorage = async () => {
      const versionData = localStorage.getItem('latestLeagueVersion');
      if (versionData) {
        const parsedVersionData = JSON.parse(versionData);
        setLatestLeagueVersion(parsedVersionData.latestVersion || '');
      } else {
        const latestVersion = await fetchAndUpdateLatestLeagueVersionIfNeeded();
        setLatestLeagueVersion(latestVersion);
      }
    };

    if (game === 'League of Legends') {
      getLatestLeagueVersionFromLocalStorage();
    }
  }, [game]);

  return latestLeagueVersion;
};