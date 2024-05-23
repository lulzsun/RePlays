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

export const getLatestVersion = async () => {
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
