import React, { useEffect } from 'react';
import Card from '../components/Card';
import { getDirectories, getFiles } from '../helpers/fileIO';
import { postMessage } from '../helpers/messenger';

export default function Sessions () {
  useEffect(() => {    
    postMessage('RetrieveVideos', 'test');
    getDirectories('/Plays').then(dirs => {
      console.log('Folders: ', dirs);
    });
    getFiles('/Plays/League of Legends').then(files => {
      console.log('Files: ', files);
    });
  });

	return (
    <div className="flex flex-col h-full"> 
      <div className="flex-initial">
       <p>Hello from Sessions page!</p>
      </div>
      <div className="flex-auto overflow-auto h-full">
        <div className="gap-8 grid grid-flow-row sm:grid-cols-2 md:grid-cols-3 xl:grid-cols-4 2xl:grid-cols-5">
        <Card/> <Card/> <Card/> <Card/> <Card/> 
        <Card/> <Card/> <Card/> <Card/> <Card/> 
        <Card/> <Card/> <Card/> <Card/> <Card/>
        </div>
      </div>
      {/* <div style={{flex: '0 1 40px'}}>footer</div> */}
    </div>
	)
}