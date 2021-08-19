import React from 'react';
import Card from '../components/Card';
import { VirtuosoGrid } from 'react-virtuoso'
import VideoSortControls from '../components/VideoSortControls';

interface Props {
  videoType: string;
  gameList: string[];
  game: string;
  sortBy: string;
  videos: Video[];
  size: number;
}

export const VideosPage: React.FC<Props> = ({videoType, gameList, game, sortBy, videos, size}) => {
	return (
    <div className="flex flex-col h-full border-0 border-b"> 
      <div className="pb-4 flex-initial border-0 border-b">
       {videoType}
       <VideoSortControls gameList={gameList} game={game} sortBy={sortBy} size={size}/>
      </div>
      <VirtuosoGrid
        totalCount={videos.length}
        overscan={4}
        listClassName={"gap-8 grid grid-flow-row sm:grid-cols-2 md:grid-cols-3 xl:grid-cols-4 2xl:grid-cols-5 pr-8 mt-4 mb-4"}
        itemClassName={"overflow-hidden shadow-lg rounded-lg h-90 md:w-auto cursor-pointer m-auto"}
        itemContent={index => 
          <Card key={videos[index].fileName} 
            game={videos[index].game}
            video={videos[index].fileName}
            videoType={videoType}
            date={videos[index].date}
            size={videos[index].size}
            thumb={videos[index].thumbnail}/>
        }
      />
    </div>
	)
}

export default VideosPage;