import React from 'react';
import { useRef } from 'react';
import { useParams } from 'react-router-dom';

type PlayerParams = {
  game: string;
  video: string;
};

export default function Player () {
  let { game, video } = useParams<PlayerParams>();
  const videoElement = useRef<HTMLVideoElement>(null);
  return (
    <div className="h-full flex flex-col">
      {/* <div className="flex-initial">
        <b>top</b> 
      </div> */}
      <div className="relative flex w-full h-full bg-black justify-center cursor-pointer" 
        onClick={() => {
          (videoElement.current?.paused ? videoElement.current?.play() : videoElement.current?.pause())
        }}>
        <video ref={videoElement} className="absolute h-full" src={`${window.location.protocol}//${window.location.host}/Plays/${game}/${video}`}/>
      </div>
      <div className="flex flex-initial w-full h-20 border items-center justify-center">
        <b>top</b>
      </div>
    </div>
  )
}