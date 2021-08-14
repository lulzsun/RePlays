import { SyntheticEvent, useEffect } from 'react';
import { useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import Clip from '../components/Clip';

type PlayerParams = {
  game: string;
  video: string;
};

export default function Player () {
  let { game, video } = useParams<PlayerParams>();
  const videoElement = useRef<HTMLVideoElement>(null);
  const timelineElement = useRef<HTMLDivElement>(null);
  const seekWindowElement = useRef<HTMLDivElement>(null);
  const seekBarElement = useRef<HTMLDivElement>(null);
  
  var seekDragging = false, clipDragging = -1, clipDragOffset = 0;
  const [zoom, setZoom] = useState(100);
  const [clips, setClips] = useState<Clip[]>([]);
  const clipsRef = useRef<HTMLDivElement[]>([]);

  useEffect(() => {
    document.addEventListener('mousedown', handleOnMouseDown);
    document.addEventListener('mousemove', handleOnMouseMove);
    document.addEventListener('mouseup', handleOnMouseUp);
    return () => {
      document.removeEventListener('mousedown', handleOnMouseDown);
      document.removeEventListener('mousemove', handleOnMouseMove);
      document.removeEventListener('mouseup', handleOnMouseUp);
    }
  }, []);

  function handleOnMouseDown(e: MouseEvent) {
    let element = e.target as HTMLDivElement;
  
    // seeker handling
    if(element === seekBarElement.current) {
      seekDragging = true;
    }
    else if(seekWindowElement.current?.contains(element)) {
      if (e.detail === 1) {
        if(element === seekWindowElement.current)
          mouseSeek(e);
      } else if (e.detail === 2) {
        mouseSeek(e);
      }
    }

    // clips handling
    if(clipsRef.current?.indexOf(element.parentElement as HTMLDivElement) != -1) {
      let index = clipsRef.current?.indexOf(element.parentElement as HTMLDivElement);
      clipDragging = index;
      clipDragOffset = e.clientX - clipsRef.current[clipDragging].getBoundingClientRect().left;
    }
  }

  function handleOnMouseMove(e: MouseEvent) {
    if(seekDragging) {
      mouseSeek(e);
    }
    if(clipDragging !== -1 && seekWindowElement.current && timelineElement.current) {
      let clickLeft = (e.clientX - clipDragOffset + timelineElement.current.scrollLeft - seekWindowElement.current.offsetLeft);
      if(clickLeft < 0) clickLeft = 0;
      if(clickLeft > seekWindowElement.current.clientWidth - clipsRef.current[clipDragging].getBoundingClientRect().width) 
        clickLeft = seekWindowElement.current.clientWidth - clipsRef.current[clipDragging].getBoundingClientRect().width;
      clipsRef.current[clipDragging].style.left = `${clickLeft / seekWindowElement.current.clientWidth * 100}%`;
    }
  }

  function handleOnMouseUp(e: MouseEvent) {
    seekDragging = false;
    clipDragging = -1;
  }

  function handleAddClip() {
    if(seekWindowElement.current && seekBarElement.current) {
      let start = seekBarElement.current.offsetLeft / seekWindowElement.current.clientWidth * 100;

      console.log('add');
      let newClips = clips.slice();
      newClips.push({id: Date.now(), start: start, duration: 10});
      setClips(newClips);
    }
  }

  function handleVideoLoad(e: SyntheticEvent) {
    console.log((e));
  }

  function handleVideoPlaying(e: SyntheticEvent) {
    const videoElement = (e.target as HTMLVideoElement);
    if(seekBarElement.current) {
      seekBarElement.current.style.left = `calc(${videoElement.currentTime / videoElement.duration * 100}% - 3px)`;
    }
  }

  function mouseSeek(e: MouseEvent) {
    if(seekBarElement.current && seekWindowElement.current && videoElement.current && timelineElement.current) { 
      let clickLeft = (e.clientX + timelineElement.current.scrollLeft - seekWindowElement.current.offsetLeft);
      if(clickLeft < 0) clickLeft = 0;
      if(clickLeft > seekWindowElement.current.clientWidth) clickLeft = seekWindowElement.current.clientWidth;
      videoElement.current.currentTime = (clickLeft / seekWindowElement.current.clientWidth) * videoElement.current.duration;
      seekBarElement.current.style.left = `${clickLeft - 3}px`;
    }
  }

  return (
    <div className="h-full flex flex-col">
      <div className="relative flex w-full h-full bg-black justify-center cursor-pointer" 
        onClick={() => {
          (videoElement.current?.paused ? videoElement.current?.play() : videoElement.current?.pause())
        }}>
        <video ref={videoElement} className="absolute h-full" src={`${window.location.protocol}//${window.location.host}/Plays/${game}/${video}`} 
          onLoadedMetadata={handleVideoLoad} 
          onTimeUpdate={handleVideoPlaying}/>
      </div>

      <div className="flex flex-initial h-20 grid grid-flow-row">
        <div ref={timelineElement} className="w-full h-full overflow-x-scroll bg-gray-400">
          <div style={{ height: '1rem', width: `calc(${zoom}% - 12px)` }} className="mx-1.5 grid grid-flow-col bg-gray-400 border-gray-300 border-l-2">
            <div className="border-gray-300 border-r-2"></div>
            <div className="border-gray-300 border-r-2"></div>
            <div className="border-gray-300 border-r-2"></div>
            <div className="border-gray-300 border-r-2"></div>
            <div className="border-gray-300 border-r-2"></div>
            <div className="border-gray-300 border-r-2"></div>
          </div>

          <div ref={seekWindowElement} style={{ height: 'calc(100% - 1rem)', width: `calc(${zoom}% - 12px)` }} className="mx-1.5 relative bg-gray-300">
            <div ref={seekBarElement} style={{ zIndex: 999, width: '6px', left: '-3px'}} className="absolute bg-red-500 rounded-lg h-full cursor-ew-resize"/>
            {clips && clips.map((clip, i) => {
              return <Clip key={clip.id} ref={e => clipsRef.current[i] = e!} id={clip.id} start={clip.start} duration={clip.duration}/>
            })}
          </div>
        </div> 
      </div>

      <div className="flex flex-initial grid grid-flow-col grid-cols-3">
        <div className="flex justify-start">
          <div className="border-2 rounded-lg">
            <button className="justify-center w-auto h-full px-4 py-2 text-sm font-medium leading-5 text-gray-700 transition duration-150 ease-in-out bg-white hover:bg-gray-200 hover:text-gray-500 focus:outline-none focus:border-blue-300 focus:shadow-outline-blue active:bg-gray-50 active:text-gray-800" 
              type="button">
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" className="align-bottom inline" viewBox="0 0 16 16">
                  <path d="m11.596 8.697-6.363 3.692c-.54.313-1.233-.066-1.233-.697V4.308c0-.63.692-1.01 1.233-.696l6.363 3.692a.802.802 0 0 1 0 1.393z"/>
                </svg>
            </button>
            <button className="justify-center w-auto h-full px-4 py-2 text-sm font-medium leading-5 text-gray-700 transition duration-150 ease-in-out bg-white hover:bg-gray-200 hover:text-gray-500 focus:outline-none focus:border-blue-300 focus:shadow-outline-blue active:bg-gray-50 active:text-gray-800" 
              type="button">
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" className="align-bottom inline" viewBox="0 0 16 16">
                  <path fillRule="evenodd" d="M8 3a5 5 0 1 1-4.546 2.914.5.5 0 0 0-.908-.417A6 6 0 1 0 8 2v1z"/>
                  <path d="M8 4.466V.534a.25.25 0 0 0-.41-.192L5.23 2.308a.25.25 0 0 0 0 .384l2.36 1.966A.25.25 0 0 0 8 4.466z"/>
                </svg>
            </button>
            <span className="cursor-pointer -mt-0.5 mb-0.5 inline-block align-middle w-auto h-full px-4 py-2 text-sm font-medium leading-5 text-gray-700 transition duration-150 ease-in-out bg-white hover:bg-gray-200 hover:text-gray-500 active:bg-gray-50 active:text-gray-800">
              1x
            </span>
            <button className="justify-center w-auto h-full px-4 py-2 text-sm font-medium leading-5 text-gray-700 transition duration-150 ease-in-out bg-white hover:bg-gray-200 hover:text-gray-500 focus:outline-none focus:border-blue-300 focus:shadow-outline-blue active:bg-gray-50 active:text-gray-800" 
              type="button">
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" className="align-bottom inline" viewBox="0 0 16 16">
                  <path d="M11.536 14.01A8.473 8.473 0 0 0 14.026 8a8.473 8.473 0 0 0-2.49-6.01l-.708.707A7.476 7.476 0 0 1 13.025 8c0 2.071-.84 3.946-2.197 5.303l.708.707z"/>
                  <path d="M10.121 12.596A6.48 6.48 0 0 0 12.025 8a6.48 6.48 0 0 0-1.904-4.596l-.707.707A5.483 5.483 0 0 1 11.025 8a5.483 5.483 0 0 1-1.61 3.89l.706.706z"/>
                  <path d="M8.707 11.182A4.486 4.486 0 0 0 10.025 8a4.486 4.486 0 0 0-1.318-3.182L8 5.525A3.489 3.489 0 0 1 9.025 8 3.49 3.49 0 0 1 8 10.475l.707.707zM6.717 3.55A.5.5 0 0 1 7 4v8a.5.5 0 0 1-.812.39L3.825 10.5H1.5A.5.5 0 0 1 1 10V6a.5.5 0 0 1 .5-.5h2.325l2.363-1.89a.5.5 0 0 1 .529-.06z"/>
                </svg>
            </button>
          </div>
        </div>

        <div className="flex justify-center">
          <div className="border-2 rounded-lg">
            
            <span className="cursor-pointer -mt-0.5 mb-0.5 inline-block align-middle w-auto h-full px-4 py-2 text-sm font-medium leading-5 text-gray-700 transition duration-150 ease-in-out bg-white hover:bg-gray-200 hover:text-gray-500 active:bg-gray-50 active:text-gray-800">
              <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" className="-mt-0.5 align-center mr-2 inline" viewBox="0 0 16 16">
                <path d="M2 3a.5.5 0 0 0 .5.5h11a.5.5 0 0 0 0-1h-11A.5.5 0 0 0 2 3zm2-2a.5.5 0 0 0 .5.5h7a.5.5 0 0 0 0-1h-7A.5.5 0 0 0 4 1zm2.765 5.576A.5.5 0 0 0 6 7v5a.5.5 0 0 0 .765.424l4-2.5a.5.5 0 0 0 0-.848l-4-2.5z"/>
                <path d="M1.5 14.5A1.5 1.5 0 0 1 0 13V6a1.5 1.5 0 0 1 1.5-1.5h13A1.5 1.5 0 0 1 16 6v7a1.5 1.5 0 0 1-1.5 1.5h-13zm13-1a.5.5 0 0 0 .5-.5V6a.5.5 0 0 0-.5-.5h-13A.5.5 0 0 0 1 6v7a.5.5 0 0 0 .5.5h13z"/>
              </svg>
              2 Clips: 0:10
            </span>
            <button className="justify-center w-auto h-full px-4 py-2 text-sm font-medium leading-5 text-gray-700 transition duration-150 ease-in-out bg-white hover:bg-gray-200 hover:text-gray-500 focus:outline-none focus:border-blue-300 focus:shadow-outline-blue active:bg-gray-50 active:text-gray-800" 
              type="button">
              <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" className="align-bottom inline" viewBox="0 0 16 16">
                <path d="M2 1a1 1 0 0 0-1 1v12a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1V2a1 1 0 0 0-1-1H9.5a1 1 0 0 0-1 1v4.5h2a.5.5 0 0 1 .354.854l-2.5 2.5a.5.5 0 0 1-.708 0l-2.5-2.5A.5.5 0 0 1 5.5 6.5h2V2a2 2 0 0 1 2-2H14a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2V2a2 2 0 0 1 2-2h2.5a.5.5 0 0 1 0 1H2z"/>
              </svg>
            </button>
          </div>
        </div>

        <div className="flex justify-end">
          <div className="border-2 rounded-lg">
            <button className="justify-center w-auto h-full px-4 py-2 text-sm font-medium leading-5 text-gray-700 transition duration-150 ease-in-out bg-white hover:bg-gray-200 hover:text-gray-500 focus:outline-none focus:border-blue-300 focus:shadow-outline-blue active:bg-gray-50 active:text-gray-800" 
              type="button" onClick={() => handleAddClip()}>
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" className="align-bottom inline" viewBox="0 0 16 16">
                  <path d="M3.5 3.5c-.614-.884-.074-1.962.858-2.5L8 7.226 11.642 1c.932.538 1.472 1.616.858 2.5L8.81 8.61l1.556 2.661a2.5 2.5 0 1 1-.794.637L8 9.73l-1.572 2.177a2.5 2.5 0 1 1-.794-.637L7.19 8.61 3.5 3.5zm2.5 10a1.5 1.5 0 1 0-3 0 1.5 1.5 0 0 0 3 0zm7 0a1.5 1.5 0 1 0-3 0 1.5 1.5 0 0 0 3 0z"/>
                </svg>
            </button>
            <span className="text-center cursor-pointer -mt-0.5 mb-0.5 inline-block align-middle w-12 h-full px-4 py-2 text-sm font-medium leading-5 text-gray-700 transition duration-150 ease-in-out bg-white hover:bg-gray-200 hover:text-gray-500 active:bg-gray-50 active:text-gray-800">
              -
            </span>
            <span className="text-center cursor-pointer -mt-0.5 mb-0.5 inline-block align-middle w-auto h-full px-4 py-2 text-sm font-medium leading-5 text-gray-700 transition duration-150 ease-in-out bg-white hover:bg-gray-200 hover:text-gray-500 active:bg-gray-50 active:text-gray-800">
              100%
            </span>
            <span className="text-center cursor-pointer -mt-0.5 mb-0.5 inline-block align-middle w-12 h-full px-4 py-2 text-sm font-medium leading-5 text-gray-700 transition duration-150 ease-in-out bg-white hover:bg-gray-200 hover:text-gray-500 active:bg-gray-50 active:text-gray-800">
              +
            </span>
          </div>
        </div>
      </div>
    </div>
  )
}