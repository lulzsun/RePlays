import React, { useContext } from 'react';
import { Link } from 'react-router-dom';
import { formatBytes } from '../helpers/utils';
import { postMessage } from '../helpers/messenger';
import { ModalContext } from '../App';
import UploadModal from '../pages/UploadModal';

interface Props {
  game?: string;
  size?: number;
  date?: string;
  video?: string;
  videoType?: string;
  thumb?: string;
  checked?: boolean;
  onChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
}

export const Card: React.FC<Props> = ({date=Date.now().toString(), game="Game Unknown", thumb="video_thumbnail_placeholder.png", size=0, video="", videoType="", checked, onChange}) => {
  const modalCtx = useContext(ModalContext);
  
  function handleUpload() {
    console.log(`${game} ${video} ${videoType} to upload`);
    var thumb = `${window.location.protocol}//${window.location.host}/Plays/${game}/.thumbs/${video}`;
    thumb = thumb.substr(0, thumb.lastIndexOf('.')) + ".png" || thumb + ".png";

    modalCtx?.setData({title: "Upload", context: <UploadModal video={video} game={game} thumb={thumb}/>, cancel: true});
    modalCtx?.setOpen(true);
  }
  
  return (
    <div className={"relative w-full block h-full group rounded-lg border " + (checked ? "border-blue-500" : "border-gray-500")}>
      <div className="absolute z-40 w-full flex justify-between">
        <div className={"m-2 group-hover:opacity-100 " + (checked ? "opacity-100" : "opacity-0")}>
          <input type="checkbox" className="h-4 w-4" checked={(checked === undefined || checked === false ? false : true)} onChange={(e) => {onChange(e);}}/>
        </div>
        <div className="m-1 mr-2 dropdown" style={{zIndex: 50}}>
          <button type="button" aria-haspopup="true" aria-expanded="true" aria-controls="headlessui-menu-items-117" className="opacity-0 group-hover:opacity-100">
            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" className="h-6 w-6 text-white" viewBox="0 0 16 16">
              <path d="M3 9.5a1.5 1.5 0 1 1 0-3 1.5 1.5 0 0 1 0 3zm5 0a1.5 1.5 0 1 1 0-3 1.5 1.5 0 0 1 0 3zm5 0a1.5 1.5 0 1 1 0-3 1.5 1.5 0 0 1 0 3z"/>
            </svg>
          </button>
          <div className="opacity-0 invisible dropdown-menu transition-all duration-300 transform origin-top-right -translate-y-2 scale-95">
            <div className="absolute right-0 w-auto mt-2 origin-top-right bg-white border border-gray-500 divide-y divide-gray-100 rounded-md shadow-lg outline-none" aria-labelledby="headlessui-menu-button-1" id="headlessui-menu-items-117" role="menu">
              <div className="cursor-pointer text-gray-700 flex justify-between w-full px-4 py-2 text-sm leading-5 text-right whitespace-nowrap"
              onClick={() => {postMessage("ShowInFolder", {filePath: `${game}/${video}`})}}>Show In Folder</div>
              <div className="cursor-pointer text-gray-700 flex justify-between w-full px-4 py-2 text-sm leading-5 text-right whitespace-nowrap"
              onClick={() => {postMessage("Delete", {filePaths: [`${game}/${video}`]})}}>Delete</div>
              {videoType === "Clips" && <div className="cursor-pointer text-gray-700 flex justify-between w-full px-4 py-2 text-sm leading-5 text-right whitespace-nowrap"
              onClick={() => {handleUpload()}}>Upload</div> }
            </div>
          </div>
        </div>
      </div>
      <Link to={`/player/${game}/${video}/${videoType}`}>
        <div className="relative w-full rounded-t-lg object-cover overflow-hidden items-center">
          <div className="absolute z-30 w-full h-full bg-black opacity-0 group-hover:opacity-50"/>
          <img className="absolute z-20 w-full" alt="" src={`${window.location.protocol}//${window.location.host}/Plays/${game}/.thumbs/${thumb}`}/>
          <img className="relative z-10 w-full" alt="" src={"video_thumbnail_placeholder.png"}/>
        </div>
        <div className="bg-white dark:bg-gray-900 text-gray-800 dark:text-white w-full rounded-b-lg p-4 text-xs font-medium">
          {game}
          <p className="text-gray-700 dark:text-gray-400 font-light">
            {new Date(date).toLocaleDateString()} | {new Date(date).toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'})} | {formatBytes(size)}
          </p>
        </div>
      </Link>
    </div>
	)
}

export default Card;