import Clip from '../components/Clip';
import { useParams } from 'react-router-dom';
import { secondsToHHMMSS } from '../helpers/utils';
import { SyntheticEvent, useContext, useEffect, useRef, useState } from 'react';
import { ContextMenuContext } from '../App';
import { postMessage } from '../helpers/messenger';

type PlayerParams = {
  game: string;
  video: string;
};

const ZOOMS = [100, 110, 125, 150, 175, 200, 250, 300, 400, 500, 1000, 2000, 3000, 4000, 5000, 7500, 10000];

export default function Player () {
  let { game, video } = useParams<PlayerParams>();
  const videoElement = useRef<HTMLVideoElement>(null);
  const volumeSliderElement = useRef<HTMLInputElement>(null);
  const timelineElement = useRef<HTMLDivElement>(null);
  const seekWindowElement = useRef<HTMLDivElement>(null);
  const seekBarElement = useRef<HTMLDivElement>(null);
  const targetSeekElement = useRef<HTMLDivElement>(null);
  
  const [clips, setClips] = useState<Clip[]>([]);
  const [currentZoom, setZoom] = useState(0);
  const [currentPlaybackRate, setPlaybackRate] = useState(1);
  const [currentTime, setCurrentTime] = useState(0);
  const [playbackClips, setPlaybackClips] = useState(-1);
  const clipsRef = useRef<HTMLDivElement[]>([]);

  const contextMenuCtx = useContext(ContextMenuContext);

  useEffect(() => {
    var seekDragging = false, clipDragging = -1, clipDragOffset = 0, clipResizeDir = '', clipResizeLimit = 0;
    
    function handleOnKeyDown(e: KeyboardEvent) {
      if(e.key === ' ') videoElement.current?.paused ? videoElement.current?.play() : videoElement.current?.pause();
      if(e.key === 'ArrowLeft') videoElement.current!.currentTime -= 5;
      if(e.key === 'ArrowRight') videoElement.current!.currentTime += 5;
    }
  
    function handleOnMouseDown(e: MouseEvent) {
      let element = e.target as HTMLDivElement;
    
      // seeker handling
      if(e.button === 0) {
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
      }
  
      // clips handling
      if(clipsRef.current?.indexOf(element.parentElement as HTMLDivElement) !== -1) {
        let index = clipsRef.current?.indexOf(element.parentElement as HTMLDivElement);
        if(e.button === 0) { // clip reposition
          clipDragging = index;
          clipDragOffset = e.clientX - clipsRef.current[clipDragging]?.getBoundingClientRect().left;
        }
        else if(e.button === 2) { // context menu / Delete Clip
          handleDeleteClip(e, index);
        }
      } else { // clip resizing
        let index = clipsRef.current?.indexOf((element.parentElement)?.parentElement as HTMLDivElement);
        if(e.button === 0) { // clip reposition
          clipDragging = index;
          clipResizeDir = (element.parentElement)?.getAttribute('data-side')!;
          clipResizeLimit = clipsRef.current[clipDragging]?.clientWidth + clipsRef.current[clipDragging]?.offsetLeft;
        }
        else if(e.button === 2) { // context menu / Delete Clip
          handleDeleteClip(e, index);
        }
      }
    }
  
    function handleOnMouseMove(e: MouseEvent) {
      if(seekDragging) {
        mouseSeek(e);
      }
      if(clipDragging !== -1 && clipResizeDir === '') {
        let clickLeft = (e.clientX - clipDragOffset + timelineElement.current!.scrollLeft - seekWindowElement.current!.offsetLeft);
        if(clickLeft < 0) clickLeft = 0;
        else if(clickLeft > seekWindowElement.current!.clientWidth - clipsRef.current[clipDragging].getBoundingClientRect().width) 
          clickLeft = seekWindowElement.current!.clientWidth - clipsRef.current[clipDragging].getBoundingClientRect().width;
        clipsRef.current[clipDragging].style.left = `${clickLeft / seekWindowElement.current!.clientWidth * 100}%`;
      }
      else if (clipDragging !== -1 && clipResizeDir !== '') {
        if(clipResizeDir === 'right') {
          let clickLeft = (e.clientX - clipsRef.current[clipDragging]?.offsetLeft + timelineElement.current!.scrollLeft - seekWindowElement.current!.offsetLeft);
          if(clickLeft > seekWindowElement.current!.offsetWidth) clickLeft = seekWindowElement.current!.offsetWidth;
          clipsRef.current[clipDragging].style.width = `${clickLeft / seekWindowElement.current!.clientWidth * 100}%`;
        }
        else if(clipResizeDir === 'left') {
          let clickLeft = (e.clientX + timelineElement.current!.scrollLeft - seekWindowElement.current!.offsetLeft);
          if(clickLeft < 0) clickLeft = 0;
          if(clickLeft > clipResizeLimit) return;
          clipsRef.current[clipDragging].style.width = `${(clipsRef.current[clipDragging].offsetWidth + (clipsRef.current[clipDragging].offsetLeft - clickLeft)) / seekWindowElement.current!.clientWidth * 100}%`;
          clipsRef.current[clipDragging].style.left = `${clickLeft / seekWindowElement.current!.clientWidth * 100}%`;
        }
      }
    }
  
    function handleOnMouseUp(e: MouseEvent) {
      seekDragging = false;
      clipResizeDir = '';
      if(clipDragging !== -1) {
        let clipsCopy = [...clips];
        clipsCopy[clipDragging].start = clipsRef.current[clipDragging].offsetLeft / seekWindowElement.current!.clientWidth * 100;
        clipsCopy[clipDragging].duration = clipsRef.current[clipDragging].offsetWidth / seekWindowElement.current!.clientWidth * 100;
        setClips(clipsCopy);
        clipDragging = -1;
      }
    }
    
    document.addEventListener('keydown', handleOnKeyDown);
    document.addEventListener('mousedown', handleOnMouseDown);
    document.addEventListener('mousemove', handleOnMouseMove);
    document.addEventListener('mouseup', handleOnMouseUp);

    if(clips.length !== 0) {
      let videoMetadata = JSON.parse(localStorage.getItem("videoMetadata")!);
      videoMetadata[`/${game}/${video}`] = {clips};
      localStorage.setItem("videoMetadata", JSON.stringify(videoMetadata));
    }

    return () => {
      document.removeEventListener('keydown', handleOnKeyDown);
      document.removeEventListener('mousedown', handleOnMouseDown);
      document.removeEventListener('mousemove', handleOnMouseMove);
      document.removeEventListener('mouseup', handleOnMouseUp);
    }
  }, [clips, contextMenuCtx]);

  useEffect(() => {
    seekBarElement.current!.style.left = `calc(${currentTime / videoElement.current!.duration * 100}% - 3px)`;
    targetSeekElement.current!.style.left = seekBarElement.current!.offsetLeft+6 + 'px';
    targetSeekElement.current!.scrollIntoView({
      behavior: 'auto',
      block: 'center',
      inline: 'center'
    });
    timelineElement.current!.scrollTop = 0;
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [currentZoom]);

  function handleAddClip() {
    if(seekWindowElement.current && seekBarElement.current) {
      let start = currentTime / videoElement.current!.duration * 100;
      let newClips = clips.slice();

      if(videoElement.current)
        newClips.push({id: Date.now(), start: start, duration: 10 / videoElement.current.duration * 100}); // 10 seconds
      setClips(newClips);
    }
  }

  function handleDeleteClip(e: MouseEvent, index: number) {
    contextMenuCtx?.setItems([{name: 'Delete', onClick: () => { 
      let newClips = clips.slice();
      if(videoElement.current)
        newClips.splice(index, 1);
      setClips(newClips);

      let videoMetadata = JSON.parse(localStorage.getItem("videoMetadata")!);
      videoMetadata[`/${game}/${video}`] = {clips: newClips};
      localStorage.setItem("videoMetadata", JSON.stringify(videoMetadata));
    }}]);
    contextMenuCtx?.setPosition({x: e.pageX, y: e.pageY});
  }

  function handleSaveClips() {
    let convertedClips: any[] = [];
    clips.forEach(clip => {
      convertedClips.push({
        // now the start & duration are seconds, TODO: maybe have it be this way from the start, instead of having to convert?
        start: clip.start / 100 * videoElement.current!.duration,
        duration: clip.duration / 100 * videoElement.current!.duration
      });
    });
    postMessage("CreateClips", {videoPath: `/${game}/${video}`, clipSegments: convertedClips});
  }

  function handlePlayClips() {
    if(videoElement.current) {
      if(playbackClips === -1) {
        setClips(clips => clips.sort((a, b) => (a.start > b.start) ? 1 : -1));
        setPlaybackClips(0);
        videoElement.current.currentTime = (clips.sort((a, b) => (a.start > b.start) ? 1 : -1)[0].start / 100 * videoElement.current.duration)+0.0001;
        videoElement.current.play();
      } else {
        videoElement.current.pause();
      }
    }
  }

  function handleVideoLoad(e: SyntheticEvent) {
    let videoMetadata = JSON.parse(localStorage.getItem("videoMetadata")!);

    console.log((e));
    if(videoElement.current && volumeSliderElement.current) {
      videoElement.current.volume = parseInt(volumeSliderElement.current.value) / 100;
      videoElement.current.play();
    }
    if(videoMetadata[`/${game}/${video}`])
      setClips(videoMetadata[`/${game}/${video}`].clips);
  }

  function handleVideoPlaying(e: SyntheticEvent) {
    const videoElement = (e.target as HTMLVideoElement);
    if(playbackClips !== -1 && clips) {
      let _playbackClips = playbackClips;
      for (let index = 0; index < clips.length; index++) {
        const clip = clips[index];
        const start = clip.start / 100 * videoElement.duration;
        const duration = clip.duration / 100 * videoElement.duration;
        if(start < videoElement.currentTime && (start+duration) > videoElement.currentTime) {
          setPlaybackClips(index);
          _playbackClips = index;
          break;
        }
      }

      var start = clips[_playbackClips].start / 100 * videoElement.duration;
      var duration = clips[_playbackClips].duration / 100 * videoElement.duration;
      if(videoElement.currentTime >= start+duration) {
        if(clips.length === _playbackClips+1) {
          videoElement.pause();
        }
        else {
          setPlaybackClips(value => value+1);
          var nextStart = clips[_playbackClips+1].start / 100 * videoElement.duration;
          videoElement.currentTime = nextStart;
        }
      }
      else if(videoElement.currentTime < start) {
        videoElement.currentTime = start+0.0001;
        videoElement.play();
      }
    }
    setCurrentTime(videoElement.currentTime);
    seekBarElement.current!.style.left = `calc(${videoElement.currentTime / videoElement.duration * 100}% - 3px)`;
    targetSeekElement.current!.style.left = seekBarElement.current!.offsetLeft+6 + 'px';
  }

  function mouseSeek(e: MouseEvent) {
    let clickLeft = (e.clientX + timelineElement.current!.scrollLeft - seekWindowElement.current!.offsetLeft);
    if(clickLeft < 0) clickLeft = 0;
    else if(clickLeft > seekWindowElement.current!.clientWidth) clickLeft = seekWindowElement.current!.clientWidth;
    let newCurrentTime = (clickLeft / seekWindowElement.current!.clientWidth) * videoElement.current!.duration;
    videoElement.current!.currentTime = newCurrentTime;
    seekBarElement.current!.style.left = `${clickLeft - 3}px`;
  }

  return (
    <div className="h-full flex flex-col">
      <div className="relative flex w-full h-full bg-black justify-center cursor-pointer" 
        onClick={() => {
          (videoElement.current?.paused ? videoElement.current?.play() : videoElement.current?.pause())
        }}>
        <video ref={videoElement} className="absolute h-full" src={`${window.location.protocol}//${window.location.host}/Plays/${game}/${video}`} 
          onLoadedMetadata={handleVideoLoad} 
          onTimeUpdate={handleVideoPlaying}
          onPause={() => setPlaybackClips(-1)}/>
      </div>

      <div className="flex flex-initial h-20 grid grid-flow-row">
        <div ref={timelineElement} className="w-full h-full overflow-x-scroll overflow-y-hidden bg-gray-400"> 
          <div style={{ height: '1rem', width: `calc(${ZOOMS[currentZoom]}% - 12px)` }} className="inline-block mx-1.5 grid grid-flow-col bg-gray-400 border-gray-300 border-l-2">
            <div className="border-gray-300 border-r-2"></div>
            <div className="border-gray-300 border-r-2"></div>
            <div className="border-gray-300 border-r-2"></div>
            <div className="border-gray-300 border-r-2"></div>
            <div className="border-gray-300 border-r-2"></div>
            <div className="border-gray-300 border-r-2"></div>
          </div>
          <div ref={seekWindowElement} style={{ height: '45px', width: `calc(${ZOOMS[currentZoom]}% - 12px)` }} className="inline-block mx-1.5 relative bg-gray-300">
            <div ref={seekBarElement} style={{ width: '6px', left: '-3px'}} className="z-30 absolute bg-red-500 rounded-lg h-full cursor-ew-resize"/>
            {clips && clips.map((clip, i) => {
              return <Clip key={clip.id} ref={e => clipsRef.current[i] = e!} id={clip.id} start={clip.start} duration={clip.duration}/>
            })}
          </div>
          <div ref={targetSeekElement} style={{ height: 'calc(100%)', width: '6px', left: '3px'}} className="relative bg-green-500 rounded-lg h-full cursor-ew-resize"/>
        </div> 
      </div>

      <div className="flex flex-initial grid grid-flow-col">
        <div className="flex justify-start">
          <div className="border-2 rounded-lg">
            <button title={`${(videoElement.current?.paused ? 'Play' : 'Pause')}`} className="justify-center w-auto h-full px-4 py-2 text-sm font-medium leading-5 text-gray-700 transition duration-150 ease-in-out bg-white hover:bg-gray-200 hover:text-gray-500 focus:outline-none focus:border-blue-300 focus:shadow-outline-blue active:bg-gray-50 active:text-gray-800" 
              type="button" onClick={() => {
                (videoElement.current?.paused ? videoElement.current?.play() : videoElement.current?.pause())
              }}>
                { videoElement.current?.paused ? 
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" className="align-bottom inline" viewBox="0 0 16 16">
                  <path d="m11.596 8.697-6.363 3.692c-.54.313-1.233-.066-1.233-.697V4.308c0-.63.692-1.01 1.233-.696l6.363 3.692a.802.802 0 0 1 0 1.393z"/>
                </svg> : 
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" className="align-bottom inline" viewBox="0 0 16 16">
                  <path d="M5.5 3.5A1.5 1.5 0 0 1 7 5v6a1.5 1.5 0 0 1-3 0V5a1.5 1.5 0 0 1 1.5-1.5zm5 0A1.5 1.5 0 0 1 12 5v6a1.5 1.5 0 0 1-3 0V5a1.5 1.5 0 0 1 1.5-1.5z"/>
                </svg> }
            </button>
            <button title="Rewind 5 Seconds" className="justify-center w-auto h-full px-4 py-2 text-sm font-medium leading-5 text-gray-700 transition duration-150 ease-in-out bg-white hover:bg-gray-200 hover:text-gray-500 focus:outline-none focus:border-blue-300 focus:shadow-outline-blue active:bg-gray-50 active:text-gray-800" 
              type="button" onClick={() => videoElement.current!.currentTime -= 5}>
              <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" className="align-bottom inline" viewBox="0 0 16 16">
                <path fillRule="evenodd" d="M8 3a5 5 0 1 1-4.546 2.914.5.5 0 0 0-.908-.417A6 6 0 1 0 8 2v1z"/>
                <path d="M8 4.466V.534a.25.25 0 0 0-.41-.192L5.23 2.308a.25.25 0 0 0 0 .384l2.36 1.966A.25.25 0 0 0 8 4.466z"/>
              </svg>
            </button>
            <div className="relative z-40 inline-block text-left dropdown">
              <button title="Playback Speed" className="-mt-0.5 mb-0.5 inline-block align-middle w-auto h-full px-4 py-2 text-sm font-medium leading-5 text-gray-700 transition duration-150 ease-in-out bg-white hover:bg-gray-200 hover:text-gray-500 active:bg-gray-50 active:text-gray-800" 
              type="button" aria-haspopup="true" aria-expanded="true" aria-controls="headlessui-menu-items-117">
                {(currentPlaybackRate + '').replace(/^0+/, '')}x
              </button>
              <div className="absolute -top-1/3 opacity-0 invisible dropdown-menu transition-all duration-300 transform">
                <div className="absolute transform -translate-y-full left-0 w-auto origin-top-left bg-white border border-gray-200 divide-y divide-gray-100 rounded-md shadow-lg outline-none" aria-labelledby="headlessui-menu-button-1" id="headlessui-menu-items-117" role="menu">
                  <div className="cursor-pointer text-gray-700 flex justify-between w-full px-4 py-2 text-sm leading-5 text-left" 
                  onClick={() => {videoElement.current!.playbackRate = 0.25; setPlaybackRate(videoElement.current!.playbackRate);}}>.25x</div>
                  <div className="cursor-pointer text-gray-700 flex justify-between w-full px-4 py-2 text-sm leading-5 text-left"
                  onClick={() => {videoElement.current!.playbackRate = 0.5; setPlaybackRate(videoElement.current!.playbackRate);}}>.5x</div>
                  <div className="cursor-pointer text-gray-700 flex justify-between w-full px-4 py-2 text-sm leading-5 text-left"
                  onClick={() => {videoElement.current!.playbackRate = 0.75; setPlaybackRate(videoElement.current!.playbackRate);}}>.75x</div>
                  <div className="cursor-pointer text-gray-700 flex justify-between w-full px-4 py-2 text-sm leading-5 text-left"
                  onClick={() => {videoElement.current!.playbackRate = 1; setPlaybackRate(videoElement.current!.playbackRate);}}>1x</div>
                  <div className="cursor-pointer text-gray-700 flex justify-between w-full px-4 py-2 text-sm leading-5 text-left"
                  onClick={() => {videoElement.current!.playbackRate = 1.5; setPlaybackRate(videoElement.current!.playbackRate);}}>1.5x</div>
                  <div className="cursor-pointer text-gray-700 flex justify-between w-full px-4 py-2 text-sm leading-5 text-left"
                  onClick={() => {videoElement.current!.playbackRate = 2; setPlaybackRate(videoElement.current!.playbackRate);}}>2x</div>
                  <div className="cursor-pointer text-gray-700 flex justify-between w-full px-4 py-2 text-sm leading-5 text-left"
                  onClick={() => {videoElement.current!.playbackRate = 4; setPlaybackRate(videoElement.current!.playbackRate);}}>4x</div>
                </div>
              </div>
            </div>
            <div className="relative z-40 inline-block text-left dropdown">
              <button title="Volume" className="-mt-0.5 mb-0.5 inline-block align-middle w-auto h-full px-4 py-2 text-sm font-medium leading-5 text-gray-700 transition duration-150 ease-in-out bg-white hover:bg-gray-200 hover:text-gray-500 active:bg-gray-50 active:text-gray-800" 
              type="button" aria-haspopup="true" aria-expanded="true" aria-controls="headlessui-menu-items-117">
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" className="align-bottom inline" viewBox="0 0 16 16">
                  <path d="M11.536 14.01A8.473 8.473 0 0 0 14.026 8a8.473 8.473 0 0 0-2.49-6.01l-.708.707A7.476 7.476 0 0 1 13.025 8c0 2.071-.84 3.946-2.197 5.303l.708.707z"/>
                  <path d="M10.121 12.596A6.48 6.48 0 0 0 12.025 8a6.48 6.48 0 0 0-1.904-4.596l-.707.707A5.483 5.483 0 0 1 11.025 8a5.483 5.483 0 0 1-1.61 3.89l.706.706z"/>
                  <path d="M8.707 11.182A4.486 4.486 0 0 0 10.025 8a4.486 4.486 0 0 0-1.318-3.182L8 5.525A3.489 3.489 0 0 1 9.025 8 3.49 3.49 0 0 1 8 10.475l.707.707zM6.717 3.55A.5.5 0 0 1 7 4v8a.5.5 0 0 1-.812.39L3.825 10.5H1.5A.5.5 0 0 1 1 10V6a.5.5 0 0 1 .5-.5h2.325l2.363-1.89a.5.5 0 0 1 .529-.06z"/>
                </svg>
              </button>
              <div className="absolute -top-1/3 opacity-0 invisible dropdown-menu transition-all duration-300 transform">
                <div className="absolute transform -translate-y-full left-0 w-auto origin-top-left bg-white border border-gray-200 divide-y divide-gray-100 rounded-md shadow-lg outline-none" aria-labelledby="headlessui-menu-button-1" id="headlessui-menu-items-117" role="menu">
                  <div className="text-gray-700 flex justify-between w-full px-4 py-2 text-sm leading-5 text-left" role="menuitem">
                    <input ref={volumeSliderElement} type="range" min="1" max="100" step="1" 
                    onChange={(e) => { if(videoElement.current) {videoElement.current.volume = parseInt((e.target as HTMLInputElement).value) / 100;} }}/>
                  </div>
                </div>
              </div>
            </div>
            <span className="-mt-0.5 mb-0.5 inline-block align-middle w-auto h-full px-4 py-2 text-sm font-medium leading-5 text-gray-700 transition duration-150 ease-in-out bg-white hover:bg-gray-200 hover:text-gray-500 active:bg-gray-50 active:text-gray-800">
              {`${secondsToHHMMSS(currentTime)} / ${secondsToHHMMSS(videoElement.current?.duration || 0)}`}
            </span>
          </div>
        </div>

        <div className="flex justify-center">
          {clips.length > 0 && <div className="border-2 rounded-lg">
            <span title={`${playbackClips === -1 ? 'Play' : 'Stop'} Clip${clips.length > 1 ? 's' : ''}`} className="cursor-pointer -mt-0.5 mb-0.5 inline-block align-middle w-auto h-full px-4 py-2 text-sm font-medium leading-5 text-gray-700 transition duration-150 ease-in-out bg-white hover:bg-gray-200 hover:text-gray-500 active:bg-gray-50 active:text-gray-800"
              onClick={() => handlePlayClips()}>
              {(playbackClips === -1 ? 
              <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" className="-mt-0.5 align-center mr-2 inline" viewBox="0 0 16 16">
                <path d="M8 15A7 7 0 1 1 8 1a7 7 0 0 1 0 14zm0 1A8 8 0 1 0 8 0a8 8 0 0 0 0 16z"/>
                <path d="M6.271 5.055a.5.5 0 0 1 .52.038l3.5 2.5a.5.5 0 0 1 0 .814l-3.5 2.5A.5.5 0 0 1 6 10.5v-5a.5.5 0 0 1 .271-.445z"/>
              </svg> :
              <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" className="-mt-0.5 align-center mr-2 inline" viewBox="0 0 16 16">
                <path d="M8 15A7 7 0 1 1 8 1a7 7 0 0 1 0 14zm0 1A8 8 0 1 0 8 0a8 8 0 0 0 0 16z"/>
                <path d="M5 6.5A1.5 1.5 0 0 1 6.5 5h3A1.5 1.5 0 0 1 11 6.5v3A1.5 1.5 0 0 1 9.5 11h-3A1.5 1.5 0 0 1 5 9.5v-3z"/>
              </svg>
              )}
              {clips.length} Clip{clips.length > 1 && 's'}: {secondsToHHMMSS(clips.map(clip => clip.duration / 100 * videoElement.current!.duration).reduce((prev, next) => prev + next))}
            </span>
            <button title={`Save Clip${clips.length > 1 ? 's' : ''}`} type="button" className="justify-center w-auto h-full px-4 py-2 text-sm font-medium leading-5 text-gray-700 transition duration-150 ease-in-out bg-white hover:bg-gray-200 hover:text-gray-500 focus:outline-none focus:border-blue-300 focus:shadow-outline-blue active:bg-gray-50 active:text-gray-800" 
              onClick={() => handleSaveClips()}>
              <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" className="align-bottom inline" viewBox="0 0 16 16">
                <path d="M2 1a1 1 0 0 0-1 1v12a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1V2a1 1 0 0 0-1-1H9.5a1 1 0 0 0-1 1v4.5h2a.5.5 0 0 1 .354.854l-2.5 2.5a.5.5 0 0 1-.708 0l-2.5-2.5A.5.5 0 0 1 5.5 6.5h2V2a2 2 0 0 1 2-2H14a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2V2a2 2 0 0 1 2-2h2.5a.5.5 0 0 1 0 1H2z"/>
              </svg>
            </button>
          </div>}
        </div>

        <div className="flex justify-end">
          <div className="border-2 rounded-lg">
            <button title="Clip" className="justify-center w-auto h-full px-4 py-2 text-sm font-medium leading-5 text-gray-700 transition duration-150 ease-in-out bg-white hover:bg-gray-200 hover:text-gray-500 focus:outline-none focus:border-blue-300 focus:shadow-outline-blue active:bg-gray-50 active:text-gray-800" 
              type="button" onClick={() => handleAddClip()}>
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" className="align-bottom inline" viewBox="0 0 16 16">
                  <path d="M3.5 3.5c-.614-.884-.074-1.962.858-2.5L8 7.226 11.642 1c.932.538 1.472 1.616.858 2.5L8.81 8.61l1.556 2.661a2.5 2.5 0 1 1-.794.637L8 9.73l-1.572 2.177a2.5 2.5 0 1 1-.794-.637L7.19 8.61 3.5 3.5zm2.5 10a1.5 1.5 0 1 0-3 0 1.5 1.5 0 0 0 3 0zm7 0a1.5 1.5 0 1 0-3 0 1.5 1.5 0 0 0 3 0z"/>
                </svg>
            </button>
            <span title="Zoom Out" className="text-center cursor-pointer -mt-0.5 mb-0.5 inline-block align-middle w-12 h-full px-4 py-2 text-sm font-medium leading-5 text-gray-700 transition duration-150 ease-in-out bg-white hover:bg-gray-200 hover:text-gray-500 active:bg-gray-50 active:text-gray-800"
              onClick={() => {if (currentZoom-1 > -1) setZoom(currentZoom-1);}}>
              -
            </span>
            <span className="text-center -mt-0.5 mb-0.5 inline-block align-middle w-auto h-full px-4 py-2 text-sm font-medium leading-5 text-gray-700 transition duration-150 ease-in-out bg-white hover:bg-gray-200 hover:text-gray-500 active:bg-gray-50 active:text-gray-800">
              {`${ZOOMS[currentZoom]}%`}
            </span>
            <span title="Zoom In" className="text-center cursor-pointer -mt-0.5 mb-0.5 inline-block align-middle w-12 h-full px-4 py-2 text-sm font-medium leading-5 text-gray-700 transition duration-150 ease-in-out bg-white hover:bg-gray-200 hover:text-gray-500 active:bg-gray-50 active:text-gray-800"
              onClick={() => {if (currentZoom+1 < ZOOMS.length) setZoom(currentZoom+1);}}>
              +
            </span>
          </div>
        </div>
      </div>
    </div>
  )
}