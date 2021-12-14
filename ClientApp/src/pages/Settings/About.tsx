import Button from "../../components/Button";
import { postMessage } from "../../helpers/messenger";

interface Props {
}

export const About: React.FC<Props> = ({}) => {
	return (
    <div className="flex flex-col gap-2 font-medium text-base pb-7"> 
      <h1 className="font-bold text-2xl">About</h1>
      <div className="cursor-pointer flex flex-row gap-2" 
      onClick={(e) => {postMessage("ShowFolder", "https://github.com/lulzsun/RePlays")}}>
        <img className="rounded-full" alt="GitHub-Mark-120px-plus.png" src="https://avatars.githubusercontent.com/u/28168454?v=4" width="32" height="32"/>
        <span className="font-normal text-sm">This client is currently being maintained by lulzsun! Thanks for the continued support! 😍🤟</span>
      </div>

      <span className="text-gray-700 mt-2">RePlays Pages</span>
      <span className="font-normal text-sm">Project Page:
        <a className="cursor-pointer underline pl-2" onClick={(e) => {postMessage("ShowFolder", "https://github.com/lulzsun/RePlays")}}>https://github.com/lulzsun/RePlays</a>
      </span>

      <span className="text-gray-700 mt-2">RePlays Team</span>
      <span className="font-normal text-sm">lulzsun:
        <a className="cursor-pointer underline pl-2" onClick={(e) => {postMessage("ShowFolder", "https://github.com/lulzsun")}}>https://github.com/lulzsun</a>
      </span>
      <span className="font-normal text-sm">Contributors:
        <a className="cursor-pointer underline pl-2" onClick={(e) => {postMessage("ShowFolder", "https://github.com/lulzsun/RePlays/graphs/contributors")}}>https://github.com/lulzsun/RePlays/graphs/contributors</a>
      </span>

      <span className="text-gray-700 mt-2">External libraries</span>
      <span className="font-normal text-sm">7-zip
        <a className="cursor-pointer underline pl-2" onClick={(e) => {postMessage("ShowFolder", "https://www.7-zip.org/")}}>https://www.7-zip.org/</a>
      </span>
      <span className="font-normal text-sm">FFmpeg
        <a className="cursor-pointer underline pl-2" onClick={(e) => {postMessage("ShowFolder", "https://ffmpeg.org/")}}>https://ffmpeg.org/</a>
      </span>
      <span className="font-normal text-sm">Squirrel
        <a className="cursor-pointer underline pl-2" onClick={(e) => {postMessage("ShowFolder", "https://github.com/Squirrel/Squirrel.Windows")}}>https://github.com/Squirrel/Squirrel.Windows</a>
      </span>

      <span className="text-gray-700 mt-2">License</span>
      <Button text="Open License" width={"auto"} onClick={(e) => {postMessage("ShowLicense")}}/>
    </div>
	)
}

export default About;