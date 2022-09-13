import Button from "../../components/Button";
import { postMessage } from "../../helpers/messenger";

interface Props {
    updateSettings: () => void;
    games: CustomGame[] | undefined;
  }

export const Games: React.FC<Props> = ({games, updateSettings}) => {
    return (
        <div className="flex flex-col gap-2 font-medium text-base pb-7"> 
        <h1 className="font-semibold text-2xl">Custom Games</h1>
        <span className="font-normal text-sm">Add .exe files to this list to ensure they will be detected and correctly named.</span>
        <Button text="Add Program" width={"auto"} onClick={(e) => {postMessage("AddProgram", "customgames")}}/>
        <div className="flex flex-row gap-1">
            <div className="flex flex-col gap-2">
                {games !== undefined && games.map((item) => {
                return <Button text={item.gameExe.replace(/^.*[\\\/]/, '')} width={"auto"} onClick={(e) => {postMessage("RemoveCustomGame", {"gameExe": item.gameExe, "gameName": item.gameName})}}/>
                }
                )}
            </div>
            <div className="flex flex-col gap-2">
                {games !== undefined && games.map((item) => {
                return <input className={`inline-flex align-middle justify-center w-full h-full px-4 py-2 text-sm font-medium leading-5 text-gray-700 transition duration-150 ease-in-out bg-white border border-gray-300 rounded-md hover:text-gray-700 focus:outline-none focus:border-blue-300 focus:shadow-outline-blue active:bg-gray-50 active:text-gray-800`}
                type="text" defaultValue={item.gameName} onBlur={(e) => {
                    if (games !== undefined)
                    item.gameName = e.target.value;
                    updateSettings();
                  }}/>
                })}
            </div>
        </div>
        </div>
	)
}

export default Games;