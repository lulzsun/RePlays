import { useContext, useEffect, useState } from "react";
import { postMessage } from '../helpers/messenger';
import { ModalContext } from "../Contexts";

interface Props {
    video: string;
    game: string;
    thumb?: string;
}

export const CompressModal: React.FC<Props> = ({video, game, thumb}) => {
  const modalCtx = useContext(ModalContext);

  const [quality, setQuality] = useState("medium");
  const [title, setTitle] = useState(video);

  useEffect(() => {
    modalCtx?.setConfirm(() => {postMessage("CompressClip", {filePath: `${game}/${video}`, game: `${game}`, quality: `${quality}`});});
    
  }, [quality, title, game, video]);
  
	return (
    <div className="flex flex-row gap-6">
      <img className="h-28 border" alt={"video_thumbnail_placeholder.png"} src={thumb}/>
      <div className="flex justify-center items-center">
        <div className="flex flex-col">
          Preset
          <div className="group relative w-full">
            <svg className="absolute w-5 h-5 -ml-8 mt-2 top-px left-full text-gray-700 group-hover:text-gray-700 cursor-pointer pointer-events-none" viewBox="0 0 20 20" fill="currentColor">
              <path fillRule="evenodd" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" clipRule="evenodd"/>
            </svg>
            <select className={`inline-flex justify-center w-full py-2 text-sm font-medium leading-5 text-gray-700 group-hover:text-gray-700 transition duration-150 ease-in-out bg-white border border-gray-300 rounded-md focus:outline-none focus:border-blue-300 focus:shadow-outline-blue active:bg-gray-50 active:text-gray-800 cursor-pointer`}
            name="destinationDdm" id="destinationDdm" defaultValue={quality} onChange={(e) => setQuality(e.target.value)}>
              <option value="ultrafast">Ultra fast</option>
              <option value="veryfast">Very fast</option>
              <option value="fast">Fast</option>
              <option value="medium" defaultChecked>Medium</option>
              <option value="slow">Slow</option>
              <option value="slower">Slower</option>
              <option value="veryslow">Very slow</option>
            </select>
          </div>
        </div>
      </div>
    </div>
	)
}

export default CompressModal;