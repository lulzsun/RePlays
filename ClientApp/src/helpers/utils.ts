export function secondsToHHMMSS(seconds: number) {
  if(seconds >= 3600)
    return new Date(seconds * 1000).toISOString().substr(11, 8).replace(/^0+/, '');
  else
    return new Date(seconds * 1000).toISOString().substr(14, 5);
}

export function formatBytes(bytes: number, decimals = 2) {
  if (bytes === 0) return '0 Bytes';

  const k = 1024;
  const dm = decimals < 0 ? 0 : decimals;
  const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'];

  const i = Math.floor(Math.log(bytes) / Math.log(k));

  return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
}