import Card from '../components/Card';
import React, { Dispatch, SetStateAction, useEffect, useState } from 'react';
import { VirtuosoGrid } from 'react-virtuoso'
import { postMessage } from '../helpers/messenger';
import VideoSortControls from '../components/VideoSortControls';
import VideoDeleteControls from '../components/VideoDeleteControls';

interface Props {
  videoType: string;
  gameList: string[];
  game: string;
  sortBy: string;
  videos: Video[];
  size: number;
  scrollPos: number;
  setScrollPos: Dispatch<SetStateAction<number>>;
}

export const VideosPage: React.FC<Props> = ({videoType, gameList, game, sortBy, videos, size, scrollPos, setScrollPos}) => {
  const [checkedVideos, setCheckedVideos] = useState(Array((videos != null ? videos.length : 0)).fill(false));
  const [checkedLength, setCheckedLength] = useState(0);
  const [videoView, setVideoView] = useState('grid');
  const [customScrollParent, setCustomScrollParent] = useState<HTMLElement | null>(null);

  useEffect(() => {
    if(videoType === "Sessions" && videos != null && localStorage.getItem("videoMetadata") !== null) { // this purges videoMetadata of sessions that do not exist anymore
      let json = localStorage.getItem("videoMetadata") || '{}';
      let videoMetadata = JSON.parse(json);
      let updatedVideoMetadata: any = {};
      videos.forEach(video => {
        if(videoMetadata[`/${video.fileName}`] !== undefined) {
          updatedVideoMetadata[`/${video.fileName}`] = videoMetadata[`/${video.fileName}`];
        }
      });
      if(JSON.stringify(updatedVideoMetadata) === JSON.stringify({})) return;
      localStorage.setItem("videoMetadata", JSON.stringify(updatedVideoMetadata));
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [videos]);

  useEffect(() => {
    const timer = setTimeout(() => {
      if(customScrollParent)
        customScrollParent.scrollTop = scrollPos;
    }, 69); // (?) VirtuosoGrid takes too long to populate and so we need to wait a little before we set scrollPos
    return () => clearTimeout(timer);
  }, [customScrollParent]);

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
      
      {videos != null ? 
      <div ref={setCustomScrollParent} onScroll={(e) => setScrollPos((e.target as HTMLElement).scrollTop)} className="h-full overflow-y-auto"><VirtuosoGrid
        //@ts-ignore
        customScrollParent={customScrollParent}
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
            folder={videos[index].folder}
            date={videos[index].date}
            size={videos[index].size}
            duration={videos[index].metadata.duration}
            kills={videos[index].metadata.kills !== undefined ? videos[index].metadata.kills : undefined}
            assists={videos[index].metadata.assists !== undefined ? videos[index].metadata.assists : undefined}
            deaths={videos[index].metadata.deaths !== undefined ? videos[index].metadata.deaths : undefined}
            thumb={videos[index].thumbnail}
            checked={checkedVideos[index]}
            onChange={(e) => onVideoSelected(e, index)}/>
        }
      /></div>
      :
      // loading spinner
      <div className="flex items-center justify-center h-full">
        <svg className="animate-spin w-20 h-20" xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16">
          <path d="M11.534 7h3.932a.25.25 0 0 1 .192.41l-1.966 2.36a.25.25 0 0 1-.384 0l-1.966-2.36a.25.25 0 0 1 .192-.41zm-11 2h3.932a.25.25 0 0 0 .192-.41L2.692 6.23a.25.25 0 0 0-.384 0L.342 8.59A.25.25 0 0 0 .534 9z"></path>
          <path fillRule="evenodd" d="M8 3c-1.552 0-2.94.707-3.857 1.818a.5.5 0 1 1-.771-.636A6.002 6.002 0 0 1 13.917 7H12.9A5.002 5.002 0 0 0 8 3zM3.1 9a5.002 5.002 0 0 0 8.757 2.182.5.5 0 1 1 .771.636A6.002 6.002 0 0 1 2.083 9H3.1z"></path>
        </svg>
      </div>}
    </div>
	)
}

export default VideosPage;