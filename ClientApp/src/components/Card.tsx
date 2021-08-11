import React from 'react';
import { Link } from 'react-router-dom';

interface Props {
  game?: string;
  modified?: string;
  date?: string;
  url?: string;
}

export const Card: React.FC<Props> = ({game, url="video_thumbnail_placeholder.png"}: Props) => {
	return (
    <Link to="/editor" className="w-full block h-full">
      <div className="max-h-40 w-full object-cover overflow-hidden items-center">
        <img alt="thumbnail" src={url}
          onError={(e)=>{(e.target as HTMLImageElement).onerror = null; (e.target as HTMLImageElement).src="video_thumbnail_placeholder.png"}}/>
      </div>
      <p className="bg-white dark:bg-gray-800 w-full p-4 text-gray-800 dark:text-white text-xs font-medium mb-2">
        Game Unknown
        <p className="text-gray-500 dark:text-gray-300 font-light">
          2021/8/7 | 4:20 PM | 0.30 GB
        </p>
      </p>
    </Link>
	)
}

export default Card;