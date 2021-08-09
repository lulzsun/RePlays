import React from 'react';
import { Link } from 'react-router-dom';

interface Props {
  game?: string;
  modified?: string;
  date?: string;
  url?: string;
}

export const Card: React.FC<Props> = ({game, url}) => {
	return (
    <div className="overflow-hidden shadow-lg rounded-lg h-90 md:w-auto cursor-pointer m-auto">
      <Link to="/editor" className="w-full block h-full">
        <div className="max-h-40 w-full object-cover bg-black overflow-hidden items-center">
          <img alt="thumbnail" src={url}
            onError={(e)=>{(e.target as HTMLImageElement).onerror = null; (e.target as HTMLImageElement).src="video_thumbnail_placeholder.png"}}/>
        </div>
        <div className="bg-white dark:bg-gray-800 w-full p-4">
          <p className="text-gray-800 dark:text-white text-xs font-medium mb-2">
            Game Unknown
          </p>
          <p className="text-gray-500 dark:text-gray-300 font-light text-xs">
            2021/8/7 | 4:20 PM | 0.30 GB
          </p>
        </div>
      </Link>
    </div>
	)
}

export default Card;