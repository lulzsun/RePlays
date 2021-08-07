import React, { useEffect } from 'react';

export default function Card () {
  function handleImageOnError() {
    
  }
	return (
    <div className="overflow-hidden shadow-lg rounded-lg h-90 md:w-auto cursor-pointer m-auto">
      <a href="#" className="w-full block h-full">
        <img alt="blog photo" src="/images/blog/1.jpg" className="max-h-40 w-full object-cover" 
          onError={(e)=>{(e.target as HTMLImageElement).onerror = null; (e.target as HTMLImageElement).src="video_thumbnail_placeholder.png"}}/>
        <div className="bg-white dark:bg-gray-800 w-full p-4">
          <p className="text-gray-800 dark:text-white text-xs font-medium mb-2">
            Game Unknown
          </p>
          <p className="text-gray-500 dark:text-gray-300 font-light text-xs">
            2021/8/7 | 4:20 PM | 0.30 GB
          </p>
      </div>
    </a>
  </div>
	)
}
