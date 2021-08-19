import React from 'react';
import { Link } from 'react-router-dom';
import { formatBytes } from '../helpers/utils';

interface Props {
  game?: string;
  size?: number;
  date?: string;
  video?: string;
  videoType?: string;
  thumb?: string;
}

export const Card: React.FC<Props> = ({date=Date.now().toString(), game="Game Unknown", thumb="video_thumbnail_placeholder.png", size=0, video=""}: Props) => {
	return (
    <Link to={`/player/${game}/${video}`} className="w-full block">
      <div className="relative w-full object-cover overflow-hidden items-center">
        <img className="absolute z-20 w-full" alt="thumbnail" src={`${window.location.protocol}//${window.location.host}/Plays/${game}/.thumbs/${thumb}`}/>
        <img className="relative z-10 w-full" alt="thumbnail" src={"video_thumbnail_placeholder.png"}/>
      </div>
      <div className="bg-white dark:bg-gray-800 w-full p-4 text-gray-800 dark:text-white text-xs font-medium mb-2">
        {game}
        <p className="text-gray-500 dark:text-gray-300 font-light">
          {new Date(date).toLocaleDateString()} | {new Date(date).toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'})} | {formatBytes(size)}
        </p>
      </div>
    </Link>
	)
}

export default Card;