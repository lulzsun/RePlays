import Card from '../components/Card';
import React, { useState } from 'react';
import { VirtuosoGrid } from 'react-virtuoso'
import { postMessage } from '../helpers/messenger';
import VideoSortControls from '../components/VideoSortControls';
import VideoDeleteControls from '../components/VideoDeleteControls';
import { useEffect } from 'react';

interface Props {
  videoType: string;
  gameList: string[];
  game: string;
  sortBy: string;
  videos: Video[];
  size: number;
}

export const VideosPage: React.FC<Props> = ({videoType, gameList, game, sortBy, videos, size}) => {
  const [checkedVideos, setCheckedVideos] = useState(Array(videos.length).fill(false));
  const [checkedLength, setCheckedLength] = useState(0);
  const [videoView, setVideoView] = useState('grid');

  useEffect(() => {
    if(videoType === "Sessions") { // this purges videoMetadata of sessions that do not exist anymore
      let videoMetadata = JSON.parse(localStorage.getItem("videoMetadata")!);
      let updatedVideoMetadata = JSON.parse(localStorage.getItem("videoMetadata")!);
      updatedVideoMetadata = {};
      videos.forEach(video => {
        if(videoMetadata[`/${video.game}/${video.fileName}`] !== undefined) {
          updatedVideoMetadata[`/${video.game}/${video.fileName}`] = videoMetadata[`/${video.game}/${video.fileName}`];
        }
      });
      localStorage.setItem("videoMetadata", JSON.stringify(updatedVideoMetadata));
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [videos]);

  function onVideoSelected(e: React.ChangeEvent<HTMLInputElement>, index:number) {
    console.log((e.target as HTMLInputElement).checked);

    if((e.target as HTMLInputElement).checked && !checkedVideos[index])
      checkedVideos[index] = true;
    else if(!(e.target as HTMLInputElement).checked && checkedVideos[index])
      checkedVideos[index] = false;

    setCheckedLength(checkedVideos.filter(x => x === true).length);
  }

  function onVideoDelete() {
    var filePaths: string[] = [];
    checkedVideos.forEach((video, i) => {
      if(video === true)
        filePaths.push(`${videos[i].game}/${videos[i].fileName}`);
    });
    setCheckedVideos(Array(videos.length).fill(false)); setCheckedLength(0);
    postMessage('Delete', {filePaths});
  }

	return (
    <div className="flex flex-col h-full border-0 border-b"> 
      <div className="pb-4 flex-initial border-0 border-b">
       {videoType}
       <VideoSortControls gameList={gameList} game={game} sortBy={sortBy} size={size} setVideoView={setVideoView}/>
       {checkedLength > 0 && 
       <VideoDeleteControls length={checkedLength} 
         selectAll={() => {setCheckedVideos(Array(videos.length).fill(true)); setCheckedLength(videos.length);}}
         unSelectAll={() => {setCheckedVideos(Array(videos.length).fill(false)); setCheckedLength(0);}}
         deleteSelected={() => onVideoDelete()}/>}
      </div>
      <VirtuosoGrid
        totalCount={videos.length}
        overscan={4}
        listClassName={(
          videoView === 'grid' ? 
          "gap-8 grid grid-flow-row sm:grid-cols-2 md:grid-cols-3 xl:grid-cols-4 2xl:grid-cols-5 pr-8 mt-4 mb-4" 
          : 
          "gap-8 grid grid-flow-row grid-cols-1 pr-8 mt-4 mb-4" // TODO, make this look better
        )}
        itemClassName={"overflow-hidden shadow-lg h-90 md:w-auto cursor-pointer m-auto"}
        itemContent={index => 
          <Card key={videos[index].fileName} 
            game={videos[index].game}
            video={videos[index].fileName}
            videoType={videoType}
            date={videos[index].date}
            size={videos[index].size}
            thumb={videos[index].thumbnail}
            checked={checkedVideos[index]}
            onChange={(e) => onVideoSelected(e, index)}/>
        }
      />
    </div>
	)
}

export default VideosPage;